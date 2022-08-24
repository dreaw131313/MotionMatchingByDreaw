using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class MotionMatchingAnimationEvent : IComparable<MotionMatchingAnimationEvent>
	{
		[SerializeField]
		public string Name;
		[SerializeField]
		public float EventTime;
		
		public MotionMatchingAnimationEvent(string name, float eventTime)
		{
			Name = name;
			EventTime = eventTime;
		}

		public int CompareTo(MotionMatchingAnimationEvent other)
		{
			if(other == null)
			{
				return 1;
			}
			if (this.EventTime < other.EventTime)
			{
				return -1;
			}
			else if(this.EventTime > other.EventTime)
			{
				return 1;
			}
			return 0;
		}
	}
}