using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay.Core
{
	public class AnimationCurveExtensions : MonoBehaviour
	{
		public static void ClampAnimationCurve(ref AnimationCurve curve, float minTime, float maxTime, float minValue, float maxValue)
		{
			if (curve.keys.Length == 0)
			{
				return;
			}

			Keyframe[] keys = curve.keys;

			for (int i = 0; i < keys.Length; i++)
			{
				Keyframe key = keys[i];

				key.time = Mathf.Clamp(key.time, minTime, maxTime);
				key.value = Mathf.Clamp(key.value, minValue, maxValue);

				keys[i] = key;
			}

			curve.keys = keys;
		}
	}
}
