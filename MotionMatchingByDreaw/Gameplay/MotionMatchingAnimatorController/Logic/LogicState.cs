using MotionMatching.Gameplay.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public abstract class LogicState
	{
		protected Transform transform;
		protected MotionMatchingComponent motionMatching;
		public State_SO m_DataState;
		public LogicMotionMatchingLayer m_LogicLayer;
		public MotionMatchingLayer_SO DataLayer
		{
			get
			{
				return m_LogicLayer.DataLayer;
			}
		}

		protected PlayableAnimationSystem animationSystem;

		public NativeMotionGroup CurrentMotionGroup;
		public SectionsDependencies CurrentSectionDependecies;

		public int m_CurrentClipIndex;
		public MotionMatchingDataInfo CurrentClipInfo
		{
			get { return CurrentMotionGroup.MotionDataInfos[m_CurrentClipIndex]; }
		}
		public float m_CurrentClipLocalTime;
		public float m_CurrentClipGlobalTime;
		public bool m_IsBlockedToEnter;
		public bool m_IsPlayingCurrentStateAnimation;

		// Changing section
		public int m_CurrentSectionMask;
		protected bool isSectionMaskChanged = false;
		int sectionMaskBuffor;

		public int Index { get => m_DataState.Index; }

		public MotionMatchingStateType StateType { get { return m_DataState.StateType; } }

		protected PlayableAnimationLayerData AnimationLayer { get => animationSystem.layers[m_LogicLayer.Index]; private set { } }


		protected List<MotionMatchingStateBehavior> Behaviors;

		// Animation evnets data:
		private bool shouldFireAnimationEvents;
		private int currentAnimationEventIndex;
		private float animationEventTime;

		MotionMatchingAnimationEvent currentAnimationEvent;


		public PlayableStateMixer StateMixer;
		public List<BlendedAnimationData> CurrenBlendingAnimationData;

		protected LogicState(
			State_SO state,
			MotionMatchingComponent component,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			)
		{
			this.m_DataState = state;
			this.m_LogicLayer = logicLayer;
			this.motionMatching = component;
			this.transform = component.transform;
			this.animationSystem = animationSystem;

			Behaviors = new List<MotionMatchingStateBehavior>();

			if (!m_DataState.IsRuntimeValid())
			{
				m_IsBlockedToEnter = true;
			}
			else
			{
				shouldFireAnimationEvents = false;
				currentAnimationEventIndex = -1;

				CurrentMotionGroup = m_DataState.MotionData;
				CurrentSectionDependecies = CurrentMotionGroup.SectionsDependencies;
			}

			StateMixer = new PlayableStateMixer(animationSystem.Graph);
			StateMixer.OnRemoveClipPlayable = RemoveBlendedAnimationData;
			StateMixer.OnRemovingAllPlayables = RemoveAllBlendedAnimationData;

			CurrenBlendingAnimationData = new List<BlendedAnimationData>();

#if UNITY_EDITOR
			CheckSectionDependenciesCOuntInData();
#endif

		}

		public virtual void Awake()
		{

		}

		public virtual void Start()
		{
			m_CurrentClipIndex = m_LogicLayer.DataLayer.StartStateData.StartClipIndex;
			m_CurrentClipGlobalTime = m_LogicLayer.DataLayer.StartStateData.StartClipTime;
			m_CurrentClipLocalTime = m_LogicLayer.DataLayer.StartStateData.StartClipTime;
			m_CurrentSectionMask = 1;
			m_IsBlockedToEnter = true;

			m_IsPlayingCurrentStateAnimation = false;

			PlayAnimation(DataLayer.StartStateData.StartClipIndex, DataLayer.StartStateData.StartClipTime, 0f, 0f);

			if (motionMatching.PerformStateBehaviorsCallbacks)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].Enter();
				}
			}

			if (m_DataState.SpeedMultiplier != StateMixer.GetSpeedMultiplier())
			{
				StateMixer.SetSpeed(m_DataState.SpeedMultiplier);
			}
		}

		public void OnPreEnter()
		{
			CurrenBlendingAnimationData.Clear();
			m_IsBlockedToEnter = true;
			m_IsPlayingCurrentStateAnimation = false;
		}

		public virtual void Enter(SwitchStateInfo switchStateInfo)
		{
			m_CurrentSectionMask = switchStateInfo.SectionMask;

			if (motionMatching.PerformStateBehaviorsCallbacks)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].Enter();
				}
			}
		}

		public virtual void Exit()
		{
			if (motionMatching.PerformStateBehaviorsCallbacks)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].Exit();
				}
			}
			m_IsBlockedToEnter = false;
		}

		public virtual void FixedUpdate()
		{
			if (motionMatching.PerformStateBehaviorsCallbacks &&
				!m_LogicLayer.WillSwitchState &&
				m_IsPlayingCurrentStateAnimation)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].FixedUpdate();
				}
			}
		}

		public virtual void Update()
		{
			if (motionMatching.PerformStateBehaviorsCallbacks &&
				!m_LogicLayer.WillSwitchState &&
				m_IsPlayingCurrentStateAnimation)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].Update();
				}
			}
		}

		public void PerformAnimationTimeScaling()
		{
			if (CurrentClipInfo != null && CurrentClipInfo.UseAnimationSpeedCurve)
			{
				float evaluatedValue = CurrentClipInfo.AnimationSpeedCurve.Evaluate(m_CurrentClipLocalTime);


				StateMixer.SetSpeed(evaluatedValue);
			}
		}

		public virtual void LateUpdate()
		{
			if (motionMatching.PerformStateBehaviorsCallbacks &&
				!m_LogicLayer.WillSwitchState &&
				m_IsPlayingCurrentStateAnimation)
			{
				FireAnimationEvent();
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].LateUpdate();
				}
			}
			TrySetFloatParametersFromCurve();
		}

		public virtual void OnDestroy()
		{

		}

		public virtual void OnReEnter(SwitchStateInfo info)
		{

		}

		private void TrySetFloatParametersFromCurve()
		{
			if (CurrentClipInfo.Curves != null)
			{
				for (int i = 0; i < CurrentClipInfo.Curves.Count; i++)
				{
					motionMatching.SetFloat(CurrentClipInfo.Curves[i].Name, CurrentClipInfo.Curves[i].Curve.Evaluate(m_CurrentClipLocalTime));
				}
			}
		}

		internal abstract void CompleteScheduledJobs();

		#region Setters

		public void SetCurrentSectionIndex(string name)
		{
			int value;
			if (CurrentMotionGroup.SectionIndexes.TryGetValue(name, out value))
			{
				isSectionMaskChanged = true;
				sectionMaskBuffor = 1 << CurrentMotionGroup.SectionIndexes[name];

				//#if UNITY_EDITOR
				//				Debug.Log($"In state {dataState.GetName()} seted section of name {name}.");
				//#endif
				return;
			}
#if UNITY_EDITOR
			Debug.LogWarning($"State \"{m_DataState.Name}\" has no section of name \"{name}\"");
#endif
		}

		public void SetCurrentSectionIndex(int sectionIndex)
		{
			isSectionMaskChanged = true;
			sectionMaskBuffor = 1 << sectionIndex;
		}

		public void SetCurrentDefaultSectionMask()
		{
			isSectionMaskChanged = true;
			sectionMaskBuffor = 1;
		}

		public void SetCurrentSectionMask(int sectionMask)
		{
			isSectionMaskChanged = true;
			sectionMaskBuffor = sectionMask;
		}

		protected void UpdateSectionMask()
		{
			if (isSectionMaskChanged)
			{
				isSectionMaskChanged = false;
				m_CurrentSectionMask = sectionMaskBuffor;
			}
		}

		#endregion

		#region getters

		public bool IsInSection(string sectionName)
		{
			if (!m_IsPlayingCurrentStateAnimation)
			{
				return false;
			}

			int sectionIndex;
			if (!CurrentMotionGroup.SectionIndexes.TryGetValue(sectionName, out sectionIndex))
			{
				return false;
			}

			return CurrentClipInfo.Sections[sectionIndex].Contain(m_CurrentClipLocalTime);
		}

		public bool IsInSection(int sectionIndex)
		{
			if (!m_IsPlayingCurrentStateAnimation)
			{
				return false;
			}

			return CurrentClipInfo.Sections[sectionIndex].Contain(m_CurrentClipLocalTime);
		}

		public abstract bool ShouldPerformMotionMatchingLooking();


		#endregion

		/// <summary>
		/// Is called only in MotionMathcing state update;
		/// </summary>
		protected void UpdateSectionDependecies()
		{
			if (CurrentSectionDependecies == null)
			{
				return;
			}

			//m_LogicLayer.SectionsDependecies.Clear();

			//for (int i = 1; i < CurrentSectionDependecies.SectionSettings.Count; i++)
			//{
			//	if (CurrentClipInfo.Sections[i].Contain(m_CurrentClipLocalTime))
			//	{
			//		for (int j = 0; j < CurrentSectionDependecies.SectionSettings[i].SectionInfos.Count; j++)
			//		{
			//			m_LogicLayer.SectionsDependecies.Add(CurrentSectionDependecies.SectionSettings[i].SectionInfos[j]);
			//		}
			//	}
			//}

			float currentClipLocalTime = m_CurrentClipLocalTime;

			m_LogicLayer.SectionsDependecies.Clear();

			List<SectionSettings> sectionSettings = CurrentSectionDependecies.SectionSettings;
			List<DataSection> currentClipSections = CurrentClipInfo.Sections;

			for (int i = 1; i < sectionSettings.Count; i++)
			{
				DataSection dataSection = currentClipSections[i];
				if (dataSection.Contain(currentClipLocalTime))
				{
					List<SectionInfo> sectionInfos = sectionSettings[i].SectionInfos;
					for (int j = 0; j < sectionInfos.Count; j++)
					{
						m_LogicLayer.SectionsDependecies.Add(sectionInfos[j]);
					}
				}
			}
		}

		protected MotionMatchingJobOutput JoinJobsOutput()
		{
			MotionMatchingJobOutput bestOutput = m_LogicLayer.JobsOutput[0];
			for (int i = 1; i < CurrentMotionGroup.JobsCount; i++)
			{
				if (bestOutput.FrameCost > m_LogicLayer.JobsOutput[i].FrameCost)
				{
					bestOutput = m_LogicLayer.JobsOutput[i];
				}
			}

			return bestOutput;
		}

		protected void PlayAnimation(MotionMatchingJobOutput output, float blendTime, float minWeightToAchive)
		{
			PlayAnimation(output.FrameClipIndex, output.FrameTime, blendTime, minWeightToAchive);
		}

		protected void PlayAnimation(int clipIndex, float time, float blendTime, float minWeightToAchive)
		{
			if (!m_IsPlayingCurrentStateAnimation)
			{
				OnPlayFirstAnimation(blendTime);
			}

			motionMatching.OnPlayNewAnim();

			m_IsPlayingCurrentStateAnimation = true;
			m_CurrentClipIndex = clipIndex;
			m_CurrentClipGlobalTime = time;
			m_CurrentClipLocalTime = time;

			//CurrentClipInfo = CurrentMotionGroup.MotionDataInfos[m_CurrentClipIndex];

			StateMixer.PlayMotionMatchingDataInfo(
				CurrentMotionGroup.MotionDataInfos[clipIndex],
				blendTime,
				time,
				minWeightToAchive,
				Index,
				DataLayer.PassIK,
				DataLayer.FootPassIK
				);

			CurrenBlendingAnimationData.Add(new BlendedAnimationData(Index, clipIndex, CurrentClipInfo.FindInYourself));

			SelectEventOnNewAnimationPlay();

			motionMatching.OnPlayMotionMatchingAnimation.Invoke(
				CurrentMotionGroup,
				CurrentClipInfo,
				m_CurrentClipIndex,
				m_CurrentClipLocalTime
				);


			if (m_DataState.SpeedMultiplier != StateMixer.GetSpeedMultiplier())
			{
				StateMixer.SetSpeed(m_DataState.SpeedMultiplier);
			}
		}

		private void RemoveBlendedAnimationData(int index)
		{
			CurrenBlendingAnimationData.RemoveAt(index);
		}

		private void RemoveAllBlendedAnimationData()
		{
			CurrenBlendingAnimationData.Clear();
		}

		protected virtual void OnPlayFirstAnimation(float blendTime)
		{
			var mixer = animationSystem.layers[m_LogicLayer.Index].PlayStateMixer(blendTime, animationSystem.Graph, Index);
			StateMixer.OnStartPlay(ref mixer);

			int stateBlendMixerIndex = m_LogicLayer.m_CurrentBlendedStates.IndexOf(this);

			if (stateBlendMixerIndex != -1)
			{
				m_LogicLayer.m_CurrentBlendedStates[stateBlendMixerIndex] = null;
			}

			m_LogicLayer.m_CurrentBlendedStates.Add(this);
		}

		public void AddBehavior(MotionMatchingStateBehavior behavior)
		{
			if (Behaviors == null)
			{
				Behaviors = new List<MotionMatchingStateBehavior>();
			}
			else
			{
				if (Behaviors.Contains(behavior)) return;
			}

			behavior.SetBasic(
				this,
				motionMatching,
				motionMatching.transform,
				motionMatching.gameObject
				);
			Behaviors.Add(behavior);
		}

		public void RemoveBehavior(MotionMatchingStateBehavior behavior)
		{
			Behaviors.Remove(behavior);
		}

		public void ClearBehaviors()
		{
			Behaviors.Clear();
		}

		public abstract void RunTestJob();

		public virtual bool SetFindingNextAnimationActive(bool isFindingActive)
		{
			return false;
		}

#if UNITY_EDITOR
		public virtual void OnDrawGizmos()
		{

		}
#endif

		private void FireAnimationEvent()
		{
			if (shouldFireAnimationEvents)
			{
				if (CurrentClipInfo.IsLooping)
				{
					while (m_CurrentClipGlobalTime >= animationEventTime)
					{
						motionMatching.InvokeAnimationEvent(currentAnimationEvent.Name);

						currentAnimationEventIndex = (currentAnimationEventIndex + 1) % CurrentClipInfo.AnimationEvents.Count;
						currentAnimationEvent = CurrentClipInfo.AnimationEvents[currentAnimationEventIndex];

						if (currentAnimationEventIndex == 0)
						{
							animationEventTime = Mathf.Ceil(m_CurrentClipGlobalTime / CurrentClipInfo.Length) * CurrentClipInfo.Length + currentAnimationEvent.EventTime;
						}
						else
						{
							animationEventTime = Mathf.Floor(m_CurrentClipGlobalTime / CurrentClipInfo.Length) * CurrentClipInfo.Length + currentAnimationEvent.EventTime;
						}
					}
				}
				else
				{
					while (m_CurrentClipLocalTime >= currentAnimationEvent.EventTime)
					{
						motionMatching.InvokeAnimationEvent(currentAnimationEvent.Name);

						currentAnimationEventIndex = (currentAnimationEventIndex + 1) % CurrentClipInfo.AnimationEvents.Count;

						if (currentAnimationEventIndex == 0)
						{
							shouldFireAnimationEvents = false;
							break;
						}
						else
						{
							currentAnimationEvent = CurrentClipInfo.AnimationEvents[currentAnimationEventIndex];
						}
					}
				}
			}
		}

		private void SelectEventOnNewAnimationPlay()
		{
			List<MotionMatchingAnimationEvent> events = CurrentClipInfo.AnimationEvents;
			if (events == null || events.Count == 0)
			{
				shouldFireAnimationEvents = false;
			}
			else
			{
				shouldFireAnimationEvents = true;

				currentAnimationEvent = null;
				currentAnimationEventIndex = -1;
				for (int i = 0; i < events.Count; i++)
				{
					if (events[i].EventTime > m_CurrentClipLocalTime)
					{
						currentAnimationEventIndex = i;
						currentAnimationEvent = events[i];
						animationEventTime = events[i].EventTime;
						break;
					}
				}

				if (currentAnimationEvent == null)
				{
					if (CurrentClipInfo.IsLooping)
					{
						currentAnimationEventIndex = 0;
						currentAnimationEvent = events[0];

						animationEventTime = CurrentClipInfo.Length + currentAnimationEvent.EventTime;
					}
					else
					{
						shouldFireAnimationEvents = false;
					}
				}
			}
		}


		void CheckSectionDependenciesCOuntInData()
		{
			var nmg = m_DataState.MotionData;
			if (nmg)
			{
				var sd = nmg.SectionsDependencies;
				if (sd == null)
				{
					throw new System.Exception($"Native motion group \"{nmg.name}\" do not have seted section dependencies asset!");
				}

				for (int i = 0; i < nmg.MotionDataInfos.Count; i++)
				{
					var mdi = nmg.MotionDataInfos[i];

					if (mdi.Sections.Count != sd.SectionsCount)
					{
						throw new System.Exception($"In native motion group \"{nmg.name}\" animation data have diffrent sections count than setted Section Dependencies asset with name \"{sd.name}\"!");
					}
				}
			}
		}
	}

}