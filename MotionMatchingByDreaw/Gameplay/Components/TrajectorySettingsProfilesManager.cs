using MotionMatching.Gameplay;
using UnityEngine;

namespace MotionMatching
{

	public class TrajectorySettingsProfilesManager : MonoBehaviour
	{
		[SerializeField]
		private TrajectoryMaker trajectoryMaker;
		[SerializeField]
		private MotionMatchingComponent motionMatching;
		[Space]
		[SerializeField]
		private TrajectorySettingsProfiles profiles;

		public TrajectorySettingsProfiles Profiles { get => profiles; }
		public MotionMatchingComponent MotionMatching { get => motionMatching; }
		public TrajectoryMaker TrajectoryMaker { get => trajectoryMaker; }

		private void Awake()
		{
			if (trajectoryMaker == null)
			{
				trajectoryMaker = GetComponent<TrajectoryMaker>();
			}

			if (motionMatching == null)
			{
				motionMatching = GetComponent<MotionMatchingComponent>();
			}
		}

		public bool SetProfile(string profileName)
		{
			if (profiles != null)
			{
				return profiles.SetProfile(profileName, trajectoryMaker, motionMatching);
			}

			return false;
		}

		public bool SetBestProfileFromSpeed(float speed)
		{
			if (profiles != null)
			{
				return profiles.SetProfileBasedOnSpeed(speed, trajectoryMaker, motionMatching);
			}

			return false;
		}
	}

}