#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[Serializable]
public class AnimationStateSettings {
	public string name;
	public string layer;
	public bool isBlendTree;
	public bool isDefaultState;
	public BlendTreeSettings blendTree;

	public AnimatorState Build(AnimatorController animatorController, Dictionary<string, Motion> animations, string animationsClipPath){
		AnimatorState state = new AnimatorState();
		AnimationClip clip = new AnimationClip();

		state.name = this.name;
		clip.name = this.name;

		if(this.isBlendTree){
			AnimationClip clipB = new AnimationClip();

			clip.name = $"{state.name}_A";
			clipB.name = $"{state.name}_B";

			if(!animations.ContainsKey($"{state.name}_A")){
				animations.Add($"{state.name}_A", clip);
				AssetDatabase.CreateAsset(clip, $"{animationsClipPath}{state.name}_A.anim");
			}

			if(!animations.ContainsKey($"{state.name}_B")){
				animations.Add($"{state.name}_B", clipB);
				AssetDatabase.CreateAsset(clipB, $"{animationsClipPath}{state.name}_B.anim");
			}

			BlendTree blendTree = this.blendTree.Build(this.name, animations);
			state.motion = blendTree;
			AnimatorControllerParameter acp = this.blendTree.blendParameter.Build();

			if(!CheckControllerHasParameter(acp.name, animatorController)){
				animatorController.AddParameter(acp);
			}
		}
		else{
			if(!animations.ContainsKey(state.name)){
				animations.Add(state.name, clip);
				AssetDatabase.CreateAsset(clip, $"{animationsClipPath}{state.name}.anim");
			}

			state.motion = animations[state.name];
		}

		return state;
	}

	public void PostDeserializationSetup(){
		if(this.layer == "")
			this.layer = "Base Layer";

		if(this.isBlendTree)
			this.blendTree.PostDeserializationSetup();
	}

	private bool CheckControllerHasParameter(string name, AnimatorController controller){
		for(int i=0; i < controller.parameters.Length; i++){
			if(controller.parameters[i].name == name){
				return true;
			}
		}
		return false;
	}
}

#endif