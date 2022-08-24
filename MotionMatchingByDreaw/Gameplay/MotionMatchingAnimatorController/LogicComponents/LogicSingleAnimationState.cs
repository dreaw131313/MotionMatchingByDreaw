using MotionMatching.Gameplay.Jobs;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	// Single animation Motion machiing state logic

	public class LogicSingleAnimationState : LogicState
	{

		bool isFindingRunning;
		bool shouldPlayAnimationWithoutFinding;
		float transitionBlendTime = 0f;
		private bool isStateAnimationPlaying;

		public SingleAnimationStateFeatures Features;

		public bool IsStateAnimationPlaying { get => isStateAnimationPlaying; private set => isStateAnimationPlaying = value; }

		public LogicSingleAnimationState(
			SingleAnimationState_SO state,
			MotionMatchingComponent component,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			) :
			base(state, component, logicLayer, animationSystem)
		{
			IsStateAnimationPlaying = false;

			Features = state.Features;
		}

		public override void Enter(SwitchStateInfo switchStateInfo)
		{
			base.Enter(switchStateInfo);

			m_CurrentClipGlobalTime = 0;
			m_CurrentClipLocalTime = 0;

			isFindingRunning = true;
			transitionBlendTime = switchStateInfo.BlendTime;

			UpdateSectionMask();

			if (switchStateInfo.PerformFinding)
			{
				motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
				m_LogicLayer.PrepareAndStartPoseCalculationJob();

				shouldPlayAnimationWithoutFinding = false;

				int batchSize = CurrentMotionGroup.FramesPerThread;
				m_LogicLayer.SingleAnimationStateJob.BatchSize = batchSize;
				m_LogicLayer.SingleAnimationStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
				m_LogicLayer.SingleAnimationStateJob.CurrentPose = m_LogicLayer.CurrentPose;
				m_LogicLayer.SingleAnimationStateJob.SectionMask = m_CurrentSectionMask;
				m_LogicLayer.SingleAnimationStateJob.Frames = CurrentMotionGroup.Frames;
				m_LogicLayer.SingleAnimationStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
				m_LogicLayer.SingleAnimationStateJob.Bones = CurrentMotionGroup.Bones;
				m_LogicLayer.SingleAnimationStateJob.Outputs = m_LogicLayer.JobsOutput;

				switch (Features.AnimationFindingType)
				{
					case SingleAnimationFindingType.FindInAll:
						{
							m_LogicLayer.SingleAnimationStateJob.FindOnlyInSelectedData = false;
						}
						break;
					case SingleAnimationFindingType.FindInSpecificAnimation:
						{
#if UNITY_EDITOR
							if (CurrentMotionGroup.MotionDataInfos == null ||
								Features.AnimationIndexToFind >= CurrentMotionGroup.MotionDataInfos.Count)
							{
								throw new System.Exception($"In single animation state \"{m_DataState.Name}\" of motion matching animator \"{motionMatching.motionMatchingController.name}\" on game object \"{transform.gameObject.name}\" index of selected animation to find is out of range of animation data in motion group of this state!");
							}
#endif

							MotionMatchingDataInfo infoToFind = CurrentMotionGroup.MotionDataInfos[Features.AnimationIndexToFind];

							m_LogicLayer.SingleAnimationStateJob.FindOnlyInSelectedData = true;
							m_LogicLayer.SingleAnimationStateJob.StartFindingFrameIndex = infoToFind.StartFrameDataIndex;
							m_LogicLayer.SingleAnimationStateJob.FramesFindingCount = infoToFind.FrameDataCount;
						}
						break;
				}


				m_LogicLayer.SingleAnimationJobHandle = m_LogicLayer.SingleAnimationStateJob.ScheduleBatch(
					CurrentMotionGroup.Frames.Length,
					batchSize,
					m_LogicLayer.poseCalculationJobHandle
					);
			}
			else
			{
				shouldPlayAnimationWithoutFinding = true;
			}
		}

		public override void Exit()
		{
			base.Exit();


		}

		public override void Awake()
		{
			base.Awake();
		}

		public override void Start()
		{
			base.Start();
			isFindingRunning = false;
			IsStateAnimationPlaying = true;
		}

		public override void FixedUpdate()
		{
			if (!isFindingRunning)
			{
				base.FixedUpdate();
			}
		}

		public override void Update()
		{
			if (isFindingRunning)
			{
				if (shouldPlayAnimationWithoutFinding)
				{
					PlayAnimationWithoutFinding();
				}
				else
				{
					CompleteSingleAnimationJob();
				}
			}
			else
			{
				base.Update();
				if (!CurrentClipInfo.IsLooping && StateMixer.GetLastAnimationTime() > CurrentClipInfo.Length)
				{
					m_CurrentClipGlobalTime = CurrentClipInfo.Length;
					m_CurrentClipLocalTime = CurrentClipInfo.Length;
				}
				else
				{
					m_CurrentClipGlobalTime = StateMixer.GetLastAnimationTime();
					m_CurrentClipLocalTime = m_CurrentClipGlobalTime % CurrentClipInfo.Length;
				}

				float blendTime = Features.blendTime;
				if (m_CurrentClipLocalTime > (CurrentClipInfo.Length - blendTime - Time.deltaTime))
				{
					switch (Features.updateType)
					{
						case SingleAnimationUpdateType.PlayInSequence:
							PlayInSequenceUpdate(blendTime);
							break;
						case SingleAnimationUpdateType.PlayRandom:
							PlayRandomUpdate(blendTime);
							break;
					}
				}
			}
		}

		public override void LateUpdate()
		{
			if (!isFindingRunning)
			{
				base.LateUpdate();
			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		internal override void CompleteScheduledJobs()
		{
			m_LogicLayer.SingleAnimationJobHandle.Complete();
		}

		private void CompleteSingleAnimationJob()
		{
			//if (!logicLayer.SingleAnimationJobHandle.IsCompleted)
			//{
			//	return;
			//}
			isFindingRunning = false;

			m_LogicLayer.SingleAnimationJobHandle.Complete();

			MotionMatchingJobOutput output = JoinJobsOutput();

			PlayAnimation(output, transitionBlendTime, 0f);
			IsStateAnimationPlaying = true;

			for (int i = 0; i < Behaviors.Count; i++)
			{
				Behaviors[i].OnCompleteEnterFindingJob();
			}
		}

		private void PlayInSequenceUpdate(float blendTime)
		{
			m_CurrentClipIndex = (m_CurrentClipIndex + 1) % CurrentMotionGroup.MotionDataInfos.Count;

			PlayAnimation(m_CurrentClipIndex, 0f, Features.blendTime, 0f);
			m_CurrentClipGlobalTime = 0f;
			m_CurrentClipLocalTime = 0f;
		}

		private void PlayRandomUpdate(float blendTime)
		{

			if (Features.CanBlendToTheSameAnimation)
			{
				m_CurrentClipIndex = UnityEngine.Random.Range(0, CurrentMotionGroup.MotionDataInfos.Count);
			}
			else
			{
				int newClipIndex = UnityEngine.Random.Range(0, CurrentMotionGroup.MotionDataInfos.Count);
				while (newClipIndex == m_CurrentClipIndex)
				{
					newClipIndex = UnityEngine.Random.Range(0, CurrentMotionGroup.MotionDataInfos.Count);
				}
				m_CurrentClipIndex = newClipIndex;

				PlayAnimation(m_CurrentClipIndex, 0f, Features.blendTime, 0f);
				m_CurrentClipGlobalTime = 0f;
				m_CurrentClipLocalTime = 0f;
			}
		}

		private void PlayAnimationWithoutFinding()
		{
			isFindingRunning = false;
			shouldPlayAnimationWithoutFinding = false;

			MotionMatchingJobOutput output = new MotionMatchingJobOutput();

			int desiredSectionIndex = 0;
			int sectionMask = m_CurrentSectionMask;

			if (sectionMask != 0)
			{
				while (sectionMask != 1)
				{
					desiredSectionIndex += 1;
					sectionMask = sectionMask >> 1;
				}
			}

			switch (Features.AnimationFindingType)
			{
				case SingleAnimationFindingType.FindInAll:
					{
						for (int i = 0; i < CurrentMotionGroup.MotionDataInfos.Count; i++)
						{
							if (CurrentMotionGroup.MotionDataInfos[i].Sections[desiredSectionIndex].timeIntervals.Count > 0)
							{
								output.FrameClipIndex = i;
								output.FrameTime = CurrentMotionGroup.MotionDataInfos[i].Sections[desiredSectionIndex].timeIntervals[0].x;
								break;
							}
						}
					}
					break;
				case SingleAnimationFindingType.FindInSpecificAnimation:
					{
						int dataIndex = Features.AnimationIndexToFind;
						output.FrameClipIndex = dataIndex;

						MotionMatchingDataInfo info = CurrentMotionGroup.MotionDataInfos[dataIndex];

						if (info.Sections[desiredSectionIndex].timeIntervals.Count > 0)
						{
							output.FrameTime = info.Sections[desiredSectionIndex].timeIntervals[0].x;
						}
						else
						{
							output.FrameTime = 0f;
						}
					}
					break;
			}

			PlayAnimation(output, transitionBlendTime, 0f);

			for (int i = 0; i < Behaviors.Count; i++)
			{
				Behaviors[i].OnCompleteEnterFindingJob();
			}
		}

		public override void RunTestJob()
		{
			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.SingleAnimationStateJob.BatchSize = batchSize;
			m_LogicLayer.SingleAnimationStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.SingleAnimationStateJob.CurrentPose = m_LogicLayer.CurrentPose;
			m_LogicLayer.SingleAnimationStateJob.SectionMask = m_CurrentSectionMask;
			m_LogicLayer.SingleAnimationStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.SingleAnimationStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.SingleAnimationStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.SingleAnimationStateJob.Outputs = m_LogicLayer.JobsOutput;

			m_LogicLayer.SingleAnimationJobHandle = m_LogicLayer.SingleAnimationStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize
				);
		}

		public override bool ShouldPerformMotionMatchingLooking()
		{
			return false;
		}

		public override void OnReEnter(SwitchStateInfo info)
		{
			isSectionMaskChanged = false;
			m_CurrentSectionMask = info.SectionMask;

			isFindingRunning = true;
			transitionBlendTime = info.BlendTime;

			motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
			m_LogicLayer.PrepareAndStartPoseCalculationJob();

			shouldPlayAnimationWithoutFinding = false;

			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.SingleAnimationStateJob.BatchSize = batchSize;
			m_LogicLayer.SingleAnimationStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.SingleAnimationStateJob.CurrentPose = m_LogicLayer.CurrentPose;
			m_LogicLayer.SingleAnimationStateJob.SectionMask = m_CurrentSectionMask;
			m_LogicLayer.SingleAnimationStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.SingleAnimationStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.SingleAnimationStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.SingleAnimationStateJob.Outputs = m_LogicLayer.JobsOutput;

			switch (Features.AnimationFindingType)
			{
				case SingleAnimationFindingType.FindInAll:
					{
						m_LogicLayer.SingleAnimationStateJob.FindOnlyInSelectedData = false;
					}
					break;
				case SingleAnimationFindingType.FindInSpecificAnimation:
					{
#if UNITY_EDITOR
						if (CurrentMotionGroup.MotionDataInfos == null ||
							Features.AnimationIndexToFind >= CurrentMotionGroup.MotionDataInfos.Count)
						{
							throw new System.Exception($"In single animation state \"{m_DataState.Name}\" of motion matching animator \"{motionMatching.motionMatchingController.name}\" on game object \"{transform.gameObject.name}\" index of selected animation to find is out of range of animation data in motion group of this state!");
						}
#endif

						MotionMatchingDataInfo infoToFind = CurrentMotionGroup.MotionDataInfos[Features.AnimationIndexToFind];

						m_LogicLayer.SingleAnimationStateJob.FindOnlyInSelectedData = true;
						m_LogicLayer.SingleAnimationStateJob.StartFindingFrameIndex = infoToFind.StartFrameDataIndex;
						m_LogicLayer.SingleAnimationStateJob.FramesFindingCount = infoToFind.FrameDataCount;
					}
					break;
			}


			m_LogicLayer.SingleAnimationJobHandle = m_LogicLayer.SingleAnimationStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize,
				m_LogicLayer.poseCalculationJobHandle
				);
		}
	}
}
