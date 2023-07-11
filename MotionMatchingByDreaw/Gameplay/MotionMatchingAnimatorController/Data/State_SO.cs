using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public enum MotionMatchingStateType : int
	{
		MotionMatching,
		SingleAnimation,
		ContactAnimationState,

		Undefined = int.MaxValue
	}

	public abstract class State_SO : ScriptableObject
	{
		public abstract MotionMatchingStateType StateType { get; }

		#region Common features
		[SerializeField]
		public string Name = "<MotionMatchingState>";
		[SerializeField]
		public int Index;
		[SerializeField]
		public List<Transition> Transitions;
		[SerializeField]
		public int StartSection = 0;
		[SerializeField]
		public MotionMatchingStateTag Tag = MotionMatchingStateTag.None;
		#endregion

		#region Trajectory
		[SerializeField]
		public bool TrajectoryCorrection;
		#endregion

		#region Motion Data Groups
		[SerializeField]
		public NativeMotionGroup MotionData;
		#endregion

		[SerializeField]
		public float SpeedMultiplier = 1f;


		public bool IsRuntimeValid()
		{
			return MotionData != null;
		}

		public int GetPoseBonesCount()
		{
			return MotionData.PoseBonesCount;
		}

		public int GetTrajectoryPointsCount()
		{
			return MotionData.SingleTrajectoryPointsCount;
		}

		public abstract LogicState CreateLogicState(
			MotionMatchingComponent motionMatching,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			);

#if UNITY_EDITOR
		[SerializeField]
		public bool animDataFold = false;
		[SerializeField]
		public StateNode Node;

		[SerializeField]
		public int SequenceID = 0;

		public void ValidateTransitions()
		{
			if (Transitions != null) return;

			for (int i = 0; i < Transitions.Count; i++)
			{
				Transitions[i].nextStateIndex = Transitions[i].ToState.Index;
			}
			EditorUtility.SetDirty(this);
		}

		public void UpadateMotionGroups()
		{
			if (MotionData != null)
			{
				MotionData.UpdateFromAnimationData();
			}

			AssetDatabase.Refresh();
		}

		public Transition AddTransitionToState(State_SO toState)
		{
			if (ContainTranstionToState(toState) || toState == this) return null;

			if (Transitions == null) Transitions = new List<Transition>();

			Transition newTransition = new Transition(toState.Index);
			newTransition.FromState = this;
			newTransition.ToState = toState;
			newTransition.nextStateIndex = toState.Index;
			newTransition.PortalToStateIndex = -1;

			Transitions.Add(newTransition);

			EditorUtility.SetDirty(this);

			return newTransition;
		}

		public Transition AddTransitionToPortal(PortalToState portal, int portalID)
		{
			if (ContainTranstionToState(portal.State) || portal.State == this) return null;

			if (Transitions == null) Transitions = new List<Transition>();


			Transition newTransition = new Transition(portal.State.Index);
			newTransition.FromState = this;
			newTransition.ToState = portal.State;
			newTransition.PortalToStateIndex = portalID;
			newTransition.nextStateIndex = portal.State.Index;

			Transitions.Add(newTransition);

			EditorUtility.SetDirty(this);

			return newTransition;
		}

		public void RemoveTransition(State_SO toState)
		{
			if (toState == null) return;

			for (int i = 0; i < Transitions.Count; i++)
			{
				if (Transitions[i].ToState == toState)
				{
					Transitions.RemoveAt(i);
					ValidateTransitions();


					EditorUtility.SetDirty(this);
					return;
				}
			}
		}


		public void RemoveTransition(Transition transitionToRemove)
		{
			if (Transitions == null) return;

			for (int i = 0; i < Transitions.Count; i++)
			{
				if (Transitions[i] == transitionToRemove)
				{
					Transitions.RemoveAt(i);

					ValidateTransitions();

					EditorUtility.SetDirty(this);
					return;
				}
			}
		}

		public void RemoveTranstionToPortal(PortalToState removedPortal, int portalID)
		{
			if (removedPortal == null) return;

			for (int i = 0; i < Transitions.Count; i++)
			{
				Transition t = Transitions[i];
				if (t.PortalToStateIndex == portalID && t.ToState == removedPortal.State)
				{
					Transitions.RemoveAt(i);
					ValidateTransitions();

				}
				else if (t.PortalToStateIndex > portalID)
				{
					t.PortalToStateIndex -= 1;
				}
			}


			EditorUtility.SetDirty(this);
		}

		private bool ContainTranstionToState(State_SO toState)
		{
			if (Transitions == null) return false;

			for (int i = 0; i < Transitions.Count; i++)
			{
				if (Transitions[i].ToState == toState)
				{
					return true;
				}
			}

			return false;
		}
#endif
	}

	[System.Serializable]
	public class StateNode
	{
		public Vector2 Position;

		public StateNode(Vector2 position)
		{
			Position = position;
		}
	}

}