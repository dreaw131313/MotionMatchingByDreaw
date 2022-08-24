using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "TrajectoryCorrectionProfile", menuName = "Motion Matching/Trajectory Assets/Trajectory Correction Profile")]
	public class TrajectoryCorrectionProfiles : ScriptableObject
	{
		[SerializeField]
		public List<CorrectionSettingSet> CorrectionSettings = new List<CorrectionSettingSet>();

		private Dictionary<string, int> settingsIndexes;


		public TrajectoryCorrectionSettings GetSettings(string name)
		{
			return CorrectionSettings[settingsIndexes[name]].Settings;
		}

		private void OnEnable()
		{
			settingsIndexes = new Dictionary<string, int>(CorrectionSettings.Count);

			for (int i = 0; i < CorrectionSettings.Count; i++)
			{
				settingsIndexes.Add(CorrectionSettings[i].Name, i);
			}
		}
	}

	[System.Serializable]
	public struct CorrectionSettingSet
	{
		[SerializeField]
		public string Name;
		[SerializeField]
		public TrajectoryCorrectionSettings Settings;
	}

	[System.Serializable]
	public struct TrajectoryCorrectionSettings
	{
		[SerializeField]
		public TrajectoryCorrectionType CorrectionType;
		[SerializeField]
		[Min(0f)]
		public float MinSpeedToPerformCorrection;
		[SerializeField]
		[Tooltip("Minimum angle at which the trajectory is corrected.")]
		[Range(0f, 180f)]
		public float MinAngle;
		[SerializeField]
		[Tooltip("Maximum angle at which the trajectory is corrected.")]
		[Range(0f, 180f)]
		public float MaxAngle;
		[SerializeField]
		[Tooltip("Speed in minimum angle.")]
		[Min(0f)]
		public float MinAngleSpeed;
		[SerializeField]
		[Tooltip("Speed in maximum angle or constant speed in constant correction type.")]
		[Min(0f)]
		public float MaxAngleSpeed;
		[SerializeField]
		[Tooltip("If it is true, correction is always applied, even in state where trajectory correction is disabled, but not in contact state.")]
		public bool ForceTrajectoryCorrection;
	}
}