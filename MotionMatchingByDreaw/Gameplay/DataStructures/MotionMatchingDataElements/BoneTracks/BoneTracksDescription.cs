using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "BoneTrackDescription", menuName = "Motion Matching/Data/Tracks/Tracks Description")]
	public class BoneTracksDescription : ScriptableObject
	{
		[SerializeField]
		public List<BoneTrackSettings> SettingsList;

		private Dictionary<string, int> tracksIndexes;
		public ReadOnlyDictionary<string, int> TracksIndexes { get; private set; } = null;

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

			tracksIndexes = new Dictionary<string, int>();

			for (int i = 0; i < SettingsList.Count; i++)
			{
				tracksIndexes.Add(SettingsList[i].TrackName, i);
			}

			TracksIndexes = new ReadOnlyDictionary<string, int>(tracksIndexes);
		}
	}

	[System.Serializable]
	public class BoneTrackSettings
	{
		[SerializeField]
		public string TrackName;
		[SerializeField]
		public string DataBoneName;
		[SerializeField]
		public string ConditionBoneName;
		[SerializeField]
		[Min(0.00001f)]
		public float SamplingTime = 0.03333f;
		[SerializeField]
		public BoneTrackFilter Filter;

	}

}