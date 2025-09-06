using System;

[Serializable]
public class AnimationStateMapping {
	public string type;
	public string targetLayer;
	public string currentState;
	public string currentLayer;
	public string playState;
	public string[] stop_layers;
	private AnimationStateMappingType mappingType;

	public AnimationStateMappingType GetMapType(){return this.mappingType;}

	public void PostDeserializationSetup(){
		switch(type){
			case "play_on":
				this.mappingType = AnimationStateMappingType.PLAY_ON;
				break;
			case "continue_current_on":
				this.mappingType = AnimationStateMappingType.CONTINUE_CURRENT_ON;
				break;
			case "stop_layers":
				this.mappingType = AnimationStateMappingType.STOP_LAYERS;
				break;
			default:
				this.mappingType = AnimationStateMappingType.PLAY_ON;
				break;
		}

		if(this.currentLayer == "")
			this.currentLayer = "Base Layer";
		if(this.targetLayer == "")
			this.targetLayer = "Base Layer";
	}

	public override string ToString(){
		return $"Type: {this.type} -- if CurrentLayer = {this.currentLayer} and state is = {this.currentState}, then play state {this.playState} at layer {this.targetLayer}";
	}
}