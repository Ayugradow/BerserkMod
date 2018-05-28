using System;
using System.Collections.Generic;
using BerserkMod.Extensions;
using GlobalEnums;
using HutongGames.PlayMaker;
using InControl;
using ModCommon;
using Modding;
using UnityEngine;
using Object = UnityEngine.Object;
namespace BerserkMod
{
    public class Berserker : Mod
    {
        private static Berserker _instance;

        public override string GetVersion() => "0.1.1";

        public override void Initialize()
        {
            _instance = this;
            _instance.Log("BerserkerMod initializing!");

            On.HeroController.FixedUpdate += HeroController_FixedUpdate;
            On.HeroController.TakeDamage += HeroController_TakeDamage;
            On.HeroController.CharmUpdate += HeroController_CharmUpdate;
            ModHooks.Instance.HitInstanceHook += Instance_HitInstanceHook;
            On.HeroController.Update += HeroController_Update;
            On.NailSlash.StartSlash += NailSlash_StartSlash;
            

            _berserkToggle.berserkL.AddDefaultBinding(InputControlType.LeftStickButton);
            _berserkToggle.berserkR.AddDefaultBinding(InputControlType.RightStickButton);
            _berserkToggle.berserkKb.AddDefaultBinding(Key.Backspace);

            _instance.Log("BerserkerMod initialized!");

        }

        private void NailSlash_StartSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            self.GetComponent<tk2dSpriteAnimator>().Sprite.color = Color.black;
            orig(self);
        }

        private void HeroController_Update(On.HeroController.orig_Update orig, HeroController self)
        {
            orig(self);
            if ((!_berserkToggle.berserkR.IsPressed || !_berserkToggle.berserkL.IsPressed) &&
                !_berserkToggle.berserkKb.IsPressed)
            {
                activeTimer = 2f;
                cameraZoom.ZoomFactor = 1f;
            }
            else if (PlayerData.instance.GetInt("health") > 1)
            {
                if (!berserkOn)
                {
                    if (activeTimer > 0f)
                    {
                        if (furyVignette != null)
                        {
                            foreach (SpriteRenderer spriteRenderer in furyVignette.GetComponent<FadeGroup>().spriteRenderers)
                            {
                                spriteRenderer.enabled = true;
                                spriteRenderer.color = new Color(255f, 153f, 0f);
                                spriteRenderer.drawMode = SpriteDrawMode.Simple;
                            }
                        }

                        if (audioPlayerUI != null)
                        {
                            audioPlayerUI.Spawn(HeroController.instance.gameObject.transform.position,
                                Quaternion.Euler(Vector3.up));
                            audioPlayerUI.GetComponent<AudioSource>().Play();
                        }

                        Log(activeTimer);
                        activeTimer -= Time.fixedDeltaTime;
                        cameraZoom.ZoomFactor = - 0.1f * activeTimer + 1.2f;
                    }
                    else
                    {
                        if (furyVignette != null)
                        {
                            foreach (SpriteRenderer spriteRenderer in furyVignette.GetComponent<FadeGroup>().spriteRenderers)
                            {
                                spriteRenderer.enabled = false;
                            }
                        }

                        if (audioPlayerUI != null)
                            audioPlayerUI.GetComponent<AudioSource>().Stop();

                        Log("Activating BERSERK");

                        cameraZoom.ZoomFactor = 1;
                        berserkOn = true;
                        deactivate = false;
                        playedFury = false;
                        activeTimer = 2f;
                    }
                }
            }

            if (!playedFury)
            {
                if (fury != null)
                {
                    fury.GetComponent<ParticleSystem>().Play();
                    Log("Fury");
                }

                if (renderer != null)
                {
                    renderer.enabled = true;
                    Log("Renderer");
                }

                if (furyParticle != null)
                {
                    furyParticle.GetComponent<ParticleSystem>().Play();
                    Log("Fury particle");
                }

                if (rage != null)
                {
                    rage.SetActive(true);
                    Log("Rage");
                }

                playedFury = true;
            }

            if (deactivate)
            {
                if (renderer != null)
                    renderer.enabled = false;

                if (fury != null)
                    fury.GetComponent<ParticleSystem>().Stop();

                if (furyParticle != null)
                    furyParticle.GetComponent<ParticleSystem>().Stop();

                if (rage != null)
                    rage.SetActive(false);

                if (furyVignette != null)
                {
                    foreach (SpriteRenderer spriteRenderer in furyVignette.GetComponent<FadeGroup>().spriteRenderers)
                    {
                        spriteRenderer.enabled = false;
                    }
                }

                deactivate = false;
            }

            //foreach (MeshRenderer meshRenderer in slashMeshRenderer)
            //{
            //    if (meshRenderer == null || !meshRenderer.enabled) continue;
            //    meshRenderer.material.color = Color.black;
            //}
        }

        private HitInstance Instance_HitInstanceHook(Fsm owner, HitInstance hit)
        {
            if (!PlayerData.instance.GetBool("equippedCharm_29") || !berserkOn) return hit;
            hit.Multiplier = 2f;
            return hit;
        }

        private void HeroController_CharmUpdate(On.HeroController.orig_CharmUpdate orig, HeroController self)
        {
            orig(self);
            if (!PlayerData.instance.GetBool("equippedCharm_29") || !berserkOn) return;
            animator.Stop();
            animator.Play("Hive Health Recover2");
            animator.PlayFromFrame(0);
            played1 = true;
            played2 = false;
            downTimer = 1f;
        }

        private void HeroController_TakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            if (!PlayerData.instance.GetBool("equippedCharm_29") || !berserkOn) orig(self, go, damageSide, damageAmount, hazardType);
            animator.Stop();
            animator.Play("Hive Health Recover2");
            animator.PlayFromFrame(0);
            played1 = true;
            played2 = false;
            downTimer = 1f;
            orig(self, go, damageSide, 2*damageAmount, hazardType);
        }

        private void HeroController_FixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
        {
            orig(self);
            if (!setup)
            {
                PlayerData.instance.SetBool("hasCharm", true);
                PlayerData.instance.SetBool("gotCharm_29", true);
                PlayerData.instance.SetInt("charmCost_29", 0);
                setup = true;
            }

            if (!PlayerData.instance.GetBool("equippedCharm_29")) return;

            if (cameraZoom == null)
            {
                foreach (var camera in Object.FindObjectsOfType<Transform>())
                {
                    if (camera.gameObject.GetComponent<tk2dCamera>() == null) continue;
                    cameraZoom = camera.gameObject.GetComponent<tk2dCamera>();
                    cameraCtrl = cameraZoom.GetComponent<CameraController>();

                    if (cameraCtrl == null)
                        cameraCtrl = cameraZoom.gameObject.AddComponent<CameraController>();

                    Log($"Got camera {cameraZoom} and camera controller {cameraCtrl}");
                }
            }

            if (hero == null)
            {
                hero = FSMUtility.LocateFSM(HeroController.instance.gameObject, "ProxyFSM");
                HeroController.instance.gameObject.PrintSceneHierarchyTree("Knight");

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
                    sprite.FlashGrimmflame();
                    break;
                }
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

            if (furyVignette == null)
            {
                foreach (Transform transform in Object.FindObjectsOfType<Transform>())
                {
                    if (!transform.gameObject.name.Contains("fury_effects_v2")) continue;
                    furyVignette = transform.gameObject;
                    furyVignette.PrintSceneHierarchyTree("Fury Vignette");
                    Log($"Found fury vignette {transform.gameObject.name}");
                }
            }

            if (hero == null || hive == null || blob == null || fury == null || rage == null || !berserkOn) return;
            if (blob.transform.localPosition != def + 0.94f * (PlayerData.instance.GetInt("health") - 1) * Vector3.right)
            {
                Log(@"Updating health");
                furyParticle.transform.localPosition = blob.transform.localPosition;
                blob.transform.localPosition = def + 0.94f * (PlayerData.instance.GetInt("health") - 1) * Vector3.right;
            }

            if (downTimer > 0f && PlayerData.instance.GetInt("health") > 1)
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
            else if (PlayerData.instance.GetInt("health") <= 1)
            {
                berserkOn = false;
                deactivate = true;
                renderer.enabled = false;
                fury.GetComponent<ParticleSystem>().Stop();
                rage.SetActive(false);
            }
        }

        public PlayMakerFSM hive;
        public PlayMakerFSM hero;
        public float downTimer = 1f;
        public float activeTimer = 2;
        public bool played1;
        public bool played2;
        public bool playedFury = true;
        public bool deactivate;
        public SpriteFlash sprite;
        public tk2dSpriteAnimator animator;
        public MeshRenderer renderer;
        public GameObject blob;
        public GameObject fury;
        public GameObject rage;
        public GameObject audioPlayerUI;
        public GameObject furyVignette;
        public AudioClip audioClip;
        private Vector3 def;
        private readonly Toggle _berserkToggle = new Toggle();
        private bool setup;
        private bool berserkOn;
        private GameObject furyParticle;
        private tk2dCamera cameraZoom;
        private CameraController cameraCtrl;
        private Color defSlashColor;
    }
}
