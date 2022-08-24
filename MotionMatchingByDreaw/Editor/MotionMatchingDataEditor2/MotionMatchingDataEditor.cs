using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class MotionMatchingDataEditor : EditorWindow
	{
		Rect leftSpace;
		Rect rightSpace;

		Rect leftSpaceLayout;
		Rect rightSpaceLayout;

		bool resizing = false;

		float resizeFactor;

		float margin = 10f;

		PreparingDataPlayableGraph playableGraph;

		bool isDataSwitched = false;
		MotionMatchingData currentEditedData;
		MotionMatchingData bufforDataToCheckSwitch = null;
		MotionMatchingData dataToCopyOptions;
		GameObject animatedObject;

		bool _bDrawTrajectory = true;
		bool _bDrawPose = true;

		float curveTimeIndicatorWidth = 1f;

		Texture2D backgroundTex;
		Texture2D pointerTex;


		//[MenuItem("MM Data Editor", menuItem = "MotionMatching/Data Editor/Motion Matching Data Editor Old", priority = 1)]
		//private static void ShowWindow()
		//{
		//	MotionMatchingDataEditor editor = EditorWindow.GetWindow<MotionMatchingDataEditor>();
		//	editor.titleContent = new GUIContent("MM Data Editor Old");
		//	editor.position = new Rect(100, 100, 1000, 300);
		//}

		//[OnOpenAssetAttribute(3)]
		//public static bool step1(int instanceID, int line)
		//{
		//	MotionMatchingData asset;
		//	try
		//	{
		//		asset = (MotionMatchingData)EditorUtility.InstanceIDToObject(instanceID);
		//	}
		//	catch (System.Exception)
		//	{
		//		return false;
		//	}

		//	if (EditorWindow.HasOpenInstances<MotionMatchingDataEditor>())
		//	{
		//		EditorWindow.GetWindow<MotionMatchingDataEditor>().SetAsset(asset);
		//		EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();
		//		return true;
		//	}

		//	MotionMatchingDataEditor.ShowWindow();
		//	EditorWindow.GetWindow<MotionMatchingDataEditor>().SetAsset(asset);
		//	EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();

		//	return true;
		//}

		private void SetAsset(MotionMatchingData asset)
		{
			this.bufforDataToCheckSwitch = asset;
		}

		private void OnEnable()
		{
			InitRect();

			playableGraph = new PreparingDataPlayableGraph();

			SceneView.duringSceneGui += OnSceneGUI;

			//Undo.undoRedoPerformed += OnUndoRedoPerformed;

			backgroundTex = new Texture2D(1, 1);
			backgroundTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f));
			backgroundTex.Apply();

			pointerTex = new Texture2D(1, 1);
			pointerTex.SetPixel(0, 0, new Color(1, 1, 1));
			pointerTex.Apply();
		}

		private void OnDestroy()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			playableGraph.Destroy();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			GUI.DrawTexture(leftSpace, GUIResources.GetMediumTexture_1());
			GUI.DrawTexture(rightSpace, GUIResources.GetMediumTexture_2());

			FitRects();
			ResizeRects(e);

			resizeFactor = leftSpace.width / this.position.width;

			DoLayoutLeftMenu(e);
			DoLayoutRightMenu(e);

			AnimationPlaying();
			OnCurrentAnimationTimeChange();


			if (currentEditedData != null)
			{
				EditorUtility.SetDirty(currentEditedData);
				Undo.RecordObject(currentEditedData, "MM_Data editor Change");
			}
		}

		private void Update()
		{
			if (playableGraph == null)
			{
				playableGraph = new PreparingDataPlayableGraph();
			}


			//if (gameObject != null || editedData != null)
			//{
			//	Undo.RecordObject(this, "Some Random text");
			//	EditorUtility.SetDirty(this);
			//}
		}

		private void OnSceneGUI(SceneView obj)
		{
			DrawSceneGUI(obj);
		}

		private static void OnUndoRedoPerformed()
		{
			MotionMatchingDataEditor editor = EditorWindow.GetWindow<MotionMatchingDataEditor>();
			if (editor == null)
			{
				Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			}
			else
			{
				editor.Repaint();
			}
		}

		private void InitRect()
		{
			resizeFactor = 0.7f;
			resizing = false;
			leftSpaceLayout = new Rect();
			rightSpaceLayout = new Rect();

			leftSpace = new Rect(0, 0, this.position.width * resizeFactor, this.position.height);
			rightSpace = new Rect(leftSpace.x + leftSpace.width, 0, this.position.width * (1f - resizeFactor), this.position.height);
		}

		private void FitRects()
		{
			leftSpace.height = this.position.height;
			rightSpace.height = this.position.height;

			rightSpace.x = leftSpace.x + leftSpace.width;

			leftSpace.width = resizeFactor * this.position.width;
			rightSpace.width = this.position.width - leftSpace.width;
		}

		private void ResizeRects(Event e)
		{
			GUILayoutElements.ResizingRectsHorizontal(
				this,
				ref leftSpace,
				ref rightSpace,
				e,
				ref resizing,
				7,
				7
				);
		}

		#region LEFT MENU

		// Properties
		//string[] optionsNames = { "Sections", "Contacts", "Curves", "IK Tracks", "Events" };
		//int selectedOption = 0;


		private enum MotionMatchingDataEditingTool
		{
			Sections,
			Contacts,
			Curves,
			IkTracks,
			Events
		}

		MotionMatchingDataEditingTool currentTool;


		Vector2 leftMenuScroll = Vector2.zero;

		// SECTIONS
		private enum SectionSelectedType
		{
			NotLookingForNewPoseSection,
			NeverLookingForNewPoseSection,
			NormalSection,
			None
		}

		SectionSelectedType selectedSectionType = SectionSelectedType.None;
		int selectedSectionIndex = -1;
		float betweenSectionsSpace = 5f;

		// CONTACTS
		//float contactOptionsMargin = 30f;

		bool drawContactsPositions = true;
		bool drawContactsRSN = false;

		bool drawPositionManipulator;
		bool drawRotationManipuator;

		// Functions
		private void DoLayoutLeftMenu(Event e)
		{
			rightSpaceLayout.Set(
				rightSpace.x + margin,
				rightSpace.y + margin,
				rightSpace.width - margin,
				rightSpace.height - margin
				);

			GUILayout.BeginArea(rightSpaceLayout);
			DrawLeftMenu(e);
			GUILayout.EndArea();
		}

		private void DrawLeftMenu(Event e)
		{
			DrawNeededAssets();
			GUILayout.Space(5);
			DrawCommonOptions();
			GUILayout.Space(5);
			DrawPosibleOptions();
			GUILayout.Space(10);
			leftMenuScroll = EditorGUILayout.BeginScrollView(leftMenuScroll);
			DrawSelectedOptionLeftMenu();
			EditorGUILayout.EndScrollView();
		}

		private void DrawNeededAssets()
		{
			GUILayout.BeginVertical();
			animatedObject = (GameObject)EditorGUILayout.ObjectField(animatedObject, typeof(GameObject), true);

			bufforDataToCheckSwitch = (MotionMatchingData)EditorGUILayout.ObjectField(bufforDataToCheckSwitch, typeof(MotionMatchingData), true);
			if (bufforDataToCheckSwitch != currentEditedData)
			{
				currentEditedData = bufforDataToCheckSwitch;
				isDataSwitched = true;
				if (currentEditedData != null)
				{
					OnAnimationDataSwitched();
				}
				this.Repaint();
			}
			else
			{
				isDataSwitched = false;
			}


			GUILayout.EndVertical();
			GUILayout.Space(5);
		}

		private void OnAnimationDataSwitched()
		{
			animationState = AnimationState.NotCreated;
			contactPointsRL = new ReorderableList(currentEditedData.contactPoints, typeof(MotionMatchingContact), true, false, true, true);

			selectedSectionType = SectionSelectedType.None;
			selectedSectionIndex = 0;
		}

		private void DrawCommonOptions()
		{
			GUILayout.BeginHorizontal();
			{
				_bDrawPose = GUILayout.Toggle(_bDrawPose, "Draw Pose", new GUIStyle("Button"));
				_bDrawTrajectory = GUILayout.Toggle(_bDrawTrajectory, "Draw Trajectory", new GUIStyle("Button"));
			}
			GUILayout.EndHorizontal();
		}

		private void DrawPosibleOptions()
		{
			//selectedOption = GUILayout.Toolbar(selectedOption, optionsNames);
			//selectedOption = EditorGUILayout.Popup(selectedOption, optionsNames);

			currentTool = (MotionMatchingDataEditingTool)EditorGUILayout.EnumPopup(currentTool);
		}

		private void DrawSelectedOptionLeftMenu()
		{
			if (currentEditedData == null)
			{
				return;
			}
			switch (currentTool)
			{
				case MotionMatchingDataEditingTool.Sections:
					{
						SectionOptionsLeftMenu();
					}
					break;
				case MotionMatchingDataEditingTool.Contacts:
					{
						ContactsOptionsLeftMenu();
					}
					break;
				case MotionMatchingDataEditingTool.Curves:
					{
						DrawCurvesLeftMenu();
					}
					break;
				case MotionMatchingDataEditingTool.IkTracks:
					{
						DrawIKTracksLeftMenu();
					}
					break;
				case MotionMatchingDataEditingTool.Events:
					{
						DrawAniamtionEventsLeftMenu();
					}
					break;
			}
		}

		private void SectionOptionsLeftMenu()
		{
			bool result;
			GUILayout.BeginVertical();

			// Selecting Not Looking for new pose section
			result = selectedSectionType == SectionSelectedType.NotLookingForNewPoseSection;
			if (GUILayoutElements.DrawHeader(
					"NotLookingForNewPose",
					GUIResources.GetLightHeaderStyle_MD(),
					GUIResources.GetDarkHeaderStyle_SM(),
					result
					))
			{
				selectedSectionType = SectionSelectedType.NotLookingForNewPoseSection;
				selectedSectionIndex = -1;
			}
			GUILayout.Space(betweenSectionsSpace);
			// Selecting Never Looking for new pose section
			result = selectedSectionType == SectionSelectedType.NeverLookingForNewPoseSection;
			if (GUILayoutElements.DrawHeader(
					"NeverChecking",
					GUIResources.GetLightHeaderStyle_MD(),
					GUIResources.GetDarkHeaderStyle_SM(),
					result
					))
			{
				selectedSectionType = SectionSelectedType.NeverLookingForNewPoseSection;
				selectedSectionIndex = -1;

			}
			GUILayout.Space(betweenSectionsSpace);
			// Selecting other sections
			result = selectedSectionType == SectionSelectedType.NormalSection;

			for (int i = 0; i < currentEditedData.sections.Count; i++)
			{
				if (GUILayoutElements.DrawHeader(
					$"{i + 1}. {currentEditedData.sections[i].sectionName}",
					GUIResources.GetLightHeaderStyle_MD(),
					GUIResources.GetDarkHeaderStyle_SM(),
					result && i == selectedSectionIndex
					))
				{
					selectedSectionType = SectionSelectedType.NormalSection;
					selectedSectionIndex = i;

				}
				GUILayout.Space(betweenSectionsSpace);
			}
			GUILayout.EndVertical();
		}


		private void ContactsOptionsLeftMenu()
		{
			GUILayout.Space(5);
			ContactPointsDrawingOptionsInSceneView();
			ContactsButtonOptions();

			Rect lastRect = GUILayoutUtility.GetLastRect();



			Rect sliderTestRect = new Rect(
				lastRect.x,
				lastRect.y + lastRect.height,
				leftSpace.width * 0.9f,
				20f
				);

			GUILayout.Space(10);
		}

		private void ContactPointsDrawingOptionsInSceneView()
		{
			GUILayout.Label("Calculated Contacts Draw options:");

			GUILayout.BeginHorizontal();
			{
				drawContactsPositions = GUILayout.Toggle(drawContactsPositions, "Position", new GUIStyle("Button"));

				drawContactsRSN = GUILayout.Toggle(drawContactsRSN, "Reverse Normal", new GUIStyle("Button"));
			}
			GUILayout.EndHorizontal();


		}

		private void ContactsButtonOptions()
		{
			GUILayout.Space(5);

			if (GUILayout.Button("Sort contacts", GUIResources.Button_MD()) && currentEditedData != null)
			{
				currentEditedData.contactPoints.Sort();
			}

			if (GUILayout.Button("Calculate Contacts", GUIResources.Button_MD()) && currentEditedData != null && animatedObject != null)
			{
				if (animatedObject == null)
				{
					Debug.LogWarning("Game object in MM Data Editor is NULL!");
					return;
				}
				else
				{
					currentEditedData.contactPoints.Sort();

					MotionDataCalculator.CalculateContactPoints(
						currentEditedData,
						currentEditedData.contactPoints.ToArray(),
						this.playableGraph,
						this.animatedObject
						);

					playableGraph.Initialize(animatedObject);
					playableGraph.CreateAnimationDataPlayables(currentEditedData, currentAnimaionTime);
				}
			}
		}


		#endregion

		#region RIGHT MENU

		Vector2 rightScroll = Vector2.zero;

		string playBTNText = "▶";
		string pauseBTNText = "||";
		float previewBTNWidth = 55f;
		float animPlayingBTNWidth = 25;
		float animSliderRightMargin = 10f;

		float currentAnimaionTime = 0f;
		float bufforAnimationTime = 0f;

		private enum AnimationState
		{
			NotCreated,
			Stoped,
			Playing
			//Paused
		}

		AnimationState animationState = AnimationState.NotCreated;

		DataSection selectedSection;
		ReorderableList sectionIntervalsRL;
		ReorderableList contactPointsRL;

		private void DoLayoutRightMenu(Event e)
		{
			leftSpaceLayout.Set(
				leftSpace.x + margin,
				leftSpace.y + margin,
				leftSpace.width - 2 * margin,
				leftSpace.height - margin
				);

			GUILayout.BeginArea(leftSpaceLayout);
			DrawRightMenu(e);
			GUILayout.EndArea();
		}

		private void DrawRightMenu(Event e)
		{
			DrawPlayAnimationOptions();
			DrawAboveScrollOptions();

			rightScroll = GUILayout.BeginScrollView(rightScroll);
			DrawRightMenuOptions(e);
			GUILayout.EndScrollView();
		}

		private void DrawPlayAnimationOptions()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Preview", GUILayout.Width(previewBTNWidth)) && currentEditedData != null && animatedObject != null)
			{
				//gameObject.transform.position = Vector3.zero;
				//gameObject.transform.rotation = Quaternion.identity;
				OnPreviewBTN();
			}
			if (animationState != AnimationState.Playing)
			{
				if (GUILayout.Button(playBTNText, GUILayout.Width(animPlayingBTNWidth)) && currentEditedData != null && animatedObject != null)
				{
					OnPlayBTN();

				}
			}
			else
			{
				if (GUILayout.Button(pauseBTNText, GUILayout.Width(animPlayingBTNWidth)) && currentEditedData != null && animatedObject != null)
				{
					OnPauseBTN();
				}
			}

			GUILayout.Space(5);

			currentAnimaionTime = EditorGUILayout.Slider(
				currentAnimaionTime,
				0f,
				currentEditedData != null ? currentEditedData.animationLength : 0f
				);
			GUILayout.Space(animSliderRightMargin);
			GUILayout.EndHorizontal();
		}


		//float PlayingTime = 0;
		float bufforPlayingTime = 0;
		bool previewBTNClick = false;

		private void OnPreviewBTN()
		{
			previewBTNClick = true;
			animationState = AnimationState.Stoped;

			float deltaTime = -currentAnimaionTime;
			if (playableGraph != null && playableGraph.IsValid())
			{
				playableGraph.ClearMainMixerInput();
				playableGraph.Destroy();
			}
			playableGraph = new PreparingDataPlayableGraph();
			playableGraph.Initialize(animatedObject);
			playableGraph.CreateAnimationDataPlayables(currentEditedData);
			currentAnimaionTime = 0f;
		}

		private void OnPlayBTN()
		{
			animationState = AnimationState.Playing;

			bufforPlayingTime = Time.realtimeSinceStartup;
		}

		private void OnPauseBTN()
		{
			animationState = AnimationState.Stoped;
		}

		private void DrawAboveScrollOptions()
		{
			if (currentEditedData == null)
			{
				return;
			}
			GUILayout.Space(5);
			switch (currentTool)
			{
				case MotionMatchingDataEditingTool.Sections:
					DrawRightSectionsOptionsAboveScroll();
					break;
				case MotionMatchingDataEditingTool.Contacts:
					DrawRightContactsOptionsAboveScrollOptions();
					break;
			}
			GUILayout.Space(10);
		}

		private void DrawRightSectionsOptionsAboveScroll()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Sort Intervals", GUIResources.Button_MD()))
			{
				selectedSection.timeIntervals.Sort(delegate (float2 x, float2 y)
				{
					if (x.x < y.x)
					{
						return -1;
					}
					return 1;
				});
			}
			if (GUILayout.Button("Set Interval Start", GUIResources.Button_MD()))
			{
				if (sectionIntervalsRL != null && selectedSection != null)
				{
					int selectedIntervalIndex = sectionIntervalsRL.index;
					if (0 <= selectedIntervalIndex && selectedIntervalIndex < selectedSection.timeIntervals.Count)
					{
						float2 newTimeInterval = new float2(
							currentAnimaionTime,
							selectedSection.timeIntervals[selectedIntervalIndex].y
							);

						selectedSection.timeIntervals[selectedIntervalIndex] = newTimeInterval;
					}
				}
			}
			if (GUILayout.Button("Set Interval End", GUIResources.Button_MD()))
			{
				if (sectionIntervalsRL != null && selectedSection != null)
				{
					int selectedIntervalIndex = sectionIntervalsRL.index;
					if (0 <= selectedIntervalIndex && selectedIntervalIndex < selectedSection.timeIntervals.Count)
					{
						float2 newTimeInterval = new float2(
							selectedSection.timeIntervals[selectedIntervalIndex].x,
							currentAnimaionTime
							);

						selectedSection.timeIntervals[selectedIntervalIndex] = newTimeInterval;
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawRightContactsOptionsAboveScrollOptions()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set Contact Start Time", GUIResources.Button_MD()) && currentEditedData != null)
			{
				if (contactPointsRL != null)
				{
					if (0 <= contactPointsRL.index && contactPointsRL.index < currentEditedData.contactPoints.Count)
					{
						MotionMatchingContact cp = currentEditedData.contactPoints[contactPointsRL.index];
						cp.SetStartTime(currentAnimaionTime);
						currentEditedData.contactPoints[contactPointsRL.index] = cp;
					}
				}
			}
			if (GUILayout.Button("Set Contact End Time", GUIResources.Button_MD()) && currentEditedData != null)
			{
				if (contactPointsRL != null)
				{
					if (0 <= contactPointsRL.index && contactPointsRL.index < currentEditedData.contactPoints.Count)
					{
						MotionMatchingContact cp = currentEditedData.contactPoints[contactPointsRL.index];
						cp.SetEndTime(currentAnimaionTime);
						currentEditedData.contactPoints[contactPointsRL.index] = cp;
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawRightMenuOptions(Event e)
		{
			if (currentEditedData == null)
			{
				return;
			}
			switch (currentTool)
			{
				case MotionMatchingDataEditingTool.Sections:
					if (currentEditedData != null)
					{
						DrawSelectedSectionOptions();
					}
					break;
				case MotionMatchingDataEditingTool.Contacts:
					if (currentEditedData != null)
					{
						DrawContactsOptionsOnRightMenu();
					}
					break;
				case MotionMatchingDataEditingTool.Curves:
					{
						DrawCurvesRightMenu();
					}
					break;
				case MotionMatchingDataEditingTool.IkTracks:
					{
						DrawIKTracksRightMenu();
					}
					break;
				case MotionMatchingDataEditingTool.Events:
					{
						OnDrawEventsRightMenu();
					}
					break;
			}
		}

		private void DrawSelectedSectionOptions()
		{
			switch (selectedSectionType)
			{
				case SectionSelectedType.NotLookingForNewPoseSection:
					DrawSelectedSection(currentEditedData.notLookingForNewPose);
					UpdateFromOtherSection();
					break;
				case SectionSelectedType.NeverLookingForNewPoseSection:
					DrawSelectedSection(currentEditedData.neverChecking);
					UpdateFromOtherSection();
					break;
				case SectionSelectedType.NormalSection:
					DrawSelectedSection(currentEditedData.sections[selectedSectionIndex]);
					//Updatinh frams sections mask:
					UpdateFromOtherSection();
					UpdateFrameSectionMask();
					break;
			}
		}

		private void DrawSelectedSection(DataSection section)
		{
			if (selectedSection != section)
			{
				selectedSection = section;
				sectionIntervalsRL = new ReorderableList(selectedSection.timeIntervals, typeof(float2), true, false, true, true);
			}

			HandleSectionIntervals(sectionIntervalsRL, currentEditedData);
			sectionIntervalsRL.DoLayoutList();

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Copy Section Settings", GUIResources.Button_MD()))
			{
				if (dataToCopyOptions != null)
				{
					switch (selectedSectionType)
					{
						case SectionSelectedType.NotLookingForNewPoseSection:
							currentEditedData.notLookingForNewPose.timeIntervals.Clear();
							for (int i = 0; i < dataToCopyOptions.notLookingForNewPose.timeIntervals.Count; i++)
							{
								currentEditedData.notLookingForNewPose.timeIntervals.Add(new float2(
									dataToCopyOptions.notLookingForNewPose.timeIntervals[i].x,
									dataToCopyOptions.notLookingForNewPose.timeIntervals[i].y
									));
							}
							break;
						case SectionSelectedType.NeverLookingForNewPoseSection:
							currentEditedData.neverChecking.timeIntervals.Clear();
							for (int i = 0; i < dataToCopyOptions.neverChecking.timeIntervals.Count; i++)
							{
								currentEditedData.neverChecking.timeIntervals.Add(new float2(
									dataToCopyOptions.neverChecking.timeIntervals[i].x,
									dataToCopyOptions.neverChecking.timeIntervals[i].y
									));
							}
							break;
						case SectionSelectedType.NormalSection:
							if (0 <= selectedSectionIndex && selectedSectionIndex < dataToCopyOptions.sections.Count)
							{
								currentEditedData.sections[selectedSectionIndex].timeIntervals.Clear();
								for (int i = 0; i < dataToCopyOptions.sections[selectedSectionIndex].timeIntervals.Count; i++)
								{
									currentEditedData.AddSectionInterval(
										selectedSectionIndex,
										i,
										dataToCopyOptions.sections[selectedSectionIndex].timeIntervals[i]
										);
								}
							}
							break;
					}
					UpdateFrameSectionMask();
				}
			}

			dataToCopyOptions = (MotionMatchingData)EditorGUILayout.ObjectField(dataToCopyOptions, typeof(MotionMatchingData), true);
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
		}

		private void HandleSectionIntervals(ReorderableList list, MotionMatchingData currentData)
		{
			list.headerHeight = 2f;

			list.elementHeight = 40f;

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				float numberWidth = 50f;
				float space = 10f;

				float VSpace = 10f;
				float elementHeight = rect.height - 2 * VSpace;
				Rect r1 = new Rect(rect.x, rect.y + VSpace, 50, elementHeight);
				Rect r2 = new Rect(r1.x + numberWidth + space, rect.y + VSpace, rect.width - 2 * (numberWidth + space), elementHeight);
				Rect r3 = new Rect(r2.x + r2.width + space, rect.y + VSpace, 50, elementHeight);

				float min = ((float2)list.list[index]).x;
				float max = ((float2)list.list[index]).y;

				min = EditorGUI.FloatField(r1, min);
				max = EditorGUI.FloatField(r3, max);
				EditorGUI.MinMaxSlider(r2, ref min, ref max, 0f, currentData.animationLength);

				if (index == list.index)
				{
					list.list[index] = new float2(min, max);
				}
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				list.list.Add(new float2(0.0f, currentData.animationLength));

				for (int frameIndex = 0; frameIndex < currentEditedData.frames.Count; frameIndex++)
				{
					FrameData f = currentEditedData.frames[frameIndex];

					f.sections.SetSection(selectedSectionIndex, selectedSection.Contain(f.localTime));

					currentEditedData.frames[frameIndex] = f;
				}

				list.index = list.count - 1;
			};


			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				if (list.index <= list.list.Count && list.index >= 0)
				{
					list.list.RemoveAt(list.index);
				}
			};


		}

		private void DrawContactsOptionsOnRightMenu()
		{
			if (contactPointsRL == null || isDataSwitched)
			{
				contactPointsRL = new ReorderableList(currentEditedData.contactPoints, typeof(MotionMatchingContact), true, false, true, true);
			}

			HandleContactPointsReorderbleList(contactPointsRL, currentEditedData, 2);
			contactPointsRL.DoLayoutList();

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Copy Contacts Settings", GUIResources.Button_MD()))
			{
				if (dataToCopyOptions != null)
				{
					currentEditedData.contactPoints.Clear();
					for (int i = 0; i < dataToCopyOptions.contactPoints.Count; i++)
					{
						currentEditedData.contactPoints.Add(dataToCopyOptions.contactPoints[i]);
					}
				}
			}

			dataToCopyOptions = (MotionMatchingData)EditorGUILayout.ObjectField(dataToCopyOptions, typeof(MotionMatchingData), true);
			GUILayout.EndHorizontal();

			GUILayout.Space(10);

		}

		private void HandleContactPointsReorderbleList(
			ReorderableList rList,
			MotionMatchingData currentData,
			int elementLines
			)
		{
			rList.onSelectCallback = (ReorderableList list) =>
			{

			};

			rList.onAddCallback = (ReorderableList list) =>
			{
				currentData.contactPoints.Add(new MotionMatchingContact(0f));
			};

			rList.onRemoveCallback = (ReorderableList list) =>
			{
				currentData.contactPoints.RemoveAt(list.index);
				list.index = list.count - 1;
			};

			rList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if (currentData.contactPoints.Count == 0 || (currentData.contactPoints.Count - 1) < index)
				{
					return;
				}
				index = Mathf.Clamp(index, 0, rList.count - 1);
				MotionMatchingContact cp = currentData.contactPoints[index];

				float H = 20f;
				float space = 5f;
				float numberL = 50f;
				Rect startRect = new Rect(rect.x, rect.y + space, numberL, H);
				Rect sliderRect = new Rect(rect.x + startRect.width + space, rect.y + space, rect.width - 2f * (space + numberL), H);
				Rect endRect = new Rect(sliderRect.x + sliderRect.width + space, rect.y + space, numberL, H);
				Rect posRect = new Rect(rect.x, sliderRect.y + H, 0.5f * rect.width, 2 * H);
				Rect normalRect = new Rect(posRect.x + posRect.width, sliderRect.y + H, 0.5f * rect.width, 2 * H);

				cp.endTime = Mathf.Clamp(cp.endTime, cp.startTime, currentData.animationLength);


				float startTime = EditorGUI.FloatField(startRect, cp.startTime);
				float endTime = EditorGUI.FloatField(endRect, cp.endTime);
				EditorGUI.MinMaxSlider(sliderRect, ref startTime, ref endTime, 0f, currentData.animationLength);
				Vector3 position = EditorGUI.Vector3Field(posRect, new GUIContent("Position"), cp.position);
				string normalName = /*currentData.contactsType == ContactStateType.Impacts ? "Impact rotation" :*/ "Contact rotation";
				Vector4 rotation = new Vector4(cp.rotation.x, cp.rotation.y, cp.rotation.z, cp.rotation.w);
				rotation = EditorGUI.Vector4Field(normalRect, new GUIContent(normalName), rotation);

				if (rList.index == index)
				{
					cp.startTime = startTime;
					cp.endTime = endTime;
					cp.position = position;
					//cp.contactNormal = normal;
				}


				currentData.contactPoints[index] = cp;
			};

			rList.elementHeightCallback = (int index) =>
			{
				return elementLines * 40f;
			};

			rList.headerHeight = 5f;

			rList.drawHeaderCallback = (Rect rect) =>
			{

			};
		}
		#endregion


		private void AnimationPlaying()
		{
			switch (animationState)
			{
				case AnimationState.NotCreated:
					break;
				case AnimationState.Stoped:
					StopedAnimationState();
					break;
				case AnimationState.Playing:
					PlayingAnimationState();
					break;
			}
		}

		private void PlayingAnimationState()
		{
			float deltaTime = Time.realtimeSinceStartup - bufforPlayingTime;

			currentAnimaionTime += deltaTime;

			bufforPlayingTime = Time.realtimeSinceStartup;

			if (currentAnimaionTime > currentEditedData.animationLength)
			{
				//gameObject.transform.position = Vector3.zero;
				//gameObject.transform.rotation = Quaternion.identity;
				OnPreviewBTN();
				OnPlayBTN();
				//currentAnimaionTime -= editedData.animationLength;
			}

		}

		private void StopedAnimationState()
		{

		}

		private void PausedAnimationState()
		{

		}

		private void OnCurrentAnimationTimeChange()
		{
			float deltaTime = currentAnimaionTime - bufforAnimationTime;

			bufforAnimationTime = currentAnimaionTime;
			if (previewBTNClick)
			{
				previewBTNClick = false;
			}
			else
			{
				if (playableGraph != null)
				{
					if (playableGraph.IsValid() && playableGraph.IsDataValid(currentEditedData))
					{

						float minDelta = 0.01667f;
						if (deltaTime > minDelta)
						{
							int deltas = Mathf.Abs(Mathf.CeilToInt(deltaTime / minDelta));
							float finalDelta = deltaTime / (float)deltas;

							for (int i = 0; i < deltas; i++)
							{

								playableGraph.EvaluateMotionMatchgData(currentEditedData, finalDelta);
								//playableGraph.Evaluate(finalDelta);
							}
						}
						else
						{
							playableGraph.EvaluateMotionMatchgData(currentEditedData, deltaTime);
							//playableGraph.Evaluate(deltaTime);
						}

						this.Repaint();
					}
				}
			}
		}

		#region Curves region

		private void DrawCurvesLeftMenu()
		{
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Add new Curve"))
				{
					Keyframe[] keys = new Keyframe[2];
					keys[0] = new Keyframe(0, 0);
					keys[1] = new Keyframe(currentEditedData.animationLength, 0);
					MotionMatchingDataCurve c = new MotionMatchingDataCurve();
					c.Curve = new AnimationCurve(keys);
					currentEditedData.Curves.Add(c);
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawCurvesRightMenu()
		{

			for (int i = 0; i < currentEditedData.Curves.Count; i++)
			{
				MotionMatchingDataCurve curve = currentEditedData.Curves[i];
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label($"{i + 1}.\t", GUILayout.MaxWidth(20));
						GUILayout.Label("Curve name:", GUILayout.Width(100));
						curve.Name = GUILayout.TextField(curve.Name);
						if (GUILayout.Button("X", GUILayout.Width(30)))
						{
							currentEditedData.Curves.RemoveAt(i);
							i--;
							continue;
						}
						GUILayout.Space(margin);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("Add key", GUILayout.Width(75)))
						{
							bool shouldAddNewKey = true;
							for (int keyIndex = 0; keyIndex < curve.Curve.keys.Length; keyIndex++)
							{
								if (curve.Curve.keys[keyIndex].time == currentAnimaionTime)
								{
									Keyframe k = curve.Curve.keys[keyIndex];
									k.value = curve.currentKeyValue_editorOnly;
									shouldAddNewKey = false;
									curve.Curve.MoveKey(i, k);
								}
							}
							if (shouldAddNewKey)
							{
								curve.Curve.AddKey(new Keyframe(currentAnimaionTime, curve.currentKeyValue_editorOnly));
							}
						}
						curve.currentKeyValue_editorOnly = EditorGUILayout.FloatField(curve.currentKeyValue_editorOnly, GUILayout.Width(50));

					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						curve.Curve = EditorGUILayout.CurveField(curve.Curve, GUILayout.Height(50));

						Rect curveControlRect = GUILayoutUtility.GetLastRect();

						Rect timeRect = new Rect(
							curveControlRect.x + curveControlRect.width * currentAnimaionTime / currentEditedData.animationLength,
							curveControlRect.y,
							curveTimeIndicatorWidth,
							curveControlRect.height
							);

						EditorGUI.DrawRect(timeRect, Color.white);



						GUILayout.Space(10);
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("Clear keys", GUILayout.Width(150)))
						{
							Keyframe[] keys = new Keyframe[2];
							keys[0] = new Keyframe(0, 0);
							keys[1] = new Keyframe(currentEditedData.animationLength, 0);
							curve.Curve = new AnimationCurve(keys);
						}

					}
					GUILayout.EndHorizontal();

				}
				GUILayout.EndVertical();
			}

			GUILayout.Space(20);
		}

		#endregion


		#region Scene GUI Drawing

		private void DrawSceneGUI(SceneView sceneView)
		{
			if (animatedObject != null && currentEditedData != null)
			{
				if (currentTool == MotionMatchingDataEditingTool.Contacts)
				{
					DrawSelectedContactPoint();
					DrawCalculatedContactPoints();
				}

				if (_bDrawTrajectory)
				{
					Handles.color = Color.cyan;
					Handles.DrawWireCube(animatedObject.transform.position, Vector3.one * 0.1f);
					Trajectory t = new Trajectory(currentEditedData.trajectoryPointsTimes.Count);
					currentEditedData.GetTrajectoryInTime(ref t, currentAnimaionTime);
					Handles.color = Color.green;
					t.TransformToWorldSpace(animatedObject.transform);
					MM_Gizmos.DrawTrajectory_Handles(
						currentEditedData.trajectoryPointsTimes.ToArray(),
						animatedObject.transform.position,
						animatedObject.transform.forward,
						t,
						0.04f,
						0.2f
						);
				}

				if (_bDrawPose)
				{
					PoseData p = new PoseData(currentEditedData[0].pose.Count);
					currentEditedData.GetPoseInTime(ref p, currentAnimaionTime);
					p.TransformToWorldSpace(animatedObject.transform);
					MM_Gizmos.DrawPose(p, Color.blue, Color.yellow);
				}

				if (currentTool == MotionMatchingDataEditingTool.Contacts)
				{
					float length = 66f;
					float height = 30f;
					Rect r = new Rect(
						sceneView.position.width / 2f - length / 2f,
						10f,
						length,
						height
						);
					DrawContactGizmosSelectRect(r);
				}
			}
		}

		float drawCubeSize = 0.05f;
		float arrowLength = 0.5f;
		float arrowArmLength = 0.2f;

		private void DrawSelectedContactPoint()
		{
			if (currentEditedData == null || animatedObject == null)
			{
				return;
			}
			if (contactPointsRL != null)
			{
				if (0 <= contactPointsRL.index && contactPointsRL.index < currentEditedData.contactPoints.Count)
				{
					Handles.color = Color.green;
					MotionMatchingContact cp = currentEditedData.contactPoints[contactPointsRL.index];
					Vector3 drawPosition = animatedObject.transform.TransformPoint(cp.position);
					Vector3 drawDirection = animatedObject.transform.TransformDirection(cp.contactNormal);

					cp.position = animatedObject.transform.TransformPoint(cp.position);
					// Changing contactPoint position
					if (drawPositionManipulator)
					{
						Vector3 cpPosBuffor = cp.position;
						Vector3 cpSurNorBuffor = cp.contactNormal;

						cp.position = Handles.PositionHandle(cp.position, Quaternion.identity);
					}

					// Changing contactPoint surface reverse normal
					if (drawRotationManipuator)
					{
						//Vector3 contactNormal = animatedObject.transform.TransformDirection(cp.contactNormal);
						//Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dirRSN.normalized);

						//rot = Handles.RotationHandle(rot, cp.position);
						if (cp.rotation.x == 0f &&
							cp.rotation.y == 0f &&
							cp.rotation.z == 0f &&
							cp.rotation.w == 0f)
						{
							cp.rotation = Quaternion.identity;
						}
						cp.rotation = Handles.RotationHandle(cp.rotation, cp.position);

						Vector3 contactNormal = cp.rotation * Vector3.forward;
						cp.contactNormal = contactNormal;

						cp.contactNormal = animatedObject.transform.InverseTransformDirection(cp.contactNormal);
					}

					Handles.DrawWireCube(drawPosition, Vector3.one * drawCubeSize);
					MM_Gizmos.DrawArrowHandles(drawPosition, drawDirection, arrowLength, arrowArmLength);

					cp.position = animatedObject.transform.InverseTransformPoint(cp.position);
					currentEditedData.contactPoints[contactPointsRL.index] = cp;
				}
			}
		}

		private void DrawCalculatedContactPoints()
		{
			switch (currentEditedData.contactsType)
			{
				case ContactStateType.NormalContacts:
					DrawSceneGUIContacts();
					break;
					//case ContactStateType.Impacts:
					//	DrawSceneGUIImpacts();
					//	break;
			}

		}

		private void DrawSceneGUIContacts()
		{
			List<FrameContact> cpList = new List<FrameContact>();
			currentEditedData.GetContactPoints(ref cpList, currentAnimaionTime);

			Handles.color = Color.red;

			for (int i = 0; i < cpList.Count; i++)
			{
				Vector3 cpPos = animatedObject.transform.TransformPoint(cpList[i].position);

				if (drawContactsPositions)
				{
					Handles.DrawWireCube(cpPos, Vector3.one * drawCubeSize);
				}
				if (drawContactsRSN)
				{
					Vector3 cpRSN = animatedObject.transform.TransformDirection(cpList[i].normal);

					MM_Gizmos.DrawArrowHandles(cpPos, cpRSN.normalized, arrowLength, arrowArmLength);
				}
			}
		}

		private void DrawSceneGUIImpacts()
		{
			FrameData frame = currentEditedData.GetClossestFrame(currentAnimaionTime);

			if (frame.contactPoints.Length != 1)
			{
				return;
			}
			Vector3 cpPos = animatedObject.transform.TransformPoint(frame.contactPoints[0].position);
			Vector3 cpRSN = animatedObject.transform.TransformDirection(frame.contactPoints[0].normal);
			if (drawContactsPositions)
			{
				Handles.DrawWireCube(cpPos, Vector3.one * drawCubeSize);
			}
			if (drawContactsRSN)
			{
				MM_Gizmos.DrawArrowHandles(cpPos, cpRSN.normalized, arrowLength, arrowArmLength);
			}

		}

		private void DrawContactGizmosSelectRect(Rect rect)
		{
			Handles.BeginGUI();
			{
				GUILayout.BeginArea(rect);
				{
					GUILayout.BeginHorizontal();
					float width = rect.width / 2f - 3;
					float height = rect.height;

					if (drawPositionManipulator)
					{
						drawPositionManipulator = GUILayout.Toggle(
							drawPositionManipulator,
							EditorGUIUtility.IconContent("MoveTool On"),
							new GUIStyle("Button"),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					else
					{
						drawPositionManipulator = GUILayout.Toggle(
							drawPositionManipulator,
							EditorGUIUtility.IconContent("MoveTool"),
							new GUIStyle("Button"),
							GUILayout.Width(30),
							GUILayout.Height(30)
							);
					}

					if (drawRotationManipuator)
					{
						drawRotationManipuator = GUILayout.Toggle(
							drawRotationManipuator,
							EditorGUIUtility.IconContent("RotateTool On"),
							new GUIStyle("Button"),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					else
					{
						drawRotationManipuator = GUILayout.Toggle(
							drawRotationManipuator,
							EditorGUIUtility.IconContent("RotateTool"),
							new GUIStyle("Button"),
							GUILayout.Width(30),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndArea();
			}
			Handles.EndGUI();
		}
		#endregion

		private void UpdateFrameSectionMask()
		{
			for (int sectionIndex = 0; sectionIndex < currentEditedData.sections.Count; sectionIndex++)
			{
				for (int i = 0; i < currentEditedData.frames.Count; i++)
				{
					FrameData editedFrame = currentEditedData.frames[i];

					editedFrame.sections.SetSection(
						sectionIndex,
						currentEditedData.sections[sectionIndex].Contain(editedFrame.localTime)
						);

					currentEditedData.frames[i] = editedFrame;
				}
			}
		}

		int fromSectionCopyIndex = 0;

		private void UpdateFromOtherSection()
		{
			List<string> sectionNames = new List<string>();
			foreach (DataSection s in currentEditedData.sections)
			{
				sectionNames.Add(s.sectionName);
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Copy from section", GUILayout.Width(120));
				fromSectionCopyIndex = EditorGUILayout.Popup(fromSectionCopyIndex, sectionNames.ToArray());
				if (GUILayout.Button("Copy Section"))
				{
					DataSection sourceSection = dataToCopyOptions.sections[fromSectionCopyIndex];
					selectedSection.timeIntervals.Clear();

					foreach (float2 interval in sourceSection.timeIntervals)
					{
						selectedSection.timeIntervals.Add(interval);
					}

					for (int frameIndex = 0; frameIndex < currentEditedData.frames.Count; frameIndex++)
					{
						FrameData f = currentEditedData.frames[frameIndex];

						f.sections.SetSection(selectedSectionIndex, selectedSection.Contain(f.localTime));

						currentEditedData.frames[frameIndex] = f;
					}
				}
			}
			GUILayout.EndHorizontal();
		}


		#region IK Tracks

		private void DrawIKTracksLeftMenu()
		{
			if (currentEditedData == null || animatedObject == null)
			{
				return;
			}

			GUILayout.BeginVertical();
			{

			}
			GUILayout.EndVertical();

			GUILayout.Space(10);

			GUILayout.BeginVertical();
			{
				if (GUILayout.Button("CalculateTracks"))
				{

				}
			}
			GUILayout.EndVertical();
		}

		private void DrawIKTracksRightMenu()
		{
			if (currentEditedData == null || animatedObject == null)
			{
				return;
			}

		}

		#endregion

		#region Animation events 
		private void DrawAniamtionEventsLeftMenu()
		{
			if (GUILayout.Button("AddEvent"))
			{
				currentEditedData.AddAnimationEvent(new MotionMatchingAnimationEvent("<AnimationEvent>", currentAnimaionTime));
			}

			if (GUILayout.Button("Sort events:"))
			{
				currentEditedData.AnimationEvents.Sort();
			}
		}

		private void OnDrawEventsRightMenu()
		{
			if (currentEditedData.AnimationEvents == null)
			{
				return;
			}

			float horizontalMargin = 10f;

			for (int i = 0; i < currentEditedData.AnimationEvents.Count; i++)
			{
				MotionMatchingAnimationEvent animationEvent = currentEditedData.AnimationEvents[i];

				bool isRemoved = false;
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label($"Event name", GUILayout.Width(80f));
						animationEvent.Name = EditorGUILayout.DelayedTextField(animationEvent.Name);

						if (GUILayout.Button("X", GUILayout.Width(25)))
						{
							isRemoved = true;
						}
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						GUILayout.Label($"Event time", GUILayout.Width(80f));
						animationEvent.EventTime = EditorGUILayout.Slider(animationEvent.EventTime,0, currentEditedData.animationLength);

						if (GUILayout.Button("Set current time", GUILayout.MaxWidth(100f)))
						{
							animationEvent.EventTime = currentAnimaionTime;
						}

						animationEvent.EventTime = Mathf.Clamp(animationEvent.EventTime, 0f, currentEditedData.animationLength);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();

				if (isRemoved)
				{
					currentEditedData.AnimationEvents.RemoveAt(i);
					i--;
				}
				else
				{
					GUILayout.Space(horizontalMargin);
				}
			}
		}
		#endregion
	}
}
