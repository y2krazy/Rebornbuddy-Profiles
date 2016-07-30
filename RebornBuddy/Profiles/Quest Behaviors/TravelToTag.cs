using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clio.XmlEngine;
using Clio.Utilities;
using System.Threading;
using System.Threading.Tasks;
using ff14bot.Behavior;
using TreeSharp;
using Buddy.Coroutines;
using ff14bot.Managers;
using Pathfinding;
using System.ComponentModel;
using ff14bot.Objects;

namespace ff14bot.NeoProfiles {
    [XmlElement("TravelToSetting")]
    class TravelToSettingTag : ProfileBehavior {
        [XmlAttribute("Setting")]
        public string Setting { get; set; }

        [XmlAttribute("Value")]
        public bool Value { get; set; }

        private bool _done = false;
        public override bool IsDone { get { return _done; } }

        protected override Composite CreateBehavior() {
            return new TreeSharp.Action(r => {
                if ("UseAethernet".Equals(Setting)) {
                    TravelToTag.World.UseAethernet = Value;
                    _done = true;
                }
            });
        }

        protected override void OnResetCachedDone() {
            _done = false;
            base.OnResetCachedDone();
        }
    }

    #region Athernet Tag
    [XmlElement("Aethernet")]
    class AethernetTag : ProfileBehavior {
        [XmlAttribute("City")]
        public string City { get; set; }

        protected override Composite CreateBehavior() {
            return new ActionRunCoroutine(r => Task());
        }

        public async Task<bool> Task() {
            if (!TravelToTag.World.Aethernets.Keys.Contains(City)) {
                if (WorldManager.ZoneId == 418 || WorldManager.ZoneId == 419) City = "Ishgard";
                if (WorldManager.ZoneId == 132 || WorldManager.ZoneId == 133) City = "Gridania";
                if (WorldManager.ZoneId == 130 || WorldManager.ZoneId == 131) City = "Uldah";
                if (WorldManager.ZoneId == 128 || WorldManager.ZoneId == 129) City = "Limsa";
            }

            if (TravelToTag.World.Aethernets.ContainsKey(City)) {
                bool UsingAetheryte = TravelToTag.World.UseAetheryte;
                bool UsingAethernet = TravelToTag.World.UseAethernet;
                TravelToTag.World.UseAethernet = false;
                TravelToTag.World.UseAetheryte = false;

                List<TravelToTag.World.Aethernet> list = TravelToTag.World.Aethernets[City];
                Log("{0}", list[0].Name);

                // go to first (should be the aetheryte)

                await TravelToTag.TravelTo(list[0].ZoneId, list[0].Location, 10f);
                await UseAethernet(Convert.ToUInt32(list[0].NpcId));
                int known = RemoteWindows.SelectString.LineCount;
                RemoteWindows.SelectString.ClickSlot(Convert.ToUInt32(RemoteWindows.SelectString.LineCount - 1));

                if (list.Count != known) {
                    foreach (int i in TravelToTag.World.AethernetsGetOrder[City]) {
                        TravelToTag.World.Aethernet a = list[i];
                        if (a.NpcId < 0) continue;
                        await TravelToTag.TravelTo(a.ZoneId, a.Location, 3f);
                        await UseAethernet(Convert.ToUInt32(a.NpcId));
                        RemoteWindows.SelectString.ClickSlot(Convert.ToUInt32(RemoteWindows.SelectString.LineCount - 1));
                    }
                } else {
                    Log("Already know all {0} aethernets in {1}.", known, City);
                }

                TravelToTag.World.UseAetheryte = UsingAetheryte;
                TravelToTag.World.UseAethernet = UsingAethernet;
            }

            _done = true;
            return true;
        }

        public static async Task<bool> UseAethernet(uint NPCId) {
            await Coroutine.Wait(Timeout.Infinite, () => !Core.Player.IsCasting);

            while (!RemoteWindows.SelectString.IsOpen) {
                GameObjectManager.GetObjectByNPCId(NPCId).Interact();
                await Coroutine.Wait(500, () => RemoteWindows.SelectString.IsOpen);
                await Coroutine.Wait(Timeout.Infinite, () => !Core.Player.IsCasting);
                while (RemoteWindows.Talk.DialogOpen) {
                    RemoteWindows.Talk.Next();
                    await Coroutine.Yield();
                }
            }

            if (RemoteWindows.SelectString.Lines().Any(r => r.Contains("Aethernet."))) {
                ff14bot.RemoteWindows.SelectString.ClickLineContains("Aethernet.");
                await Coroutine.Wait(2000, () => !RemoteWindows.SelectString.IsOpen);
                await Coroutine.Wait(Timeout.Infinite, () => RemoteWindows.SelectString.IsOpen);
            }

            return true;
        }

        private bool _done = false;
        public override bool IsDone { get { return _done; } }
    }
    #endregion

    [XmlElement("TravelTo")]
    class TravelToTag : ProfileBehavior {
        public const float DISTANCE_COST_AETHERYTE = 100f;
        public const float DISTANCE_COST_ZONELINE = 50f;
        public const int ZONE_DELAY = 1000;

        [XmlAttribute("ZoneId")]
        public uint ZoneId { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 Location { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("UseAetheryte")]
        public bool UseAetheryte { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("Debug")]
        public bool Debug { get; set; }

        [DefaultValue(5.0f)]
        [XmlAttribute("StopRadius")]
        public float StopRadius { get; set; }

        protected override Composite CreateBehavior() {
            return new ActionRunCoroutine(r => Task());
        }

        private bool _done = false;
        public override bool IsDone { get { return _done; } }

        public delegate bool Cond();
        public Cond Condition = new Cond(() => false);

        public async Task<bool> Task() {
            World.UseAetheryte = UseAetheryte;
            World.Debug = Debug;
            Area area = World.getArea(Location, ZoneId);

            if (StopRadius == 0f) {
                StopRadius = 5.0f;
            }

            Log("Travel to {0}: {1} -- Area: {2}", ZoneId, Location, area.Name);

            Entrance[] Entrances = World.ShortestPath(Location, ZoneId);
            Log("===== Path to use: =====\n-- {0}", String.Join("\n-- ", Entrances.Select(r => r.Name)));

            
            foreach (Entrance e in Entrances) {
                await e.Utilize();
            }

            while (await CommonTasks.ExecuteCoroutine(CommonBehaviors.MoveAndStop(ret => Location, StopRadius, true, null, RunStatus.Failure))) {
                if (Condition()) break;
                
                await Coroutine.Yield();
            }
            
            _done = true;

            return true;
        }

        public async static Task<bool> TravelTo(uint ZoneId, Vector3 Location, float StopRadius = 3.0f) {
            Area area = World.getArea(Location, ZoneId);

            if (StopRadius == 0f) {
                StopRadius = 5.0f;
            }

            ff14bot.Helpers.Logging.Write("Travel to {0}: {1} -- Area: {2}", ZoneId, Location, area.Name);

            Entrance[] Entrances = World.ShortestPath(Location, ZoneId);
            ff14bot.Helpers.Logging.Write("===== Path to use: =====\n-- {0}", String.Join("\n-- ", Entrances.Select(r => r.Name)));


            foreach (Entrance e in Entrances) {
                await e.Utilize();
            }

            while (await CommonTasks.ExecuteCoroutine(CommonBehaviors.MoveAndStop(ret => Location, StopRadius, true, null, RunStatus.Failure))) {
                await Coroutine.Yield();
            }
            return true;
        }

        public async static Task<bool> TravelTo(uint ZoneId, Vector3 Location, Func<bool> StopCondition) {
            Area area = World.getArea(Location, ZoneId);

            float StopRadius = 3.0f;

            ff14bot.Helpers.Logging.Write("Travel to {0}: {1} -- Area: {2}", ZoneId, Location, area.Name);

            Entrance[] Entrances = World.ShortestPath(Location, ZoneId);
            ff14bot.Helpers.Logging.Write("===== Path to use: =====\n-- {0}", String.Join("\n-- ", Entrances.Select(r => r.Name)));


            foreach (Entrance e in Entrances) {
                await e.Utilize();
            }

            while (await CommonTasks.ExecuteCoroutine(CommonBehaviors.MoveAndStop(ret => Location, StopRadius, true, null, RunStatus.Failure))) {
                if (StopCondition()) break;
                await Coroutine.Yield();
            }
            return true;
        }

        public static async Task<bool> AetheryteTo(uint AetheryteId) {
            await Coroutine.Wait(Timeout.Infinite, () => !Core.Player.IsCasting && !Core.Player.InCombat);
            while (!Core.Player.IsCasting) {
                WorldManager.TeleportById(AetheryteId);
                await Coroutine.Wait(500, () => Core.Player.IsCasting);
            }
            if (await Coroutine.Wait(5000, () => Core.Player.IsCasting)) {
                await Coroutine.Wait(5000, () => !Core.Player.IsCasting);
                await Coroutine.Wait(5000, () => CommonBehaviors.IsLoading);
                await Coroutine.Wait(Timeout.Infinite, () => !CommonBehaviors.IsLoading);
                await Coroutine.Sleep(TravelToTag.ZONE_DELAY);
            }

            return true;
        }

        public static async Task<bool> NavigateTo(Vector3 Loc, float InteractDistance = 3f) {
            uint zid = WorldManager.ZoneId;

            if (World.Debug) { ff14bot.Helpers.Logging.Write("In NavigateTo -- Navigating to {0} with InteractDistance {1}.", Loc, InteractDistance); }

            while (await CommonTasks.ExecuteCoroutine(CommonBehaviors.MoveAndStop(ret => Loc, InteractDistance, true, null, RunStatus.Failure))) {
                if (CommonBehaviors.IsLoading) {
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("End NavigateTo -- Loading Screen -- Navigating to {0} with InteractDistance {1}.", Loc, InteractDistance); }
                    return true;
                }
                if (zid != WorldManager.ZoneId) {
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("End NavigateTo -- Zone Changed -- Navigating to {0} with InteractDistance {1}.", Loc, InteractDistance); }
                    return false;
                }
                if (Core.Player.Distance(Loc) < InteractDistance) {
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("End NavigateTo -- Within range -- Navigating to {0} with InteractDistance {1}.", Loc, InteractDistance); }
                    return true;
                }
                await Coroutine.Yield();
            }

            if (World.Debug) { ff14bot.Helpers.Logging.Write("End NavigateTo -- Arrived -- Navigating to {0} with InteractDistance {1}.", Loc, InteractDistance); }
            return true;
        }

        public static float PI = Convert.ToSingle(Math.PI);
        public static async Task<bool> NoMeshMoveTo(Vector3 Location, float range = 3.0f) {
            bool moving = false;

            while (Core.Player.Location.Distance(Location) > range) {
                float z = Location.Z - Core.Player.Location.Z;
                float x = Location.X - Core.Player.Location.X;
                // The Z axis goes from top to bottom for whatever reason, so for simplicity
                // take its inverse
                float angle = Convert.ToSingle(Math.Atan2(x, -z));

                MovementManager.SetFacing(PI - angle);
                if (!moving) {
                    MovementManager.MoveForwardStart();
                    moving = true;
                }

                await Coroutine.Yield();
            }

            MovementManager.MoveForwardStop();
            return true;
        }

        public static async Task<bool> UsePorter(uint NPCId, params uint[] DialogOptions) {
            Chocobo.BlockSummon = true;
            if (Chocobo.Summoned) {
                await Chocobo.DismissChocobo();
            }

            List<uint> p = new List<uint>();
            p.Add(0);
            p.AddRange(DialogOptions);

            bool result = await YesnoNPC(NPCId, p.ToArray());

            await Coroutine.Wait(Timeout.Infinite, () => Core.Me.IsMounted);
            await Coroutine.Wait(Timeout.Infinite, () => !Core.Me.IsMounted);

            Chocobo.BlockSummon = false;

            return result;
        }

        public static async Task<bool> YesnoNPC(uint NPCId, params uint[] DialogOptions) {
            DialogOptions = DialogOptions ?? new uint[0];
            int i = 0;

            GameObjectManager.GetObjectByNPCId(NPCId).Interact();
            await Coroutine.Wait(Timeout.Infinite, () => !Core.Me.IsMounted);
            while (await (Coroutine.Wait(Timeout.Infinite, () => RemoteWindows.Talk.DialogOpen || RemoteWindows.SelectIconString.IsOpen || RemoteWindows.SelectString.IsOpen || RemoteWindows.SelectYesno.IsOpen || CommonBehaviors.IsLoading))) {
                if (RemoteWindows.Talk.DialogOpen) {
                    RemoteWindows.Talk.Next();
                    await Coroutine.Yield();
                } else if (RemoteWindows.SelectYesno.IsOpen) {
                    RemoteWindows.SelectYesno.ClickYes();
                    await Coroutine.Wait(Timeout.Infinite, () => !RemoteWindows.SelectYesno.IsOpen);
                } else if (RemoteWindows.SelectIconString.IsOpen) {
                    RemoteWindows.SelectIconString.ClickSlot(DialogOptions[i++]);
                    await Coroutine.Wait(Timeout.Infinite, () => ! RemoteWindows.SelectIconString.IsOpen);
                } else if (RemoteWindows.SelectString.IsOpen) {
                    RemoteWindows.SelectString.ClickSlot(DialogOptions[i++]);
                    await Coroutine.Wait(Timeout.Infinite, () => !RemoteWindows.SelectString.IsOpen);
                } else if (CommonBehaviors.IsLoading) {
                    await Coroutine.Wait(Timeout.Infinite, () => !CommonBehaviors.IsLoading);
                    return true;
                }
            }

            return true;
        }

        public static async Task<bool> Aethernet(uint NPCId, string Target) {
            while (!RemoteWindows.SelectString.IsOpen) {
                GameObjectManager.GetObjectByNPCId(NPCId).Interact();
                await Coroutine.Wait(500, () => RemoteWindows.SelectString.IsOpen);
            }
            
            if (RemoteWindows.SelectString.Lines().Any(r => r.Contains("Aethernet."))) {
                ff14bot.RemoteWindows.SelectString.ClickLineContains("Aethernet.");
                await Coroutine.Wait(2000, () => !RemoteWindows.SelectString.IsOpen);
                await Coroutine.Wait(Timeout.Infinite, () => RemoteWindows.SelectString.IsOpen);
            }

            ff14bot.RemoteWindows.SelectString.ClickLineContains(Target);
            await Coroutine.Wait(5000, () => CommonBehaviors.IsLoading);
            await Coroutine.Wait(Timeout.Infinite, () => ! CommonBehaviors.IsLoading);

            return true;
        }

        public class World {
            public static Dictionary<string, Area> Areas = null;
            public static Dictionary<Entrance, Area> Entrances = new Dictionary<Entrance, Area>();
            public static bool Debug = false;
            public static bool UseAethernet = true;
            public static bool UseAetheryte = true;

            public static bool CanAetheryte(uint AetheryteId) {
                if (!UseAetheryte) {
                    return false;
                }
                return WorldManager.HasAetheryteId(AetheryteId);
            }
            // todo: check for airship pass
            public static bool HasAirshipPass() {
                //return true;
                // airship areas aren't meshed(?), don't use them.
                return false;
            }

            #region Common Locations
            public static class CommonNPC {
                public static Tuple<uint, Vector3> GridaniaAirshipTicketer = new Tuple<uint, Vector3>(1000106, new Vector3(29.00732f, -19f, 105.4856f));
                public static Tuple<uint, Vector3> UldahAirshipTicketer = new Tuple<uint, Vector3>(1004433, new Vector3(-23.60571f, 83.19999f, -2.304138f));
                public static Tuple<uint, Vector3> LimsaAirshipTicketer = new Tuple<uint, Vector3>(1002695, new Vector3(-25.92511f, 91.99995f, -3.677429f));
                public static Tuple<uint, Vector3> LimsaUpperLiftOperator = new Tuple<uint, Vector3>(1003597, new Vector3(8.194031f, 39.99998f, 17.74622f));
                public static Tuple<uint, Vector3> LimsaAirshipLiftOperator = new Tuple<uint, Vector3>(1003583, new Vector3(-7.248047f, 91.49999f, -16.12885f));
                public static Tuple<uint, Vector3> LimsaLowerLiftOperator = new Tuple<uint, Vector3>(1003611, new Vector3(9.781006f, 20.99925f, 15.09113f));
                public static Tuple<uint, Vector3> LimsaLowerFerryman = new Tuple<uint, Vector3>(1000868, new Vector3(-192.0043f, 0.9999907f, 211.6884f));
                public static Tuple<uint, Vector3> AleportFerryman = new Tuple<uint, Vector3>(1003584, new Vector3(317.4333f, -36.325f, 352.8649f));
                public static Tuple<uint, Vector3> UmbraIslesFerryman = new Tuple<uint, Vector3>(1005239, new Vector3(-289.9062f, -41.2455f, 406.5156f));
                public static Tuple<uint, Vector3> CandlekeepFerryman = new Tuple<uint, Vector3>(1002269, new Vector3(-43.96124f, 9.022386f, 848.1117f));
                public static Tuple<uint, Vector3> CostadelSolFerryman = new Tuple<uint, Vector3>(1003585, new Vector3(607.8126f, 11.62379f, 391.8668f));
            }
            public static class CommonLocation {
                public static Vector3 GridaniaAirshipArrival = new Vector3(-23.60571f, 83.19999f, -2.304138f);
                public static Vector3 UldahAirshipArrival = new Vector3(-16.95973f, 82.99959f, 3.588338f);
                public static Vector3 LimsaAirshipArrival = new Vector3(-12.33966f, 91.49995f, -7.363967f);
                public static Vector3 LimsaLowerLiftDestination = new Vector3(10.76334f, 20.99923f, 13.26101f);
                public static Vector3 LimsaUpperLiftDestination = new Vector3(9.287986f, 40.00025f, 15.32217f);
                public static Vector3 LimsaAirshipLIftDestination = new Vector3(-10.18961f, 91.49963f, -17.19572f);
                public static Vector3 AleportFerryArrival = new Vector3(320.396f, -40.425f, 371.4217f);
                public static Vector3 UmbraIslesArraival = new Vector3(-290.1408f, -40.98465f, 413.8002f);
                public static Vector3 LimsaLowerFerryArrival = new Vector3(-190.2045f, 0.9999907f, 208.4925f);
                public static Vector3 CandlekeepFerryArrival = new Vector3(-29.56782f, 8.921356f, 855.014f);
                public static Vector3 CostadelSolFerryArrival = new Vector3(-29.56782f, 8.921356f, 855.014f);
            }
            #endregion

            #region Aethernets
            public class Aethernet {
                public string Name;
                public int NpcId;
                public uint ZoneId;
                public Vector3 Location;
                public Aethernet(string n, int i, uint z, Vector3 l) {
                    Name = n;
                    NpcId = i;
                    ZoneId = z;
                    Location = l;
                }
            }

            public static Dictionary<String, List<int>> AethernetsGetOrder = new Dictionary<string, List<int>>() {
                {"Ishgard", new List<int>() {
                    3, 1, 2, 5, 6, 7, 4, 8
                }}
            };

            public static Dictionary<string, List<Aethernet>> Aethernets = new Dictionary<string, List<Aethernet>>() {
                {"Limsa", new List<Aethernet>() {
                    new Aethernet("Limsa Lominsa Aetheryte Plaza.", 8, 129, new Vector3(-84.03149f, 20.76746f, 0.01519775f)),
                    new Aethernet("The Aftcastle.", 41, 128, new Vector3(16.06769f, 40.78735f, 68.80286f)),
                    new Aethernet("Culinarians' Guild.", 42, 128, new Vector3(-56.50421f, 44.47998f, -131.4565f)),
                    new Aethernet("Arcanists' Guild", 43, 129, new Vector3(-335.1645f, 12.6192f, 56.38196f)),
                    new Aethernet("Fishermen's Guild.", 44, 129, new Vector3(-179.4003f, 4.806519f, 182.9709f)),
                    new Aethernet("Marauders' Guild.", 48, 128, new Vector3(-5.172852f, 44.63257f, -218.0667f)),
                    new Aethernet("Hawkers' Alley.", 49, 129, new Vector3(-213.6111f, 16.73914f, 51.80432f)),
                    new Aethernet("Zephyr Gate (Middle La Noscea).", -1, 134, new Vector3(-3.214426f, 41.84665f, 146.1318f)),
                    new Aethernet("Tempest Gate (Lower La Noscea).", -1, 135, new Vector3(-25.82222f, 70.64693f, 102.9528f))
                }},
                {"Uldah", new List<Aethernet>() {
                    new Aethernet("Ul'dah Aetheryte Plaza.", 9, 130, new Vector3(-144.5182f, -1.358093f, -169.6651f)),
                    new Aethernet("Adventurers' Guild.", 33, 130, new Vector3(64.22522f, 4.53186f, -115.3124f)),
                    new Aethernet("Thaumaturges' Guild.", 34, 130, new Vector3(-154.8333f, 14.63336f, 73.07532f)),
                    new Aethernet("Gladiators' Guild.", 35, 131, new Vector3(-53.84918f, 10.69653f, 12.22241f)),
                    new Aethernet("Miners' Guild.", 36, 131, new Vector3(33.49353f, 13.22949f, 113.2067f)),
                    new Aethernet("Alchemists' Guild.", 37, 131, new Vector3(-98.25293f, 42.34375f, 88.45642f)),
                    new Aethernet("Weavers' Guild.", 47, 131, new Vector3(89.64673f, 12.92438f, 58.27417f)),
                    new Aethernet("Goldsmiths' Guild.", 50, 131, new Vector3(-19.33325f, 14.60284f, 72.03784f)),
                    new Aethernet("The Chamber of Rule.", 51, 131, new Vector3(6.637634f, 30.65527f, -24.82648f)),
                    new Aethernet("Gate of the Sultana (Western Thanalan).", -1, 140, new Vector3(447.8187f, 93.93484f, 166.3656f)),
                    new Aethernet("Gate of Nald (Central Thanalan).", -1, 141, new Vector3(-124.079f, 15.83116f, 312.2194f)),
                    new Aethernet("Gate of Thal (Central Thanalan).", -1, 141, new Vector3(37.70456f, 15.74899f, 576.9001f))
                }},
                {"Gridania", new List<Aethernet>() {
                    new Aethernet("Gridania Aetheryte Plaza.", 2, 132, new Vector3(32.9137f, 2.670288f, 30.0144f)),
                    new Aethernet("Archers' Guild.", 25, 132, new Vector3(166.5828f, -1.724304f, 86.13721f)),
                    new Aethernet("Leatherworkers' Guild.", 26, 133, new Vector3(101.274f, 9.018005f, -111.3146f)),
                    new Aethernet("Lancers' Guild.", 27, 133, new Vector3(121.2329f, 12.64966f, -229.6331f)),
                    new Aethernet("Conjurers' Guild.", 28, 133, new Vector3(-145.1591f, 4.959106f, -11.76477f)),
                    new Aethernet("Botanists' Guild.", 29, 133, new Vector3(-311.0857f, 7.94989f, -177.0505f)),
                    new Aethernet("Mih Khetto's Amphitheatre.", 30, 133, new Vector3(-73.92999f, 7.980469f, -140.1542f)),
                    new Aethernet("Blue Badger Gate (Central Shroud).", -1, 148, new Vector3(129.0498f, 24.85179f, -302.7715f)),
                    new Aethernet("Yellow Serpent Gate (North Shroud).", -1, 154, new Vector3(448.3075f, -1.762989f, 198.0476f))
                }},
                {"Ishgard", new List<Aethernet>() {
                    new Aethernet("Ishgard Aetheryte Plaza.", 70, 418, new Vector3(-63.98114f, 11.1543f, 43.9917f)),
                    new Aethernet("The Forgotten Knight.", 80, 418, new Vector3(45.79224f, 24.55164f, 0.9917603f)),
                    new Aethernet("Skysteel Manufactory.", 81, 418, new Vector3(-111.4366f, 16.12872f, -27.05432f)),
                    new Aethernet("The Brume.", 82, 418, new Vector3(49.42395f, -11.15442f, 66.69714f)),
                    new Aethernet("Athenaeum Astrologicum.", 83, 419, new Vector3(133.379f, -8.86554f, -64.77466f)),
                    new Aethernet("The Jeweled Crozier.", 84, 419, new Vector3(-134.6914f, -11.79523f, -15.39642f)),
                    new Aethernet("Saint Reymanaud Cathedral.", 85, 419, new Vector3(-77.95837f, 10.60498f, -126.5432f)),
                    new Aethernet("The Tribunal.", 86, 419, new Vector3(78.01941f, 11.00171f, -126.5126f)),
                    new Aethernet("The Last Vigil.", 87, 419, new Vector3(0.01519775f, 16.52545f, -32.51703f)),
                    new Aethernet("The Gates of Judgement (Coerthas Central Highlands).", -1, 155, new Vector3(-159.1856f, 304.1538f, -322.0619f))
                }}
            };

            public static void LimsaAethernet() {
                List<Aethernet> Crystals = new List<Aethernet>();
                Crystals.Add(new Aethernet("Limsa Lominsa Aetheryte Plaza.", 8, 129, new Vector3(-84.03149f, 20.76746f, 0.01519775f)));
                Crystals.Add(new Aethernet("The Aftcastle.", 41, 128, new Vector3(16.06769f, 40.78735f, 68.80286f)));
                Crystals.Add(new Aethernet("Culinarians' Guild.", 42, 128, new Vector3(-56.50421f, 44.47998f, -131.4565f)));
                Crystals.Add(new Aethernet("Arcanists' Guild", 43, 129, new Vector3(-335.1645f, 12.6192f, 56.38196f)));
                Crystals.Add(new Aethernet("Fishermen's Guild.", 44, 129, new Vector3(-179.4003f, 4.806519f, 182.9709f)));
                Crystals.Add(new Aethernet("Marauders' Guild.", 48, 128, new Vector3(-5.172852f, 44.63257f, -218.0667f)));
                Crystals.Add(new Aethernet("Hawkers' Alley.", 49, 129, new Vector3(-213.6111f, 16.73914f, 51.80432f)));
                Crystals.Add(new Aethernet("Zephyr Gate (Middle La Noscea).", -1, 134, new Vector3(-3.214426f, 41.84665f, 146.1318f)));
                Crystals.Add(new Aethernet("Tempest Gate (Lower La Noscea).", -1, 135, new Vector3(-25.82222f, 70.64693f, 102.9528f)));
                
                foreach (Aethernet c in Crystals) {
                    foreach (Aethernet d in Crystals.Where(r => r.Name != c.Name && r.NpcId != -1)) {
                        string name = string.Format("Aethernet: {0} => {1}", d.Name, c.Name);
                        Entrance e = new Entrance(name, c.Location, d.Location, async () => { await Aethernet(Convert.ToUInt32(d.NpcId), c.Name); }, () => { return UseAethernet; });
                        if (d.NpcId == 8) e.InteractDistance = 9f;
                        getArea(c.Location, c.ZoneId).Entrances[e] = getArea(d.Location, d.ZoneId);
                    }
                }
            }

            public static void UldahAethernet() {
                List<Aethernet> Crystals = new List<Aethernet>();
                Crystals.Add(new Aethernet("Ul'dah Aetheryte Plaza.", 9, 130, new Vector3(-144.5182f, -1.358093f, -169.6651f)));
                Crystals.Add(new Aethernet("Adventurers' Guild.", 33, 130, new Vector3(64.22522f, 4.53186f, -115.3124f)));
                Crystals.Add(new Aethernet("Thaumaturges' Guild.", 34, 130, new Vector3(-154.8333f, 14.63336f, 73.07532f)));
                Crystals.Add(new Aethernet("Gladiators' Guild.", 35, 131, new Vector3(-53.84918f, 10.69653f, 12.22241f)));
                Crystals.Add(new Aethernet("Miners' Guild.", 36, 131, new Vector3(33.49353f, 13.22949f, 113.2067f)));
                Crystals.Add(new Aethernet("Alchemists' Guild.", 37, 131, new Vector3(-98.25293f, 42.34375f, 88.45642f)));
                Crystals.Add(new Aethernet("Weavers' Guild.", 47, 131, new Vector3(89.64673f, 12.92438f, 58.27417f)));
                Crystals.Add(new Aethernet("Goldsmiths' Guild.", 50, 131, new Vector3(-19.33325f, 14.60284f, 72.03784f)));
                Crystals.Add(new Aethernet("The Chamber of Rule.", 51, 131, new Vector3(6.637634f, 30.65527f, -24.82648f)));
                Crystals.Add(new Aethernet("Gate of the Sultana (Western Thanalan).", -1, 140, new Vector3(447.8187f, 93.93484f, 166.3656f)));
                Crystals.Add(new Aethernet("Gate of Nald (Central Thanalan).", -1, 141, new Vector3(-124.079f, 15.83116f, 312.2194f)));
                Crystals.Add(new Aethernet("Gate of Thal (Central Thanalan).", -1, 141, new Vector3(37.70456f, 15.74899f, 576.9001f)));

                foreach (Aethernet c in Crystals) {
                    foreach (Aethernet d in Crystals.Where(r => r.Name != c.Name && r.NpcId != -1)) {
                        string name = string.Format("Aethernet: {0} => {1}", d.Name, c.Name);
                        Entrance e = new Entrance(name, c.Location, d.Location, async () => { await Aethernet(Convert.ToUInt32(d.NpcId), c.Name); }, () => { return UseAethernet; });
                        if (d.NpcId == 9) e.InteractDistance = 10f;
                        getArea(c.Location, c.ZoneId).Entrances[e] = getArea(d.Location, d.ZoneId);
                    }
                }
            }

            public static void GridaniaAethernet() {
                List<Aethernet> Crystals = new List<Aethernet>();
                Crystals.Add(new Aethernet("Gridania Aetheryte Plaza.", 2, 132, new Vector3(32.9137f, 2.670288f, 30.0144f)));
                Crystals.Add(new Aethernet("Archers' Guild.", 25, 132, new Vector3(166.5828f, -1.724304f, 86.13721f)));
                Crystals.Add(new Aethernet("Leatherworkers' Guild.", 26, 133, new Vector3(101.274f, 9.018005f, -111.3146f)));
                Crystals.Add(new Aethernet("Lancers' Guild.", 27, 133, new Vector3(121.2329f, 12.64966f, -229.6331f)));
                Crystals.Add(new Aethernet("Conjurers' Guild.", 28, 133, new Vector3(-145.1591f, 4.959106f, -11.76477f)));
                Crystals.Add(new Aethernet("Botanists' Guild.", 29, 133, new Vector3(-311.0857f, 7.94989f, -177.0505f)));
                Crystals.Add(new Aethernet("Mih Khetto's Amphitheatre.", 30, 133, new Vector3(-73.92999f, 7.980469f, -140.1542f)));
                Crystals.Add(new Aethernet("Blue Badger Gate (Central Shroud).", -1, 148, new Vector3(129.0498f, 24.85179f, -302.7715f)));
                Crystals.Add(new Aethernet("Yellow Serpent Gate (North Shroud).", -1, 154, new Vector3(448.3075f, -1.762989f, 198.0476f)));


                foreach (Aethernet c in Crystals) {
                    foreach (Aethernet d in Crystals.Where(r => r.Name != c.Name && r.NpcId != -1)) {
                        string name = string.Format("Aethernet: {0} => {1}", d.Name, c.Name);
                        Entrance e = new Entrance(name, c.Location, d.Location, async () => { await Aethernet(Convert.ToUInt32(d.NpcId), c.Name); }, () => { return UseAethernet; });
                        if (d.NpcId == 2) e.InteractDistance = 9f;
                        getArea(c.Location, c.ZoneId).Entrances[e] = getArea(d.Location, d.ZoneId);
                    }
                }
            }

            public static void IshgardAethernet() {
                List<Aethernet> Crystals = new List<Aethernet>();
                Crystals.Add(new Aethernet("Ishgard Aetheryte Plaza.", 70, 418, new Vector3(-63.98114f, 11.1543f, 43.9917f)));
                Crystals.Add(new Aethernet("The Forgotten Knight.", 80, 418, new Vector3(45.79224f, 24.55164f, 0.9917603f)));
                Crystals.Add(new Aethernet("Skysteel Manufactory.", 81, 418, new Vector3(-111.4366f, 16.12872f, -27.05432f)));
                Crystals.Add(new Aethernet("The Brume.", 82, 418, new Vector3(49.42395f, -11.15442f, 66.69714f)));
                Crystals.Add(new Aethernet("Athenaeum Astrologicum.", 83, 419, new Vector3(133.379f, -8.86554f, -64.77466f)));
                Crystals.Add(new Aethernet("The Jeweled Crozier.", 84, 419, new Vector3(-134.6914f, -11.79523f, -15.39642f)));
                Crystals.Add(new Aethernet("Saint Reymanaud Cathedral.", 85, 419, new Vector3(-77.95837f, 10.60498f, -126.5432f)));
                Crystals.Add(new Aethernet("The Tribunal.", 86, 419, new Vector3(78.01941f, 11.00171f, -126.5126f)));
                Crystals.Add(new Aethernet("The Last Vigil.", 87, 419, new Vector3(0.01519775f, 16.52545f, -32.51703f)));
                Crystals.Add(new Aethernet("The Gates of Judgement (Coerthas Central Highlands).", -1, 155, new Vector3(-159.1856f, 304.1538f, -322.0619f)));

                foreach (Aethernet c in Crystals) {
                    foreach (Aethernet d in Crystals.Where(r => r.Name != c.Name && r.NpcId != -1)) {
                        string name = string.Format("Aethernet: {0} => {1}", d.Name, c.Name);
                        Entrance e = new Entrance(name, c.Location, d.Location, async () => { await Aethernet(Convert.ToUInt32(d.NpcId), c.Name); }, () => { return UseAethernet; });
                        if (d.NpcId == 70) e.InteractDistance = 9f;
                        getArea(c.Location, c.ZoneId).Entrances[e] = getArea(d.Location, d.ZoneId);
                    }
                }
            }
            #endregion

            #region Availability
            public static bool IsTheChurningMistsAvailable {
                get {
                    return ConditionParser.IsQuestCompleted(67153) || ConditionParser.GetQuestStep(67153) == 255;
                }
            }
            public static bool IsCoerthasWesternHighlandsAvailable {
                get {
                    return ConditionParser.IsQuestCompleted(67119) || ConditionParser.GetQuestStep(67119) >= 1;
                }
            }
            public static bool CanFlyCoerthasWesternHighlands {
                get {
                    return false;
                }
            }
            #endregion

            public static void Init() {
                Areas = new Dictionary<String, Area>();

                #region Areas
                Add(new Area("Unknown Area", 0));

                Add(new Area("130", 130));     // Ul'dah - Steps of Nald
                Add(new Area("130a", 130));    // Ul'dah - Steps of Nald (Airship Landing)
                Add(new Area("131", 131));     // Ul'dah - Steps of Thal
                Add(new Area("140", 140));     // Western Thanalan
                Add(new Area("212", 212));     // The Waking Sands
                Add(new Area("212a", 212));     // The Waking Sands - Solar
                Add(new Area("141", 141));     // Central Thanalan
                Add(new Area("145", 145));     // Eastern Thanalan
                Add(new Area("146", 146));     // Southern Thanalan
                Add(new Area("147", 147));     // Northern Thanalan

                Add(new Area("128", 128));     // Limsa Lominsa Upper Decks
                Add(new Area("128a", 128));    // Limsa Lominsa Upper Decks (Airship Landing)
                Add(new Area("129", 129));     // Lower Lominsa Lower Decks
                Add(new Area("134", 134));     // Middle La Noscea
                Add(new Area("135", 135));     // Lower La Noscea
                Add(new Area("137a", 137));    // Eastern La Noscea (West)
                Add(new Area("137b", 137));    // Eastern La Noscea (East)
                Add(new Area("138", 138));     // Western La Noscea
                Add(new Area("138b", 138));    // Western La Noscea - Isles of Umbra
                Add(new Area("139a", 139));    // Upper La Noscea (West)
                Add(new Area("139b", 139));    // Upper La Nosea (East)
                Add(new Area("180", 180));     // Outer La Noscea

                Add(new Area("132", 132));     // New Gridania
                Add(new Area("133", 133));     // Old Gridania
                Add(new Area("148", 148));     // Central Shroud
                Add(new Area("152", 152));     // East Shroud
                Add(new Area("153", 153));     // South Shroud
                Add(new Area("153b", 153));    // South Shroud (Raya-O-Senna)
                Add(new Area("154", 154));     // North Shroud

                Add(new Area("156", 156));     // Mor Dhona
                Add(new Area("351", 351));     // Rising Stones
                Add(new Area("351a", 351));    // Rising Stones Solar

                Add(new Area("155", 155));     // Coerthas Central Highlands
                Add(new Area("397", 387));     // Coerthas Western Highlands
                Add(new Area("395", 395));     // Intercessory
                Add(new Area("418", 418));     // Foundation

                Add(new Area("398", 398));     // Dravanian Forelands
                Add(new Area("399", 399));     // Dravanian Hinterlands

                Add(new Area("419", 419));     // The Pillars
                Add(new Area("433", 433));     // Fortemps Manor
                Add(new Area("439", 439));     // Chocobo Proving Grounds

                Add(new Area("400", 400));     // The Churning Mists
                Add(new Area("401a", 401));     // The Sea of Clouds (South)
                Add(new Area("401b", 401));     // The Sea of Clouds (North)
                Add(new Area("402", 402));     // Azys Lla
                #endregion

                #region Ishgard
                // Azys Lla
                Areas["402"].Entrances[new Entrance("Helix Aetheryte", new Vector3(-722.8046f, -182.2996f, -593.4081f), new Vector3(), async () => { await AetheryteTo(74); }, () => CanAetheryte(74))] = null;

                // The Churning Mists
                Areas["400"].Entrances[new Entrance("Moghome Aetheryte", new Vector3(259.205f, -37.70508f, 596.8566f), new Vector3(), async () => { await AetheryteTo(78); }, () => CanAetheryte(78))] = null;
                Areas["400"].Entrances[new Entrance("Zenith Aetheryte", new Vector3(-584.9546f, 52.84192f, 313.4354f), new Vector3(), async () => { await AetheryteTo(79); }, () => CanAetheryte(79))] = null;
                Areas["400"].Entrances[new Entrance("Dravanian Forelands => Churning Mists", new Vector3(173.706f, -64.49505f, 697.2959f), new Vector3(-693.843f, 5.20875f, -840.9705f), async () => { await YesnoNPC(1011946); }, () => IsTheChurningMistsAvailable)] = Areas["398"];
                // TODO: Remove when bridge gets meshed
                // Bridge north of Zenith meshed correctly, so make bot think of it as a shortcut
                Areas["400"].Entrances[new Entrance("Zenith Bridge: West to East", new Vector3(-547.0854f, 51.15618f, 186.4203f), new Vector3(-604.8343f, 69.9454f, 204.7363f), async () => { await NoMeshMoveTo(new Vector3(-547.0854f, 51.15618f, 186.4203f)); })] = Areas["400"];
                Areas["400"].Entrances[new Entrance("Zenith Bridge: East to West", new Vector3(-604.9f, 70f, 210.0123f), new Vector3(-548.5076f, 52.27778f, 191.8792f), async () => { await NoMeshMoveTo(new Vector3(-604.9f, 70f, 210.0123f)); })] = Areas["400"];


                // The Dravanian Hinterlands
                // TODO: when does this become usable?
                Areas["399"].Entrances[new Entrance("Coerthas Western Highlands => Dravanian Hinterlands", new Vector3(755.2931f, 278.0881f, 481.727f), new Vector3(-157.0327f, 221.9644f, 745.6353f))] = Areas["397"];

                // The Dravanian Forelands
                Areas["398"].Entrances[new Entrance("Tailfeather Aetheryte", new Vector3(532.6771f, -48.72211f, 30.16699f), new Vector3(), async () => { await AetheryteTo(76); }, () => CanAetheryte(76))] = null;
                Areas["398"].Entrances[new Entrance("Anyx Trine Aetheryte", new Vector3(-304.1276f, -16.70868f, 32.05908f), new Vector3(), async () => { await AetheryteTo(77); }, () => CanAetheryte(77))] = null;
                // TODO: determine when this becomes available, after Quest: [Purple Flame, Purple Flame] or during [Where the Chocobos Roam]
                Areas["398"].Entrances[new Entrance("Coerthas Western Highlands => Dravanian Forelands", new Vector3(826.4198f, -13.27414f, 327.6102f), new Vector3(-853.8564f, 117.683f, -659.189f))] = Areas["397"];
                Areas["398"].Entrances[new Entrance("Churning Mists => Dravanian Forelands", new Vector3(-684.4588f, 4.907196f, -823.263f), new Vector3(201.5868f, -68.68091f, 709.3461f), async () => { await YesnoNPC(2005371); }, () => IsTheChurningMistsAvailable)] = Areas["400"];

                // Sea of clouds
                Areas["401a"].Entrances[new Entrance("Camp Cloudtop Aetheryte", new Vector3(-615.7473f, -118.3643f, 546.5934f), new Vector3(), async () => { await AetheryteTo(72); }, () => CanAetheryte(72))] = null;
                Areas["401b"].Entrances[new Entrance("Ok' Zundu Aetheryte", new Vector3(-613.1533f, -49.48505f, -415.0302f), new Vector3(), async () => { await AetheryteTo(73); }, () => CanAetheryte(73))] = null;
                Areas["401a"].Entrances[new Entrance("Airship: The Pillars => Camp Cloudtop", new Vector3(-738.6288f, -105.0688f, 462.5234f), new Vector3(149.7367f, -12.26397f, -7.858459f), async () => { await YesnoNPC(1011211); })] = Areas["419"];
                Areas["419"].Entrances[new Entrance("Airship: Camp Cloudtop => The Pillars", new Vector3(160.9854f, -12.5463f, -17.85655f), new Vector3(-734.9813f, -105.0583f, 459.3728f), async () => { await YesnoNPC(1011949); })] = Areas["401a"];
                Areas["401b"].Entrances[new Entrance("Airship: The Pillars => The Blue Window", new Vector3(-810.2095f, -57.84716f, 165.293f), new Vector3(147.3258f, -12.63491f, -12.40564f), async () => { await YesnoNPC(1011212); })] = Areas["419"];
                Areas["419"].Entrances[new Entrance("Airship: The Blue Window => The Pillars", new Vector3(160.5264f, -12.63491f, -15.12555f), new Vector3(-812.0089f, -57.8775f, 162.7679f), async () => { await YesnoNPC(2005370); })] = Areas["401b"];

                // Coerthas Western Highlands
                Areas["397"].Entrances[new Entrance("Falcon's Nest Aetheryte", new Vector3(474.8759f, 217.9446f, 708.5221f), new Vector3(), async () => { await AetheryteTo(71); }, () => CanAetheryte(71))] = null;
                // TODO: determine when this becomes available, after Quest: [Purple Flame, Purple Flame] or during [Where the Chocobos Roam]
                Areas["397"].Entrances[new Entrance("Dravanian Forelands => Coerthas Western Highlands", new Vector3(-822.7368f, 118.5188f, -639.7134f), new Vector3(874.5078f, -2.72726f, 356.1995f))] = Areas["398"];
                Areas["397"].Entrances[new Entrance("Dravanian Hinterlands => Coerthas Western Highlands", new Vector3(-153.8628f, 221.2611f, 691.557f), new Vector3(767.3764f, 291.2085f, 508.4365f))] = Areas["399"];
                Areas["397"].Entrances[new Entrance("Porter: Foundation => Coerthas Western Highlands (5 gil)", new Vector3(490.1016f, 217.9514f, 758.1215f), new Vector3(-163.4394f, 2.15106f, -5.508545f), async () => { await UsePorter(1011195, 0); }, () => IsCoerthasWesternHighlandsAvailable)] = Areas["418"];
                // TODO: the sea of clouds entrance

                // TODO: Remove when bridge gets meshed
                // Bridge in CWH isn't meshed correctly, so make bot think of it as a shortcut
                Areas["397"].Entrances[new Entrance("Coerthas Western Highlands Bridge: South to North", new Vector3(419.3284f, 160.1759f, -101.0591f), new Vector3(313.8732f, 158.5474f, 164.2541f), async () => { await NoMeshMoveTo(new Vector3(419.3284f, 160.1759f, -101.0591f)); })] = Areas["397"];
                Areas["397"].Entrances[new Entrance("Coerthas Western Highlands Bridge: North to South", new Vector3(313.8732f, 158.5474f, 164.2541f), new Vector3(415.7484f, 159.7469f, -87.30489f), async () => { await NoMeshMoveTo(new Vector3(313.8732f, 158.5474f, 164.2541f)); })] = Areas["397"];

                // Ishgard
                Areas["418"].Entrances[new Entrance("Porter: Coerthas Western Highlands => Foundation", new Vector3(-160.0552f, 2.03334f, -5.680345f), new Vector3(483.1157f, 217.9514f, 751.0642f), async () => { await YesnoNPC(1011226, 0, 0); })] = Areas["397"];

                Areas["419"].Entrances[new Entrance("Foundation => The Pillars (West)", new Vector3(-290.5434f, -20.1422f, -76.7705f), new Vector3(-153.0722f, 32.5022f, -145.5933f))] = Areas["418"];
                Areas["418"].Entrances[new Entrance("The Pillars => Foundation (West)", new Vector3(-153.239f, 28.12983f, -132.7884f), new Vector3(-317.8078f, -26.4271f, -61.85497f))] = Areas["419"];
                Areas["418"].Entrances[new Entrance("The Pillars => Foundation (W.Central)", new Vector3(-63.99956f, 18.54314f, -89.3743f), new Vector3(-14.0554f, -14.04756f, -63.08229f))] = Areas["419"];
                Areas["419"].Entrances[new Entrance("Foundation => The Pillars (W.Central)", new Vector3(-35.43108f, -9.301202f, -71.42577f), new Vector3(-55.13786f, 22.20579f, -100.184f))] = Areas["418"];
                Areas["418"].Entrances[new Entrance("The Pillars => Foundation (E.Central)", new Vector3(55.42097f, 27.5757f, -72.08213f), new Vector3(14.29555f, -14.00075f, -63.12065f))] = Areas["419"];
                Areas["419"].Entrances[new Entrance("Foundation => The Pillars (E.Central)", new Vector3(41.39346f, -8.216792f, -69.45927f), new Vector3(52.47797f, 31.42938f, -84.88495f))] = Areas["418"];
                Areas["418"].Entrances[new Entrance("The Pillars => Foundation (East)", new Vector3(148.8234f, -20f, 63.40912f), new Vector3(262.3371f, -13.73498f, -104.0928f))] = Areas["419"];
                Areas["419"].Entrances[new Entrance("Foundation => The Pillars (East)", new Vector3(244.661f, -13.69776f, -104.1193f), new Vector3(159.3606f, -19.47741f, 51.71542f))] = Areas["418"];

                Areas["418"].Entrances[new Entrance("Foundation Aetheryte", new Vector3(-63.98114f, 11.1543f, 43.9917f), new Vector3(), async () => { await AetheryteTo(70); }, () => CanAetheryte(70))] = null;
                Areas["418"].Entrances[new Entrance("Coerthas Central Highlands => Foundation", new Vector3(-2.600469f, -2.558855f, 144.2003f), new Vector3(-163.8972f, 304.1538f, -333.0587f), async () => { await YesnoNPC(1012149); })] = Areas["155"];
                Areas["418"].Entrances[new Entrance("Chocobo Proving Grounds => Foundation", new Vector3(90.17953f, 23.97913f, -35.54169f), new Vector3(30.77734f, 7.644714f, -21.43903f), async () => { await YesnoNPC(2005335); })] = Areas["439"];
                Areas["439"].Entrances[new Entrance("Foundation => Chocobo Proving Grounds", new Vector3(27.15762f, 6.514109f, -19.24278f), new Vector3(94.59058f, 24.06099f, -32.30341f), async () => { await YesnoNPC(1011214); })] = Areas["418"];
                Areas["419"].Entrances[new Entrance("Fortemps Manor => The Pillars", new Vector3(14.64971f, 16.00967f, -7.255503f), new Vector3(-0.01531982f, 1.144348f, 13.19904f), async () => { await YesnoNPC(2005334); })] = Areas["433"];
                Areas["433"].Entrances[new Entrance("The Pillars => Fortemps Manor", new Vector3(0.9157115f, 0.006256104f, 11.2496f), new Vector3(17.99036f, 16.00967f, -9.567444f), async () => { await YesnoNPC(1011217); })] = Areas["419"];

                /** Coerthas Central Highlands **/
                Areas["155"].Entrances[new Entrance("Coerthas Central Highlands Aetheryte", new Vector3(227.0998f, 312f, -229.2892f), new Vector3(), async () => { await AetheryteTo(23); }, () => CanAetheryte(23))] = null;
                Areas["155"].Entrances[new Entrance("North Shroud => Coerthas Central Highlands", new Vector3(2.071978f, 188.1056f, 561.2241f), new Vector3(-370.9257f, -4.079877f, 182.618f))] = Areas["154"];
                Areas["155"].Entrances[new Entrance("Mor Dhona => Coerthas Central Highlands", new Vector3(-236.4915f, 218.9656f, 685.1878f), new Vector3(129.9205f, 31.71007f, -774.7407f))] = Areas["156"];
                Areas["155"].Entrances[new Entrance("Foundation => Coerthas Central Highlands", new Vector3(-162.1208f, 304.1538f, -319.8753f), new Vector3(4.592957f, -2.52555f, 149.4926f), async () => { await YesnoNPC(1011224); })] = Areas["418"];
                Areas["155"].Entrances[new Entrance("Intercessory => Coerthas Central Highlands", new Vector3(264.0891f, 302.1291f, -219.9644f), new Vector3(-4.013123f, 1.174927f, 10.97125f), async () => { await YesnoNPC(2004781); })] = Areas["395"];
                Areas["395"].Entrances[new Entrance("Coerthas Central Highlands => Intercessory", new Vector3(-3.985f, -5.960464E-08f, 7.424f), new Vector3(264.9119f, 302.2624f, -223.7126f), async () => { await YesnoNPC(1009973); })] = Areas["155"];
                #endregion

                // Airships
                Areas["132"].Entrances[new Entrance("Airship: Ul'dah => Gridania (120 gil)", CommonLocation.GridaniaAirshipArrival, CommonNPC.UldahAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.UldahAirshipTicketer.Item1, 0); }, HasAirshipPass)] = Areas["130a"];
                Areas["128a"].Entrances[new Entrance("Airship: Ul'dah => Limsa Lominsa (120 gil)", CommonLocation.LimsaAirshipArrival, CommonNPC.UldahAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.UldahAirshipTicketer.Item1, 1); }, HasAirshipPass)] = Areas["130a"];
                Areas["132"].Entrances[new Entrance("Airship: Limsa Lominsa => Gridania (120 gil)", CommonLocation.GridaniaAirshipArrival, CommonNPC.LimsaAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.LimsaAirshipTicketer.Item1, 1); }, HasAirshipPass)] = Areas["128a"];
                Areas["130a"].Entrances[new Entrance("Airship: Limsa Lominsa => Ul'dah (120 gil)", CommonLocation.UldahAirshipArrival, CommonNPC.LimsaAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.LimsaAirshipTicketer.Item1, 1); }, HasAirshipPass)] = Areas["128a"];
                Areas["130a"].Entrances[new Entrance("Airship: Gridania => Ul'dah (120 gil)", CommonLocation.UldahAirshipArrival, CommonNPC.GridaniaAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.GridaniaAirshipTicketer.Item1, 0); }, HasAirshipPass)] = Areas["132"];
                Areas["128a"].Entrances[new Entrance("Airship: Gridania => Limsa Lominsa (120 gil)", CommonLocation.LimsaAirshipArrival, CommonNPC.GridaniaAirshipTicketer.Item2, async () => { await YesnoNPC(CommonNPC.GridaniaAirshipTicketer.Item1, 0); }, HasAirshipPass)] = Areas["132"];

                #region Thanalan
                // Ul'dah - Steps of Nald
                Areas["130"].Entrances[new Entrance("Ul'dah - Steps of Nald Aetheryte", new Vector3(-144.5182f, -1.358093f, -169.6651f), new Vector3(), async () => { await AetheryteTo(9); }, () => CanAetheryte(9))] = null;
                Areas["130"].Entrances[new Entrance("Ul'dah - Steps of Thal (Sapphire Avenue Exchange) => Ul'dah - Steps of Nald (Gate of Nald)", new Vector3(86.09682f, 4f, -117.2348f), new Vector3(88.73158f, 4f, -113.0255f))] = Areas["131"];
                Areas["130"].Entrances[new Entrance("Ul'dah - Steps of Thal (Gold Court) => Ul'dah - Steps of Nald)", new Vector3(-13.62124f, 10f, -48.27236f), new Vector3(-9.119192f, 10f, -36.26114f))] = Areas["131"];
                Areas["130"].Entrances[new Entrance("Ul'dah - Steps of Thal (Coliseum) => Ul'dah - Steps of Nald", new Vector3(-124.3192f, 9.999994f, -7.104227f), new Vector3(-114.8121f, 8.450851f, -9.050534f))] = Areas["131"];
                Areas["130"].Entrances[new Entrance("Central Thanalan => Ul'dah - Steps of Nald", new Vector3(39.47267f, 4f, -151.0563f), new Vector3(-117.0586f, 18.34012f, 343.7968f))] = Areas["141"];
                Areas["130"].Entrances[new Entrance("Western Thanalan => Ul'dah - Steps of Nald", new Vector3(-169.793f, 13.79746f, -13.04881f), new Vector3(477.6428f, 96.62057f, 159.6893f))] = Areas["140"];

                // Ul'dah - Lift
                Areas["130"].Entrances[new Entrance("Lift: Ul'dah Steps of Nald (Airship Landing) => Ul'dah - Steps of Nald", new Vector3(-20.82143f, 10f, -43.83702f), new Vector3(-25.98621f, 81.8f, -31.99823f), async () => { await YesnoNPC(1004339, 1); })] = Areas["130a"];
                Areas["130"].Entrances[new Entrance("Lift: Ul'dah - Steps of Thal => Ul'dah Steps of Nald", new Vector3(-20.82143f, 10f, -43.83702f), new Vector3(-19.36377f, 34f, -42.58801f), async () => { await YesnoNPC(1001854, 1); })] = Areas["131"];
                Areas["130a"].Entrances[new Entrance("Lift: Ul'dah - Steps of Nald => Ul'dah Steps of Nald (Airship Landing)", new Vector3(320.301f, -40.425f, 372.9197f), new Vector3(-23.33112f, 10f, -43.44244f), async () => { await YesnoNPC(1001834, 0); })] = Areas["130"];
                Areas["130a"].Entrances[new Entrance("Lift: Ul'dah - Steps of Thal => Ul'dah Steps of Nald (Airship Landing)", new Vector3(320.301f, -40.425f, 372.9197f), new Vector3(-19.36377f, 34f, -42.58801f), async () => { await YesnoNPC(1001854, 0); })] = Areas["131"];
                Areas["131"].Entrances[new Entrance("Lift: Ul'dah - Steps of Nald => Ul'dah Steps of Thal", new Vector3(-20.16854f, 34f, -43.80382f), new Vector3(-23.33112f, 10f, -43.44244f), async () => { await YesnoNPC(1001834, 1); })] = Areas["130"];
                Areas["131"].Entrances[new Entrance("Lift: Ul'dah Steps of Nald (Airship Landing) => Ul'dah - Steps of Thal", new Vector3(-20.16854f, 34f, -43.80382f), new Vector3(-25.98621f, 81.8f, -31.99823f), async () => { await YesnoNPC(1004339, 0); })] = Areas["130a"];

                // Ul'dah - Steps of Thal
                Areas["131"].Entrances[new Entrance("Ul'dah - Steps of Nald (Gate of Nald) => Ul'dah - Steps of Thal (Sapphire Avenue Exchange)", new Vector3(102.7438f, 4f, -106.1348f), new Vector3(98.42829f, 4f, -104.3171f))] = Areas["130"];
                Areas["131"].Entrances[new Entrance("Ul'dah - Steps of Nald => Ul'dah - Steps of Thal (Gold Court)", new Vector3(-5.544728f, 14.01931f, -22.75612f), new Vector3(-9.475415f, 10f, -36.95284f))] = Areas["130"];
                Areas["131"].Entrances[new Entrance("Ul'dah - Steps of Nald => Ul'dah - Steps of Thal (Coliseum)", new Vector3(-105.1949f, 6.98457f, -6.699931f), new Vector3(-116.255f, 8.925577f, -9.35843f))] = Areas["130"];

                // Western Thanalan
                Areas["140"].Entrances[new Entrance("Horizon Aetheryte", new Vector3(71.5827f, 45f, -221.1892f), new Vector3(), async () => { await AetheryteTo(17); }, () => CanAetheryte(17))] = null;
                Areas["140"].Entrances[new Entrance("Ul'dah - Steps of Nald => Western Thanalan", new Vector3(461.9227f, 95.93246f, 157.761f), new Vector3(-185.3983f, 13.19162f, -14.15938f))] = Areas["130"];
                Areas["140"].Entrances[new Entrance("Ferry: Limsa Lominsa => Western Thanalan (80 gil)", new Vector3(-485.4331f, 23.99994f, -324.3528f), new Vector3(-360.9217f, 8.000013f, 38.92566f), async () => { await YesnoNPC(1003540); })] = Areas["129"];
                Areas["140"].Entrances[new Entrance("Ferry: Crescent Cove => Silver Bazaar (40 gil)", new Vector3(-300.7811f, 13.98375f, 320.6214f), new Vector3(-335.9579f, 13.9835f, -99.65674f), async () => { await YesnoNPC(1004019); })] = Areas["140"];
                Areas["140"].Entrances[new Entrance("Ferry: Silver Bazaar => Crescent Cove (40 gil)", new Vector3(-330.3051f, 13.9835f, -115.0322f), new Vector3(-298.7564f, 13.9837f, 303.3646f), async () => { await YesnoNPC(1002039); })] = Areas["140"];
                Areas["140"].Entrances[new Entrance("Waking Sands => Vesper Bay", new Vector3(-482.4016f, 17.07477f, -386.8822f), new Vector3(-15.7016f, 1.083313f, -0.01531982f), async () => { await YesnoNPC(2001716); })] = Areas["212"];
                Areas["140"].Entrances[new Entrance("Central Thanalan => Western Thanalan", new Vector3(259.2195f, 52.57924f, -2.305837f), new Vector3(-411.4633f, -2.477333f, 99.8319f))] = Areas["141"];

                // Waking Sands
                Areas["212"].Entrances[new Entrance("Vesper Bay => Waking Sands", new Vector3(-11.33008f, 0.01275751f, 0.193f), new Vector3(-480.9186f, 17.99036f, -386.862f), async () => { await YesnoNPC(2001711); })] = Areas["140"];
                Areas["212"].Entrances[new Entrance("Waking Sands Solar => Waking Sands", new Vector3(20.71891f, 1.022653f, -0.587f), new Vector3(25.4978f, 2.090454f, -0.01531982f), async () => { await YesnoNPC(2001717); })] = Areas["212a"];
                Areas["212a"].Entrances[new Entrance("Waking Sands => Waking Sands - Solar", new Vector3(31.13166f, 1.016423f, -0.137f), new Vector3(23.23944f, 2.090454f, -0.01531982f), async () => { await YesnoNPC(2001715); })] = Areas["212"];

                // Central Thanalan
                Areas["141"].Entrances[new Entrance("Black Bursh Station Aetheryte", new Vector3(-16.1593f, 0.3203735f, -166.5828f), new Vector3(), async () => { await AetheryteTo(53); }, () => CanAetheryte(53))] = null;
                Areas["141"].Entrances[new Entrance("Ul'dah - Steps of Nald => Central Thanalan", new Vector3(-111.8262f, 17.89077f, 330.5841f), new Vector3(45.09832f, 3.254284f, -168.5899f))] = Areas["130"];
                Areas["141"].Entrances[new Entrance("Eastern Thanalan => Central Thanalan", new Vector3(442.8031f, -17.99998f, -171.5594f), new Vector3(-570.1678f, -18.3384f, 344.5357f))] = Areas["145"];
                Areas["141"].Entrances[new Entrance("Northern Thanalan => Central Thanalan", new Vector3(-29.34273f, 33f, -479.1114f), new Vector3(36.29619f, 6.186262f, 519.8497f))] = Areas["147"];
                Areas["141"].Entrances[new Entrance("Western Thanalan => Central Thanalan", new Vector3(-395.2121f, -0.2030827f, 95.3881f), new Vector3(263.4269f, 54.0038f, -14.6999f))] = Areas["140"];
                Areas["141"].Entrances[new Entrance("Southern Thanalan => Central Thanalan", new Vector3(221.1204f, 2.753296f, 662.5154f), new Vector3(-432.6459f, 12.8645f, -432.5895f))] = Areas["146"];

                // Eastern Thanalan
                Areas["145"].Entrances[new Entrance("Camp Drybone Aetheryte", new Vector3(-386.3432f, -57.1756f, 142.5956f), new Vector3(), async () => { await AetheryteTo(18); }, () => CanAetheryte(18))] = null;
                Areas["145"].Entrances[new Entrance("Central Thanalan => Eastern Thanalan", new Vector3(-559.1193f, -19.88919f, 334.06f), new Vector3(455.6808f, -17.99983f, -184.7122f))] = Areas["141"];
                Areas["145"].Entrances[new Entrance("South Shroud => Eastern Thanalan", new Vector3(364.9249f, 29.29049f, -285.2814f), new Vector3(-288.6162f, -0.3033465f, 701.236f))] = Areas["153"];
                Areas["145"].Entrances[new Entrance("Southern Thanalan => Eastern Thanalan", new Vector3(-178.4159f, -44.61437f, 480.1496f), new Vector3(-25.28012f, 18.05769f, -774.562f))] = Areas["146"];

                // Southern Thanalan
                Areas["146"].Entrances[new Entrance("Little Ala Mhigo Aetheryte", new Vector3(-159.3805f, 30.10596f, -415.4575f), new Vector3(), async () => { await AetheryteTo(19); }, () => CanAetheryte(19))] = null;
                Areas["146"].Entrances[new Entrance("Forgotten Springs Aetheryte", new Vector3(-326.6194f, 10.69653f, 406.6376f), new Vector3(), async () => { await AetheryteTo(20); }, () => CanAetheryte(20))] = null;
                Areas["146"].Entrances[new Entrance("Central Thanalan => Southern Thanalan", new Vector3(-415.6913f, 12.86053f, -414.6158f), new Vector3(236.86f, 2.568542f, 678.5849f))] = Areas["141"];
                Areas["146"].Entrances[new Entrance("Eastern Thanalan => Southern Thanalan", new Vector3(-27.06229f, 15.74283f, -758.1667f), new Vector3(-166.7256f, -46.93199f, 495.1603f))] = Areas["145"];
                Areas["146"].Entrances[new Entrance("Southern Thanalan - Nald's Reflection Shortcut", new Vector3(-470.0356f, -2.476013f, 91.57136f), new Vector3(-292.1035f, -2.910112f, 245.5939f), async () => { await YesnoNPC(1004597); })] = Areas["146"];
                Areas["146"].Entrances[new Entrance("Southern Thanalan - Minotaur Malm Shortcut", new Vector3(-297.8185f, -2.227972f, 248.1728f), new Vector3(-471.275f, -2.650424f, 95.6283f), async () => { await YesnoNPC(1004596); })] = Areas["146"];

                // Nothern Thanalan
                Areas["147"].Entrances[new Entrance("Camp Bluefrog Aetheryte", new Vector3(20.98108f, 8.8349f, 454.0321f), new Vector3(), async () => { await AetheryteTo(21); }, () => CanAetheryte(21))] = null;
                Areas["147"].Entrances[new Entrance("Ceruleum Processing Plant Aetheryte", new Vector3(-30.53061f, 48.30948f, -30.50763f), new Vector3(), async () => { await AetheryteTo(22); }, () => CanAetheryte(22))] = null;
                Areas["147"].Entrances[new Entrance("Central Thanalan => Northern Thanalan", new Vector3(33.34794f, 5.380385f, 498.0243f), new Vector3(-27.70755f, 33f, -499.8461f))] = Areas["141"];
                Areas["147"].Entrances[new Entrance("Mor Dhona => Northern Thanalan", new Vector3(-111.2588f, 84.57f, -406.6626f), new Vector3(-417.2111f, -3.216816f, -110.9722f))] = Areas["156"];
                #endregion

                #region La Noscea
                /** Limsa Lominsa Upper Decks **/
                Areas["128"].Entrances[new Entrance("Lower La Noscea => Limsa Lominsa Upper Decks", new Vector3(28.40079f, 44.5f, 169.1677f), new Vector3(-55.94417f, 75.83566f, 116.4803f))] = Areas["135"];
                Areas["128"].Entrances[new Entrance("Limsa Lominsa Lower Decks => Limsa Lominsa Upper Decks (South)", new Vector3(-87.5079f, 35.65992f, 110.8052f), new Vector3(-90.8895f, 22.44292f, 117.4222f))] = Areas["129"];
                Areas["128"].Entrances[new Entrance("Limsa Lominsa Lower Decks => Limsa Lominsa Upper Decks (Drowning Wench)", new Vector3(-0.621704f, 42.99917f, -27.04843f), new Vector3(-9.604985f, 21.82867f, 26.00806f))] = Areas["129"];
                Areas["128"].Entrances[new Entrance("Limsa Lominsa Lower Decks => Limsa Lominsa Upper Decks (Bismarck)", new Vector3(-64.88069f, 41.90938f, -127.8045f), new Vector3(-83.53827f, 18.60168f, -30.40671f))] = Areas["129"];

                /** Limsa Lominsa Lower Decks **/
                Areas["129"].Entrances[new Entrance("Lower Lominsa Lower Decks Aetheryte", new Vector3(-84.03149f, 20.76746f, 0.01519775f), new Vector3(), async () => { await AetheryteTo(8); }, () => CanAetheryte(8))] = null;
                Areas["129"].Entrances[new Entrance("Limsa Lominsa Upper Decks => Limsa Lominsa Lower Decks (South)", new Vector3(-90.57703f, 19.99887f, 102.9584f), new Vector3(-98.31777f, 35.26831f, 102.7672f))] = Areas["128"];
                Areas["129"].Entrances[new Entrance("Limsa Lominsa Upper Decks => Limsa Lominsa Lower Decks (Drowning Wench)", new Vector3(-0.5702329f, 20.00082f, 28.10955f), new Vector3(-10.05185f, 41.48492f, -26.14086f))] = Areas["128"];
                Areas["129"].Entrances[new Entrance("Limsa Lominsa Upper Decks => Limsa Lominsa Lower Decks (Bismarck)", new Vector3(-82.46004f, 18.00033f, -18.44299f), new Vector3(-75.82471f, 38.71817f, -119.815f))] = Areas["128"];
                Areas["129"].Entrances[new Entrance("Middle La Noscea => Limsa Lominsa Lower Decks", new Vector3(49.78521f, 20f, -0.7983324f), new Vector3(-47.67752f, 34.70946f, 155.9692f))] = Areas["134"];
                Areas["129"].Entrances[new Entrance("Ferry: Western Thanalan => Limsa Lominsa (80 gil)", new Vector3(-357.0279f, 8.000004f, 48.82743f), new Vector3(-488.4871f, 24.44998f, -333.5775f), async () => { await YesnoNPC(1004037); })] = Areas["140"];
                Areas["129"].Entrances[new Entrance("Ferry: Aleport => Limsa Lominsa (40 gil)", CommonLocation.LimsaLowerFerryArrival, CommonNPC.AleportFerryman.Item2, async () => { await YesnoNPC(CommonNPC.AleportFerryman.Item1, 0); })] = Areas["138"];
                Areas["129"].Entrances[new Entrance("Ferry: Costa del Sol => Limsa Lominsa (40 gil)", CommonLocation.LimsaLowerFerryArrival, CommonNPC.CostadelSolFerryman.Item2, async () => { await YesnoNPC(CommonNPC.CostadelSolFerryman.Item1, 0); })] = Areas["137b"];

                /** Limsa Lominsa Elevator **/
                Areas["128a"].Entrances[new Entrance("Lift: Limsa Lominsa Upper Decks => Limsa Lominsa Airship Landing", CommonLocation.LimsaAirshipLIftDestination, CommonNPC.LimsaUpperLiftOperator.Item2, async () => {await YesnoNPC(CommonNPC.LimsaUpperLiftOperator.Item1, 0);})] = Areas["128"];
                Areas["128a"].Entrances[new Entrance("Lift: Limsa Lominsa Lower Decks => Limsa Lominsa Airship Landing", CommonLocation.LimsaAirshipLIftDestination, CommonNPC.LimsaLowerLiftOperator.Item2, async () => { await YesnoNPC(CommonNPC.LimsaLowerLiftOperator.Item1, 0); })] = Areas["129"];
                Areas["128"].Entrances[new Entrance("Lift: Limsa Lominsa Lower Decks => Limsa Lominsa Upper Decks", CommonLocation.LimsaUpperLiftDestination, CommonNPC.LimsaLowerLiftOperator.Item2, async () => { await YesnoNPC(CommonNPC.LimsaLowerLiftOperator.Item1, 1); })] = Areas["129"];
                Areas["128"].Entrances[new Entrance("Lift: Limsa Lominsa Airship Landing => Limsa Lominsa Upper Decks", CommonLocation.LimsaUpperLiftDestination, CommonNPC.LimsaAirshipLiftOperator.Item2, async () => { await YesnoNPC(CommonNPC.LimsaAirshipLiftOperator.Item1, 0); })] = Areas["128a"];
                Areas["129"].Entrances[new Entrance("Lift: Limsa Lominsa Upper Decks => Limsa Lominsa Lower Decks", CommonLocation.LimsaLowerLiftDestination, CommonNPC.LimsaUpperLiftOperator.Item2, async () => { await YesnoNPC(CommonNPC.LimsaUpperLiftOperator.Item1, 1); })] = Areas["128"];
                Areas["129"].Entrances[new Entrance("Lift: Limsa Lominsa Airship Landing => Limsa Lominsa Lower Decks", CommonLocation.LimsaLowerLiftDestination, CommonNPC.LimsaAirshipLiftOperator.Item2, async () => { await YesnoNPC(CommonNPC.LimsaAirshipLiftOperator.Item1, 1); })] = Areas["128a"];

                /** Middle La Noscea **/
                Areas["134"].Entrances[new Entrance("Summerford Farms Aetheryte", new Vector3(227.985f, 115.526f, -257.0382f), new Vector3(), async () => { await AetheryteTo(52); }, () => CanAetheryte(52))] = null;
                Areas["134"].Entrances[new Entrance("Limsa Lominsa Lower Decks => Middle La Noscea", new Vector3(67.50195f, 19.12451f, -0.02922494f), new Vector3(-24.16885f, 38.42021f, 149.4466f))] = Areas["129"];
                Areas["134"].Entrances[new Entrance("Lower La Noscea => Middle La Noscea", new Vector3(185.952f, 65.03447f, 285.4184f), new Vector3(227.7118f, 74.85463f, -343.8353f))] = Areas["135"];
                Areas["134"].Entrances[new Entrance("Eastern La Noscea (West) => Middle La Noscea", new Vector3(-166.4881f, 34.49757f, -720.7057f), new Vector3(-119.2448f, 70.31301f, 46.74168f))] = Areas["137a"];
                Areas["134"].Entrances[new Entrance("Western La Noscea => Middle La Noscea", new Vector3(-359.7911f, 32.42902f, -589.2034f), new Vector3(813.3707f, 49.27356f, 396.5388f))] = Areas["138"];

                /** Western La Noscea **/
                Areas["138"].Entrances[new Entrance("Aleport Aetheryte", new Vector3(260.9445f, -19.60791f, 218.5244f), new Vector3(), async () => { await AetheryteTo(14); }, () => CanAetheryte(14))] = null;
                Areas["138"].Entrances[new Entrance("Ferry: Limsa Lominsa Lower Decks => Western La Noscea (40 gil)", CommonLocation.AleportFerryArrival, CommonNPC.LimsaLowerFerryman.Item2, async () => { await YesnoNPC(CommonNPC.LimsaLowerFerryman.Item1, 0); })] = Areas["129"];
                Areas["138"].Entrances[new Entrance("Upper La Noscea (West) => Western La Noscea", new Vector3(406.6572f, 25.39354f, -1.202113f), new Vector3(-483.6111f, 2.342671f, 290.8712f))] = Areas["139a"];
                Areas["138"].Entrances[new Entrance("Middle La Noscea => Western La Noscea", new Vector3(805.982f, 50.66865f, 373.9198f), new Vector3(-376.5387f, 32.77518f, -609.4202f))] = Areas["134"];
                Areas["138"].Entrances[new Entrance("Ferry: Western La Noscea - Isles of Umbra => Aleport", CommonLocation.AleportFerryArrival, CommonNPC.UmbraIslesFerryman.Item2, async () => { await YesnoNPC(CommonNPC.UmbraIslesFerryman.Item1); })] = Areas["138b"];
                Areas["138"].Entrances[new Entrance("Ferry: Candlekeep Quay => Aleport", CommonLocation.AleportFerryArrival, CommonNPC.CandlekeepFerryman.Item2, async () => { await YesnoNPC(CommonNPC.CandlekeepFerryman.Item1); })] = Areas["135"];
                
                /** Western La Noscea - Isles of Umbra **/
                Areas["138b"].Entrances[new Entrance("Ferry: Western La Noscea - Aleport => Isles of Umbra (40 gil)", new Vector3(), CommonNPC.AleportFerryman.Item2, async () => { await YesnoNPC(CommonNPC.AleportFerryman.Item1, 2); })] = Areas["138"];

                /** Lower La Noscea **/
                Areas["135"].Entrances[new Entrance("Moraby Drydocks Aetheryte", new Vector3(156.115f, 15.51843f, 673.2128f), new Vector3(), async () => { await AetheryteTo(10); }, () => CanAetheryte(10))] = null;
                Areas["135"].Entrances[new Entrance("Ferry: Aleport => Candlekeep Quay (40 gil)", CommonLocation.CandlekeepFerryArrival, CommonNPC.AleportFerryman.Item2, async () => { await YesnoNPC(CommonNPC.AleportFerryman.Item1, 1); })] = Areas["138"];
                Areas["135"].Entrances[new Entrance("Limsa Lominsa Upper Decks => Lower La Noscea", new Vector3(-42.33875f, 72.17329f, 116.4148f), new Vector3(24.73166f, 44.49993f, 184.6321f))] = Areas["128"];
                Areas["135"].Entrances[new Entrance("Middle La Noscea => Lower La Noscea", new Vector3(240.606f, 74.5574f, -330.3864f), new Vector3(208.7329f, 65.49705f, 285.4128f))] = Areas["134"];
                Areas["135"].Entrances[new Entrance("Eastern La Noscea (West) => Lower La Noscea", new Vector3(569.4417f, 96.15405f, -511.5478f), new Vector3(-131.346f, 70.52193f, 749.6811f))] = Areas["137a"];
                Areas["135"].Entrances[new Entrance("Eastern La Noscea (East) => Lower La Noscea", new Vector3(695.5978f, 79.21304f, -378.4725f), new Vector3(244.0079f, 56.77236f, 843.5204f))] = Areas["137b"];

                /** Eastern La Noscea (West) **/
                Areas["137a"].Entrances[new Entrance("Wineport Aetheryte", new Vector3(-18.38715f, 72.67859f, 3.829956f), new Vector3(), async () => { await AetheryteTo(12); }, () => CanAetheryte(12))] = null;
                Areas["137a"].Entrances[new Entrance("Lower La Noscea => Eastern La Noscea (West)", new Vector3(-129.5122f, 67.80698f, 728.1898f), new Vector3(570.6517f, 96.49591f, -529.3972f))] = Areas["135"];
                Areas["137a"].Entrances[new Entrance("Middle La Noscea => Eastern La Noscea (West)", new Vector3(-99.86313f, 70.34401f, 42.60612f), new Vector3(-160.4433f, 36.23851f, -740.9053f))] = Areas["134"];
                Areas["137a"].Entrances[new Entrance("Upper La Noscea (East) => Eastern La Noscea (West)", new Vector3(85.30895f, 78.87074f, -100.2011f), new Vector3(717.2458f, 0.2723883f, 220.1715f))] = Areas["139b"];
                Areas["137a"].Entrances[new Entrance("Ferry: Eastern La Noscea (East) => (West) (40 gil", new Vector3(22.89248f, 34.07887f, 224.6646f), new Vector3(344.6859f, 31.88946f, 89.37195f), async () => { await YesnoNPC(1003588); })] = Areas["137b"];

                /** Eastern La Noscea (East) **/
                Areas["137b"].Entrances[new Entrance("Costa del Sol Aetheryte", new Vector3(489.1584f, 20.82849f, 468.803f), new Vector3(), async () => { await AetheryteTo(11); }, () => CanAetheryte(11))] = null;
                Areas["137b"].Entrances[new Entrance("Lower La Noscea => Eastern La Noscea (East)", new Vector3(249.671f, 55.15573f, 820.3791f), new Vector3(694.3947f, 80.30103f, -393.9648f))] = Areas["135"];
                Areas["137b"].Entrances[new Entrance("Ferry: Limsa Lominsa Lower Decks => Costa del Sol (40 gil)", CommonLocation.CostadelSolFerryArrival, CommonNPC.LimsaLowerFerryman.Item2, async () => { await YesnoNPC(CommonNPC.LimsaLowerFerryman.Item1, 1); })] = Areas["129"];
                Areas["137b"].Entrances[new Entrance("Ferry: Eastern La Noscea (West) => (East) (40 gil)", new Vector3(345.9356f, 32.76929f, 95.30911f), new Vector3(20.46234f, 34.07887f, 222.156f), async () => { await YesnoNPC(1003589); })] = Areas["137a"];

                /** Upper La Noscea (West) **/
                Areas["139a"].Entrances[new Entrance("Western La Noscea => Upper La Noscea (West)", new Vector3(-467.5851f, 0.5428234f, 277.4943f), new Vector3(410.082f, 32.36998f, -14.46223f))] = Areas["138"];
                Areas["139a"].Entrances[new Entrance("Upper La Noscea (East) => Upper La Noscea (West)", new Vector3(-338.2086f, -1.024994f, 113.7944f), new Vector3(220.9048f, -0.9591975f, 257.4043f), async () => { await YesnoNPC(1003587); })] = Areas["139b"];
                Areas["139a"].Entrances[new Entrance("Outer La Noscea => Upper La Noscea (West)", new Vector3(-349.9635f, 47.59047f, -15.20869f), new Vector3(-322.9061f, 49.65966f, -76.66608f))] = Areas["180"];

                /** Upper La Noscea (East) **/
                Areas["139b"].Entrances[new Entrance("Camp Bronze Lake Aetheryte", new Vector3(437.4303f, 5.508484f, 94.59058f), new Vector3(), async () => { await AetheryteTo(15); }, () => CanAetheryte(15))] = null;
                Areas["139b"].Entrances[new Entrance("Upper La Noscea (West) => Upper La Noscea(East)", new Vector3(220.9477f, -0.9591975f, 259.2396f), new Vector3(-342.1226f, -1.024988f, 111.467f), async () => { await YesnoNPC(1003586); })] = Areas["139a"];
                Areas["139b"].Entrances[new Entrance("Eastern La Noscea (West) => Upper La Noscea (East)", new Vector3(715.241f, 0.9529843f, 201.5285f), new Vector3(74.10374f, 81.03893f, -125.8123f))] = Areas["137a"];
                Areas["139b"].Entrances[new Entrance("Outer La Noscea => Upper La Noscea(East)", new Vector3(290.8214f, 41.23425f, -195.5544f), new Vector3(250.6487f, 52.03183f, -247.5523f))] = Areas["180"];

                /** Outer La Noscea **/
                Areas["180"].Entrances[new Entrance("Camp Overlook Aetheryte", new Vector3(-117.5403f, 66.02576f, -212.665f), new Vector3(), async () => { await AetheryteTo(16); }, () => CanAetheryte(16))] = null;
                Areas["180"].Entrances[new Entrance("Upper Loa Noscea (West) => Outer La Noscea", new Vector3(-338.6587f, 48.9953f, -20.68578f), new Vector3(-332.9697f, 53.48553f, -94.12056f))] = Areas["139a"];
                Areas["180"].Entrances[new Entrance("Upper Loa Noscea (East) => Outer La Noscea", new Vector3(230.4483f, 56.94048f, -253.7788f), new Vector3(281.8253f, 43.83224f, -207.8224f))] = Areas["139b"];
                #endregion

                /** Mor Dhona **/
                Areas["156"].Entrances[new Entrance("Revenant's Toll Aetheryte", new Vector3(40.02429f, 24.00244f, -668.0247f), new Vector3(), async () => { await AetheryteTo(24); }, () => CanAetheryte(24))] = null;
                Areas["156"].Entrances[new Entrance("Coerthas Central Highlands => Mor Dhona", new Vector3(101.1873f, 30.71345f, -746.9458f),new Vector3(-216.9399f, 217.2534f, 705.7411f))] = Areas["155"];
                Areas["156"].Entrances[new Entrance("Northern Thanalan => Mor Dhona", new Vector3(-425.7081f, -3.216816f, -135.04f), new Vector3(-92.94735f, 84.4271f, -419.7699f))] = Areas["147"];
                Areas["156"].Entrances[new Entrance("Rising Stones => Revenant's Toll", new Vector3(22.47181f, 21.25273f, -635.0448f), new Vector3(0f, 3f, 27.5f), async () => { await YesnoNPC(2002879); })] = Areas["351"];
                Areas["351"].Entrances[new Entrance("Revenant's Toll => Rising Stones", new Vector3(0.133f, 1.999999f, 22.579f), new Vector3(21.13373f, 22.32391f, -631.281f), async () => { await YesnoNPC(2002881); })] = Areas["156"];
                Areas["351a"].Entrances[new Entrance("Rising Stones => Rising Stones Solar", new Vector3(-0.158f, -1.995725f, -35.126f), new Vector3(-0.01531982f, -1.022339f, -26.7796f), async () => { await YesnoNPC(2002878); })] = Areas["351"];
                Areas["351"].Entrances[new Entrance("Rising Stones Solar => Rising Stones", new Vector3(0.092f, -2.000001f, -23.359f), new Vector3(-0.01531982f, -1.022339f, -29.25159f), async () => { await YesnoNPC(2002880); })] = Areas["351a"];

                #region Black Shroud
                /** New Gridania **/
                Areas["132"].Entrances[new Entrance("New Gridania Aetheryte", new Vector3(32.9137f, 2.670288f, 30.0144f), new Vector3(), async () => { await AetheryteTo(2); }, () => CanAetheryte(2))] = null;
                Areas["132"].Entrances[new Entrance("Central Shroud => New Gridania", new Vector3(145.6204f, -13.13162f, 156.1534f), new Vector3(128.952f, 25.16017f, -319.9819f))] = Areas["148"];
                Areas["132"].Entrances[new Entrance("Old Gridania => New Gridania (East)", new Vector3(98.47223f, 4.56348f, 18.99329f), new Vector3(136.427f, 10.84288f, -19.23801f))] = Areas["133"];
                Areas["132"].Entrances[new Entrance("Old Gridania => New Gridania (Central)", new Vector3(8.928661f, 0.8263273f, -3.976203f), new Vector3(12.31165f, 8.22765f, -83.77032f))] = Areas["133"];
                Areas["132"].Entrances[new Entrance("Old Gridania => New Gridania (West)", new Vector3(-100.9869f, 0.7769685f, 15.4259f), new Vector3(-125.5775f, 5.141682f, -34.72672f))] = Areas["133"];

                /** Old Gridania **/
                Areas["133"].Entrances[new Entrance("New Gridania => Old Gridania (East)", new Vector3(150.8661f, 12.78843f, -32.86936f), new Vector3(103.2913f, 5.536325f, 11.79158f))] = Areas["132"];
                Areas["133"].Entrances[new Entrance("New Gridania => Old Gridania (Central)", new Vector3(14.31013f, 8.291188f, -94.10746f), new Vector3(11.99076f, 1.299149f, -17.55139f))] = Areas["132"];
                Areas["133"].Entrances[new Entrance("New Gridania => Old Gridania (West)", new Vector3(-137.8023f, 5.092733f, -34.99907f), new Vector3(-106.1919f, 1.381603f, 4.724148f))] = Areas["132"];
                Areas["133"].Entrances[new Entrance("North Shroud => Old Gridania", new Vector3(459.3893f, -1.486341f, 189.8555f), new Vector3(-198.8998f, 10.42959f, -93.88439f))] = Areas["154"];
                Areas["133"].Entrances[new Entrance("East Shroud => Old Gridania", new Vector3(177.529f, -1.805331f, -244.6017f), new Vector3(-578.027f, 7.932977f, 74.81494f), async () => { await YesnoNPC(1000541); })] = Areas["152"];

                /** Central Shroud **/
                Areas["148"].Entrances[new Entrance("Bentbranch Meadows Aetheryte", new Vector3(13.0769f, 0.5645142f, 35.90442f), new Vector3(), async () => { await AetheryteTo(3); }, () => CanAetheryte(3))] = null;
                Areas["148"].Entrances[new Entrance("New Gridania => Central Shroud", new Vector3(129.7087f, 24.94524f, -304.9619f), new Vector3(160.0627f, -12.95517f, 158.6402f))] = Areas["132"];
                Areas["148"].Entrances[new Entrance("East Shroud => Central Shroud", new Vector3(380.9508f, -3.194325f, -182.1808f), new Vector3(-514.5909f, 19.05822f, 280.8161f))] = Areas["152"];
                Areas["148"].Entrances[new Entrance("South Shroud => Central Shroud", new Vector3(159.7071f, -23.89946f, 534.0981f), new Vector3(-371.92f, 30.84801f, -248.1044f))] = Areas["153"];
                Areas["148"].Entrances[new Entrance("North Shroud => Central Shroud", new Vector3(-500.2215f, 72.74133f, -333.4413f), new Vector3(21.09042f, -54.53596f, 536.2278f))] = Areas["154"];

                /** East Shroud **/
                Areas["152"].Entrances[new Entrance("The Hawthorne Hut Aetheryte", new Vector3(-186.5416f, 3.799438f, 297.5662f), new Vector3(), async () => { await AetheryteTo(4); }, () => CanAetheryte(4))] = null;
                Areas["152"].Entrances[new Entrance("Old Gridania => East Shroud", new Vector3(-569.4891f, 8.895483f, 78.67169f), new Vector3(181.4144f, -2.35195f, -240.4059f), async () => { await YesnoNPC(1001263, 0); })] = Areas["133"];
                Areas["152"].Entrances[new Entrance("South Shroud => East Shroud", new Vector3(-173.4916f, 6.542872f, 451.9733f), new Vector3(278.5638f, 10.36668f, -263.5502f))] = Areas["153"];
                Areas["152"].Entrances[new Entrance("Central Shroud => East Shroud", new Vector3(-515.3698f, 18.11873f, 268.9737f), new Vector3(389.1552f, -3.401926f, -188.643f))] = Areas["148"];

                /** South Shroud **/
                Areas["153"].Entrances[new Entrance("Quarrymill Aetheryte", new Vector3(178.6068f, 10.54395f, -68.19263f), new Vector3(), async () => { await AetheryteTo(5); }, () => CanAetheryte(5))] = null;
                Areas["153"].Entrances[new Entrance("Camp Tranquil Aetheryte", new Vector3(-230.0603f, 22.62909f, 355.4589f), new Vector3(), async () => { await AetheryteTo(6); }, () => CanAetheryte(6))] = null;
                Areas["153"].Entrances[new Entrance("East Shroud => South Shroud", new Vector3(275.1211f, 11.12285f, -253.3687f), new Vector3(-157.1264f, 3.225082f, 452.0183f))] = Areas["152"];
                Areas["153"].Entrances[new Entrance("Central Shroud => South Shroud", new Vector3(-360.2558f, 27.72983f, -232.4207f), new Vector3(160.573f, -23.98136f, 555.6692f))] = Areas["148"];
                Areas["153"].Entrances[new Entrance("Eastern Thanalan => South Shroud", new Vector3(-273.2602f, 1.110993f, 677.6337f), new Vector3(375.2838f, 33.79282f, -300.2152f))] = Areas["145"];

                /** South Shroud (Raya-O-Senna)**/
                Areas["153b"].Entrances[new Entrance("South Shroud Jump to Raya-O-Senna", new Vector3(-137.6063f, 7.416739f, 273.5531f), new Vector3(-143.325f, 8.179872f, 269.7141f), async () => {
                    if (Core.Player.IsMounted) {
                        ff14bot.Managers.Actionmanager.Dismount();
                        await Buddy.Coroutines.Coroutine.Sleep(3000);
                    }
                    ff14bot.Managers.MovementManager.SetFacing(0.9891489f);
                    ff14bot.Managers.MovementManager.MoveForwardStart();
                    await Buddy.Coroutines.Coroutine.Sleep(500);
                    ff14bot.Managers.MovementManager.Jump();
                    await Buddy.Coroutines.Coroutine.Sleep(1000);
                    ff14bot.Managers.MovementManager.MoveForwardStop();
                    await Buddy.Coroutines.Coroutine.Sleep(200);
                })] = Areas["153"];

                /** North Shroud **/
                Areas["154"].Entrances[new Entrance("Fallgourd Float Aetheryte", new Vector3(-41.58087f, -38.55963f, 233.7528f), new Vector3(), async () => { await AetheryteTo(7); }, () => CanAetheryte(7))] = null;
                Areas["154"].Entrances[new Entrance("Coerthas Central Highlands => North Shroud", new Vector3(-357.282f, -10.38887f, 199.3128f), new Vector3(11.50554f, 181.0844f, 589.8878f))] = Areas["155"];
                Areas["154"].Entrances[new Entrance("Old Gridania => North Shroud", new Vector3(451.1789f, -1.579551f, 199.6343f), new Vector3(-212.1242f, 10.22579f, -93.75554f))] = Areas["133"];
                Areas["154"].Entrances[new Entrance("Central Shroud => North Shroud", new Vector3(-2.348698f, -56.6928f, 503.0575f), new Vector3(-501.3149f, 74.43162f, -361.8614f))] = Areas["148"];
                #endregion

                LimsaAethernet();
                UldahAethernet();
                GridaniaAethernet();
                IshgardAethernet();

                foreach (Area a in Areas.Values) {
                    foreach (Entrance e in a.Entrances.Keys) {
                        e.Destination = a;
                        Entrances[e] = a;
                        if (a.Entrances[e] != null) {
                            e.Source = a.Entrances[e];
                            a.Entrances[e].Exits[e] = a;
                        }
                    }
                }
            }
            public static void Add(Area a) {
                Areas[a.Name] = a;
            }
            public static Area getArea(Vector3 Location, uint ZoneId) {
                if (Areas == null) Init();

                // Upper La Noscea
                if (ZoneId == 139) {
                    if (Location.X < -100f) return Areas["139a"];
                    else return Areas["139b"];
                }

                // Western La Noscea -- rough estimates
                if (ZoneId == 138) {
                    if (Location.X < -200f && Location.Z > 350f) return Areas["138b"];
                    return Areas["138"];
                }

                // Eastern La Noscea
                if (ZoneId == 137) {
                    if (Location.Z < 32.81161f) return Areas["137a"];
                    if (Location.X > 165.6897f) return Areas["137b"];
                    return Areas["137a"];
                }

                // Ul'dah - Steps of Nald
                if (ZoneId == 130) {
                    if (Location.Y > 70f) return Areas["130a"];
                    else return Areas["130"];
                }

                // Waking Sands
                if (ZoneId == 212) {
                    if (Location.X > 24.8f) return Areas["212a"];
                    return Areas["212"];
                }

                // Rising Stones
                // z < -28.73209f
                // z = -27.20882
                if (ZoneId == 351) {
                    if (Location.Z < -28f) return Areas["351a"];
                    return Areas["351"];
                }

                // Limsa Lominsa Upper Decks
                if (ZoneId == 128) {
                    if (Location.Y > 70f) return Areas["128a"];
                    else return Areas["128"];
                }

                // South Shroud
                if (ZoneId == 153) {
                    Vector3 RayaOSenna = new Vector3(-139.4522f, 8.712891f, 281.6968f);
                    if (Location.Y > 5.0 && RayaOSenna.Distance(Location) < 10f) {
                        return Areas["153b"];
                    }

                    return Areas["153"];
                }

                // Sea of Clouds
                if (ZoneId == 401) {
                    // XYZ="-652.7488, -176.5292, 304.9598"
                    if (Location.X < -652.7488f) {
                        if (Location.Z > 304.9598f) return Areas["401a"];
                        else return Areas["401b"];
                    }
                    // XYZ="-516.8286, -135.2137, 118.1619"
                    if (Location.X < -516.8286f) {
                        if (Location.Z > 118.1619f) return Areas["401a"];
                        else return Areas["401b"];
                    }
                    // XYZ="518.5327, -39.81855, 221.7367"
                    if (Location.X < 518.5327f) {
                        if (Location.Z > 221.7367f) return Areas["401a"];
                        else return Areas["401b"];
                    }
                    // XYZ="569.8486, -44.1792, -221.9509"
                    if (Location.Z > -221.9509f) return Areas["401a"];
                    return Areas["401b"];
                }

                if (!Areas.ContainsKey(ZoneId.ToString())) {
                    return Areas["Unknown Area"];
                }

                return Areas[ZoneId.ToString()];
            }

            /**
             * Use Dijkstra's Algorithm
             * 
             * We'll consider each entrance a vertex and the distance between entrances in each zone
             * an edge.
             */
            public static Entrance[] ShortestPath(Vector3 Location, uint ZoneId) {
                if (Debug) ff14bot.Helpers.Logging.Write("In Shortest Path from {0}: {1} to {2}: {3}", WorldManager.ZoneId, Core.Player.Location, ZoneId, Location);
                List<Entrance> Result = new List<Entrance>();
                Area TargetArea = getArea(Location, ZoneId);
                Area CurrentArea = getArea(Core.Player.Location, WorldManager.ZoneId);
                
                if (Debug) ff14bot.Helpers.Logging.Write("Resolving Current Area: [{0}] and TargetArea: [{1}]", CurrentArea.Name, TargetArea.Name);

                /*
                if (TargetArea == CurrentArea) {
                    return Result.ToArray();
                }
                 */

                // non-optimal solution for now:
                // propagate out from Target until we find an aetheryte or our current area
                Dictionary<Entrance, Entrance> previous = new Dictionary<Entrance, Entrance>();
                Dictionary<Entrance, float> distance = new Dictionary<Entrance, float>();
                List<Entrance> nodes = new List<Entrance>();
                List<Entrance> Candidates = new List<Entrance>();

                Entrance TargetLocation = new Entrance("Target Location", Location, Location);

                foreach (Entrance e in Entrances.Keys) {
                    if (e.Destination == TargetArea) {
                        distance[e] = Location.Distance2D(e.Location);
                        previous[e] = TargetLocation;
                    } else {
                        distance[e] = float.MaxValue;
                        previous[e] = null;
                    }
                    nodes.Add(e);
                }
                
                while (nodes.Count > 0) {
                   nodes.Sort((x, y) => distance[x] - distance[y] < 0 ? -1 : 1);
                    Entrance ent = nodes[0];
                    nodes.Remove(ent);
                    
                    /**
                    ff14bot.Helpers.Logging.Write("# nodes: {0}", nodes.Count);

                    ff14bot.Helpers.Logging.Write("node: {0} -- {1} || {2} --- prev: {3}", ent.Name, distance[ent], ent.Destination.Name, previous[ent] != null ? previous[ent].Name : "-");
                    foreach (Entrance e in nodes) {
                        ff14bot.Helpers.Logging.Write("{0} -- {1} || {2} --- prev: {3}", e.Name, distance[e], e.Destination.Name, previous[e] != null ? previous[e].Name : "-");
                    }
                    */
                    
                 

                    if (!ent.CanUse()) {
                        continue;
                    }

                    if (ent.Source != null) {
                        foreach (Entrance e in ent.Source.Entrances.Keys) {
                            float VisitDistance = distance[ent] + 25.0f + ent.SourceLocation.Distance2D(e.Location);
                            if (VisitDistance < distance[e]) {
                                previous[e] = ent;
                                distance[e] = VisitDistance;
                            }
                        }

                        if (CurrentArea == ent.Source) {
                            Candidates.Add(ent);
                        }
                    } else {
                        Candidates.Add(ent);
                    }
                }


                int DebugsPrinted = 0;
                foreach (Entrance e in Candidates) {
                    if (e.Source == null && distance[e] < float.MaxValue) {
                        distance[e] += 100f;
                    } else {
                        distance[e] += e.SourceLocation.Distance2D(Core.Player.Location);
                    }
                    if (Debug && DebugsPrinted++ < 6) ff14bot.Helpers.Logging.Write("candidate: {0} -- {1} || {2} --- prev: {3}", e.Name, distance[e], e.Destination.Name, previous[e] != null ? previous[e].Name : "-");
                }

                Candidates.Sort((x, y) => distance[x] - distance[y] < 0 ? -1 : 1);

                if (Candidates.Count == 0) {
                    return Result.ToArray();
                }
                Entrance itr = Candidates[0];
                // case where we are near enough to not use any entrances
                if (CurrentArea == TargetArea) {
                    if (Core.Player.Location.Distance2D(Location) <= distance[itr]) {
                        if (Debug) ff14bot.Helpers.Logging.Write("Nearest path is by foot");
                        return Result.ToArray();
                    }
                }

                while (itr != TargetLocation) {
                    ff14bot.Helpers.Logging.Write(itr.Name);
                    Result.Add(itr);
                    itr = previous[itr];
                }

                return Result.ToArray();
            }
        }
        public class Area {
            public string Name;
            public ushort ZoneId;
            public Dictionary<Entrance, Area> Entrances = new Dictionary<Entrance, Area>();
            public Dictionary<Entrance, Area> Exits = new Dictionary<Entrance, Area>();
            public Area(string n, ushort z) {
                Name = n;
                ZoneId = z;
            }
        }
        public class Entrance {
            public static Vector3 NullVector3 = new Vector3();
            public string Name;
            public Vector3 Location;
            public Vector3 SourceLocation;
            public delegate bool CanUseDelegate();
            public delegate Task UseDelegate();
            public CanUseDelegate CanUse;
            public UseDelegate Use;
            public Area Source;
            public Area Destination;
            public float InteractDistance = 3f;
            public Entrance(string n, Vector3 l, Vector3 sl, UseDelegate u = null, CanUseDelegate c = null) {
                Name = n;
                Location = l;
                SourceLocation = sl;
                if (c == null) {
                    CanUse = () => {
                        //ff14bot.Helpers.Logging.Write("In default CanUse");
                        return true;
                    };
                } else {
                    CanUse = c;
                }
                if (u == null) {
                    Use = async () => {
                        if (World.Debug) { ff14bot.Helpers.Logging.Write("In {0}'s Use.", Name); }
                        await Coroutine.Wait(5000, () => CommonBehaviors.IsLoading);
                        await Coroutine.Wait(5000, () => !CommonBehaviors.IsLoading);
                        await Coroutine.Sleep(TravelToTag.ZONE_DELAY);
                    };
                } else {
                    Use = u;
                }
            }

            public async Task<bool> Utilize() {
                //if (World.Debug) { ff14bot.Helpers.Logging.Write("In {0}'s Utilize.", Name); }
                if (!CanUse()) {
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("In {0}'s Utilize. -- Can't Utilize.", Name); }
                    return false;
                }

                if (SourceLocation != NullVector3) {
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("In {0}'s Utilize. -- Navigating to {1} with InteractDistance {2}.", Name, SourceLocation, InteractDistance); }
                    await NavigateTo(SourceLocation, InteractDistance);
                    if (World.Debug) { ff14bot.Helpers.Logging.Write("In {0}'s Utilize. -- Arrived at {1} with InteractDistance {2}.", Name, SourceLocation, InteractDistance); }
                }
                await Use();
                return true;
            }
        }
    }
}