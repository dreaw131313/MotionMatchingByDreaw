using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
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