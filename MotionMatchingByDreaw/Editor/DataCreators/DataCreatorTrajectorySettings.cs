using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CreateAssetMenu(fileName = "TrajectoryTimes", menuName = "Motion Matching/Creators/Trajectory Times")]
	public class DataCreatorTrajectorySettings : ScriptableObject
	{
		[SerializeField]
		public List<float> TrajectoryTimes;

		//[Header("Generation times:")]
		//[SerializeField]
		//[Min(0)]
		//public int PastPointsCount = 2;
		//[SerializeField]
		//public float MinTime = -0.66f;
		//[Space]
		//[SerializeField]
		//[Min(1)]
		//public int FuterPointsCount = 3;
		//[SerializeField]
		//public float MaxTime = 1f;


		//private void OnValidate()
		//{
		//	float minPointTime = 0.001f;
		//	MinTime = Mathf.Clamp(MinTime, float.MinValue, -minPointTime);
		//	MaxTime = Mathf.Clamp(MaxTime,  minPointTime, float.MaxValue);
		//}
	}

}
