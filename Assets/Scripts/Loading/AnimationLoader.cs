using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationLoader : BaseLoader {
	private static Dictionary<string, RuntimeAnimatorController> controllers = new Dictionary<string, RuntimeAnimatorController>();
	private static Dictionary<string, AnimationStateMapping[]> stateMappings = new Dictionary<string, AnimationStateMapping[]>();
	private static Dictionary<string, MultiAimData[]> rigs = new Dictionary<string, MultiAimData[]>();
	private static Dictionary<string, BattleStyleData> battleStyles = new Dictionary<string, BattleStyleData>();
	private static Dictionary<string, string> armatureName = new Dictionary<string, string>();
	private static bool isClient;

	private static readonly string CONTROLLERS_PATHS = "SerializedData/AnimatorControllers";
	private static readonly string ANIMATION_RESFOLDER = "Animations/";
	private static readonly string BATTLE_STYLE_RESFOLDER = "BattleStyles/";
	public static readonly string ANIMATION_CLIP_RESFOLDER = "AnimationClips/";

	
	public AnimationLoader(bool isClient){AnimationLoader.isClient = isClient;}

	public override bool Load(){
		if(isClient){
			LoadCharacterControllers();
			LoadStateMappings();
			LoadRigs();
			LoadArmatureName();
			LoadBattleStyles();
		}

		return true;
	}

	public static RuntimeAnimatorController GetController(string controller){return controllers[controller];}
	public static AnimationStateMapping[] GetAnimationMapping(string controller){return stateMappings[controller];}
	public static MultiAimData[] GetRig(string controller){return rigs[controller];}
	public static bool ContainsRig(string controller){return rigs.ContainsKey(controller);}
	public static string GetArmatureName(string controller){return armatureName[controller];}
	public static BattleStyleData GetBattleStyle(string styleName){return battleStyles[styleName];}

	private void LoadArmatureName(){
		string respath;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/armature";

			TextAsset armature = Resources.Load<TextAsset>(respath);

			if(armature == null){
				throw new AnimationImportException($"Couldn't locate the Armature Name: {respath} while loading Animations");
			}

			armatureName.Add(controllerName, armature.text);
		}	
	}

	private void LoadRigs(){
		string respath;
		Wrapper<MultiAimData> wrapper;

		foreach(string controllerName in controllers.Keys){
			respath = $"{ANIMATION_RESFOLDER}{controllerName}/rigs";

			TextAsset rigsJson = Resources.Load<TextAsset>(respath);

			if(rigsJson == null){
				throw new AnimationImportException($"Couldn't locate the Rigs: {respath} while loading Animations");
			}

			wrapper = JsonUtility.FromJson<Wrapper<MultiAimData>>(rigsJson.text);

			foreach(MultiAimData rig in wrapper.data){
				rig.PostDeserializationSetup();
			}

			rigs.Add(controllerName, wrapper.data);
		}		
	}

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

	private void LoadBattleStyles(){
		BattleStyleData bsd;
        TextAsset[] assets = Resources.LoadAll<TextAsset>(BATTLE_STYLE_RESFOLDER);

        foreach(TextAsset asset in assets){
        	bsd = JsonUtility.FromJson<BattleStyleData>(asset.text);
        	bsd.PostDeserializationSetup();
			battleStyles.Add(asset.name, bsd);
        }
	}
}