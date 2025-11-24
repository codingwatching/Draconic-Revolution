using System;

[Serializable]
public struct BattleStyleData{
	public int combo_hits;
	public Wrapper<StateClipPair> overrides;
	private StateClipPair[] clipPairs;


	public void PostDeserializationSetup(){this.clipPairs = overrides.data;}
	public int GetComboHits(){return this.combo_hits;}
	public StateClipPair[] GetOverrides(){return this.clipPairs;}
}