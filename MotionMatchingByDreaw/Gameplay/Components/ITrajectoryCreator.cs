using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public interface ITrajectoryCreator
	{
		/// <summary>
		/// This function is called by Motion Matching component just before finding new best place in animations is started.
		/// </summary>
		/// <param name="trajectoryInWorldSpace">A trajectory that must be filled with trajectory points created by ITrajectoryCreator.</param>
		/// <param name="trajectoryCount">The number of trajectory points requested by Motion Matching component.</param>
		void GetTrajectoryToMotionMatchingComponent(ref NativeArray<TrajectoryPoint> trajectoryInWorldSpace, int trajectoryCount);

		TrajectoryPoint GetTrajectoryPoint(int index);

		void InitializeTrajectoryCreator(MotionMatchingComponent mmc);

		void SetTrajectoryFromNativeContainer(ref NativeArray<TrajectoryPoint> traj, Transform inSpace = null);

		void SetPastAnimationTrajectoryFromMotionMatchingComponent(ref NativeArray<TrajectoryPoint> trajectory, int firstIndexWithFutureTime);

		Vector3 GetTrajectoryMakerPosition();

		Vector3 GetVelocity();
	}
}