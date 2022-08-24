using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "TrajectoryProfile", menuName = "Motion Matching/Trajectory Assets/Trajectory Profile")]
	public class TrajectoryProfile : ScriptableObject, ISerializationCallbackReceiver
	{
		[SerializeField]
		public List<TrajectoryCreationSettingsPair> trajectorySettings;

		private Dictionary<string, TrajectoryCreationSettingsPair> settingsDictionary;

		public TrajectoryCreationSettings GetSettings(string name)
		{
#if UNITY_EDITOR
			if (trajectorySettings.Count == 0)
			{
				throw new System.Exception($"Trajectory Profile \"{this.name}\" does not have any trajectory settings!");
			}
#endif
			return settingsDictionary[name].Settings;
		}

		public TrajectoryCreationSettings GetBestSettingsBasedOnVelocity(float testedVelocity)
		{
#if UNITY_EDITOR
			if(trajectorySettings.Count == 0)
			{
				throw new System.Exception($"Trajectory Profile \"{this.name}\" does not have any trajectory settings!");
			}
#endif

			float delta = float.MaxValue;
			int settingsIndex = 0;
			for (int i = 0; i < trajectorySettings.Count; i++)
			{
				float currentDelta = Mathf.Abs(testedVelocity - trajectorySettings[i].Settings.MaxSpeed);
				if (currentDelta < delta)
				{
					delta = currentDelta;
					settingsIndex = i;
				}
			}

			return trajectorySettings[settingsIndex].Settings;
		}

		public void OnAfterDeserialize()
		{
			settingsDictionary = new Dictionary<string, TrajectoryCreationSettingsPair>();
			for(int i = 0; i < trajectorySettings.Count; i++)
			{
				settingsDictionary.Add(trajectorySettings[i].Name, trajectorySettings[i]);
			}
		}

		public void OnBeforeSerialize()
		{
		}
	}

	[System.Serializable]
	public class TrajectoryCreationSettingsPair
	{
		[SerializeField]
		public string Name;
		[SerializeField]
		public TrajectoryCreationSettings Settings;
	}

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
