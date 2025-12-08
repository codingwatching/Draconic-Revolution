using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

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
		Transform tpAnimObj = tpParent.Find("Animator");

		LoadMapping(controllerName);
		this.isPlayer = isUserCharacter;

		this.tpAnimator = tpAnimObj.GetComponent<Animator>();
		this.shapeKeyAnimator = tpParent.GetComponent<ShapeKeyAnimator>();
		this.rigControllerTP = new ProceduralAnimationRigController(tpParent.gameObject, tpAnimObj.gameObject, controllerName);
		this.rigControllerTP.Build();

		if(this.isPlayer){
			Transform fpParent = this.transform.Find("FP-Rig");
			Transform fpAnimObj = fpParent.Find("Animator");
			
			this.fpAnimator = fpAnimObj.GetComponent<Animator>();
			this.rigControllerFP = new ProceduralAnimationRigController(fpParent.gameObject, fpAnimObj.gameObject, $"{controllerName}_FP");
			this.rigControllerFP.Build();

			SetFirstPersonRotation(firstPersonBuilder);
		}

		this.INIT = true;
	}


	// Plays bone animation
	public void Play(BoneAnimationRequest request){
		if(!this.INIT)
			return;

		bool found = false;
		int stateHash = Animator.StringToHash(request.name);

		if(this.stateMappings.ContainsKey(request)){
			for(int i=0; i < this.stateMappings[request].Count; i++){
				if(HandleBoneRequest(this.stateMappings[request][i])){
					found = true;
					break;
				}
			}
			if(!found){
				this.tpAnimator.CrossFade(request.name, 0.1f, layer:this.tpAnimator.GetLayerIndex(request.layer));
			}
		}
		else{
			this.tpAnimator.CrossFade(request.name, 0.1f, layer:this.tpAnimator.GetLayerIndex(request.layer));
		}

		if(this.isPlayer){
			if(this.fpAnimator.HasState(this.tpAnimator.GetLayerIndex(request.layer), stateHash)){
				this.fpAnimator.CrossFade(request.name, 0.1f);
			}
			else{
				this.fpAnimator.CrossFade("Empty", 0.1f);
			}
		}
	}

	// Plays/Stops/Registers ShapeKey Animations based on the settings inputted
	public void Play(string shapeKey, ShapeKeyAnimationSettings settings){
		if(!this.INIT)
			return;

		this.shapeKeyAnimator.Play(shapeKey, settings);
	}

	// Looks for every Layer to find if the current playing State is StateName and return the normalizedTime
	// Return -1 if no state like that is found
	public float GetAnimationTime(string stateName){
		AnimatorStateInfo stateInfo;

		for(int i=0; i < this.tpAnimator.layerCount; i++){
			stateInfo = this.tpAnimator.GetCurrentAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}

			stateInfo = this.tpAnimator.GetNextAnimatorStateInfo(i);

			if(stateInfo.IsName(stateName)){
				return stateInfo.normalizedTime;
			}
		}

		return -1f;

	}

	public void AssignAimTracker(Transform tracker){
		this.rigControllerTP.AssignHeadTrackingSource(tracker);
		this.rigControllerFP.AssignHeadTrackingSource(tracker);
	}

	public Animator GetThirdPersonAnimator(){return this.tpAnimator;}
	public Animator GetFirstPersonAnimator(){return this.fpAnimator;}

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