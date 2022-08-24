using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MotionMatching.Gameplay.Jobs
{
	[BurstCompile]
	public struct ContactMotionMatchingJob : IJobParallelForBatch
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
		public NativeList<FrameContact> CurrentContactPoints;
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


			for (int frameIndex = startIndex; frameIndex < frameCount; frameIndex++)
			{
				bool shouldBeSkipped = false;

				for (int i = 0; i < RecentlyPlayedClipsIndexes.Length; i++)
				{
					if (RecentlyPlayedClipsIndexes[i] == Frames[frameIndex].clipIndex)
					{
						shouldBeSkipped = true;
						break;
					}
				}

				if (shouldBeSkipped)
				{
					continue;
				}


				if ((Frames[frameIndex].sections.sections & SectionMask) != SectionMask)
				{
					continue;
				}

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

				// Contact cost

				int contactStartIndex = frameIndex * ContactsCount;
				float contactsCost = 0f;
				for (int contactIndex = 0; contactIndex < ContactsCount; contactIndex++)
				{
					contactsCost += CurrentContactPoints[contactIndex].CalculateCost(Contacts[contactStartIndex + contactIndex]);
				}

				float finalCost = trajectoryCost + poseCost + contactsCost;

				if (finalCost < bestCost)
				{
					bestCost = finalCost;
					bestFrameIndex = frameIndex;
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