using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public interface IMotionMatchingRootMotionOverride
	{
		public abstract void PerformRootMotionMovement(Vector3 position, Quaternion rotation);
	}
}
