using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public struct BoneData
	{
		[SerializeField]
		public float3 localPosition;
		[SerializeField]
		public float3 velocity;

		public static BoneData operator +(BoneData x, BoneData y)
		{
			return new BoneData(x.localPosition + y.localPosition, x.velocity + y.velocity);
		}

		public static BoneData operator *(float x, BoneData y)
		{
			return new BoneData(x * y.localPosition, x * y.velocity);
		}

		public static BoneData operator *(BoneData y, float x)
		{
			return new BoneData(x * y.localPosition, x * y.velocity);
		}


		public static BoneData operator /(BoneData bone, float x)
		{
			return new BoneData(bone.localPosition / x, bone.velocity / x);
		}

		public BoneData(float3 localPosition, float3 velocity)
		{
			this.localPosition = localPosition;
			this.velocity = velocity;
		}

		public static BoneData Lerp(BoneData bone1, BoneData bone2, float factor)
		{
			return new BoneData(
				math.lerp(bone1.localPosition, bone2.localPosition, factor),
				math.lerp(bone1.velocity, bone2.velocity, factor)
				);
		}

		public void Set(BoneData bone)
		{
			this.localPosition = bone.localPosition;
			this.velocity = bone.velocity;
		}

		public void Set(float3 pos, float3 vel)
		{
			this.localPosition = pos;
			this.velocity = vel;
		}

		public static float3 CalculateVelocity(float3 firstPos, float3 nextPos, float frameTime)
		{
			float3 vel = float3.zero;
			float3 deltaPosition = nextPos - firstPos;
			vel.x = deltaPosition.x / frameTime;
			vel.y = deltaPosition.y / frameTime;
			vel.z = deltaPosition.z / frameTime;
			return vel;
		}


		#region Cost calculation
		[BurstCompile]
		public float CalculateCost(BoneData toBone)
		{
			//	float cost = 0;
			//	cost += CalculatePositionCost(toBone);
			//	cost += CalculateVelocityCost(toBone);

			//float3 posDelta = math.abs(localPosition - toBone.localPosition);
			//float3 velDelta = math.abs(velocity - toBone.velocity);
			//float cost = posDelta.x + posDelta.y + velDelta.x + velDelta.y;

			float3 posDelta = localPosition - toBone.localPosition;
			float3 velDelta = velocity - toBone.velocity;
			return math.lengthsq(posDelta) + math.lengthsq(velDelta);
		}

		[BurstCompile]
		public float CalculatePositionCost(BoneData bone)
		{
			float cost = math.lengthsq(bone.localPosition - localPosition);
			return cost;
		}

		[BurstCompile]
		public float CalculateVelocityCost(BoneData bone)
		{
			float cost = math.lengthsq(bone.velocity - velocity);
			return cost;
		}
		#endregion

		public static int FromArray(out BoneData bone, float[] array, int startIndex = 0)
		{
			bone = new BoneData(
				new float3(array[startIndex + 0], array[startIndex + 1], array[startIndex + 2]),
				new float3(array[startIndex + 3], array[startIndex + 4], array[startIndex + 5])
				);
			return startIndex + 6;
		}

		public static int FromArray(out BoneData bone, List<float> array, int startIndex = 0)
		{
			bone = new BoneData(
				new float3(array[startIndex + 0], array[startIndex + 1], array[startIndex + 2]),
				new float3(array[startIndex + 3], array[startIndex + 4], array[startIndex + 5])
				);
			return startIndex + 6;
		}

		public float[] ToFloatArray()
		{
			float[] array = new float[6];
			array[0] = localPosition.x;
			array[1] = localPosition.y;
			array[2] = localPosition.z;
			array[3] = velocity.x;
			array[4] = velocity.y;
			array[5] = velocity.z;

			return array;
		}
	}
}