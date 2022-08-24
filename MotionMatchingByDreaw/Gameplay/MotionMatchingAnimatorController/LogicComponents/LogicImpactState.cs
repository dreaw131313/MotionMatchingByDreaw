using MotionMatching.Gameplay.Jobs;
#if UNITY_EDITOR
using MotionMatching.Tools;
#endif
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public class LogicImpactState : LogicState
	{
		private bool isJobRunning;
		private float blendTime;


		public ContactStateFeatures Features;

		NativeList<int> recentlyPlayedClipsIndexes;
		float lastEnterToStateTime = 0;

		public LogicImpactState(
			ContactState_SO state,
			MotionMatchingComponent component,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			) :
			base(state, component, logicLayer, animationSystem)
		{
			Features = state.Features;

			if (Features.NotSearchInRecentClips)
			{
				recentlyPlayedClipsIndexes = new NativeList<int>(Features.RemeberedRecentlyPlayedClipsCount, Allocator.Persistent);
			}
			else
			{
				recentlyPlayedClipsIndexes = new NativeList<int>(0, Allocator.Persistent);
			}
		}

		public override void Enter(SwitchStateInfo switchStateInfo)
		{
			base.Enter(switchStateInfo);
			UpdateSectionMask();

			m_CurrentClipGlobalTime = 0;
			m_CurrentClipLocalTime = 0;

			StartLookginJob(switchStateInfo);
		}

		public override void Exit()
		{
			base.Exit();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			if (recentlyPlayedClipsIndexes.IsCreated)
			{
				recentlyPlayedClipsIndexes.Dispose();
			}
		}

		public override void Update()
		{
			base.Update();

			if (isJobRunning)
			{
				CompleteImpactJob();
				isJobRunning = false;
			}
		}

		public override void LateUpdate()
		{
			base.LateUpdate();

			if (m_IsPlayingCurrentStateAnimation)
			{
				m_CurrentClipGlobalTime = StateMixer.GetLastAnimationTime();
				if (m_CurrentClipGlobalTime > CurrentClipInfo.Length)
				{
					m_CurrentClipLocalTime = CurrentClipInfo.Length;
				}
				else
				{
					m_CurrentClipLocalTime = m_CurrentClipGlobalTime % CurrentClipInfo.Length;
				}
			}
		}

		public override void RunTestJob()
		{

		}

		public override bool ShouldPerformMotionMatchingLooking()
		{
			return false;
		}

		internal override void CompleteScheduledJobs()
		{
			m_LogicLayer.ImpactJobHandle.Complete();
		}

		public override void OnReEnter(SwitchStateInfo info)
		{
			isSectionMaskChanged = false;
			m_CurrentSectionMask = info.SectionMask;

			StartLookginJob(info);
		}

		private void StartLookginJob(SwitchStateInfo switchStateInfo)
		{
			isJobRunning = true;
			blendTime = switchStateInfo.BlendTime;

			motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
			m_LogicLayer.PrepareAndStartPoseCalculationJob();

			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.ImpactStateJob.BatchSize = batchSize;
			m_LogicLayer.ImpactStateJob.ContactsCount = this.CurrentMotionGroup.MotionDataInfos[0].ContactPoints.Count;
			m_LogicLayer.ImpactStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.ImpactStateJob.CurrentPose = m_LogicLayer.CurrentPose;

			m_LogicLayer.MultiplayContactsByWeightFromNativeMotionGroup(CurrentMotionGroup.ContactsCostWeight);
			m_LogicLayer.ImpactStateJob.DesiredImpact = m_LogicLayer.localSpaceContacts[0];
			m_LogicLayer.ImpactStateJob.SectionMask = m_CurrentSectionMask;


			if (!Features.NotSearchInRecentClips)
			{
				recentlyPlayedClipsIndexes.Clear();
			}
			else
			{
				float deltaTimeFromLastEnterToState = Time.time - lastEnterToStateTime;
				if (deltaTimeFromLastEnterToState >= Features.TimeToResetRecentlyPlayedClips)
				{
					lastEnterToStateTime = Time.time;
					recentlyPlayedClipsIndexes.Clear();
				}
			}

			m_LogicLayer.ImpactStateJob.RecentlyPlayedClipsIndexes = this.recentlyPlayedClipsIndexes;


			m_LogicLayer.ImpactStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.ImpactStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.ImpactStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.ImpactStateJob.Contacts = CurrentMotionGroup.Contacts;
			m_LogicLayer.ImpactStateJob.Outputs = m_LogicLayer.JobsOutput;


			m_LogicLayer.ImpactJobHandle = m_LogicLayer.ImpactStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize,
				m_LogicLayer.poseCalculationJobHandle
				);
		}

		private void CompleteImpactJob()
		{
			m_LogicLayer.ImpactJobHandle.Complete();

			MotionMatchingJobOutput output = JoinJobsOutput();
			PlayAnimation(output, blendTime, 0f);
			HandleRecentlyPlayedClips(output.FrameClipIndex);

			for (int i = 0; i < Behaviors.Count; i++)
			{
				Behaviors[i].OnCompleteEnterFindingJob();
			}

		}

		private void HandleRecentlyPlayedClips(int currentPlayedAnimationIndex)
		{
			if (recentlyPlayedClipsIndexes.Length >= Features.RemeberedRecentlyPlayedClipsCount)
			{
				while (recentlyPlayedClipsIndexes.Length >= Features.RemeberedRecentlyPlayedClipsCount && recentlyPlayedClipsIndexes.Length > 0)
				{
					recentlyPlayedClipsIndexes.RemoveAt(0);
				}
			}

			recentlyPlayedClipsIndexes.Add(currentPlayedAnimationIndex);
		}


#if UNITY_EDITOR
		public override void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;

			foreach (SwitchStateContact contact in m_LogicLayer.globalSpaceContacts)
			{
				// seted contacts
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(contact.frameContact.position, 0.05f);
				Gizmos.color = Color.blue;
				MM_Gizmos.DrawArrow(
					contact.frameContact.position,
					contact.frameContact.position + contact.frameContact.normal,
					0.1f
					);
			}
		}
#endif
	}
}
