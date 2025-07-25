using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class PlayerSheetController : MonoBehaviour {
	private CharacterSheet sheet;
	private Light characterLight;
	private HDAdditionalLightData HDRPLightData;
	private RealisticLight realisticLight;

	private GameObject middle;

	private float voxelLightIntensity = 0f;

	void Awake(){
		this.middle = this.gameObject.transform.Find("MiddlePoint").gameObject;

		this.characterLight = middle.AddComponent<Light>();
		this.HDRPLightData = middle.AddComponent<HDAdditionalLightData>();
		this.realisticLight = middle.AddComponent<RealisticLight>();

		this.characterLight.enabled = false;
		this.HDRPLightData.enabled = false;
		this.realisticLight.enabled = false;
	}



	public void SetSheet(CharacterSheet sheet){
		this.sheet = sheet;
	}

	public float GetVoxelLightIntensity(){return this.voxelLightIntensity;}

	public void SetVoxelLightIntensity(float intensity){
		this.voxelLightIntensity = intensity;
	}

	public CharacterSheet GetSheet(){return this.sheet;}

	public bool IsEnabled(){return this.characterLight.enabled;}

	public void Enable(bool realisticLight){
		this.characterLight.enabled = true;
		this.HDRPLightData.enabled = true;

		if(realisticLight)
			this.realisticLight.enabled = true;
	}

	public void Disable(bool realisticLight){
		this.characterLight.enabled = false;
		this.HDRPLightData.enabled = false;

		if(realisticLight)
			this.realisticLight.enabled = false;
	}

	public Light GetLight(){return this.characterLight;}
	public HDAdditionalLightData GetLightData(){return this.HDRPLightData;}
	public RealisticLight GetRealisticLight(){return this.realisticLight;}
}