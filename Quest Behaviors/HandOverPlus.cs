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

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("HandOverPlus")]

    class HandOverPlusTag : HandOverTag
    {
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => QuestId == 65997 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66182 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 66856 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(1);
                    })
                ),
                new Decorator(ret => QuestId == 67019 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 67238 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68271 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 68299 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
				new Decorator(ret => QuestId == 68407 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68487 && SelectYesno.IsOpen,
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