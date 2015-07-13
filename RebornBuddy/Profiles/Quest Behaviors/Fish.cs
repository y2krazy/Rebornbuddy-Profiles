using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
	[XmlElement("FishSpot")]
    public class FishSpot
    {
		[XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }
		
		[XmlAttribute("Heading")]
        public float Heading { get; set; }

        public FishSpot()
        {
            XYZ = Vector3.Zero;
            Heading = 0f;
        }

        public FishSpot(string xyz, float heading)
        {
            XYZ = new Vector3(xyz);
            Heading = heading;
        }
		
		public FishSpot(Vector3 xyz, float heading)
        {
            XYZ = xyz;
            Heading = heading;
        }

        public string ToString()
        {
            var ret = "[FishSpot] Location: " + XYZ + ", Heading: " + Heading;

            return ret;
        }
    }


    [XmlElement("Fish")]
    public class FishTag : ProfileBehavior
    {
        private bool _done;

        [XmlElement("FishSpots")]
        public IndexedList<FishSpot> FishSpots { get; set; }

		[DefaultValue(0)]
        [XmlAttribute("Mooch")]
        public int MoochLevel { get; set; }
		
		[DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }
		
		[XmlAttribute("Weather")]
        public string Weather { get; set; }
		
		[XmlAttribute("Stealth")]
        public bool Stealth { get; set; }

        public override bool IsDone { get { return _done; } }
		
		public Version Version { get { return new Version(1, 2, 1); } }

		private int mooch = 0;
        private int fishcount = 0;
		private int amissfish = 0;
        private int fishlimit = System.Convert.ToInt32(Clio.Common.MathEx.Random(20, 30));

        private bool spotinit = false;
		
		protected override void OnStart()
		{
			GamelogManager.MessageRecevied += ReceiveMessage;
			FishSpots.IsCyclic = true;
		}

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !ConditionCheck(),
                    new Action(r =>
                    {
                        _done = true;
                    })
                ),
				new Decorator(ret => amissfish > FishSpots.Count,
                    new Action(r =>
                    {
						Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] The fish are amiss at all of the FishSpots.");
						Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] This zone has been blacklisted, please fish somewhere else and then restart the profile.");
                        _done = true;
                    })
                ),
				new Decorator(ret => Stealth == true && !Core.Player.HasAura(47),
					new Sequence(
						new Action(r =>
						{
							Settings.CharacterSettings.Instance.UseMount = false;
							Actionmanager.DoAction(298, Core.Player);
						}),
						new Sleep(2,3)
					)
				),
				new Decorator(ret => FishingManager.State == FishingState.Reelin || FishingManager.State == FishingState.Quit || FishingManager.State == FishingState.PullPoleIn,
					new ActionAlwaysSucceed()
				),
                new Decorator(ret => Vector3.Distance(Core.Player.Location, FishSpots.CurrentOrDefault.XYZ) > 1,
					CommonBehaviors.MoveAndStop(ret => FishSpots.CurrentOrDefault.XYZ, 1, stopInRange: true)
                ),
                new Decorator(ret => Vector3.Distance(Core.Player.Location, FishSpots.CurrentOrDefault.XYZ) < 2,
                    new PrioritySelector(
						new Decorator(ret => Core.Player.IsMounted,
                            CommonBehaviors.Dismount()
                        ),
                        new Decorator(ret => fishcount >= fishlimit && Actionmanager.CanCast(299, Core.Player),
                              new Action(r => {
                                Actionmanager.DoAction(299, Core.Player);
                                ChangeFishSpot();
                                spotinit = false;
                              })
                          ),
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                ff14bot.Managers.MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !spotinit,
                            new Action(r =>
                            {
                                FaceFishSpot();
								Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Will fish for " + fishlimit + " fish before moving again.");
                                spotinit = true;
                            })
                        ),
						new Decorator(ret => Weather != null && Weather != WorldManager.CurrentWeather,
                            new Sequence(
								new Action(r =>
								{
									Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Waiting for the proper weather...");
								}),
								new Wait(36000, ret => Weather == WorldManager.CurrentWeather, new ActionAlwaysSucceed())
							)
                        ),
						new Decorator(ret => Actionmanager.CanCast(297, Core.Player) && MoochLevel != 0 && mooch < MoochLevel,
							new Sequence(
								new Action(r =>
								{
									ff14bot.Managers.FishingManager.Mooch();
									mooch++;
									if(MoochLevel > 1)
									{
										Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Mooching, this is mooch " + mooch + " of " + MoochLevel + " mooches.");
									} else {
										Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Mooching, this will be the only mooch.");
									}
								}),
								new Sleep(2,3)
							)
                        ),
                        new Decorator(ret => (FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady) && (Core.Player.CurrentGPPercent == 100f || Core.Player.CurrentGP > 600 && Actionmanager.CanCast(4102, Core.Player)),
                            new Action(r =>
                            {
                                if (Core.Player.CurrentGPPercent == 100f || Core.Player.CurrentGP > 600 && Actionmanager.CanCast(4102, Core.Player))
                                {
                                    Actionmanager.DoAction(4102, Core.Player);
                                }
                            })
                        ),
                        new Decorator(ret => FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady,
                            new Action(r =>
                            {
                                FishingManager.Cast();
								if(mooch != 0)
								{
									mooch = 0;
									Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Resetting mooch level.");
								}
                            })
                        ),
                        new Decorator(ret => FishingManager.CanHook && FishingManager.State == FishingState.Bite,
                            new Action(r =>
                            {
                                if (Core.Player.CurrentGP >= 74 && Actionmanager.CanCast(4103, Core.Player) && Core.Player.HasAura("Gathering Fortune Up"))
                                {
                                    Actionmanager.DoAction(4103, Core.Player);
                                    Logging.Write(Colors.Green, "[Fish v" + Version.ToString() +"] Powerful Hookset");
                                }
                                else
                                {
                                    FishingManager.Hook();
                                }

								amissfish = 0;
								if(mooch == 0) {
									fishcount++;
								}
								
								Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Fished " + fishcount + " of " + fishlimit + " fish at this FishSpot.");
                            })
                        )
                    )
                )
            );
        }

        public bool ConditionCheck()
        {
            var Conditional = ScriptManager.GetCondition(Condition);

            return Conditional();
        }

        public void FaceFishSpot()
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

        public void ChangeFishSpot()
        {
            FishSpots.Next();
			Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Changing FishSpots...");
            fishcount = 0;
			Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] Resetting fish count...");
            fishlimit = System.Convert.ToInt32(Clio.Common.MathEx.Random(20, 30));
            spotinit = false;
        }
		
		private void ReceiveMessage(object sender, ChatEventArgs e)
        {
            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents == "The fish sense something amiss. Perhaps it is time to try another location.")
            {
				Logging.Write(Colors.Gold, "[Fish v" + Version.ToString() +"] The fish sense something amiss!");
				amissfish++;
				Actionmanager.DoAction(299, Core.Player);
                ChangeFishSpot();
				spotinit = false;
            }
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
			spotinit = false;
			fishcount = 0;
        }

        protected override void OnDone()
        {
			Actionmanager.DoAction(299, Core.Player);
        }
    }
}