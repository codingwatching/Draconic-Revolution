using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerEntityRadar : EntityRadar{
	private ChunkLoader_Server cl;
	private PlayerServerInventory psi;
	private Item cachedItem;
	private DroppedItemAI cachedItemAI;
	public bool HAS_RECEIVED_ITEMS;

	public PlayerEntityRadar(Vector3 pos, Vector3 dir, CastCoord coords, EntityID entityID, EntityHandler_Server ehs, PlayerServerInventory psi, ChunkLoader_Server cl){
		this.SetTransform(ref pos, ref dir, ref coords);
		this.entityHandler = ehs;
		this.FOV = 180;
		this.visionDistance = 1.4f;
		this.entitySubscription = new HashSet<EntityType>(){EntityType.DROP};
		this.psi = psi;
		this.ID = entityID;
		this.cl = cl;
	}

	protected override bool PreAnalysisAI(AbstractAI ai){
		this.cachedItemAI = (DroppedItemAI)ai;

		if(this.cachedItemAI.IsOnPickupMode())
			return false;

		if(this.cachedItemAI.markedForDelete || this.cachedItemAI.markedForChange)
			return false;

		if(this.cachedItemAI.IsCreatedByPlayer() && this.cachedItemAI.playerCode == this.ID.code){
			return false;
		}

		return true;
	}

	protected override bool PostAnalysisAI(AbstractAI ai){
		this.cachedItemAI = (DroppedItemAI)ai;

		ItemStack aiItem = this.cachedItemAI.GetItemStack();
		byte playerSelectedSlot;
		int2 inventorySlot = this.psi.CheckFits(this.ID.code, aiItem);

		if(inventorySlot.x == -1)
			return false;
		else{
			playerSelectedSlot = this.entityHandler.GetSheet(this.ID.code).GetHotbarSlot();
			
			// Checks if ItemStack was previously empty (to trigger OnHold event)
			if(playerSelectedSlot == inventorySlot.x){ // If it's the same slot as the selected one
				if(psi.GetSlot(this.ID.code, playerSelectedSlot).GetItemStack().GetID() != aiItem.GetID()){
					aiItem.GetItem().OnHoldServer(this.cl, aiItem, this.ID.code);

					NetMessage message = new NetMessage(NetCode.SENDITEMINHAND);
					message.SendItemInHand(this.ID.code, aiItem.GetID(), aiItem.GetAmount());
					this.cl.server.SendToClientsExcept(this.ID.code, message);
				}
			}

			// If can completely take the stack
			if(aiItem.GetStacksize() >= inventorySlot.y + aiItem.GetAmount()){
				if(inventorySlot.y == 0)
					psi.CreateSlotAt((byte)inventorySlot.x, this.ID.code, CreateSlot(aiItem));

				psi.ChangeQuantity(this.ID.code, (byte)inventorySlot.x, (byte)(inventorySlot.y + aiItem.GetAmount()));
				this.cachedItemAI.SetPickupMode();
				this.HAS_RECEIVED_ITEMS = true;
				return true;
			}
			// If player can't take the entire stack
			else{
				psi.ChangeQuantity(this.ID.code, (byte)inventorySlot.x, aiItem.GetStacksize());
				this.HAS_RECEIVED_ITEMS = true;
				return false;
			}
		}
	}

	protected override void CreateEntityRadarEvent(AbstractAI ai, ref List<EntityEvent> ieq){
		ai.AddToInboundEventQueue(new EntityEvent(EntityEventType.ITEM_PICKUP, false, new EntityRadarEvent(ai.GetID(), ai.GetPosition(), ai.GetPosition(), ai), this.position - ai.position));
	}

	private PlayerServerInventorySlot CreateSlot(ItemStack its){
		this.cachedItem = its.GetItem();

		if(this.cachedItem.GetMemoryStorageType() == MemoryStorageType.ITEM){
			return new ItemPlayerInventorySlot(its.GetID(), its.GetAmount());
		} 
		else if(this.cachedItem.GetMemoryStorageType() == MemoryStorageType.WEAPON){
			return new WeaponPlayerInventorySlot(its.GetID(), ((Weapon)this.cachedItem).currentDurability, ((Weapon)this.cachedItem).refineLevel, ((Weapon)this.cachedItem).extraEffect);
		}
		else{
			// STORAGE ITEMS NOT IMPLEMENTED
			return new EmptyPlayerInventorySlot();
		}
	}
}