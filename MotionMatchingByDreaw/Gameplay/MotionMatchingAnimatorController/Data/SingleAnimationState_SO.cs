using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public class SingleAnimationState_SO : State_SO
	{
		[SerializeField]
		public SingleAnimationStateFeatures Features;

		public override MotionMatchingStateType StateType => MotionMatchingStateType.SingleAnimation;

		public override LogicState CreateLogicState(MotionMatchingComponent motionMatching, LogicMotionMatchingLayer logicLayer, PlayableAnimationSystem animationSystem)
		{
			return new LogicSingleAnimationState(this, motionMatching, logicLayer, animationSystem);
		}
	}
}