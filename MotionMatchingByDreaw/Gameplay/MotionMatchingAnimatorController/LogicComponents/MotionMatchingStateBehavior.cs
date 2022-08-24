using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	/// <summary>
	/// Class that provide posibility to execute logic on MotionMatchingStates.
	/// </summary>
	public abstract class MotionMatchingStateBehavior
	{
		protected LogicState logicState;
		protected MotionMatchingComponent motionMatching;
		protected Transform transform; // transfor from Motion Matching component
		protected GameObject gameObject; // game object which have Motion Matching comonent 

		public abstract void Enter();

		public abstract void Update();

		public abstract void LateUpdate();

		public abstract void FixedUpdate();

		public abstract void Exit();

		/// <summary>
		/// Called once after contact index change only when added to MotionMatchingContactState
		/// </summary>
		public abstract void OnDestinationContactPointChange(int destinationPointIndex);

		/// <summary>
		/// Called when time of animation reach start time of contact.
		/// </summary>
		/// <param name="achivedContactPointIndex"></param>
		public abstract void OnAchiveContactPoint(int achivedContactPointIndex);

		public abstract void OnBeginMovingToContactPoint(int toContactPointIndex);

		public abstract void OnEndContact(int endedContactIndex);

		/// <summary>
		/// Called once after enter to state after first Job has been completed or right after enter to state when in transition is selected option "Do not perform finding".
		/// </summary>
		public abstract void OnCompleteEnterFindingJob();

		public void SetBasic(LogicState logicState, MotionMatchingComponent mmAnimator, Transform transform, GameObject gameObject)
		{
			this.logicState = logicState;
			this.motionMatching = mmAnimator;
			this.transform = transform;
			this.gameObject = gameObject;
		}

	}
}