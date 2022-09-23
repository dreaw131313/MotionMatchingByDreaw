using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CreateAssetMenu(fileName ="MotionMatchingDataCreator2", menuName ="Motion Matching/Creators/MotionMatching Data Creator 2")]
	public class DataCreator_New : ScriptableObject
	{
		[SerializeField]
		public GameObject AnimatedGameObject;
		[SerializeField]
		public BonesProfile BonesMask;
		[SerializeField]
		public DataCreatorTrajectorySettings TrajectorySettings = null;
		[SerializeField]
		public List<MotionMatchingDataCreationSetup> Setups;

	}

	[System.Serializable]
	public class MotionMatchingDataCreationSetup
	{
		[SerializeField]
		public string Name = "Creator setup";
		[SerializeField]
		public List<AnimationClip> clips = new List<AnimationClip>();
		[SerializeField]
		public int PosesPerSecond;
		[SerializeField]
		public string saveDataPath = "";
		[SerializeField]
		public float CutTimeFromStart = 0f;
		[SerializeField]
		public float CutTimeToEnd = 0f;
		[SerializeField]
		public bool OverrideTrajectory = true;
		[SerializeField]
		public bool FindInYourself = true;
		[SerializeField]
		public bool BlendToYourself = true;
		[SerializeField]
		public bool CutTimeOnEnds = false;
		[SerializeField]
		public bool AnimationFold;
		[SerializeField]
		public bool BlendTreesFold;
	}
}