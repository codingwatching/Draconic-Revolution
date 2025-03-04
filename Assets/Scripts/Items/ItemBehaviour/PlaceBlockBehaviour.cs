using System;
using UnityEngine;
using Unity.Mathematics;

[Serializable]
public class PlaceBlockBehaviour : ItemBehaviour{
	public string blockName;
	private ushort blockID;

	public override void PostDeserializationSetup(bool isClient){
		this.blockID = VoxelLoader.GetBlockID(this.blockName);
	}

	public override void OnUseClient(ChunkLoader cl, ItemStack its, Vector3 usagePos, CastCoord targetBlock, CastCoord referencePoint1, CastCoord referencePoint2, CastCoord referencePoint3){
		Item it = its.GetItem();

		if(this.PlaceBlock(this.blockID, (byte)(its.GetAmount()-1), targetBlock, referencePoint1, referencePoint2, referencePoint3, cl)){
			cl.playerRaycast.lastBlockPlaced = it.GetID();
			if(its.Decrement()){
				cl.playerEvents.hotbar.SetNull(PlayerEvents.hotbarSlot);
				cl.playerEvents.DestroyItemEntity();
			}
			cl.playerEvents.DrawHotbarSlot(PlayerEvents.hotbarSlot);
			cl.playerEvents.invUIPlayer.DrawSlot(1, PlayerEvents.hotbarSlot);
			cl.playerEvents.invUIPlayer.SendInventoryDataToServer();
		}
	}

	private bool PlaceBlock(ushort blockCode, byte newQuantity, CastCoord targetBlock, CastCoord playerHead, CastCoord playerBody, CastCoord currentHitBlock, ChunkLoader loader){
		// Won't happen if not raycasting something or if block is in player's body or head
		if(!currentHitBlock.active || (CastCoord.Eq(targetBlock, playerHead) && VoxelLoader.CheckSolid(blockCode)) || (CastCoord.Eq(targetBlock, playerBody) && VoxelLoader.CheckSolid(blockCode))){
			return false;
		}

		if(loader.GetBlock(targetBlock) != 0)
			return false;

		NetMessage message = new NetMessage(NetCode.DIRECTBLOCKUPDATE);
		message.DirectBlockUpdate(BUDCode.PLACE, targetBlock.GetChunkPos(), targetBlock.blockX, targetBlock.blockY, targetBlock.blockZ, loader.playerRaycast.facing, blockCode, ushort.MaxValue, ushort.MaxValue, slot:PlayerEvents.hotbarSlot, newQuantity:newQuantity);
		loader.client.Send(message);
		return true;
	}
}