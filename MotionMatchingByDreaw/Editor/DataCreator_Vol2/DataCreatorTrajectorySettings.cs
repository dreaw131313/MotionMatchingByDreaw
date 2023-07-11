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

	}

}
