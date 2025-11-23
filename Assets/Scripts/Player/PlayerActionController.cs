using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionController : MonoBehaviour {
	
	// Unity Reference
	private AnimationHandler animationHandler;
	private Animator animator;
	private Animator animatorFP;

	// Flags
	private bool INIT = false;

	// Cache
	private RuntimeAnimatorController originalController;
	private RuntimeAnimatorController originalControllerFP;

	// Battle Style
	private string currentStyle;


	public void Init(){
		if(this.INIT)
			return;

		this.INIT = true;
		this.animationHandler = this.gameObject.GetComponent<AnimationHandler>();
		this.animator = this.animationHandler.GetThirdPersonAnimator();
		this.animatorFP = this.animationHandler.GetFirstPersonAnimator();
		this.originalController = this.animator.runtimeAnimatorController;
		this.originalControllerFP = this.animatorFP.runtimeAnimatorController;
	}

	public void UseStyle(string styleName){
		if(!this.INIT)
			Init();

		if(this.currentStyle == styleName)
			return;

		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(this.originalController);
		AnimatorOverrideController animationOverrideControllerFP = new AnimatorOverrideController(this.originalControllerFP);

		animationOverrideController = ApplyOverrides(animationOverrideController, AnimationLoader.GetBattleStyleOverrides(styleName));
		animationOverrideControllerFP = ApplyOverrides(animationOverrideControllerFP, AnimationLoader.GetBattleStyleOverrides($"{styleName}-FP"));

		this.animator.runtimeAnimatorController = animationOverrideController;
		this.animatorFP.runtimeAnimatorController = animationOverrideControllerFP;
	}

	public void RemoveAllStyles(){
		this.animator.runtimeAnimatorController = this.originalController;
		this.animatorFP.runtimeAnimatorController = this.originalControllerFP;
	}

	private AnimatorOverrideController ApplyOverrides(AnimatorOverrideController controller, StateClipPair<string, string>[] overrides){
		foreach(StateClipPair<string, string> over in overrides){
			controller[Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{over.state}")] = Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{over.clip}");
		}

		return controller;
	}
}