using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class DataSection
	{
		[SerializeField]
		public List<float2> timeIntervals;
#if UNITY_EDITOR
		[SerializeField]
		public string sectionName;
		[SerializeField]
		public bool fold = false;
#endif

		public DataSection()
		{
			timeIntervals = new List<float2>();
#if UNITY_EDITOR
			sectionName = string.Empty;
#endif
		}

		public DataSection(string name)
		{
#if UNITY_EDITOR
			this.sectionName = name;
#endif
			timeIntervals = new List<float2>();
		}

		public bool SetTimeIntervalWithCheck(int index, float2 change)
		{
			float2 buffor = timeIntervals[index];

			timeIntervals[index] = change;

			if (timeIntervals[index].x == buffor.x && timeIntervals[index].y == buffor.y)
			{
				return false;
			}

			return true;
		}

		public void SetTimeInterval(int index, float2 interval)
		{
			timeIntervals[index] = interval;
		}

		public bool Contain(float localTime)
		{
			for (int i = 0; i < timeIntervals.Count; i++)
			{
				if (localTime >= timeIntervals[i].x && localTime <= timeIntervals[i].y)
				{
					return true;
				}
			}

			return false;
		}

		public float GetSectionTime()
		{
			float time = 0;
			for (int i = 0; i < timeIntervals.Count; i++)
			{
				time += (timeIntervals[i].y - timeIntervals[i].x);
			}

			return time;
		}

		public int GetNextOrCurrentIntervalIndex(float localTime)
		{
			for (int i = 0; i < timeIntervals.Count; i++)
			{
				if (timeIntervals[i].x <= localTime && localTime <= timeIntervals[i].y)
				{
					return i;
				}
				else if (localTime < timeIntervals[i].x)
				{
					return i;
				}
			}

			if (timeIntervals.Count > 0)
			{
				float2 lastInterval = timeIntervals[timeIntervals.Count - 1];
				if (lastInterval.y < localTime)
				{
					return timeIntervals.Count - 1;
				}
			}


			return -1;
		}
	}
}