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
                new Decorator(ret => QuestId == 65995 && GameObjectManager.GetObjectByNPCId(2002521) != null && GameObjectManager.GetObjectByNPCId(2002521).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002521).Location) <= 3,
                            new Action(r =>
                            {
                                GameObjectManager.GetObjectByNPCId(2002521).Interact();
                            })
				        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002521).Location, 3)
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
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002521).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 66633 && GameObjectManager.GetObjectByNPCId(2002522) != null && GameObjectManager.GetObjectByNPCId(2002522).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002522).Location) <= 3,
                            new Action(r =>
                            {
                                GameObjectManager.GetObjectByNPCId(2002522).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002522).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 66448 && GameObjectManager.GetObjectByNPCId(2002279) != null && GameObjectManager.GetObjectByNPCId(2002279).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002279).Location) <= 3,
                            new Action(r =>
                            {
                                GameObjectManager.GetObjectByNPCId(2002279).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002279).Location, 3)
                    )
                ),
				new Decorator(ret => QuestId == 66057 && ((GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible) || (GameObjectManager.GetObjectByNPCId(2002427) != null && GameObjectManager.GetObjectByNPCId(2002427).IsVisible)),
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002428).Location) <= 3,
									new Action(r =>
									{
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
				new Decorator(ret => QuestId == 66638 && GameObjectManager.GetObjectByNPCId(1650) != null && !GameObjectManager.GetObjectByNPCId(1650).CanAttack,
                    new PrioritySelector(
						new Decorator(ret => GameObjectManager.GetObjectByNPCId(1650) != null && GameObjectManager.GetObjectByNPCId(1650).IsVisible,
							new PrioritySelector(
								new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1650).Location) <= 3,
									new Action(r =>
									{
										ff14bot.Managers.Actionmanager.DoAction(190, GameObjectManager.GetObjectByNPCId(1650));
									})
								),
								CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1650).Location, 3)
							)
						)
					)
                ),
                base.CreateBehavior()
                );
        }
    }
}