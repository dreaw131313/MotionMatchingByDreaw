using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace MotionMatching.Gameplay.Jobs
{
	[BurstCompile]
	public struct TransformTrajectoryToLocalSpaceJob : IJobParallelForBatch
	{
		// Input
		[ReadOnly]
		public quaternion Rotation;
		[ReadOnly]
		public float3 Position;
		[ReadOnly]
		public NativeArray<TrajectoryPoint> GlobalSpaceTrajectory;
		public float MotionGroupTrajectoryWeight;
		public float MotionGroupTrajectoryPositionWeight;
		public float MotionGroupTrajectoryVelocityWeight;
		public float MotionGroupTrajectoryOrientationWeight;

		// output
		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public NativeArray<TrajectoryPoint> LocalSpaceTrajectory;

		public void Execute(int startIndex, int count)
		{
			float4x4 matrix = math.inverse(new float4x4(Rotation, Position));

			for (int i = startIndex; i < count; i++)
			{
				TrajectoryPoint point = GlobalSpaceTrajectory[i];

				point.Position = math.transform(matrix, point.Position) * (MotionGroupTrajectoryPositionWeight * MotionGroupTrajectoryWeight);
				point.Velocity = math.mul(math.inverse(Rotation), point.Velocity) * (MotionGroupTrajectoryVelocityWeight * MotionGroupTrajectoryWeight);
				point.Orientation = math.mul(math.inverse(Rotation), point.Orientation) * (MotionGroupTrajectoryOrientationWeight * MotionGroupTrajectoryWeight);

				LocalSpaceTrajectory[i] = point;
			}

		}
	}
}