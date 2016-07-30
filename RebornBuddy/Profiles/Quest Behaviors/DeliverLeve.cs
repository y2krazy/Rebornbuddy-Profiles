using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using System;
using System.Linq;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
	[XmlElement("DevlierLeve")]

	public class DevlierLeveTag : ProfileBehavior
	{
		private bool _done;

		[XmlAttribute("LeveName")]
		public string LeveName { get; set; }

		[XmlAttribute("NpcId")]
		public int NpcId { get; set; }

		[XmlAttribute("ItemId")]
		public int  ItemId { get; set; }

		[XmlAttribute("XYZ")]
		public Vector3 XYZ { get; set; }

		public bool handedover = false;
		public bool interacted = false;
		public bool leveopened = false;

		public override bool IsDone { get { return _done; } }

		protected override Composite CreateBehavior()
		{
			return new PrioritySelector(
                new Decorator(ret => Talk.DialogOpen,
					new Action(r =>
				                {
				                Talk.Next();
				                })
                ),
                new Decorator(ret => SelectYesno.IsOpen,
                    new Action(r =>
     				            {
     					        SelectYesno.ClickYes();
     				            })
                ),
                new Decorator(ret => SelectIconString.IsOpen,
                    new Action(r =>
     							{
                                    SelectIconString.ClickLineContains(LeveName);
     							
		                        })
                 ),
             	  new Decorator(ret => Request.IsOpen ,
                    new Action(r =>
                    {
                        foreach(ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
                        {
                            if (slot.RawItemId == (uint)ItemId || slot.TrueItemId == (uint)ItemId)
                            {
                                slot.Handover();
                                Request.HandOver();
                                //handedover = true;
                            }
                        }
                    })
                ),
			  
                new Decorator(ret => JournalResult.IsOpen,
                    new Action(r =>
                    {
                        JournalResult.Complete();
                    })
                ),
                new Decorator(ret => interacted && !Core.Player.HasTarget,
                    new Action(r =>
                    {
                        _done = true;
                    })
                ),
                new Decorator(ret => Vector3.Distance(Core.Player.Location, XYZ) > 3,
                    CommonBehaviors.MoveTo(r => XYZ, "Moving to deliver Leve")
                ),
                new Decorator(ret => Vector3.Distance(Core.Player.Location, XYZ) <= 3 && MovementManager.IsMoving,
                    ff14bot.Behavior.CommonBehaviors.MoveStop()
                ),
                new Decorator(ret => Vector3.Distance(Core.Player.Location, XYZ) <= 3 && !MovementManager.IsMoving && !interacted,
                    new Action(r =>
                    {
                        GameObjectManager.GetObjectByNPCId((uint)NpcId).Interact();
                        interacted = true;
                    })
                ),
                new ActionAlwaysSucceed()
            );
        }

        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            handedover = false;
            interacted = false;
			leveopened = false;
            _done = false;
        }
		
		public bool WindowCheck()
        {
            foreach (var foundwindow in RaptureAtkUnitManager.GetRawControls)
			{
				if(foundwindow.Name == "GuildLeve")
				{
					return true;
				}
			}
			
			return false;
        }
		
		public static bool IsOpen
		{
			get
			{
				return RaptureAtkUnitManager.GetRawControls.Any(r => r.Name == "GuildLeve");
			}
		}

		public static void AcceptLeve(uint leve)
		{
			// Value Pairs: {3, 0xB}, {3, 1}, {3, 0x236}
			// Pair 1 is unknown
			// Pair 2 is unknown
			// Pair 3 is the leve number
			RaptureAtkUnitManager.GetWindowByName("GuildLeve").SendAction(2, 3, 3, 4, leve);
		}
		
		public static void Close()
		{
			// Value Pairs: {3, 0xFFFFFFFF}
			// Pair 1 is unknown
			if(IsOpen)
			{
				RaptureAtkUnitManager.GetWindowByName("GuildLeve").SendAction(1, 3, 0xFFFFFFFF);
			}
		}
	
		
        protected override void OnDone()
        {
            
        }
    }
}

