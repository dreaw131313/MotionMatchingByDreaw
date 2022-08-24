using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public struct StateSwitchTrigger
	{
		[SerializeField]
		internal bool value;

		public StateSwitchTrigger(bool value)
		{
			this.value = value;
		}
	}
}
