public struct EntityID{
	public EntityType type;
	public ChunkPos pos;
	public ulong code;

	public EntityID(EntityType t, ChunkPos p, ulong c){
		this.type = t;
		this.pos = p;
		this.code = c;
	}

	public bool Equals(EntityID other){
		return this.type == other.type && this.code == other.code;
	}

	public bool IsDiffPosition(EntityID other){
		return !(this.pos == other.pos);
	}

	public override string ToString(){
		return this.type + "  " + this.pos + "   Code: " + this.code;
	}
}