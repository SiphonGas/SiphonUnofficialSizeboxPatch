using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JapaneseData : ScriptableObject {
	[System.SerializableAttribute]
	public class MorphTranslation {
		public string english;
		public string japanese;
	}
	public List<MorphTranslation> translation;

	public string GetTranslation(string japanese) {
		if(translation == null) {
			translation = new List<MorphTranslation>();
		}

		foreach(MorphTranslation tr in translation) {
			if(tr.japanese == japanese) {
				return tr.english;
			}
		}

		MorphTranslation newTrans = new MorphTranslation();
		newTrans.japanese = japanese;
		newTrans.english = "";
		translation.Add(newTrans);
		return "";
	}
}
