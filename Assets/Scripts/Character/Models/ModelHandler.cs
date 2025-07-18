using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ModelHandler{
	private static GameObject assets;

	private static Dictionary<ModelType, Dictionary<string, ModelInfo>> models = new Dictionary<ModelType, Dictionary<string, ModelInfo>>();
	private static BiMap<ushort, string> clothesMap = new BiMap<ushort, string>(); 
	private static BiMap<ushort, string> legsMap = new BiMap<ushort, string>(); 
	private static BiMap<ushort, string> bootsMap = new BiMap<ushort, string>(); 
	private static BiMap<ushort, string> hatsMap = new BiMap<ushort, string>(); 
	private static BiMap<ushort, string> faceMap = new BiMap<ushort, string>();
	private static BiMap<ushort, string> hairMap = new BiMap<ushort, string>();
	private static BiMap<ushort, string> addonMap = new BiMap<ushort, string>();
	private static BiMap<ushort, string> essentialsMap = new BiMap<ushort, string>();

	private static readonly string ASSET_BUNDLE_RESPATH = "CharacterModels/characters";
	private static readonly string CLOTHES_DB = "CharacterModels/clothes_db";
	private static readonly string LEGS_DB = "CharacterModels/legs_db";
	private static readonly string BOOTS_DB = "CharacterModels/boots_db";
	private static readonly string HATS_DB = "CharacterModels/hats_db";
	private static readonly string HAIR_DB = "CharacterModels/hair_db";
	private static readonly string FACE_DB = "CharacterModels/faces_db";
	private static readonly string ADDONS_DB = "CharacterModels/addons_db";
	private static readonly string ESSENTIALS_DB = "CharacterModels/essentials_db";
	private static readonly string ARMATURE_MALE = "ManArmt";
	private static readonly string ARMATURE_FEMALE = "WomanArmt";

	private static readonly Quaternion ROTATION = Quaternion.Euler(0, -90, 0);

	private static TextAsset cachedText;



	static ModelHandler(){
		assets = Resources.Load<GameObject>(ASSET_BUNDLE_RESPATH);
		assets = GameObject.Instantiate(assets);
		assets.transform.position = new Vector3(0,-999999,0);
		assets.name = "ModelAssets";

		LoadModelInfo();

		GameObject.DontDestroyOnLoad(assets);
	}

	public static void Run(){}

	public static GameObject GetModelObject(ModelType type, string name){
		ModelInfo mi = GetModelInfo(type, name);
		return GameObject.Instantiate(GameObject.Find("ModelAssets/" + mi.blenderReference));
	}

	public static List<Vector3> GetVertices(ModelType type, string name){
		List<Vector3> verts = new List<Vector3>();

		ModelInfo mi = GetModelInfo(type, name);
		GameObject go = GameObject.Instantiate(GameObject.Find("ModelAssets/" + mi.blenderReference));

		Mesh mesh = go.GetComponent<SkinnedMeshRenderer>().sharedMesh;
		mesh.GetVertices(verts);

		return verts;
	}

	public static bool HasModel(ModelType type, string name){
		return GetModelInfo(type, name).hasModel;
	}

	public static char GetHatCover(ModelType type, string name){
		if(type != ModelType.HEADGEAR)
			return ' ';
		return GetModelInfo(type, name).coverHair;
	}

	public static GameObject GetModelByCode(ModelType type, ushort code){
		switch(type){
			case ModelType.CLOTHES:
				return GetModelObject(type, clothesMap.Get(code));
			case ModelType.LEGS:
				return GetModelObject(type, legsMap.Get(code));
			case ModelType.FOOTGEAR:
				return GetModelObject(type, bootsMap.Get(code));
			case ModelType.HEADGEAR:
				return GetModelObject(type, hatsMap.Get(code));
			case ModelType.HAIR:
				return GetModelObject(type, hairMap.Get(code));
			case ModelType.FACE:
				return GetModelObject(type, faceMap.Get(code));
			case ModelType.ADDON:
				return GetModelObject(type, addonMap.Get(code));
			default:
				return GetModelObject(type, clothesMap.Get(code));
		}
	}

	public static string GetModelName(ModelType type, ushort code){
		switch(type){
			case ModelType.CLOTHES:
				return clothesMap.Get(code).Split("/")[0];
			case ModelType.LEGS:
				return legsMap.Get(code).Split("/")[0];
			case ModelType.FOOTGEAR:
				return bootsMap.Get(code).Split("/")[0];
			case ModelType.HEADGEAR:
				return hatsMap.Get(code).Split("/")[0];
			case ModelType.HAIR:
				return hairMap.Get(code).Split("/")[0];
			case ModelType.FACE:
				return faceMap.Get(code).Split("/")[0];
			case ModelType.ADDON:
				return addonMap.Get(code).Split("/")[0];
			default:
				return "";
		}
	}

	public static GameObject GetArmature(bool isMale=true, bool rotated=false){
		if(isMale){
			if(rotated)
				return GameObject.Instantiate(GameObject.Find("ModelAssets/" + ARMATURE_MALE), Vector3.zero, ROTATION);
			else
				return GameObject.Instantiate(GameObject.Find("ModelAssets/" + ARMATURE_MALE));
		}
		else{
			if(rotated)
				return GameObject.Instantiate(GameObject.Find("ModelAssets/" + ARMATURE_FEMALE), Vector3.zero, ROTATION);
			else
				return GameObject.Instantiate(GameObject.Find("ModelAssets/" + ARMATURE_FEMALE));
		}
	}

	public static ModelInfo GetModelInfo(ModelType type, string name){
		return models[type][name];
	}

	public static List<ModelInfo> GetAllModelInfoList(ModelType t, bool filterByGender=false, char gender = 'M'){
		List<ModelInfo> outList = new List<ModelInfo>();
		
		foreach(ModelInfo mi in models[t].Values){
			outList.Add(mi);
		}

		// Filters
		if(filterByGender)
			outList = FilterGenderModels(outList, gender);

		return outList;
	}

	public static List<ModelInfo> FilterGenderModels(List<ModelInfo> modelList, char gender){
		List<ModelInfo> outList = new List<ModelInfo>();
		bool isMale = (gender == 'M'); 

		foreach(ModelInfo mi in modelList){
			if(isMale){
				if(mi.sex == 'M'){
					outList.Add(mi);
				}
			}
			else{
				if(mi.sex == 'F'){
					outList.Add(mi);
				}
			}
		}

		return outList;
	}

	public static Transform[] GetArmatureBones(Transform armature, Dictionary<string, int> mapping){
		List<Transform> listTransforms = new List<Transform>();
		Transform[] array = new Transform[mapping.Count];

		// Gets Hips 
		armature.GetComponentsInChildren<Transform>(listTransforms);
		
		foreach(Transform t in listTransforms){
			if(!t.name.EndsWith("end") && t.name != "Armature"){
				if(mapping.ContainsKey(t.name)){
					array[mapping[t.name]] = t;
				}
			}
		}

		return array;
	}

	public static ushort GetCode(ModelType t, string name){
		switch(t){
			case ModelType.CLOTHES:
				return clothesMap.Get(name);
			case ModelType.LEGS:
				return legsMap.Get(name);
			case ModelType.FOOTGEAR:
				return bootsMap.Get(name);
			case ModelType.HEADGEAR:
				return hatsMap.Get(name);
			case ModelType.HAIR:
				return hairMap.Get(name);
			case ModelType.FACE:
				return faceMap.Get(name);
			case ModelType.ADDON:
				return addonMap.Get(name);
			default:
				return 0;
		}
	}

	public static string GetName(ModelType t, ushort code){
		switch(t){
			case ModelType.CLOTHES:
				return clothesMap.Get(code);
			case ModelType.LEGS:
				return legsMap.Get(code);
			case ModelType.FOOTGEAR:
				return bootsMap.Get(code);
			case ModelType.HEADGEAR:
				return hatsMap.Get(code);
			case ModelType.HAIR:
				return hairMap.Get(code);
			case ModelType.FACE:
				return faceMap.Get(code);
			case ModelType.ADDON:
				return addonMap.Get(code);
			default:
				return "";
		}
	}

	private static void LoadModelInfo(){
		cachedText = Resources.Load<TextAsset>(CLOTHES_DB);
		ProcessTextAsset(ModelType.CLOTHES, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(LEGS_DB);
		ProcessTextAsset(ModelType.LEGS, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(BOOTS_DB);
		ProcessTextAsset(ModelType.FOOTGEAR, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(HATS_DB);
		ProcessTextAsset(ModelType.HEADGEAR, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(HAIR_DB);
		ProcessTextAsset(ModelType.HAIR, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(FACE_DB);
		ProcessTextAsset(ModelType.FACE, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(ADDONS_DB);
		ProcessTextAsset(ModelType.ADDON, cachedText.ToString());
		cachedText = Resources.Load<TextAsset>(ESSENTIALS_DB);
		ProcessTextAsset(ModelType.ESSENTIAL, cachedText.ToString());
	}

	private static void ProcessTextAsset(ModelType t, string text){
		string[] lines = text.Split("\r\n");
		string[] lineElements;
		string name;

		ushort i = 0;

		foreach(string line in lines){
			if(line.Length == 0)
				continue;
			if(line[0] == '#')
				continue;

			lineElements = line.Split('\t');

			if(!models.ContainsKey(t))
				models.Add(t, new Dictionary<string, ModelInfo>());


			name = BuildName(lineElements);

			if(lineElements.Length < 4)
				models[t].Add(name, new ModelInfo(t, lineElements[0], lineElements[1], lineElements[2][0]));
			else if(lineElements.Length == 4)
				models[t].Add(name, new ModelInfo(t, lineElements[0], lineElements[1], lineElements[2][0], lineElements[3][0]));
			else
				models[t].Add(name, new ModelInfo(t, lineElements[0], lineElements[1], lineElements[2][0], lineElements[3][0], lineElements[4][0]));

			switch(t){
				case ModelType.CLOTHES:
					clothesMap.Add(i, name);
					break;
				case ModelType.LEGS:
					legsMap.Add(i, name);
					break;
				case ModelType.FOOTGEAR:
					bootsMap.Add(i, name);
					break;
				case ModelType.HEADGEAR:
					hatsMap.Add(i, name);
					break;
				case ModelType.FACE:
					faceMap.Add(i, name);
					break;
				case ModelType.ADDON:
					addonMap.Add(i, name);
					break;
				case ModelType.HAIR:
					hairMap.Add(i, name);
					break;
				case ModelType.ESSENTIAL:
					essentialsMap.Add(i, name);
					break;
				default:
					break;
			}

			i++;
		}
	}

	private static string BuildName(string[] miSerialized){
		return miSerialized[0] + "/" + miSerialized[2][0];
	}
}