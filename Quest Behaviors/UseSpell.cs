using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("UseSpell")]

    public class UseSpellTag : ProfileBehavior
    {
        private bool _done;
        [Clio.XmlEngine.XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get { return Position; }
            set { Position = value; }
        }

        public Vector3 Position;

        [XmlAttribute("Name")]
        public string Name { get; set; }
		
		[XmlAttribute("Condition")]
        public string Condition { get; set; }

        [XmlAttribute("SpellID")]
        [XmlAttribute("SpellId")]
        public uint SpellID { get; set; }

        [XmlAttribute("NPC")]
        [XmlAttribute("NpcId")]
        public uint NPC { get; set; }

        [DefaultValue(3)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }

        public override bool IsDone { get { return _done; } }

		public bool casted = false;

        private bool IsFinished
        {
            get
            {
                var unit = GameObjectManager.GetObjectByNPCId(NPC);

                if (unit != null)
                {
                    return !unit.IsVisible;
                }
                return true;
            }
        }

        protected Composite Behavior()
        {
            return new PrioritySelector(
				new Decorator(ret => Condition != null && !ConditionCheck(),
                    new Action(ret => _done = true)),
				new Decorator(ret => QuestId != 0 && StepId != 0 && QuestLogManager.GetQuestById(QuestId).Step > StepId,
                    new Action(r =>
                    {
                        _done = true;
                    })
                ),
				new Decorator(ret => Core.Player.IsMounted,
                    CommonBehaviors.Dismount()
                ),
				new Decorator(ret => Talk.DialogOpen,
                    new Action(r =>
                    {
                        Talk.Next();
                    })
                ),
				new Decorator(ret => casted,
                    new Sequence(
						new Sleep(3,5),
						new Action(r => casted = false)
					)
				),
                new Decorator(ret => Core.Me.Location.Distance(Position) <= Distance && IsFinished,
                    new Action(ret => _done = true)),
                new Decorator(ret => Core.Me.Location.Distance(Position) <= Distance && !casted,
                    new Action(r =>
                    {
                        Navigator.PlayerMover.MoveStop();
                        if (!Core.Player.IsCasting)
                        {
                            GameObjectManager.GetObjectByNPCId(NPC).Face();
                            if (ActionManager.DoAction(SpellID, GameObjectManager.GetObjectByNPCId(NPC)))
                            {
                                ActionManager.DoAction(SpellID, GameObjectManager.GetObjectByNPCId(NPC));
								casted = true;
                            }
                            else
                            {
                                ActionManager.DoActionLocation(SpellID, Core.Player.Location);
								casted = true;
                            }
                        }
                    })
				),
                CommonBehaviors.MoveAndStop(ret => Position, Distance, stopInRange: true, destinationName: Name),
                new ActionAlwaysSucceed()
                );
        }

        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            _done = false;
        }
		
		public bool ConditionCheck()
        {
            var Conditional = ScriptManager.GetCondition(Condition);

            return Conditional();
        }

        private Composite _cache;

        protected override void OnStart()
        {

            //We need to cache the behaviors creation because we use the GUID to remove it later.
            _cache = Behavior();
            TreeHooks.Instance.InsertHook("TreeStart", 0, _cache);
        }

        protected override void OnDone()
        {
            TreeHooks.Instance.RemoveHook("TreeStart", _cache);
        }
    }
}