using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public struct BattleStyleData{
	public int combo_hits;
	public Wrapper<StateClipPair> overrides;
	private StateClipPair[] clipPairs;
	private Dictionary<string, StateClipPair> map;


	public void PostDeserializationSetup(){
		this.map = new Dictionary<string, StateClipPair>();
		this.clipPairs = overrides.data;

		for(int i=0; i < this.clipPairs.Length; i++){
			this.map.Add(this.clipPairs[i].state, this.clipPairs[i]);
		}
	}

	public int GetComboHits(){return this.combo_hits;}
	public StateClipPair[] GetOverrides(){return this.clipPairs;}
	public StateClipPair GetStateStyleData(string state){return this.map[state];}
}