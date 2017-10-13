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
    [XmlElement("PickupQuestPlus")]

    class PickupQuestPlusTag : PickupQuestTag
    {
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => QuestId == 65713 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65714 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65715 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65716 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65717 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65718 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65719 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65752 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 65789 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65987 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66046 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67023 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67201 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67254 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67591 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67670 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67935 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67937 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67942 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67943 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67966 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68024 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68107 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68112 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68120 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68168 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68429 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68451 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68487 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(2);
                    })
                ),
                new Decorator(ret => QuestId == 68488 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68496 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 68508 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                base.CreateBehavior()
                );
        }
    }
}