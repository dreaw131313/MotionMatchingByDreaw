using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

namespace MotionMatching.Gameplay.Jobs
{
	public struct MotionMatchingJobOutput
	{
		public int FrameClipIndex;
		public float FrameTime;
		public float FrameCost;

		public MotionMatchingJobOutput(int clipIndex, float frameTime, float frameCost)
		{
			FrameClipIndex = clipIndex;
			FrameTime = frameTime;
			FrameCost = frameCost;
		}
	}

	public struct CurrentPlayingClipInfo
	{
		public int clipIndex;
		public int groupIndex;
		public bool notFindInYourself;

		public CurrentPlayingClipInfo(int clipIndex, int groupIndex, bool notFindInYourself)
		{
			this.clipIndex = clipIndex;
			this.notFindInYourself = notFindInYourself;
			this.groupIndex = groupIndex;
		}
	}
}