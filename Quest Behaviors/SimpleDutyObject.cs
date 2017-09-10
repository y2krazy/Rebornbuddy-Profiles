using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using System;
using System.Collections.Generic;
using System.Threading;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags {
    [XmlElement("SimpleDutyObject")]
    class SimpleDutyObjectTag : SimpleDutyTag {
        private Dictionary<string, bool> Checkpoints = new Dictionary<string, bool>();

        public void Reset(object sender, EventArgs e) {
            Checkpoints.Clear();
        }

        public bool HasCheckPointReached(object cp) {
            string checkpoint = cp.ToString();
            if (!Checkpoints.ContainsKey(checkpoint)) Checkpoints.Add(checkpoint, false);
            return Checkpoints[checkpoint];
        }

        public void CheckPointReached(object cp) {
            string checkpoint = cp.ToString();
            if (!Checkpoints.ContainsKey(checkpoint)) Checkpoints.Add(checkpoint, true);
            else Checkpoints[checkpoint] = true;
        }

        protected override void OnStart() {
            ff14bot.NeoProfiles.GameEvents.OnPlayerDied += Reset;
            base.OnStart();
        }

        protected override void OnResetCachedDone() {
            Reset(null, null);
            base.OnResetCachedDone();
        }

        protected Composite Q67124() {
            // (67124) Heavensward - At the end of Our Hope
            Vector3 c1 = new Vector3(175.0911f, 130.9083f, -430.1f);
            Vector3 c2 = new Vector3(362.1796f, 137.2033f, -383.6978f);
            Vector3 c3 = new Vector3(444.7922f, 160.8083f, -566.6742f);

            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67124 && GameObjectManager.GetObjectByNPCId(2005850) != null && GameObjectManager.GetObjectByNPCId(2005850).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005850).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005850).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005850).Location, 3)
                    )
                ),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(1) && Core.Me.Location.Distance(c1) < 5, new Action(a => { CheckPointReached(1); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(1), CommonBehaviors.MoveAndStop(ret => c1, 3)),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(2) && Core.Me.Location.Distance(c2) < 5, new Action(a => { CheckPointReached(2); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(2), CommonBehaviors.MoveAndStop(ret => c2, 3)),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(3) && Core.Me.Location.Distance(c3) < 3, new Action(a => { CheckPointReached(3); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(3), CommonBehaviors.MoveAndStop(ret => c3, 3)),
                base.CreateBehavior()
            );
        }

        protected Composite Q67131() {
            // (67131) Heavensward - A Series of Unfortunate Events
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67131 && GameObjectManager.GetObjectByNPCId(4129) != null && GameObjectManager.GetObjectByNPCId(4129).IsVisible && Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(4129).Location) > 20,
                    CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(4129).Location, 15)
                ),
                new Decorator(ret => QuestId == 67131 && GameObjectManager.GetObjectByNPCId(2005710) != null && GameObjectManager.GetObjectByNPCId(2005710).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005710).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005710).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005710).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        protected Composite Q67137() {
            // (67137) Heavensward -  Keeping the Flame Alive
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67137 && GameObjectManager.GetObjectByNPCId(2005546) != null && GameObjectManager.GetObjectByNPCId(2005546).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005546).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005546).Interact();
                                CheckPointReached(1);
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005546).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67137 && GameObjectManager.GetObjectByNPCId(2006332) != null && GameObjectManager.GetObjectByNPCId(2006332).IsVisible && !Core.Player.InCombat && HasCheckPointReached(1),
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006332).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2006332).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006332).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        protected Composite Q68123() {
            // (68123) Stormblood - With Heart and Steel
            Vector3 c1 = new Vector3(-299.7701f, -116.947f, -343.4173f);
            Vector3 c2 = new Vector3(-300f, -74.1117f, -416.5f);

            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 68123 && GameObjectManager.GetObjectByNPCId(2007630) != null && GameObjectManager.GetObjectByNPCId(2007630).IsVisible,
                    new PrioritySelector(
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007630).Location, 1)
                    )
                ),
                new Decorator(ret => DutyManager.InInstance && QuestId == 68123 && HasCheckPointReached(1) && Core.Me.Location.Distance(c1) < 3,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(c1) < 3,
                            new Action(r => {
                                MovementManager.MoveForwardStart();
                                Thread.Sleep(3000);
                                MovementManager.MoveForwardStop();
                            })
                        )
                    )
                ),
                new Decorator(ret => DutyManager.InInstance && QuestId == 68123 && !HasCheckPointReached(1) && Core.Me.Location.Distance(c1) < 3, new Action(a => { CheckPointReached(1); })),
                new Decorator(ret => DutyManager.InInstance && !Core.Player.InCombat && QuestId == 68123 && !HasCheckPointReached(1), CommonBehaviors.MoveAndStop(ret => c1, 3)),
                new Decorator(ret => DutyManager.InInstance && QuestId == 68123 && !HasCheckPointReached(2) && Core.Me.Location.Distance(c2) < 5, new Action(a => { CheckPointReached(2); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 68123 && !HasCheckPointReached(2), CommonBehaviors.MoveAndStop(ret => c2, 3)),
                base.CreateBehavior()
            );
        }

        protected override Composite CreateBehavior() {
            if (QuestId == 67124) return Q67124();
            if (QuestId == 67131) return Q67131();
            if (QuestId == 67137) return Q67137();
            if (QuestId == 68123) return Q68123();

            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                base.CreateBehavior()
            );
        }
    }
}