using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public class MotionMatchingState_SO : State_SO
	{
		[SerializeField]
		public MotionMatchingStateFeatures Features;

		public override MotionMatchingStateType StateType => MotionMatchingStateType.MotionMatching;

		public override LogicState CreateLogicState(MotionMatchingComponent motionMatching, LogicMotionMatchingLayer logicLayer, PlayableAnimationSystem animationSystem)
		{
			return new LogicMotionMatchingState(this, motionMatching, logicLayer, animationSystem);
		}
	}
}