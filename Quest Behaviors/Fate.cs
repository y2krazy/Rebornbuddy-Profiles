using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.BotBases;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Fate")]
    public class FateTag : ProfileBehavior
    {
        private bool _done;
        private int _min, _max, _timeout;
        private uint localindex = 0;

        [XmlAttribute("MaxLevel")]
        public string MaxLevel { get; set; }

        [XmlAttribute("MinLevel")]
        public string MinLevel { get; set; }

        [XmlAttribute("Timeout")]
        [DefaultValue("600")]
        public string timeout { get; set; }

        private BattleCharacter npc;
        private FatebotSettings fatebotInstance = FatebotSettings.Instance;

        //private int timeout = 100;
        private uint LastFateId = 0;

        private DateTime saveNow = DateTime.Now;
        private bool Hunting = false;
        public override bool IsDone { get { return _done; } }

        //some Statistics
        private FateData currentfate;

        private int FatesDone;
        private int MobsHunted;
        private int Died;

        //----------------------
        public bool IsCompleted = false;

        //Fate class variables
        public static Vector3 Position = new Vector3(0f, 0f, 0f);

        private uint fateid = 0;
        private string FateName = "";
        private string FateStatus = "";
        private ITargetingProvider tempProvider;

        //-------
        public static int currentstep = 0;  //currentstep 1 we are in a fate / currentstep 0 we are not in a fate

        private static readonly Stopwatch ClusterTimer = Stopwatch.StartNew();
        private int Distance = 2;

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                 new Decorator(ret => DateTime.Now > saveNow + TimeSpan.FromSeconds(_timeout) && currentstep == 0,
                               new Action(r => OnTimeout())),
                 // This one will run always kind of a pulse one
                 new Sequence(
                    new Action(r => CountDeath()),
                    new Action(r => IsFateStillActive()),
                    new Action(r => UpdateFateData()),
                    new ActionAlwaysFail() //always fail that the rest of the tree is traveresd
                 ),
                 //Start fighting Fate Mobs but only when we are in close range to the fate position.TBD enhance this filter

            #region sync        //level Sync

                 new Decorator(r => currentfate != null && FateManager.WithinFate && currentfate.MaxLevel < Core.Player.ClassLevel && !Core.Me.IsLevelSynced,
                  new ActionRunCoroutine(async r =>
                  {
                      Logging.Write("Applying Level Sync.");

                      ToDoList.LevelSync();

                      await Coroutine.Sleep(500);

                      return false;
                  })
                  ),

            #endregion sync        //level Sync

            #region Movment

                  new Decorator(ret => currentstep == 1 && Vector3.Distance(Core.Player.Location, Position) > (currentfate.Radius - 10),
                         CommonBehaviors.MoveAndStop(ret => Position, Distance, stopInRange: true, destinationName: "Moving to Fates.")

                  ),

            #region Handin

                  new Decorator(r => currentfate != null && FateManager.WithinFate && currentfate.Icon == FateIconType.KillHandIn && currentfate.TimeLeft.Minutes <= 8,
                     new Sequence(
                          new Action(r =>
                         {
                             Poi.Clear("Handing in items.");
                             Logging.Write("Hand-in Fate");
                             var npc = GameObjectManager
                                    .GetObjectsOfType<BattleCharacter>()
                                    .Where(
                                        b => b.IsFate && !b.CanAttack && b.FateId == currentfate.Id);
                             var q = from s in npc
                                     group s by s into g
                                     orderby g.Count() descending
                                     select g.Key;
                             if (q.LastOrDefault() == null)
                             {
                                 Logging.Write("Could not find handin NPC. Something is wrong.");
                                 return;
                             }
                             tempProvider = CombatTargeting.Instance.Provider;
                             CombatTargeting.Instance.Provider = new NullTargetingProvider();
                             MoveTo(q.LastOrDefault().Location);
                             GameObjectManager.GetObjectByNPCId(q.LastOrDefault().NpcId).Interact();
                             Talk.Next();
                             InventoryManager.GetBagByInventoryBagId(InventoryBagId.KeyItems).FilledSlots.LastOrDefault().Handover();
                             Request.HandOver();
                             CombatTargeting.Instance.Provider = tempProvider;
                         }),
                          new ActionAlwaysFail() //always fail that the rest of the tree is traveresd
                          )),

                          new Decorator(ret => Talk.DialogOpen,
                            new Action(r =>
                            {
                                Talk.Next();
                            })),
                            new Decorator(ret => Request.IsOpen,
                            new Action(r =>
                            {
                                GameObjectManager.GetObjectByNPCId(npc.NpcId).Interact();
                                InventoryManager.GetBagByInventoryBagId(InventoryBagId.KeyItems).FilledSlots.LastOrDefault().Handover();
                                Request.HandOver();
                            })),

                  //Find fates

            #endregion Handin

            #endregion Movment

            #region escort

                  new Decorator(r => currentfate != null && fateid != 0 && Poi.Current.Type != PoiType.Kill,
                  new ActionRunCoroutine(async r => MoveToFocusedFate())

                ),

            #endregion escort

                  new Decorator(ret => currentfate == null && currentstep == 0,
                  new Sequence(
                  new Action(r =>
                  {
                      getFates();
                      if (currentfate != null)
                      { GoFate(); }
                      else
                      { GoHunting(); }
                  }
                            )
            )),
                     new ActionAlwaysSucceed()
            );
        }

        // End of B Tree

        private async Task MoveToFocusedFate()
        {
            Vector3 currentMove;
            if (currentfate.Icon == FateIconType.ProtectNPC ||
                  currentfate.Icon == FateIconType.ProtectNPC2)
            {
                if (ClusterTimer.ElapsedMilliseconds > 5000)
                {
                    Logging.Write("Moving using cluster logic.");

                    var x = 0.0f;
                    var y = 0.0f;
                    var z = 0.0f;
                    var total = 0.0f;
                    GameObjectManager.GetObjectsOfType<BattleCharacter>()
                        .Where(
                            bc =>
                                bc.Type == GameObjectType.Pc &&
                                bc.Location.Distance(currentfate.Location) < currentfate.Radius)
                        .ForEach(
                            bc =>
                            {
                                total++;
                                x += bc.Location.X;
                                y += bc.Location.Y;
                                z += bc.Location.Z;
                            });
                    currentMove = new Vector3(x / total, y / total, z / total);
                    Navigator.MoveTo(currentMove);

                    ClusterTimer.Restart();
                }
            }
            else
            {
                Poi.Current = new Poi(getFateTargets(), PoiType.Kill);
            }
        }

        private void GoFate()
        {
            if (currentfate != null)
            {
                Logging.Write("Fate Details: {0}", currentfate);
                Position = currentfate.Location;
                fateid = currentfate.Id;
                currentstep = 1;
            }
        }

        private void GoHunting()
        {
            if (currentfate == null)
            {
                if (Hunting)
                {
                    Logging.Write("Let's pass some time with hunting!");
                    var target = getNormalTargets();
                    if (target != null)
                    {
                        Poi.Current = new Poi(target, PoiType.Kill);
                        MobsHunted++;
                    }
                    if (Poi.Current != null)
                        Poi.Current.BattleCharacter.Target();
                }
            }
        }

        private void SetLastFate()
        {
            LastFateId = currentfate.Id;
            Logging.Write("Setting last Fate to {0}.", currentfate.Name);
        }

        private void UpdateFateData()
        {
            foreach (FateData item in FateManager.ActiveFates)
            {
                if (item.Id == fateid)
                {
                    Position = item.Location;
                    FateName = item.Name;
                    FateStatus = item.Status.ToString();
                }
            }
        }

        private void IsFateStillActive()
        {
            if (currentstep > 0)
            {
                int found = 0;
                foreach (FateData item in FateManager.ActiveFates)
                {
                    if (item.Id == fateid) { found = 1; }
                }
                if (found == 0)
                {
                    if (currentfate != null)
                        SetLastFate();
                    currentstep = 0;
                    fateid = 0;
                    currentfate = null;
                    Logging.Write("Fate no longer active.");
                }
            }
        }

        private void CountDeath()
        {
            if (Core.Me.IsDead)
            { Died++; }
        }

        private async Task<bool> EscortFate()
        {
            Logging.Write("ESCORT");

            if (currentfate.Icon == FateIconType.ProtectNPC ||
                    currentfate.Icon == FateIconType.ProtectNPC2)
            {
                var npc = GameObjectManager
                    .GetObjectsOfType<BattleCharacter>()
                    .FirstOrDefault(
                        b => b.IsFate && !b.CanAttack && b.FateId == currentfate.Id);
                Logging.Write("NPC ={0}", npc);
                if (npc != null && npc.IsValid && (npc.IsBehind || npc.IsFlanking) &&
                    Core.Me.Distance(npc) > 7)
                {
                    Logging.Write("Moving using escort fate logic.");
                    Navigator.Stop();

                    Func<bool> isInFront = () => !npc.IsBehind;
                    Logging.Write("M1");
                    await Coroutine.Sleep(500);
                    Logging.Write("M2");
                    MovementManager.MoveForwardStart();
                    while (npc.IsValid && (Core.Me.Distance(npc) > 1 || !isInFront()))
                    {
                        Core.Me.Face(npc);
                        if (!MovementManager.IsMoving)
                            MovementManager.MoveForwardStart();
                        await Coroutine.Sleep(200);
                    }
                    Logging.Write("M3");
                    await Coroutine.Sleep(700);
                    MovementManager.MoveForwardStop();

                    Logging.Write("Reached destination, moving stopped.");
                }
                else
                {
                    if (FateManager.WithinFate)
                    {
                        Logging.Write("Idle in escort fate.");
                    }
                }
            }
            else
            {
                Poi.Current = new Poi(getFateTargets(), PoiType.Kill);
            }

            return true;
        }

        private void OnTimeout()
        {
            Logging.Write("TREE: Decorator1, Action 1");
            _done = true;
            Logging.Write("Timeout we are done for now.");
            Logging.Write("Moving to the nearest Aetheryte.");
            var destination = WorldManager.AetheryteIdsForZone(WorldManager.ZoneId)
                .Select(a => a.Item2)
                .OrderBy(a => Core.Me.Distance(a))
                .FirstOrDefault();
            Navigator.MoveToPointWithin(destination, 30);
            Logging.Write("--------------------------------------");
            Logging.Write("I did {0} Fates this session.", FatesDone);
            Logging.Write("I hunted and killed {0} mobs.", MobsHunted);
            Logging.Write("I died {0} times.", Died);
            Logging.Write("--------------------------------------");
        }

        public static GameObject getFateTargets()
        {
            var _target = GameObjectManager.GameObjects.Where(unit => (unit as BattleCharacter) != null && unit.CanAttack && unit.IsTargetable && unit.IsVisible
                                                                         && (unit as BattleCharacter).FateId != 0 && !(unit as BattleCharacter).IsDead).OrderBy(unit => unit.Distance(Core.Player.Location)).Take(1);
            Logging.Write("Analyzing Fate Targets.");
            var targetArray = _target as GameObject[] ?? _target.ToArray();
            if (targetArray.Length > 0) { return targetArray[0]; } else { return null; }
        }

        public GameObject getNormalTargets()
        {
            var _target = GameObjectManager.GameObjects.Where(unit => (unit as BattleCharacter) != null && unit.CanAttack && unit.IsTargetable && unit.IsVisible
                                                                    && (unit as BattleCharacter).FateId == 0 && !(unit as BattleCharacter).IsDead).OrderBy(unit => unit.Distance(Core.Player.Location)).Take(3);
            var targetArray = _target as GameObject[] ?? _target.ToArray();
            if (targetArray.Length > 0 && targetArray[0].MaxHealth > Core.Me.CurrentHealth * 3) { return null; }
            if (targetArray.Length > 0 && targetArray[0].NpcId == 541) { return null; }
            if (targetArray.Length > 0) { return targetArray[0]; }
            return null;  // pick a random target
        }

        public List<FateData> MyFilter(List<FateData> List)
        {
            List<FateData> ReturnList = new List<FateData>();
            foreach (FateData f in List)
            {
                if (f.Icon.ToString() == "Boss" && f.Progress > 85 || fatebotInstance.BlackListedFates.Contains(f.Name))
                    Logging.Write("Skipping Fate {0}. Progress is greater than 85% or the fate is blacklisted.", f.Name);
                else
                {
                    ReturnList.Add(f);

                    Logging.Write("Adding Fate: {0}. Distance is {1}.", f.Name, Core.Me.Distance(f.Location));
                }
            }
            return ReturnList;
        }

        public async Task<bool> getFates()
        {
            List<FateData> FateCandidates = FateManager.ActiveFates.ToList();
            var FateList = MyFilter(FateCandidates);
            currentfate = FateList.OrderBy(fate => Core.Me.Distance(fate.Location)).FirstOrDefault(fate => fate.Level < _max && fate.Level > _min);
            if (currentfate == null) { return false; }

            return true;
        }

        // check all fates and return the FateData wiht the given Id or null
        public static FateData IsFateActive(uint id)
        {
            var _fate = FateManager.ActiveFates.Where(fate => fate.Id == id).Take(1);
            var fateArray = _fate as FateData[] ?? _fate.ToArray();
            if (fateArray.Length > 0)
            { return fateArray[0]; }

            return null;
        }

        public static async Task<bool> MoveTo(Vector3 location)
        {
            bool goalReached = false;

            float distance = Core.Me.Location.Distance(location);
            if (distance < 3f) { return true; }

            while (Core.Me.IsAlive && !goalReached)
            {
                Navigator.MoveTo(location);
                distance = Core.Me.Location.Distance(location);
                goalReached = distance < 3f;

                if (MovementManager.IsMoving && !Core.Me.IsMounted)
                {
                    if (ActionManager.IsSprintReady && WorldManager.InSanctuary) { ActionManager.Sprint(); }
                    else if (ActionManager.IsSprintReady && !WorldManager.InSanctuary && Core.Me.InCombat) { ActionManager.Sprint(); }
                }

                await Coroutine.Yield();
            }

            return true;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }

        private ITargetingProvider CachedProvider;

        protected override void OnStart()
        {
            _min = Convert.ToInt32(MinLevel);
            _max = Convert.ToInt32(MaxLevel);
            _timeout = Convert.ToInt32(timeout);
            currentstep = 0;
            Logging.Write("Doing fates and hunt in between.");
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
        public HashSet<uint> IgnoreNpcIds = new HashSet<uint>()
        {
            1201
        };

        private BattleCharacter[] _aggroedBattleCharacters;

        /// <summary> Gets the objects by weight. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <returns> The objects by weight. </returns>
        public List<BattleCharacter> GetObjectsByWeight()
        {
            BattleCharacter[] allUnits = GameObjectManager.GetObjectsOfType<BattleCharacter>().ToArray();

            _aggroedBattleCharacters = GameObjectManager.Attackers.ToArray();
            var inCombat = Core.Player.InCombat;
            var hostileUnits = allUnits.Where(r => IsValidUnit(inCombat, r))
                    .Select(n => new Score
                    {
                        Unit = n,
                        Weight = GetScoreForUnit(n)
                    }).ToArray();

            // Order by weight (descending).
            return hostileUnits.OrderByDescending(s => s.Weight).Select(s => s.Unit).ToList();
        }

        /// <summary> Query if 'unit' is valid unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        private bool IsValidUnit(bool incombat, BattleCharacter unit)
        {
            if (!unit.IsValid || unit.IsDead || !unit.IsVisible || unit.CurrentHealthPercent <= 0)
                return false;

            if (IgnoreNpcIds.Contains(unit.NpcId))
                return false;

            // Ignore blacklisted mobs if they're in combat with us!
            if (Blacklist.Contains(unit.ObjectId, BlacklistFlags.Combat))
                return false;

            var fategone = unit.IsFateGone;
            if (fategone)
                return false;

            //Make sure we always return true for units inside our aggro list
            if (_aggroedBattleCharacters.Contains(unit))
                return true;

            if (!unit.IsFate)
                return false;

            if (!unit.CanAttack)
                return false;

            if (Vector3.Distance(unit.Location, FateTag.Position) > 50)
                return false;

            return !incombat;
        }

        /// <summary> Gets score for a unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <param name="unit"> The unit. </param>
        /// <returns> The score for unit. </returns>

        private double GetScoreForUnit(BattleCharacter unit)
        {
            double weight = 200 - (2 * unit.Distance());

            weight += unit.MaxHealth; // asuming that bosses have huge health this should make us target a boss

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
                weight -= 100;

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