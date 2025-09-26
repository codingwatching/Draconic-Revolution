using System;

public struct BoneAnimationRequest {
	public string name;
	public string layer;

	public BoneAnimationRequest(string n, string l){
		this.name = n;

		if(l == "")
			this.layer = "Base Layer";
		else
			this.layer = l;
	}

	public bool Equals(BoneAnimationRequest other){return this.layer == other.layer && this.name == other.name;}
	public override bool Equals(object obj){return obj is BoneAnimationRequest other && Equals(other);}
	public override int GetHashCode(){return HashCode.Combine(this.name, this.layer);}
	public override string ToString(){return $"{this.name} | {this.layer}";}
}