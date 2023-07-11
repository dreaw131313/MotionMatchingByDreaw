using MotionMatching.Gameplay.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public struct BlendedAnimationData
	{
		public int StateIndex;
		public int ClipIndex;
		public bool FindInYourself;

		public BlendedAnimationData(int stateIndex, int clipIndex, bool findInYourself)
		{
			StateIndex = stateIndex;
			ClipIndex = clipIndex;
			FindInYourself = findInYourself;
		}
	}


	public struct SwitchStateInfo
	{
		public int NextStateIndex;
		public int SectionMask;
		public float BlendTime;
		public bool ShouldSwitch;
		public uint Priority;
		public bool PerformFinding;
		public int SectionMaskAfterFinding;

		public SwitchStateInfo(
			int nextStateIndex,
			int sectionMask,
			float blendTime,
			bool shouldSwitch,
			bool performFinding,
			int sectionMaskAfetrFinding,
			uint priority = 0
			)
		{
			NextStateIndex = nextStateIndex;
			SectionMask = sectionMask;
			BlendTime = blendTime;
			ShouldSwitch = shouldSwitch;
			Priority = priority;
			PerformFinding = performFinding;
			SectionMaskAfterFinding = sectionMaskAfetrFinding;
		}
	}

	public class LogicMotionMatchingLayer
	{
		private MotionMatchingLayer_SO dataLayer;
		private Transform transform;
		private MotionMatchingComponent motionMatching;
		private PlayableAnimationSystem animationSystem;

		public List<LogicState> logicStates;

		public List<LogicState> m_CurrentBlendedStates;


		public NativeList<BlendedAnimationData> AnimationDataForJob;

		// Switch State features

		//public NativeArray<TrajectoryPoint> CurrentAnimationTrajectory;
		//public NativeArray<TrajectoryPoint> AnimationTrajectoryBuffor;

		public NativeArray<BoneData> CurrentPose;
		//private NativeArray<BoneData> PoseBuffor;

		public List<SwitchStateContact> globalSpaceContacts; // for moving between
		public NativeList<FrameContact> localSpaceContacts; // for job
		public NativeList<FrameContact> animationContactPoints; // for moving between globalSpaceContacts, geted from native motion group

		// Switching state:
		private SwitchStateInfo stateSwitchInfo;
		internal bool WillSwitchState
		{
			get
			{
				return stateSwitchInfo.ShouldSwitch;
			}
		}

		public int CurrentStateIndex { get; private set; }
		public LogicState CurrentLogicState { get { return logicStates[CurrentStateIndex]; } set { } }
		public State_SO CurrentDataState { get { return dataLayer.States[CurrentStateIndex]; } set { } }
		public PlayableAnimationLayerData AnimationLayer { get { return animationSystem.layers[Index]; } set { } }
		public int Index { get => dataLayer.Index; private set { } }

		public MotionMatchingLayer_SO DataLayer { get => dataLayer; private set { } }

		public SwitchStateInfo StateSwitchInfo { get => stateSwitchInfo; private set => stateSwitchInfo = value; }

		#region global stuff for states:
		public NativeList<SectionInfo> SectionsDependecies;
		public NativeArray<MotionMatchingJobOutput> JobsOutput;

		// Jobs:
		public JobHandle MotionMatchingJobHandle;
		public MotionMatchingJob MotionMatchingStateJob;
		public JobHandle SingleAnimationJobHandle;
		public SingleAnimationMotionMatchingJob SingleAnimationStateJob;
		public JobHandle ContactJobHandle;
		public ContactMotionMatchingJob ContactStateJob;
		public JobHandle ImpactJobHandle;
		public ImpactMotionMatchingJob ImpactStateJob;

		// Pose evaluation job
		CurrentPoseCalculationJob poseCalculationJob;
		NativeList<PoseCalculationClipInfo> poseCalcualationClipInfos;
		public JobHandle poseCalculationJobHandle;

		#endregion

		public LogicMotionMatchingLayer(
			MotionMatchingLayer_SO layer,
			MotionMatchingComponent motionMatching,
			PlayableAnimationSystem animationSystem
			)
		{
			this.motionMatching = motionMatching;
			this.transform = motionMatching.transform;
			this.dataLayer = layer;
			this.animationSystem = animationSystem;

			stateSwitchInfo = new SwitchStateInfo(-1, -1, -1, false, false, 0);
			logicStates = new List<LogicState>();

			m_CurrentBlendedStates = new List<LogicState>();
			AnimationDataForJob = new NativeList<BlendedAnimationData>(10, Allocator.Persistent);

			int poseBonesCount = dataLayer.GetPoseBonesCount();
			CurrentPose = new NativeArray<BoneData>(poseBonesCount, Allocator.Persistent);
			//PoseBuffor = new NativeArray<BoneData>(poseBonesCount, Allocator.Persistent);

			int contactPointsStartSize = 5;
			globalSpaceContacts = new List<SwitchStateContact>();
			localSpaceContacts = new NativeList<FrameContact>(contactPointsStartSize, Allocator.Persistent);
			animationContactPoints = new NativeList<FrameContact>(contactPointsStartSize, Allocator.Persistent);

			MotionMatchingStateJob = new MotionMatchingJob();
			SingleAnimationStateJob = new SingleAnimationMotionMatchingJob();
			ContactStateJob = new ContactMotionMatchingJob();

			SectionsDependecies = new NativeList<SectionInfo>(10, Allocator.Persistent);

			//int singleTrajectoryPointsCount = dataLayer.GetSingleTrajectoryPointsCount();
			//CurrentAnimationTrajectory = new NativeArray<TrajectoryPoint>(singleTrajectoryPointsCount, Allocator.Persistent);
			//AnimationTrajectoryBuffor = new NativeArray<TrajectoryPoint>(singleTrajectoryPointsCount, Allocator.Persistent);

			int poseEvaluationClipCount = 10;
			poseCalculationJob = new CurrentPoseCalculationJob();
			poseCalcualationClipInfos = new NativeList<PoseCalculationClipInfo>(poseEvaluationClipCount, Allocator.Persistent);

			AnimationLayer.OnRemoveClipPlayable = RemoveBlendedState;
			AnimationLayer.OnRemovingAllPlayables = OnClearAllBlendedStates;
		}

		#region Initialization

		public void InitializeJobsOutputArray(int jobsOutputCount)
		{
			JobsOutput = new NativeArray<MotionMatchingJobOutput>(jobsOutputCount, Allocator.Persistent);

			for (int i = 0; i < jobsOutputCount; i++)
			{
				JobsOutput[i] = new MotionMatchingJobOutput(144, 144, 144);
			}
		}
		#endregion


		public void FixedUpdate()
		{
			CurrentLogicState.FixedUpdate();
		}

		public void Update()
		{
			CurrentLogicState.Update();
			CurrentLogicState.PerformAnimationTimeScaling();

			float deltaTime = Time.deltaTime;
			for(int i = 0; i < m_CurrentBlendedStates.Count; i++)
			{
				LogicState lState = m_CurrentBlendedStates[i];
				lState?.StateMixer.PerformBlends(deltaTime);
			}
		}

		public void LateUpdate()
		{
			CurrentLogicState.LateUpdate();
			CheckCurrentStateTransitions();
			SwitchStateLogic();
		}

		public void OnDestroy()
		{
			WaitForJobsEnd();

			for (int i = 0; i < logicStates.Count; i++)
			{
				logicStates[i].OnDestroy();
			}

			if (SectionsDependecies.IsCreated)
			{
				SectionsDependecies.Dispose();
			}
			if (JobsOutput.IsCreated)
			{
				JobsOutput.Dispose();
			}

			if (poseCalcualationClipInfos.IsCreated)
			{
				poseCalcualationClipInfos.Dispose();
			}

			// Pose
			CurrentPose.Dispose();
			//PoseBuffor.Dispose();
			// Trajectory
			//CurrentAnimationTrajectory.Dispose();
			//AnimationTrajectoryBuffor.Dispose();
			// blended clips
			AnimationDataForJob.Dispose();
			// Contacts 
			localSpaceContacts.Dispose();
			animationContactPoints.Dispose();
		}

		public void BeginMotionMatchingStateMachine()
		{
			CurrentStateIndex = dataLayer.StartStateData.StartState.Index;
			CurrentLogicState.Start();
		}

		public bool IsTrajectorryCorrectionEnabledInCurrentState()
		{
			return CurrentDataState.TrajectoryCorrection;
		}

		public void RemoveBlendedState(int index)
		{
			m_CurrentBlendedStates.RemoveAt(index);
		}

		public void OnClearAllBlendedStates()
		{
			m_CurrentBlendedStates.Clear();
		}

		public void SetTrajectoryFromMotionGroup(ref NativeArray<TrajectoryPoint> trajectory)
		{
			if (m_CurrentBlendedStates.Count == 0) return;

			var logicState = m_CurrentBlendedStates[m_CurrentBlendedStates.Count - 1];

			int lastClipIndex = logicState.CurrenBlendingAnimationData.Count - 1;

			BlendedAnimationData data = logicState.CurrenBlendingAnimationData[lastClipIndex];

			dataLayer.States[data.StateIndex].MotionData.GetTrajectoryInTime(
					ref trajectory,
					logicState.StateMixer.GetMixerInputTime(lastClipIndex),
					data.ClipIndex
					);
		}

		public void SetPastTrajectoryFromMotionGroup(ref NativeArray<TrajectoryPoint> trajectory)
		{
			if (m_CurrentBlendedStates.Count == 0) return;

			var logicState = m_CurrentBlendedStates[m_CurrentBlendedStates.Count - 1];

			int lastClipIndex = logicState.CurrenBlendingAnimationData.Count - 1;

			BlendedAnimationData data = logicState.CurrenBlendingAnimationData[lastClipIndex];

			dataLayer.States[data.StateIndex].MotionData.UpdateTrajectoryFromDataWithTrajectoryCostCorrection(
					ref trajectory,
					motionMatching.FirstIndexWithFutureTime,
					logicState.StateMixer.GetMixerInputTime(lastClipIndex),
					data.ClipIndex
					);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="trajectoryPointIndex"></param>
		/// <returns>Animation point position in local space</returns>
		public Vector3 GetCurrentAnimationTrajectoryPointPosition(int trajectoryPointIndex)
		{
			if (m_CurrentBlendedStates.Count == 0) return transform.forward;

			if (m_CurrentBlendedStates.Count == 0)
			{
				return transform.position;
			}

			var logicState = m_CurrentBlendedStates[m_CurrentBlendedStates.Count - 1];

			int lastClipIndex = logicState.CurrenBlendingAnimationData.Count - 1;
			BlendedAnimationData data = logicState.CurrenBlendingAnimationData[lastClipIndex];

			return dataLayer.States[data.StateIndex].MotionData.GetTrajectoryPointPosition(
				data.ClipIndex,
				logicState.StateMixer.GetMixerInputTime(lastClipIndex),
				trajectoryPointIndex
				);
		}


		public void PrepareAndStartPoseCalculationJob()
		{
			int lastPlayingStateIndex = m_CurrentBlendedStates.Count - 1;
			var lastLogicState = m_CurrentBlendedStates[lastPlayingStateIndex];

			float stateWeight = animationSystem.layers[Index].StatesMixer.GetInputWeight(lastPlayingStateIndex);

			float deltaTime = Time.deltaTime;
			poseCalcualationClipInfos.Clear();

			NativeMotionGroup oldMotionGroup = lastLogicState.m_DataState.MotionData;

			int playableCount = lastLogicState.StateMixer.GetInputCount();

			for (int playableIndex = 0; playableIndex < playableCount; playableIndex++)
			{
				BlendedAnimationData currentPlayingData = lastLogicState.CurrenBlendingAnimationData[playableIndex];

				MotionMatchingDataInfo motionMatchingDataInfo = oldMotionGroup.MotionDataInfos[currentPlayingData.ClipIndex];

				PoseCalculationClipInfo clipInfo = new PoseCalculationClipInfo();
				clipInfo.CurrentTime = (lastLogicState.StateMixer.GetMixerInputTime(playableIndex) + deltaTime) % motionMatchingDataInfo.Length;
				clipInfo.StartBoneIndex = motionMatchingDataInfo.StartBoneIndex;
				clipInfo.Weight = lastLogicState.StateMixer.GetInputWeight(playableIndex);
				clipInfo.FrameTime = motionMatchingDataInfo.FrameTime;
				clipInfo.FramesCount = motionMatchingDataInfo.FrameDataCount;

				poseCalcualationClipInfos.Add(clipInfo);
			}

			poseCalculationJob.PoseBonesCount = CurrentPose.Length;
			poseCalculationJob.ClipsInfos = poseCalcualationClipInfos;
			poseCalculationJob.Bones = oldMotionGroup.Bones;
			poseCalculationJob.OutputPose = CurrentPose;

			NativeMotionGroup newMotionGroup = CurrentLogicState.CurrentMotionGroup;

			// old motion group weights:
			poseCalculationJob.OldPoseWeight = oldMotionGroup.PoseCostWeight;
			poseCalculationJob.OldBonesWeights = oldMotionGroup.NativeNormalizedBonesWeights;

			// new motion group weights:
			poseCalculationJob.NewPoseWeight = newMotionGroup.PoseCostWeight;
			poseCalculationJob.NewBonesWeights = newMotionGroup.NativeNormalizedBonesWeights;


			poseCalculationJobHandle = poseCalculationJob.Schedule(motionMatching.TransformTrajectoryJobHandle);
		}

		internal void MultiplayContactsByWeightFromNativeMotionGroup(float contactsWeight)
		{
			//float contactsWeight = this.CurrentMotionGroup.ContactsCostWeight;

			for (int i = 0; i < localSpaceContacts.Length; i++)
			{
				FrameContact contact = localSpaceContacts[i];
				contact.position = contact.position * contactsWeight;
				contact.normal = contact.normal * contactsWeight;
				localSpaceContacts[i] = contact;
			}
		}

		private void CheckCurrentStateTransitions()
		{
			if (stateSwitchInfo.Priority < 1 && CurrentLogicState.m_IsPlayingCurrentStateAnimation)
			{
				for (int transitionIndex = 0; transitionIndex < CurrentDataState.Transitions.Count; transitionIndex++)
				{
					Transition transition = CurrentDataState.Transitions[transitionIndex];
					float localTime = CurrentLogicState.m_CurrentClipLocalTime;
					if (CurrentDataState.StateType != MotionMatchingStateType.MotionMatching &&
						CurrentLogicState.m_CurrentClipGlobalTime > CurrentLogicState.CurrentClipInfo.Length &&
						!CurrentLogicState.CurrentClipInfo.IsLooping)
					{
						localTime = CurrentLogicState.CurrentClipInfo.Length;
					}

					int optionIndex = transition.ShouldTransitionBegin(
						localTime,
						CurrentLogicState.m_CurrentClipGlobalTime,
						motionMatching,
						CurrentDataState.StateType,
						CurrentLogicState.CurrentClipInfo,
						CurrentLogicState.CurrentMotionGroup
						);
					if (optionIndex > -1)
					{
						if (!logicStates[transition.nextStateIndex].m_IsBlockedToEnter)
						{
							TransitionOptions option = transition.options[optionIndex];
							int nextStateIndex = transition.nextStateIndex;
							int sectionMask;
							int sectionMaskAfterFirstFinding;
							State_SO nextState = dataLayer.States[nextStateIndex];

							if (nextState.MotionData.SectionsDependencies != null)
							{
								sectionMask = option.WhereCanFindingBestPoseSection;
								sectionMaskAfterFirstFinding = option.SectionAfterFirstFinding;
							}
							else
							{
								sectionMask = 1;
								sectionMaskAfterFirstFinding = 1;
							}

							stateSwitchInfo = new SwitchStateInfo(
								nextStateIndex,
								sectionMask,
								option.BlendTime,
								true,
								option.PerformFinding,
								sectionMaskAfterFirstFinding,
								1
								);
						}
					}
				}
			}
		}

		private void SwitchStateLogic()
		{
			if (stateSwitchInfo.ShouldSwitch /*&& animationSystem.layers[Index].BlendGroupsCount < 2*/)
			{
				if (MotionMatchingJobHandle.IsCompleted &&
					SingleAnimationJobHandle.IsCompleted &&
					ContactJobHandle.IsCompleted)
				{
					SwitchState(stateSwitchInfo);
					stateSwitchInfo = new SwitchStateInfo(-1, -1, -1, false, false, 0);
				}
			}
		}

		public bool SetSwitchStateInfo(SwitchStateInfo info)
		{
			if (info.Priority < stateSwitchInfo.Priority)
			{
				return false;
			}

			if (logicStates[info.NextStateIndex].m_IsBlockedToEnter || stateSwitchInfo.NextStateIndex == info.NextStateIndex)
			{
				return false;
			}

			stateSwitchInfo = info;
			return true;
		}

		internal bool OnReenterToState(SwitchStateInfo info)
		{
			MotionMatchingStateType curretnStateType = CurrentDataState.StateType;
			if (CurrentStateIndex == info.NextStateIndex && info.PerformFinding &&
				(curretnStateType == MotionMatchingStateType.ContactAnimationState ||
				curretnStateType == MotionMatchingStateType.SingleAnimation))
			{
				LogicImpactState logicImpactState = CurrentLogicState as LogicImpactState;
				if (logicImpactState != null)
				{
					CurrentLogicState.CompleteScheduledJobs();
					CurrentLogicState.OnReEnter(info);
					return true;
				}

				LogicSingleAnimationState singleAnimationState = CurrentLogicState as LogicSingleAnimationState;
				if (singleAnimationState != null)
				{
					CurrentLogicState.CompleteScheduledJobs();
					CurrentLogicState.OnReEnter(info);
					return true;
				}
			}

			return false;
		}

		public void UpdateCurrentNativeBlendedAnimationData(LogicState fromState)
		{
			AnimationDataForJob.Clear();
			for (int i = 0; i < fromState.CurrenBlendingAnimationData.Count; i++)
			{
				AnimationDataForJob.Add(fromState.CurrenBlendingAnimationData[i]);
			}
		}


		/// <summary>
		/// Switching state in logic layer class.
		/// </summary>
		/// <param name="nextStateIndex"></param>
		/// <param name="blendTime"></param>
		private void SwitchState(SwitchStateInfo switchStateInfo)
		{
			LogicState oldState = CurrentLogicState;
			LogicState newState = logicStates[switchStateInfo.NextStateIndex];

			newState.OnPreEnter();

			if (0 <= CurrentStateIndex && CurrentStateIndex < logicStates.Count)
			{
				CurrentLogicState.CompleteScheduledJobs();
				CurrentLogicState.Exit();
			}

			CurrentStateIndex = switchStateInfo.NextStateIndex;
			CurrentLogicState.Enter(switchStateInfo);

			motionMatching.SwitchStateEvent.Invoke(oldState, newState);
		}

		public MotionMatchingDataInfo GetDataInfo(BlendedAnimationData blendedData)
		{
			return dataLayer.States[blendedData.StateIndex].MotionData.MotionDataInfos[blendedData.ClipIndex];
		}

		public MotionMatchingStateType GetCurrentStateType()
		{
			return CurrentDataState.StateType;
		}

		/// <summary>
		/// Contacts position and normal must be in global space.
		/// </summary>
		/// <param name="contacts"></param>
		public void SetContactPoints(ref SwitchStateContact[] contacts)
		{
			ContactJobHandle.Complete();
			globalSpaceContacts.Clear();
			localSpaceContacts.Clear();

			for (int i = 0; i < contacts.Length; i++)
			{
				globalSpaceContacts.Add(contacts[i]);
				FrameContact localContact = new FrameContact(
					transform.InverseTransformPoint(contacts[i].frameContact.position),
					transform.InverseTransformDirection(contacts[i].frameContact.normal),
					false
					);
				localSpaceContacts.Add(localContact);
			}
		}

		public void SetImpact(SwitchStateImpact impact)
		{
			ContactJobHandle.Complete();
			globalSpaceContacts.Clear();
			localSpaceContacts.Clear();

			globalSpaceContacts.Add(new SwitchStateContact(
				new FrameContact(impact.Position, impact.Direction, false),
				Vector3.zero
				));
			FrameContact localContact = new FrameContact(
					transform.InverseTransformPoint(globalSpaceContacts[0].frameContact.position),
					transform.InverseTransformDirection(globalSpaceContacts[0].frameContact.normal),
					false
					);
			localSpaceContacts.Add(localContact);
		}

		public void SetContactPoints(List<SwitchStateContact> contacts)
		{
			ContactJobHandle.Complete();
			globalSpaceContacts.Clear();
			localSpaceContacts.Clear();

			for (int i = 0; i < contacts.Count; i++)
			{
				globalSpaceContacts.Add(contacts[i]);
				FrameContact localContact = new FrameContact(
					transform.InverseTransformPoint(contacts[i].frameContact.position),
					transform.InverseTransformDirection(contacts[i].frameContact.normal),
					false
					);
				localSpaceContacts.Add(localContact);
			}
		}

		internal void DoAndCompleteFirstJobs()
		{
			// First MotionMatchingJob
			for (int i = 0; i < logicStates.Count; i++)
			{
				if (dataLayer.States[i].StateType == MotionMatchingStateType.MotionMatching)
				{
					logicStates[i].RunTestJob();
					MotionMatchingJobHandle.Complete();
					break;
				}
			}

			// First singleAnimationJob
			for (int i = 0; i < logicStates.Count; i++)
			{
				if (dataLayer.States[i].StateType == MotionMatchingStateType.SingleAnimation)
				{
					logicStates[i].RunTestJob();
					SingleAnimationJobHandle.Complete();
					break;
				}
			}

			// First ContactJob
			for (int i = 0; i < logicStates.Count; i++)
			{
				if (dataLayer.States[i].StateType == MotionMatchingStateType.ContactAnimationState)
				{
					LogicState s = logicStates[i];
					logicStates[i].RunTestJob();
					ContactJobHandle.Complete();
					break;
				}
			}
		}

		//#if UNITY_EDITOR
		/// <summary>
		/// Editor only. have low performance
		/// </summary>
		/// <returns>Current pose.</returns>
		public void GetCurrentPose(ref BoneData[] pose)
		{
			//poseCalculationJobHandle.Complete();
			//for (int i = 0; i < CurrentPose.Length; i++)
			//{
			//	pose[i] = CurrentPose[i];
			//}

			for (int i = 0; i < CurrentPose.Length; i++)
			{
				pose[i] = new BoneData(float3.zero, float3.zero);
			}

			var lastBlendedLogicState = m_CurrentBlendedStates[m_CurrentBlendedStates.Count - 1];

			for (int clipDataIndex = 0; clipDataIndex < lastBlendedLogicState.CurrenBlendingAnimationData.Count; clipDataIndex++)
			{
				BlendedAnimationData data = lastBlendedLogicState.CurrenBlendingAnimationData[clipDataIndex];

				var logicState = logicStates[data.StateIndex];

				dataLayer.States[data.StateIndex].MotionData.GetCurrentPoseInTimeWithWeight(
					ref pose,
					data.ClipIndex,
					lastBlendedLogicState.StateMixer.GetMixerInputTime(clipDataIndex),
					lastBlendedLogicState.StateMixer.GetInputWeight(clipDataIndex)
					);
			}
		}
		//#endif

		internal void WaitForJobsEnd()
		{
			MotionMatchingJobHandle.Complete();
			SingleAnimationJobHandle.Complete();
			ContactJobHandle.Complete();
			ImpactJobHandle.Complete();
			poseCalculationJobHandle.Complete();
		}
	}
}
