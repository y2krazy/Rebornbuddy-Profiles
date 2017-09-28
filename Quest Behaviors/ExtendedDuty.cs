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
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{	
    [XmlElement("ExtendedDuty")]

    class ExtendedDutyTag : SimpleDutyTag
    {
		public bool didthething = false;
		
		public void ResetThing(object sender, EventArgs e)
		{
			didthething = false;
		}
		
		protected override void OnStart()
		{
			ff14bot.NeoProfiles.GameEvents.OnPlayerDied += ResetThing;
			base.OnStart();
		}
		
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
				CommonBehaviors.HandleLoading,
				new Decorator(ret => QuestLogManager.InCutscene,
					new ActionAlwaysSucceed()
				),
				new Decorator(ret => QuestId == 65680 && !Core.Player.InCombat && ((GameObjectManager.GetObjectByNPCId(2004819) != null && GameObjectManager.GetObjectByNPCId(2004819).IsVisible) || (GameObjectManager.GetObjectByNPCId(2004821) != null && GameObjectManager.GetObjectByNPCId(2004821).IsVisible) || (GameObjectManager.GetObjectByNPCId(2004824) != null && GameObjectManager.GetObjectByNPCId(2004824).IsVisible) || (GameObjectManager.GetObjectByNPCId(2004823) != null && GameObjectManager.GetObjectByNPCId(2004823).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2004819) != null && GameObjectManager.GetObjectByNPCId(2004819).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2004819).Location) <= 2,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Bindings!");
										GameObjectManager.GetObjectByNPCId(2004819).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2004819).Location, 2)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2004821) != null && GameObjectManager.GetObjectByNPCId(2004821).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2004821).Location) <= 2,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Bindings!");
										GameObjectManager.GetObjectByNPCId(2004821).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2004821).Location, 2)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2004824) != null && GameObjectManager.GetObjectByNPCId(2004824).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2004824).Location) <= 2,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Bindings!");
										GameObjectManager.GetObjectByNPCId(2004824).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2004824).Location, 2)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2004823) != null && GameObjectManager.GetObjectByNPCId(2004823).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2004823).Location) <= 2,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Bindings!");
										GameObjectManager.GetObjectByNPCId(2004823).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2004823).Location, 2)
							)
						)
					)
                ),
                // new Decorator(ret => QuestId == 65753 && !Core.Player.InCombat && Vector3.Distance(Core.Player.Location, new Vector3(213.2676f, -36.40339f, 309.0858f)) <= 3,
                    // new PrioritySelector(
						// new Decorator(ret => !Core.Me.HasAura(614) && Vector3.Distance(Core.Player.Location, new Vector3(213.2676f, -36.40339f, 309.0858f)) <= 3,
							// new Action(r =>
							// {
								// Logging.Write("[ExtendedDuty] Using Hide on me!");
								// ActionManager.DoAction(2245, Core.Me);
							// })
						// ),
						// CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(3165).Location, 3)
                    // )
                // ),
				new Decorator(ret => QuestId == 65801 && GameObjectManager.GetObjectByNPCId(1004142) != null && !GameObjectManager.GetObjectByNPCId(1004142).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1004142) != null && GameObjectManager.GetObjectByNPCId(1004142).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1004142).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Handing over items to the Order of the Nald'thal Priest!");
										ff14bot.Managers.GameObjectManager.GetObjectByNPCId(1004142).Interact();
										Thread.Sleep(2000);
										ff14bot.RemoteWindows.Talk.Next();
										Thread.Sleep(5000);
										foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
										{
											if(slot.RawItemId == 2000404)
											{
												slot.Handover();
											}
										}
										Thread.Sleep(2000);
										foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
										{
											if(slot.RawItemId == 2000411)
											{
												slot.Handover();
											}
										}
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.Request.IsOpen)
											ff14bot.RemoteWindows.Request.HandOver();
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.SelectYesno.IsOpen)
											ff14bot.RemoteWindows.SelectYesno.ClickYes();
										Thread.Sleep(2000);
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1004142).Location, 3)
							)
						)
					)
                ),
				new Decorator(ret => QuestId == 65853 && GameObjectManager.GetObjectByNPCId(1002684) != null && !GameObjectManager.GetObjectByNPCId(1002684).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1002684) != null && GameObjectManager.GetObjectByNPCId(1002684).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1002684).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Handing over items to Solkwyb!");
										ff14bot.Managers.GameObjectManager.GetObjectByNPCId(1002684).Interact();
										Thread.Sleep(2000);
										ff14bot.RemoteWindows.Talk.Next();
										Thread.Sleep(5000);
										foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
										{
											if(slot.RawItemId == 2000525)
											{
												slot.Handover();
											}
										}
										Thread.Sleep(2000);
										foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
										{
											if(slot.RawItemId == 2000526)
											{
												slot.Handover();
											}
										}
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.Request.IsOpen)
											ff14bot.RemoteWindows.Request.HandOver();
										Thread.Sleep(2000);
										ff14bot.RemoteWindows.Talk.Next();
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.SelectYesno.IsOpen)
											ff14bot.RemoteWindows.SelectYesno.ClickYes();
										Thread.Sleep(2000);
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1002684).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 65886 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2001471) != null && GameObjectManager.GetObjectByNPCId(2001471).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2001471).Location) <= 3,
                            new Action(r =>
                            {
                                GameObjectManager.GetObjectByNPCId(2001471).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2001471).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 65995 && GameObjectManager.GetObjectByNPCId(2002521) != null && GameObjectManager.GetObjectByNPCId(2002521).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002521).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with K'lyhia's Grimoire!");
                                GameObjectManager.GetObjectByNPCId(2002521).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002521).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 66057 && ((GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible) || (GameObjectManager.GetObjectByNPCId(2002427) != null && GameObjectManager.GetObjectByNPCId(2002427).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002428).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Rope Bond!");
										GameObjectManager.GetObjectByNPCId(2002428).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002428).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2002427) != null && GameObjectManager.GetObjectByNPCId(2002427).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002427).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Rope Bond!");
										GameObjectManager.GetObjectByNPCId(2002427).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002427).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 66448 && GameObjectManager.GetObjectByNPCId(2002279) != null && GameObjectManager.GetObjectByNPCId(2002279).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002279).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Draconian Rosary!");
                                GameObjectManager.GetObjectByNPCId(2002279).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002279).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 66540 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
                    new PrioritySelector(
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !didthething,
                            new Action(r =>
                            {
                                var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
								foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
								{
									if (slot.RawItemId == 2000771)
									{
										Logging.Write("[ExtendedDuty] Using the Imperial Smoke Signal on Destination!");
										slot.UseItem(targetnpc);
										didthething = true;
									}
								}
                            })
                        )
                    )
                ),
                new Decorator(ret => QuestId == 66596 && GameObjectManager.GetObjectByNPCId(2002296) != null && GameObjectManager.GetObjectByNPCId(2002296).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002296).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Glowing Grimoire!");
                                GameObjectManager.GetObjectByNPCId(2002296).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002296).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 66597 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
                    new PrioritySelector(
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !didthething,
                            new Action(r =>
                            {
                                var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
								foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
								{
									if (slot.RawItemId == 2000951)
									{
										Logging.Write("[ExtendedDuty] Using Aetherometer on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										Logging.Write("[ExtendedDuty] Using Aetherometer on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										didthething = true;
									}
								}
                            })
                        )
                    )
                ),
				new Decorator(ret => QuestId == 66624 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
                    new PrioritySelector(
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !didthething,
                            new Action(r =>
                            {
                                var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
								foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
								{
									if (slot.RawItemId == 2000952)
									{
										Logging.Write("[ExtendedDuty] Using Nymeia Lilies on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										Logging.Write("[ExtendedDuty] Using Nymeia Lilies on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										didthething = true;
									}
								}
                            })
                        )
                    )
                ),
				new Decorator(ret => QuestId == 66626 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
                    new PrioritySelector(
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !didthething,
                            new Action(r =>
                            {
                                var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
								foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
								{
									if (slot.RawItemId == 2000953)
									{
										Logging.Write("[ExtendedDuty] Using Nymeia Lilies on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										Logging.Write("[ExtendedDuty] Using Nymeia Lilies on Destination!");
										slot.UseItem(targetnpc);
										Thread.Sleep(5000);
										didthething = true;
									}
								}
                            })
                        )
                    )
                ),
                new Decorator(ret => QuestId == 66633 && GameObjectManager.GetObjectByNPCId(2002522) != null && GameObjectManager.GetObjectByNPCId(2002522).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002522).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Stolen Crate!");
                                GameObjectManager.GetObjectByNPCId(2002522).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002522).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 66638 && GameObjectManager.GetObjectByNPCId(1650) != null && !GameObjectManager.GetObjectByNPCId(1650).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1650) != null && GameObjectManager.GetObjectByNPCId(1650).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1650).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Using Physick on Tonberry Wanderer!");
										ActionManager.DoAction(190, GameObjectManager.GetObjectByNPCId(1650));
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1650).Location, 3)
							)
						)
					)
                ),
				new Decorator(ret => QuestId == 67212 && ((GameObjectManager.GetObjectByNPCId(2005846) != null && GameObjectManager.GetObjectByNPCId(2005846).IsVisible) || (GameObjectManager.GetObjectByNPCId(2005845) != null && GameObjectManager.GetObjectByNPCId(2005845).IsVisible) || (GameObjectManager.GetObjectByNPCId(2005844) != null && GameObjectManager.GetObjectByNPCId(2005844).IsVisible) || (GameObjectManager.GetObjectByNPCId(2005843) != null && GameObjectManager.GetObjectByNPCId(2005843).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005846) != null && GameObjectManager.GetObjectByNPCId(2005846).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005846).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2005846!");
										GameObjectManager.GetObjectByNPCId(2005846).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005846).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005845) != null && GameObjectManager.GetObjectByNPCId(2005845).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005845).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2005845!");
										GameObjectManager.GetObjectByNPCId(2005845).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005845).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005844) != null && GameObjectManager.GetObjectByNPCId(2005844).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005844).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2005844!");
										GameObjectManager.GetObjectByNPCId(2005844).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005844).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005843) != null && GameObjectManager.GetObjectByNPCId(2005843).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005843).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2005843!");
										GameObjectManager.GetObjectByNPCId(2005843).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005843).Location, 3)
							)
						)
					)
                ),
                // Attacked by mobs when trying to use the object - need to attack the mobs first
                // new Decorator(ret => QuestId == 67224 && GameObjectManager.GetObjectByNPCId(2006084) != null && GameObjectManager.GetObjectByNPCId(2006084).IsVisible,
                    // new PrioritySelector(
                        // new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006084).Location) <= 3,
                            // new Action(r =>
                            // {
								// Logging.Write("[ExtendedDuty] Interacting with object 2006084!");
                                // GameObjectManager.GetObjectByNPCId(2006084).Interact();
                            // })
				        // ),
                        // CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006084).Location, 3)
                    // )
                // ),
				new Decorator(ret => QuestId == 67240 && ((GameObjectManager.GetObjectByNPCId(2006369) != null && GameObjectManager.GetObjectByNPCId(2006369).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006370) != null && GameObjectManager.GetObjectByNPCId(2006370).IsVisible) || (GameObjectManager.GetObjectByNPCId(2005848) != null && GameObjectManager.GetObjectByNPCId(2005848).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006175) != null && GameObjectManager.GetObjectByNPCId(2006175).IsVisible) || (GameObjectManager.GetObjectByNPCId(2005851) != null && GameObjectManager.GetObjectByNPCId(2005851).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006369) != null && GameObjectManager.GetObjectByNPCId(2006369).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006369).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Black Standard!");
										GameObjectManager.GetObjectByNPCId(2006369).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006369).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006370) != null && GameObjectManager.GetObjectByNPCId(2006370).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006370).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Crimson Standard!");
										GameObjectManager.GetObjectByNPCId(2006370).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006370).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005848) != null && GameObjectManager.GetObjectByNPCId(2005848).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005848).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Ambush Site!");
										GameObjectManager.GetObjectByNPCId(2005848).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005848).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006175) != null && GameObjectManager.GetObjectByNPCId(2006175).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006175).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Signal Mortar!");
										GameObjectManager.GetObjectByNPCId(2006175).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006175).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005851) != null && GameObjectManager.GetObjectByNPCId(2005851).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005851).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Townsperson!");
										GameObjectManager.GetObjectByNPCId(2005851).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005851).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 67549 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2005959) != null && GameObjectManager.GetObjectByNPCId(2005959).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005959).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with The Balance!");
                                GameObjectManager.GetObjectByNPCId(2005959).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005959).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 67552 && ((GameObjectManager.GetObjectByNPCId(2005851) != null && GameObjectManager.GetObjectByNPCId(2005851).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006044) != null && GameObjectManager.GetObjectByNPCId(2006044).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006045) != null && GameObjectManager.GetObjectByNPCId(2006045).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2005851) != null && GameObjectManager.GetObjectByNPCId(2005851).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Vector3.Distance(Core.Player.Location, new Vector3(6.698669f, 44.51124f, -41.33673f)) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Townsperson!");
										GameObjectManager.GetObjectByNPCId(2005851).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005851).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006044) != null && GameObjectManager.GetObjectByNPCId(2006044).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006044).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006044!");
										GameObjectManager.GetObjectByNPCId(2006044).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006044).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006045) != null && GameObjectManager.GetObjectByNPCId(2006045).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006045).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006045!");
										GameObjectManager.GetObjectByNPCId(2006045).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006045).Location, 3)
							)
						)
					)
                ),
				new Decorator(ret => QuestId == 67555 && GameObjectManager.GetObjectByNPCId(3861) != null && !GameObjectManager.GetObjectByNPCId(3861).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(3861) != null && GameObjectManager.GetObjectByNPCId(3861).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(3861).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Using spell 3594 on NPC 3861!");
										ActionManager.DoAction(3594, GameObjectManager.GetObjectByNPCId(3861));
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(3861).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 67555 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2005632) != null && GameObjectManager.GetObjectByNPCId(2005632).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005632).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with Sealed Missive!");
                                GameObjectManager.GetObjectByNPCId(2005632).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005632).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67590 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2006125) != null && GameObjectManager.GetObjectByNPCId(2006125).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006125).Location) <= 3 && !didthething,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Frightened Maid!");
                                GameObjectManager.GetObjectByNPCId(2006125).Interact();
								Thread.Sleep(1000);
                                GameObjectManager.GetObjectByNPCId(2006125).Interact();
								Thread.Sleep(1000);
								didthething = true;
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006125).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67592 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2005823) != null && GameObjectManager.GetObjectByNPCId(2005823).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005823).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with Captive Pilgrims!");
                                GameObjectManager.GetObjectByNPCId(2005823).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005823).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67593 && GameObjectManager.GetObjectByNPCId(2006340) != null && GameObjectManager.GetObjectByNPCId(2006340).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006340).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with Stolen Goods!");
                                GameObjectManager.GetObjectByNPCId(2006340).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006340).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 67640 && Core.Player.InCombat && ((GameObjectManager.GetObjectByNPCId(2006281) != null && GameObjectManager.GetObjectByNPCId(2006281).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006283) != null && GameObjectManager.GetObjectByNPCId(2006283).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006285) != null && GameObjectManager.GetObjectByNPCId(2006285).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006282) != null && GameObjectManager.GetObjectByNPCId(2006282).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006284) != null && GameObjectManager.GetObjectByNPCId(2006284).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006286) != null && GameObjectManager.GetObjectByNPCId(2006286).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006285) != null && GameObjectManager.GetObjectByNPCId(2006285).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006285).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006285).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006285).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006283) != null && GameObjectManager.GetObjectByNPCId(2006283).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006283).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006283).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006283).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006281) != null && GameObjectManager.GetObjectByNPCId(2006281).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006281).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006281).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006281).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006286) != null && GameObjectManager.GetObjectByNPCId(2006286).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006286).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006286).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006286).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006284) != null && GameObjectManager.GetObjectByNPCId(2006284).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006284).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006284).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006284).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006282) != null && GameObjectManager.GetObjectByNPCId(2006282).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006282).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with Lingering Aether!");
										GameObjectManager.GetObjectByNPCId(2006282).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006282).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 67885 && Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2007454) != null && GameObjectManager.GetObjectByNPCId(2007454).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007454).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with object 2007454!");
                                GameObjectManager.GetObjectByNPCId(2007454).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007454).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 67947 && !Core.Player.InCombat && ((GameObjectManager.GetObjectByNPCId(6049) != null && GameObjectManager.GetObjectByNPCId(6049).IsVisible) || (GameObjectManager.GetObjectByNPCId(2007826) != null && GameObjectManager.GetObjectByNPCId(2007826).IsVisible) || (GameObjectManager.GetObjectByNPCId(2007825) != null && GameObjectManager.GetObjectByNPCId(2007825).IsVisible) || (GameObjectManager.GetObjectByNPCId(2007824) != null && GameObjectManager.GetObjectByNPCId(2007824).IsVisible) || (GameObjectManager.GetObjectByNPCId(2007823) != null && GameObjectManager.GetObjectByNPCId(2007823).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(6049) != null && GameObjectManager.GetObjectByNPCId(6049).IsVisible,
							new PrioritySelector(
								new Decorator(ret => !Core.Me.HasAura(840),
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Using Nocturnal Sect on me!");
										ActionManager.DoAction(3605, Core.Me);
									})
								),
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(6049).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Using Aspected Benefic on Geomantic Bond!");
										ActionManager.DoAction(3595, GameObjectManager.GetObjectByNPCId(6049));
										Thread.Sleep(10000);
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(6049).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2007826) != null && GameObjectManager.GetObjectByNPCId(2007826).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007826).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Northerly Shrine!");
										GameObjectManager.GetObjectByNPCId(2007826).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007826).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2007825) != null && GameObjectManager.GetObjectByNPCId(2007825).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007825).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Easterly Shrine!");
										GameObjectManager.GetObjectByNPCId(2007825).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007825).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2007824) != null && GameObjectManager.GetObjectByNPCId(2007824).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007824).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Southerly Shrine!");
										GameObjectManager.GetObjectByNPCId(2007824).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007824).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2007823) != null && GameObjectManager.GetObjectByNPCId(2007823).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007823).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Westerly Shrine!");
										GameObjectManager.GetObjectByNPCId(2007823).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007823).Location, 3)
							)
						)
					)
                ),
				new Decorator(ret => QuestId == 67954 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
                    new PrioritySelector(
						new Decorator(ret => MovementManager.IsMoving,
                            new Action(r =>
                            {
                                MovementManager.MoveForwardStop();
                            })
                        ),
                        new Decorator(ret => !didthething,
                            new Action(r =>
                            {
                                var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
								foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
								{
									if (slot.RawItemId == 2002053)
									{
										Logging.Write("[ExtendedDuty] Using the Herbal Remedy on Sanche!");
										slot.UseItem(targetnpc);
										didthething = true;
									}
								}
                            })
                        )
                    )
                ),
                new Decorator(ret => QuestId == 68051 && Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2008289) != null && GameObjectManager.GetObjectByNPCId(2008289).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008289).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with Ovoo!");
                                GameObjectManager.GetObjectByNPCId(2008289).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008289).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 68085 && !Core.Player.InCombat && ((GameObjectManager.GetObjectByNPCId(2008919) != null && GameObjectManager.GetObjectByNPCId(2008919).IsVisible) || (GameObjectManager.GetObjectByNPCId(2008914) != null && GameObjectManager.GetObjectByNPCId(2008914).IsVisible) || (GameObjectManager.GetObjectByNPCId(2008945) != null && GameObjectManager.GetObjectByNPCId(2008945).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2008945) != null && GameObjectManager.GetObjectByNPCId(2008945).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008945).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Destination!");
										GameObjectManager.GetObjectByNPCId(2008945).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008945).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2008914) != null && GameObjectManager.GetObjectByNPCId(2008914).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008914).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Tattered Diary!");
										GameObjectManager.GetObjectByNPCId(2008914).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008914).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2008919) != null && GameObjectManager.GetObjectByNPCId(2008919).IsVisible && GameObjectManager.GetObjectByNPCId(2008919).IsTargetable,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008919).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Queer Device!");
										GameObjectManager.GetObjectByNPCId(2008919).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008919).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 68086 && GameObjectManager.GetObjectByNPCId(2008907) != null && GameObjectManager.GetObjectByNPCId(2008907).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008907).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with Gate Winch!");
                                GameObjectManager.GetObjectByNPCId(2008907).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008907).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 68098 && GameObjectManager.GetObjectByNPCId(1021866) != null && !GameObjectManager.GetObjectByNPCId(1021866).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1021866) != null && GameObjectManager.GetObjectByNPCId(1021866).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1021866).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Handing over the Banquet Invitation to Gurumi Borlumi's Retainer!");
										ff14bot.Managers.GameObjectManager.GetObjectByNPCId(1021866).Interact();
										Thread.Sleep(2000);
										ff14bot.RemoteWindows.Talk.Next();
										Thread.Sleep(3000);
										foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
										{
											if(slot.RawItemId == 2002195)
											{
												slot.Handover();
											}
										}
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.Request.IsOpen)
											ff14bot.RemoteWindows.Request.HandOver();
										Thread.Sleep(2000);
										ff14bot.RemoteWindows.Talk.Next();
										Thread.Sleep(2000);
										if (ff14bot.RemoteWindows.SelectYesno.IsOpen)
											ff14bot.RemoteWindows.SelectYesno.ClickYes();
										Thread.Sleep(2000);
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1004142).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 68121 && GameObjectManager.GetObjectByNPCId(2007939) != null && GameObjectManager.GetObjectByNPCId(2007939).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2007939).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Pile of Books!");
                                GameObjectManager.GetObjectByNPCId(2007939).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2007939).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 68461 && !Core.Player.InCombat && ((GameObjectManager.GetObjectByNPCId(2008882) != null && GameObjectManager.GetObjectByNPCId(2008882).IsVisible) || (GameObjectManager.GetObjectByNPCId(2008434) != null && GameObjectManager.GetObjectByNPCId(2008434).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2008882) != null && GameObjectManager.GetObjectByNPCId(2008882).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008882).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Silver Coffer!");
										GameObjectManager.GetObjectByNPCId(2008882).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008882).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2008434) != null && GameObjectManager.GetObjectByNPCId(2008434).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008434).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with the Leather-bound Tome!");
										GameObjectManager.GetObjectByNPCId(2008434).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008434).Location, 3)
							)
						)
					)
                ),
                new Decorator(ret => QuestId == 68486 && GameObjectManager.GetObjectByNPCId(2008896) != null && GameObjectManager.GetObjectByNPCId(2008896).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2008896).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with the Shackles!");
                                GameObjectManager.GetObjectByNPCId(2008896).Interact();
								Thread.Sleep(15000);
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2008896).Location, 3)
                    )
                ),
                base.CreateBehavior()
                );
        }
    }
}