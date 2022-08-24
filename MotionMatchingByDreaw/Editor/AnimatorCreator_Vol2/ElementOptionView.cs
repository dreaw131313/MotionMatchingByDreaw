using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using MotionMatching.Gameplay;

namespace MotionMatching.Tools
{
	public class ElementOptionView
	{
		MM_AnimatorController animator;
		MotionMatchingLayer selectedLayer;
		MotionMatchingNode selectedNode;
		MotionMatchingState selectedState;
		MotionMatching.Gameplay.Transition selectedTransition;

		// Portal node stuff
		string portalFindingName = "";
		int selectedPortalStateIndex = -1;
		List<string> stateNames = new List<string>();

		ReorderableList transitionList;

		float descriptionWidth = 150f;
		//float margin = 10f;

		// Transition stuff
		ReorderableList transitionOptionsList;
		ReorderableList boolConditions;
		ReorderableList triggerConditions;
		ReorderableList intConditions;
		ReorderableList floatConditions;

		TransitionOptions selectedTransitionOption = null;

		// consts
		const float HORIZONTAL_MARGIN = 10;
		const float VERTICAL_MARGIN = 10;

		public void SetNeededReferences(
			MM_AnimatorController animator,
			MotionMatchingLayer layer,
			MotionMatchingNode node,
			 MotionMatching.Gameplay.Transition transition
			)
		{
			this.animator = animator;
			if (layer != null)
			{
				if (selectedLayer != layer)
				{
					selectedNode = null;
					selectedState = null;
					selectedTransition = null;
				}
				selectedLayer = layer;
				selectedNode = node;
				if (selectedNode != null && selectedNode.stateIndex >= 0 && selectedNode.stateIndex < selectedLayer.states.Count)
				{
					if (selectedState == null)
					{
						transitionList = new ReorderableList(
							selectedLayer.states[selectedNode.stateIndex].Transitions,
							typeof(MotionMatching.Gameplay.Transition),
							true,
							true,
							false,
							false
							);
					}
					else if (selectedState.Index != selectedNode.stateIndex)
					{
						transitionList = new ReorderableList(
							selectedLayer.states[selectedNode.stateIndex].Transitions,
							typeof(MotionMatching.Gameplay.Transition),
							true,
							true,
							false,
							false
							);
					}

					selectedState = selectedLayer.states[selectedNode.stateIndex];
				}
				else
				{
					selectedState = null;
				}
				if (selectedTransition == null && transition != null)
				{
					selectedTransition = transition;
					transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
				}
				else if (transition != selectedTransition && transition != null)
				{
					selectedTransition = transition;
					transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
				}
				else if (transition == null)
				{
					selectedTransition = null;
				}
			}
			else
			{
				selectedLayer = null;
				selectedNode = null;
				selectedState = null;
				selectedTransition = null;
			}
		}

		public void Draw()
		{
			if (selectedState != null && selectedNode.nodeType != MotionMatchingNodeType.Portal)
			{
				DrawStateOptions();
			}
			else if (selectedNode != null)
			{
				GUILayout.Space(5);
				GUILayout.Label("Portal node state selection", GUIResources.GetDarkHeaderStyle_MD());
				GUILayout.Space(5);
				DrawPortal();
			}
			else if (selectedTransition != null)
			{
				DrawTransitionOption();
			}
			else
			{
				GUILayout.Space(5);
				GUILayout.Label("Nothing is selected", GUIResources.GetDarkHeaderStyle_MD());
			}
		}

		#region State drawing
		private void DrawStateOptions()
		{
			DrawCommonFeatures();
			switch (selectedState.stateType)
			{
				case MotionMatchingStateType.MotionMatching:
					DrawMotionMatchingFeataures();
					break;
				case MotionMatchingStateType.SingleAnimation:
					DrawSingleAnimationFeatures();
					break;
				case MotionMatchingStateType.ContactAnimationState:
					DrawContactStateFeatures();
					break;
			}



			GUILayoutElements.DrawHeader(
						"Transition List",
						GUIResources.GetDarkHeaderStyle_MD()
						);

			if (transitionList == null)
			{
				transitionList = new ReorderableList(selectedState.Transitions, typeof(MotionMatching.Gameplay.Transition), true, true, false, false);
			}
			if (transitionList != null)
			{
				HandleTranistionList(transitionList, selectedState.Transitions);
				transitionList.DoLayoutList();
			}
		}

		private void DrawCommonFeatures()
		{
			GUILayoutElements.DrawHeader(
				selectedState.stateType.ToString(),
				GUIResources.GetDarkHeaderStyle_MD()
				);

			#region Common options
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField("Name", GUILayout.Width(descriptionWidth));

				string stateName = selectedState.Name;
				stateName = EditorGUILayout.TextField(selectedState.Name);

				stateName = selectedLayer.MakeStateNameUnique(stateName, selectedState.Index);

				selectedState.Name = stateName;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField("Tag", GUILayout.Width(descriptionWidth));
				selectedState.Tag = (MotionMatchingStateTag)EditorGUILayout.EnumPopup(selectedState.Tag);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.LabelField("Speed multiplayer", GUILayout.Width(descriptionWidth));
			selectedState.SpeedMultiplier = EditorGUILayout.FloatField(selectedState.SpeedMultiplier);
			GUILayout.EndHorizontal();


			if (selectedNode.nodeType == MotionMatchingNodeType.State)
			{
				if (selectedLayer.startStateIndex == selectedState.Index)
				{
					GUILayoutElements.DrawHeader(
						"Start state option",
						GUIResources.GetDarkHeaderStyle_MD()
						);

					NativeMotionGroup motionGroup = selectedState.MotionData;

					if (motionGroup != null)
					{
						List<string> clipNames = new List<string>(motionGroup.AnimationData.Count);
						for (int i = 0; i < motionGroup.AnimationData.Count; i++)
						{
							clipNames.Add(motionGroup.AnimationData[i].name);
						}

						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10);
							GUILayout.Label("Start clip index", GUILayout.Width(descriptionWidth));
							//selectedLayer.StartClipIndex = EditorGUILayout.IntSlider(
							//	selectedLayer.StartClipIndex,
							//	0,
							//	motionGroup.AnimationData.Count != 0 ? motionGroup.AnimationData.Count - 1 : 0
							//	);

							selectedLayer.StartClipIndex = EditorGUILayout.Popup(
								selectedLayer.StartClipIndex,
								clipNames.ToArray()
								);
						}
						GUILayout.EndHorizontal();

						if (motionGroup.AnimationData.Count > 0 && selectedLayer.StartClipIndex < motionGroup.AnimationData.Count &&
							motionGroup.AnimationData[selectedLayer.StartClipIndex] != null)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(10);
								GUILayout.Label("Start clip time", GUILayout.Width(descriptionWidth));
								selectedLayer.StartClipTime = EditorGUILayout.Slider(
									selectedLayer.StartClipTime,
									0f,
									selectedState.MotionData.AnimationData[selectedLayer.StartClipIndex].animationLength
									);
							}
							GUILayout.EndHorizontal();
						}
					}
					else
					{
						GUILayout.Space(10f);
					}
				}
			}

			DrawTrajectoryOptions();

			GUILayout.Space(5);

			GUILayoutElements.DrawHeader(
				   "Animation data",
				   // GUIResources.GetLightHeaderStyle_MD(),
				   GUIResources.GetDarkHeaderStyle_MD()//,
													   //ref selectedState.animDataFold
				   );

			if (/*selectedState.animDataFold*/ true)
			{
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				{
					selectedState.MotionData = (NativeMotionGroup)EditorGUILayout.ObjectField(
						selectedState.MotionData,
						typeof(NativeMotionGroup),
						false
						);
				}
				GUILayout.EndHorizontal();

				if (selectedState.MotionData != null)
				{
					DrawMotionGroupAnimationDataInfo(selectedState.MotionData);
				}
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Update Motion Groups", GUIResources.Button_MD()))
			{
				selectedState.UpadateMotionGroups();
			}
			GUILayout.Space(2);

			#endregion
		}

		private void DrawTrajectoryOptions()
		{
			if (selectedState.stateType != MotionMatchingStateType.ContactAnimationState)
			{
				GUILayoutElements.DrawHeader(
						"Trajectory correction",
						GUIResources.GetDarkHeaderStyle_MD()
						);

				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				selectedState.TrajectoryCorrection = EditorGUILayout.Toggle(new GUIContent("Trajectory correction"), selectedState.TrajectoryCorrection);
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Space(5);
			}
		}

		private void DrawMotionGroupAnimationDataInfo(NativeMotionGroup nativeGroup)
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
		}

		private void DrawMotionMatchingFeataures()
		{
			MotionMatchingStateFeatures features = selectedLayer.m_MotionMatchingStateFeatures[selectedState.StateFeaturesIndex];

			GUILayoutElements.DrawHeader(
						"Motion Matching state features:",
						GUIResources.GetDarkHeaderStyle_MD()
						);


			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				GUILayout.Label("Update interval", GUILayout.Width(descriptionWidth));
				features.updateInterval = EditorGUILayout.FloatField(features.updateInterval);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				GUILayout.Label("Blend time", GUILayout.Width(descriptionWidth));
				features.blendTime = EditorGUILayout.FloatField(features.blendTime);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				GUILayout.Label("Max clip delta time", GUILayout.Width(descriptionWidth));
				features.maxClipDeltaTime = EditorGUILayout.FloatField(features.maxClipDeltaTime);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				GUILayout.Label("Min weight to achive", GUILayout.Width(descriptionWidth));
				features.minWeightToAchive = EditorGUILayout.Slider(features.minWeightToAchive, 0.0f, 1.0f);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				GUILayout.Label("Max blended clips count", GUILayout.Width(descriptionWidth));
				features.maxBlendedClipCount = EditorGUILayout.IntSlider(features.maxBlendedClipCount, 2, 30);
			}
			GUILayout.EndHorizontal();
		}

		private void DrawSingleAnimationFeatures()
		{
			SingleAnimationStateFeatures features = selectedLayer.m_SingleAnimationStateFeatures[selectedState.StateFeaturesIndex];
			GUILayoutElements.DrawHeader(
						"Single Animation state features:",
						GUIResources.GetDarkHeaderStyle_MD()
						);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HORIZONTAL_MARGIN);
				//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
				features.AnimationFindingType = (SingleAnimationFindingType)EditorGUILayout.EnumPopup(new GUIContent("Finding type"), features.AnimationFindingType);
			}
			GUILayout.EndHorizontal();

			switch (features.AnimationFindingType)
			{
				case SingleAnimationFindingType.FindInAll:
					{

					}
					break;
				case SingleAnimationFindingType.FindInSpecificAnimation:
					{
						if (selectedState.MotionData != null)
						{
							NativeMotionGroup motionGroup = selectedState.MotionData;

							string[] animationList = new string[motionGroup.AnimationData.Count];
							for (int i = 0; i < motionGroup.AnimationData.Count; i++)
							{
								if (motionGroup.AnimationData[i] == null)
								{
									animationList[i] = "Null Animation data";
								}
								else
								{
									animationList[i] = motionGroup.AnimationData[i].name;
								}
							}

							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(2f * HORIZONTAL_MARGIN);
								features.AnimationIndexToFind = EditorGUILayout.Popup(
									new GUIContent("Animation to find"),
									features.AnimationIndexToFind,
									animationList
									);
							}
							GUILayout.EndHorizontal();
						}
					}
					break;
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HORIZONTAL_MARGIN);
				//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
				features.loop = EditorGUILayout.Toggle(new GUIContent("Loop"), features.loop);
			}
			GUILayout.EndHorizontal();

			if (!features.loop)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HORIZONTAL_MARGIN);
					//GUILayout.Label("Loop count before stop", GUILayout.Width(descriptionWidth));
					features.loopCountBeforeStop = Mathf.Clamp(
						EditorGUILayout.IntField(new GUIContent("Loops count before stop"), features.loopCountBeforeStop),
						1,
						int.MaxValue
						);
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HORIZONTAL_MARGIN);
				//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
				features.updateType = (SingleAnimationUpdateType)EditorGUILayout.EnumPopup(new GUIContent("Update type"), features.updateType);
			}
			GUILayout.EndHorizontal();

			if (features.updateType != SingleAnimationUpdateType.PlaySelected)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HORIZONTAL_MARGIN);
					//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
					features.blendTime = EditorGUILayout.FloatField(new GUIContent("Blend time"), features.blendTime);
				}
				GUILayout.EndHorizontal();
			}
			switch (features.updateType)
			{
				case SingleAnimationUpdateType.PlaySelected:
					break;
				case SingleAnimationUpdateType.PlayInSequence:
					break;
				case SingleAnimationUpdateType.PlayRandom:
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(10);
						features.CanBlendToTheSameAnimation = EditorGUILayout.Toggle(new GUIContent("Blend to current animation"), features.CanBlendToTheSameAnimation);
					}
					GUILayout.EndHorizontal();
					break;
			}


		}

		private void DrawContactStateFeatures()
		{
			GUILayoutElements.DrawHeader(
						"Contact state features:",
						GUIResources.GetDarkHeaderStyle_MD()
						);

			ContactStateFeatures csFeatures = selectedLayer.m_ContactStateFeatures[selectedState.StateFeaturesIndex];

			csFeatures.DrawEditorGUI(selectedState.MotionData);
		}

		private void DrawImpactStateFeatures(ContactStateFeatures features)
		{

		}

		private void DrawPortal()
		{
			GUILayout.BeginHorizontal();
			portalFindingName = EditorGUILayout.TextField("State name", portalFindingName);
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			if (stateNames == null)
			{
				stateNames = new List<string>();
			}
			stateNames.Clear();
			foreach (MotionMatchingState s in selectedLayer.states)
			{
				if (s.stateType == MotionMatchingStateType.ContactAnimationState)
				{
					continue;
				}
				bool goToNextState = false;
				foreach (MotionMatching.Gameplay.Transition t in s.Transitions)
				{
					if (t.nodeID == selectedNode.ID)
					{
						goToNextState = true;
						break;
					}
				}
				if (goToNextState) { continue; }

				if (portalFindingName != "")
				{
					if (s.Name.Contains(portalFindingName))
					{
						stateNames.Add(s.Name);
					}
				}
				else
				{
					stateNames.Add(s.Name);
				}
			}

			if (selectedNode.stateIndex >= 0 && selectedNode.stateIndex < selectedLayer.states.Count)
			{
				for (int i = 0; i < stateNames.Count; i++)
				{
					if (stateNames[i] == selectedLayer.states[selectedNode.stateIndex].Name)
					{
						selectedPortalStateIndex = i;
						break;
					}
				}
			}
			else
			{
				selectedPortalStateIndex = selectedNode.stateIndex;
			}

			selectedPortalStateIndex = GUILayout.SelectionGrid(
				selectedPortalStateIndex,
				stateNames.ToArray(),
				1
				);
			if (selectedPortalStateIndex >= 0 && selectedPortalStateIndex < stateNames.Count)
			{
				int newPortalStateIndex = selectedLayer.GetStateIndexEditorOnly(stateNames[selectedPortalStateIndex]);

				if (selectedNode.stateIndex != newPortalStateIndex)
				{
					selectedLayer.SetPortalState2(selectedNode.ID, newPortalStateIndex);
				}
			}

		}

		private void HandleTranistionList(ReorderableList list, List<MotionMatching.Gameplay.Transition> tList)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				GUI.Label(rect, "TransitionList");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string transitionString = selectedState.Name + "  ->  ";

				transitionString += selectedLayer.states[tList[index].nextStateIndex].Name;

				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, transitionString);
			};
		}

		#endregion


		#region Transition Drawing
		private void DrawTransitionOption()
		{
			if (selectedTransition == null)
			{
				return;
			}
			#region Common transition options
			string transitionDest = selectedLayer.GetStateName(selectedTransition.fromStateIndex) + " -> " + selectedLayer.GetStateName(selectedTransition.nextStateIndex);

			GUILayoutElements.DrawHeader(
						transitionDest,
						GUIResources.GetDarkHeaderStyle_MD()
						);
			GUILayout.Space(5);

			//if (transitionOptionsList == null)
			//{
			//    transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
			//}

			if (transitionOptionsList != null)
			{
				HandleTransitionOptionList(transitionOptionsList, selectedTransition.options);
				transitionOptionsList.DoLayoutList();
			}

			if (!(transitionOptionsList.index >= 0 && transitionOptionsList.index < selectedTransition.options.Count) && selectedTransition.options.Count > 0)
			{
				transitionOptionsList.index = 0;
			}

			#endregion

			if (transitionOptionsList.index >= 0 && transitionOptionsList.index < selectedTransition.options.Count)
			{
				TransitionOptions option = selectedTransition.options[transitionOptionsList.index];
				GUILayout.Space(5);
				GUILayoutElements.DrawHeader(
						"Common options",
						GUIResources.GetDarkHeaderStyle_MD()
						);
				GUILayout.Space(5);

				GUILayout.BeginHorizontal();
				option.BlendTime = EditorGUILayout.FloatField("Blend time", option.BlendTime);
				option.BlendTime = Mathf.Clamp(option.BlendTime, 0.00001f, float.MaxValue);
				GUILayout.EndHorizontal();

				// From state Option
				GUILayoutElements.DrawHeader(
						"From state options",
						GUIResources.GetDarkHeaderStyle_MD()
						);
				GUILayout.Space(5);

				switch (selectedLayer.GetStateType(selectedTransition.fromStateIndex))
				{
					case MotionMatchingStateType.MotionMatching:
						DrawTransitionFromMMState(option);
						break;
					case MotionMatchingStateType.SingleAnimation:
						DrawTransitionFromSAState(option, selectedLayer.states[selectedTransition.fromStateIndex]);
						break;
					case MotionMatchingStateType.ContactAnimationState:
						DrawTransitionFromSAState(option, selectedLayer.states[selectedTransition.fromStateIndex]);
						break;
				}
				// To state options
				GUILayoutElements.DrawHeader(
						"To state options",
						GUIResources.GetDarkHeaderStyle_MD()
						);
				GUILayout.Space(5);

				switch (selectedLayer.GetStateType(selectedTransition.nextStateIndex))
				{
					case MotionMatchingStateType.MotionMatching:
						DrawTransitionToMMState(option, selectedLayer.states[selectedTransition.nextStateIndex]);
						break;
					case MotionMatchingStateType.SingleAnimation:
						DrawTransitionToSAState(option, selectedLayer.states[selectedTransition.nextStateIndex]);
						break;
				}

				// Drawing Condition
				GUILayoutElements.DrawHeader(
						"Option Conditions",
						GUIResources.GetDarkHeaderStyle_MD()
						);
				GUILayout.Space(5);

				DrawCondition(option);
			}
		}

		private void HandleTransitionOptionList(ReorderableList list, List<TransitionOptions> toList)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, "Transition options (\"From section\" -> \"To section\")");
			};
			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				TransitionOptions option = toList[index];

				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.width = rect.width * 0.9f;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);

				MotionMatchingState fromState = selectedLayer.states[selectedTransition.fromStateIndex];
				MotionMatchingState toState = selectedLayer.states[selectedTransition.nextStateIndex];


				string fromSection = "";
				string toSection = "";

				if (fromState.MotionData == null)
				{
					fromSection = "Null motion data";
				}
				else if (fromState.MotionData.SectionsDependencies == null)
				{
					fromSection = "Null section dependecies";
				}
				else
				{
					int fromStateSectionIndex = Mathf.Clamp(
						option.WhenCanCheckingSection,
						0,
						fromState.MotionData.SectionsDependencies.SectionSettings.Count - 1
						);

					SectionSettings sectionSettings = fromState.MotionData.SectionsDependencies.SectionSettings[fromStateSectionIndex];
					fromSection = sectionSettings.name;
				}

				if (toState.MotionData == null)
				{
					toSection = "Null motion data";
				}
				else if (toState.MotionData.SectionsDependencies == null)
				{
					toSection = "Null section Dependecies";
				}
				else
				{
					int toSectionsMask = option.WhereCanFindingBestPoseSection;

					for (int i = 0; i < toState.MotionData.SectionsDependencies.SectionSettings.Count; i++)
					{
						int checkedSection = 1 << i;
						if ((toSectionsMask & checkedSection) == checkedSection)
						{
							if (toSection.Length == 0)
							{
								toSection += $"{toState.MotionData.SectionsDependencies.SectionSettings[i].name}";
							}
							else
							{
								toSection += $", {toState.MotionData.SectionsDependencies.SectionSettings[i].name}";
							}
						}
					}

				}

				GUI.Label(newRect, $"{fromSection}  ->  {toSection}");
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				string optionName = "New option";
				string newName = optionName;
				//int counter = 0;
				//for (int i = 0; i < toList.Count; i++)
				//{
				//	if (toList[i].GetName() == newName)
				//	{
				//		counter++;
				//		newName = optionName + counter.ToString();
				//		i = 0;
				//	}
				//}

				toList.Add(new TransitionOptions(newName));

				MotionMatchingState fromState = selectedLayer.states[selectedTransition.fromStateIndex];
				MotionMatchingState toState = selectedLayer.states[selectedTransition.nextStateIndex];

				//toList[toList.Count - 1].AddCheckingTransitionOption(fromState);
				//toList[toList.Count - 1].AddFindigBestPoseOption(toState);
			};
		}

		private void DrawTransitionFromMMState(TransitionOptions option)
		{

		}

		private void DrawWarningOnNullMotionGroupInState(MotionMatchingState state)
		{
			GUILayout.BeginHorizontal();
			{
				GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
				style.normal.textColor = Color.yellow;
				style.fontSize = 13;
				GUILayout.Space(5);
				GUILayout.Label(string.Format("Motion group in state \"{0}\" is null!", state.Name), style);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
		}

		private void DrawTransitionFromSAState(TransitionOptions option, MotionMatchingState from)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Transition after first section interval");
				GUILayout.Space(5f);
				option.TransitionAfterSectionStart = EditorGUILayout.Toggle(
					option.TransitionAfterSectionStart
					);
			}
			GUILayout.EndHorizontal();

			if (!option.TransitionAfterSectionStart)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Check transition on max lenght");
					option.StartOnExitTime = EditorGUILayout.Toggle(
						option.StartOnExitTime
						);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(5);
			}


			if (from.MotionData == null)
			{
				DrawWarningOnNullMotionGroupInState(from);
			}
			else if (from.MotionData.SectionsDependencies != null)
			{
				List<string> sectionNames = new List<string>();

				int index = 0;
				foreach (SectionSettings sectionSettings in from.MotionData.SectionsDependencies.SectionSettings)
				{
					sectionNames.Add(sectionSettings.name);

					index++;
				}

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("When Checking Section name", GUILayout.Width(200));
					option.WhenCanCheckingSection = EditorGUILayout.Popup(option.WhenCanCheckingSection, sectionNames.ToArray());
				}
				GUILayout.EndHorizontal();
				sectionNames = null;
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("When Checking Section name: Always");
					option.WhenCanCheckingSection = 0;
				}
				GUILayout.EndHorizontal();
			}

		}

		private void DrawTransitionToMMState(TransitionOptions option, MotionMatchingState to)
		{
			if (to.MotionData == null)
			{
				DrawWarningOnNullMotionGroupInState(to);
			}
			else if (to.MotionData.SectionsDependencies != null)
			{
				List<string> sectionNames = new List<string>();

				foreach (SectionSettings sectionSettings in to.MotionData.SectionsDependencies.SectionSettings)
				{
					sectionNames.Add(sectionSettings.name);
				}

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Where Finding Section name", GUILayout.Width(200));
					option.WhereCanFindingBestPoseSection = EditorGUILayout.MaskField(option.WhereCanFindingBestPoseSection, sectionNames.ToArray());

					if (option.WhereCanFindingBestPoseSection == 0)
					{
						option.WhereCanFindingBestPoseSection = 1;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Change section after first finding", GUILayout.Width(200));
					option.PerformFinding = EditorGUILayout.Toggle(
						new GUIContent("", "If true, after first finding section will be seted to section after finding."),
						option.PerformFinding,
						GUILayout.Width(20)
						);
				}
				GUILayout.EndHorizontal();

				if (option.PerformFinding)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Section after first finding", GUILayout.Width(200));
						option.SectionAfterFirstFinding = EditorGUILayout.MaskField(option.SectionAfterFirstFinding, sectionNames.ToArray());

						if (option.SectionAfterFirstFinding == 0)
						{
							option.SectionAfterFirstFinding = 1;
						}
					}
					GUILayout.EndHorizontal();
				}

				sectionNames = null;
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Where Finding Section name: Always");
					option.WhereCanFindingBestPoseSection = 0;
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawTransitionToSAState(TransitionOptions option, MotionMatchingState to)
		{
			if (to.MotionData == null)
			{
				DrawWarningOnNullMotionGroupInState(to);
			}
			else if (to.MotionData.SectionsDependencies != null)
			{
				List<string> sectionNames = new List<string>();

				foreach (SectionSettings sectionSettings in to.MotionData.SectionsDependencies.SectionSettings)
				{
					sectionNames.Add(sectionSettings.name);
				}

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Perform finding", GUILayout.Width(200));
					option.PerformFinding = EditorGUILayout.Toggle(
						new GUIContent("", "If false, findng pose will not be performd. First animation in motion group will be played from start of section \"Where Finding Section name\""),
						option.PerformFinding,
						GUILayout.Width(20)
						);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Where Finding Section name", GUILayout.Width(200));

					if (option.PerformFinding)
					{
						option.WhereCanFindingBestPoseSection = EditorGUILayout.MaskField(option.WhereCanFindingBestPoseSection, sectionNames.ToArray());

						if (option.WhereCanFindingBestPoseSection == 0)
						{
							option.WhereCanFindingBestPoseSection = 1;
						}
					}
					else
					{

						int index = 0;
						int checkedMask = option.WhereCanFindingBestPoseSection;

						while ((checkedMask & 1) != 1)
						{
							checkedMask = checkedMask >> 1;
							index += 1;
						}

						index = EditorGUILayout.Popup(index, sectionNames.ToArray());

						option.WhereCanFindingBestPoseSection = 1 << index;
					}
				}
				GUILayout.EndHorizontal();

				sectionNames = null;
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Where Finding Section name: Always");
					option.WhereCanFindingBestPoseSection = 0;
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawCondition(TransitionOptions option)
		{
			if (selectedTransitionOption != option)
			{
				selectedTransitionOption = option;
				boolConditions = new ReorderableList(option.boolConditions, typeof(ConditionBool));
				triggerConditions = new ReorderableList(option.TriggerConditions, typeof(ConditionTrigger));
				intConditions = new ReorderableList(option.intConditions, typeof(ConditionInt));
				floatConditions = new ReorderableList(option.floatConditions, typeof(ConditionFloat));
			}

			HandleBoolConditionList(boolConditions, option);
			HandleTriggerConditionList(triggerConditions, option);
			HandleIntConditionList(intConditions, option);
			HandleFloatConditionList(floatConditions, option);

			boolConditions.DoLayoutList();
			triggerConditions.DoLayoutList();
			intConditions.DoLayoutList();
			floatConditions.DoLayoutList();
		}

		private void HandleBoolConditionList(ReorderableList list, TransitionOptions option)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, "Bool conditions");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string[] bools = new string[animator.BoolParameters.Count];

				for (int i = 0; i < animator.BoolParameters.Count; i++)
				{
					bools[i] = animator.BoolParameters[i].Name;
				}

				Rect r1 = new Rect(
					rect.x + rect.width * 0.03f,
					rect.y + 0.1f * rect.height,
					rect.width * 0.45f,
					rect.height * 0.8f
					);
				Rect r2 = new Rect(
					r1.x + r1.width + rect.width * 0.04f,
					rect.y + 0.1f * rect.height,
					rect.width * 0.45f,
					rect.height * 0.8f
					);

				ConditionBool condition = option.boolConditions[index];

				if (animator.BoolParameters.Count > 0)
				{
					condition.CheckingValueIndex = EditorGUI.Popup(r1, condition.CheckingValueIndex, bools);
				}
				else
				{
					condition.CheckingValueIndex = -1;
					EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
				}
				//condition.boolConditions[index].checkingValueName = GUI.TextField(r1, condition.boolConditions[index].checkingValueName);
				condition.CheckType = (BoolConditionType)EditorGUI.EnumPopup(r2, condition.CheckType);

				option.boolConditions[index] = condition;
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				option.boolConditions.Add(new ConditionBool());
			};
		}

		private void HandleTriggerConditionList(ReorderableList list, TransitionOptions option)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				Rect newRect = rect;
				newRect.height = 0.9f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.05f * rect.height);
				GUI.Label(newRect, "Trigger conditions");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string[] triggers = animator.TriggersNames.ToArray();

				Rect r1 = new Rect(
					rect.x + rect.width * 0.05f,
					rect.y + 0.1f * rect.height,
					rect.width * 0.9f,
					rect.height * 0.8f
					);

				ConditionTrigger condition = option.TriggerConditions[index];

				if (animator.TriggersNames.Count > 0)
				{
					condition.CheckingValueIndex = EditorGUI.Popup(r1, condition.CheckingValueIndex, triggers);
				}
				else
				{
					condition.CheckingValueIndex = -1;
					EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
				}

				option.TriggerConditions[index] = condition;
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				option.TriggerConditions.Add(new ConditionTrigger());
			};
		}

		private void HandleIntConditionList(ReorderableList list, TransitionOptions option)
		{
			string[] ints = new string[animator.IntParamters.Count];

			for (int i = 0; i < animator.IntParamters.Count; i++)
			{
				ints[i] = animator.IntParamters[i].Name;
			}

			list.drawHeaderCallback = (Rect rect) =>
			{
				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, "Int conditions");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				float horizontalMargin = 5f;
				float toogleWidth = 20f;
				float baseWidth = (rect.width - 5 * horizontalMargin - toogleWidth) / 3f;
				float height = rect.height * 0.8f;
				float rectHeight = rect.y + 0.1f * rect.height;

				Rect r1 = new Rect(
					rect.x + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect r2 = new Rect(
					r1.x + r1.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect r3 = new Rect(
					r2.x + r2.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect switchToParameterRect = new Rect(
					r3.x + r3.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);


				ConditionInt condition = option.intConditions[index];

				if (animator.IntParamters.Count > 0)
				{
					condition.CheckingValueIndex = EditorGUI.Popup(r1, condition.CheckingValueIndex, ints);
				}
				else
				{
					condition.CheckingValueIndex = -1;
					EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
				}

				condition.CheckType = (ConditionType)EditorGUI.EnumPopup(r2, condition.CheckType);



				if (condition.UseOtherFloatAsConditionValue)
				{
					condition.ConditionValueIndex = EditorGUI.Popup(r3, condition.ConditionValueIndex, ints);
				}
				else
				{
					condition.ConditionValue = EditorGUI.IntField(r3, condition.ConditionValue);
				}

				condition.UseOtherFloatAsConditionValue = EditorGUI.Toggle(
					switchToParameterRect,
					condition.UseOtherFloatAsConditionValue
					);

				option.intConditions[index] = condition;
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				option.intConditions.Add(new ConditionInt());
			};
		}

		private void HandleFloatConditionList(ReorderableList list, TransitionOptions option)
		{
			string[] floatNames = new string[animator.FloatParamaters.Count];


			for (int i = 0; i < animator.FloatParamaters.Count; i++)
			{
				floatNames[i] = animator.FloatParamaters[i].Name;
			}


			list.drawHeaderCallback = (Rect rect) =>
			{
				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, "Float conditions");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				float horizontalMargin = 5f;
				float toogleWidth = 20f;
				float baseWidth = (rect.width - 5 * horizontalMargin - toogleWidth) / 3f;
				float height = rect.height * 0.8f;
				float rectHeight = rect.y + 0.1f * rect.height;

				Rect r1 = new Rect(
					rect.x + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect r2 = new Rect(
					r1.x + r1.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect r3 = new Rect(
					r2.x + r2.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);
				Rect switchToParameterRect = new Rect(
					r3.x + r3.width + horizontalMargin,
					rectHeight,
					baseWidth,
					height
					);

				ConditionFloat condition = option.floatConditions[index];

				if (animator.FloatParamaters.Count > 0)
				{
					condition.CheckingValueIndex = EditorGUI.Popup(
						r1,
						condition.CheckingValueIndex,
						floatNames
						);
				}
				else
				{
					condition.CheckingValueIndex = -1;
					EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
				}

				condition.CheckType = (ConditionType)EditorGUI.EnumPopup(
					r2,
					condition.CheckType
					);

				if (option.floatConditions[index].UseOtherFloatAsConditionValue)
				{
					condition.ConditionValueIndex = EditorGUI.Popup(
						r3,
						condition.ConditionValueIndex,
						floatNames
						);
				}
				else
				{
					condition.ConditionValue = EditorGUI.FloatField(
						r3,
						condition.ConditionValue
						);
				}

				condition.UseOtherFloatAsConditionValue = EditorGUI.Toggle(
					switchToParameterRect,
					condition.UseOtherFloatAsConditionValue
					);

				option.floatConditions[index] = condition;
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				option.floatConditions.Add(new ConditionFloat());
			};
		}
		#endregion

	}
}
