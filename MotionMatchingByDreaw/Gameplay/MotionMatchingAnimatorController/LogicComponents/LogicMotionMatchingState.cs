using MotionMatching.Gameplay.Jobs;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public enum WinerFrameCheckingType
	{
		CheckOnlyLastClip,
		CheckAllPlayedClips
	}
	// Motion machiing state logic
	public class LogicMotionMatchingState : LogicState
	{
		private float findingTimer = 0f;
		private bool onEnterStateFinding = false;
		private bool isFindingNewPose = false;
		private bool isFirstClipPlayed;
		private float firstAnimBlendTime;

		private bool isFindingNextAnimationEnabled = true;

		MotionMatchingStateFeatures Features;

		// not looking for new pose section
		bool isCurrentAnimationHaveNotLookingIntervals;
		int currentNotLookingIntervalIndex;
		List<float2> notLookingForNewPoseIntervals;

		public LogicMotionMatchingState(
			MotionMatchingState_SO state,
			MotionMatchingComponent component,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			) :
			base(state, component, logicLayer, animationSystem)
		{
			Features = state.Features;

			onEnterStateFinding = false;
			isFindingNewPose = false;
		}

		public override void Awake()
		{
			base.Awake();
		}

		public override void Start()
		{
			base.Start();
			findingTimer = 0f;
			isFindingNewPose = false;
			onEnterStateFinding = false;
			isFirstClipPlayed = true;
			m_CurrentSectionMask = m_DataState.StartSection;
		}

		public override void Enter(SwitchStateInfo switchStateInfo)
		{
			base.Enter(switchStateInfo);

			UpdateSectionMask();

			isFindingNextAnimationEnabled = true;

			firstAnimBlendTime = switchStateInfo.BlendTime;
			findingTimer = 0f;
			isFindingNewPose = true;
			onEnterStateFinding = true;
			isFirstClipPlayed = false;

			motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
			m_LogicLayer.PrepareAndStartPoseCalculationJob();


			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.SingleAnimationStateJob.BatchSize = batchSize;
			//logicLayer.SingleAnimationStateJob.TrajectoryWeight = dataState.TrajectoryCostWeight;
			//logicLayer.SingleAnimationStateJob.PoseWeight = dataState.PoseCostWeight;
			m_LogicLayer.SingleAnimationStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.SingleAnimationStateJob.CurrentPose = m_LogicLayer.CurrentPose;
			m_LogicLayer.SingleAnimationStateJob.SectionMask = m_CurrentSectionMask;
			m_LogicLayer.SingleAnimationStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.SingleAnimationStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.SingleAnimationStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.SingleAnimationStateJob.Outputs = m_LogicLayer.JobsOutput;
			m_LogicLayer.SingleAnimationStateJob.FindOnlyInSelectedData = false;

			m_LogicLayer.SingleAnimationJobHandle = m_LogicLayer.SingleAnimationStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize,
				m_LogicLayer.poseCalculationJobHandle
				);

			if (switchStateInfo.PerformFinding)
			{
				SetCurrentSectionMask(switchStateInfo.SectionMaskAfterFinding);
			}
		}

		public override void Exit()
		{
			base.Exit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
		}

		public override void Update()
		{
			if (isFindingNewPose)
			{
				CompleteFindingNewPose();
			}

			base.Update();
		}

		public override void LateUpdate()
		{
			base.LateUpdate();

			if (!isFindingNewPose)
			{
				if (findingTimer > Features.updateInterval)
				{
					findingTimer = 0f;

					bool isInNotLookingSection = false;
					if (isCurrentAnimationHaveNotLookingIntervals)
					{
						isInNotLookingSection = IsInNotLookingForSectionIntervals();
					}

					if (isFindingNextAnimationEnabled &&
						StateMixer.GetInputCount() < Features.maxBlendedClipCount &&
						!isInNotLookingSection)
					{
						isFindingNewPose = true;
						StartJobsToFindNewBestPlace();
					}
				}
				else
				{
					findingTimer += Time.deltaTime;
				}
			}

			if (m_IsPlayingCurrentStateAnimation)
			{
				m_CurrentClipGlobalTime = StateMixer.GetLastAnimationTime();
				if (CurrentClipInfo.IsLooping)
				{
					m_CurrentClipLocalTime = m_CurrentClipGlobalTime % CurrentClipInfo.Length;
				}
				else
				{
					m_CurrentClipLocalTime = Mathf.Clamp(m_CurrentClipGlobalTime, 0f, CurrentClipInfo.Length);
				}
			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		internal override void CompleteScheduledJobs()
		{
			m_LogicLayer.SingleAnimationJobHandle.Complete();
			m_LogicLayer.MotionMatchingJobHandle.Complete();
		}

		private void CompleteFindingNewPose()
		{
			if (onEnterStateFinding)
			{
				//if (!m_LogicLayer.SingleAnimationJobHandle.IsCompleted)
				//{
				//	return;
				//}
				onEnterStateFinding = false;
				m_LogicLayer.SingleAnimationJobHandle.Complete();


				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].OnCompleteEnterFindingJob();
				}
			}
			else
			{
				if (!m_LogicLayer.MotionMatchingJobHandle.IsCompleted)
				{
					return;
				}

				m_LogicLayer.MotionMatchingJobHandle.Complete();
			}
			isFindingNewPose = false;

			MotionMatchingJobOutput output = JoinJobsOutput();
			if (!IsTheWinnerAtTheSamePlace(WinerFrameCheckingType.CheckAllPlayedClips, output))
			{
				if (m_CurrentClipIndex == output.FrameClipIndex && CurrentClipInfo.BlendToYourself ||
					m_CurrentClipIndex != output.FrameClipIndex ||
					!isFirstClipPlayed)
				{
					PlayAnimation(
						output,
						isFirstClipPlayed ? Features.blendTime : firstAnimBlendTime,
						Features.minWeightToAchive
						);
					isFirstClipPlayed = true;

					FetchNotLookingForNewPoseSectionData();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>True if new animation should be played, else false.</returns>
		private bool IsTheWinnerAtTheSamePlace(
			WinerFrameCheckingType type,
			MotionMatchingJobOutput output
			)
		{
			if (CurrenBlendingAnimationData.Count == 0)
			{
				return false;
			}
			switch (type)
			{
				case WinerFrameCheckingType.CheckOnlyLastClip:
					{
						int lastBlendedDataIndex = CurrenBlendingAnimationData.Count - 1;
						return IsOutputAtTheSamePlace(
							output,
							CurrenBlendingAnimationData[lastBlendedDataIndex],
							StateMixer.GetMixerInputTime(lastBlendedDataIndex)
							);
					}
				case WinerFrameCheckingType.CheckAllPlayedClips:
					{
						for (int i = 0; i < CurrenBlendingAnimationData.Count; i++)
						{
							if (IsOutputAtTheSamePlace(
								output,
								CurrenBlendingAnimationData[i],
								StateMixer.GetMixerInputTime(i)
								))
							{
								return true;
							}
						}
					}
					break;
			}

			return false;
		}

		private bool IsOutputAtTheSamePlace(MotionMatchingJobOutput output, BlendedAnimationData blendedData, float blendedDataTime)
		{
			if (output.FrameClipIndex != blendedData.ClipIndex ||
				Index != blendedData.StateIndex)
			{
				return false;
			}

			BlendedAnimationData newData = new BlendedAnimationData();
			newData.ClipIndex = output.FrameClipIndex;
			newData.StateIndex = this.Index;

			MotionMatchingDataInfo blendedDataInfo = m_LogicLayer.GetDataInfo(blendedData);
			//MotionMatchingDataInfo newDataInfo = m_LogicLayer.GetDataInfo(newData);

			float localTime = blendedDataTime % blendedDataInfo.Length;

			float clipDeltaTime = math.abs((float)(localTime - output.FrameTime));
			if (clipDeltaTime > Features.maxClipDeltaTime)
			{
				return false;
			}

			return true;
		}

		private void StartJobsToFindNewBestPlace()
		{
			UpdateSectionMask();
			//logicLayer.UpdateCurrentPose();
			motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
			UpdateSectionDependecies();
			m_LogicLayer.UpdateCurrentNativeBlendedAnimationData(this);
			StartMotionMatchingJob();
		}

		private void StartMotionMatchingJob()
		{
			m_LogicLayer.PrepareAndStartPoseCalculationJob();

			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.MotionMatchingStateJob.BatchSize = batchSize;
			m_LogicLayer.MotionMatchingStateJob.SectionMask = m_CurrentSectionMask;
			m_LogicLayer.MotionMatchingStateJob.CurrentStateIndex = Index;


			m_LogicLayer.MotionMatchingStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.MotionMatchingStateJob.CurrentPose = m_LogicLayer.CurrentPose;

			m_LogicLayer.MotionMatchingStateJob.CurrentPlayingClips = m_LogicLayer.AnimationDataForJob;
			m_LogicLayer.MotionMatchingStateJob.sectionDependecies = m_LogicLayer.SectionsDependecies;

			m_LogicLayer.MotionMatchingStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.MotionMatchingStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.MotionMatchingStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.MotionMatchingStateJob.Outputs = m_LogicLayer.JobsOutput;

			m_LogicLayer.MotionMatchingJobHandle = m_LogicLayer.MotionMatchingStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize,
				m_LogicLayer.poseCalculationJobHandle
				);
		}

		public override void RunTestJob()
		{
			int batchSize = CurrentMotionGroup.FramesPerThread;

			m_LogicLayer.MotionMatchingStateJob.BatchSize = batchSize;
			m_LogicLayer.MotionMatchingStateJob.SectionMask = m_CurrentSectionMask;
			m_LogicLayer.MotionMatchingStateJob.CurrentStateIndex = Index;

			m_LogicLayer.MotionMatchingStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.MotionMatchingStateJob.CurrentPose = m_LogicLayer.CurrentPose;

			m_LogicLayer.MotionMatchingStateJob.CurrentPlayingClips = m_LogicLayer.AnimationDataForJob;
			m_LogicLayer.MotionMatchingStateJob.sectionDependecies = m_LogicLayer.SectionsDependecies;

			m_LogicLayer.MotionMatchingStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.MotionMatchingStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.MotionMatchingStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.MotionMatchingStateJob.Outputs = m_LogicLayer.JobsOutput;

			m_LogicLayer.MotionMatchingJobHandle = m_LogicLayer.MotionMatchingStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize
				);
		}

		public override bool ShouldPerformMotionMatchingLooking()
		{
			return false;
		}

		public override bool SetFindingNextAnimationActive(bool isFindingActive)
		{
			isFindingNextAnimationEnabled = isFindingActive;
			return true;
		}

		private void FetchNotLookingForNewPoseSectionData()
		{
			isCurrentAnimationHaveNotLookingIntervals = CurrentClipInfo.NotLookingForNewPose.timeIntervals != null && CurrentClipInfo.NotLookingForNewPose.timeIntervals.Count > 0;
			if (isCurrentAnimationHaveNotLookingIntervals)
			{
				currentNotLookingIntervalIndex = CurrentClipInfo.NotLookingForNewPose.GetNextOrCurrentIntervalIndex(m_CurrentClipLocalTime);

				notLookingForNewPoseIntervals = CurrentClipInfo.NotLookingForNewPose.timeIntervals;
			}
		}

		private bool IsInNotLookingForSectionIntervals()
		{
			float2 interval = notLookingForNewPoseIntervals[currentNotLookingIntervalIndex];

			if (CurrentClipInfo.IsLooping)
			{
				float animationGlobalTime = m_CurrentClipGlobalTime;
				float animationLength = CurrentClipInfo.Length;
				float globalLoopsTime = Mathf.Floor(m_CurrentClipGlobalTime / animationLength) * animationLength;
				interval.x += globalLoopsTime;
				interval.y += globalLoopsTime;

				if (interval.x <= animationGlobalTime && animationGlobalTime < interval.y)
				{
					return true;
				}
				else if (interval.y <= animationGlobalTime)
				{
					currentNotLookingIntervalIndex = (currentNotLookingIntervalIndex + 1) % notLookingForNewPoseIntervals.Count;

					if (currentNotLookingIntervalIndex == 0)
					{
						float2 nextInterval = notLookingForNewPoseIntervals[currentNotLookingIntervalIndex];
						nextInterval.x += globalLoopsTime + animationLength;
						nextInterval.y += globalLoopsTime + animationLength;

						if (nextInterval.x <= animationGlobalTime && animationGlobalTime < nextInterval.y)
						{
							return true;
						}
					}
					else
					{
						if (interval.x <= animationGlobalTime && animationGlobalTime < interval.y)
						{
							return true;
						}
					}
				}
			}
			else
			{
				float localTime = m_CurrentClipLocalTime;

				if (currentNotLookingIntervalIndex == notLookingForNewPoseIntervals.Count - 1)
				{
					if (interval.x <= localTime && localTime < interval.y)
					{
						return true;
					}
				}
				else
				{
					if (interval.x <= localTime && localTime < interval.y)
					{
						return true;
					}
					else if (interval.y <= localTime)
					{
						currentNotLookingIntervalIndex = (currentNotLookingIntervalIndex + 1) % notLookingForNewPoseIntervals.Count;
						interval = notLookingForNewPoseIntervals[currentNotLookingIntervalIndex];

						if (interval.x <= localTime && localTime < interval.y)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
