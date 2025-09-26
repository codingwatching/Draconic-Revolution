using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[Serializable]
public class MultiAimData{
	public string rig_name;
	public string constrained_bone;
	public string[] active_states;
	public Vector2 limits;
	public bool constrainedXAxis;
	public bool constrainedYAxis;
	public bool constrainedZAxis;
	public bool maintain_offset;
	public float intensity;
	public Vector3 offset;

	private Transform constrainedObject;
	private HashSet<string> validStates;

	public bool HasState(string state){return this.validStates.Contains(state);}

	public void PostDeserializationSetup(){
		this.validStates = new HashSet<string>(this.active_states);
	}

	public MultiAimConstraint BuildConstraint(Transform armature, GameObject rigObject, Transform eyeTracker){
		MultiAimConstraint constraint = rigObject.AddComponent<MultiAimConstraint>();
		MultiAimConstraintData data = constraint.data;
		WeightedTransformArray arr = new WeightedTransformArray();

		data = new MultiAimConstraintData{
			constrainedObject = armature.Find(this.constrained_bone),
			constrainedXAxis = this.constrainedXAxis,
			constrainedYAxis = this.constrainedYAxis,
			constrainedZAxis = this.constrainedZAxis,
			maintainOffset = this.maintain_offset,
			offset = this.offset,
			limits = this.limits,
			aimAxis = MultiAimConstraintData.Axis.Z,
			upAxis = MultiAimConstraintData.Axis.Y,
			worldUpType = MultiAimConstraintData.WorldUpType.Vector,
			worldUpAxis = MultiAimConstraintData.Axis.Y
		};

		arr.Add(new WeightedTransform(eyeTracker, 1-intensity));
		data.sourceObjects = arr;
		constraint.data = data;

		return constraint;
	}
}