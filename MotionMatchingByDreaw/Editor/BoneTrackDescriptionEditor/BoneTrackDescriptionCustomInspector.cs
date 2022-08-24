using MotionMatching.Gameplay;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(BoneTracksDescription))]
	public class BoneTrackDescriptionCustomInspector : Editor
	{
		GameObject gameObjectToCalculateTracks;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			BoneTracksDescription description = target as BoneTracksDescription;

			GUILayout.FlexibleSpace();
			if (description != null)
			{

				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginVertical();
					{
						GUILayout.Space(20f);
						gameObjectToCalculateTracks = EditorGUILayout.ObjectField(
							"Game object to calculate tracks",
							gameObjectToCalculateTracks,
							typeof(GameObject),
							true
							) as GameObject;


						if (GUILayout.Button(new GUIContent("Generate tracks in NativeMotionGroups", "In Native Motion Groups with selected Bone Track Description")))
						{
							if (gameObjectToCalculateTracks != null)
							{
								GenerateTracksInNativeMotionGroups(description);
							}
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
		}

		private void GenerateTracksInNativeMotionGroups(BoneTracksDescription description)
		{
			GameObject go = gameObjectToCalculateTracks;
			Vector3 goPos = go.transform.position;
			Quaternion goRot = go.transform.rotation;

			PreparingDataPlayableGraph graph = new PreparingDataPlayableGraph();
			graph.Initialize(go);

			NativeMotionGroup[] groups = NativeMotionGroup.GetAllExsitingNativeMotionGroupsInProject_EditorOnly();
			List<NativeMotionGroup> groupsToCalculate = new List<NativeMotionGroup>();


			foreach (NativeMotionGroup group in groups)
			{
				if (group.TracksDescription == description)
				{
					groupsToCalculate.Add(group);
				}
			}

			string log = $"Bone tracks \"{description.name}\" are generated in datas:";

			foreach (NativeMotionGroup group in groupsToCalculate)
			{
				foreach (MotionMatchingData data in group.AnimationData)
				{
					log += $"\n\t\"{data.name}\"";

					data.BoneTracks = new List<BoneTrack>();

					foreach (BoneTrackSettings settings in description.SettingsList)
					{
						data.BoneTracks.Add(new BoneTrack(settings));
					}

					EditorUtility.SetDirty(data);

					foreach (BoneTrack track in data.BoneTracks)
					{
						if (MotionDataCalculator.CreateTracksIntervals(
								go,
								graph,
								data.clips[0],
								track.TrackSettings,
								track
							))
						{
							EditorUtility.SetDirty(data);

							MotionDataCalculator.CreateBoneTrackData(
								go,
								graph,
								data.clips[0],
								track
								);

							EditorUtility.SetDirty(data);
						}
					}
				}
			}

			graph.Destroy();
			AssetDatabase.SaveAssets();

			Debug.Log(log);

			go.transform.SetPositionAndRotation(goPos, goRot);
		}
	}
}