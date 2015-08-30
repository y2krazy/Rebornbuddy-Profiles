namespace ff14bot.NeoProfiles
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Clio.Utilities;
    using Clio.XmlEngine;

    using ff14bot.Behavior;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Objects;
    using ff14bot.RemoteWindows;

    using TreeSharp;

    using Action = TreeSharp.Action;

    [XmlElement("Fate")]
    public class FateTag : ProfileBehavior
    {
        //Fate class variables
        public static Vector3 Position = new Vector3(0f, 0f, 0f);

        //-------
        public static int currentstep;

        private readonly int Distance = 2;

        private readonly bool Hunting = true;

        private bool _done;

        private int _max;

        private int _min;

        private int _timeout;

        private ITargetingProvider CachedProvider;

        private int Died;

        private uint fateid;

        private string FateName = "";

        //some Statistics

        private int FatesDone;

        private string FateStatus = "";

        //----------------------
        public bool IsCompleted = false;

        private uint localindex = 0;

        private int MobsHunted;

        //private int timeout = 100;
        private DateTime saveNow = DateTime.Now;

        [XmlElement("FateIds")]
        public List<string> FateIds { get; set; }

        [XmlAttribute("MaxLevel")]
        public string MaxLevel { get; set; }

        [XmlAttribute("MinLevel")]
        public string MinLevel { get; set; }

        [XmlAttribute("Timeout")]
        [DefaultValue("600")]
        public string timeout { get; set; }

        [DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        private Func<bool> conditionFunc;

        public override bool IsDone
        {
            get
            {
                return _done;
            }
        }

        protected bool Conditional()
        {
            if (conditionFunc == null)
            {
                conditionFunc = ScriptManager.GetCondition(Condition);
            }

            return conditionFunc();
        }

        protected Composite ShouldLevelSync
        {
            get
            {
                return new Decorator(
                    ret =>
                        {
                            var fate = FateManager.GetFateById(fateid);
                            // null or invalid, don't sync
                            if (fate == null || !fate.IsValid)
                            {
                                return false;
                            }

                            // not within the fate, don't sync
                            if (!fate.HotSpot.WithinHotSpot(Core.Player.Location))
                            {
                                return false;
                            }

                            // within level range, don't sync
                            if (fate.MaxLevel >= Core.Player.ClassLevel)
                            {
                                return false;
                            }

                            return true;
                        },new Action(r => ToDoList.LevelSync()));
            }
        }

        protected override Composite CreateBehavior()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => !this.Conditional(),
                        new Action(r => _done = true)),
                    ShouldLevelSync,
                    new Decorator(
                        ret => DateTime.Now > saveNow + TimeSpan.FromSeconds(_timeout) && currentstep == 0,
                        new Action(
                            r =>
                                {
                                    _done = true;
                                    Logging.Write("Timeout we are done for now");
                                    Logging.Write("--------------------------------------");
                                    Logging.Write("I did int this session {0} Fates", FatesDone);
                                    Logging.Write("I hunted and killed {0} mobs", MobsHunted);
                                    Logging.Write("I could not avoid to Die {0}times.", Died);
                                    Logging.Write("--------------------------------------");
                                })),
                    new Sequence(
                        new Action(
                            r =>
                                {
                                    if (Core.Me.IsDead)
                                    {
                                        Died++;
                                    }

                                    //update the Class variables for the Fates
                                    foreach (var item in FateManager.ActiveFates)
                                    {
                                        if (item.Id == fateid)
                                        {
                                            Position = item.Location;
                                            FateName = item.Name;
                                            FateStatus = item.Status.ToString();

                                            //Logging.Write("Update: Location: {0}, Name: {1} Status {2} {3}", item.Location, item.Name, FateStatus, fateid);
                                        }
                                    }
                                }),
                        new ActionAlwaysFail() //always fail that the rest of the tree is traveresd
                        ),

                    //Start fighting Fate Mobs but only when we are in close range to the fate position.TBD enhance this filter
                    new Decorator(
                        ret =>
                        !Core.Me.InCombat && fateid != 0 && !Core.Me.IsDead
                        && Vector3.Distance(Core.Player.Location, Position) < 10,
                        new Action(
                            r =>
                                {
                                    var target = getTargets();
                                    if (target == null)
                                    {
                                        Logging.Write("Did not find a Target.");
                                        if (IaFateactive(fateid) != null)
                                        {
                                            //enable movement again
                                            currentstep = 1;
                                            return;
                                        }
                                        Logging.Write("Fate done");
                                        this.FatesDone++;
                                        MovementManager.MoveForwardStop();
                                        currentstep = 0;
                                        this.fateid = 0;
                                        return;
                                    }

                                    //start killing fatemobs
                                    //Poi.Current = new Poi(getTargets() as GameObject, PoiType.Kill);
                                    Poi.Current.BattleCharacter.Target();
                                })),
                    new Decorator(
                        ret => currentstep == 1 && Vector3.Distance(Core.Player.Location, Position) > 5,
                        new Sequence(
                            CommonBehaviors.MoveAndStop(ret => Position, Distance, true, "Fates"),
                            new Action(r => { Logging.Write("Inside Moving"); })
                            //new Action(r => {currentstep=2;})
                            )),

                    //Find fates
                    new Decorator(
                        ret => FateManager.ActiveFates.FirstOrDefault() != null && currentstep == 0,
                        new Action(
                            r =>
                                {
                                    var currentfate = getFates();
                                    if (currentfate != null)
                                    {
                                        Logging.Write(currentfate);

                                        Logging.Write("We found a fate.");
                                        Position = currentfate.Location;
                                        fateid = currentfate.Id;
                                        currentstep = 1;
                                    }
                                    else
                                    {
                                        Logging.Write("No matching fates");
                                        //if hunting is enabled start killing any mobs around
                                        if (Hunting)
                                        {
                                            Logging.Write("Let's pass some time with hunting");
                                            Poi.Current = new Poi(this.getNormalTargets(), PoiType.Kill);
                                            MobsHunted++;
                                            if (Poi.Current != null)
                                            {
                                                Poi.Current.BattleCharacter.Target();
                                            }
                                        }
                                    }
                                })),
                    new ActionAlwaysSucceed());
        }

        public static GameObject getTargets()
        {
            var _target =
                GameObjectManager.GameObjects.Where(
                    unit =>
                    (unit as BattleCharacter) != null && unit.CanAttack && unit.IsTargetable && unit.IsVisible
                    && (unit as BattleCharacter).FateId != 0 && !(unit as BattleCharacter).IsDead)
                    .OrderBy(unit => unit.Distance(Core.Player.Location))
                    .Take(1);

            Logging.Write("Searching Targets");
            var targetArray = _target as GameObject[] ?? _target.ToArray();
            if (targetArray.Length > 0)
            {
                return targetArray[0];
            }
            return null;
        }

        public GameObject getNormalTargets()
        {
            //	var _target = GameObjectManager.GameObjects.Where(unit => (unit as BattleCharacter) != null && unit.CanAttack && unit.IsTargetable && unit.IsVisible
            //                                                            && (unit as BattleCharacter).FateId == 0 && !(unit as BattleCharacter).IsDead).OrderBy(unit => unit.Distance(Core.Player.Location)).Take(1);

            var _target =
                GameObjectManager.GameObjects.Where(
                    unit =>
                    (unit as BattleCharacter) != null && unit.CanAttack && unit.IsTargetable && unit.IsVisible
                    && (unit as BattleCharacter).FateId == 0 && !(unit as BattleCharacter).IsDead)
                    .OrderByDescending(unit => unit.Distance(Core.Player.Location))
                    .Take(1);

            var targetArray = _target as GameObject[] ?? _target.ToArray();
            if (targetArray.Length > 0 && targetArray[0].MaxHealth > (Core.Me.CurrentHealth) * 3)
            {
                return null;
            }
            if (targetArray.Length > 0 && targetArray[0].NpcId == 541)
            {
                return null;
            }
            if (targetArray.Length > 0)
            {
                return targetArray[0];
            }
            return null;
        }

        public FateData getFates()
        {
            return
                FateManager.ActiveFates.FirstOrDefault(
                    fate =>
                    fate.Level < _max && fate.Level > _min
                    && (FateIds.Count == 0 || FateIds.Any(f => int.Parse(f) == fate.Id)));
        }

        // check all fates and return the FateData wiht the given Id or null
        public static FateData IaFateactive(uint id)
        {
            var _fate = FateManager.ActiveFates.Where(fate => fate.Id == id).Take(1);
            var fateArray = _fate as FateData[] ?? _fate.ToArray();

            if (fateArray.Length > 0)
            {
                return fateArray[0];
            }
            return null;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
            conditionFunc = null;
        }

        protected override void OnStart()
        {
            if (FateIds == null)
            {
                FateIds = new List<string>();
            }

            foreach (var fateId in FateIds)
            {
                Logging.Write("Fate: Added fate " + fateId);
            }

            _min = Convert.ToInt32(MinLevel);
            _max = Convert.ToInt32(MaxLevel);
            _timeout = Convert.ToInt32(timeout);
            currentstep = 0;
            Logging.Write("Doing fates and hunt in between");
            Logging.Write("Stats: MinFate level={0} MaxFatelvl={1}", _min, _max);
            // MaxLevel = "34";
            // MinLevel = "25";
            CachedProvider = CombatTargeting.Instance.Provider;
            CombatTargeting.Instance.Provider = new MySuperAwesomeTargetingProvider();
            saveNow = DateTime.Now;
        }

        protected override void OnDone()
        {
            currentstep = 0;

            CombatTargeting.Instance.Provider = CachedProvider;
        }
    }

    //----------------------------------------------------------------------------------

    public class MySuperAwesomeTargetingProvider : ITargetingProvider
    {
        private BattleCharacter[] _aggroedBattleCharacters;

        public HashSet<uint> IgnoreNpcIds = new HashSet<uint> { 1201 };

        /// <summary> Gets the objects by weight. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <returns> The objects by weight. </returns>
        public List<BattleCharacter> GetObjectsByWeight()
        {
            var allUnits = GameObjectManager.GetObjectsOfType<BattleCharacter>().ToArray();

            _aggroedBattleCharacters = GameObjectManager.Attackers.ToArray();
            var inCombat = Core.Player.InCombat;
            var hostileUnits =
                allUnits.Where(r => IsValidUnit(inCombat, r))
                    .Select(n => new Score { Unit = n, Weight = GetScoreForUnit(n) })
                    .ToArray();

            // Order by weight (descending).
            return hostileUnits.OrderByDescending(s => s.Weight).Select(s => s.Unit).ToList();
        }

        /// <summary> Query if 'unit' is valid unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        private bool IsValidUnit(bool incombat, BattleCharacter unit)
        {
            if (!unit.IsValid || unit.IsDead || !unit.IsVisible || unit.CurrentHealthPercent <= 0)
            {
                return false;
            }

            if (IgnoreNpcIds.Contains(unit.NpcId))
            {
                return false;
            }

            // Ignore blacklisted mobs if they're in combat with us!
            if (Blacklist.Contains(unit.ObjectId, BlacklistFlags.Combat))
            {
                return false;
            }

            var fategone = unit.IsFateGone;
            if (fategone)
            {
                return false;
            }

            //Make sure we always return true for units inside our aggro list
            if (_aggroedBattleCharacters.Contains(unit))
            {
                return true;
            }

            if (!unit.IsFate)
            {
                return false;
            }

            // if (FateBot.CurrentFate == null || !FateBot.CurrentFate.IsValid)
            //   return false;

            if (!unit.CanAttack)
            {
                return false;
            }

            if (Vector3.Distance(unit.Location, FateTag.Position) > 50)
            {
                return false;
            }
            //  if (!FateBot.CurrentFate.HotSpot.WithinHotSpot2D(unit.Location))
            //      

            return !incombat;
        }

        /// <summary> Gets score for a unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <param name="unit"> The unit. </param>
        /// <returns> The score for unit. </returns>
        private double GetScoreForUnit(BattleCharacter unit)
        {
            double weight = 200 - (2 * unit.Distance());

            if (unit.CurrentTargetId == Core.Player.CurrentTargetId)
            {
                // Little extra weight on current targets.
                weight += 120;
            }

            //weight -= (int)npc.Toughness * 50;

            // Force 100 weight on any in-combat NPCs.
            if (_aggroedBattleCharacters.Contains(unit))
            {
                weight += 100;
            }

            //Units that are targeting the player, focus on low health ones so that we can reduce the incoming damage
            if (unit.CurrentTargetId == Core.Player.ObjectId)
            {
                weight += (100 - (unit.CurrentHealthPercent));
            }

            // Less weight on out of combat targets.
            if (!unit.InCombat)
            {
                weight -= 100;
            }

            return weight;
        }

        private class Score
        {
            public BattleCharacter Unit;

            public double Weight;
        }
    }

    //----------------------------------------------------------------------------------
}