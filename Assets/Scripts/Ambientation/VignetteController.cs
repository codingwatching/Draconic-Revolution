using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/*
	Blending between different local vignette effects is not possible in Unity
	Find another solution for later, when multiple vignette effect are active
*/
public class VignetteController : MonoBehaviour {
	// Unity Reference
	public AmbientHandler ambientHandler;
	private Transform volumeParent;

	private readonly int size = 3;

	private Volume[] volumes;
	private GameObject[] volumeHolder;
	private Vignette[] vignettes;
	private VignetteData[] data;
	private bool[] volumeUsage;
	private bool[] stopCoroutine;

	void Start(){
		this.volumeParent = this.gameObject.transform;
		this.volumes = new Volume[size];
		this.volumeHolder = new GameObject[size];
		this.vignettes = new Vignette[size];
		this.data = new VignetteData[size];
		this.volumeUsage = new bool[size];
		this.stopCoroutine = new bool[size];

		InitializeArrays(size);
	}

	void OnDestroy(){
		for(int i=0; i < this.volumeHolder.Length; i++){
			GameObject.Destroy(this.volumeHolder[i]);
		}
	}

	public void Add(VignetteData vdata){
		int index = Index(vdata.vignetteEffectName);

		if(index < 0){
			index = FindFree(vdata);

			if(index < 0)
				return;

			this.data[index] = vdata;
			this.volumeUsage[index] = true;
		}
		else{
			this.stopCoroutine[index] = true;
		}

		StartCoroutine(AddCoroutine(vdata, index));
	}

	public void Remove(VignetteData vdata){
		int index = Index(vdata.vignetteEffectName);

		if(index < 0)
			return;

		if(this.volumes[index].weight != 1){
			this.stopCoroutine[index] = true;
		}

		StartCoroutine(RemoveCoroutine(vdata, index));
	}

    private IEnumerator AddCoroutine(VignetteData data, int index){
    	int timeout = 0;

    	// Waits for coroutine on the same slot to halt
    	while(this.stopCoroutine[index]){
    		timeout++;

    		// Adds a timeout
    		if(timeout >= 20)
    			yield break;

    		yield return null;
    	}

        float elapsed = this.volumes[index].weight * data.effectTime;
        float step;

        this.vignettes[index].color.value = data.color;
        this.vignettes[index].intensity.value = data.intensity;
        this.vignettes[index].smoothness.value = data.smoothness;
        this.vignettes[index].center.value = data.center;
        this.vignettes[index].roundness.value = data.roundness;

        while(elapsed < data.effectTime && !this.stopCoroutine[index]){
        	elapsed += Time.deltaTime;
        	step = elapsed / data.effectTime;

        	if(elapsed > data.effectTime){step = 1f;}

        	this.volumes[index].weight = step;

			yield return null;
        }

        this.stopCoroutine[index] = false;
    }

    private IEnumerator RemoveCoroutine(VignetteData data, int index){
    	int timeout = 0;

    	// Waits for coroutine on the same slot to halt
    	while(this.stopCoroutine[index]){
    		timeout++;

    		// Adds a timeout
    		if(timeout >= 20)
    			yield break;

    		yield return null;
    	}

        float elapsed = (1 - this.volumes[index].weight) * data.effectTime;
        float step;

        while(elapsed < data.effectTime && !this.stopCoroutine[index]){
        	elapsed += Time.deltaTime;
        	step = elapsed / data.effectTime;

        	if(elapsed > data.effectTime){step = 1f;}

        	this.volumes[index].weight = 1 - step;

			yield return null;
        }

        if(!this.stopCoroutine[index])
        	this.volumeUsage[index] = false;

        this.stopCoroutine[index] = false;
    }

	private bool Contains(string vignetteName){
		for(int i=0; i < this.vignettes.Length; i++){
			if(this.data[i].vignetteEffectName == vignetteName && this.volumeUsage[i])
				return true;
		}

		return false;
	}

	private int FindFree(VignetteData vdata){
		int index = Index(vdata.vignetteEffectName);

		if(index == -1){
			for(int i=0; i < this.data.Length; i++){
				if(!this.volumeUsage[i])
					return i;
			}
			return -1;
		}
		else{
			return -2;
		}
	}

	private int Index(string vignetteName){
		for(int i=0; i < this.vignettes.Length; i++){
			if(this.data[i].vignetteEffectName == vignetteName && this.volumeUsage[i])
				return i;
		}

		return -1;
	}

	private void InitializeArrays(int size){
		for(int i=0; i < size; i++){
			this.volumeHolder[i] = new GameObject();
			this.volumeHolder[i].name = $"Vignette Slot {i+1}";
			this.volumeHolder[i].transform.SetParent(this.volumeParent);
			this.volumeHolder[i].transform.localPosition = Vector3.zero;
			this.volumeHolder[i].transform.eulerAngles = Vector3.zero;
			this.volumeHolder[i].AddComponent<BoxCollider>();

			this.volumes[i] = this.volumeHolder[i].AddComponent<Volume>();
			this.volumes[i].isGlobal = false;
			this.volumes[i].priority = 1 + i;
			this.volumes[i].profile = ScriptableObject.CreateInstance<VolumeProfile>();
			this.volumes[i].weight = 0f;

			this.vignettes[i] = this.volumes[i].profile.Add<Vignette>(true);

			this.data[i] = VignetteData.DEFAULT;
			this.volumeUsage[i] = false;
			this.stopCoroutine[i] = false;
		}
	}
}