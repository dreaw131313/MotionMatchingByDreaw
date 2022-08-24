using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class MotionMatchingDataCurve
	{
		[SerializeField]
		public string Name = "NewCurve";
		[SerializeField]
		public AnimationCurve Curve = new AnimationCurve();

		public MotionMatchingDataCurve()
		{
			Curve = new AnimationCurve();
		}

		public MotionMatchingDataCurve(string name)
		{
			this.Name = name;
			Curve = new AnimationCurve();
		}


#if UNITY_EDITOR
		[System.NonSerialized]
		public float currentKeyValue_editorOnly = 0f;

#endif
	}
}