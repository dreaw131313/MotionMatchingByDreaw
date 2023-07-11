using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public class ContactState_SO : State_SO
	{
		[SerializeField]
		public ContactStateFeatures Features;

		public override MotionMatchingStateType StateType => MotionMatchingStateType.ContactAnimationState;

		public override LogicState CreateLogicState(MotionMatchingComponent motionMatching, LogicMotionMatchingLayer logicLayer, PlayableAnimationSystem animationSystem)
		{
			switch (Features.contactStateType)
			{
				case ContactStateType.NormalContacts:
					{
						return new LogicContactState(this, motionMatching, logicLayer, animationSystem);
					}
				case ContactStateType.Impacts:
					{
						return new LogicImpactState(this, motionMatching, logicLayer, animationSystem);
					}
			}

			throw new System.Exception("Wrong contact state type!");
		}
	}
}