using UnityEngine;
using UnityEditor;
using System.Collections;

public class ModelImportSettings : AssetPostprocessor {

	private void OnPreprocessModel() {
		ModelImporter model = (ModelImporter) assetImporter;
		if(model != null) 
		{
			if(assetPath.Contains("GTS"))
			{
				model.globalScale = 1000;
			}
		}
	}
/*
	private void OnPostprocessModel(GameObject go)
	{
		ModelImporter model = assetImporter as ModelImporter;
		model.animationType = ModelImporterAnimationType.Human;
		Avatar avatar = AvatarBuilder.BuildHumanAvatar(go, model.humanDescription);
		model.sourceAvatar = avatar;

	}*/
	
}
