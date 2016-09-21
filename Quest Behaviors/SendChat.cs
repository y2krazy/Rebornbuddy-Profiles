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
    [XmlElement("SendChat")]

    public class SendChatTag : ProfileBehavior
    {
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private bool _done;

        [XmlAttribute("Message")]
        public string Message { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("ATEntry")]
        public int ATEntry { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("Page")]
        public int Page { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("ATSubEntry")]
        public int ATSubEntry { get; set; }

        public bool Sent = false;

        public override bool IsDone { get { return _done; } }

        public Keys GetKey(int value)
        {
            switch(value)
            {
                case 0:
                    return Keys.D0;
                case 1:
                    return Keys.D1;
                case 2:
                    return Keys.D2;
                case 3:
                    return Keys.D3;
                case 4:
                    return Keys.D4;
                case 5:
                    return Keys.D5;
                case 6:
                    return Keys.D6;
                case 7:
                    return Keys.D7;
                case 8:
                    return Keys.D8;
                case 9:
                    return Keys.D9;
            }
            return Keys.D0;
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !Sent && !string.IsNullOrEmpty(Message) && ATEntry == -1,
                    new Action(r =>
                    {
                        ChatManager.SendChat(Message);
                        Sent = true;
						if(StepId == 0 && QuestId == 0)
						{
							_done = true;
						}
                    })
                ),
                new Decorator(ret => !Sent && string.IsNullOrEmpty(Message) && ATEntry != -1 && ATSubEntry != -1,
                    new Action(r =>
                    {
                        // Set up injection
                        IntPtr edit = Core.Memory.Process.MainWindowHandle;
                        const uint WM_KEYDOWN = 0x100;
                        const uint WM_KEYUP = 0x0101;
						const uint WM_CHAR = 0x0102;

                        // Send keys
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.Enter), IntPtr.Zero);
                        PostMessage(edit, WM_KEYUP, (IntPtr)(Keys.Enter), IntPtr.Zero);
                        Thread.Sleep(500);
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.Tab), IntPtr.Zero);
                        PostMessage(edit, WM_KEYUP, (IntPtr)(Keys.Tab), IntPtr.Zero);
                        Thread.Sleep(500);
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)(GetKey(ATEntry)), IntPtr.Zero);
                        PostMessage(edit, WM_KEYUP, (IntPtr)(GetKey(ATEntry)), IntPtr.Zero);
                        Thread.Sleep(500);
                        // If it's not on the first page, navigate to the correct page
                        if (Page > 1)
                        {
                            int i = 1;
                            while (i < Page)
                            {
                                PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.Right), IntPtr.Zero);
                                PostMessage(edit, WM_KEYUP, (IntPtr)(Keys.Right), IntPtr.Zero);
                                Thread.Sleep(500);
                                i++;
                            }
                        }
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)(GetKey(ATSubEntry)), IntPtr.Zero);
                        PostMessage(edit, WM_KEYUP, (IntPtr)(GetKey(ATSubEntry)), IntPtr.Zero);
                        Thread.Sleep(500);
                        PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.Enter), IntPtr.Zero);
                        PostMessage(edit, WM_KEYUP, (IntPtr)(Keys.Enter), IntPtr.Zero);
                        Thread.Sleep(500);

                        // Clean up
                        Sent = true;
                        if (StepId == 0 && QuestId == 0)
                        {
                            _done = true;
                        }
                    })
                ),
                new Decorator(ret => QuestLogManager.GetQuestById(QuestId).Step == StepId,
                    new ActionAlwaysSucceed()
                ),
                new Decorator(ret => QuestLogManager.GetQuestById(QuestId).Step > StepId,
                    new Action(r =>
                    {
                        _done = true;
                    })
                )
            );
        }

        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            _done = false;
        }

        protected override void OnDone()
        {

        }
    }
}