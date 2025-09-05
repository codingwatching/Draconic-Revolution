using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class AnimationHandler : MonoBehaviour {
	private bool INIT = false;
	private bool isPlayer = false;

	private Animator tpAnimator;
	private Animator fpAnimator;
	private ShapeKeyAnimator shapeKeyAnimator;
	private ProceduralAnimationRigController rigControllerTP;
	private ProceduralAnimationRigController rigControllerFP;
	private Dictionary<BoneAnimationRequest, List<AnimationStateMapping>> stateMappings = new Dictionary<BoneAnimationRequest, List<AnimationStateMapping>>();

	public void Init(string controllerName, CharacterBuilder firstPersonBuilder, bool isUserCharacter=false){
		Transform tpParent = this.transform.Find("TP-Rig");
		Transform fpParent = this.transform.Find("FP-Rig");

		LoadMapping(controllerName);
		this.INIT = true;
		this.isPlayer = isUserCharacter;

		this.tpAnimator = tpParent.GetComponent<Animator>();
		this.shapeKeyAnimator = tpParent.GetComponent<ShapeKeyAnimator>();
		this.rigControllerTP = new ProceduralAnimationRigController(tpParent.gameObject, tpParent.Find("Animator").gameObject, controllerName);
		this.rigControllerTP.Build();

		if(this.isPlayer){
			this.fpAnimator = fpParent.GetComponent<Animator>();
			this.rigControllerFP = new ProceduralAnimationRigController(fpParent.gameObject, fpParent.Find("Animator").gameObject, $"{controllerName}_FP");
			this.rigControllerFP.Build();

			SetFirstPersonRotation(firstPersonBuilder);
		}
	}


	// Plays bone animation
	public void Play(BoneAnimationRequest request){
		if(!this.INIT)
			return;

		if(this.stateMappings.ContainsKey(request)){
			for(int i=0; i < this.stateMappings[request].Count; i++){
				if(HandleBoneRequest(this.stateMappings[request][i])){
					break;
				}
			}
		}
		else{
			this.tpAnimator.CrossFade(request.name, 0.1f, layer:this.tpAnimator.GetLayerIndex(request.layer));
		}
	}

	// Plays/Stops/Registers ShapeKey Animations based on the settings inputted
	public void Play(string shapeKey, ShapeKeyAnimationSettings settings){
		if(!this.INIT)
			return;

		this.shapeKeyAnimator.Play(shapeKey, settings);
	}

	public void AssignAimTracker(Transform tracker){
		this.rigControllerTP.AssignHeadTrackingSource(tracker);
		this.rigControllerFP.AssignHeadTrackingSource(tracker);
	}

	private void SetFirstPersonRotation(CharacterBuilder builder){
		builder.SetFirstPersonRotation(rigControllerFP);
	}

	// Returns true if boneRequest plays the animation
	private bool HandleBoneRequest(AnimationStateMapping mapping){
		AnimatorStateInfo currentState = this.tpAnimator.GetCurrentAnimatorStateInfo(this.tpAnimator.GetLayerIndex(mapping.currentLayer));

		switch(mapping.GetMapType()){
			case AnimationStateMappingType.PLAY_ON:
				return PlayOn(currentState, mapping);
			case AnimationStateMappingType.CONTINUE_CURRENT_ON:
				return ContinueCurrentOn(currentState, mapping);
			case AnimationStateMappingType.STOP_LAYERS:
				return StopLayers(mapping);
			default:
				return false;
		}
	}

	private bool PlayOn(AnimatorStateInfo currentState, AnimationStateMapping mapping){
		if(currentState.IsName(mapping.currentState)){
			this.tpAnimator.CrossFade(mapping.playState, 0.1f, layer:this.tpAnimator.GetLayerIndex(mapping.targetLayer));
			return true;
		}

		return false;
	}

	private bool ContinueCurrentOn(AnimatorStateInfo currentState, AnimationStateMapping mapping){
		if(currentState.IsName(mapping.currentState)){
			this.tpAnimator.Play(mapping.currentState, this.tpAnimator.GetLayerIndex(mapping.targetLayer), normalizedTime:currentState.normalizedTime);
			this.tpAnimator.Play(mapping.playState, this.tpAnimator.GetLayerIndex(mapping.currentLayer));
			return true;
		}

		return false;
	}

	private bool StopLayers(AnimationStateMapping mapping){
		string[] layers = mapping.stop_layers;

		for(int i=0; i < layers.Length; i++){
			// Plays the default layer state
			this.tpAnimator.CrossFade(0, 0.1f, layer:this.tpAnimator.GetLayerIndex(layers[i]));
		}

		this.tpAnimator.CrossFade(mapping.playState, 0.1f, layer:this.tpAnimator.GetLayerIndex(mapping.targetLayer));
		return true;
	}

	private void LoadMapping(string controllerName){
		BoneAnimationRequest request;

		foreach(AnimationStateMapping map in AnimationLoader.GetAnimationMapping(controllerName)){
			request = new BoneAnimationRequest(map.playState, map.currentLayer);

			if(!this.stateMappings.ContainsKey(request)){
				this.stateMappings.Add(request, new List<AnimationStateMapping>());
			}

			this.stateMappings[request].Add(map);
		}
	}
}