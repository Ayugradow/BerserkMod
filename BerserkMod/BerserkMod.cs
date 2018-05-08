using System.Linq;
using Modding;
using UnityEngine;
using BerserkMod.Extensions;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using InControl;
using ModCommon;

namespace BerserkMod
{
    public class Berserker : Mod, ITogglableMod
    {
        private static Berserker _instance;

        public override string GetVersion() => "0.0.1";

        public override void Initialize()
        {
            _instance = this;
            _instance.Log("BerserkerMod initializing!");

            On.HeroController.FixedUpdate += HeroController_FixedUpdate;
            On.HeroController.TakeDamage += HeroController_TakeDamage;
            On.HeroController.CharmUpdate += HeroController_CharmUpdate;
            On.HutongGames.PlayMaker.Actions.TakeDamage.OnEnter += TakeDamage_OnEnter;
            ModHooks.Instance.HitInstanceHook += Instance_HitInstanceHook;
            ModHooks.Instance.HeroUpdateHook += Instance_HeroUpdateHook;
            

            _berserkToggle.berserkL.AddDefaultBinding(InputControlType.LeftStickButton);
            _berserkToggle.berserkR.AddDefaultBinding(InputControlType.RightStickButton);
            _berserkToggle.berserkKb.AddDefaultBinding(Key.End);

            _instance.Log("BerserkerMod initialized!");
        }

        private void Instance_HeroUpdateHook()
        {
            Log($"{berserkOn}");
            if ((!_berserkToggle.berserkR.IsPressed || !_berserkToggle.berserkL.WasPressed) &&
                (!_berserkToggle.berserkR.WasPressed || !_berserkToggle.berserkL.IsPressed) &&
                !_berserkToggle.berserkKb.WasPressed) return;
            berserkOn = !berserkOn;
            if (renderer != null)
                renderer.enabled = berserkOn;
            if (fury != null)
                if (berserkOn)
                {
                    fury.GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    fury.GetComponent<ParticleSystem>().Stop();
                }

            if (furyParticle != null)
                if (berserkOn)
                {
                    furyParticle.GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    furyParticle.GetComponent<ParticleSystem>().Stop();
                }

            if (rage != null)
                rage.SetActive(berserkOn);
            if (audioPlayerUI != null)
            {
                if (berserkOn)
                {
                    audioPlayerUI.Spawn(HeroController.instance.gameObject.transform.position,
                        Quaternion.Euler(Vector3.up));
                    audioPlayerUI.GetComponent<AudioSource>().Play();
                }
                else
                {
                    audioPlayerUI.GetComponent<AudioSource>().Stop();
                }
            }
            if (berserkOn)
            {
                knightAnimator.Play("SD Charge Ground");
                PlayMakerFSM.BroadcastEvent("SUPERDASH CHARGING G");
                knightAnimator.PlayFromFrame(0);
            }
            else
            {
                knightAnimator.Stop();
            }
        }

        private HitInstance Instance_HitInstanceHook(Fsm owner, HitInstance hit)
        {
            if (!PlayerData.instance.equippedCharm_29 || !berserkOn) return hit;
            hit.Multiplier = 2f;
            return hit;
        }

        private void HeroController_CharmUpdate(On.HeroController.orig_CharmUpdate orig, HeroController self)
        {
            orig(self);
            if (!PlayerData.instance.equippedCharm_29 || !berserkOn) return;
            animator.Stop();
            animator.Play("Hive Health Recover2");
            animator.PlayFromFrame(0);
            played1 = true;
            played2 = false;
            downTimer = 1f;
        }

        private void HeroController_TakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
        {
            if (!PlayerData.instance.equippedCharm_29 || !berserkOn) orig(self, go, damageSide, damageAmount, hazardType);
            animator.Stop();
            animator.Play("Hive Health Recover2");
            animator.PlayFromFrame(0);
            played1 = true;
            played2 = false;
            downTimer = 1f;
            orig(self, go, damageSide, 2*damageAmount, hazardType);
        }

        private void TakeDamage_OnEnter(On.HutongGames.PlayMaker.Actions.TakeDamage.orig_OnEnter orig, TakeDamage self)
        {
            orig(self);
            if (!PlayerData.instance.equippedCharm_29 || !berserkOn) return;
            animator.Stop();
            animator.Play("Hive Health Recover2");
            animator.PlayFromFrame(0);
            played1 = true;
            played2 = false;
            downTimer = 10f;
            if (self.Target.Value.GetComponent<HealthManager>() == null) return;
            hitTimer = 2f;
        }

        private void HeroController_FixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
        {
            orig(self);
            if (!PlayerData.instance.gotCharm_29)
            {
                PlayerData.instance.hasCharm = true;
                PlayerData.instance.gotCharm_29 = true;
                PlayerData.instance.charmCost_29 = 0;
                setup = true;
            }

            if (!PlayerData.instance.equippedCharm_29) return;
            if (hero == null)
            {
                hero = FSMUtility.LocateFSM(HeroController.instance.gameObject, "ProxyFSM");

                Log($"Got hero {hero.name}");
            }

            if (hive == null)
            {
                List<PlayMakerFSM> fsms = PlayMakerFSM.FsmList;
                foreach (PlayMakerFSM fsm in fsms)
                {
                    if (fsm.FsmName != "Hive Health Regen") continue;
                    Log($"Got hive {fsm.FsmName}");
                    hive = fsm;
                    foreach (FsmState state in hive.FsmStates)
                    {
                        state.ClearTransitions();
                        state.RemoveActionsOfType<FsmStateAction>();
                    }
                    break;
                }
            }

            if (blob == null)
            {
                foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
                {
                    if (!go.name.Contains("Recovery")) continue;
                    blob = go;
                    blob.SetActive(true);
                    Log($"Got blob {blob}");
                    renderer = blob.GetComponent<MeshRenderer>();
                    renderer.SetActiveWithChildren(berserkOn);
                    animator = blob.GetComponent<tk2dSpriteAnimator>();
                    def = blob.transform.localPosition;
                    sprite = blob.GetComponent<SpriteFlash>() ?? blob.AddComponent<SpriteFlash>();
                    sprite.FlashingFury();
                    break;
                }
            }

            if (knightAnimator == null)
            {
                knightAnimator = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
                Log($"Got Knight animator {knightAnimator} and clip {knightAnimator.GetClipByName("SD Charge Ground")}");
            }

            if (fury == null)
            {
                foreach (Transform transform in Object.FindObjectsOfType<Transform>())
                {
                    if (!transform.gameObject.name.Contains("Charm Effects")) continue;
                    fury = transform.gameObject.FindGameObjectNameContainsInChildren("Fury");
                    Log($"Got fury effect {fury}");
                    break;
                }
            }

            if (rage == null)
            {
                foreach (Transform transform in Object.FindObjectsOfType<Transform>())
                {
                    if (!transform.gameObject.name.Contains("Charm Effects")) continue;
                    rage = transform.gameObject.FindGameObjectNameContainsInChildren("Rage");
                    Log($"Got rage effect {rage}");
                    break;
                }
            }

            if (audioPlayerUI == null)
            {
                foreach (Transform transform in Object.FindObjectsOfType<Transform>())
                {
                    if (!transform.gameObject.name.Contains("Fury")) continue;
                    audioPlayerUI = transform.gameObject;
                    audioClip = audioPlayerUI.GetComponent<AudioSource>().clip;
                    Log($"Got audio player UI {audioPlayerUI} and clip {audioClip}");
                    break;
                }
            }

            if (furyParticle == null)
            {
                foreach (Transform transform in Object.FindObjectsOfType<Transform>())
                {
                    if (!transform.gameObject.name.Contains("Fury Particle")) continue;
                    furyParticle = transform.gameObject;
                    Log($"Got fury particle effect {furyParticle}");
                    break;
                }
            }

            //if (audioClip == null)
            //{
            //    foreach (var clip in Object.FindObjectsOfType<Transform>())
            //    {
            //        //audioClip = clip;
            //        if (clip.gameObject.name != "Health 1") continue;
            //        clip.gameObject.PrintSceneHierarchyTree("Health 1");
            //        Log($"Got audio clip {clip}");
            //    }
            //}

            if (hero == null || hive == null || blob == null || fury == null || rage == null || !berserkOn) return;
            if (blob.transform.localPosition != def + 0.94f * (PlayerData.instance.health - 1) * Vector3.right)
            {
                Log(@"Updating health");
                furyParticle.transform.localPosition = blob.transform.localPosition;
                blob.transform.localPosition = def + 0.94f * (PlayerData.instance.health - 1) * Vector3.right;
            }

            if (downTimer > 0f && hitTimer <= 0 && PlayerData.instance.health > 1)
            {
                if (!played1)
                {
                    animator.Stop();
                    animator.Play("Hive Health Recover2");
                    animator.PlayFromFrame(0);
                    played1 = true;
                }

                if (downTimer < 0.5f && !played2)
                {
                    animator.Stop();
                    animator.Play("Hive Health Recover1");
                    animator.PlayFromFrame(0);
                    played2 = true;
                }

                downTimer -= Time.fixedDeltaTime;
            }
            else if (downTimer <= 0f)
            {
                played1 = false;
                played2 = false;
                HeroController.instance.TakeHealth(1);
                hero.SendEvent("HeroCtrl-HeroDamaged");
                hero.SendEvent("HERO DAMAGED");
                downTimer = 1f;
            }
            else if (PlayerData.instance.health <= 1)
            {
                berserkOn = false;
                renderer.enabled = false;
                fury.GetComponent<ParticleSystem>().Stop();
                rage.SetActive(false);
            }

            if (hitTimer > 0f)
            {
                hitTimer -= Time.fixedDeltaTime;
            }
            else
            {
                hitTimer = 0f;
            }
        }

        public void Unload()
        {
            _instance.Log("Disabling BerserkerMod!");
        }

        public PlayMakerFSM hive;
        public PlayMakerFSM hero;
        public float downTimer = 1f;
        public float hitTimer;
        public bool played1;
        public bool played2;
        public SpriteFlash sprite;
        public tk2dSpriteAnimator animator;
        public tk2dSpriteAnimator knightAnimator;
        public MeshRenderer renderer;
        public GameObject blob;
        public GameObject fury;
        public GameObject rage;
        public GameObject audioPlayerUI;
        public AudioClip audioClip;
        private Vector3 def;
        private Toggle _berserkToggle = new Toggle();
        private bool setup;
        private bool berserkOn;
        private GameObject furyParticle;
    }
}
