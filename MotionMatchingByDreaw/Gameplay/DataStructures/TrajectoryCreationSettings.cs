using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public struct TrajectoryCreationSettings
	{
		[SerializeField]
		public TrajectoryCreationType CreationType;
		[SerializeField]
		[Range(0f, 20f)]
		public float Bias;
		[SerializeField]
		[Range(0f, 1f)]
		public float Stiffness;
		[SerializeField]
		[Range(0f, 5f)]
		public float MaxTimeToCalculateFactor;
		[SerializeField]
		public float MaxSpeed;
		[SerializeField]
		public float Acceleration;
		[SerializeField]
		public float Deceleration;
		[SerializeField]
		public bool Strafe;
		[SerializeField]
		internal PastTrajectoryType PastTrajectoryCreationType;

		public TrajectoryCreationSettings(
			TrajectoryCreationType creationType, 
			float bias, 
			float stiffness, 
			float maxTimeToCalculateFactor, 
			float maxSpeed, 
			float acceleration, 
			float deceleration, 
			bool strafe, 
			PastTrajectoryType pastTrajectoryCreationType
			)
		{
			CreationType = creationType;
			Bias = bias;
			Stiffness = stiffness;
			MaxTimeToCalculateFactor = maxTimeToCalculateFactor;
			MaxSpeed = maxSpeed;
			Acceleration = acceleration;
			Deceleration = deceleration;
			Strafe = strafe;
			PastTrajectoryCreationType = pastTrajectoryCreationType;
		}
	}
}
