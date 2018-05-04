using System.Linq;
using Modding;
using UnityEngine;
using BerserkMod.Extensions;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;

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
            On.HutongGames.PlayMaker.Actions.TakeDamage.OnEnter += TakeDamage_OnEnter;

            _instance.Log("BerserkerMod initialized!");
        }

        private void TakeDamage_OnEnter(On.HutongGames.PlayMaker.Actions.TakeDamage.orig_OnEnter orig, TakeDamage self)
        {
            orig(self);
            if (self.Target.Value.GetComponent<HealthManager>() == null) return;
            hitTimer = 2f;
            self.Multiplier.Value *= 1.75f;
        }

        private void HeroController_FixedUpdate(On.HeroController.orig_FixedUpdate orig, HeroController self)
        {
            orig(self);
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
                    break;
                }
                
                Log("Got hive");
            }
            else if (check == null || idle == null || recovery2 == null || recovery1 == null)
            {
                foreach (FsmState fsmState in hive.FsmStates)
                {
                    Log(fsmState.Name);
                }
                check = hive.GetState("Check");
                idle = hive.GetState("Idle");
                recovery2 = hive.GetState("Recover 2");
                recovery1 = hive.GetState("Recover 1");
                Log("Got states");

                

                check.RemoveActionsOfType<PlayerDataBoolTest>();
                check.RemoveActionsOfType<SetMeshRenderer>();

                recovery2.RemoveActionsOfType<SetFloatValue>();
                recovery2.RemoveActionsOfType<FloatAdd>();
                recovery2.RemoveActionsOfType<FloatCompare>();

                recovery1.RemoveActionsOfType<FloatAdd>();
                recovery1.RemoveActionsOfType<FloatCompare>();

                check.AddAction(new SetMeshRenderer
                {
                    gameObject = new FsmOwnerDefault
                    {
                        GameObject = GameObject.Find("Recovery Blob")
                    },
                    active = new FsmBool
                    {
                        Value = true
                    }

                });
                check.AddAction(new NextFrameEvent
                {
                    sendEvent = FsmEvent.Finished
                });

                check.ClearTransitions();
                idle.ClearTransitions();
                recovery2.ClearTransitions();
                recovery1.ClearTransitions();

                check.AddTransition("FINISHED", "Idle");
                idle.AddTransition("DRAINING 1", "Recover 2");
                recovery2.AddTransition("DRAINING 2", "Recover 1");

                check.AddTransition("DAMAGE TAKEN", "Recover 2");
                idle.AddTransition("DAMAGE TAKEN", "Recover 2");
                recovery2.AddTransition("DAMAGE TAKEN", "Recover 2");
                recovery1.AddTransition("DAMAGE TAKEN", "Recover 2");
            }
            else
            {
                if (downTimer > 0f && hitTimer <= 0)
                {
                    if (oldtimer <= 0f)
                    {
                        hive.SendEvent("DRAINING 1");
                    }
                    oldtimer = downTimer;
                    downTimer -= Time.fixedDeltaTime;
                    if (downTimer <= 5f)
                    {
                        hive.SendEvent("DRAINING 2");
                    }
                }
                else if (downTimer <= 0f)
                {
                    oldtimer = downTimer;
                    HeroController.instance.AddHealth(0);
                    HeroController.instance.TakeHealth(1);
                    hero.SendEvent("HeroCtrl-HeroDamaged");
                    hero.SendEvent("HERO DAMAGED");
                    downTimer = 10f;
                }

                if (hitTimer > 0f)
                {
                    hitTimer -= Time.fixedDeltaTime;
                    downTimer = 10f;
                }
                else
                {
                    hitTimer = 0f;
                }
            }

        }

        public void Unload()
        {
            _instance.Log("Disabling BerserkerMod!");
        }

        public FsmEvent draining2 = new FsmEvent("DRAINING 2");
        public FsmEvent draining1 = new FsmEvent("DRAINING 1");
        public FsmState idle;
        public FsmState check;
        public FsmState recovery1;
        public FsmState recovery2;
        public PlayMakerFSM hive;
        public PlayMakerFSM hero;
        public float oldtimer;
        public float downTimer = 10f;
        public float hitTimer;
        public int hitCounter;
    }
}
