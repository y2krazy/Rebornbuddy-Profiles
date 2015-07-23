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
        #region Fields

        private static bool isFishing;
        private static Regex regex = new Regex(@"You land an{0,1} (.+) measuring \d{1,4}\.\d ilms!", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private FishResult fishResult = new FishResult();

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

        [XmlElement("ItemNames")]
        public List<string> ItemNames { get; set; }

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

        [DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        [XmlAttribute("Weather")]
        public string Weather { get; set; }

        [XmlAttribute("Stealth")]
        public bool Stealth { get; set; }

        [XmlAttribute("Collect")]
        public bool Collect { get; set; }

        [XmlAttribute("CollectabilityValue")]
        public int UCollectabilityValue { get; set; }
        public uint CollectabilityValue { get { return Convert.ToUInt32(UCollectabilityValue); } }

        [XmlAttribute("Patience")]
        public bool Patience { get; set; }

        [XmlAttribute("Snagging")]
        public bool Snagging { get; set; }

        [DefaultValue(4102)]
        [XmlAttribute("PatienceId")]
        public int UPatienceId { get; set; }
        public uint PatienceId { get { return Convert.ToUInt32(UPatienceId); } }

        // 4103 - Powerful | 4179 Precision
        [DefaultValue(4103)]
        [XmlAttribute("HooksetId")]
        public int UHooksetId { get; set; }
        public uint HooksetId { get { return Convert.ToUInt32(UHooksetId); } }

        public override bool IsDone { get { return _done; } }

        public Version Version { get { return new Version(3, 0, 4); } }

        #endregion
        
        #region Fishing Composites

        protected Composite DismountAction
        {
            get
            {
                return new Decorator(ret => Core.Player.IsMounted, CommonBehaviors.Dismount());
            }
        }

        protected Composite CollectablesAction
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Collect == true && SelectYesNoItem.IsOpen
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

        protected Composite FishCountLimitAction
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        fishcount >= fishlimit && Actionmanager.CanCast(299, Core.Player) && !HasPatience
                        && !SelectYesNoItem.IsOpen,
                        new Action(
                            r =>
                                {
                                    Actionmanager.DoAction(299, Core.Player);
                                    ChangeFishSpot();
                                }));
            }
        }

        protected Composite StopMovingAction
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

        protected Composite InitFishSpotAction
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

        protected Composite CheckWeatherAction
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

        protected Composite CollectorsGloveAction
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Actionmanager.HasSpell(4101) && (Collect == true && !Core.Player.HasAura("Collector's Glove"))
                        || (Collect != true && Core.Player.HasAura("Collector's Glove")),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        Log("Casting Collector's Glove");
                                        Actionmanager.DoAction(4101, Core.Player);
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite SnaggingAction
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Actionmanager.HasSpell(4100) && Actionmanager.CanCast(4100, Core.Player)
                        && ((Snagging == true && !HasSnagging) || (Snagging != true && HasSnagging)),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        Log("Toggle Snagging");
                                        Actionmanager.DoAction(4100, Core.Player);
                                    }),
                            new Sleep(2, 3)));
            }
        }

        protected Composite MoochAction
        {
            get
            {
                return
                    new Decorator(
                        ret => Actionmanager.CanCast(297, Core.Player) && MoochLevel != 0 && mooch < MoochLevel && MoochConditionCheck(),
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

        protected Composite PatienceAction
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                        Patience == true
                        && (FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady)
                        && !HasPatience && Actionmanager.CanCast(PatienceId, Core.Player)
                        && (Core.Player.CurrentGP >= 600 || Core.Player.CurrentGPPercent == 100f),
                        new Sequence(
                            new Action(
                                r =>
                                    {
                                        Actionmanager.DoAction(PatienceId, Core.Player);
                                        Log("Patience activated");
                                    }),
                            new Sleep(1, 2)));
            }
        }

        protected Composite ReleaseAction
        {
            get
            {
                return
                    new Decorator(
                        ret => FishingManager.State == FishingState.PoleReady && Actionmanager.CanCast(300, Core.Player),
                        new Action(
                            r =>
                                {
                                    ResetMooch();

                                    // Keep the fish
                                    if (this.ItemNames.Count == 0
                                        || this.ItemNames.Contains(
                                            this.fishResult.FishName,
                                            StringComparer.InvariantCultureIgnoreCase))
                                    {
                                        FishingManager.Cast();
                                        return;
                                    }

                                    Log("Released " + this.fishResult.FishName);

                                    // Release the fish
                                    Actionmanager.DoAction(300, Core.Player);
                                }));
            }
        }

        protected Composite CastAction
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

        protected Composite HookAction
        {
            get
            {
                return new Decorator(
                    ret => FishingManager.CanHook && FishingManager.State == FishingState.Bite,
                    new Action(
                        r =>
                            {
                                if (HasPatience && Actionmanager.CanCast(HooksetId, Core.Player))
                                {
                                    Actionmanager.DoAction(HooksetId, Core.Player);
                                    Log("Using Hookset (" + HooksetId + ")");
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
                                    Actionmanager.DoAction(298, Core.Player);
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

        private bool HasPatience
        {
            get
            {
                return Core.Player.HasAura("Gathering Fortune Up");
            }
        }

        private bool HasSnagging
        {
            get
            {
                return Core.Player.HasAura("Snagging");
            }
        }

        public static bool IsFishing()
        {
            return isFishing;
        }

        protected override void OnStart()
        {
            if (this.ItemNames == null)
            {
                this.ItemNames = new List<string>();
            }

            if (this.Collectables == null)
            {
                this.Collectables = new List<Collectable>();
            }

            GamelogManager.MessageRecevied += ReceiveMessage;
            FishSpots.IsCyclic = true;
            isFishing = false;
        }

        protected override void OnDone()
        {
            Thread.Sleep(6000);
            Actionmanager.DoAction(299, Core.Player);
            isFishing = false;
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
                CheckStealth,
                StateTransitionAlwaysSucceed,
                MoveToFishSpot,
                GoFish(
                    DismountAction,
                    CollectablesAction,
                    FishCountLimitAction,
                    StopMovingAction,
                    InitFishSpotAction,
                    CheckWeatherAction,
                    CollectorsGloveAction,
                    SnaggingAction,
                    MoochAction,
                    PatienceAction,
                    ReleaseAction,
                    CastAction,
                    HookAction));
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

        private bool ConditionCheck()
        {
            var conditional = ScriptManager.GetCondition(Condition);

            return conditional();
        }

        private bool MoochConditionCheck()
        {
            var moochConditional = ScriptManager.GetCondition(MoochCondition);

            return moochConditional();
        }

        private void FaceFishSpot()
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

        private void ChangeFishSpot()
        {
            FishSpots.Next();
            Log("Changing FishSpots...");
            fishcount = 0;
            Log("Resetting fish count...");
            fishlimit = this.GetFishLimit();
            spotinit = false;
            isFishing = false;
        }

        private int GetFishLimit()
        {
            return System.Convert.ToInt32(Clio.Common.MathEx.Random(this.MinimumFishPerSpot, this.MaximumFishPerSpot));
        }

        private void ResetMooch()
        {
            if (mooch != 0)
            {
                mooch = 0;
                Log("Resetting mooch level.");
            }
        }

        private FishResult GetFishResult(string message)
        {
            var fish = new FishResult();

            fish.Name = this.ParseFishName(message);
            fish.IsHighQuality = this.IsCatchHQ(fish.Name);

            return fish;
        }

        private string ParseFishName(string message)
        {
            if (regex.IsMatch(message))
            {
                var match = regex.Match(message);

                return match.Groups[1].Value;
            }

            return "Parse Error";
        }

        private bool IsCatchHQ(string fishname)
        {
            if(fishname[fishname.Length-2] == ' ')
            {
                return true;
            }

            return false;
        }
        
        private void ReceiveMessage(object sender, ChatEventArgs e)
        {
            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents.StartsWith("You land"))
            {
                this.fishResult = this.GetFishResult(e.ChatLogEntry.Contents);
            }

            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents == "The fish sense something amiss. Perhaps it is time to try another location.")
            {
                Log("The fish sense something amiss!");
                amissfish++;
                Actionmanager.DoAction(299, Core.Player);
                ChangeFishSpot();
            }
        }

        private void Log(string message, Color color)
        {
            Logging.Write(color, string.Format("[Fish v" + Version.ToString() + "] {0}", message));
        }

        private void Log(string message)
        {
            Log(message, Colors.Gold);
        }

        #endregion
    }
}