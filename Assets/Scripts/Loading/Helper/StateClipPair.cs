using System;

[Serializable]
public struct StateClipPair<T, U>{
	public T state;
	public U clip;

	public override string ToString(){
		return "{" + this.state.ToString() + ": " + this.clip.ToString() + "}";
	}
}