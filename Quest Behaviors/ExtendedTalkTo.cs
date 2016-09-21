using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags {
    [XmlElement("ExtendedTalkTo")]
    class ExtendedTalkToTag : TalkToTag {
        protected override Composite CreateBehavior() {
            return new PrioritySelector(
                new Decorator(c => QuestLogManager.InCutscene && RemoteWindows.SelectYesno.IsOpen, new Action(ctx => { RemoteWindows.SelectYesno.ClickYes(); })),
                new Decorator(c => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                base.CreateBehavior()
            );
        }
    }
}