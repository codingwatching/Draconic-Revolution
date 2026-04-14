using System;

public struct PlayerVoxelLocation{
	public static PlayerVoxelLocation zero = new PlayerVoxelLocation{feet = 0, body = 0, head = 0};
	public ushort feet;
	public ushort body;
	public ushort head;

	public override string ToString(){return $"Feet: {this.feet} -- Body: {this.body} -- Head: {this.head}";}
}