using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public delegate void MotionMatchingAnimationEventDelegate();

	public class MotionMatchingComponentAnimationEvent
	{
		MotionMatchingAnimationEventDelegate eventFunction;

		public void AddFunction(MotionMatchingAnimationEventDelegate delegateFunction)
		{
			eventFunction += delegateFunction;
		}

		public void RemoveFunction(MotionMatchingAnimationEventDelegate delegateFunction)
		{
			eventFunction -= delegateFunction;
		}

		public void Invoke()
		{
			eventFunction.Invoke();
		}
	}
}