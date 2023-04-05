using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace MotionMatching.Gameplay.Jobs
{
	[BurstCompile(DisableSafetyChecks = true)]
	public struct MotionMatchingJob : IJobParallelForBatch
	{
		[ReadOnly]
		public int BatchSize;
		// Changeable data input
		[ReadOnly]
		public int SectionMask;
		[ReadOnly]
		public int CurrentStateIndex;
		[ReadOnly]
		public NativeArray<TrajectoryPoint> CurrentTrajectory;
		[ReadOnly]
		public NativeArray<BoneData> CurrentPose;

		[ReadOnly]
		public NativeList<BlendedAnimationData> CurrentPlayingClips;
		[ReadOnly]
		public NativeArray<SectionInfo> sectionDependecies;

		// Constant data Input
		[ReadOnly]
		public NativeArray<FrameDataInfo> Frames;
		[ReadOnly]
		public NativeArray<TrajectoryPoint> TrajectoryPoints;
		[ReadOnly]
		public NativeArray<BoneData> Bones;

		// Output
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

			int sectionDependeciesCount = sectionDependecies.Length;
			int currentPlayingClipsCount = CurrentPlayingClips.Length;

			for (int frameIndex = startIndex; frameIndex < frameCount; frameIndex++)
			{
				FrameDataInfo frameData = Frames[frameIndex];

				if (frameData.NeverChecking || (Frames[frameIndex].sections.sections & SectionMask) != SectionMask)
				{
					continue;
				}

				bool skipFrame = false;
				for (int i = 0; i < currentPlayingClipsCount; i++)
				{
					BlendedAnimationData blendedData = CurrentPlayingClips[i];

					if (CurrentStateIndex == blendedData.StateIndex &&
						frameData.clipIndex == blendedData.ClipIndex &&
						!blendedData.FindInYourself)
					{
						skipFrame = true;
						break;
					}
				}

				if (skipFrame)
				{
					continue;
				}

				float sectionDependeciesWeight = 1f;
				for (int sectionInfoIndex = 0; sectionInfoIndex < sectionDependeciesCount; sectionInfoIndex++)
				{
					SectionInfo sectionInfo = sectionDependecies[sectionInfoIndex];
					if (frameData.sections.GetSection(sectionInfo.sectionIndex))
					{
						sectionDependeciesWeight *= sectionInfo.sectionWeight;
					}
				}

				int tpStartIndex = frameIndex * trajectoryCount;
				float trajectoryCost = 0f;

				for (int tpIndex = 0; tpIndex < trajectoryCount; tpIndex++)
				{
					TrajectoryPoint TP = CurrentTrajectory[tpIndex];
					TrajectoryPoint dataTP = TrajectoryPoints[tpStartIndex + tpIndex];

					//float3 posDelta = TP.Position - dataTP.Position;
					//float3 velDelta = TP.Velocity - dataTP.Velocity;
					//float3 orientDelta = TP.Orientation - dataTP.Orientation;

					trajectoryCost +=
						math.lengthsq(TP.Position - dataTP.Position) +
						math.lengthsq(TP.Velocity - dataTP.Velocity) +
						math.lengthsq(TP.Orientation - dataTP.Orientation);
				}

				int boneStartIndex = frameIndex * poseCount;
				float poseCost = 0f;

				for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
				{
					BoneData bone = CurrentPose[boneIndex];
					BoneData dataBone = Bones[boneStartIndex + boneIndex];

					//float3 posDelta = bone.localPosition - dataBone.localPosition;
					//float3 velDelta = bone.velocity - dataBone.velocity;
					poseCost +=
						math.lengthsq(bone.localPosition - dataBone.localPosition) +
						math.lengthsq(bone.velocity - dataBone.velocity);
				}

				float finalCost = sectionDependeciesWeight * (trajectoryCost + poseCost);

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