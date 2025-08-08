using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationLoader : BaseLoader {
	private static Dictionary<string, RuntimeAnimatorController> controllers = new Dictionary<string, RuntimeAnimatorController>();
	private static Dictionary<string, AnimationStateMapping[]> stateMappings = new Dictionary<string, AnimationStateMapping[]>();
	private static bool isClient;

	private static readonly string CONTROLLERS_PATHS = "SerializedData/AnimatorControllers";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";


	public AnimationLoader(bool isClient){AnimationLoader.isClient = isClient;}

	public override bool Load(){
		if(isClient){
			LoadCharacterControllers();
			LoadStateMappings();
		}

		return true;
	}

	public static RuntimeAnimatorController GetController(string controller){return controllers[controller];}
	public static AnimationStateMapping[] GetAnimationMapping(string controller){return stateMappings[controller];}

	private void LoadCharacterControllers(){
		RuntimeAnimatorController currentController;

		TextAsset controllerJson = Resources.Load<TextAsset>(CONTROLLERS_PATHS);

		if(controllerJson == null){
			throw new AnimationImportException($"Couldn't locate the AnimatorController Mappings in RESPATH: {CONTROLLERS_PATHS} while loading RuntimeAnimatorController");
		}

		Wrapper<ValuePair<string, string>> wrapper = JsonUtility.FromJson<Wrapper<ValuePair<string, string>>>(controllerJson.text);

		foreach(ValuePair<string, string> vp in wrapper.data){
			currentController = Resources.Load<RuntimeAnimatorController>(vp.value);

			if(currentController == null){
				throw new AnimationImportException($"AnimatorController was not found in Resources Path: {vp.value}");
			}

			controllers.Add(vp.key, currentController);
		}
	}

	private void LoadStateMappings(){
		string respath;
		Wrapper<AnimationStateMapping> wrapper;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/mappings";

			TextAsset mappingJson = Resources.Load<TextAsset>(respath);

			if(mappingJson == null){
				throw new AnimationImportException($"Couldn't locate the AnimationMapping: {respath} while loading Animations");
			}

			wrapper = JsonUtility.FromJson<Wrapper<AnimationStateMapping>>(mappingJson.text);

			foreach(AnimationStateMapping mapping in wrapper.data){
				mapping.PostDeserializationSetup();
			}

			stateMappings.Add(controllerName, wrapper.data);
		}
	}
}