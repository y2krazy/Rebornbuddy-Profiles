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
    [XmlElement("GroundTargeting")]

    public class GroundTargetingTag : ProfileBehavior
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

        [XmlAttribute("ItemID")]
        [XmlAttribute("ItemId")]
        public uint ItemID { get; set; }

        [DefaultValue(10)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }
		
		[XmlAttribute("Condition")]
        public string Condition { get; set; }
		
		[XmlAttribute("NPC")]
        [XmlAttribute("NpcId")]
        public uint NPC { get; set; }
		
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

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
				new Decorator(ret => !ConditionCheck(),
                    new Action(ret => _done = true)),
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
                new Decorator(ret => Core.Me.Location.Distance(Position) <= Distance,
                    new PrioritySelector(
                        new Decorator(ret => ActionManager.InSpellInRangeLOS(1, GameObjectManager.GetObjectByNPCId(NPC)) == Enums.SpellRangeCheck.ErrorNotInLineOfSight,
                            CommonBehaviors.MoveToLos(r => GameObjectManager.GetObjectByNPCId(NPC), true)
                        ),
                        new Decorator(ret => true,
                            new Action(r =>
                            {
                                if (Core.Player.IsMounted)
                                {
                                    ActionManager.Dismount();
                                }
                                Navigator.PlayerMover.MoveStop();
                                if (!Core.Player.IsCasting)
                                {
							        ActionManager.DoActionLocation(Enums.ActionType.KeyItem, ItemID, XYZ);
							        casted = true;
                                }
                            })
                        )
                    )
				),
                CommonBehaviors.MoveAndStop(ret => Position, Distance, stopInRange: true, destinationName: Name)
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

        protected override void OnDone()
        {
			
        }
    }
}