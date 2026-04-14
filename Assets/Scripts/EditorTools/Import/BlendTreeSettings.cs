#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

[Serializable]
public class BlendTreeSettings {
	public float minThreshold;
	public float maxThreshold;
	public BlendingParameterSettings blendParameter;

	// Requires a dict of <string, Motion> to be created by the AnimationLoader first
	public BlendTree Build(string blendTreeName, Dictionary<string, Motion> clips){
		string clipA = $"{blendTreeName}_A";
		string clipB = $"{blendTreeName}_B";

		BlendTree tree = new BlendTree();

		tree.name = blendTreeName;
		tree.useAutomaticThresholds = false;
		tree.minThreshold = this.minThreshold;
		tree.maxThreshold = this.maxThreshold;
		tree.AddChild(clips[clipA], this.minThreshold);
		tree.AddChild(clips[clipB], this.maxThreshold);
		tree.blendParameter = this.blendParameter.parameterName;
		tree.blendType = BlendTreeType.Simple1D;

		return tree;
	}

	public void PostDeserializationSetup(){
		this.blendParameter.PostDeserializationSetup();
	}
}
#endif