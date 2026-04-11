using System;
using UnityEngine;

[Serializable]
public struct VignetteData{
	public string vignetteEffectName;
	public Color color;
	public Vector2 center;
	public float intensity;
	public float smoothness;
	public float roundness;
	public float effectTime;

	public static VignetteData DEFAULT = new VignetteData{
		vignetteEffectName = "default",
		color = Color.black,
		center = new Vector2(-2f, -2f),
		intensity = 0f,
		smoothness = 1f,
		roundness = 1f,
		effectTime = 1f
	};
}