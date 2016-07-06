using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using System.Windows.Media;
using Action = TreeSharp.Action;
using NeoGaia.ConnectionHandler;
using System.IO;
using Buddy.Coroutines;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Travel")]

    public class TravelTag : ProfileBehavior
    {
        private bool _done;
        uint localindex = 0;

        [DefaultValue("128")]
        [XmlAttribute("To")]
        public string Destination { get; set; }


        private bool _prio = true;
        private uint Present_NPC;
        public override bool IsDone { get { return _done; } }
        public override bool HighPriority { get { return _prio; } }
        public bool IsCompleted = false;


        private readonly HashSet<uint> SupportedNPC = new HashSet<uint>()
        {
            1003583,
            1003597,
            1000106,
            1003611,
            1002695,
            1004433,
            1004339,
            1001834,
            1003540,
            1000541,
            1004037,
            1000868,
            1001263,
            1002039,
            1003587,
            1003585,
            1005238,
            1012149,
            1011224,
            1012331,
            1011211,
            1011949,
            2005371,
            1011946,
            1011212,
            2005370,
            1012153,
            2005372,
            1003588,
            1003589,
            1003586,
        };


        Vector3 Position = new Vector3("0,0,0");
        private int currentstep = 0;
        private bool stepcomplete = false;
        private int Distance = 3;
        private List<AreaInfo> wayhome = new List<AreaInfo>();
        private float DistanceToTarget = 999;

        #region composites

        private Composite HandleDone
        {
            get
            {
                return new Decorator(ret => currentstep >= wayhome.Count(),
                    new Sequence(
                        new Action(r =>
                        {
                            _done = true;
                            Log("We reached the end of our path");
                        }),
                        new ActionAlwaysSucceed()
                        )
                    );
            }
        }
        private Composite HandleSelectIconString
        {
            get
            {
                return new Decorator(ret => SelectIconString.IsOpen,
                    new Sequence(
                        new Action(r =>
                        {
                            Log("Inside SelctIconstring");
                            SelectIconString.ClickSlot(localindex);
                        }

                            ),
                        new Sleep(2, 4)
                        ));
            }
        }

        private Composite HandleSelectString
        {
            get
            {
                return new Decorator(ret => SelectString.IsOpen,
                     new Sequence(
                    new Action(r =>
                    {
                        Log("Inside Selctstring");
                        SelectString.ClickSlot(localindex);
                    }

                 ),
                    new Sleep(2, 4)
                ));
            }
        }

        private Composite HandleTalkDialog
        {
            get
            {
                return new Decorator(ret => Talk.DialogOpen,
                    new Sequence(
                    new Action(r =>
                    {
                        Log("Inside Talk");
                        Talk.Next();
                    }

                 ),
                    new Sleep(2, 4)
                ));
            }
        }

        private Composite HandleSelectYesno
        {
            get
            {
                return new Decorator(ret => SelectYesno.IsOpen,
                     new Sequence(
                    new Action(r =>
                    {
                        Log("Inside YesNo");
                        SelectYesno.ClickYes();
                        stepcomplete = false;

                        currentstep++;
                        Log("Increased Currentstep to {0}", currentstep);
                    }

                 ),
                    new Sleep(10, 10)
                ));
            }
        }
        #endregion

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(

                CommonBehaviors.HandleLoading,

                HandleDone,
                HandleSelectIconString,
                HandleSelectString,
                HandleTalkDialog,
                HandleSelectYesno,

               new Decorator(ret => ConditionCheck() && wayhome[currentstep].Communicationlocalindex > 0 && !Navigator.InPosition(Core.Player.Location, wayhome[currentstep].XYZ, Distance),
                 new Sequence(
                     new Action(r =>
                     {
                         MovementManager.MoveForwardStop();
                         Log("Stopping player");
                     }),
                    new Action(r =>
                    {
                        Log("Setting Communication localindex to:" + wayhome[currentstep].Communicationlocalindex);
                        localindex = (uint)wayhome[currentstep].Communicationlocalindex - 1;

                        var obj = GameObjectManager.GetObjectByNPCId(Present_NPC);
                        if (obj != null)
                        {
                            obj.Target();
                            Log("Found NPC with NpcId {0}", Present_NPC);
                            obj.Interact();
                        }


                    }),
                 new Sleep(1, 4)
            )),

                 new Decorator(ret => WorldManager.ZoneId == 139 && Core.Player.Location.X > -300.0000 && wayhome[currentstep].Name == "Upper La Noscea-->Western La Noscea",
                  //  new Decorator(ret => wayhome[currentstep].Name == "Upper La Noscea-->Western La Noscea",
                  new Sequence(
                       new ActionRunCoroutine(r => Lanosca_spezial()),
                       CommonBehaviors.MoveAndStop(ret => LanoscaPosition, 1, stopInRange: true, destinationName: "Ferry Skipper")
                                  )),

                 //new Decorator(ret => !Navigator.InPosition(Core.Player.Location, Position,Distance),
                 //    new Sequence(
                 //       //  new Action(r =>           Log("Moving player")),
                 //
                 //       new Action(r => Position = wayhome[currentstep].XYZ),
                 //       CommonBehaviors.MoveAndStop(ret => Position, Distance - 1, stopInRange: true, destinationName: "travelbot"),
                 //
                 //        new Action(r => DistanceToTarget = Core.Me.Location.Distance(Position))
                 //   //new Action(r =>            Log("Distance to Target is {0}",DistanceToTarget))
                 //   )),
                 CommonBehaviors.MoveAndStop(ret => wayhome[currentstep].XYZ, Distance - 1, stopInRange: true, destinationName: "travelbot"),

                 new Decorator(ret => !Navigator.InPosition(Core.Player.Location, wayhome[currentstep].XYZ, Distance) && wayhome[currentstep].Communicationlocalindex < 0,
                       new Sequence(

                        new Action(r => MovementManager.MoveForwardStart()),
                        new Sleep(1000),
                         new Action(r => MovementManager.MoveForwardStop()),
                        new Sleep(5000),
                         new Action(r =>
                         {
                             stepcomplete = true;
                             currentstep++;
                             Log("Increased Currentstep {0}", currentstep);
                         }))),

                new ActionAlwaysSucceed()
             );
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }

        private static Vector3 LanoscaPosition = new Vector3(219.4604f, -0.9591975f, 258.5569f);

        public async Task<bool> Lanosca_spezial()
        {

            //Log("We trying to communicate with Ferry skipper ");
            //in this case we need to take the ferry

            //This should really be an npcid check.
            var ferrySkipper = GameObjectManager.GameObjects.FirstOrDefault(r => r.EnglishName == "Ferry Skipper");
            if (ferrySkipper != null)
            {
                ferrySkipper.Target();
                ferrySkipper.Interact();
                //Thread.Sleep should NOT be used, need to make this function async and add awaits
                //Thread.Sleep(1000);
                await Coroutine.Sleep(1000);
                Talk.Next();
                //Thread.Sleep(1000);
                await Coroutine.Sleep(1000);
                SelectYesno.ClickYes();
                //Thread.Sleep(1000); ;
                await Coroutine.Sleep(1000);
                return true;
            }


            return false;
        }


        private string GetPathStart(Dictionary<string, AreaInfo> _areas)
        {

            string special = WorldManager.ZoneId + "_";
            //Should not be using a dictionary if you have to iterate ALL the items.
            var possiblepositions = _areas.Keys.Where(x => x.StartsWith(special)).ToList();
            if (possiblepositions.Count > 0)
            {
                Log("found {0} possible Starting Locations", possiblepositions.Count);
                //These requests should be combined into one query
                foreach (string s in possiblepositions)
                {
                    Log("Possible  starting path is {0}", s);
                    AreaInfo value = _areas[s];
                    Log("Trying to Navigate to {0}", value.XYZ);

                    var target = new CanFullyNavigateTarget { Position = value.XYZ };
                    Log("Possible  target  is {0}", target.Position);

                    var result = Navigator.NavigationProvider.CanFullyNavigateToAsync(new[] { target }).Result;
                    //var result = Navigator.NavigationProvider.CanFullyNavigateToAsync(new[] { target }).Wait();
                    //var result = task.WaitAndUnwrapException();

                    var targetResult = result != null ? result.FirstOrDefault() : null;
                    Log("Can Navigate  Result {0}", targetResult.CanNavigate);
                    if (targetResult.CanNavigate == 1)
                    {
                        Log("Found my way {0}", s.Split(new Char[] { '-' }).FirstOrDefault());

                        return s.Split(new Char[] { '-' }).FirstOrDefault();
                    }
                    Thread.Sleep(5000);
                }
                return "";
            }
            else
            {
                return WorldManager.ZoneId.ToString();
            }
        }


        protected override void OnStart()
        {

            pathing a = new pathing();

            var start = GetPathStart(a.getAreas());
            if (start == "")
            {
                Log("Could not find a path Stopping. Last try CurrentzoneId");
                start = WorldManager.ZoneId.ToString();
                _done = true;
                //Should probably be a return statment here
            }

            a.setStart(start);
            a.setEnd(Destination);
            if (WorldManager.ZoneId.ToString() == Destination)
            {
                Log("We are already in the right Zone");
                Log("Lets fix it:" + currentstep + "  " + wayhome.Count());
                _done = true;
                //Should probably be a return statment here
            }


            a.Calculate();
            wayhome = a.GetPath();

            foreach (var item in wayhome)
            {
                Log("{0}", item);
            }


        }

        protected override void OnDone()
        {
            Navigator.PlayerMover.MoveStop();
        }

        public bool ConditionCheck()
        {
            int found = 0;
            var units = GameObjectManager.GameObjects;
            foreach (var unit in units.OrderBy(r => r.Distance()))
            {
                if (found > 0) break;
                foreach (var npcid in SupportedNPC)
                {
                    // Log("name=" + name + " Gameobject=" + unit.EnglishName);
                    if (npcid == unit.NpcId && found == 0)
                    {
                        Present_NPC = npcid;
                        found++;
                    }
                }
            }

            if (found > 0) { return true; } else { return false; }
        }
    }



    //-------------------------------------------------

    class Graph
    {
        Dictionary<string, Dictionary<string, int>> vertices = new Dictionary<string, Dictionary<string, int>>();

        public void add_vertex(string name, Dictionary<string, int> edges)
        {
            vertices[name] = edges;
        }

        public List<string> shortest_path(string start, string finish)
        {
            var previous = new Dictionary<string, string>();
            var distances = new Dictionary<string, int>();
            var nodes = new List<string>();

            List<string> path = null;

            foreach (var vertex in vertices)
            {
                if (vertex.Key == start)
                {
                    distances[vertex.Key] = 0;
                }
                else
                {
                    distances[vertex.Key] = int.MaxValue;
                }

                nodes.Add(vertex.Key);
            }

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);

                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest == finish)
                {
                    path = new List<string>();
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }

                    break;
                }

                if (distances[smallest] == int.MaxValue)
                {
                    break;
                }

                foreach (var neighbor in vertices[smallest])
                {
                    var alt = distances[smallest] + neighbor.Value;

                    // Log(neighbor);
                    if (alt < distances[neighbor.Key])
                    {
                        distances[neighbor.Key] = alt;
                        previous[neighbor.Key] = smallest;
                    }
                }
            }

            return path;
        }
    }


    class pathing
    {




        string[] myarray;
        string start = "";
        string end = "";
        List<AreaInfo> pathlist = new List<AreaInfo>();






        static Dictionary<string, AreaInfo> areas = new Dictionary<string, AreaInfo>();
        static Graph graph = new Graph();

        List<AreaInfo> returnlist = new List<AreaInfo>();


        //Make this static so it only never needs to be done once
        static pathing()
        {

            // x = (?:([-0-9.]+)), y = (?:([-0-9.]+)), z = (?:([-0-9.]+))
            // XYZ = new Vector3\($1f,$2f,$3f\)

            // x = (?:([-0-9.]+)), z = (?:([-0-9.]+)), y = (?:([-0-9.]+))
            // XYZ = new Vector3\($1f,$3f,$2f\)

            // expansion
            /*looks like The Dravanian Hinterlands is similar to The Sea of Clouds, with a split zone; East side is accessible by 397 (blocked for me, may unlock after a quest?), 398 and 478, where West side is accessible only through 478 --- meaning 478 has two exits, both to 399, but one for the East side and one for the West side
            478 - 399W: < MoveTo Name = "The Dravanian Hinterlands West" XYZ = "74.39938, 205, 140.4551" >
            399W-478: <MoveTo Name="Idyllshire" XYZ="-540.4974, 155.7123, -515.0025">
            */

            // areas.Add("", new AreaInfo { x = , y = , z = , Name = "", Communicationlocalindex = -1 });

            //---- Experimental-----
            // areas.Add("401_North-419", new AreaInfo {XYZ = new Vector3(-812.0089f,-57.8775f,162.7679f), Name = " Blue Window -->The Pillars", Communicationlocalindex = 1 });
            // areas.Add("419-401_North", new AreaInfo {XYZ = new Vector3(147.3258f,-12.63491f,-12.40564f), Name = "The Pillars --> Blue Window", Communicationlocalindex = 1 });

            areas.Add("478-399_West", new AreaInfo { XYZ = new Vector3(74.39938f, 205f, 140.4551f), Name = "Idyllshire --> The Dravanian Hinterlands West", Communicationlocalindex = -1 });
            areas.Add("399_West-478", new AreaInfo { XYZ = new Vector3(-540.4974f, 155.7123f, -515.0025f), Name = "The Dravanian Hinterlands West --> Idyllshire", Communicationlocalindex = -1 });

            areas.Add("478-399_East", new AreaInfo { XYZ = new Vector3(144.5908f, 207f, 114.8838f), Name = "Idyllshire --> The Dravanian Hinterlands East", Communicationlocalindex = -1 });
            areas.Add("399_East-478", new AreaInfo { XYZ = new Vector3(-227.6785f, 106.5826f, -628.679f), Name = "Idyllshire --> The Dravanian Hinterlands East", Communicationlocalindex = -1 });

            //---------

            //   The Dravanian Hinterlands-- > Idyllshire: 399 - 478, < -227.6785, 106.5826, -628.679 >
            //Idyllshire-- > The Dravanian Hinterlands: 478 - 399, < 144.5908, 207, 114.8838 >

            areas.Add("419-402", new AreaInfo { XYZ = new Vector3(168.4442f, -14.34896f, 49.57654f), Name = "The Pillars -> Azys Lla", Communicationlocalindex = 1 });
            areas.Add("402-419", new AreaInfo { XYZ = new Vector3(-877.0629f, -184.3138f, -670.1103f), Name = "Azys Lla -> The Pillars", Communicationlocalindex = 1 });


            areas.Add("399_East-398", new AreaInfo { XYZ = new Vector3(904.6548f, 161.711f, 189.163f), Name = "The Dravanian Hinterlands --> The Dravanian Forelands", Communicationlocalindex = -1 });
            areas.Add("398-399_East", new AreaInfo { XYZ = new Vector3(-795.4093f, -122.2338f, 577.756f), Name = "The Dravanian Forelands --> The Dravanian Hinterlands", Communicationlocalindex = -1 });


            areas.Add("155-418", new AreaInfo { XYZ = new Vector3(-163.8972f, 304.1538f, -333.0587f), Name = "Coerthas Central Highlands --> Foundation", Communicationlocalindex = 1 });
            areas.Add("418-155", new AreaInfo { XYZ = new Vector3(4.592957f, -2.52555f, 149.4926f), Name = " Foundation -->Coerthas Central Highlands ", Communicationlocalindex = 1 });
            areas.Add("418-419", new AreaInfo { XYZ = new Vector3(-57.32227f, 20.69349f, -96.31832f), Name = "Foundation --> The Pillars", Communicationlocalindex = -1 });
            areas.Add("419-418", new AreaInfo { XYZ = new Vector3(-16.78843f, -13.06285f, -67.11987f), Name = "The Pillars --> Foundation", Communicationlocalindex = -1 });
            // areas.Add("418-397", new AreaInfo {XYZ = new Vector3(-163.4394f,2.15106f,-5.508545f), Name = " The Pillars--> Coerthas Western Highlands", Communicationlocalindex = -1 });
            areas.Add("419-401", new AreaInfo { XYZ = new Vector3(151.9916f, -12.55534f, -7.858459f), Name = " The Pillars --> Camp Cloudtop", Communicationlocalindex = 1 });
            //areas.Add("398-399", new AreaInfo {XYZ = new Vector3(798.7896f,-122.5395f,577.9781f), Name = " Dravanian Forelands --> The Dravanian Hinterlands", Communicationlocalindex = -1 });
            areas.Add("398-400", new AreaInfo { XYZ = new Vector3(-692.5875f, 5.001416f, -838.3893f), Name = "Dravanian Forelands --> Churning Mists", Communicationlocalindex = 1 });
            areas.Add("400-398", new AreaInfo { XYZ = new Vector3(201.5868f, -68.68091f, 709.3461f), Name = "Churning Mists --> The Dravanian Forelands", Communicationlocalindex = 1 });

            areas.Add("401-419", new AreaInfo { XYZ = new Vector3(-734.9813f, -105.0583f, 459.3728f), Name = " Camp Cloudtop -->  The Pillars", Communicationlocalindex = 1 });
            areas.Add("397-398", new AreaInfo { XYZ = new Vector3(-848.7283f, 117.683f, -655.5744f), Name = " Coerthas Western Highlands --> The Dravanian Forelands", Communicationlocalindex = -1 });
            areas.Add("398-397", new AreaInfo { XYZ = new Vector3(870.7913f, -3.649778f, 350.4391f), Name = "The Dravanian Forelands --> Coerthas Western Highlands", Communicationlocalindex = -1 });

            //ul dah lift unten nach komen
            areas.Add("130-130-1", new AreaInfo { XYZ = new Vector3(-20.59343f, 10f, -44.79702f), Name = "ul'dah- ul dah list lower", Communicationlocalindex = 1 });

            areas.Add("130-131", new AreaInfo { XYZ = new Vector3(-120.054825f, 10.031486f, -8.766253f), Name = "ul'dah-Ul dah - Steps of Thal", Communicationlocalindex = -1 });
            // lift oben nach Girandia
            areas.Add("130-1-132", new AreaInfo { XYZ = new Vector3(-22.364f, 83.199f, -4.82f), Name = "ul'dah-Ul dah - Girandia", Communicationlocalindex = 1 });
            areas.Add("130-1-128-3", new AreaInfo { XYZ = new Vector3(-22.364f, 83.199f, -4.82f), Name = "ul'dah lift -Limsa (Upper)", Communicationlocalindex = 2 });

            //Lift oben Koordinaten und fahre nachunten
            areas.Add("130-2-130", new AreaInfo { XYZ = new Vector3(-24.62714f, 81.8f, -29.91334f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });
            areas.Add("130-3-130-2", new AreaInfo { XYZ = new Vector3(-24.62714f, 81.8f, -29.91334f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = -1 });

            areas.Add("128-128-1", new AreaInfo { XYZ = new Vector3(8.763986f, 40.0003f, 15.04217f), Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = 1 });
            areas.Add("128-1-132", new AreaInfo { XYZ = new Vector3(-23.511f, 91.99f, -3.719f), Name = "Limsa (Upper)-->New Gridania", Communicationlocalindex = 2 });
            areas.Add("128-1-130-3", new AreaInfo { XYZ = new Vector3(-23.511f, 91.99f, -3.719f), Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
            areas.Add("128-2-128", new AreaInfo { XYZ = new Vector3(-8.450267f, 91.5f, -15.72492f), Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
            areas.Add("128-3-128-2", new AreaInfo { XYZ = new Vector3(-9.610059f, 91.49965f, -16.58086f), Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = -1 });

            areas.Add("130-140", new AreaInfo { XYZ = new Vector3(-180.163864f, 14.000000f, -14.053099f), Name = "ul'dah-Ul dah - Western Thanalan", Communicationlocalindex = -1 });
            areas.Add("130-141", new AreaInfo { XYZ = new Vector3(43.772938f, 3.999976f, -163.789337f), Name = "ul'dah-Ul dah -Central Thanalan", Communicationlocalindex = -1 });
            areas.Add("130-178", new AreaInfo { XYZ = new Vector3(29.635f, 7.000f, -80.346f), Name = "ul'dah-Ul dah -Ul dah - Inn", Communicationlocalindex = 1 });
            areas.Add("178-130", new AreaInfo { XYZ = new Vector3(-0.213f, -2.82f, 6.4577f), Name = "Ul dah - Inn- ul'dah-Ul dah", Communicationlocalindex = -1 });
            areas.Add("131-130", new AreaInfo { XYZ = new Vector3(64.644173f, 8.000000f, -82.809769f), Name = " Steps of Thal-ul'dah-Ul dah", Communicationlocalindex = -1 });
            areas.Add("131-141", new AreaInfo { XYZ = new Vector3(163.689270f, 3.999968f, 43.879639f), Name = " Steps of Thal-Central Thanalan", Communicationlocalindex = -1 });
            areas.Add("140-129", new AreaInfo { XYZ = new Vector3(-486.74f, 23.99f, -331.675f), Name = " Western Thanalan- Limsa (Lower)", Communicationlocalindex = 1 });
            areas.Add("140-141", new AreaInfo { XYZ = new Vector3(261.533295f, 53.309750f, -9.452567f), Name = " Western Thanalan-Central Thanalan", Communicationlocalindex = -1 });
            areas.Add("140-341", new AreaInfo { XYZ = new Vector3(316.839722f, 67.180557f, 236.260666f), Name = "Western Thanalan-Cutterscry??", Communicationlocalindex = -1 });
            areas.Add("140-130", new AreaInfo { XYZ = new Vector3(471.744293f, 96.620567f, 159.596161f), Name = "Western Thanalan-ul'dah-Ul dah", Communicationlocalindex = -1 });
            areas.Add("140-212", new AreaInfo { XYZ = new Vector3(-482.076f, 17.075f, -387.232f), Name = "Western Thanalan-Waking Sands", Communicationlocalindex = -1 });
            areas.Add("141-130", new AreaInfo { XYZ = new Vector3(-116.704605f, 18.374495f, 339.036774f), Name = "Central Thanalan-Waking Sands", Communicationlocalindex = -1 });
            areas.Add("141-131", new AreaInfo { XYZ = new Vector3(13.381968f, 18.375681f, 563.856323f), Name = "Central Thanalan-Steps of Thal", Communicationlocalindex = -1 });
            areas.Add("141-145", new AreaInfo { XYZ = new Vector3(450.149078f, -17.999840f, -179.656448f), Name = "Central Thanalan-Eastern Thanalan", Communicationlocalindex = -1 });
            areas.Add("141-146", new AreaInfo { XYZ = new Vector3(232.728973f, 2.753296f, 674.237854f), Name = "Central Thanalan-Southern Thanalan", Communicationlocalindex = -1 });
            areas.Add("141-147", new AreaInfo { XYZ = new Vector3(-27.951309f, 33.000000f, -494.383026f), Name = "Central Thanalan-Northern Thanalan", Communicationlocalindex = -1 });
            areas.Add("141-140", new AreaInfo { XYZ = new Vector3(-406.028320f, -1.327996f, 98.055061f), Name = "Central Thanalan-Western Thanalan", Communicationlocalindex = -1 });
            areas.Add("145-153", new AreaInfo { XYZ = new Vector3(369.957184f, 32.385502f, -296.594879f), Name = "Eastern Thanalan-South Shroud ", Communicationlocalindex = -1 });
            areas.Add("145-146", new AreaInfo { XYZ = new Vector3(-169.531570f, -46.311321f, 489.810608f), Name = "Eastern Thanalan-Southern Thanalan  ", Communicationlocalindex = -1 });
            areas.Add("145-141", new AreaInfo { XYZ = new Vector3(-564.688477f, -19.019501f, 340.296539f), Name = "Eastern Thanalan-Central Thanalan  ", Communicationlocalindex = -1 });
            areas.Add("146-141", new AreaInfo { XYZ = new Vector3(-427.950775f, 12.862315f, -426.944122f), Name = "Southern Thanalan- Central Thanalan ", Communicationlocalindex = -1 });
            areas.Add("146-145", new AreaInfo { XYZ = new Vector3(-28.638260f, 17.150595f, -767.939148f), Name = "Southern Thanalan-Eastern Thanalan ", Communicationlocalindex = -1 });
            areas.Add("147-141", new AreaInfo { XYZ = new Vector3(36.747425f, 6.114986f, 512.867920f), Name = "Northern Thanalan-Central Thanalan  ", Communicationlocalindex = -1 });
            areas.Add("147-156", new AreaInfo { XYZ = new Vector3(-96.634842f, 84.427101f, -415.727081f), Name = "Northern Thanalan-Mor Dhona ", Communicationlocalindex = -1 });
            areas.Add("156-147", new AreaInfo { XYZ = new Vector3(-419.502350f, -3.216816f, -116.779129f), Name = "Mor Dhona-Northern Thanalan ", Communicationlocalindex = -1 });
            areas.Add("156-155", new AreaInfo { XYZ = new Vector3(124.911911f, 31.45f, -770.00f), Name = "Mor Dhona-Coerthas ", Communicationlocalindex = -1 });
            areas.Add("156-351", new AreaInfo { XYZ = new Vector3(21.124185562134f, 21.252725601196f, -631.37310791016f), Name = "Mor Dhona-Ini ??? ", Communicationlocalindex = -1 });
            areas.Add("341-140", new AreaInfo { XYZ = new Vector3(-10.554447f, -11.076664f, -197.729034f), Name = "Cutterscry??-Western Thanalan", Communicationlocalindex = -1 });
            areas.Add("132-133", new AreaInfo { XYZ = new Vector3(100.587738f, 4.958726f, 15.518513f), Name = "Girandia-Old Gridania", Communicationlocalindex = -1 });
            areas.Add("132-148", new AreaInfo { XYZ = new Vector3(154.172577f, -12.851933f, 157.515976f), Name = "Girandia-Central Shroud", Communicationlocalindex = -1 });
            //Girandia Airplane
            areas.Add("132-130-2", new AreaInfo { XYZ = new Vector3(29.85914f, -19f, 103.573f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });

            areas.Add("132-128-2", new AreaInfo { XYZ = new Vector3(29.1025f, -19.000f, 102.408f), Name = "Girandia-Limsa (Upper)", Communicationlocalindex = 2 });
            areas.Add("132-179", new AreaInfo { XYZ = new Vector3(25.977f, -8f, 100.151f), Name = "Girandia-Limsa Lominsa - Inn", Communicationlocalindex = 1 });
            areas.Add("132-204", new AreaInfo { XYZ = new Vector3(232f, 1.90f, 45.5f), Name = "Girandia-Limsa Lominsa - Command", Communicationlocalindex = -1 });
            areas.Add("204-132", new AreaInfo { XYZ = new Vector3(0f, 1f, 9.8f), Name = "Limsa Lominsa Command- New Gridania", Communicationlocalindex = -1 });
            areas.Add("179-132", new AreaInfo { XYZ = new Vector3(-0.081f, 9.685f, 6.3329f), Name = "Limsa Lominsa Command- Limsa Lominsa - Inn", Communicationlocalindex = -1 });
            areas.Add("351-156", new AreaInfo { XYZ = new Vector3(0.044669650495052f, 2.053418636322f, 27.388998031616f), Name = "???- Limsa Lominsa - Inn--Mor Dhona", Communicationlocalindex = -1 });
            areas.Add("133-132", new AreaInfo { XYZ = new Vector3(140.014786f, 11.062968f, -21.999306f), Name = "Old Gridania-->New Gridania", Communicationlocalindex = -1 });
            areas.Add("133-152", new AreaInfo { XYZ = new Vector3(179.866f, -2.239f, -241.585f), Name = "Old Gridania-->East Shroud", Communicationlocalindex = 1 });
            areas.Add("133-154", new AreaInfo { XYZ = new Vector3(-207.365845f, 10.368533f, -95.740646f), Name = "Old Gridania-->North Shroud", Communicationlocalindex = -1 });
            areas.Add("133-205", new AreaInfo { XYZ = new Vector3(-159.5f, 4f, -4.199f), Name = "Old Gridania-->Lotus Stand", Communicationlocalindex = -1 });
            areas.Add("205-133", new AreaInfo { XYZ = new Vector3(45.799f, 7.699f, 47.400f), Name = "Lotus Stand-->Old Gridania", Communicationlocalindex = -1 });
            areas.Add("148-132", new AreaInfo { XYZ = new Vector3(127.973625f, 25.274239f, -315.603302f), Name = "Central Shroud-->New Gridania", Communicationlocalindex = -1 });
            areas.Add("148-152", new AreaInfo { XYZ = new Vector3(385.957275f, -3.278250f, -184.674515f), Name = "Central Shroud-->East Shroud", Communicationlocalindex = -1 });
            areas.Add("148-153", new AreaInfo { XYZ = new Vector3(159.596100f, -23.807894f, 550.260925f), Name = "Central Shroud-->South Shroud", Communicationlocalindex = -1 });
            areas.Add("148-154", new AreaInfo { XYZ = new Vector3(-501.604462f, 74.197563f, -355.052673f), Name = "Central Shroud-->North Shroud", Communicationlocalindex = -1 });
            areas.Add("152-133", new AreaInfo { XYZ = new Vector3(-575.629f, 8.274f, 74.9197f), Name = "East Shroud-->Old Gridania", Communicationlocalindex = 1 });
            areas.Add("152-148", new AreaInfo { XYZ = new Vector3(-515.380066f, 18.856667f, 276.312958f), Name = "East Shroud-->Central Shroud", Communicationlocalindex = -1 });
            areas.Add("152-153", new AreaInfo { XYZ = new Vector3(-161.377731f, 4.327631f, 450.613647f), Name = "East Shroud-->South Shroud", Communicationlocalindex = -1 });
            areas.Add("153-148", new AreaInfo { XYZ = new Vector3(-368.008545f, 29.841984f, -243.654633f), Name = "South Shroud-->Central Shroud", Communicationlocalindex = -1 });
            areas.Add("153-152", new AreaInfo { XYZ = new Vector3(276.864288f, 11.068289f, -259.381195f), Name = "South Shroud-->East Shroud", Communicationlocalindex = -1 });
            areas.Add("153-145", new AreaInfo { XYZ = new Vector3(-285.880859f, -0.202368f, 696.760864f), Name = "South Shroud-->Eastern Thanalan", Communicationlocalindex = -1 });
            areas.Add("154-133", new AreaInfo { XYZ = new Vector3(454.377014f, -1.377332f, 194.206085f), Name = "North Shroud-->Old Gridania", Communicationlocalindex = -1 });
            areas.Add("154-148", new AreaInfo { XYZ = new Vector3(18.537865f, -54.870895f, 531.396240f), Name = "North Shroud-->Central Shroud", Communicationlocalindex = -1 });
            areas.Add("154-155", new AreaInfo { XYZ = new Vector3(-369.235107f, -5.984657f, 189.336166f), Name = "North Shroud-->Coerthas", Communicationlocalindex = -1 });
            areas.Add("155-154", new AreaInfo { XYZ = new Vector3(9.579012f, 183.084641f, 580.486389f), Name = "Coerthas-->North Shroud", Communicationlocalindex = -1 });
            areas.Add("155-156", new AreaInfo { XYZ = new Vector3(-221.167343f, 217.634018f, 702.521790f), Name = "Coerthas-->Mor Dhona", Communicationlocalindex = -1 });
            areas.Add("128-129", new AreaInfo { XYZ = new Vector3(-5.868670f, 43.095970f, -27.703053f), Name = "Limsa (Upper)-->Limsa (Lower)", Communicationlocalindex = -1 });
            areas.Add("128-135", new AreaInfo { XYZ = new Vector3(24.692057f, 44.499928f, 180.197906f), Name = "Limsa (Upper)-->Lower La Noscea", Communicationlocalindex = -1 });

            areas.Add("128-177", new AreaInfo { XYZ = new Vector3(13.245f, 39.999f, 11.8289f), Name = "Limsa (Upper)-->Gridania - Inn", Communicationlocalindex = 1 });
            areas.Add("177-128", new AreaInfo { XYZ = new Vector3(-0.113f, 0.007f, 7.086f), Name = "Gridania - Inn-->Limsa (Upper)", Communicationlocalindex = -1 });
            areas.Add("129-128", new AreaInfo { XYZ = new Vector3(-83.549187f, 17.999935f, -25.898380f), Name = "Limsa (Lower)-->Limsa (Upper)", Communicationlocalindex = -1 });
            areas.Add("129-134", new AreaInfo { XYZ = new Vector3(63.212173f, 19.999994f, 0.221235f), Name = "Limsa (Lower)-->Middle La Noscea", Communicationlocalindex = -1 });
            areas.Add("129-140", new AreaInfo { XYZ = new Vector3(-359.895f, 8f, 41.566f), Name = "Limsa (Lower)-->Western Thanalan", Communicationlocalindex = 1 });
            areas.Add("129-138", new AreaInfo { XYZ = new Vector3(-191.834f, 1f, 210.829f), Name = "Limsa (Lower)-->Western La Noscea", Communicationlocalindex = 1 });
            areas.Add("129-137_East", new AreaInfo { XYZ = new Vector3(-190.834f, 1f, 210.829f), Name = "Limsa (Lower)-->Eastern La Noscea", Communicationlocalindex = 2 });
            areas.Add("134-129", new AreaInfo { XYZ = new Vector3(-43.422066f, 35.445602f, 153.802917f), Name = "Middle La Noscea-->Limsa (Lower)", Communicationlocalindex = -1 });
            areas.Add("134-135", new AreaInfo { XYZ = new Vector3(203.290405f, 65.182816f, 285.331512f), Name = "Middle La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
            areas.Add("134-137_West", new AreaInfo { XYZ = new Vector3(-163.673187f, 35.884563f, -734.864807f), Name = "Middle La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            areas.Add("134-138", new AreaInfo { XYZ = new Vector3(-375.221436f, 33.130100f, -603.032593f), Name = "Middle La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
            areas.Add("135-128", new AreaInfo { XYZ = new Vector3(-52.436810f, 75.830246f, 116.130196f), Name = "Lower La Noscea-->Limsa (Upper)", Communicationlocalindex = -1 });
            areas.Add("135-134", new AreaInfo { XYZ = new Vector3(230.518661f, 74.490341f, -342.391663f), Name = "Lower La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            areas.Add("135-137_East", new AreaInfo { XYZ = new Vector3(694.988586f, 79.927017f, -387.720428f), Name = "Lower La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            areas.Add("135-339", new AreaInfo { XYZ = new Vector3(598.555847f, 61.519623f, -108.400681f), Name = "Lower La Noscea-->???", Communicationlocalindex = -1 });
            // multi area test 21.74548, 34.07887, 223.4946
            areas.Add("137_West-137_East", new AreaInfo { XYZ = new Vector3(21.74548f, 34.07887f, 223.4946f), Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });
            areas.Add("137_East-137_West", new AreaInfo { XYZ = new Vector3(345.3907f, 32.77044f, 91.39402f), Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });



            areas.Add("139_East-139_West", new AreaInfo { XYZ = new Vector3(221.7828f, -0.9591975f, 258.2541f), Name = "Upper La Noscea East-->Upper La Noscea West", Communicationlocalindex = 1 });
            areas.Add("139_West-139_East", new AreaInfo { XYZ = new Vector3(-340.5905f, -1.024988f, 111.8383f), Name = "Upper La Noscea West-->Upper La Noscea East", Communicationlocalindex = 1 });



            //--------
            areas.Add("137_East-129", new AreaInfo { XYZ = new Vector3(606.901f, 11.6f, 391.991f), Name = "Eastern La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
            areas.Add("137_West-134", new AreaInfo { XYZ = new Vector3(-113.323311f, 70.324112f, 47.165649f), Name = "Eastern La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            areas.Add("137_East-135", new AreaInfo { XYZ = new Vector3(246.811844f, 56.341099f, 837.507141f), Name = "Eastern La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
            areas.Add("137_West-139_East", new AreaInfo { XYZ = new Vector3(78.965446f, 80.393074f, -119.879181f), Name = "Eastern La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
            areas.Add("138-129", new AreaInfo { XYZ = new Vector3(318.314f, -36f, 351.376f), Name = ">Western La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
            areas.Add("138-135", new AreaInfo { XYZ = new Vector3(318.314f, -36f, 351.376f), Name = ">Western La Noscea-->Lower La Noscea", Communicationlocalindex = 2 });
            areas.Add("138-134", new AreaInfo { XYZ = new Vector3(811.963623f, 49.586365f, 390.644775f), Name = ">Western La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            areas.Add("138-139_West", new AreaInfo { XYZ = new Vector3(410.657715f, 30.619648f, -10.786478f), Name = ">Western La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
            areas.Add("139_East-137_West", new AreaInfo { XYZ = new Vector3(719.070007f, 0.217405f, 214.217957f), Name = ">Upper La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            areas.Add("139_West-138", new AreaInfo { XYZ = new Vector3(-476.706177f, 1.921210f, 287.913330f), Name = "Upper La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
            areas.Add("139_West-180", new AreaInfo { XYZ = new Vector3(-344.8658f, 48.09458f, -17.46293f), Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });
            areas.Add("139_East-180", new AreaInfo { XYZ = new Vector3(286.4225f, 41.63181f, -201.1194f), Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });


            areas.Add("180-139_West", new AreaInfo { XYZ = new Vector3(-320.6279f, 51.65852f, -75.99368f), Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
            areas.Add("180-139_East", new AreaInfo { XYZ = new Vector3(240.5355f, 54.22388f, -252.5956f), Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
            areas.Add("339-135", new AreaInfo { XYZ = new Vector3(-9.653202f, 48.346123f, -169.488068f), Name = ">???-->Lower La Noscea-", Communicationlocalindex = -1 });
            areas.Add("212-140", new AreaInfo { XYZ = new Vector3(-15.132613182068f, 0f, -0.0081899836659431f), Name = "Waking Sands-->Western Thanalan", Communicationlocalindex = -1 });







            graph.add_vertex("130", new Dictionary<string, int>() { { "131", 1 }, { "130-1", 1 }, { "140", 5 }, { "141", 5 }, { "178", 1 } });
            graph.add_vertex("130-1", new Dictionary<string, int>() { { "132", 3 }, { "128-3", 3 } }); //lift Oben
            graph.add_vertex("130-2", new Dictionary<string, int>() { { "130", 1 } });//lift unten
            graph.add_vertex("130-3", new Dictionary<string, int>() { { "130-2", 1 } });//lift unte
            graph.add_vertex("128", new Dictionary<string, int>() { { "129", 1 }, { "128-1", 1 }, { "135", 5 }, { "177", 1 } });
            graph.add_vertex("128-1", new Dictionary<string, int>() { { "132", 3 }, { "130-3", 3 } }); //lift Oben
            graph.add_vertex("128-2", new Dictionary<string, int>() { { "128", 1 } });//lift unten
            graph.add_vertex("128-3", new Dictionary<string, int>() { { "128-2", 1 } });//lift unten

            graph.add_vertex("178", new Dictionary<string, int>() { { "130", 1 } });
            graph.add_vertex("131", new Dictionary<string, int>() { { "130", 1 }, { "141", 5 } });
            graph.add_vertex("140", new Dictionary<string, int>() { { "129", 3 }, { "141", 5 }, { "341", 5 }, { "130", 5 }, { "212", 5 } });
            graph.add_vertex("141", new Dictionary<string, int>() { { "130", 5 }, { "131", 1 }, { "140", 5 }, { "145", 5 }, { "146", 5 }, { "147", 5 } });
            graph.add_vertex("145", new Dictionary<string, int>() { { "153", 5 }, { "146", 5 }, { "141", 5 } });
            graph.add_vertex("146", new Dictionary<string, int>() { { "141", 5 }, { "145", 5 } });
            graph.add_vertex("147", new Dictionary<string, int>() { { "141", 5 }, { "156", 5 } });
            graph.add_vertex("156", new Dictionary<string, int>() { { "147", 5 }, { "155", 5 }, { "351", 5 } });
            graph.add_vertex("341", new Dictionary<string, int>() { { "140", 5 } });
            graph.add_vertex("132", new Dictionary<string, int>() { { "133", 1 }, { "148", 5 }, { "130-2", 3 }, { "128-2", 3 }, { "179", 1 }, { "204", 1 } });
            graph.add_vertex("204", new Dictionary<string, int>() { { "132", 5 } });
            graph.add_vertex("179", new Dictionary<string, int>() { { "132", 1 } });
            graph.add_vertex("351", new Dictionary<string, int>() { { "156", 5 } });
            graph.add_vertex("133", new Dictionary<string, int>() { { "132", 1 }, { "152", 3 }, { "154", 5 }, { "205", 1 } });
            graph.add_vertex("205", new Dictionary<string, int>() { { "133", 1 } });
            graph.add_vertex("148", new Dictionary<string, int>() { { "132", 5 }, { "152", 5 }, { "153", 5 }, { "154", 5 } });
            graph.add_vertex("152", new Dictionary<string, int>() { { "133", 3 }, { "148", 5 }, { "153", 5 } });
            graph.add_vertex("153", new Dictionary<string, int>() { { "148", 5 }, { "152", 5 }, { "145", 5 } });
            graph.add_vertex("154", new Dictionary<string, int>() { { "133", 5 }, { "148", 5 }, { "155", 5 } });
            graph.add_vertex("155", new Dictionary<string, int>() { { "154", 5 }, { "156", 5 }, { "418", 5 } });



            graph.add_vertex("177", new Dictionary<string, int>() { { "128", 1 } });

            graph.add_vertex("129", new Dictionary<string, int>() { { "128", 1 }, { "134", 5 }, { "140", 3 }, { "138", 3 }, { "137_East", 3 } });
            graph.add_vertex("134", new Dictionary<string, int>() { { "129", 5 }, { "135", 5 }, { "137_West", 5 }, { "138", 5 } });
            graph.add_vertex("135", new Dictionary<string, int>() { { "128", 5 }, { "134", 5 }, { "137_West", 5 }, { "339", 5 } });
            //----
            graph.add_vertex("137_East", new Dictionary<string, int>() { { "137_West", 1 }, { "129", 3 }, { "135", 5 } });
            graph.add_vertex("137_West", new Dictionary<string, int>() { { "137_East", 1 }, { "134", 5 }, { "139_East", 5 } });
            //---

            graph.add_vertex("138", new Dictionary<string, int>() { { "129", 3 }, { "135", 3 }, { "134", 5 }, { "139_West", 5 } });
            graph.add_vertex("139_East", new Dictionary<string, int>() { { "137_West", 5 }, { "180", 5 }, { "139_West", 1 } });
            graph.add_vertex("139_West", new Dictionary<string, int>() { { "138", 5 }, { "180", 5 }, { "139_East", 1 } });

            graph.add_vertex("180", new Dictionary<string, int>() { { "139_East", 5 }, { "139_West", 5 } });

            graph.add_vertex("339", new Dictionary<string, int>() { { "135", 5 } });
            graph.add_vertex("212", new Dictionary<string, int>() { { "140", 5 } });
            graph.add_vertex("418", new Dictionary<string, int>() { { "155", 5 }, { "419", 5 } });
            graph.add_vertex("401", new Dictionary<string, int>() { { "419", 5 } });
            // graph.add_vertex("401_North", new Dictionary<string, int>() { { "419", 5 } });
            //---- experimental
            graph.add_vertex("419", new Dictionary<string, int>() { { "401", 5 }, { "402", 5 }, { "418", 5 } });
            graph.add_vertex("402", new Dictionary<string, int>() { { "419", 5 } });
            //---------
            graph.add_vertex("397", new Dictionary<string, int>() { { "398", 5 } });
            graph.add_vertex("398", new Dictionary<string, int>() { { "397", 5 }, { "400", 5 }, { "399_East", 5 } });
            graph.add_vertex("399_East", new Dictionary<string, int>() { { "398", 5 }, { "478", 5 } });
            graph.add_vertex("399_West", new Dictionary<string, int>() { { "478", 5 } });
            graph.add_vertex("478", new Dictionary<string, int>() { { "399_West", 5 }, { "399_East", 5 } });
            graph.add_vertex("400", new Dictionary<string, int>() { { "398", 5 } });
        }

        public void setStart(string Start)
        {
            start = Start;
        }

        public void setEnd(string End)
        {
            end = End;
        }
        public Dictionary<string, AreaInfo> getAreas()
        {
            return areas;
        }
        public void Calculate()
        {
            //start = WorldManager.ZoneId;
            //Log("We are currently in Zone " + start);
            // end = Destination;
            // Log("Our Destination= " + end);

            List<string> mlist = new List<string>();
            Logging.WriteDiagnostic("Start {0} end {1}", start, end);
            mlist = graph.shortest_path(start, end);

            var size = mlist.Count;
            if (size > 0)
            {
                mlist.Add(start);
                mlist.Reverse();
                myarray = mlist.ToArray();
            }
            else
            {
                Logging.WriteDiagnostic("could not Find a valid Path");
            }
        }

        public List<AreaInfo> GetPath()
        {
            int n = 0;
            while (n < (myarray.Length - 1))
            {
                string test = myarray[n] + "-" + myarray[n + 1];
                if (areas.ContainsKey(test))
                {
                    Logging.WriteDiagnostic(test);
                    returnlist.Add(areas[test]);
                    //	Console.WriteLine(areas[test]);
                }
                else
                {
                    Logging.WriteDiagnostic("I did not find Coordinates for moving  {0}", test);
                }
                n++;
            }
            return returnlist;
        }

    }







    class AreaInfo
    {
        public Vector3 XYZ { get; set; }
        public string Name { get; set; }
        public int Communicationlocalindex { get; set; }

        public override bool Equals(object obj)
        {
            var ai = obj as AreaInfo;
            if (ReferenceEquals(ai, null))
                return false;
            return Name == ai.Name && Communicationlocalindex == ai.Communicationlocalindex;
        }



        public override string ToString()
        {
            return string.Format("{{ Name = {0}, XYZ = {1}, Communicationlocalindex = {2} }}", Name, XYZ, Communicationlocalindex);
        }
    }
}







