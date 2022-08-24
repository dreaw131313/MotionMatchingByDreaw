using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public struct SectionAcces
	{
		private DataSection dataSection;

		public SectionAcces(DataSection dataSection)
		{
			this.dataSection = dataSection;
		}

		public bool IsValid
		{
			get
			{
				return dataSection != null;
			}
		}

		public bool TryGetIntervalEnd(float localTime, out float endTime)
		{
			endTime = -1;
			List<float2> intervals = dataSection.timeIntervals;
			for (int i = 0; i < intervals.Count; i++)
			{
				if (localTime >= intervals[i].x && localTime <= intervals[i].y)
				{
					endTime = intervals[i].y;
					return true;
				}
			}

			return false;
		}

		public bool TryGetIntervalStart(float localTime, out float endTime)
		{
			endTime = -1;
			List<float2> intervals = dataSection.timeIntervals;
			for (int i = 0; i < intervals.Count; i++)
			{
				if (localTime >= intervals[i].x && localTime <= intervals[i].y)
				{
					endTime = intervals[i].x;
					return true;
				}
			}

			return false;
		}

		public bool TryGetInterval(float localTime, out float2 interval)
		{
			List<float2> intervals = dataSection.timeIntervals;
			for (int i = 0; i < intervals.Count; i++)
			{
				if (localTime >= intervals[i].x && localTime <= intervals[i].y)
				{
					interval = intervals[i];
					return true;
				}
			}
			interval = new float2(-1, -1);
			return false;
		}

		public float2 GetInterval(int index)
		{
			return dataSection.timeIntervals[index];
		}

		public bool IsInSection(float localTime)
		{
			return dataSection.Contain(localTime);
		}
	}
}