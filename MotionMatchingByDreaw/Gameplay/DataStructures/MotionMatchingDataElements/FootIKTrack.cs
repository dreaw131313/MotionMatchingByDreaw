using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class FootIKTrack
	{
		[SerializeField]
		public float2[] PerformIKIntervals; // x - start interval time, y - end interval time

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public bool IsInPerfomrIKTrack(float inTime)
		{

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public float GetTimeToClosestStartInterval(float fromTime)
		{

			return 0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public float2 GetClossestNextInterval(float fromTime)
		{

			return float2.zero;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public float2 GetClossestPreviousInterval(float fromTime)
		{

			return float2.zero;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public IKIntervalInfo GetNextIntervalInfo(float fromTime)
		{
			return new IKIntervalInfo();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inTime">Local animation time.</param>
		/// <returns></returns>
		public IKIntervalInfo GetPreviousIntervalInfo(float fromTime)
		{
			return new IKIntervalInfo();
		}

	}

	public struct IKIntervalInfo
	{
		public bool IsInInterval; // if true TimeToInterval is 0f
		public float TimeToInterval;
		public float2 IntervalTime; // x - start interval time, y - end interval time
	}


}