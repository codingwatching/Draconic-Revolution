using System;
using UnityEngine;

[Serializable]
public class AnimationStateMapping {
	public string state;
	public string[] layers;
	public int priority;
	public string[] stopLayer;

	public void PostDeserializationSetup(){
		for(int i=0; i < this.layers.Length; i++){
			if(this.layers[i] == "")
				this.layers[i] = "Base Layer";
		}
	}

	public override string ToString(){return $"{this.state} -- {this.layers} -- {this.priority}";}
}