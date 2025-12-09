using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionController : MonoBehaviour {
	
	// Unity Reference
	private AnimationHandler animationHandler;
	private Animator animator;
	private Animator animatorFP;

	// Flags
	private bool INIT = false;

	// Original Animators
	private RuntimeAnimatorController originalController;
	private RuntimeAnimatorController originalControllerFP;

	// Battle Style
	private string currentStyleName;
	private BattleStyleData currentStyle;
	private bool weaponSheathed = true;
	private int comboHit = 0;

	// Default Config
	private float hitWindowStart = .48f;
	private float attackExitTime = .8f;
	private HashSet<PlayerActionType> registeredAction;

	// Cache
	private float cachedTime;

	void Update(){
		if(!this.INIT)
			return;

		ResetCombo();

		if(this.registeredAction.Contains(PlayerActionType.PRIMARY_ACTION)){
			if(this.comboHit >= 1){
				this.cachedTime = this.animationHandler.GetAnimationTime($"Attack {this.comboHit}");

				if(this.cachedTime != -1f){
					if(this.cachedTime >= this.hitWindowStart && this.cachedTime < this.attackExitTime && this.comboHit < this.currentStyle.GetComboHits()){
						this.comboHit++;
						this.animator.SetInteger("Attack_Combo", this.comboHit);
						this.animatorFP.SetInteger("Attack_Combo", this.comboHit);
						this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
					}
				}
			}
			else if(this.comboHit == 0){
				this.comboHit++;
				this.animator.SetInteger("Attack_Combo", this.comboHit);
				this.animatorFP.SetInteger("Attack_Combo", this.comboHit);
				this.animationHandler.Play(new BoneAnimationRequest($"Attack {this.comboHit}", ""));
				this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
			}
		}
	}


	public void Init(){
		if(this.INIT)
			return;

		this.INIT = true;
		this.animationHandler = this.gameObject.GetComponent<AnimationHandler>();
		this.animator = this.animationHandler.GetThirdPersonAnimator();
		this.animatorFP = this.animationHandler.GetFirstPersonAnimator();
		this.originalController = this.animator.runtimeAnimatorController;
		this.originalControllerFP = this.animatorFP.runtimeAnimatorController;
		this.registeredAction = new HashSet<PlayerActionType>();
	}

	public void UseStyle(string styleName){
		if(!this.INIT)
			Init();

		if(this.currentStyleName == styleName)
			return;

		this.currentStyle = AnimationLoader.GetBattleStyle(styleName);

		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(this.originalController);
		AnimatorOverrideController animationOverrideControllerFP = new AnimatorOverrideController(this.originalControllerFP);

		animationOverrideController = ApplyOverrides(animationOverrideController, this.currentStyle.GetOverrides());
		animationOverrideControllerFP = ApplyOverrides(animationOverrideControllerFP, AnimationLoader.GetBattleStyle($"{styleName}-FP").GetOverrides());

		this.animator.runtimeAnimatorController = animationOverrideController;
		this.animatorFP.runtimeAnimatorController = animationOverrideControllerFP;
	}

	public void RemoveAllStyles(){
		this.animator.runtimeAnimatorController = this.originalController;
		this.animatorFP.runtimeAnimatorController = this.originalControllerFP;
	}

	public void Sheathe(){
		this.weaponSheathed = !this.weaponSheathed;

		if(this.weaponSheathed){
			this.animationHandler.Play(new BoneAnimationRequest("Idle", ""));
			this.comboHit = 0;
		}
		else
			this.animationHandler.Play(new BoneAnimationRequest("Idle Hand", ""));
	}

	// Registers a primary action
	public void RegisterPrimaryAction(){this.registeredAction.Add(PlayerActionType.PRIMARY_ACTION);}

	public void VerifyMovement(Vector3 facingDirection, Vector3 movementDirection, float momentum, MovementFlags flags){
		int direction = 0;
		float angle;

		if(movementDirection.magnitude == 0){
			this.animator.SetBool("Stop_Movement", true);
		}
		else{
			this.animator.SetBool("Stop_Movement", false);
		}

		this.animator.SetBool("Is_Grounded", flags.isGrounded);
		this.animator.SetBool("Is_Sheathed", this.weaponSheathed);

		angle = Vector3.SignedAngle(facingDirection, movementDirection, Vector3.up);
		
		if(movementDirection.magnitude == 0)
			direction = 0;
		else if(Mathf.Abs(angle) >= 180)
			direction = 2;
		else if(Mathf.Abs(angle) <= 50)
			direction = 1;
		else if(angle > 0)
			direction = 3;
		else
			direction = 4;

		this.animator.SetInteger("Direction", direction);
	}

	// Used only in Menu
	public static void UseStyle(Animator animator, string styleName, bool isMale){
		if(isMale)
			styleName = $"{styleName}-Man";
		else
			styleName = $"{styleName}-Woman";

		AnimatorOverrideController animationOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
		animationOverrideController = ApplyOverrides(animationOverrideController, AnimationLoader.GetBattleStyle(styleName).GetOverrides());
		animator.runtimeAnimatorController = animationOverrideController;
	}

	private static AnimatorOverrideController ApplyOverrides(AnimatorOverrideController controller, StateClipPair[] overrides){
		foreach(StateClipPair over in overrides){
			controller[Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{over.state}")] = Resources.Load<AnimationClip>($"{AnimationLoader.ANIMATION_CLIP_RESFOLDER}{over.clip}");
		}

		return controller;
	}

	private void ResetMovement(){
	}

	private void ResetCombo(){
		if(this.comboHit >= 1){
			this.cachedTime = this.animationHandler.GetAnimationTime($"Attack {this.comboHit}");

			if(this.cachedTime == -1){
				this.comboHit = 0;
				this.registeredAction.Remove(PlayerActionType.PRIMARY_ACTION);
			}
		}
	}
}