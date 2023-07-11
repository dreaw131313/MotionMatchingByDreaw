using MotionMatching.Gameplay;
using MotionMatching.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class AnimatorControllerEditorRightSpace : AnimatorEditorSpace
	{
		public MotionMatchingLayer_SO SelectedLayer
		{
			get
			{
				if (Animator == null) return null;
				return Animator.SelectedLayer;
			}
		}

		private State_SO selectedStateBuffor = null;
		public State_SO SelectedState
		{
			get => Animator.SelectedState;
		}


		private MotionMatching.Gameplay.Transition transitionBuffor;
		public MotionMatching.Gameplay.Transition SelectedTransition
		{
			get => Editor.graphMenu.SelectedTransition;
		}

		private PortalToState portalBuffor = null;
		public PortalToState SelectedPortal
		{
			get => Editor.graphMenu.SelectedPortal;
		}

		const float VerticalMargin = 5f;
		const float HorizontalMargin = 10f;
		Vector2 scroll;

		HeaderStyle headerStyle = new HeaderStyle(23f, new Color(120f / 255f, 120f / 255f, 120f / 255f));

		private void DrawHeader(string headerText)
		{
			GUILayout.Label(headerText, headerStyle.Style);
		}

		public override void PerfomrOnGUI(Event e)
		{
			Rect areaRect = MMGUIUtility.MakeMargins(Position, 7.5f, 3f, 0f, 0f);

			GUILayout.BeginArea(areaRect);
			{
				OnGUI(e);
			}
			GUILayout.EndArea();

		}

		public override void OnEnable()
		{

		}

		public override void OnChangeAnimatorAsset()
		{

		}

		protected override void OnGUI(Event e)
		{
			if (Animator == null) return;

			OnChangeSelectionCallbacks();

			if (SelectedState != null)
			{
				DrawStateOptions();
			}
			else if (SelectedTransition != null)
			{
				DrawTransition();
			}
			else if (SelectedPortal != null)
			{
				PortalOnGUI(e);
			}
			else
			{
				DrawNothingSelected();
			}

		}


		protected void OnChangeSelectionCallbacks()
		{
			OnChangeSelectedState();

			OnTransitionChange();
		}

		#region State gui:
		ReorderableList transitionList;

		private void OnChangeSelectedState()
		{
			if (selectedStateBuffor == SelectedState)
			{
				return;
			}

			GUI.FocusControl(null);

			selectedStateBuffor = SelectedState;
			if (SelectedState == null) return;


			if (SelectedState.Transitions == null) SelectedState.Transitions = new List<Gameplay.Transition>();
			transitionList = new ReorderableList(SelectedState.Transitions, typeof(MotionMatching.Gameplay.Transition), true, false, false, false);
		}

		private void DrawStateOptions()
		{
			GUILayout.Space(VerticalMargin);
			DrawHeader(SelectedState.StateType.ToString());

			scroll = GUILayout.BeginScrollView(scroll);
			{
				DrawCommonFeatures();
				switch (SelectedState.StateType)
				{
					case MotionMatchingStateType.MotionMatching:
						{
							MotionMatchingState_SO s = SelectedState as MotionMatchingState_SO;

							if (s.Features == null) s.Features = new MotionMatchingStateFeatures();

							DrawMotionMatchingFeataures(s.Features);
						}
						break;
					case MotionMatchingStateType.SingleAnimation:
						{
							SingleAnimationState_SO s = SelectedState as SingleAnimationState_SO;

							if (s.Features == null) s.Features = new SingleAnimationStateFeatures();

							DrawSingleAnimationFeatures(s.Features);
						}
						break;
					case MotionMatchingStateType.ContactAnimationState:
						{
							ContactState_SO s = SelectedState as ContactState_SO;

							if (s.Features == null) s.Features = new ContactStateFeatures();

							DrawContactStateFeatures(s.Features);
						}
						break;
				}

				GUILayout.BeginVertical();
				{
					DrawHeader("Transition List");
					GUILayout.Space(VerticalMargin);
					HandleTranistionList(transitionList, SelectedState.Transitions);
					transitionList.DoLayoutList();
				}
				GUILayout.EndVertical();

			}
			GUILayout.EndScrollView();
		}

		private void DrawCommonFeatures()
		{
			#region Common options
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HorizontalMargin);
				EditorGUILayout.LabelField("Name", GUILayout.Width(descriptionWidth));

				string stateName = EditorGUILayout.TextField(SelectedState.Name);

				if (stateName != SelectedState.Name)
				{
					stateName = SelectedLayer.MakeStateNameUniqueForState(SelectedState, stateName);
					SelectedState.Name = stateName;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HorizontalMargin);
				EditorGUILayout.LabelField("Tag", GUILayout.Width(descriptionWidth));
				SelectedState.Tag = (MotionMatchingStateTag)EditorGUILayout.EnumPopup(SelectedState.Tag);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(VerticalMargin);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HorizontalMargin);
				EditorGUILayout.LabelField("Speed multiplayer", GUILayout.Width(descriptionWidth));
				SelectedState.SpeedMultiplier = EditorGUILayout.FloatField(SelectedState.SpeedMultiplier);
			}
			GUILayout.EndHorizontal();


			if (SelectedLayer.GetStartState() == SelectedState)
			{
				DrawHeader("Start state option");

				NativeMotionGroup motionGroup = SelectedState.MotionData;

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

						SelectedLayer.StartStateData.StartClipIndex = EditorGUILayout.Popup(
							SelectedLayer.StartStateData.StartClipIndex,
							clipNames.ToArray()
							);
					}
					GUILayout.EndHorizontal();

					if (motionGroup.AnimationData.Count > 0 && SelectedLayer.StartStateData.StartClipIndex < motionGroup.AnimationData.Count &&
						motionGroup.AnimationData[SelectedLayer.StartStateData.StartClipIndex] != null)
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10);
							GUILayout.Label("Start clip time", GUILayout.Width(descriptionWidth));
							SelectedLayer.StartStateData.StartClipTime = EditorGUILayout.Slider(
								SelectedLayer.StartStateData.StartClipTime,
								0f,
								SelectedState.MotionData.AnimationData[SelectedLayer.StartStateData.StartClipIndex].animationLength
								);
						}
						GUILayout.EndHorizontal();
					}
				}
				else
				{
					GUILayout.Label("Motion data is null!");
				}
			}

			DrawTrajectoryOptions();

			GUILayout.Space(VerticalMargin);


			DrawHeader("Animation data");
			GUILayout.Space(VerticalMargin);
			GUILayout.BeginHorizontal();
			{
				SelectedState.MotionData = (NativeMotionGroup)EditorGUILayout.ObjectField(
					SelectedState.MotionData,
					typeof(NativeMotionGroup),
					false
					);
			}
			GUILayout.EndHorizontal();

			if (SelectedState.MotionData != null)
			{
				DrawMotionGroupAnimationDataInfo(SelectedState.MotionData);
			}

			GUILayout.Space(VerticalMargin);

			if (GUILayout.Button("Update Motion Groups", GUIResources.Button_MD()))
			{
				SelectedState.UpadateMotionGroups();
			}
			GUILayout.Space(VerticalMargin);

			#endregion
		}

		private void DrawTrajectoryOptions()
		{
			if (SelectedState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				DrawHeader("Trajectory correction");

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HorizontalMargin);
					SelectedState.TrajectoryCorrection = EditorGUILayout.Toggle(new GUIContent("Trajectory correction"), SelectedState.TrajectoryCorrection);
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Space(VerticalMargin);
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
				GUILayout.Space(HorizontalMargin);
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

		private void DrawMotionMatchingFeataures(MotionMatchingStateFeatures features)
		{
			DrawHeader("Motion Matching state features:");


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

		private void DrawSingleAnimationFeatures(SingleAnimationStateFeatures features)
		{
			DrawHeader("Single Animation state features:");

			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(HorizontalMargin);
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
						if (SelectedState.MotionData != null)
						{
							NativeMotionGroup motionGroup = SelectedState.MotionData;

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
								GUILayout.Space(2f * HorizontalMargin);
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
				GUILayout.Space(HorizontalMargin);
				//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
				features.loop = EditorGUILayout.Toggle(new GUIContent("Loop"), features.loop);
			}
			GUILayout.EndHorizontal();

			if (!features.loop)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HorizontalMargin);
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
				GUILayout.Space(HorizontalMargin);
				//GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
				features.updateType = (SingleAnimationUpdateType)EditorGUILayout.EnumPopup(new GUIContent("Update type"), features.updateType);
			}
			GUILayout.EndHorizontal();

			if (features.updateType != SingleAnimationUpdateType.PlaySelected)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HorizontalMargin);
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

		private void DrawContactStateFeatures(ContactStateFeatures csFeatures)
		{
			DrawHeader("Contact state features:");

			csFeatures.DrawEditorGUI(SelectedState.MotionData);
		}

		private void DrawImpactStateFeatures(ContactStateFeatures features)
		{

		}

		private void HandleTranistionList(ReorderableList list, List<MotionMatching.Gameplay.Transition> tList)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				GUI.Label(rect, "TransitionList");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string transitionString = SelectedState.Name + "  ->  ";

				transitionString += SelectedLayer.States[tList[index].nextStateIndex].Name;

				Rect newRect = rect;
				newRect.height = 0.8f * rect.height;
				newRect.x += (0.05f * rect.width);
				newRect.y += (0.1f * rect.height);
				GUI.Label(newRect, transitionString);
			};
		}


		#endregion

		#region Transition gui:
		float descriptionWidth = 150f;

		//float margin = 10f;

		// Transition stuff

		ReorderableList transitionOptionsList;
		ReorderableList boolConditions;
		ReorderableList triggerConditions;
		ReorderableList intConditions;
		ReorderableList floatConditions;

		TransitionOptions selectedOptionBuffor = null;
		TransitionOptions SelectedOption = null;



		private void OnTransitionChange()
		{
			if (transitionBuffor == SelectedTransition) return;

			transitionBuffor = SelectedTransition;
			SelectedOption = null;

			GUI.FocusControl(null);

			if (SelectedTransition == null) return;

			if (SelectedTransition.options == null) SelectedTransition.options = new List<TransitionOptions>();
			transitionOptionsList = new ReorderableList(SelectedTransition.options, typeof(TransitionOptions), true, false, true, true);

			if (SelectedTransition.options.Count > 0)
			{
				SelectedOption = SelectedTransition.options[0];
			}
		}

		private void OnTransitionOptionChange()
		{
			if (selectedOptionBuffor == SelectedOption) return;

			GUI.FocusControl(null);

			selectedOptionBuffor = SelectedOption;

			if (SelectedOption == null) return;


			if (SelectedOption.boolConditions == null) SelectedOption.boolConditions = new List<ConditionBool>();
			boolConditions = new ReorderableList(SelectedOption.boolConditions, typeof(ConditionBool), true, true, true, true);

			if (SelectedOption.TriggerConditions == null) SelectedOption.TriggerConditions = new List<ConditionTrigger>();
			triggerConditions = new ReorderableList(SelectedOption.TriggerConditions, typeof(ConditionTrigger), true, true, true, true);

			if (SelectedOption.intConditions == null) SelectedOption.intConditions = new List<ConditionInt>();
			intConditions = new ReorderableList(SelectedOption.intConditions, typeof(ConditionInt), true, false, true, true);

			if (SelectedOption.floatConditions == null) SelectedOption.floatConditions = new List<ConditionFloat>();
			floatConditions = new ReorderableList(SelectedOption.floatConditions, typeof(ConditionFloat), true, false, true, true);
		}

		private void DrawTransition()
		{
			GUILayout.Space(VerticalMargin);

			string transitionDest = SelectedTransition.FromState.Name + " -> " + SelectedTransition.ToState.Name;

			DrawHeader(transitionDest);
			GUILayout.Space(VerticalMargin);

			//if (transitionOptionsList == null)
			//{
			//    transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
			//}

			HandleTransitionOptionList(transitionOptionsList, SelectedTransition.options);
			transitionOptionsList.DoLayoutList();

			int newSelectedOption = Mathf.Clamp(transitionOptionsList.index, 0, SelectedTransition.options.Count);
			if (newSelectedOption < SelectedTransition.options.Count)
			{
				SelectedOption = SelectedTransition.options[newSelectedOption];
			}

			OnTransitionOptionChange();

			if (SelectedOption != null)
			{
				GUILayout.Space(VerticalMargin);
				DrawHeader("Common options");
				GUILayout.Space(VerticalMargin);

				GUILayout.BeginHorizontal();
				{
					SelectedOption.BlendTime = EditorGUILayout.FloatField("Blend time", SelectedOption.BlendTime);
					SelectedOption.BlendTime = Mathf.Clamp(SelectedOption.BlendTime, 0.00001f, float.MaxValue);
				}
				GUILayout.EndHorizontal();

				// From state Option
				DrawHeader("From state options");
				GUILayout.Space(VerticalMargin);

				switch (SelectedTransition.FromState.StateType)
				{
					case MotionMatchingStateType.MotionMatching:
						{
							DrawTransitionFromMMState(SelectedOption);
						}
						break;
					case MotionMatchingStateType.SingleAnimation:
					case MotionMatchingStateType.ContactAnimationState:
						{
							DrawTransitionFromSAState(SelectedOption, SelectedTransition.FromState);
						}
						break;
				}
				// To state options
				DrawHeader("To state options");
				GUILayout.Space(VerticalMargin);

				switch (SelectedTransition.ToState.StateType)
				{
					case MotionMatchingStateType.MotionMatching:
						{
							DrawTransitionToMMState(SelectedOption, SelectedTransition.ToState);
						}
						break;
					case MotionMatchingStateType.SingleAnimation:
						{
							DrawTransitionToSAState(SelectedOption, SelectedTransition.ToState);
						}
						break;
				}

				// Drawing Condition
				DrawHeader("Option Conditions");
				GUILayout.Space(VerticalMargin);

				DrawCondition(SelectedOption);
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

				State_SO fromState = SelectedTransition.FromState;
				State_SO toState = SelectedTransition.ToState;


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

				//toList[toList.Count - 1].AddCheckingTransitionOption(fromState);
				//toList[toList.Count - 1].AddFindigBestPoseOption(toState);
			};
		}

		private void DrawTransitionFromMMState(TransitionOptions option)
		{

		}

		private void DrawWarningOnNullMotionGroupInState(State_SO state)
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

		private void DrawTransitionFromSAState(TransitionOptions option, State_SO from)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Transition after first section interval");
				GUILayout.Space(HorizontalMargin);
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
				GUILayout.Space(VerticalMargin);
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

		private void DrawTransitionToMMState(TransitionOptions option, State_SO to)
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

		private void DrawTransitionToSAState(TransitionOptions option, State_SO to)
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
				string[] bools = new string[Animator.BoolParameters.Count];

				for (int i = 0; i < Animator.BoolParameters.Count; i++)
				{
					bools[i] = Animator.BoolParameters[i].Name;
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

				if (Animator.BoolParameters.Count > 0)
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
				string[] triggers = Animator.TriggersNames.ToArray();

				Rect r1 = new Rect(
					rect.x + rect.width * 0.05f,
					rect.y + 0.1f * rect.height,
					rect.width * 0.9f,
					rect.height * 0.8f
					);

				ConditionTrigger condition = option.TriggerConditions[index];

				if (Animator.TriggersNames.Count > 0)
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
			string[] ints = new string[Animator.IntParameters.Count];

			for (int i = 0; i < Animator.IntParameters.Count; i++)
			{
				ints[i] = Animator.IntParameters[i].Name;
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

				if (Animator.IntParameters.Count > 0)
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
			string[] floatNames = new string[Animator.FloatParamaters.Count];


			for (int i = 0; i < Animator.FloatParamaters.Count; i++)
			{
				floatNames[i] = Animator.FloatParamaters[i].Name;
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

				if (Animator.FloatParamaters.Count > 0)
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

		#region Portal gui:
		SearchField m_SerachStateForPortalState;
		SearchField PortalSearchField
		{
			get
			{
				if (m_SerachStateForPortalState == null)
				{
					m_SerachStateForPortalState = new SearchField();
				}
				return m_SerachStateForPortalState;
			}
		}

		string searchPortalStateValue = "";

		OnePixelColorTexture SelectedStateForPortalTexture = new OnePixelColorTexture(0, 132, 255, 80);

		private void OnSelectedPortalChange()
		{
			if (portalBuffor == SelectedPortal) return;

			GUI.FocusControl(null);

			portalBuffor = SelectedPortal;

			if (SelectedPortal == null) return;

		}

		private void PortalOnGUI(Event e)
		{

			GUILayout.BeginVertical();
			{
				GUILayout.Space(VerticalMargin);
				string header = "Portal: \"{0}\"";

				if (SelectedPortal.State != null)
				{
					DrawHeader(string.Format(header, SelectedPortal.State.Name));
				}
				else
				{
					DrawHeader(string.Format(header, "Empty"));
				}

				GUILayout.Space(VerticalMargin);

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Search state:", GUILayout.Width(80f));
					searchPortalStateValue = PortalSearchField.OnToolbarGUI(searchPortalStateValue);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(VerticalMargin / 2f);

				DrawStatesToSelect();
			}
			GUILayout.EndVertical();
		}

		private void DrawStatesToSelect()
		{
			if (SelectedLayer.States == null || SelectedLayer.States.Count == 0) return;

			scroll = GUILayout.BeginScrollView(scroll);
			{
				foreach (var state in SelectedLayer.States)
				{
					bool nameFindResult = true;

					if (searchPortalStateValue.Length > 0)
					{
						nameFindResult = state.Name.ToLower().Contains(searchPortalStateValue.ToLower());
					}

					if (state.StateType != MotionMatchingStateType.ContactAnimationState && nameFindResult)
					{
						DrawPortalSelectionState(state);
					}
				}
			}
			GUILayout.EndScrollView();
		}

		private void DrawPortalSelectionState(State_SO state)
		{

			bool isStateChanged = false;
			if (GUILayout.Button(state.Name))
			{
				isStateChanged = true;
			}

			if (isStateChanged)
			{
				State_SO oldState = SelectedPortal.State;
				SelectedPortal.State = state;
				OnChangePortalState(SelectedPortal, oldState, SelectedPortal.State);
			}

			Rect lastRect = GUILayoutUtility.GetLastRect();

			if (SelectedPortal.State == state)
			{
				GUI.DrawTexture(lastRect, SelectedStateForPortalTexture.Texture);
			}
		}

		private void OnChangePortalState(PortalToState portal, State_SO oldState, State_SO newState)
		{
		}

		#endregion

		#region Nothing selected

		private void DrawNothingSelected()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.Space(VerticalMargin);
				DrawHeader("Nothing selected");
			}
			GUILayout.EndVertical();
		}
		#endregion
	}

	public class HeaderStyle
	{
		GUIStyle style;

		public GUIStyle Style
		{
			get
			{
				if (style == null || backgroundTexture.IsTextureNull)
				{
					style = new GUIStyle();
					style.fixedHeight = height;
					style.normal.background = backgroundTexture.Texture;
					style.fontSize = Mathf.RoundToInt(height * 0.6f);
					style.contentOffset = new Vector2(10, (height - style.fontSize * 1.333f) / 2f);

				}
				return style;
			}
		}

		OnePixelColorTexture backgroundTexture;

		float height;

		public HeaderStyle(float height, Color color)
		{
			this.height = height;
			backgroundTexture = new OnePixelColorTexture(color);
		}

	}
}