using System;

[Serializable]
public struct StateClipPair{
	public string state;
	public string clip;
	public string direction;
	public float momentum;

	public override string ToString(){
		return "{" + this.state.ToString() + ": " + this.clip.ToString() + "}";
	}
}