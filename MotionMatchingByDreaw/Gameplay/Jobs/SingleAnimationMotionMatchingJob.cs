using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay.Jobs
{
	//[BurstCompile(CompileSynchronously = true)]
	[BurstCompile]
	public struct SingleAnimationMotionMatchingJob : IJobParallelForBatch
	{
		[ReadOnly]
		public int BatchSize;

		[ReadOnly]
		public NativeArray<TrajectoryPoint> CurrentTrajectory;
		[ReadOnly]
		public NativeArray<BoneData> CurrentPose;
		[ReadOnly]
		public int SectionMask;

		[ReadOnly]
		public NativeArray<FrameDataInfo> Frames;
		[ReadOnly]
		public NativeArray<TrajectoryPoint> TrajectoryPoints;
		[ReadOnly]
		public NativeArray<BoneData> Bones;

		public bool FindOnlyInSelectedData;
		public int StartFindingFrameIndex;
		public int FramesFindingCount;


		[WriteOnly]
		[NativeDisableParallelForRestriction]
		public NativeArray<MotionMatchingJobOutput> Outputs;

		public void Execute(int startIndex, int count)
		{
			if (FindOnlyInSelectedData)
			{
				FindInSelectedData(startIndex, count);
			}
			else
			{
				FindInAllClipsData(startIndex, count);
			}
		}

		public void FindInAllClipsData(int startIndex, int count)
		{
			int trajectoryCount = CurrentTrajectory.Length;
			int poseCount = CurrentPose.Length;

			int frameCount = startIndex + count;

			float bestCost = float.MaxValue;
			int bestFrameIndex = startIndex;


			for (int frameIndex = startIndex; frameIndex < frameCount; frameIndex++)
			{
				if ((Frames[frameIndex].sections.sections & SectionMask) != SectionMask)
				{
					continue;
				}

				int tpStartIndex = frameIndex * trajectoryCount;
				float trajectoryCost = 0f;

				for (int tpIndex = 0; tpIndex < trajectoryCount; tpIndex++)
				{

					TrajectoryPoint TP = CurrentTrajectory[tpIndex];
					TrajectoryPoint dataTP = TrajectoryPoints[tpStartIndex + tpIndex];

					float3 posDelta = TP.Position - dataTP.Position;
					float3 velDelta = TP.Velocity - dataTP.Velocity;
					float3 orientDelta = TP.Orientation - dataTP.Orientation;

					trajectoryCost += (math.lengthsq(posDelta) + math.lengthsq(velDelta) + math.lengthsq(orientDelta));

					//trajectoryCost += CurrentTrajectory[tpIndex].CalculateCost(TrajectoryPoints[tpStartIndex + tpIndex]);
				}

				int boneStartIndex = frameIndex * poseCount;
				float poseCost = 0f;

				for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
				{
					BoneData bone = CurrentPose[boneIndex];
					BoneData dataBone = Bones[boneStartIndex + boneIndex];

					float3 posDelta = bone.localPosition - dataBone.localPosition;
					float3 velDelta = bone.velocity - dataBone.velocity;
					poseCost += (math.lengthsq(posDelta) + math.lengthsq(velDelta));

					//poseCost += CurrentPose[boneIndex].CalculateCost(Bones[boneStartIndex + boneIndex]);
				}

				float finalCost = trajectoryCost + poseCost;

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

		public void FindInSelectedData(int startIndex, int count)
		{
			int jobLastFindingFrameIndex = startIndex + count;
			int lastFindingFrameIndex = StartFindingFrameIndex + FramesFindingCount;

			if (startIndex <= StartFindingFrameIndex && StartFindingFrameIndex < jobLastFindingFrameIndex ||
				StartFindingFrameIndex < startIndex && startIndex < lastFindingFrameIndex)
			{
				int trajectoryCount = CurrentTrajectory.Length;
				int poseCount = CurrentPose.Length;



				int selectedDataStartFrameIndex = math.clamp(
					math.max(StartFindingFrameIndex, startIndex),
					StartFindingFrameIndex,
					lastFindingFrameIndex
					);

				int frameCount = math.min(jobLastFindingFrameIndex, lastFindingFrameIndex);

				float bestCost = float.MaxValue;
				int bestFrameIndex = selectedDataStartFrameIndex;


				for (int frameIndex = selectedDataStartFrameIndex; frameIndex < frameCount; frameIndex++)
				{
					if ((Frames[frameIndex].sections.sections & SectionMask) != SectionMask)
					{
						continue;
					}

					int tpStartIndex = frameIndex * trajectoryCount;
					float trajectoryCost = 0f;

					for (int tpIndex = 0; tpIndex < trajectoryCount; tpIndex++)
					{
						trajectoryCost += CurrentTrajectory[tpIndex].CalculateCost(TrajectoryPoints[tpStartIndex + tpIndex]);
					}

					int boneStartIndex = frameIndex * poseCount;
					float poseCost = 0f;

					for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
					{
						poseCost += CurrentPose[boneIndex].CalculateCost(Bones[boneStartIndex + boneIndex]);
					}

					float finalCost = trajectoryCost + poseCost;

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
			else
			{
				int outputIndex = startIndex / BatchSize;
				MotionMatchingJobOutput output;
				output.FrameCost = float.MaxValue;
				output.FrameTime = 0;
				output.FrameClipIndex = Frames[StartFindingFrameIndex].clipIndex;
				Outputs[outputIndex] = output;
			}
		}
	}

}
