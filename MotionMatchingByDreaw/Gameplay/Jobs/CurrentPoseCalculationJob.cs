using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay.Jobs
{
	[BurstCompile]
	public struct CurrentPoseCalculationJob : IJob
	{
		[ReadOnly]
		public int PoseBonesCount;
		[ReadOnly]
		public NativeList<PoseCalculationClipInfo> ClipsInfos;
		[ReadOnly]
		public NativeArray<BoneData> Bones;

		// old motion group
		[ReadOnly]
		public float OldPoseWeight;
		[ReadOnly]
		public NativeArray<float2> OldBonesWeights;

		// new motion group
		[ReadOnly]
		public float NewPoseWeight;
		[ReadOnly]
		public NativeArray<float2> NewBonesWeights;


		//Output
		[WriteOnly]
		[NativeDisableParallelForRestriction]
		public NativeArray<BoneData> OutputPose;

		public void Execute()
		{
			int lastBoneIndex = OutputPose.Length;

			float animationWeightSum = 0f;

			for (int clipIndex = 0; clipIndex < ClipsInfos.Length; clipIndex++)
			{
				animationWeightSum += ClipsInfos[clipIndex].Weight;
			}

			for (int boneIndex = 0; boneIndex < lastBoneIndex; boneIndex++)
			{
				BoneData evaluatedBone = new BoneData(float3.zero, float3.zero);

				for (int clipIndex = 0; clipIndex < ClipsInfos.Length; clipIndex++)
				{
					PoseCalculationClipInfo clipInfo = ClipsInfos[clipIndex];
					BoneData clipBone = GetLerpedBoneData(ClipsInfos[clipIndex], boneIndex);
					evaluatedBone += clipBone * clipInfo.Weight / animationWeightSum;
				}

				float2 oldBoneWeight = OldBonesWeights[boneIndex];
				float2 newBoneWeight = NewBonesWeights[boneIndex];

				float positionFactor = (1f / OldPoseWeight / oldBoneWeight.x) * NewPoseWeight * newBoneWeight.x;
				float velocityFactor = (1f / OldPoseWeight / oldBoneWeight.y) * NewPoseWeight * newBoneWeight.y;

				evaluatedBone.localPosition = evaluatedBone.localPosition * positionFactor;
				evaluatedBone.velocity = evaluatedBone.velocity * velocityFactor;

				OutputPose[boneIndex] = evaluatedBone;
			}


		}

		public BoneData GetLerpedBoneData(PoseCalculationClipInfo clipInfo, int poseBoneIndex)
		{
			int localFrameIndex = (int)math.floor(clipInfo.CurrentTime / clipInfo.FrameTime);
			int lastFrameIndex = clipInfo.FramesCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int boneIndex = clipInfo.StartBoneIndex + lastFrameIndex * PoseBonesCount + poseBoneIndex;
				return Bones[boneIndex];
			}
			else
			{
				int firstBoneStartIndex = clipInfo.StartBoneIndex + localFrameIndex * PoseBonesCount;
				int secondBoneStartIndex = firstBoneStartIndex + PoseBonesCount;
				float factor = (clipInfo.CurrentTime - localFrameIndex * clipInfo.FrameTime) / clipInfo.FrameTime;

				return BoneData.Lerp(
						Bones[firstBoneStartIndex + poseBoneIndex],
						Bones[secondBoneStartIndex + poseBoneIndex],
						factor
						);
			}
		}
	}

	public struct PoseCalculationClipInfo
	{
		public float CurrentTime;
		public int StartBoneIndex;
		public float Weight;
		public float FrameTime;
		public int FramesCount;
	}

}