using MotionMatching.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(NativeMotionGroup))]
	public class NativeMotionGroupInspector : Editor
	{
		private NativeMotionGroup nativeGroup;

		private bool drawDebugEditor = false;

		const float MIN_WEIGHT_VALUE = 0.01f;

		const float VerticalMargin = 5f;

		private void OnEnable()
		{
			nativeGroup = (NativeMotionGroup)this.target;
		}

		public override void OnInspectorGUI()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Updata all existing NativeMotionGroups", GUIResources.Button_MD()))
					{
						UpdateAllNativeMotionGroups();
					}
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(target.name, GUIResources.GetMediumHeaderStyle_LG());
				}
				GUILayout.EndHorizontal();


				GUILayout.Space(10);
				DrawMotionMatchingData();

				GUILayout.Space(10);
				DrawSectinoDependecies();

				GUILayout.Space(10);
				DrawBoneTracks();

				GUILayout.Space(10);
				DrawButtons();

				GUILayout.Space(10);
				DrawDebugEditor();

			}
			GUILayout.EndVertical();

			if (target != null)
			{
				EditorUtility.SetDirty(nativeGroup);
				//Undo.RecordObject(nativeGroup, "Native Motion Group Changed");
			}
		}

		private void DrawMotionMatchingData()
		{
			GUILayoutElements.DrawHeader(
				"Motion Matching Data",
				GUIResources.GetLightHeaderStyle_MD(),
				GUIResources.GetMediumHeaderStyle_MD(),
				ref nativeGroup.FoldMotionMatchingGroups
				);

			AddingMotionMatchingData();
			GUILayout.BeginVertical();
			{
				if (nativeGroup.FoldMotionMatchingGroups)
				{
					DrawMotionMatchingDataList();
				}
			}
			DrawMotionMatchingDataSummary();

			DrawDataWeightSettings();

			GUILayout.EndVertical();

		}

		private void AddingMotionMatchingData()
		{
			Rect dropRect = GUILayoutUtility.GetLastRect();
			//dropRect.y -= scroll.y;
			if (dropRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.type == EventType.DragUpdated)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
				}
				else if (Event.current.type == EventType.DragPerform)
				{
					bool correctData = true;
					List<MotionMatchingData> newData = new List<MotionMatchingData>();
					for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
					{
						try
						{
							newData.Add((MotionMatchingData)DragAndDrop.objectReferences[i]);
						}
						catch (Exception)
						{
							correctData = false;
							break;
						}
					}

					if (correctData)
					{
						foreach (MotionMatchingData data in newData)
						{
							if (!nativeGroup.AnimationData.Contains(data))
							{
								nativeGroup.AnimationData.Add(data);
							}
						}
					}
					Event.current.Use();
				}
			}
		}

		private void DrawMotionMatchingDataList()
		{
			if (nativeGroup.AnimationData.Count == 0)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.Label("Animation data list is empty");
				}
				GUILayout.EndHorizontal();
			}
			for (int i = 0; i < nativeGroup.AnimationData.Count; i++)
			{
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(10);
						GUILayout.Label(string.Format("{0}.", i), GUILayout.Width(30));
						nativeGroup.AnimationData[i] = EditorGUILayout.ObjectField(nativeGroup.AnimationData[i], typeof(MotionMatchingData), false) as MotionMatchingData;
						if (GUILayout.Button("X", GUILayout.Width(25)))
						{
							nativeGroup.AnimationData.RemoveAt(i);
							i--;
							continue;
						}
						GUILayout.Space(10);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
			}
		}

		private void DrawMotionMatchingDataSummary()
		{
			float checkedTime = 0f;
			int checkedPoseCount = 0;
			float totalTime = 0f;
			int totalPoseCount = 0;
			foreach (MotionMatchingData data in nativeGroup.AnimationData)
			{
				if (data != null)
				{
					float checkedDataTime = data.animationLength - data.neverChecking.GetSectionTime();
					checkedTime += checkedDataTime;
					checkedPoseCount += Mathf.FloorToInt(checkedDataTime / data.frameTime);
					totalTime += data.animationLength;
					totalPoseCount += data.numberOfFrames;
				}
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				// Descriptions
				GUILayout.BeginVertical();
				{
					GUILayout.Label("Number of clips:");
					GUILayout.Label("Frames:");
					GUILayout.Label("Animations time:");
				}
				GUILayout.EndVertical();
				// Values
				GUILayout.BeginVertical();
				{
					GUILayout.Label(nativeGroup.AnimationData.Count.ToString());
					GUILayout.Label(string.Format("{0} / {1}", checkedPoseCount, totalPoseCount));
					GUILayout.Label(string.Format(
						"{0} min {1} s / {2} min {3} s",
						Mathf.FloorToInt(checkedTime / 60),
						Math.Round(checkedTime % 60, 2),
						Mathf.FloorToInt(totalTime / 60),
						Math.Round(totalTime % 60, 2)
						));
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();


			if (GUILayout.Button("Clear Motion Matching Data"))
			{
				nativeGroup.AnimationData.Clear();
			}

			GUILayout.Space(10);

			GUILayoutElements.DrawHeader("Trajectory times:", GUIResources.GetMediumHeaderStyle_SM());
			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal(GUILayout.Width(100));
				{
					GUILayout.Space(20);
					GUILayout.BeginVertical();
					{
						for (int i = 0; i < nativeGroup.TrajectoryTimes.Length; i++)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Label(string.Format("{0}.", i + 1));
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						for (int i = 0; i < nativeGroup.TrajectoryTimes.Length; i++)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Label(string.Format("{0}", nativeGroup.TrajectoryTimes[i]));
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();

			GUILayout.Space(10);

		}

		private void DrawDataWeightSettings()
		{
			GUILayoutElements.DrawHeader("Bones weights:", GUIResources.GetMediumHeaderStyle_SM());
			GUILayout.Space(5);
			// bone names
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20);
				GUILayout.BeginVertical(GUILayout.Width(100));
				{
					if (nativeGroup.BonesWeights.Count > 0)
					{
						GUILayout.BeginHorizontal();
						{
							nativeGroup.NormalizeWeights = EditorGUILayout.Toggle(new GUIContent("Normalize weights"), nativeGroup.NormalizeWeights);
							if (nativeGroup.NormalizeWeights)
							{
								foreach (MotionMatchingData data in nativeGroup.AnimationData)
								{
									if (data != null)
									{
										nativeGroup.UpdateBoneWeights(data);
										break;
									}
								}
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.Space(3);

					float lastLabel = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 95;
					for (int i = 0; i < nativeGroup.BonesWeights.Count; i++)
					{
						BoneWeightInfo bw = nativeGroup.BonesWeights[i];

						GUILayoutElements.DrawHeader(
							string.Format("{0}. {1}", i + 1, bw.BoneName),
							GUIResources.GetLightHeaderStyle_SM()
							);

						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(30f);
							bw.PositionWeight = EditorGUILayout.FloatField(
								"Position Weight:",
								Mathf.Clamp(nativeGroup.BonesWeights[i].PositionWeight, MIN_WEIGHT_VALUE, float.MaxValue)
								);

							if (nativeGroup.NormalizeWeights)
							{
								GUILayout.Label($"( {nativeGroup.NormalizedBonesWeights[i].x.ToString("F")} )");
							}
							GUILayout.Space(10);
							bw.VelocityWeight = EditorGUILayout.FloatField(
								"Velocity Weight:",
								Mathf.Clamp(nativeGroup.BonesWeights[i].VelocityWeight, MIN_WEIGHT_VALUE, float.MaxValue)
								);
							if (nativeGroup.NormalizeWeights)
							{
								GUILayout.Label($"( {nativeGroup.NormalizedBonesWeights[i].y.ToString("F")} )");
							}
						}
						GUILayout.EndHorizontal();
						nativeGroup.BonesWeights[i] = bw;
					}
					EditorGUIUtility.labelWidth = lastLabel;
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();


			GUILayoutElements.DrawHeader("Trajectory weights", GUIResources.GetMediumHeaderStyle_SM());

			GUILayout.Space(VerticalMargin);
			GUILayout.BeginHorizontal();
			{
				float lastLabel = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 130;

				GUILayout.Space(20);
				GUILayout.BeginVertical(GUILayout.Width(100));
				{
					nativeGroup.NormalizeTrajctoryWeights = EditorGUILayout.Toggle(
						"Normalize weights", nativeGroup.NormalizeTrajctoryWeights
						);

					GUILayout.BeginHorizontal();
					{
						nativeGroup.BufforTrajectoryPositionWeight = EditorGUILayout.FloatField(
							"Position weight",
							Mathf.Clamp(nativeGroup.BufforTrajectoryPositionWeight, MIN_WEIGHT_VALUE, float.MaxValue)
							);

						if (nativeGroup.NormalizeTrajctoryWeights)
						{
							GUILayout.Label($"( {nativeGroup.TrajectoryPositionWeight.ToString("F")} )");
						}
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						nativeGroup.BufforTrajectoryVelocityWeight = EditorGUILayout.FloatField(
							"Velocity weight",
							Mathf.Clamp(nativeGroup.BufforTrajectoryVelocityWeight, MIN_WEIGHT_VALUE, float.MaxValue)
							);

						if (nativeGroup.NormalizeTrajctoryWeights)
						{
							GUILayout.Label($"( {nativeGroup.TrajectoryVelocityWeight.ToString("F")} )");
						}
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						nativeGroup.BufforTrajectoryOrientationWeight = EditorGUILayout.FloatField(
							"Orientation weight",
							Mathf.Clamp(nativeGroup.BufforTrajectoryOrientationWeight, MIN_WEIGHT_VALUE, float.MaxValue)
							);

						if (nativeGroup.NormalizeTrajctoryWeights)
						{
							GUILayout.Label($"( {nativeGroup.TrajectoryOrientationWeight.ToString("F")} )");
						}
					}
					GUILayout.EndHorizontal();

					if (nativeGroup.NormalizeTrajctoryWeights)
					{
						float desiredSum = 3f;
						float currentSum =
									nativeGroup.BufforTrajectoryPositionWeight +
									nativeGroup.BufforTrajectoryVelocityWeight +
									nativeGroup.BufforTrajectoryOrientationWeight;


						nativeGroup.TrajectoryPositionWeight = nativeGroup.BufforTrajectoryPositionWeight / currentSum * desiredSum;
						nativeGroup.TrajectoryVelocityWeight = nativeGroup.BufforTrajectoryVelocityWeight / currentSum * desiredSum;
						nativeGroup.TrajectoryOrientationWeight = nativeGroup.BufforTrajectoryOrientationWeight / currentSum * desiredSum;
					}
					else
					{
						nativeGroup.TrajectoryPositionWeight = nativeGroup.BufforTrajectoryPositionWeight;
						nativeGroup.TrajectoryVelocityWeight = nativeGroup.BufforTrajectoryVelocityWeight;
						nativeGroup.TrajectoryOrientationWeight = nativeGroup.BufforTrajectoryOrientationWeight;
					}
				}
				GUILayout.EndVertical();

				EditorGUIUtility.labelWidth = lastLabel;
			}
			GUILayout.EndHorizontal();


			GUILayout.Space(10);
			float lastLabel_2 = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 150;

			int floatsPerTrajectoryPoint = 9;
			int floatsPerVelocity = 3;
			int floatsPerBone = 6;
			int floatsPerContactPoint = 6;

			float avarageTrajectoryDataWeight = nativeGroup.TrajectoryOrientationWeight + nativeGroup.TrajectoryVelocityWeight + nativeGroup.TrajectoryPositionWeight;
			avarageTrajectoryDataWeight /= 3f;

			float trajectoryFloatCount = nativeGroup.TrajectoryTimes.Length * floatsPerTrajectoryPoint * nativeGroup.TrajectoryCostWeight * avarageTrajectoryDataWeight;
			float poseFloatCount = nativeGroup.PoseBonesCount * floatsPerBone * nativeGroup.PoseCostWeight;
			float contactsFloatCount = nativeGroup.ContactPointsCount * floatsPerContactPoint * nativeGroup.ContactsCostWeight;

			float allFloatCountTakenToCalculatingCost =
				trajectoryFloatCount +
				poseFloatCount +
				contactsFloatCount;


			GUILayoutElements.DrawHeader("Cost calculation:", GUIResources.GetMediumHeaderStyle_SM());
			GUILayout.BeginVertical(GUILayout.Width(200));
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20);
				nativeGroup.TrajectoryCostWeight = EditorGUILayout.FloatField(
								"Trajectory weight:",
								Mathf.Clamp(nativeGroup.TrajectoryCostWeight, MIN_WEIGHT_VALUE, float.MaxValue)
								);

				float finalCostPercentage = (float)trajectoryFloatCount / allFloatCountTakenToCalculatingCost * 100f;
				EditorGUILayout.LabelField($"({finalCostPercentage.ToString("0.00")} %)");
			}
			GUILayout.EndHorizontal();

			//GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20);
				nativeGroup.PoseCostWeight = EditorGUILayout.FloatField(
								"Pose weight:",
								Mathf.Clamp(nativeGroup.PoseCostWeight, MIN_WEIGHT_VALUE, float.MaxValue)
								);

				float finalCostPercentage = (float)poseFloatCount / allFloatCountTakenToCalculatingCost * 100f;
				EditorGUILayout.LabelField($"({finalCostPercentage.ToString("0.00")} %)");
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20);
				nativeGroup.ContactsCostWeight = EditorGUILayout.FloatField(
								"Contacts weight:",
								Mathf.Clamp(nativeGroup.ContactsCostWeight, MIN_WEIGHT_VALUE, float.MaxValue)
								);

				float finalCostPercentage = (float)contactsFloatCount / allFloatCountTakenToCalculatingCost * 100f;
				EditorGUILayout.LabelField($"({finalCostPercentage.ToString("0.00")} %)");
			}
			GUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = lastLabel_2;
			GUILayout.EndVertical();
		}

		private void DrawSectinoDependecies()
		{
			GUILayout.BeginVertical();
			{
				GUILayoutElements.DrawHeader("Sectinos Dependecies", GUIResources.GetMediumHeaderStyle_MD());
				GUILayout.Space(5);
				nativeGroup.SectionsDependencies = (SectionsDependencies)EditorGUILayout.ObjectField(nativeGroup.SectionsDependencies, typeof(SectionsDependencies), false);

				GUILayout.Space(5);
				if (nativeGroup.SectionsDependencies != null)
				{
					if (GUILayout.Button("Generate Sectinos in Motion Matching Data", GUIResources.Button_MD()))
					{
						for (int i = 0; i < nativeGroup.AnimationData.Count; i++)
						{
							nativeGroup.SectionsDependencies.UpdateSectionDependecesInMMData(nativeGroup.AnimationData[i]);
							EditorUtility.SetDirty(nativeGroup.AnimationData[i]);
							nativeGroup.AnimationData[i].sectionFold = true;
						}
						AssetDatabase.SaveAssets();
					}
				}

				GUILayout.Space(10);
				if (GUILayout.Button("Clear Sections in motionMatchingData", GUIResources.Button_MD()))
				{
					if (EditorUtility.DisplayDialog(
						$"Clearing sections in {nativeGroup.AnimationData.Count} MotionMatchingData",
						$"Clearing sections in {nativeGroup.AnimationData.Count} MotionMatchingData. Removed sections cannot be restored.",
						"Clear sections",
						"Cancel"
						))
					{
						for (int i = 0; i < nativeGroup.AnimationData.Count; i++)
						{
							nativeGroup.AnimationData[i].ClearSections();
							EditorUtility.SetDirty(nativeGroup.AnimationData[i]);
							nativeGroup.AnimationData[i].sectionFold = true;
						}
						AssetDatabase.SaveAssets();
					}
				}
			}
			GUILayout.EndVertical();
		}

		private void DrawBoneTracks()
		{
			GUILayout.BeginVertical();
			{
				GUILayoutElements.DrawHeader("Bone Tracks:", GUIResources.GetMediumHeaderStyle_MD());
				GUILayout.Space(5);

				nativeGroup.TracksDescription = EditorGUILayout.ObjectField(nativeGroup.TracksDescription, typeof(BoneTracksDescription), false) as BoneTracksDescription;

				if (nativeGroup.TracksDescription != null)
				{
					if (GUILayout.Button("Clear bone tracks:"))
					{
						if (EditorUtility.DisplayDialog(
							$"Clearing bone tracks in {nativeGroup.AnimationData.Count} MotionMatchingData",
							$"Clearing bone tracks in {nativeGroup.AnimationData.Count} MotionMatchingData. Removed bone tracks cannot be restored.",
							"Clear bone tracks",
							"Cancel"
							))
						{
							for (int i = 0; i < nativeGroup.AnimationData.Count; i++)
							{
								if (nativeGroup.AnimationData[i].BoneTracks != null)
								{
									nativeGroup.AnimationData[i].BoneTracks.Clear();
									EditorUtility.SetDirty(nativeGroup.AnimationData[i]);
								}
							}
							AssetDatabase.SaveAssets();
						}
					}
				}

			}
			GUILayout.EndVertical();
		}

		private void DrawButtons()
		{
			GUILayout.BeginVertical();
			{
				GUILayoutElements.DrawHeader("Updating Data", GUIResources.GetMediumHeaderStyle_MD());

				GUILayout.Space(10);
				if (GUILayout.Button("Update Motion Data Group", GUIResources.Button_MD()))
				{
					nativeGroup.UpdateFromAnimationData();

					AssetDatabase.Refresh();
				}
			}
			GUILayout.EndVertical();
		}

		private void DrawDebugEditor()
		{
			GUILayoutElements.DrawHeader(
				"Debug Editor",
				GUIResources.GetLightHeaderStyle_MD(),
				GUIResources.GetMediumHeaderStyle_MD(),
				ref drawDebugEditor
				);

			if (drawDebugEditor)
			{
				base.OnInspectorGUI();
			}
		}


		[MenuItem("MotionMatching/Helpers/Update all NativeMotionGroups", priority = 1000)]
		public static void UpdateAllNativeMotionGroups()
		{
			string path = $"{Application.streamingAssetsPath}/{NativeMotionGroup.FileName}";
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}

			string[] groupsGUIDS = AssetDatabase.FindAssets("t:NativeMotionGroup");


			NativeMotionGroup[] groups = Resources.FindObjectsOfTypeAll<NativeMotionGroup>();

			string updatedMotionGroups = "Updated motion groups list:";
			int index = 1;


			foreach (string guid in groupsGUIDS)
			{
				NativeMotionGroup group = AssetDatabase.LoadAssetAtPath<NativeMotionGroup>(AssetDatabase.GUIDToAssetPath(guid));

				if (group != null)
				{
					group.UpdateFromAnimationData();
					updatedMotionGroups += string.Format("\n\t{0}.  {1}", index, group.name);
					//updatedMotionGroups += string.Format("\nPath:\t{0}", g.GetPathToBinaryAsset());
					index++;

					EditorUtility.SetDirty(group);
				}
			}

			//foreach (NativeMotionGroup group in groups)
			//{
			//	group.UpdateFromCurrentAnimationData();
			//	group.CreateBinaryData();
			//	updatedMotionGroups += string.Format("\n\t{0}.  {1}", index, group.name);
			//	//updatedMotionGroups += string.Format("\nPath:\t{0}", g.GetPathToBinaryAsset());
			//	index++;

			//	EditorUtility.SetDirty(group);
			//}

			AssetDatabase.Refresh();
			Debug.Log(updatedMotionGroups);
		}

	}

	[InitializeOnLoad]
	public class NativeMotionGroupDatabaseEditor : UnityEditor.AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string AssetPath, RemoveAssetOptions rao)
		{
			NativeMotionGroup motionGroup = (NativeMotionGroup)AssetDatabase.LoadAssetAtPath(AssetPath, typeof(NativeMotionGroup));
			if (motionGroup != null)
			{
				Debug.Log(AssetDatabase.GetAssetPath(motionGroup));
				string binaryMotionGroupPath = motionGroup.GetPathToBinaryAsset();
				if (File.Exists(binaryMotionGroupPath))
				{
					File.Delete(binaryMotionGroupPath);
				}
			}

			return AssetDeleteResult.DidNotDelete;
		}
		//
	}
}
