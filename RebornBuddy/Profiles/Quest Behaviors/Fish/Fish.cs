namespace ExBuddy.NeoProfiles
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows.Media;

    using Clio.Utilities;
    using Clio.XmlEngine;

    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;
    using ff14bot.NeoProfiles;
    using ff14bot.RemoteWindows;
    using ff14bot.Settings;

    using TreeSharp;
    using Action = TreeSharp.Action;

    [XmlElement("Fish")]
    public class FishTag : ProfileBehavior
    {
        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x0101;

        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        protected static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        protected static void PostKeyPress(VirtualKeys key)
        {
            PostKeyPress((int)key);
        }

        protected static void PostKeyPress(int key)
        {
            PostMessage(Core.Memory.Process.MainWindowHandle, WM_KEYDOWN, new IntPtr(key), IntPtr.Zero);
            PostMessage(Core.Memory.Process.MainWindowHandle, WM_KEYUP, new IntPtr(key), IntPtr.Zero);
        }

        [Serializable]
        public enum Abilities : int
        {
            None = -1,
            Sprint = 3,
            Bait = 288,
            Cast = 289,
            Hook = 296,
            Mooch = 297,
            Stealth = 298,
            Quit = 299,
            Release = 300,
            CastLight = 2135,
            Snagging = 4100,
            CollectorsGlove = 4101,
            Patience = 4102,
            PowerfulHookset = 4103,
            Chum = 4104,
            FishEyes = 4105,
            PrecisionHookset = 4179,
            Patience2 = 9001 // Need to Check this value when i get skill
        }

        #region Fields

        private static bool isFishing;
        protected static Regex fishRegex = new Regex(@"You land an{0,1} (.+) measuring \d{1,4}\.\d ilms!", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected static Regex baitRegex = new Regex(@"You apply an{0,1} (.+) to your line.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static FishResult fishResult = new FishResult();
        private static string currentBait;

        private int baitCount = InventoryManager.FilledSlots.Count(bs => bs.Item.Affinity == 19);
        private bool _done;
        private int minfish = 20;
        private int maxfish = 30;
        private int mooch;
        private int fishcount;
        private int amissfish;
        private int fishlimit;
        private bool spotinit;

        #endregion

        #region Public Properties

        // TODO: Make custom type to keep HQ or reg fish by name
        // Keepers > Keeper attr HqOnly
        [XmlElement("Keepers")]
        public List<Keeper> Keepers { get; set; }

        [XmlElement("Collectables")]
        public List<Collectable> Collectables { get; set; }

        [XmlElement("FishSpots")]
        public IndexedList<FishSpot> FishSpots { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("Mooch")]
        public int MoochLevel { get; set; }

        [DefaultValue("True")]
        [XmlAttribute("MoochCondition")]
        public string MoochCondition { get; set; }

        [DefaultValue(20)]
        [XmlAttribute("MinFish")]
        public int MinimumFishPerSpot
        {
            get
            {
                return this.minfish;
            }

            set
            {
                this.minfish = value;
            }
        }

        [DefaultValue(30)]
        [XmlAttribute("MaxFish")]
        public int MaximumFishPerSpot
        {
            get
            {
                return this.maxfish;
            }

            set
            {
                this.maxfish = value;
            }
        }

        [XmlAttribute("Bait")]
        public string Bait { get; set; }

        [DefaultValue(1)]
        [XmlAttribute("BaitDelay")]
        public int BaitDelay { get; set; }

        [XmlAttribute("Chum")]
        public bool Chum { get; set; }

        [DefaultValue(VirtualKeys.N0)]
        [XmlAttribute("ConfirmKey")]
        public VirtualKeys ConfirmKey { get; set; }

        [DefaultValue(VirtualKeys.N6)]
        [XmlAttribute("MoveCursorRightKey")]
        public VirtualKeys MoveCursorRightKey { get; set; }

        [DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        [XmlAttribute("Weather")]
        public string Weather { get; set; }

        [XmlAttribute("ShuffleFishSpots")]
        public bool Shuffle { get; set; }

        [XmlAttribute("Stealth")]
        public bool Stealth { get; set; }

        [XmlAttribute("Collect")]
        public bool Collect { get; set; }

        [XmlAttribute("CollectabilityValue")]
        public int UCollectabilityValue { get; set; }
        public uint CollectabilityValue { get { return Convert.ToUInt32(UCollectabilityValue); } }

        [DefaultValue(Abilities.None)]
        [XmlAttribute("Patience")]
        public Abilities Patience { get; set; }

        [XmlAttribute("Snagging")]
        public bool Snagging { get; set; }

        [DefaultValue(Abilities.PowerfulHookset)]
        [XmlAttribute("Hookset")]
        public Abilities Hookset { get; set; }

        public override bool IsDone { get { return _done; } }

        public Version Version { get { return new Version(3, 0, 4); } }

        #endregion
        
        #region Fishing Composites

        protected Composite DismountComposite
        {
            get
            {
                return new Decorator(ret => Core.Player.IsMounted, CommonBehaviors.Dismount());
            }
        }

        protected Composite CollectablesComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Collect && SelectYesNoItem.IsOpen
                        && InventoryManager.FilledSlots.Count(c => c.BagId != InventoryBagId.KeyItems) <= 98,
                        new Sequence(
                            new Sleep(2, 3),
                            new Action(
                                r =>
                                    {
                                        uint value = 0;
                                        value = SelectYesNoItem.CollectabilityValue;

                                        if (value < 10) new Sleep(2, 3);

                                        value = SelectYesNoItem.CollectabilityValue;
                                        Log(
                                            string.Format(
                                                "Collectible caught with value: {0} required: {1}",
                                                value.ToString(),
                                                CollectabilityValue));
                                        if (value >= CollectabilityValue || value < 10)
                                        {
                                            Log("Collecting Collectible", Colors.Green);
                                            SelectYesNoItem.Yes();
                                        }
                                        else
                                        {
                                            Log("Declining Collectible", Colors.Red);
                                            SelectYesNoItem.No();
                                        }
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite FishCountLimitComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        fishcount >= fishlimit && CanDoAbility(Abilities.Quit) && !HasPatience
                        && !SelectYesNoItem.IsOpen,
                        new Action(
                            r =>
                                {
                                    DoAbility(Abilities.Quit);
                                    ChangeFishSpot();
                                }));
            }
        }

        protected Composite StopMovingComposite
        {
            get
            {
                return new Decorator(
                    ret => MovementManager.IsMoving,
                    new Action(
                        r =>
                            {
                                ff14bot.Managers.MovementManager.MoveForwardStop();
                            }));
            }
        }

        protected Composite InitFishSpotComposite
        {
            get
            {
                return new Decorator(
                    ret => !spotinit,
                    new Action(
                        r =>
                            {
                                FaceFishSpot();
                                isFishing = true;
                                Log("Will fish for " + fishlimit + " fish before moving again.");
                                spotinit = true;
                            }));
            }
        }

        protected Composite CheckWeatherComposite
        {
            get
            {
                return new Decorator(
                    ret => Weather != null && Weather != WorldManager.CurrentWeather,
                    new Sequence(
                        new Action(
                            r =>
                                {
                                    Log("Waiting for the proper weather...");
                                }),
                        new Wait(36000, ret => Weather == WorldManager.CurrentWeather, new ActionAlwaysSucceed())));
            }
        }

        protected Composite CollectorsGloveComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        CanDoAbility(Abilities.CollectorsGlove)
                        && Collect ^ HasCollectorsGlove,
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        Log("Casting Collector's Glove");
                                        DoAbility(Abilities.CollectorsGlove);
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite SnaggingComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        CanDoAbility(Abilities.Snagging)
                        && Snagging ^ HasSnagging,
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        Log("Toggle Snagging");
                                        DoAbility(Abilities.Snagging);
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite MoochComposite
        {
            get
            {
                return
                    new Decorator(
                        ret => CanDoAbility(Abilities.Mooch) && MoochLevel != 0 && mooch < MoochLevel && MoochConditionCheck(),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        ff14bot.Managers.FishingManager.Mooch();
                                        mooch++;
                                        if (MoochLevel > 1)
                                        {
                                            Log("Mooching, this is mooch " + mooch + " of " + MoochLevel + " mooches.");
                                        }
                                        else
                                        {
                                            Log("Mooching, this will be the only mooch.");
                                        }
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite ChumComposite
        {
            get
            {
                return new Decorator(
                    ret => Chum && !HasChum && CanDoAbility(Abilities.Chum),
                    new Sequence(new Action(r => DoAbility(Abilities.Chum)), new Sleep(1, 2)));
            }
        }

        protected Composite PatienceComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Patience > Abilities.None
                        && (FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady)
                        && !HasPatience && CanDoAbility(Patience)
                        && (Core.Player.CurrentGP >= 600 || Core.Player.CurrentGPPercent == 100f),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        DoAbility(Patience);
                                        Log("Patience activated");
                                    }),
                            new Sleep(1, 2)));
            }
        }

        protected Composite ReleaseComposite
        {
            get
            {
                return
                    new Decorator(
                        ret => FishingManager.State == FishingState.PoleReady && CanDoAbility(Abilities.Release),
                        new Action(
                            r =>
                                {
                                    ResetMooch();

                                    // Keep the fish
                                    if (this.Keepers.Count == 0
                                        || this.Keepers.Any(fishResult.IsKeeper)
                                        || (CanDoAbility(Abilities.Mooch) && MoochLevel != 0)) // Do not toss an HQ fish when mooch is active, even if the condition isn't met to currently mooch.
                                    {
                                        FishingManager.Cast();
                                        return;
                                    }

                                    Log("Released " + fishResult.FishName);

                                    // Release the fish
                                    DoAbility(Abilities.Release);
                                }));
            }
        }

        protected Composite CastComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady,
                        new Action(
                            r =>
                                {
                                    FishingManager.Cast();
                                    ResetMooch();
                                }));
            }
        }

        protected Composite HookComposite
        {
            get
            {
                return new Decorator(
                    ret => FishingManager.CanHook && FishingManager.State == FishingState.Bite,
                    new Action(
                        r =>
                            {
                                if (HasPatience && CanDoAbility(Hookset))
                                {
                                    DoAbility(Hookset);
                                    Log("Using (" + Hookset + ")");
                                }
                                else
                                {
                                    FishingManager.Hook();
                                }

                                amissfish = 0;
                                if (mooch == 0)
                                {
                                    fishcount++;
                                }

                                Log("Fished " + fishcount + " of " + fishlimit + " fish at this FishSpot.");
                            }));
            }
        }

        #endregion

        #region Composites

        protected Composite Conditional
        {
            get
            {
                return new Decorator(
                    ret => !ConditionCheck(),
                    new Action(
                        r =>
                        {
                            _done = true;
                        }));
            }
        }

        protected Composite Blacklist
        {
            get
            {
                return new Decorator(
                    ret => amissfish > FishSpots.Count,
                    new Action(
                        r =>
                        {
                            Log("The fish are amiss at all of the FishSpots.");
                            Log("This zone has been blacklisted, please fish somewhere else and then restart the profile.");
                            _done = true;
                        }));
            }
        }

        protected Composite InventoryFull
        {
            get
            {
                return
                    new Decorator(
                        ret => InventoryManager.FilledSlots.Count(c => c.BagId != InventoryBagId.KeyItems) >= 100,
                        new Action(
                            r =>
                                {
                                    _done = true;
                                }));
            }
        }

        private bool HasSpecifiedBait
        {
            get
            {
                return
                    InventoryManager.FilledSlots.Any(
                        i => string.Equals(i.Name, this.Bait, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        private bool IsBaitWindowOpen
        {
            get
            {
                return RaptureAtkUnitManager.Controls.Any(c => c.Name == "Bait");    
            }
        }

        private bool IsBaitSpecified
        {
            get
            {
                return !string.IsNullOrEmpty(this.Bait);    
            }
        }

        private bool IsCorrectBaitSelected
        {
            get
            {
                return string.Equals(currentBait, this.Bait, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        protected Composite OpenBait
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        IsBaitSpecified && !IsCorrectBaitSelected && !IsBaitWindowOpen
                        && CanDoAbility(Abilities.Bait),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        DoAbility(Abilities.Bait);
                                        PostKeyPress(this.MoveCursorRightKey);
                                    }),
                            new Sleep(this.BaitDelay)));
            }
        }

        protected Composite ApplyBait
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        IsBaitSpecified
                        && IsBaitWindowOpen
                        && HasSpecifiedBait,
                        new Sequence(
                            new Sleep(this.BaitDelay),
                            new Action(
                                r =>
                                    {
                                        if (IsCorrectBaitSelected)
                                        {
                                            Log("Correct Bait Selected -> " + this.Bait);
                                            DoAbility(Abilities.Bait);
                                            return;
                                        }

                                        PostKeyPress(this.MoveCursorRightKey);
                                        Thread.Sleep(100);

                                        PostKeyPress(this.ConfirmKey);
                                        Thread.Sleep(100);

                                        PostKeyPress(this.ConfirmKey);

                                        if (baitCount < -1 && !IsCorrectBaitSelected)
                                        {
                                            Log("Unable to find specified bait -> " + this.Bait + ", ending profile");
                                            _done = true;
                                        }
                                    }),
                            new Sleep(1, 2)));
            }
        }

        protected Composite CheckStealth
        {
            get
            {
                return new Decorator(
                    ret => Stealth == true && !Core.Player.HasAura(47),
                    new Sequence(
                        new Action(
                            r =>
                                {
                                    CharacterSettings.Instance.UseMount = false;
                                    DoAbility(Abilities.Stealth);
                                }),
                        new Sleep(2, 3)));
            }
        }

        protected Composite StateTransitionAlwaysSucceed
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        FishingManager.State == FishingState.Reelin ||
                        FishingManager.State == FishingState.Quit ||
                        FishingManager.State == FishingState.PullPoleIn,
                        new ActionAlwaysSucceed());
            }
        }

        protected Composite MoveToFishSpot
        {
            get
            {
                return new Decorator(
                    ret => Vector3.Distance(Core.Player.Location, FishSpots.CurrentOrDefault.XYZ) > 1,
                    CommonBehaviors.MoveAndStop(ret => FishSpots.CurrentOrDefault.XYZ, 1, stopInRange: true));
            }
        }

        #endregion

        #region Ability Checks and Actions

        protected bool CanDoAbility(Abilities ability)
        {
            return Actionmanager.CanCast((uint)ability, Core.Player);
        }

        protected void DoAbility(Abilities ability)
        {
            Actionmanager.DoAction((uint)ability, Core.Player);
        }

        #endregion



        protected bool HasPatience
        {
            get
            {
                return Core.Player.HasAura("Gathering Fortune Up");
            }
        }

        protected bool HasSnagging
        {
            get
            {
                return Core.Player.HasAura("Snagging");
            }
        }

        protected bool HasCollectorsGlove
        {
            get
            {
                return Core.Player.HasAura("Collector's Glove");
            }
        }

        protected bool HasChum
        {
            get
            {
                return Core.Player.HasAura("Chum");
            }
        }

        public static bool IsFishing()
        {
            return isFishing;
        }

        protected override void OnStart()
        {
            if (this.Keepers == null)
            {
                this.Keepers = new List<Keeper>();
            }

            if (this.Collectables == null)
            {
                this.Collectables = new List<Collectable>();
            }

            GamelogManager.MessageRecevied += ReceiveMessage;
            FishSpots.IsCyclic = true;
            isFishing = false;
            ShuffleFishSpots();

            if (IsBaitWindowOpen && CanDoAbility(Abilities.Bait))
            {
                DoAbility(Abilities.Bait);
            }

            if (CanDoAbility(Abilities.Quit))
            {
                DoAbility(Abilities.Quit);
            }
        }

        protected override void OnDone()
        {
            Thread.Sleep(6000);
            DoAbility(Abilities.Quit);
            isFishing = false;
            CharacterSettings.Instance.UseMount = true;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
            spotinit = false;
            fishcount = 0;
            isFishing = false;
        }

        protected override Composite CreateBehavior()
        {
            this.fishlimit = GetFishLimit();

            return new PrioritySelector(
                Conditional,
                Blacklist,
                InventoryFull,
                // TODO: GetBait
                OpenBait,
                ApplyBait,
                CheckStealth,
                StateTransitionAlwaysSucceed,
                MoveToFishSpot,
                GoFish(
                    DismountComposite,
                    StopMovingComposite,
                    CheckWeatherComposite,
                    InitFishSpotComposite,
                    CollectablesComposite,
                    MoochComposite,
                    ReleaseComposite,
                    FishCountLimitComposite,
                    CollectorsGloveComposite,
                    SnaggingComposite,
                    PatienceComposite,
                    ChumComposite,
                    CastComposite,
                    HookComposite));
        }

        protected Composite GoFish(params Composite[] children)
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => Vector3.Distance(Core.Player.Location, FishSpots.CurrentOrDefault.XYZ) < 2,
                        new PrioritySelector(children)));
        }

        #region Methods

        protected virtual bool ConditionCheck()
        {
            var conditional = ScriptManager.GetCondition(Condition);

            return conditional();
        }

        protected virtual bool MoochConditionCheck()
        {
            var moochConditional = ScriptManager.GetCondition(MoochCondition);

            return moochConditional();
        }

        protected virtual void FaceFishSpot()
        {
            double i = Clio.Common.MathEx.Random(0, 25);
            i = i/100;

            double i2 = Clio.Common.MathEx.Random(0, 100);

            if (i2 > 50)
            {
                Core.Player.SetFacing(FishSpots.Current.Heading - (float)i);
            }
            else
            {
                Core.Player.SetFacing(FishSpots.Current.Heading + (float)i);
            }
        }

        protected virtual void ChangeFishSpot()
        {
            FishSpots.Next();
            Log("Changing FishSpots...");
            fishcount = 0;
            Log("Resetting fish count...");
            fishlimit = this.GetFishLimit();
            spotinit = false;
            isFishing = false;
        }

        protected virtual int GetFishLimit()
        {
            return System.Convert.ToInt32(Clio.Common.MathEx.Random(this.MinimumFishPerSpot, this.MaximumFishPerSpot));
        }

        protected void ShuffleFishSpots()
        {
            if (Shuffle && FishSpots.Index == 0)
            {
                FishSpots.Shuffle();
                Log("Shuffled fish spots");
            }
        }

        protected void ResetMooch()
        {
            if (mooch != 0)
            {
                mooch = 0;
                Log("Resetting mooch level.");
            }
        }

        protected static string GetCurrentBait(string message)
        {
            if (baitRegex.IsMatch(message))
            {
                var match = baitRegex.Match(message);

                return match.Groups[1].Value;
            }

            return "Parse Error";
        }

        protected static FishResult GetFishResult(string message)
        {
            var fish = new FishResult();

            fish.Name = ParseFishName(message);
            fish.IsHighQuality = IsCatchHQ(fish.Name);

            return fish;
        }

        protected static string ParseFishName(string message)
        {
            if (fishRegex.IsMatch(message))
            {
                var match = fishRegex.Match(message);

                return match.Groups[1].Value;
            }

            return "Parse Error";
        }

        protected static bool IsCatchHQ(string fishname)
        {
            if(fishname[fishname.Length-2] == ' ')
            {
                return true;
            }

            return false;
        }

        protected void ReceiveMessage(object sender, ChatEventArgs e)
        {
            if (e.ChatLogEntry.MessageType == MessageType.SystemMessages && e.ChatLogEntry.Contents.StartsWith("You apply"))
            {
                currentBait = GetCurrentBait(e.ChatLogEntry.Contents);
                this.baitCount--;
                Log("Applied Bait -> " + currentBait);
            }

            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents.StartsWith("You land"))
            {
                fishResult = GetFishResult(e.ChatLogEntry.Contents);
            }

            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents == "The fish sense something amiss. Perhaps it is time to try another location.")
            {
                Log("The fish sense something amiss!");
                amissfish++;
                DoAbility(Abilities.Quit);
                ChangeFishSpot();
            }
        }

        protected void Log(string message, Color color)
        {
            Logging.Write(color, string.Format("[Fish v" + Version.ToString() + "] {0}", message));
        }

        protected void Log(string message)
        {
            Log(message, Colors.Gold);
        }

        #endregion
    }
}