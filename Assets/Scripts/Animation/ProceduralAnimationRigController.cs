using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ProceduralAnimationRigController {
	private string controllerName;
	private string currentState;
	private GameObject parent;
	private Transform eyeTracker;
	private Transform armature;
	private GameObject proceduralRig;
	private RigBuilder rigBuilder;
	private MultiAimData[] multiAimData;
	private List<MultiAimConstraint> multiAimConstraints;

	public ProceduralAnimationRigController(GameObject characterObject, string controllerName){
		this.parent = characterObject;
		this.armature = this.parent.transform.Find(AnimationLoader.GetArmatureName(controllerName));
		this.controllerName = controllerName;
		this.multiAimConstraints = new List<MultiAimConstraint>();

		GenerateEyeTrackerObject();
	}

	public void ChangeState(string state){
		if(currentState == state)
			return;

		for(int i=0; i < this.multiAimConstraints.Count; i++){
			if(this.multiAimData[i].HasState(state))
				this.multiAimConstraints[i].weight = 1f;
			else
				this.multiAimConstraints[i].weight = 0f;
		}
	}

	public void AssignHeadTrackingSource(Transform t){
		WeightedTransformArray wta;
		MultiAimConstraintData data;

		for(int i=0; i < this.multiAimData.Length; i++){
			if(this.multiAimData[i].intensity == 0)
				continue;

			data = this.multiAimConstraints[i].data;
			wta = data.sourceObjects;

			if(wta.Count > 1){
				wta.RemoveAt(1);
			}

			wta.Add(new WeightedTransform(t, this.multiAimData[i].intensity));
			data.sourceObjects = wta;
			this.multiAimConstraints[i].data = data;
		}
	}

	public void Build(){
		if(!AnimationLoader.ContainsRig(this.controllerName))
			return;

		MultiAimConstraint current;
		Rig rig;

		this.proceduralRig = new GameObject();
		this.proceduralRig.name = "Procedural Rig";
		this.proceduralRig.transform.parent = this.parent.transform;
		rig = this.proceduralRig.AddComponent<Rig>();
		this.rigBuilder = this.parent.AddComponent<RigBuilder>();
		this.multiAimData = AnimationLoader.GetRig(this.controllerName);

		for(int i=0; i < this.multiAimData.Length; i++){
			GameObject go = new GameObject();
			go.name = multiAimData[i].rig_name;
			go.transform.parent = this.proceduralRig.transform;
			current = multiAimData[i].BuildConstraint(this.armature, go, this.eyeTracker);
			this.multiAimConstraints.Add(current);
		}

		this.rigBuilder.layers.Add(new RigLayer(rig));
		this.rigBuilder.Build();
	}

	private void GenerateEyeTrackerObject(){
		GameObject go = new GameObject();

		go.name = "Eye Tracker";
		go.transform.parent = this.parent.transform.Find("Camera");
		go.transform.localPosition = new Vector3(0,0,10);

		this.eyeTracker = go.transform;
	}
}
