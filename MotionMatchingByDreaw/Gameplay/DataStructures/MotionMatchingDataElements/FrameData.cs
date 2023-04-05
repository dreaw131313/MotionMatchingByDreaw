using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public struct FrameData
	{
		//[SerializeField]
		//public int frameNumber;
		[SerializeField]
		public Trajectory trajectory;
		[SerializeField]
		public PoseData pose;
		[SerializeField]
		public float localTime;
		[SerializeField]
		public FrameSections sections;
		[SerializeField]
		public FrameContact[] contactPoints;
		[SerializeField]
		public Vector3 Velocity;

		public FrameData(
			int frameNumber,
			float localTime,
			Trajectory trajectory,
			PoseData pose,
			FrameSections sections,
			Vector3 velocity
			)
		{
			//this.frameNumber = frameNumber;
			this.trajectory = trajectory;
			this.pose = pose;
			this.localTime = localTime;
			this.sections = sections;
			this.contactPoints = new FrameContact[0];
			this.Velocity = velocity;

		}

		public FrameData(
			int frameNumber,
			float localTime,
			FrameSections sections
			)
		{
			//this.frameNumber = frameNumber;
			this.localTime = localTime;
			this.sections = sections;
			this.trajectory = new Trajectory();
			this.pose = new PoseData();
			this.contactPoints = new FrameContact[0];
			this.Velocity = Vector3.zero;
		}

		public void SetTrajectory(Trajectory newTrajectory)
		{
			this.trajectory = newTrajectory;
		}

		public void SetPose(PoseData newPose)
		{
			this.pose = newPose;
		}

		public static void GetLerpedTrajectory(ref Trajectory buffor, FrameData first, FrameData second, float factor)
		{
			Trajectory.Lerp(ref buffor, first.trajectory, second.trajectory, factor);
		}

		public static void GetLerpedPose(ref PoseData buffor, FrameData first, FrameData second, float factor)
		{
			PoseData.Lerp(ref buffor, first.pose, second.pose, factor);
		}

		public float CaculateCost(FrameData toFrame, float responsivity = 1f)
		{
			float cost = 0f;
			cost += responsivity * this.trajectory.CalculateCost(toFrame.trajectory);
			cost += this.pose.CalculateCost(toFrame.pose);
			return cost;
		}

		void SetVelocity(Vector3 newVelocity)
		{
			Velocity = newVelocity;
		}
	}

	[System.Serializable]
	[BurstCompile]
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

		public FrameDataInfo(int clipIndex, float localTime, FrameSections sections, bool neverChecking)
		{
			this.clipIndex = clipIndex;
			this.localTime = localTime;
			this.sections = sections;
			this.NeverChecking = neverChecking;
		}
	}
}