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
	[XmlElement("TurnInPlus")]

	class TurnInPlusTag : TurnInTag
	{
		public bool actiontaken = false;

		protected override void OnDone()
		{
			actiontaken = false;
			base.OnDone();
		}

		protected override Composite CreateBehavior()
		{
			return new PrioritySelector(
                new Decorator(ret => QuestId == 65539 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 65964 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 66133 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66740 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(1);
                    })
                ),
                new Decorator(ret => QuestId == 67138 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67405 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(4);
                    })
                ),
				new Decorator(ret => QuestId == 67670 && SelectYesno.IsOpen,
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