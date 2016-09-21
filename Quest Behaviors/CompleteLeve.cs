using Clio.XmlEngine;
using ff14bot.NeoProfiles;
using TreeSharp;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.RemoteWindows;

namespace LeveTags
{
    [XmlElement("CompleteLeve")]
    public class CompleteLeveTag : ProfileBehavior
    {
        private bool done; 

        [XmlAttribute("NpcId")]
        public uint NpcId { get; set; }

        protected override Composite CreateBehavior()
        {
            var turnIn = new ActionRunCoroutine(r => TurnInLeve(NpcId));
            var update = new Action(r => done = true);

            return new PrioritySelector(turnIn, update);
        }

        protected override void OnResetCachedDone()
        {
            done = false;
        }

        public override bool IsDone
        {
            get { return done; }
        }

        public static async Task<bool> InteractIconString()
        {
            var npc = Core.Me.CurrentTarget;

            int ticks = 0;
            while (ticks < 20 && !SelectIconString.IsOpen)
            {
                npc.Interact();
                await Coroutine.Sleep(500);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> InteractString()
        {
            var npc = Core.Me.CurrentTarget;

            int ticks = 0;
            while (ticks < 20 && !SelectString.IsOpen)
            {
                npc.Interact();
                await Coroutine.Sleep(500);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> InteractTalk()
        {
            var npc = Core.Me.CurrentTarget;

            int ticks = 0;
            while (ticks < 20 && !Talk.DialogOpen)
            {
                npc.Interact();
                await Coroutine.Sleep(500);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> ChooseString(uint index)
        {
            if (!SelectString.IsOpen) { return false; }

            var lines = SelectString.Lines();

            int ticks = 0;
            while (ticks < 20 && SelectString.IsOpen)
            {
                SelectString.ClickSlot(index);
                await Coroutine.Sleep(500);
                ticks++;

                var current = SelectString.Lines();
                if (!lines.SequenceEqual(current)) { break; }
            }

            return true;
        }

        public static async Task<bool> ContinueTalk()
        {
            int ticks = 0;
            while (ticks < 20 && Talk.DialogOpen)
            {
                Talk.Next();
                await Coroutine.Sleep(200);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> Target(uint npcId)
        {
            int ticks = 0;
            while (Core.Me.CurrentTarget == null || Core.Me.CurrentTarget.NpcId != npcId && ticks < 50)
            {
                var npc = GameObjectManager.GetObjectByNPCId(npcId);
                if (npc != null) { npc.Target(); }
                await Coroutine.Sleep(500);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> SelectYesNo(bool choice = true)
        {
            int ticks = 0;

            while (SelectYesno.IsOpen && ticks < 20)
            {
                if (choice) { SelectYesno.ClickYes(); }
                else { SelectYesno.ClickNo(); }
                await Coroutine.Sleep(500);
                ticks++;
            }

            if (ticks < 20) { return true; }

            return false;
        }

        public static async Task<bool> SetJournalComplete()
        {
            int ticks = 0;
            while (ticks < 20 && JournalResult.IsOpen)
            {
                JournalResult.Complete();
                await Coroutine.Sleep(500);
                ticks++;
            }

            return true;
        }

        public static async Task<bool> SelectTeleport()
        {
            await Coroutine.Sleep(1000);
            RaptureAtkUnitManager.GetWindowByName("SelectYesnoCount").SendAction(1, 3, 0);
            await Coroutine.Sleep(5000);
            return true;
        }

        public static async Task<bool> TurnInLeve(uint npc)
        {
            await SelectTeleport();
            await Target(npc);
            await InteractTalk();
            await ContinueTalk();
            await ChooseString(0);
            await ContinueTalk();
            await SetJournalComplete();
            await ChooseString(4);
            await Coroutine.Sleep(2000);
            if (JournalResult.IsOpen) { JournalResult.Decline(); }
            await Coroutine.Sleep(2000);

            return true;
        }

        public static Composite CompleteLeve(uint npc)
        {
            return new ActionRunCoroutine(r => TurnInLeve(npc));
        }
    }
}
