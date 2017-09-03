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
    [XmlElement("UseObjectPlus")]

    class UseObjectPlusTag : UseObjectTag
    {
        public override Composite CustomLogic
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => QuestId == 66043 && SelectYesno.IsOpen,
                        new Action(r =>
                        {
                            SelectYesno.ClickYes();
                        })
                    ),
                    new Decorator(ret => QuestId == 66064 && SelectYesno.IsOpen,
                        new Action(r =>
                        {
                            SelectYesno.ClickYes();
                        })
                    ),
                    new Decorator(ret => QuestId == 66082 && SelectYesno.IsOpen,
                        new Action(r =>
                        {
                            SelectYesno.ClickYes();
                        })
                    ),
                    new Decorator(ret => QuestId == 67677 && SelectYesno.IsOpen,
                        new Action(r =>
                        {
                            SelectYesno.ClickYes();
                        })
                    ),
                    new Decorator(ret => QuestId == 67695 && JournalResult.IsOpen,
                        new Action(r =>
                        {
                            JournalResult.Complete();
                        })
                    ),
                    new Decorator(ret => QuestId == 68497 && SelectYesno.IsOpen,
                        new Action(r =>
                        {
                            SelectYesno.ClickYes();
                        })
                    ),
                    base.CustomLogic
                    );
            }
        }
    }
}