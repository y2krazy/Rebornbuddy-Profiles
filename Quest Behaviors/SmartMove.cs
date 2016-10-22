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
    [XmlElement("SmartMove")]

    public class SmartMoveTag : ProfileBehavior
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

        [DefaultValue(3)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("UseMesh")]
        public bool UseMesh { get; set; }

        public override bool IsDone { get { return _done; } }

        protected Composite Behavior()
        {
            return new PrioritySelector(
                new Decorator(ret => QuestId != 0 && QuestLogManager.GetQuestById(QuestId).Step > StepId,
                    new Action(r =>
                    {
                        Poi.Clear("Exiting SmartMove tag");
                        _done = true;
                    })
                ),
                new Decorator(ret => Core.Me.Location.Distance(Position) <= Distance,
                    new Action(ret => 
                        {
                            MovementManager.MoveForwardStop();
                            Poi.Clear("Exiting SmartMove tag");
                            _done = true;
                        }
                    )
                ),
                new Decorator(ret => Talk.DialogOpen,
                    new Action(ret => Talk.Next())
                ),
                new Decorator(ret => !UseMesh,
                    new Action(ret => Navigator.PlayerMover.MoveTowards(Position))
                ),
                new Decorator(ret => UseMesh,
                    CommonBehaviors.MoveAndStop(ret => Position, Distance, stopInRange: true, destinationName: Name)
                ),
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