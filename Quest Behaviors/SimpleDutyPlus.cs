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
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{	
    [XmlElement("SimpleDutyPlus")]

    class SimpleDutyPlusTag : SimpleDutyTag
    {
		public bool didthething = false;
		
		public void ResetThing(object sender, EventArgs e)
		{
			didthething = false;
		}
		
		protected override void OnStart()
		{
			ff14bot.NeoProfiles.GameEvents.OnPlayerDied += ResetThing;
			base.OnStart();
		}
		
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
				CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestId == 65888 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65889 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67137 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68612 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                base.CreateBehavior()
                );
        }
    }
}