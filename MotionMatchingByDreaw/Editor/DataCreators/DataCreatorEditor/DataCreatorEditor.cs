using MotionMatching.Gameplay;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class DataCreatorEditor : EditorWindow
	{
		Rect creatorPlacing;
		Rect creatorTypeSelecting;
		Rect elementsListSection;
		Rect elementOptionsSection;
		//Rect previewRect;

		float resizeFactor = 0.4f;
		bool resizing = false;

		Vector2 scrollView_L, scrollView_R;

		[SerializeField]
		DataCreator creator;

		string[] creatorTypes;
		int selectedCreator = 0;

		// Preview Animation
		bool playAnimation = false;
		float previewTime = 0f;


		float sliderTime;
		float sliderTimeBuffor;
		float maxSliderTime;

		Vector3 pos;
		Quaternion rot;

		PreparingDataPlayableGraph graph;


		private void SetAsset(DataCreator asset)
		{
			this.creator = asset;
		}

		[MenuItem("MotionMatching/Data Creators/Data Creator Old", priority = 2)]
		public static void ShowWindow()
		{
			DataCreatorEditor editor = EditorWindow.GetWindow<DataCreatorEditor>();
			editor.position = new Rect(new Vector2(100, 100), new Vector2(800, 600));
			editor.titleContent = new GUIContent("Data Creator");
		}

		[OnOpenAssetAttribute(2)]
		public static bool OnAssetOpen(int instanceID, int line)
		{
			DataCreator asset;
			try
			{
				asset = (DataCreator)EditorUtility.InstanceIDToObject(instanceID);
			}
			catch (System.Exception)
			{
				return false;
			}

			if (EditorWindow.HasOpenInstances<DataCreatorEditor>())
			{
				EditorWindow.GetWindow<DataCreatorEditor>().SetAsset(asset);
				EditorWindow.GetWindow<DataCreatorEditor>().Repaint();
				return true;
			}

			DataCreatorEditor.ShowWindow();
			EditorWindow.GetWindow<DataCreatorEditor>().SetAsset(asset);
			EditorWindow.GetWindow<DataCreatorEditor>().Repaint();

			return true;
		}

		private void OnEnable()
		{
			scrollView_L = Vector2.zero;
			scrollView_R = Vector2.zero;
			InitRects();
			creatorTypes = new string[2];
			creatorTypes[0] = "Basic Options";
			creatorTypes[1] = "Blend Trees";

			playAnimation = false;

			if (creator != null)
			{
				if (creator.gameObjectTransform != null)
				{
					pos = creator.gameObjectTransform.position;
					rot = creator.gameObjectTransform.rotation;
				}
			}
		}

		private void OnGUI()
		{
			AutoResizeRects();
			DrawTexturesInRects();
			Event e = Event.current;
			GUILayoutElements.ResizingRectsHorizontal(
				this,
				ref elementsListSection,
				ref elementOptionsSection,
				e,
				ref resizing,
				12f
				);
			resizeFactor = elementsListSection.width / this.position.width;

			DrawCreatorPlacing();
			DrawCreatorTypes();

			if (this.creator != null)
			{
				DrawElementListSection();
				DrawElementOptionSection();

				EditorUtility.SetDirty(this.creator);
			}
			if (creator != null && creator.gameObjectTransform != null)
			{
				PlayingAnimation();
			}
		}

		private void OnDisable()
		{
			DestroyPlayableGraph();
		}

		private void OnDestroy()
		{
			if (graph != null)
			{
				if (graph.IsValid())
				{
					graph.Destroy();
				}
			}
		}

		private void Update()
		{
			if (EditorApplication.isPlaying && !EditorApplication.isPaused)
			{
				if (graph != null && graph.IsValid())
				{
					graph.Destroy();
					graph = null;
				}
			}

			if (creator != null)
			{
				Undo.RecordObject(this, "Some Random text");
				EditorUtility.SetDirty(this);
			}
		}

		private void InitializePlayableGraph()
		{
			if (creator.gameObjectTransform == null)
			{
				return;
			}
			if (graph != null)
			{
				if (graph.IsValid())
				{
					graph.Destroy();
				}
			}

			graph = new PreparingDataPlayableGraph();
			graph.Initialize(creator.gameObjectTransform.gameObject);
		}

		private void DestroyPlayableGraph()
		{
			if (graph != null)
			{
				if (graph.IsValid())
				{
					graph.Destroy();
				}
			}
		}

		private void InitRects()
		{
			creatorPlacing = new Rect(
				0f,
				0f,
				this.position.width,
				25f
				);
			creatorTypeSelecting = new Rect(
				0f,
				creatorPlacing.x + creatorPlacing.height,
				this.position.width,
				25f
				);

			elementsListSection = new Rect(
				0f,
				creatorTypeSelecting.y + creatorTypeSelecting.height,
				resizeFactor * this.position.width,
				this.position.height - creatorTypeSelecting.y + creatorTypeSelecting.height
				);
			elementOptionsSection = new Rect(
				elementsListSection.x + elementsListSection.width,
				creatorTypeSelecting.y + creatorTypeSelecting.height,
				(1f - resizeFactor) * this.position.width,
				this.position.height - creatorTypeSelecting.y + creatorTypeSelecting.height
				);
			//previewRect = new Rect();

		}

		private void DrawTexturesInRects()
		{
			GUI.DrawTexture(creatorPlacing, GUIResources.GetDarkTexture());
			GUI.DrawTexture(creatorTypeSelecting, GUIResources.GetGraphSpaceTexture());
			GUI.DrawTexture(elementsListSection, GUIResources.GetMediumTexture_2());
			GUI.DrawTexture(elementOptionsSection, GUIResources.GetMediumTexture_1());
		}

		private void AutoResizeRects()
		{
			creatorPlacing.Set(
				0f,
				0f,
				this.position.width,
				25f
				);
			creatorTypeSelecting.Set(
				0f,
				creatorPlacing.y + creatorPlacing.height,
				this.position.width,
				25f
				);
			elementsListSection.Set(
				0f,
				creatorTypeSelecting.y + creatorTypeSelecting.height,
				resizeFactor * this.position.width,
				this.position.height - (creatorTypeSelecting.y + creatorTypeSelecting.height)
				);
			elementOptionsSection.Set(
				elementsListSection.x + elementsListSection.width,
				creatorTypeSelecting.y + creatorTypeSelecting.height,
				this.position.width - elementsListSection.width,
				this.position.height - (creatorTypeSelecting.y + creatorTypeSelecting.height)
				);
		}

		private void DrawCreatorPlacing()
		{
			GUILayout.BeginArea(creatorPlacing);
			GUILayout.Space(3);
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			creator = (DataCreator)EditorGUILayout.ObjectField(
				new GUIContent("Data creator"),
				creator,
				typeof(DataCreator),
				true
				);
			if (GUILayout.Button("Calculate Data") && creator != null)
			{
				if (creator.gameObjectTransform != null && creator.BonesMask != null && creator.BonesMask.IsValid)
				{
					CalculateDataButton();
				}
			}
			GUILayout.Space(10);
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private void DrawCreatorTypes()
		{
			GUILayout.BeginArea(creatorTypeSelecting);
			GUILayout.Space(3);
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			selectedCreator = GUILayout.Toolbar(selectedCreator, creatorTypes);
			GUILayout.Space(10);
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private void DrawElementListSection()
		{
			GUILayout.BeginArea(elementsListSection);
			scrollView_L = GUILayout.BeginScrollView(scrollView_L);
			switch (selectedCreator)
			{
				case 0:
					CreatorBasicOption.DrawOptions(this.creator);

					GUILayout.Space(10);

					if (GUILayout.Button("Calculate only clips", GUIResources.Button_MD()) && creator.gameObjectTransform != null)
					{
						CalculateDataButton(true, false);
					}
					break;
				case 1:
					BlendTreesOptions.DrawTreesList(this.creator, this);

					if (GUILayout.Button("Calculate only Blend Trees", GUIResources.Button_MD()) && creator.gameObjectTransform != null)
					{
						CalculateDataButton(false, true);
					}
					break;
			}
			GUILayout.Space(10);
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawElementOptionSection()
		{
			GUILayout.BeginArea(elementOptionsSection);
			scrollView_R = GUILayout.BeginScrollView(scrollView_R);
			switch (selectedCreator)
			{
				case 0:
					CreatorBasicOption.DrawAnimationList(this.creator);

					break;
				case 1:
					BlendTreesOptions.DrawTreesElements(this.creator, this);
					if (creator.selectedBlendTree < creator.blendTrees.Count && creator.selectedBlendTree >= 0)
					{
						PreviewAnimation();
					}
					break;
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void CalculateDataButton(
			bool calculateClips = true,
			bool calculateBlendTrees = true
			)
		{
			if (graph != null && graph.IsValid())
			{
				graph.Destroy();
			}
			creator.trajectoryStepTimes.Sort();

			string saveFolder;

			if (creator.saveDataPath == "")
			{
				saveFolder = EditorUtility.OpenFolderPanel("Place for save motion matching data", "Assets", "");

				creator.saveDataPath = saveFolder != "" ? saveFolder : creator.saveDataPath;
			}
			else
			{
				saveFolder = EditorUtility.OpenFolderPanel("Place for save motion matching data", creator.saveDataPath, "");

				creator.saveDataPath = saveFolder != "" ? saveFolder : creator.saveDataPath;
			}

			Vector3 position = creator.gameObjectTransform.position;
			Quaternion rotation = creator.gameObjectTransform.rotation;

			if (saveFolder == "") { return; }

			PreparingDataPlayableGraph calculationGraph = new PreparingDataPlayableGraph();
			calculationGraph.Initialize(creator.gameObjectTransform.gameObject);

			BonCalculationSettingsProfile[] bonesMask = creator.BonesMask.GetProfilesWithTransforms(creator.gameObjectTransform);

			if (calculateClips)
			{
				CalculateNormalClips(
					calculationGraph,
					saveFolder,
					bonesMask
					);

				calculationGraph.ClearMainMixerInput();

			}

			if (calculateBlendTrees)
			{
				CalculateBlendTrees(
					calculationGraph,
					saveFolder,
					bonesMask
					);
				calculationGraph.ClearMainMixerInput();

			}

			AssetDatabase.SaveAssets();
			creator.gameObjectTransform.position = position;
			creator.gameObjectTransform.rotation = rotation;
		}

		private void CalculateNormalClips(
			PreparingDataPlayableGraph graph,
			string saveFolder,
			BonCalculationSettingsProfile[] bonesMask
			)
		{
			if (creator.trajectorySettings != null)
			{
				creator.trajectoryStepTimes.Sort();
			}
			else
			{
				creator.trajectorySettings.TrajectoryTimes.Sort();
			}

			foreach (AnimationClip clip in creator.clips)
			{
				if (clip != null)
				{
					MotionMatchingData newCreatedAsset = MotionDataCalculator.CalculateNormalData(
						creator.gameObjectTransform.gameObject,
						graph,
						clip,
						bonesMask,
						creator.posesPerSecond,
						clip.isLooping,
						creator.gameObjectTransform,
						creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
						creator.blendToYourself,
						creator.findInYourself
						);
					string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + newCreatedAsset.name + ".asset";
					MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));

					if (creator.cutTimeFromStart > 0f)
					{
						newCreatedAsset.neverChecking.timeIntervals.Add(
							new float2(
								0f,
								math.clamp(creator.cutTimeFromStart, 0f, newCreatedAsset.animationLength)
								));
					}

					if (creator.cutTimeToEnd > 0f)
					{
						newCreatedAsset.neverChecking.timeIntervals.Add(
							new float2(
								math.clamp(newCreatedAsset.animationLength - creator.cutTimeToEnd, 0f, newCreatedAsset.animationLength),
								newCreatedAsset.animationLength
								));
					}

					if (loadedAsset == null)
					{
						AssetDatabase.CreateAsset(newCreatedAsset, path);
					}
					else
					{
						loadedAsset.UpdateFromOther(newCreatedAsset, newCreatedAsset.name, creator.OverrideTrajectory);

						if (loadedAsset.contactPoints != null)
						{
							if (loadedAsset.contactPoints.Count > 0)
							{
								MotionDataCalculator.CalculateContactPoints(
									loadedAsset,
									loadedAsset.contactPoints.ToArray(),
									graph,
									creator.gameObjectTransform.gameObject
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

		private void CalculateBlendTrees(
			PreparingDataPlayableGraph graph,
			string saveFolder,
			BonCalculationSettingsProfile[] bonesMask
			)
		{
			if (creator.trajectorySettings != null)
			{
				creator.trajectoryStepTimes.Sort();
			}
			else
			{
				creator.trajectorySettings.TrajectoryTimes.Sort();
			}
			foreach (BlendTreeInfo info in creator.blendTrees)
			{
				if (!info.IsValid())
				{
					continue;
				}
				if (info.useSpaces && info.clips.Count == 2)
				{

					for (int spaces = 1; spaces <= info.spaces; spaces++)
					{
						float currentFactor = (float)spaces / (info.spaces + 1);

						float[] clipsWeights = new float[info.clips.Count];

						clipsWeights[0] = currentFactor;
						clipsWeights[1] = 1f - currentFactor;
						//Debug.Log(clipsWeights[0]);
						//Debug.Log(clipsWeights[1]);

						MotionMatchingData dataToSave = MotionDataCalculator.CalculateBlendTreeData(
							info.name + currentFactor.ToString(),
							creator.gameObjectTransform.gameObject,
							graph,
							info.clips.ToArray(),
							bonesMask,
							creator.bonesWeights,
							creator.gameObjectTransform,
							creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
							clipsWeights,
							creator.posesPerSecond,
							false,
							info.blendToYourself,
							info.findInYourself
							);

						string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + dataToSave.name + ".asset";

						MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));

						if (loadedAsset == null)
						{
							AssetDatabase.CreateAsset(dataToSave, path);
							//AssetDatabase.SaveAssets();
						}
						else
						{
							loadedAsset.UpdateFromOther(dataToSave, dataToSave.name, creator.OverrideTrajectory);

							if (loadedAsset.contactPoints != null)
							{
								if (loadedAsset.contactPoints.Count > 0)
								{
									MotionDataCalculator.CalculateContactPoints(
										loadedAsset,
										loadedAsset.contactPoints.ToArray(),
										graph,
										creator.gameObjectTransform.gameObject
										);
								}
							}

							EditorUtility.SetDirty(loadedAsset);
							//AssetDatabase.SaveAssets();
						}
					}
				}
				else
				{
					MotionMatchingData dataToSave = MotionDataCalculator.CalculateBlendTreeData(
							info.name,
							creator.gameObjectTransform.gameObject,
							graph,
							info.clips.ToArray(),
							bonesMask,
							creator.bonesWeights,
							creator.gameObjectTransform,
							creator.trajectorySettings != null ? creator.trajectorySettings.TrajectoryTimes : creator.trajectoryStepTimes,
							info.clipsWeights.ToArray(),
							creator.posesPerSecond,
							false,
							info.blendToYourself,
							info.findInYourself
							);

					string path = saveFolder.Substring(Application.dataPath.Length - 6) + "/" + dataToSave.name + ".asset";

					MotionMatchingData loadedAsset = (MotionMatchingData)AssetDatabase.LoadAssetAtPath(path, typeof(MotionMatchingData));
					if (loadedAsset == null)
					{
						AssetDatabase.CreateAsset(dataToSave, path);
						//AssetDatabase.SaveAssets();
					}
					else
					{
						loadedAsset.UpdateFromOther(dataToSave, dataToSave.name, creator.OverrideTrajectory);
						EditorUtility.SetDirty(loadedAsset);
						//AssetDatabase.SaveAssets();
					}
				}
			}
			Debug.Log("Calculation of Blend trees completed!");
		}

		private void PreviewAnimation()
		{
			GUILayout.Label("Preview Animations", GUIResources.GetLightHeaderStyle_MD());
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Preview") && creator.gameObjectTransform != null)
			{
				PreviewButtonAction();
			}
			if (GUILayout.Button("Play") && creator.gameObjectTransform != null)
			{
				PlayButtonAction();
			}

			if (GUILayout.Button("Pause"))
			{
				PauseButtonAction();
			}
			if (GUILayout.Button("Stop") && creator.gameObjectTransform != null)
			{
				StopButtonAction();
			}
			//if (GUILayout.Button("Destory graph"))
			//{

			//}
			GUILayout.EndHorizontal();

			//sliderTime = EditorGUILayout.Slider(sliderTime, 0f, maxSliderTime);
		}

		private void PreviewButtonAction()
		{
			creator.gameObjectTransform.position = pos;
			creator.gameObjectTransform.rotation = rot;

			sliderTime = 0f;
			sliderTimeBuffor = sliderTime;

			if (graph == null)
			{
				graph = new PreparingDataPlayableGraph();
				graph.Initialize(creator.gameObjectTransform.gameObject);
			}
			else if (!graph.IsValid())
			{
				graph.Initialize(creator.gameObjectTransform.gameObject);
			}

			graph.ClearMainMixerInput();

			switch (selectedCreator)
			{
				case 1:
					BlendTreeInfo blendTree = creator.blendTrees[creator.selectedBlendTree];
					if (blendTree.IsValid())
					{
						blendTree.CreateGraphFor(
							creator.gameObjectTransform.gameObject,
							graph
							);

						maxSliderTime = blendTree.GetLength();
					}
					break;
			}
		}

		private void PlayButtonAction()
		{
			previewTime = Time.realtimeSinceStartup;
			playAnimation = true;
		}

		private void PauseButtonAction()
		{
			playAnimation = false;
		}

		private void StopButtonAction()
		{
			sliderTime = 0f;
			sliderTimeBuffor = sliderTime;
			creator.gameObjectTransform.position = pos;
			creator.gameObjectTransform.rotation = rot;
			playAnimation = false;
		}

		private void PlayingAnimation()
		{
			if (playAnimation)
			{
				float deltaTime = Time.realtimeSinceStartup - previewTime;
				if (deltaTime != 0f)
				{
					UpdateAnimator(deltaTime);
				}
			}
			else
			{
				float deltaTime = sliderTime - sliderTimeBuffor;

				if (deltaTime != 0f)
				{
					sliderTimeBuffor = sliderTime;
					UpdateAnimator(deltaTime);
				}
			}
		}

		private void UpdateAnimator(float deltaTime)
		{
			if (this.graph == null || !this.graph.IsValid())
			{
				return;
			}
			sliderTimeBuffor = sliderTime;

			sliderTime %= maxSliderTime;

			previewTime = Time.realtimeSinceStartup;

			BlendTreeInfo element = creator.blendTrees[creator.selectedBlendTree];
			float weightSum = 0f;
			float[] weights = new float[element.clips.Count];
			if (weights.Length == graph.GetMixerInputCount())
			{
				for (int i = 0; i < element.clips.Count; i++)
				{
					weightSum += element.clipsWeights[i];
				}
				for (int i = 0; i < element.clips.Count; i++)
				{
					weights[i] = element.clipsWeights[i] / weightSum;
				}

				for (int i = 0; i < element.clips.Count; i++)
				{
					graph.SetMixerInputWeight(i, weights[i]);
				}
			}
			graph.Evaluate(deltaTime);

			this.Repaint();
		}
	}
}
