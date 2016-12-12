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
								Logging.Write("[ExtendedDuty] Interacting with object 2002521!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2002428!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2002427!");
										GameObjectManager.GetObjectByNPCId(2002427).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002427).Location, 3)
							)
						)
					)
                ),
				new Decorator(ret => QuestId == 66057 && ((GameObjectManager.GetObjectByNPCId(2094) != null && GameObjectManager.GetObjectByNPCId(2094).IsVisible) || (GameObjectManager.GetObjectByNPCId(1813) != null && GameObjectManager.GetObjectByNPCId(1813).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2094) != null && GameObjectManager.GetObjectByNPCId(2094).IsVisible,
							CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2094).Location, 3)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1813) != null && GameObjectManager.GetObjectByNPCId(1813).IsVisible,
							CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1813).Location, 3)
						)
					)
                ),
                new Decorator(ret => QuestId == 66448 && GameObjectManager.GetObjectByNPCId(2002279) != null && GameObjectManager.GetObjectByNPCId(2002279).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002279).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with object 2002279!");
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
										slot.UseItem(targetnpc);
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
								Logging.Write("[ExtendedDuty] Interacting with object 2002522!");
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
										Logging.Write("[ExtendedDuty] Using spell 190 on NPC 1650 !");
										ff14bot.Managers.Actionmanager.DoAction(190, GameObjectManager.GetObjectByNPCId(1650));
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
										Logging.Write("[ExtendedDuty] Interacting with object 2006369!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2006370!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2005848!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2006175!");
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
										Logging.Write("[ExtendedDuty] Interacting with object 2005851!");
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
								Logging.Write("[ExtendedDuty] Interacting with object 2005959!");
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
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005851).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2005851!");
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
										ff14bot.Managers.Actionmanager.DoAction(3594, GameObjectManager.GetObjectByNPCId(3861));
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
								Logging.Write("[ExtendedDuty] Interacting with object 2005632!");
                                GameObjectManager.GetObjectByNPCId(2005632).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005632).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67592 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2005823) != null && GameObjectManager.GetObjectByNPCId(2005823).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005823).Location) <= 3,
                            new Action(r =>
                            {
								Logging.Write("[ExtendedDuty] Interacting with object 2005823!");
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
								Logging.Write("[ExtendedDuty] Interacting with object 2006340!");
                                GameObjectManager.GetObjectByNPCId(2006340).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006340).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 67640 && ((GameObjectManager.GetObjectByNPCId(2006281) != null && GameObjectManager.GetObjectByNPCId(2006281).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006283) != null && GameObjectManager.GetObjectByNPCId(2006283).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006285) != null && GameObjectManager.GetObjectByNPCId(2006285).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006282) != null && GameObjectManager.GetObjectByNPCId(2006282).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006284) != null && GameObjectManager.GetObjectByNPCId(2006284).IsVisible) || (GameObjectManager.GetObjectByNPCId(2006286) != null && GameObjectManager.GetObjectByNPCId(2006286).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006281) != null && GameObjectManager.GetObjectByNPCId(2006281).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006281).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006281!");
										GameObjectManager.GetObjectByNPCId(2006281).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006281).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006283) != null && GameObjectManager.GetObjectByNPCId(2006283).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006283).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006283!");
										GameObjectManager.GetObjectByNPCId(2006283).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006283).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006285) != null && GameObjectManager.GetObjectByNPCId(2006285).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006285).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006285!");
										GameObjectManager.GetObjectByNPCId(2006285).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006285).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006282) != null && GameObjectManager.GetObjectByNPCId(2006282).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006282).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006282!");
										GameObjectManager.GetObjectByNPCId(2006282).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006282).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006284) != null && GameObjectManager.GetObjectByNPCId(2006284).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006284).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006284!");
										GameObjectManager.GetObjectByNPCId(2006284).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006284).Location, 3)
							)
						),
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2006286) != null && GameObjectManager.GetObjectByNPCId(2006286).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006286).Location) <= 3,
									new Action(r =>
									{
										Logging.Write("[ExtendedDuty] Interacting with object 2006286!");
										GameObjectManager.GetObjectByNPCId(2006286).Interact();
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006286).Location, 3)
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
                base.CreateBehavior()
                );
        }
    }
}