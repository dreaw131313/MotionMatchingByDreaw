using MotionMatching.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class MotionMatchingDataCreatorEditor2 : EditorWindow
	{
		[MenuItem("MotionMatching/Data Creators/Data creator New", priority = 2)]
		private static void CreateWindow()
		{
			MotionMatchingDataCreatorEditor2 editor = EditorWindow.GetWindow<MotionMatchingDataCreatorEditor2>();
			editor.position = new Rect(new Vector2(100, 100), new Vector2(800, 600));
			editor.titleContent = new GUIContent("Motion Matching Data Creator");
		}

		[OnOpenAsset(0)]
		public static bool OnAssetOpen(int instanceID, int line)
		{
			DataCreator_New asset;
			try
			{
				asset = (DataCreator_New)EditorUtility.InstanceIDToObject(instanceID);
			}
			catch (System.Exception)
			{
				return false;
			}

			if (EditorWindow.HasOpenInstances<MotionMatchingDataCreatorEditor2>())
			{
				EditorWindow.GetWindow<MotionMatchingDataCreatorEditor2>().SetAsset(asset);
				EditorWindow.GetWindow<MotionMatchingDataCreatorEditor2>().Repaint();
				return true;
			}

			MotionMatchingDataCreatorEditor2.CreateWindow();
			EditorWindow.GetWindow<MotionMatchingDataCreatorEditor2>().SetAsset(asset);
			EditorWindow.GetWindow<MotionMatchingDataCreatorEditor2>().Repaint();

			return true;
		}

		private void SetAsset(DataCreator_New newCreator)
		{
			creator = newCreator;
		}

		private GUIStyle assetRectStyle = new GUIStyle();
		private GUIStyle basicOptionsRectStyle = new GUIStyle();
		private GUIStyle setupListRectStyle = new GUIStyle();
		private GUIStyle setupOptionsRectStyle = new GUIStyle();

		float verticalMargin = 3f;

		private void OnEnable()
		{
			selectedSetup = null;
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical();
			{
				StylesSetup();
				// asset field
				GUILayout.BeginVertical(assetRectStyle);
				{
					GUILayout.Space(verticalMargin + 2);
					AssetOptionsOnGUI();
					GUILayout.Space(verticalMargin);
				}
				GUILayout.EndVertical();

				// Base oiptions
				GUILayout.BeginVertical(basicOptionsRectStyle);
				{
					GUILayout.Space(verticalMargin);
					BaseOptionsOnGUI();
					GUILayout.Space(verticalMargin);
				}
				GUILayout.EndVertical();

				Rect lastRect = GUILayoutUtility.GetLastRect();

				float height = this.position.height - 55f;

				GUILayout.BeginHorizontal();
				{
					// setup List
					GUILayout.BeginVertical(setupListRectStyle, GUILayout.Width(this.position.width / 3f), GUILayout.Height(height));
					{
						GUILayout.Space(verticalMargin);
						SetupListOnGUI();
						GUILayout.Space(verticalMargin);
					}
					GUILayout.EndVertical();

					// setup options
					GUILayout.BeginVertical(setupOptionsRectStyle, GUILayout.Height(height));
					{
						SetupOptionsOnGUI();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();

			if (creator != null)
			{
				EditorUtility.SetDirty(creator);
				Undo.RecordObject(creator, "MotionMatching data creator change");
			}
		}

		private void StylesSetup()
		{
			assetRectStyle.normal.background = GUIResources.GetDarkTexture();
			basicOptionsRectStyle.normal.background = GUIResources.GetMediumTexture_0();
			setupListRectStyle.normal.background = GUIResources.GetMediumTexture_1();
			setupOptionsRectStyle.normal.background = GUIResources.GetMediumTexture_2();
		}


		#region Asset options
		DataCreator_New creator;
		DataCreator_New bufforCreator;
		float horizontalMargin = 7.5f;
		private void AssetOptionsOnGUI()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(horizontalMargin);

				GUILayout.Label("Data creator", GUILayout.Width(75));
				creator = (DataCreator_New)EditorGUILayout.ObjectField(creator, typeof(DataCreator_New), true, GUILayout.Width(200));

				GUILayout.Space(horizontalMargin);

				if (bufforCreator != creator)
				{
					selectedSetup = null;
					bufforCreator = creator;
				}

				//if (GUILayout.Button("Calculate data"))
				//{
				//	Debug.Log("Calculate data");
				//}
				//GUILayout.Space(horizontalMargin);
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region Base options
		Vector2 setupsScroll;

		private void BaseOptionsOnGUI()
		{
			if (creator == null)
			{
				return;
			}
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(horizontalMargin);

				{
					GUILayout.Label("Game object", GUILayout.Width(90));
					creator.AnimatedGameObject = (GameObject)EditorGUILayout.ObjectField(creator.AnimatedGameObject, typeof(GameObject), true);
				}

				GUILayout.Space(horizontalMargin);

				{
					GUILayout.Label("Bone Mask", GUILayout.Width(75));
					creator.BonesMask = (BonesProfile)EditorGUILayout.ObjectField(creator.BonesMask, typeof(BonesProfile), true);
				}

				GUILayout.Space(horizontalMargin);

				{
					GUILayout.Label("Trajectory times", GUILayout.Width(100));
					creator.TrajectorySettings = (DataCreatorTrajectorySettings)EditorGUILayout.ObjectField(creator.TrajectorySettings, typeof(DataCreatorTrajectorySettings), true);
				}

				GUILayout.Space(horizontalMargin);
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region Setup list

		MotionMatchingDataCreationSetup selectedSetup = null;

		private void SetupListOnGUI()
		{
			if (creator == null)
			{
				return;
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(horizontalMargin);
				AddSetupButton();
				GUILayout.Space(horizontalMargin);
			}
			GUILayout.EndHorizontal();

			DrawSetupsList();
		}

		private void AddSetupButton()
		{
			if (GUILayout.Button("Add creator setup"))
			{
				if(creator.Setups == null)
				{
					creator.Setups = new List<MotionMatchingDataCreationSetup>();
				}
				creator.Setups.Add(new MotionMatchingDataCreationSetup());
			}
		}

		private void DrawSetupsList()
		{
			float distanceBetweenSetups = 5f;

			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(horizontalMargin);
				setupsScroll = GUILayout.BeginScrollView(setupsScroll);
				{
					GUILayout.BeginVertical();
					{
						if (creator.Setups != null)
						{
							for (int i = 0; i < creator.Setups.Count; i++)
							{
								MotionMatchingDataCreationSetup setup = creator.Setups[i];
								GUILayout.BeginHorizontal();
								{
									if (GUILayoutElements.DrawHeader(
										setup.Name,
										GUIResources.GetLightHeaderStyle_SM(),
										GUIResources.GetDarkHeaderStyle_SM(),
										setup == selectedSetup
										))
									{
										if (selectedSetup != setup)
										{
											selectedSetup = setup;
										}
									}
								}
								GUILayout.Space(3);

								if (GUILayout.Button("X", GUILayout.Width(20)))
								{
									if (selectedSetup == setup)
									{
										selectedSetup = null;
									}
									creator.Setups.RemoveAt(i);
									i--;
								}
								GUILayout.Space(horizontalMargin);
								GUILayout.EndHorizontal();
								GUILayout.Space(distanceBetweenSetups);
							}
						}
					}
					GUILayout.Space(10);
					GUILayout.EndVertical();
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region Setup options
		Vector2 animationsScroll;

		private void SetupOptionsOnGUI()
		{
			if (creator == null || selectedSetup == null)
			{
				return;
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(horizontalMargin);
				GUILayout.BeginVertical();
				{
					DrawSetupOptions();
					DrawClipsList();
					GUILayout.Space(10);
					DrawBlendTrees();

					GUILayout.Space(10);
					if (GUILayout.Button("Calculate data in selected setup", GUIResources.Button_MD()))
					{
						bool canBeCaluclated = true;
						if (creator.AnimatedGameObject == null)
						{
							Debug.Log($"In creator {creator.name} \"Animated Game Object\" is not setted!");
							canBeCaluclated = false;
						}

						if (creator.BonesMask == null)
						{
							Debug.Log($"In creator {creator.name} \"Bone Mask\" asset is not setted!");
							canBeCaluclated = false;
						}

						if (creator.BonesMask == null)
						{
							Debug.Log($"In creator {creator.name} \"Trajectory Times\" asset is not setted!");
							canBeCaluclated = false;
						}

						if (canBeCaluclated)
						{
							CalculateDataButton(selectedSetup, true, false, false);
						}
					}
					GUILayout.Space(10);
				}
				GUILayout.EndVertical();
				GUILayout.Space(horizontalMargin);
			}
			GUILayout.EndHorizontal();
		}

		private void DrawSetupOptions()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Setup name", GUILayout.Width(80));
				selectedSetup.Name = EditorGUILayout.TextField(selectedSetup.Name, GUILayout.Width(150));
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Poses per second", GUILayout.Width(130));
				selectedSetup.PosesPerSecond = Mathf.Clamp(
					EditorGUILayout.IntField(selectedSetup.PosesPerSecond, GUILayout.Width(50)),
					1,
					int.MaxValue
					);
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Override trajectory", GUILayout.Width(120));
				selectedSetup.OverrideTrajectory = EditorGUILayout.Toggle(selectedSetup.OverrideTrajectory, GUILayout.Width(18));

				GUILayout.Label("Find in yourself", GUILayout.Width(120));
				selectedSetup.FindInYourself = EditorGUILayout.Toggle(selectedSetup.FindInYourself, GUILayout.Width(18));
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Blend to yourself", GUILayout.Width(120));
				selectedSetup.BlendToYourself = EditorGUILayout.Toggle(selectedSetup.BlendToYourself, GUILayout.Width(18));

			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Cut times on end", GUILayout.Width(120));
				selectedSetup.CutTimeOnEnds = EditorGUILayout.Toggle(selectedSetup.CutTimeOnEnds, GUILayout.Width(18));
				GUILayout.BeginHorizontal();
			}
			GUILayout.EndHorizontal();
			{
				if (selectedSetup.CutTimeOnEnds)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(10);
						float width = 75;

						GUILayout.Label("Cut time from start", GUILayout.Width(120));
						selectedSetup.CutTimeFromStart = Mathf.Clamp(
							EditorGUILayout.FloatField(selectedSetup.CutTimeFromStart, GUILayout.Width(width)),
							0f,
							float.MaxValue
							);
						GUILayout.Space(horizontalMargin);
						GUILayout.Label("Cut time from end", GUILayout.Width(120));
						selectedSetup.CutTimeToEnd = Mathf.Clamp(
							EditorGUILayout.FloatField(selectedSetup.CutTimeToEnd, GUILayout.Width(width)),
							0f,
							float.MaxValue
							);
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawClipsList()
		{
			GUILayout.Space(10);

			if (!GUILayoutElements.DrawHeader(
				"Drag and drop animations here",
				GUIResources.GetLightHeaderStyle_MD(),
				GUIResources.GetMediumHeaderStyle_MD(),
				ref selectedSetup.AnimationFold
				))
			{
				return;
			}
			AddingAnimationByDropingInLastRect();

			GUILayout.Space(5);

			animationsScroll = GUILayout.BeginScrollView(animationsScroll);
			{
				for (int i = 0; i < selectedSetup.clips.Count; i++)
				{
					GUILayout.BeginHorizontal();
					{
						EditorGUILayout.ObjectField(selectedSetup.clips[i], typeof(AnimationClip), true);
						if (GUILayout.Button("X", GUILayout.Width(20)))
						{
							selectedSetup.clips.RemoveAt(i);
							i--;
						}
						GUILayout.EndHorizontal();
					}
				}
			}
			GUILayout.EndScrollView();

			GUILayout.Space(5);

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Clear"))
				{
					selectedSetup.clips.Clear();
				}

				if (GUILayout.Button("Remove nulls"))
				{
					for (int i = 0; i < selectedSetup.clips.Count; i++)
					{
						if (selectedSetup.clips[i] == null)
						{
							selectedSetup.clips.RemoveAt(i);
							i--;
						}
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawBlendTrees()
		{
			if (!GUILayoutElements.DrawHeader(
				"Blend trees",
				GUIResources.GetLightHeaderStyle_MD(),
				GUIResources.GetMediumHeaderStyle_MD(),
				ref selectedSetup.BlendTreesFold
				))
			{
				return;
			}
		}

		private void AddingAnimationByDropingInLastRect()
		{
			Event e = Event.current;
			Rect dropRect = GUILayoutUtility.GetLastRect();
			if (dropRect.Contains(e.mousePosition))
			{
				if (Event.current.type == EventType.DragUpdated)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
				}
				else if (Event.current.type == EventType.DragPerform)
				{
					bool correctData = true;
					List<AnimationClip> newData = new List<AnimationClip>();
					for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
					{
						try
						{
							newData.Add((AnimationClip)DragAndDrop.objectReferences[i]);
						}
						catch (Exception)
						{
							correctData = false;
							break;
						}
					}

					if (correctData)
					{
						for (int i = 0; i < newData.Count; i++)
						{
							if (!selectedSetup.clips.Contains(newData[i]))
							{
								selectedSetup.clips.Add(newData[i]);
							}
						}
					}
					Event.current.Use();
				}
			}
		}

		#endregion


		#region Data calculation

		PreparingDataPlayableGraph graph;

		private void CalculateDataButton(
			MotionMatchingDataCreationSetup setup,
			bool calculateClips = true,
			bool calculateBlendTrees = true,
			bool calculateAnimationSequences = true
			)
		{
			if (graph != null && graph.IsValid())
			{
				graph.Destroy();
			}

			creator.TrajectorySettings.TrajectoryTimes.Sort();

			string saveFolder;

			if (setup.saveDataPath == "")
			{
				saveFolder = EditorUtility.OpenFolderPanel("Place for save motion matching data", "Assets", "");

				setup.saveDataPath = saveFolder != "" ? saveFolder : setup.saveDataPath;
			}
			else
			{
				saveFolder = EditorUtility.OpenFolderPanel("Place for save motion matching data", setup.saveDataPath, "");

				setup.saveDataPath = saveFolder != "" ? saveFolder : setup.saveDataPath;
			}

			Vector3 position = creator.AnimatedGameObject.transform.position;
			Quaternion rotation = creator.AnimatedGameObject.transform.rotation;

			if (saveFolder == "") { return; }

			PreparingDataPlayableGraph calculationGraph = new PreparingDataPlayableGraph();
			calculationGraph.Initialize(creator.AnimatedGameObject);

			BonCalculationSettingsProfile[] bonesSettingsArray = creator.BonesMask.GetProfilesWithTransforms(creator.AnimatedGameObject.transform);


			if (calculateClips)
			{
				CalculateNormalClips(
					setup,
					calculationGraph,
					saveFolder,
					bonesSettingsArray
					);

				calculationGraph.ClearMainMixerInput();

			}

			//if (calculateBlendTrees)
			//{
			//	CalculateBlendTrees(
			//		calculationGraph,
			//		saveFolder,
			//		bonesMask
			//		);
			//	calculationGraph.ClearMainMixerInput();

			//}

			//if (calculateAnimationSequences)
			//{
			//	CalculateAnimationsSequences(
			//		calculationGraph,
			//		saveFolder,
			//		bonesMask
			//		);

			//	calculationGraph.Destroy();
			//}		

			AssetDatabase.SaveAssets();
			creator.AnimatedGameObject.transform.position = position;
			creator.AnimatedGameObject.transform.rotation = rotation;
		}

		private void CalculateNormalClips(
			MotionMatchingDataCreationSetup setup,
			PreparingDataPlayableGraph graph,
			string saveFolder,
			BonCalculationSettingsProfile[] bonesMask
			)
		{

			foreach (AnimationClip clip in setup.clips)
			{
				if (clip != null)
				{
					MotionMatchingData newCreatedAsset = MotionDataCalculator.CalculateNormalData(
						creator.AnimatedGameObject,
						graph,
						clip,
						bonesMask,
						setup.PosesPerSecond,
						clip.isLooping,
						creator.AnimatedGameObject.transform,
						creator.TrajectorySettings.TrajectoryTimes,
						setup.BlendToYourself,
						setup.FindInYourself
						);
					string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + newCreatedAsset.name + ".asset";
					MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));

					if (setup.CutTimeOnEnds)
					{
						if (setup.CutTimeFromStart > 0f)
						{
							newCreatedAsset.neverChecking.timeIntervals.Add(
								new float2(
									0f,
									math.clamp(setup.CutTimeFromStart, 0f, newCreatedAsset.animationLength)
									));
						}

						if (setup.CutTimeToEnd > 0f)
						{
							newCreatedAsset.neverChecking.timeIntervals.Add(
								new float2(
									math.clamp(newCreatedAsset.animationLength - setup.CutTimeToEnd, 0f, newCreatedAsset.animationLength),
									newCreatedAsset.animationLength
									));
						}
					}

					if (loadedAsset == null)
					{
						AssetDatabase.CreateAsset(newCreatedAsset, path);
					}
					else
					{
						loadedAsset.UpdateFromOther(newCreatedAsset, newCreatedAsset.name, setup.OverrideTrajectory);

						if (loadedAsset.contactPoints != null)
						{
							if (loadedAsset.contactPoints.Count > 0)
							{
								MotionDataCalculator.CalculateContactPoints(
									loadedAsset,
									loadedAsset.contactPoints.ToArray(),
									graph,
									creator.AnimatedGameObject
									);
							}
						}


						EditorUtility.SetDirty(loadedAsset);
						//AssetDatabase.SaveAssets();
					}

				}
				else
				{
					Debug.Log("Element is null");
				}
			}
			Debug.Log("Calculation of normal clips completed!");
		}

		//private void CalculateBlendTrees(
		//	PreparingDataPlayableGraph graph,
		//	string saveFolder,
		//	List<Transform> bonesMask
		//	)
		//{
		//	if (creator.trajectorySettings != null)
		//	{
		//		creator.trajectoryStepTimes.Sort();
		//	}
		//	else
		//	{
		//		creator.trajectorySettings.TrajectoryTimes.Sort();
		//	}
		//	foreach (BlendTreeInfo info in creator.blendTrees)
		//	{
		//		if (!info.IsValid())
		//		{
		//			continue;
		//		}
		//		if (info.useSpaces && info.clips.Count == 2)
		//		{

		//			for (int spaces = 1; spaces <= info.spaces; spaces++)
		//			{
		//				float currentFactor = (float)spaces / (info.spaces + 1);

		//				float[] clipsWeights = new float[info.clips.Count];

		//				clipsWeights[0] = currentFactor;
		//				clipsWeights[1] = 1f - currentFactor;
		//				//Debug.Log(clipsWeights[0]);
		//				//Debug.Log(clipsWeights[1]);

		//				MotionMatchingData dataToSave = MotionDataCalculator.CalculateBlendTreeData(
		//					info.name + currentFactor.ToString(),
		//					creator.gameObjectTransform.gameObject,
		//					graph,
		//					info.clips.ToArray(),
		//					bonesMask,
		//					creator.bonesWeights,
		//					creator.gameObjectTransform,
		//					creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
		//					clipsWeights,
		//					creator.posesPerSecond,
		//					false,
		//					info.blendToYourself,
		//					info.findInYourself
		//					);

		//				string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + dataToSave.name + ".asset";

		//				MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));

		//				if (loadedAsset == null)
		//				{
		//					AssetDatabase.CreateAsset(dataToSave, path);
		//					//AssetDatabase.SaveAssets();
		//				}
		//				else
		//				{
		//					loadedAsset.UpdateFromOther(dataToSave, dataToSave.name, creator.OverrideTrajectory);

		//					if (loadedAsset.contactPoints != null)
		//					{
		//						if (loadedAsset.contactPoints.Count > 0)
		//						{
		//							MotionDataCalculator.CalculateContactPoints(
		//								loadedAsset,
		//								loadedAsset.contactPoints.ToArray(),
		//								graph,
		//								creator.gameObjectTransform.gameObject
		//								);
		//						}
		//					}

		//					EditorUtility.SetDirty(loadedAsset);
		//					//AssetDatabase.SaveAssets();
		//				}
		//			}
		//		}
		//		else
		//		{
		//			MotionMatchingData dataToSave = MotionDataCalculator.CalculateBlendTreeData(
		//					info.name,
		//					creator.gameObjectTransform.gameObject,
		//					graph,
		//					info.clips.ToArray(),
		//					bonesMask,
		//					creator.bonesWeights,
		//					creator.gameObjectTransform,
		//					creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
		//					info.clipsWeights.ToArray(),
		//					creator.posesPerSecond,
		//					false,
		//					info.blendToYourself,
		//					info.findInYourself
		//					);

		//			string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + dataToSave.name + ".asset";

		//			MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));
		//			if (loadedAsset == null)
		//			{
		//				AssetDatabase.CreateAsset(dataToSave, path);
		//				//AssetDatabase.SaveAssets();
		//			}
		//			else
		//			{
		//				loadedAsset.UpdateFromOther(dataToSave, dataToSave.name, creator.OverrideTrajectory);
		//				EditorUtility.SetDirty(loadedAsset);
		//				//AssetDatabase.SaveAssets();
		//			}
		//		}
		//	}
		//	Debug.Log("Calculation of Blend trees completed!");
		//}

		//private void CalculateAnimationsSequences(
		//	PreparingDataPlayableGraph graph,
		//	string saveFolder,
		//	List<Transform> bonesMask
		//	)
		//{
		//	if (creator.trajectorySettings != null)
		//	{
		//		creator.trajectoryStepTimes.Sort();
		//	}
		//	else
		//	{
		//		creator.trajectorySettings.TrajectoryTimes.Sort();
		//	}
		//	foreach (AnimationsSequence seq in creator.sequences)
		//	{
		//		if (!seq.IsValid())
		//		{
		//			continue;
		//		}

		//		MotionMatchingData newCreatedAsset = MotionDataCalculator.CalculateAnimationSequenceData(
		//			seq.name,
		//			seq,
		//			creator.gameObjectTransform.gameObject,
		//			graph,
		//			bonesMask,
		//			creator.bonesWeights,
		//			creator.posesPerSecond,
		//			true,
		//			creator.gameObjectTransform,
		//			creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
		//			seq.blendToYourself,
		//			seq.findInYourself
		//			);
		//		string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + newCreatedAsset.name + ".asset";
		//		MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));

		//		float startTime = 0f;
		//		float endTime = 0f;
		//		float delta = 0.1f;
		//		for (int i = 0; i < seq.findPoseInClip.Count; i++)
		//		{
		//			endTime += (seq.neededInfo[i].y - seq.neededInfo[i].x);
		//			if (!seq.findPoseInClip[i])
		//			{
		//				float startB = startTime;
		//				float endB = endTime;
		//				if (i == seq.findPoseInClip.Count - 1)
		//				{
		//					if (seq.findPoseInClip[0])
		//					{
		//						endB = endTime - delta;
		//					}
		//					if (seq.findPoseInClip[i - 1])
		//					{
		//						startB = startTime + delta;
		//					}
		//				}
		//				else if (i == 0)
		//				{
		//					if (seq.findPoseInClip[i + 1])
		//					{
		//						endB = endTime - delta;
		//					}
		//					if (seq.findPoseInClip[seq.findPoseInClip.Count - 1])
		//					{
		//						startB = startTime + delta;
		//					}
		//				}
		//				else
		//				{
		//					if (seq.findPoseInClip[i + 1])
		//					{
		//						endB = endTime - delta;
		//					}
		//					if (seq.findPoseInClip[i - 1])
		//					{
		//						startB = startTime + delta;
		//					}
		//				}

		//				newCreatedAsset.neverChecking.timeIntervals.Add(new Vector2(startB, endB));

		//			}
		//			startTime = endTime;
		//		}

		//		if (loadedAsset == null)
		//		{
		//			AssetDatabase.CreateAsset(newCreatedAsset, path);
		//			//AssetDatabase.SaveAssets();
		//		}
		//		else
		//		{
		//			loadedAsset.UpdateFromOther(newCreatedAsset, newCreatedAsset.name, creator.OverrideTrajectory);

		//			if (loadedAsset.contactPoints != null)
		//			{
		//				if (loadedAsset.contactPoints.Count > 0)
		//				{
		//					MotionDataCalculator.CalculateContactPoints(
		//						loadedAsset,
		//						loadedAsset.contactPoints.ToArray(),
		//						graph,
		//						creator.gameObjectTransform.gameObject
		//						);
		//				}
		//			}

		//			EditorUtility.SetDirty(loadedAsset);
		//			//AssetDatabase.SaveAssets();
		//		}

		//	}

		//	Debug.Log("Calculation of sequences completed!");
		//}
		#endregion
	}
}