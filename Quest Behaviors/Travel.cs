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
		List<uint> SupportedNPC = new List<uint>();
		Vector3 Position = new Vector3("0,0,0");
		private int currentstep = 0;
		private bool stepcomplete = false;
		private int Distance = 3;
		private List<AreaInfo> wayhome = new List<AreaInfo>();
		private float DistanceToTarget = 999;

		protected override Composite CreateBehavior()
		{
			return new PrioritySelector(
                new Decorator(ret => currentstep >= wayhome.Count(),
                 new Sequence(
                    new Action(r =>
    												{
    													_done = true;
    													Logging.Write("We reached the end of our path");
    												}),
                   new ActionAlwaysSucceed()
            )
            ),

                new Decorator(ret => SelectIconString.IsOpen,
                    new Sequence(
                    new Action(r =>
        												{
        													Logging.Write("Inside SelctIconString");
        													SelectIconString.ClickSlot(localindex);
        												}

                ),
                    new Sleep(2, 4)
                )),

                new Decorator(ret => SelectString.IsOpen ,
                     new Sequence(
                    new Action(r =>
        												{
        													Logging.Write("Inside SelctString");
        													SelectString.ClickSlot(localindex);
        												}

                 ),
                    new Sleep(2, 4)
                )),
                new Decorator(ret => Talk.DialogOpen,
                    new Sequence(
                    new Action(r =>
        												{
        													Logging.Write("Inside Talk");
        													Talk.Next();
        												}

                 ),
                    new Sleep(2, 4)
                )),

                new Decorator(ret => SelectYesno.IsOpen,
                     new Sequence(
                    new Action(r =>
        												{
        													Logging.Write("Inside YesNog");
        													SelectYesno.ClickYes();
        													stepcomplete = false;

        													currentstep++;
        													Logging.Write("Increased Currentstep {0}", currentstep);
        												}

                 ),
                    new Sleep(10, 10)
                )),

               new Decorator(ret => ConditionCheck() && wayhome[currentstep].Communicationlocalindex > 0 && Vector3.Distance(Core.Player.Location, Position) <= Distance,
                 new Sequence(
                     new Action(r =>
                												{
                													ff14bot.Managers.MovementManager.MoveForwardStop();
                													Logging.Write("Stopping player");
                												}),
                    new Action(r =>
    												{
    													Logging.Write("Setting Communication localindex to:" + wayhome[currentstep].Communicationlocalindex);
    													localindex = (uint)wayhome[currentstep].Communicationlocalindex-1;
    													GameObjectManager.GetObjectByNPCId(Present_NPC).Target();
    													Logging.Write("Found NPC with NpcId " + Present_NPC);
    													Core.Player.CurrentTarget.Interact();
    													//  await Buddy.Coroutines.Coroutine.Sleep(1000);
    												}),
                 new Sleep(1, 4)
            )),

                 new Decorator(ret => WorldManager.ZoneId.ToString() == "139" && Core.Player.Location.X >-300.0000 && wayhome[currentstep].Name == "Upper La Noscea-->Western La Noscea",
			//  new Decorator(ret => wayhome[currentstep].Name == "Upper La Noscea-->Western La Noscea",
                  new Sequence(
                       new Action (r => Lanosca_spezial()),
                       new Action (r => Position = new Vector3("219.4604, -0.9591975, 258.5569")),
                       CommonBehaviors.MoveAndStop(ret => Position, 1, stopInRange: true, destinationName : "Ferry Skipper")
                                  )),

                 new Decorator(ret => Vector3.Distance(Core.Player.Location, Position) > Distance,
                     new Sequence(
			//  new Action(r =>           Logging.Write("Moving player")),

                        new Action(r => Position = new Vector3((float)wayhome[currentstep].x, (float)wayhome[currentstep].y, (float)wayhome[currentstep].z)),
                        CommonBehaviors.MoveAndStop(ret => Position, Distance-1, stopInRange: true, destinationName :"travelbot"),

                         new Action(r => DistanceToTarget = Core.Me.Location.Distance(Position))
						  //new Action(r =>            Logging.Write("Distance to Target is {0}",DistanceToTarget))
					)),

                 new Decorator(ret => Vector3.Distance(Core.Player.Location, Position) <= Distance && wayhome[currentstep].Communicationlocalindex < 0,
                       new Sequence(

                        new Action(r =>MovementManager.MoveForwardStart()),
                        new Sleep(1000),
                         new Action(r =>MovementManager.MoveForwardStop()),
                        new Sleep(5000),
                         new Action(r =>
                    									{
                    										stepcomplete = true;

                    										currentstep++;
                    										Logging.Write("Increased Currentstep {0}", currentstep);
                    									}))),

                new ActionAlwaysSucceed()
             );
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		public bool Lanosca_spezial()
		{
			{
				//Logging.Write("We trying to communicate with Ferry skipper ");
				//in this case we need to take the ferry
                if (GameObjectManager.GetObjectByName("Ferry Skipper") != null && Core.Player.CurrentTarget.EnglishName!= "Ferry Skipper")
				{
					GameObjectManager.GetObjectByName("Ferry Skipper").Target();
					Core.Player.CurrentTarget.Interact();
					Thread.Sleep(1000);
					Talk.Next();
					Thread.Sleep(1000);
					SelectYesno.ClickYes();
					Thread.Sleep(1000);;
				}
			}

			return false;
		}


        private string GetPathStart(Dictionary<string, AreaInfo> _areas)
        {
           
            string special = WorldManager.ZoneId.ToString() + "_";
            Vector3 location = new Vector3();
            if (Extensions.HasKeyLike(_areas, special))
            {
                var possiblepositions = Extensions.GetKeysLike(_areas, special);
                Logging.Write("found {0} possible Starting Locations", possiblepositions.Count());
                foreach (string s in possiblepositions)
                {
                    Logging.Write("Possible  starting path is {0}", s);
                    AreaInfo value = _areas[s];
                    Logging.Write("Trying to Navigate to  X{0} Y{1} Z{2}", value.x, value.y, value.z);
                    location = new Vector3((float)value.x, (float)value.y, (float)value.z);
                    var target = new CanFullyNavigateTarget { Position = location };
                    Logging.Write("Possible  target  is {0}", target.Position);

                     var result =Navigator.NavigationProvider.CanFullyNavigateToAsync(new[] { target }).Result;
                    //var result = Navigator.NavigationProvider.CanFullyNavigateToAsync(new[] { target }).Wait();
                    //var result = task.WaitAndUnwrapException();
                    
                    var targetResult = result != null ? result.FirstOrDefault() : null;
                    Logging.Write("Can Navigate  Result {0}", targetResult.CanNavigate);
                    if (targetResult.CanNavigate == 1)
                     {
                        Logging.Write("Found my way {0}", s.Split(new Char[] { '-' }).FirstOrDefault());

                        return s.Split(new Char[] {'-'}).FirstOrDefault();
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
			SupportedNPC.Add(1003583);
			SupportedNPC.Add(1003597);
			SupportedNPC.Add(1000106);
			SupportedNPC.Add(1003611);
			SupportedNPC.Add(1002695);
			SupportedNPC.Add(1004433);
			SupportedNPC.Add(1004339);
			SupportedNPC.Add(1001834);
			SupportedNPC.Add(1003540);
			SupportedNPC.Add(1000541);
			SupportedNPC.Add(1004037);
			SupportedNPC.Add(1000868);
			SupportedNPC.Add(1001263);
			SupportedNPC.Add(1002039);
			SupportedNPC.Add(1003587);
			SupportedNPC.Add(1003585);
			SupportedNPC.Add(1005238);
			SupportedNPC.Add(1012149);
			SupportedNPC.Add(1011224);
			SupportedNPC.Add(1012331);
			SupportedNPC.Add(1011211);
			SupportedNPC.Add(1011949);
			SupportedNPC.Add(2005371);
			SupportedNPC.Add(1011946);
            SupportedNPC.Add(1011212);
            SupportedNPC.Add(2005370);
            SupportedNPC.Add(1012153);
            SupportedNPC.Add(2005372);
            SupportedNPC.Add(1003588);
            SupportedNPC.Add(1003589);
            SupportedNPC.Add(1003586);

            




            pathing a = new pathing();

            var start = GetPathStart(a.getAreas());
            if (start == "")
            {
                Logging.Write("Could not find a path Stopping. Last try CurrentzoneId");
                start = WorldManager.ZoneId.ToString();
                _done = true;
            }

            a.setStart(start);
            a.setEnd(Destination);
            if (WorldManager.ZoneId.ToString() == Destination)
            {
                Logging.Write("We are already in the right Zone");
                Logging.Write("Lets fix it:" + currentstep + "  " + wayhome.Count());
                _done = true;
            }


            a.Calculate();
                   wayhome = a.GetPath();

                   foreach (var item in wayhome)
                   {
                       Logging.Write(item);
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
                           // Logging.Write("name=" + name + " Gameobject=" + unit.EnglishName);
                           if (npcid == unit.NpcId && found == 0)
                           {
                               Present_NPC = npcid;
                               found++;
                           }
                       }
                   }

                   if (found > 0) { return true; }			else { return false; }
               }
           }



           //-------------------------------------------------

class Graph
           {
               Dictionary<String, Dictionary<String, int>> vertices = new Dictionary<String, Dictionary<String, int>>();

               public void add_vertex(String name, Dictionary<String, int> edges)
               {
                   vertices[name] = edges;
               }

               public List<String> shortest_path(String start, String finish)
               {
                   var previous = new Dictionary<String, String>();
                   var distances = new Dictionary<String, int>();
                   var nodes = new List<String>();

                   List<String> path = null;

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
                           path = new List<String>();
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

                   // Logging.Write(neighbor);
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

           public static class Extensions
           {
               public static bool HasKeyLike<T>(this Dictionary<string, T> collection, string value)
               {
                   var keysLikeCount = collection.Keys.Count(x => x.Contains(value));
                   return keysLikeCount > 0;
               }

               public static List<string> GetKeysLike<T>(this Dictionary<string, T> collection, string value)
               {


                   List<string> keyList = new List<string>(collection.Keys);


                   List<string> _keys = collection.Keys.Select(x => x.ToString()).ToList();
                   //   collection.Where(x => someValues.Contains(x.Value)).Select(x => x.Key);
                   //   myDict.Keys.ToList();
                   return keyList.Where(x => x.StartsWith(value)).ToList(); 
               }



           }

           class pathing
           {




               String[] myarray;
               String start = "";
               String end = "";
               List<AreaInfo> pathlist = new List<AreaInfo>();
               Graph g = new Graph();
               Dictionary<string, AreaInfo> areas = new Dictionary<string, AreaInfo>();
               List<AreaInfo> returnlist = new List<AreaInfo>();



               public pathing()
               {
                   // expansion
                   /*looks like The Dravanian Hinterlands is similar to The Sea of Clouds, with a split zone; East side is accessible by 397 (blocked for me, may unlock after a quest?), 398 and 478, where West side is accessible only through 478 --- meaning 478 has two exits, both to 399, but one for the East side and one for the West side
                   478 - 399W: < MoveTo Name = "The Dravanian Hinterlands West" XYZ = "74.39938, 205, 140.4551" >
                   399W-478: <MoveTo Name="Idyllshire" XYZ="-540.4974, 155.7123, -515.0025">
                   */

                // areas.Add("", new AreaInfo() { x = , y = , z = , Name = "", Communicationlocalindex = -1 });

                //---- Experimental-----
                // areas.Add("401_North-419", new AreaInfo() { x = -812.0089, y = -57.8775, z = 162.7679, Name = " Blue Window -->The Pillars", Communicationlocalindex = 1 });
            // areas.Add("419-401_North", new AreaInfo() { x = 147.3258, y = -12.63491, z = -12.40564, Name = "The Pillars --> Blue Window", Communicationlocalindex = 1 });

            areas.Add("478-399_West", new AreaInfo() { x = 74.39938, y = 205, z = 140.4551, Name = "Idyllshire --> The Dravanian Hinterlands West", Communicationlocalindex = -1 });
            areas.Add("399_West-478", new AreaInfo() { x = -540.4974, y = 155.7123, z = -515.0025, Name = "The Dravanian Hinterlands West --> Idyllshire", Communicationlocalindex = -1 });

            areas.Add("478-399_East", new AreaInfo() { x = 144.5908, y = 207, z = 114.8838, Name = "Idyllshire --> The Dravanian Hinterlands East", Communicationlocalindex = -1 });
            areas.Add("399_East-478", new AreaInfo() { x = -227.6785, y = 106.5826, z = -628.679, Name = "Idyllshire --> The Dravanian Hinterlands East", Communicationlocalindex = -1 });

            //---------

         //   The Dravanian Hinterlands-- > Idyllshire: 399 - 478, < -227.6785, 106.5826, -628.679 >
   //Idyllshire-- > The Dravanian Hinterlands: 478 - 399, < 144.5908, 207, 114.8838 >

             areas.Add("419-402", new AreaInfo() { x = 168.4442, y = -14.34896, z = 49.57654, Name = "The Pillars -> Azys Lla", Communicationlocalindex = 1 });
             areas.Add("402-419", new AreaInfo() { x =-877.0629 , y =-184.3138 , z =  -670.1103 , Name = "Azys Lla -> The Pillars", Communicationlocalindex = 1 });


            areas.Add("399_East-398", new AreaInfo() { x = 904.6548, y = 161.711, z = 189.163, Name = "The Dravanian Hinterlands --> The Dravanian Forelands", Communicationlocalindex = -1 });
            areas.Add("398-399_East", new AreaInfo() { x = -795.4093, y = -122.2338, z = 577.756, Name = "The Dravanian Forelands --> The Dravanian Hinterlands", Communicationlocalindex = -1 });
            
            
            areas.Add("155-418", new AreaInfo() { x = -163.8972, y = 304.1538, z = -333.0587, Name = "Coerthas Central Highlands --> Foundation", Communicationlocalindex = 1 });
			areas.Add("418-155", new AreaInfo() { x = 4.592957, y = -2.52555, z = 149.4926, Name = " Foundation -->Coerthas Central Highlands ", Communicationlocalindex = 1 });
			areas.Add("418-419", new AreaInfo() { x =-57.32227, y = 20.69349, z =-96.31832, Name = "Foundation --> The Pillars", Communicationlocalindex = -1 });
			areas.Add("419-418", new AreaInfo() { x =-16.78843, y =-13.06285, z =-67.11987, Name = "The Pillars --> Foundation", Communicationlocalindex = -1 });
			// areas.Add("418-397", new AreaInfo() { x = -163.4394, y = 2.15106, z = -5.508545, Name = " The Pillars--> Coerthas Western Highlands", Communicationlocalindex = -1 });
			areas.Add("419-401", new AreaInfo() { x = 151.9916, y =-12.55534, z =-7.858459, Name = " The Pillars --> Camp Cloudtop", Communicationlocalindex = 1 });
			//areas.Add("398-399", new AreaInfo() { x = 798.7896, y = -122.5395, z = 577.9781, Name = " Dravanian Forelands --> The Dravanian Hinterlands", Communicationlocalindex = -1 });
			areas.Add("398-400", new AreaInfo() { x = -692.5875, y = 5.001416, z = -838.3893, Name = "Dravanian Forelands --> Churning Mists", Communicationlocalindex = 1 });
			areas.Add("400-398", new AreaInfo() { x = 201.5868, y =-68.68091, z = 709.3461, Name = "Churning Mists --> The Dravanian Forelands", Communicationlocalindex = 1 });

			areas.Add("401-419", new AreaInfo() { x = -734.9813, y = -105.0583, z = 459.3728, Name = " Camp Cloudtop -->  The Pillars", Communicationlocalindex = 1 });
			areas.Add("397-398", new AreaInfo() { x = -848.7283, y = 117.683, z =-655.5744, Name = " Coerthas Western Highlands --> The Dravanian Forelands", Communicationlocalindex = -1 });
			areas.Add("398-397", new AreaInfo() { x = 870.7913, y = -3.649778, z = 350.4391, Name = "The Dravanian Forelands --> Coerthas Western Highlands", Communicationlocalindex = -1 });

			//ul dah lift unten nach komen
            areas.Add("130-130-1", new AreaInfo() { x =-20.59343, y = 10, z = -44.79702, Name = "ul'dah- ul dah list lower", Communicationlocalindex = 1 });

			areas.Add("130-131", new AreaInfo() { x = -120.054825, z = -8.766253, y = 10.031486, Name = "ul'dah-Ul dah - Steps of Thal", Communicationlocalindex = -1 });
			// lift oben nach Girandia
            areas.Add("130-1-132", new AreaInfo() { x = -22.364, y = 83.199, z = -4.82, Name = "ul'dah-Ul dah - Girandia", Communicationlocalindex = 1 });
			areas.Add("130-1-128-3", new AreaInfo() { x = -22.364, y = 83.199, z = -4.82, Name = "ul'dah lift -Limsa (Upper)", Communicationlocalindex = 2 });

			//Lift oben Koordinaten und fahre nachunten
            areas.Add("130-2-130", new AreaInfo() { x = -24.62714, y = 81.8, z = -29.91334, Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });
			areas.Add("130-3-130-2", new AreaInfo() { x = -24.62714, y = 81.8, z = -29.91334, Name = "Girandia-->ul'dah lift", Communicationlocalindex = -1 });

			areas.Add("128-128-1", new AreaInfo() { x = 8.763986, y = 40.0003, z = 15.04217, Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = 1 });
			areas.Add("128-1-132", new AreaInfo() { x = -23.511, y = 91.99, z = -3.719, Name = "Limsa (Upper)-->New Gridania", Communicationlocalindex = 2 });
			areas.Add("128-1-130-3", new AreaInfo() { x = -23.511, y = 91.99, z = -3.719, Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
			areas.Add("128-2-128", new AreaInfo() { x = -8.450267, y = 91.5, z = -15.72492, Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
			areas.Add("128-3-128-2", new AreaInfo() { x = -9.610059, y = 91.49965, z = -16.58086, Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = -1 });

			areas.Add("130-140", new AreaInfo() { x = -180.163864, z = -14.053099, y = 14.000000, Name = "ul'dah-Ul dah - Western Thanalan", Communicationlocalindex = -1 });
			areas.Add("130-141", new AreaInfo() { x = 43.772938, z = -163.789337, y = 3.999976, Name = "ul'dah-Ul dah -Central Thanalan", Communicationlocalindex = -1 });
			areas.Add("130-178", new AreaInfo() { x = 29.635, y = 7.000, z = -80.346, Name = "ul'dah-Ul dah -Ul dah - Inn", Communicationlocalindex = 1 });
			areas.Add("178-130", new AreaInfo() { x = -0.213, y = -2.82, z = 6.4577, Name = "Ul dah - Inn- ul'dah-Ul dah", Communicationlocalindex = -1 });
			areas.Add("131-130", new AreaInfo() { x = 64.644173, z = -82.809769, y = 8.000000, Name = " Steps of Thal-ul'dah-Ul dah", Communicationlocalindex = -1 });
			areas.Add("131-141", new AreaInfo() { x = 163.689270, z = 43.879639, y = 3.999968, Name = " Steps of Thal-Central Thanalan", Communicationlocalindex = -1 });
			areas.Add("140-129", new AreaInfo() { x = -486.74, y = 23.99, z = -331.675, Name = " Western Thanalan- Limsa (Lower)", Communicationlocalindex = 1 });
			areas.Add("140-141", new AreaInfo() { x = 261.533295, z = -9.452567, y = 53.309750, Name = " Western Thanalan-Central Thanalan", Communicationlocalindex = -1 });
			areas.Add("140-341", new AreaInfo() { x = 316.839722, z = 236.260666, y = 67.180557, Name = "Western Thanalan-Cutterscry??", Communicationlocalindex = -1 });
			areas.Add("140-130", new AreaInfo() { x = 471.744293, z = 159.596161, y = 96.620567, Name = "Western Thanalan-ul'dah-Ul dah", Communicationlocalindex = -1 });
			areas.Add("140-212", new AreaInfo() { x = -482.076, z = -387.232, y = 17.075, Name = "Western Thanalan-Waking Sands", Communicationlocalindex = -1 });
			areas.Add("141-130", new AreaInfo() { x = -116.704605, z = 339.036774, y = 18.374495, Name = "Central Thanalan-Waking Sands", Communicationlocalindex = -1 });
			areas.Add("141-131", new AreaInfo() { x = 13.381968, z = 563.856323, y = 18.375681, Name = "Central Thanalan-Steps of Thal", Communicationlocalindex = -1 });
			areas.Add("141-145", new AreaInfo() { x = 450.149078, z = -179.656448, y = -17.999840, Name = "Central Thanalan-Eastern Thanalan", Communicationlocalindex = -1 });
			areas.Add("141-146", new AreaInfo() { x = 232.728973, z = 674.237854, y = 2.753296, Name = "Central Thanalan-Southern Thanalan", Communicationlocalindex = -1 });
			areas.Add("141-147", new AreaInfo() { x = -27.951309, z = -494.383026, y = 33.000000, Name = "Central Thanalan-Northern Thanalan", Communicationlocalindex = -1 });
			areas.Add("141-140", new AreaInfo() { x = -406.028320, z = 98.055061, y = -1.327996, Name = "Central Thanalan-Western Thanalan", Communicationlocalindex = -1 });
			areas.Add("145-153", new AreaInfo() { x = 369.957184, z = -296.594879, y = 32.385502, Name = "Eastern Thanalan-South Shroud ", Communicationlocalindex = -1 });
			areas.Add("145-146", new AreaInfo() { x = -169.531570, z = 489.810608, y = -46.311321, Name = "Eastern Thanalan-Southern Thanalan  ", Communicationlocalindex = -1 });
			areas.Add("145-141", new AreaInfo() { x = -564.688477, z = 340.296539, y = -19.019501, Name = "Eastern Thanalan-Central Thanalan  ", Communicationlocalindex = -1 });
			areas.Add("146-141", new AreaInfo() { x = -427.950775, z = -426.944122, y = 12.862315, Name = "Southern Thanalan- Central Thanalan ", Communicationlocalindex = -1 });
			areas.Add("146-145", new AreaInfo() { x = -28.638260, z = -767.939148, y = 17.150595, Name = "Southern Thanalan-Eastern Thanalan ", Communicationlocalindex = -1 });
			areas.Add("147-141", new AreaInfo() { x = 36.747425, z = 512.867920, y = 6.114986, Name = "Northern Thanalan-Central Thanalan  ", Communicationlocalindex = -1 });
			areas.Add("147-156", new AreaInfo() { x = -96.634842, z = -415.727081, y = 84.427101, Name = "Northern Thanalan-Mor Dhona ", Communicationlocalindex = -1 });
			areas.Add("156-147", new AreaInfo() { x = -419.502350, z = -116.779129, y = -3.216816, Name = "Mor Dhona-Northern Thanalan ", Communicationlocalindex = -1 });
			areas.Add("156-155", new AreaInfo() { x = 124.911911, z = -770.00, y = 31.45, Name = "Mor Dhona-Coerthas ", Communicationlocalindex = -1 });
			areas.Add("156-351", new AreaInfo() { x = 21.124185562134, y = 21.252725601196, z = -631.37310791016, Name = "Mor Dhona-Ini ??? ", Communicationlocalindex = -1 });
			areas.Add("341-140", new AreaInfo() { x = -10.554447, z = -197.729034, y = -11.076664, Name = "Cutterscry??-Western Thanalan", Communicationlocalindex = -1 });
			areas.Add("132-133", new AreaInfo() { x = 100.587738, z = 15.518513, y = 4.958726, Name = "Girandia-Old Gridania", Communicationlocalindex = -1 });
			areas.Add("132-148", new AreaInfo() { x = 154.172577, z = 157.515976, y = -12.851933, Name = "Girandia-Central Shroud", Communicationlocalindex = -1 });
			//Girandia Airplane
            areas.Add("132-130-2", new AreaInfo() { x = 29.85914, y = -19, z = 103.573, Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });

			areas.Add("132-128-2", new AreaInfo() { x = 29.1025, y = -19.000, z = 102.408, Name = "Girandia-Limsa (Upper)", Communicationlocalindex = 2 });
			areas.Add("132-179", new AreaInfo() { x = 25.977, y = -8, z = 100.151, Name = "Girandia-Limsa Lominsa - Inn", Communicationlocalindex = 1 });
			areas.Add("132-204", new AreaInfo() { x = 232, y = 1.90, z = 45.5, Name = "Girandia-Limsa Lominsa - Command", Communicationlocalindex = -1 });
			areas.Add("204-132", new AreaInfo() { x = 0, y = 1, z = 9.8, Name = "Limsa Lominsa Command- New Gridania", Communicationlocalindex = -1 });
			areas.Add("179-132", new AreaInfo() { x = -0.081, y = 9.685, z = 6.3329, Name = "Limsa Lominsa Command- Limsa Lominsa - Inn", Communicationlocalindex = -1 });
			areas.Add("351-156", new AreaInfo() { x = 0.044669650495052, z = 27.388998031616, y = 2.053418636322, Name = "???- Limsa Lominsa - Inn--Mor Dhona", Communicationlocalindex = -1 });
			areas.Add("133-132", new AreaInfo() { x = 140.014786, z = -21.999306, y = 11.062968, Name = "Old Gridania-->New Gridania", Communicationlocalindex = -1 });
			areas.Add("133-152", new AreaInfo() { x = 179.866, y = -2.239, z = -241.585, Name = "Old Gridania-->East Shroud", Communicationlocalindex = 1 });
			areas.Add("133-154", new AreaInfo() { x = -207.365845, z = -95.740646, y = 10.368533, Name = "Old Gridania-->North Shroud", Communicationlocalindex = -1 });
			areas.Add("133-205", new AreaInfo() { x = -159.5, y = 4, z = -4.199, Name = "Old Gridania-->Lotus Stand", Communicationlocalindex = -1 });
			areas.Add("205-133", new AreaInfo() { x = 45.799, y = 7.699, z = 47.400, Name = "Lotus Stand-->Old Gridania", Communicationlocalindex = -1 });
			areas.Add("148-132", new AreaInfo() { x = 127.973625, z = -315.603302, y = 25.274239, Name = "Central Shroud-->New Gridania", Communicationlocalindex = -1 });
			areas.Add("148-152", new AreaInfo() { x = 385.957275, z = -184.674515, y = -3.278250, Name = "Central Shroud-->East Shroud", Communicationlocalindex = -1 });
			areas.Add("148-153", new AreaInfo() { x = 159.596100, z = 550.260925, y = -23.807894, Name = "Central Shroud-->South Shroud", Communicationlocalindex = -1 });
			areas.Add("148-154", new AreaInfo() { x = -501.604462, z = -355.052673, y = 74.197563, Name = "Central Shroud-->North Shroud", Communicationlocalindex = -1 });
			areas.Add("152-133", new AreaInfo() { x = -575.629, y = 8.274, z = 74.9197, Name = "East Shroud-->Old Gridania", Communicationlocalindex = 1 });
			areas.Add("152-148", new AreaInfo() { x = -515.380066, z = 276.312958, y = 18.856667, Name = "East Shroud-->Central Shroud", Communicationlocalindex = -1 });
			areas.Add("152-153", new AreaInfo() { x = -161.377731, z = 450.613647, y = 4.327631, Name = "East Shroud-->South Shroud", Communicationlocalindex = -1 });
			areas.Add("153-148", new AreaInfo() { x = -368.008545, z = -243.654633, y = 29.841984, Name = "South Shroud-->Central Shroud", Communicationlocalindex = -1 });
			areas.Add("153-152", new AreaInfo() { x = 276.864288, z = -259.381195, y = 11.068289, Name = "South Shroud-->East Shroud", Communicationlocalindex = -1 });
			areas.Add("153-145", new AreaInfo() { x = -285.880859, z = 696.760864, y = -0.202368, Name = "South Shroud-->Eastern Thanalan", Communicationlocalindex = -1 });
			areas.Add("154-133", new AreaInfo() { x = 454.377014, z = 194.206085, y = -1.377332, Name = "North Shroud-->Old Gridania", Communicationlocalindex = -1 });
			areas.Add("154-148", new AreaInfo() { x = 18.537865, z = 531.396240, y = -54.870895, Name = "North Shroud-->Central Shroud", Communicationlocalindex = -1 });
			areas.Add("154-155", new AreaInfo() { x = -369.235107, z = 189.336166, y = -5.984657, Name = "North Shroud-->Coerthas", Communicationlocalindex = -1 });
			areas.Add("155-154", new AreaInfo() { x = 9.579012, z = 580.486389, y = 183.084641, Name = "Coerthas-->North Shroud", Communicationlocalindex = -1 });
			areas.Add("155-156", new AreaInfo() { x = -221.167343, z = 702.521790, y = 217.634018, Name = "Coerthas-->Mor Dhona", Communicationlocalindex = -1 });
			areas.Add("128-129", new AreaInfo() { x = -5.868670, z = -27.703053, y = 43.095970, Name = "Limsa (Upper)-->Limsa (Lower)", Communicationlocalindex = -1 });
			areas.Add("128-135", new AreaInfo() { x = 24.692057, z = 180.197906, y = 44.499928, Name = "Limsa (Upper)-->Lower La Noscea", Communicationlocalindex = -1 });

			areas.Add("128-177", new AreaInfo() { x = 13.245, y = 39.999, z = 11.8289, Name = "Limsa (Upper)-->Gridania - Inn", Communicationlocalindex = 1 });
			areas.Add("177-128", new AreaInfo() { x = -0.113, y = 0.007, z = 7.086, Name = "Gridania - Inn-->Limsa (Upper)", Communicationlocalindex = -1 });
			areas.Add("129-128", new AreaInfo() { x = -83.549187, z = -25.898380, y = 17.999935, Name = "Limsa (Lower)-->Limsa (Upper)", Communicationlocalindex = -1 });
			areas.Add("129-134", new AreaInfo() { x = 63.212173, z = 0.221235, y = 19.999994, Name = "Limsa (Lower)-->Middle La Noscea", Communicationlocalindex = -1 });
			areas.Add("129-140", new AreaInfo() { x = -359.895, y = 8, z = 41.566, Name = "Limsa (Lower)-->Western Thanalan", Communicationlocalindex = 1 });
			areas.Add("129-138", new AreaInfo() { x = -191.834, y = 1, z = 210.829, Name = "Limsa (Lower)-->Western La Noscea", Communicationlocalindex = 1 });
			areas.Add("129-137_East", new AreaInfo() { x = -190.834, y = 1, z = 210.829, Name = "Limsa (Lower)-->Eastern La Noscea", Communicationlocalindex = 2 });
			areas.Add("134-129", new AreaInfo() { x = -43.422066, z = 153.802917, y = 35.445602, Name = "Middle La Noscea-->Limsa (Lower)", Communicationlocalindex = -1 });
			areas.Add("134-135", new AreaInfo() { x = 203.290405, z = 285.331512, y = 65.182816, Name = "Middle La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
			areas.Add("134-137_West", new AreaInfo() { x = -163.673187, z = -734.864807, y = 35.884563, Name = "Middle La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
			areas.Add("134-138", new AreaInfo() { x = -375.221436, z = -603.032593, y = 33.130100, Name = "Middle La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
			areas.Add("135-128", new AreaInfo() { x = -52.436810, z = 116.130196, y = 75.830246, Name = "Lower La Noscea-->Limsa (Upper)", Communicationlocalindex = -1 });
			areas.Add("135-134", new AreaInfo() { x = 230.518661, z = -342.391663, y = 74.490341, Name = "Lower La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
			areas.Add("135-137_East", new AreaInfo() { x = 694.988586, z = -387.720428, y = 79.927017, Name = "Lower La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
			areas.Add("135-339", new AreaInfo() { x = 598.555847, z = -108.400681, y = 61.519623, Name = "Lower La Noscea-->???", Communicationlocalindex = -1 });
            // multi area test 21.74548, 34.07887, 223.4946
            areas.Add("137_West-137_East", new AreaInfo() { x = 21.74548, y = 34.07887, z = 223.4946, Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });
            areas.Add("137_East-137_West", new AreaInfo() { x = 345.3907, y = 32.77044, z = 91.39402, Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });

            

            areas.Add("139_East-139_West", new AreaInfo() { x = 221.7828, y = -0.9591975, z = 258.2541, Name = "Upper La Noscea East-->Upper La Noscea West", Communicationlocalindex = 1 });
            areas.Add("139_West-139_East", new AreaInfo() { x = -340.5905, y = -1.024988, z = 111.8383, Name = "Upper La Noscea West-->Upper La Noscea East", Communicationlocalindex = 1 });


            
            //--------
            areas.Add("137_East-129", new AreaInfo() { x = 606.901, y = 11.6, z = 391.991, Name = "Eastern La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
			areas.Add("137_West-134", new AreaInfo() { x = -113.323311, z = 47.165649, y = 70.324112, Name = "Eastern La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
			areas.Add("137_East-135", new AreaInfo() { x = 246.811844, z = 837.507141, y = 56.341099, Name = "Eastern La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
			areas.Add("137_West-139_East", new AreaInfo() { x = 78.965446, z = -119.879181, y = 80.393074, Name = "Eastern La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
			areas.Add("138-129", new AreaInfo() { x = 318.314, y = -36, z = 351.376, Name = ">Western La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
			areas.Add("138-135", new AreaInfo() { x = 318.314, y = -36, z = 351.376, Name = ">Western La Noscea-->Lower La Noscea", Communicationlocalindex = 2 });
			areas.Add("138-134", new AreaInfo() { x = 811.963623, z = 390.644775, y = 49.586365, Name = ">Western La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
			areas.Add("138-139_West", new AreaInfo() { x = 410.657715, z = -10.786478, y = 30.619648, Name = ">Western La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
			areas.Add("139_East-137_West", new AreaInfo() { x = 719.070007, z = 214.217957, y = 0.217405, Name = ">Upper La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
			areas.Add("139_West-138", new AreaInfo() { x = -476.706177, z = 287.913330, y = 1.921210, Name = "Upper La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
			areas.Add("139_West-180", new AreaInfo() { x = -344.8658, y = 48.09458, z = -17.46293, Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });
            areas.Add("139_East-180", new AreaInfo() { x = 286.4225, y = 41.63181, z = -201.1194, Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });

     
            areas.Add("180-139_West", new AreaInfo() { x = -320.6279, y = 51.65852, z = -75.99368, Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
            areas.Add("180-139_East", new AreaInfo() { x = 240.5355, y = 54.22388, z = -252.5956, Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
			areas.Add("339-135", new AreaInfo() { x = -9.653202, z = -169.488068, y = 48.346123, Name = ">???-->Lower La Noscea-", Communicationlocalindex = -1 });
			areas.Add("212-140", new AreaInfo() { x = -15.132613182068, z = -0.0081899836659431, y = 0, Name = "Waking Sands-->Western Thanalan", Communicationlocalindex = -1 });







			g.add_vertex("130", new Dictionary<String, int>() { { "131", 1 }, { "130-1", 1 }, { "140", 5 }, { "141", 5 }, { "178", 1 } });
			g.add_vertex("130-1", new Dictionary<String, int>() { { "132", 3 }, { "128-3", 3 } }); //lift Oben
            g.add_vertex("130-2", new Dictionary<String, int>() { { "130", 1 } });//lift unten
            g.add_vertex("130-3", new Dictionary<String, int>() { { "130-2", 1 } });//lift unte
            g.add_vertex("128", new Dictionary<String, int>() { { "129", 1 }, { "128-1", 1 }, { "135", 5 }, { "177", 1 } });
			g.add_vertex("128-1", new Dictionary<String, int>() { { "132", 3 }, { "130-3", 3 } }); //lift Oben
            g.add_vertex("128-2", new Dictionary<String, int>() { { "128", 1 } });//lift unten
            g.add_vertex("128-3", new Dictionary<String, int>() { { "128-2", 1 } });//lift unten

            g.add_vertex("178", new Dictionary<String, int>() { { "130", 1 } });
			g.add_vertex("131", new Dictionary<String, int>() { { "130", 1 }, { "141", 5 } });
			g.add_vertex("140", new Dictionary<String, int>() { { "129", 3 }, { "141", 5 }, { "341", 5 }, { "130", 5 }, { "212", 5 } });
			g.add_vertex("141", new Dictionary<String, int>() { { "130", 5 }, { "131", 1 }, { "140", 5 }, { "145", 5 }, { "146", 5 }, { "147", 5 } });
			g.add_vertex("145", new Dictionary<String, int>() { { "153", 5 }, { "146", 5 }, { "141", 5 } });
			g.add_vertex("146", new Dictionary<String, int>() { { "141", 5 }, { "145", 5 } });
			g.add_vertex("147", new Dictionary<String, int>() { { "141", 5 }, { "156", 5 } });
			g.add_vertex("156", new Dictionary<String, int>() { { "147", 5 }, { "155", 5 }, { "351", 5 } });
			g.add_vertex("341", new Dictionary<String, int>() { { "140", 5 } });
			g.add_vertex("132", new Dictionary<String, int>() { { "133", 1 }, { "148", 5 }, { "130-2", 3 }, { "128-2", 3 }, { "179", 1 }, { "204", 1 } });
			g.add_vertex("204", new Dictionary<String, int>() { { "132", 5 } });
			g.add_vertex("179", new Dictionary<String, int>() { { "132", 1 } });
			g.add_vertex("351", new Dictionary<String, int>() { { "156", 5 } });
			g.add_vertex("133", new Dictionary<String, int>() { { "132", 1 }, { "152", 3 }, { "154", 5 }, { "205", 1 } });
			g.add_vertex("205", new Dictionary<String, int>() { { "133", 1 } });
			g.add_vertex("148", new Dictionary<String, int>() { { "132", 5 }, { "152", 5 }, { "153", 5 }, { "154", 5 } });
			g.add_vertex("152", new Dictionary<String, int>() { { "133", 3 }, { "148", 5 }, { "153", 5 } });
			g.add_vertex("153", new Dictionary<String, int>() { { "148", 5 }, { "152", 5 }, { "145", 5 } });
			g.add_vertex("154", new Dictionary<String, int>() { { "133", 5 }, { "148", 5 }, { "155", 5 } });
			g.add_vertex("155", new Dictionary<String, int>() { { "154", 5 }, { "156", 5 }, { "418", 5 } });

          

            g.add_vertex("177", new Dictionary<String, int>() { { "128", 1 } });

			g.add_vertex("129", new Dictionary<String, int>() { { "128", 1 }, { "134", 5 }, { "140", 3 }, { "138", 3 }, { "137_East", 3 } });
			g.add_vertex("134", new Dictionary<String, int>() { { "129", 5 }, { "135", 5 }, { "137_West", 5 }, { "138", 5 } });
			g.add_vertex("135", new Dictionary<String, int>() { { "128", 5 }, { "134", 5 }, { "137_West", 5 }, { "339", 5 } });
            //----
            g.add_vertex("137_East", new Dictionary<String, int>() { { "137_West", 1 }, { "129", 3 },{ "135", 5 } });
            g.add_vertex("137_West", new Dictionary<String, int>() { { "137_East", 1 }, { "134", 5 }, { "139_East", 5 } });
            //---
           
			g.add_vertex("138", new Dictionary<String, int>() { { "129", 3 }, { "135", 3 }, { "134", 5 }, { "139_West", 5 } });
			g.add_vertex("139_East", new Dictionary<String, int>() { { "137_West", 5 },  { "180", 5 }, { "139_West", 1 } });
            g.add_vertex("139_West", new Dictionary<String, int>() { { "138", 5 }, { "180", 5 }, { "139_East", 1 } });

            g.add_vertex("180", new Dictionary<String, int>() { { "139_East", 5 } , { "139_West", 5 } });

			g.add_vertex("339", new Dictionary<String, int>() { { "135", 5 } });
			g.add_vertex("212", new Dictionary<String, int>() { { "140", 5 } });
			g.add_vertex("418", new Dictionary<String, int>() { { "155", 5 }, { "419", 5 } });
			g.add_vertex("401", new Dictionary<String, int>() { { "419", 5 }  });
            // g.add_vertex("401_North", new Dictionary<String, int>() { { "419", 5 } });
            //---- experimental
            g.add_vertex("419", new Dictionary<String, int>() { { "401", 5 },{ "402", 5 }, { "418", 5 } });
            g.add_vertex("402", new Dictionary<String, int>() { { "419", 5 } });
            //---------
            g.add_vertex("397", new Dictionary<String, int>() { { "398", 5 } });
			g.add_vertex("398", new Dictionary<String, int>() { { "397", 5 }, { "400", 5 }, {"399_East", 5 } });
            g.add_vertex("399_East", new Dictionary<String, int>() { { "398", 5 }, {"478", 5 } });
            g.add_vertex("399_West", new Dictionary<String, int>() { { "478", 5 } });
            g.add_vertex("478", new Dictionary<String, int>() { { "399_West", 5 }, {"399_East", 5 } });
            g.add_vertex("400", new Dictionary<String, int>() { { "398", 5 } });
		}

		public void setStart(String Start)
		{
			start = Start;
		}

		public void setEnd(String End)
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
			//Logging.Write("We are currently in Zone " + start);
			// end = Destination;
			// Logging.Write("Our Destination= " + end);

			List<String> mlist = new List<String>();
            Logging.Write("STart {0} end {1}", start, end);
            mlist = g.shortest_path(start, end);
           
            var size = mlist.Count;
			if (size > 0)
			{
				mlist.Add(start);
				mlist.Reverse();
				myarray = mlist.ToArray();
			}
			else
			{ Logging.Write("cloud not Find a valid Path"); }
		}

		public List<AreaInfo> GetPath()
		{
			int n = 0;
			while (n < (myarray.Length - 1))
			{
				String test = myarray[n] + "-" + myarray[n + 1];
				if (areas.ContainsKey(test))
				{
					Logging.Write(test);
					returnlist.Add(areas[test]);
				//	Console.WriteLine(areas[test]);
				}
				else
                { Logging.Write("I did not find Coordinates for moving  {0}", test); }
                n++;
            }
            return returnlist;
        }

    }







    class AreaInfo
    {
        public Double x { get; set; }
        public Double y { get; set; }
        public Double z { get; set; }

        public string Name { get; set; }
        public int Communicationlocalindex { get; set; }

        public override bool Equals(object obj)
        {
            var ai = obj as AreaInfo;
            if (object.ReferenceEquals(ai, null))
                return false;
            return Name == ai.Name && x == ai.x && Communicationlocalindex == ai.Communicationlocalindex;
        }



        public override string ToString()
        {
            return string.Format("{{ Name = {0}, x = {1},y = {2},z = {3}, Communicationlocalindex = {4} }}", Name, x, y, z, Communicationlocalindex);
        }
    }
}








