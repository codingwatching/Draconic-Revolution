using System;
using UnityEngine;

public class PlayerModelHandler : MonoBehaviour {
	public GameObject parent;
	private Animator animator;
	private CharacterController controller;

	[Header("Animations")]
	public RuntimeAnimatorController maleAnimations;
	public RuntimeAnimatorController femaleAnimations;

	[Header("Materials")]
	public Material plainClothingMaterial;
	public Material dragonSkinMaterial;
	public Material eyeMaterial;
	public Material dragonHornMaterial;

	private CharacterBuilder characterBuilder;

	private static readonly Quaternion ROTATION = Quaternion.Euler(new Vector3(0, -90, 0));


	public void Awake(){
		this.animator = this.parent.AddComponent<Animator>();

		this.controller = this.parent.GetComponent<CharacterController>();

		if(this.controller == null){
			this.controller = this.parent.AddComponent<CharacterController>();
		}
	}


	// Builds any character other than Player
	public GameObject BuildModel(GameObject go, CharacterAppearance app, bool isMale){
		this.animator = go.GetComponent<Animator>();

		if(this.animator == null){
			this.animator = go.AddComponent<Animator>();
		}

		if(isMale)
			this.characterBuilder = new CharacterBuilder(go, this.maleAnimations, app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, false);
		else
			this.characterBuilder = new CharacterBuilder(go, this.femaleAnimations, app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, false);

		this.characterBuilder.Build();
		Rescale(app.race, go);

		return go;
	}

	// Builds player character
	public void BuildModel(CharacterAppearance app, bool isMale, bool isPlayerCharacter){
		if(this.characterBuilder == null){
			if(isMale)
				this.characterBuilder = new CharacterBuilder(this.parent, this.maleAnimations, app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, isPlayerCharacter);
			else
				this.characterBuilder = new CharacterBuilder(this.parent, this.femaleAnimations, app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, isPlayerCharacter);

			this.characterBuilder.Build();
		}
		else{
			this.characterBuilder.ChangeAppearanceAndBuild(app);
		}

		Rescale(app.race, this.parent);
	}

	private void Rescale(Race r, GameObject go){
		switch(r){
			case Race.DWARF:
				go.transform.localScale = RaceManager.GetSettings(Race.DWARF).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			case Race.HALFLING:
				go.transform.localScale = RaceManager.GetSettings(Race.HALFLING).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
			default:
				go.transform.localScale = RaceManager.GetSettings(Race.HUMAN).scaling * Constants.PLAYER_MODEL_SCALING_FACTOR;
				break;
		}
	}
}