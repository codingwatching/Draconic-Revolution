using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhiteMarbleBrickblock_Item : Item, IPlaceable
{
	public ushort placeableBlockID {get; set;}

	public WhiteMarbleBrickblock_Item(){
		this.SetName("White Marble Bricks");
		this.SetDescription("Pleasing white bricks");
		this.SetID(ItemID.WHITEMARBLEBRICKBLOCK);
		this.SetIconID(2, 0);
		this.SetStackSize(50);
		this.SetPrice(0);
		this.SetPriceVar(0);
		this.SetAspects(new Dictionary<ThaumicAspect, byte>(){{ThaumicAspect.Terra, 2}, {ThaumicAspect.Ordo, 2}});
		this.SetTags(new List<ItemTag>(){ItemTag.Placeable, ItemTag.Stone});
		this.SetDurability(false);
		this.placeableBlockID = (ushort)BlockID.WHITE_MARBLE_BRICKS;
		this.memoryStorageType = MemoryStorageType.ITEM;
	}

	public override int Use(){
		return this.placeableBlockID;
	}
}