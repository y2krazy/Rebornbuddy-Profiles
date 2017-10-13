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
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{	
	[XmlElement("TurnInPlus")]

	class TurnInPlusTag : TurnInTag
	{
		public bool actiontaken = false;

		protected override void OnDone()
		{
			actiontaken = false;
			base.OnDone();
		}

		protected override Composite CreateBehavior()
		{
			return new PrioritySelector(
                new Decorator(ret => QuestId == 65539 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65590 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65622 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65667 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65668 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65669 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65821 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65846 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 65880 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 65964 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 65988 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66053 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(2);
                    })
                ),
                new Decorator(ret => QuestId == 66068 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66133 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66287 && Vector3.Distance(Core.Player.Location, XYZ) < InteractDistance && !actiontaken,
                    new Sequence(
						new Action(r =>
						{
							var targetnpc = GameObjectManager.GetObjectByNPCId((uint)NpcId);
							targetnpc.Target();
							foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
							{
								if (slot.RawItemId == 2000975)
								{
									slot.UseItem(targetnpc);
									break;
								}
							}
							actiontaken = true;
						}),
						new Sleep(3,5),
						new Decorator(ret => !Talk.DialogOpen,
							new Action(r =>
							{
								actiontaken = false;
							})
						)
					)
                ),
                new Decorator(ret => QuestId == 66426 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(2);
                    })
                ),
                new Decorator(ret => QuestId == 66493 && Vector3.Distance(Core.Player.Location, XYZ) < InteractDistance && !actiontaken,
                    new Sequence(
						new Action(r =>
						{
							var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)NpcId);
							targetnpc.Target();
							foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
							{
								if (slot.RawItemId == 2000976)
								{
									slot.UseItem(targetnpc);
									actiontaken = true;
								}
							}
						}),
						new Sleep(3,5),
						new Decorator(ret => !Talk.DialogOpen,
							new Action(r =>
							{
								actiontaken = false;
							})
						)
					)
                ),
				new Decorator(ret => QuestId == 66579 && Vector3.Distance(Core.Player.Location, XYZ) < InteractDistance && !actiontaken,
                    new Sequence(
						new Action(r =>
						{
							var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)NpcId);
							targetnpc.Target();
							ChatManager.SendChat("/psych");
							actiontaken = true;
						}),
						new Sleep(3,5),
						new Decorator(ret => !Talk.DialogOpen,
							new Action(r =>
							{
								actiontaken = false;
							})
						)
					)
                ),
				new Decorator(ret => QuestId == 66584 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 66642 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 66693 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
				new Decorator(ret => QuestId == 66694 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 66724 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 66740 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(1);
                    })
                ),
				new Decorator(ret => QuestId == 66979 && Vector3.Distance(Core.Player.Location, XYZ) < InteractDistance && !actiontaken,
                    new Sequence(
						new Action(r =>
						{
							var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)NpcId);
							targetnpc.Target();
							ChatManager.SendChat("/soothe");
							actiontaken = true;
						}),
						new Sleep(3,5),
						new Decorator(ret => !Talk.DialogOpen,
							new Action(r =>
							{
								actiontaken = false;
							})
						)
					)
                ),
                new Decorator(ret => QuestId == 67138 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(0);
                    })
                ),
                new Decorator(ret => QuestId == 67405 && SelectString.IsOpen,
                    new Action(r =>
                    {
                        SelectString.ClickSlot(4);
                    })
                ),
				new Decorator(ret => QuestId == 67504 && Vector3.Distance(Core.Player.Location, XYZ) < InteractDistance && !actiontaken,
                    new Sequence(
						new Action(r =>
						{
							var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)NpcId);
							targetnpc.Target();
							ChatManager.SendChat("/cry");
							actiontaken = true;
						}),
						new Sleep(3,5),
						new Decorator(ret => !Talk.DialogOpen,
							new Action(r =>
							{
								actiontaken = false;
							})
						)
					)
                ),
				new Decorator(ret => QuestId == 67670 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 68103 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => QuestId == 68112 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				new Decorator(ret => QuestId == 68291 && SelectYesno.IsOpen,
					new Action(r =>
					{
						SelectYesno.ClickYes();
					})
				),
                new Decorator(ret => QuestId == 68501 && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
				base.CreateBehavior()
				);
		}
	}
}