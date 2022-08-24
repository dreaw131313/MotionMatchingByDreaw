using MotionMatching.Gameplay.Jobs;
using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

namespace MotionMatching.Gameplay.Jobs
{
	public struct ImpactMotionMatchingJob : IJobParallelForBatch
	{
		[ReadOnly]
		public int BatchSize;
		[ReadOnly]
		public int ContactsCount;


		[ReadOnly]
		public NativeArray<TrajectoryPoint> CurrentTrajectory;
		[ReadOnly]
		public NativeArray<BoneData> CurrentPose;
		[ReadOnly]
		public FrameContact DesiredImpact;
		[ReadOnly]
		public int SectionMask;
		[ReadOnly]
		public NativeList<int> RecentlyPlayedClipsIndexes;

		[ReadOnly]
		public NativeArray<FrameDataInfo> Frames;
		[ReadOnly]
		public NativeArray<TrajectoryPoint> TrajectoryPoints;
		[ReadOnly]
		public NativeArray<BoneData> Bones;
		[ReadOnly]
		public NativeArray<FrameContact> Contacts;

		[WriteOnly]
		[NativeDisableParallelForRestriction]
		public NativeArray<MotionMatchingJobOutput> Outputs;

		public void Execute(int startIndex, int count)
		{
			int trajectoryCount = CurrentTrajectory.Length;
			int poseCount = CurrentPose.Length;

			int frameCount = startIndex + count;

			float bestCost = float.MaxValue;
			int bestFrameIndex = startIndex;

			int recentlyPlayedClipsCount = RecentlyPlayedClipsIndexes.Length;

			for (int frameIndex = startIndex; frameIndex < frameCount; frameIndex++)
			{
				bool shouldBeSkipped = false;

				for (int i = 0; i < recentlyPlayedClipsCount; i++)
				{
					if (RecentlyPlayedClipsIndexes[i] == Frames[frameIndex].clipIndex)
					{
						shouldBeSkipped = true;
						break;
					}
				}

				if (shouldBeSkipped || (Frames[frameIndex].sections.sections & SectionMask) != SectionMask) { continue; }

				// checking if exist impactContact in this frame
				bool notImpactsInThisFrame = true;
				int contactStartIndex = frameIndex * ContactsCount;
				for (int contactIndex = 0; contactIndex < ContactsCount; contactIndex++)
				{
					FrameContact currentContact = Contacts[contactStartIndex + contactIndex];
					if (currentContact.IsImpact)
					{
						notImpactsInThisFrame = false;
					}
				}

				if (notImpactsInThisFrame) continue;

				// trajectoryCost
				int tpStartIndex = frameIndex * trajectoryCount;
				float trajectoryCost = 0f;

				for (int tpIndex = 0; tpIndex < trajectoryCount; tpIndex++)
				{
					trajectoryCost += CurrentTrajectory[tpIndex].CalculateCost(TrajectoryPoints[tpStartIndex + tpIndex]);
				}


				// Pose cost
				int boneStartIndex = frameIndex * poseCount;
				float poseCost = 0f;

				for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
				{
					poseCost += CurrentPose[boneIndex].CalculateCost(Bones[boneStartIndex + boneIndex]);
				}

				for (int contactIndex = 0; contactIndex < ContactsCount; contactIndex++)
				{
					FrameContact currentContact = Contacts[contactStartIndex + contactIndex];
					if (currentContact.IsImpact)
					{
						float impactCost = currentContact.CalculateCost(DesiredImpact);
						float finalCost = trajectoryCost + poseCost + impactCost;

						if (finalCost < bestCost)
						{
							bestCost = finalCost;
							bestFrameIndex = frameIndex;
						}
					}
				}
			}

			int outputIndex = startIndex / BatchSize;

			MotionMatchingJobOutput output;
			output.FrameCost = bestCost;
			output.FrameTime = Frames[bestFrameIndex].localTime;
			output.FrameClipIndex = Frames[bestFrameIndex].clipIndex;
			Outputs[outputIndex] = output;
		}
	}
}

namespace TestMemoryAling
{
	public struct TrajectoryPoint
	{
		[SerializeField]
		public float3 Position;
		[SerializeField]
		public float3 Velocity;
		[SerializeField]
		public float3 Orientation;
	}

	public struct BoneData
	{
		[SerializeField]
		public float3 localPosition;
		[SerializeField]
		public float3 velocity;
	}

	public struct FrameDataInfo
	{
		[SerializeField]
		public int clipIndex;
		[SerializeField]
		public float localTime;
		[SerializeField]
		public FrameSections sections;
		[SerializeField]
		public bool NeverChecking;
	}
}
