using System;
using UnityEngine;

public class PlayerModelHandler : MonoBehaviour {
	public GameObject parent;
	private CharacterController controller;
	private AnimationHandler animationHandler;
	private bool isMale;

	[Header("Materials")]
	public Material plainClothingMaterial;
	public Material dragonSkinMaterial;
	public Material eyeMaterial;
	public Material dragonHornMaterial;

	private CharacterBuilder characterBuilder;

	private static readonly Quaternion ROTATION = Quaternion.Euler(new Vector3(0, -90, 0));


	public void Awake(){
		this.animationHandler = this.parent.AddComponent<AnimationHandler>();

		this.controller = this.parent.GetComponent<CharacterController>();

		if(this.controller == null){
			this.controller = this.parent.AddComponent<CharacterController>();
		}
	}

	public void Start(){
		if(this.isMale)
			this.animationHandler.Init("BASE_Character_Man", isUserCharacter:true);
		else
			this.animationHandler.Init("BASE_Character_Woman", isUserCharacter:true);
	}


	// Builds any character other than Player
	public GameObject BuildModel(GameObject go, CharacterAppearance app, bool isMale){
		if(isMale)
			this.characterBuilder = new CharacterBuilder(go, AnimationLoader.GetController("BASE_Character_Man"), AnimationLoader.GetController("BASE_Character_Man_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, false);
		else
			this.characterBuilder = new CharacterBuilder(go, AnimationLoader.GetController("BASE_Character_Woman"), AnimationLoader.GetController("BASE_Character_Woman_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, false);

		this.characterBuilder.Build();
		Rescale(app.race, go);

		return go;
	}

	// Builds player character
	public void BuildModel(CharacterAppearance app, bool isMale, bool isPlayerCharacter){
		this.isMale = isMale;

		if(this.characterBuilder == null){
			if(isMale){
				this.characterBuilder = new CharacterBuilder(this.parent, AnimationLoader.GetController("BASE_Character_Man"), AnimationLoader.GetController("BASE_Character_Man_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, isPlayerCharacter);
			}
			else{
				this.characterBuilder = new CharacterBuilder(this.parent, AnimationLoader.GetController("BASE_Character_Woman"), AnimationLoader.GetController("BASE_Character_Woman_FP"), app, this.plainClothingMaterial, this.dragonHornMaterial, this.dragonSkinMaterial, this.eyeMaterial, isMale, isPlayerCharacter);
			}

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