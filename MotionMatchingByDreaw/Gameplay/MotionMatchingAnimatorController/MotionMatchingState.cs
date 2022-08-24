using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
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

	[System.Serializable]
	public class MotionMatchingState
	{
		#region Common features
		[SerializeField]
		public string Name;
		[SerializeField]
		public int Index;
		[SerializeField]
		public MotionMatchingStateType stateType;
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
		public int StateFeaturesIndex;

		[SerializeField]
		public float SpeedMultiplier = 1f;

		public MotionMatchingState(string name, MotionMatchingStateType type, int index, int stateID, int stateFeaturesIndex)
		{
			this.Name = name;
			this.stateType = type;
			this.Index = index;

#if UNITY_EDITOR
			this.nodeID = stateID;
#endif

			Transitions = new List<Transition>();
			SpeedMultiplier = 1f;
			StateFeaturesIndex = stateFeaturesIndex;
		}

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

		#region Getters and Setters
		#endregion

#if UNITY_EDITOR
		[SerializeField]
		public bool animDataFold = false;
		[SerializeField]
		public int nodeID;

		public bool AddTransition(MotionMatchingState toState, int nodeID, bool portal = false)
		{
			Transitions.Add(new Transition(
				toState.Index
				));
			Transitions[Transitions.Count - 1].nodeID = nodeID;
			Transitions[Transitions.Count - 1].transitionRect = new Rect();
			Transitions[Transitions.Count - 1].transitionRect.size = new Vector2(15, 15);
			Transitions[Transitions.Count - 1].toPortal = portal;
			Transitions[Transitions.Count - 1].fromStateIndex = this.Index;
			return false;
		}

		public bool RemoveTransition(int toStateIndex)
		{
			for (int i = 0; i < Transitions.Count; i++)
			{
				if (Transitions[i].nextStateIndex == toStateIndex)
				{
					Transitions.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		public void UpadateMotionGroups()
		{
			if (MotionData != null)
			{
				MotionData.UpdateFromAnimationData();
			}

			AssetDatabase.Refresh();
		}

		public void UpdateTransitinoAfterStateRemove(int stateIndex)
		{
			for (int i = 0; i < Transitions.Count; i++)
			{
				Transition t = Transitions[i];

				if (t.nextStateIndex == stateIndex)
				{
					Transitions.RemoveAt(i);
				}
				else if (t.nextStateIndex > stateIndex)
				{
					t.nextStateIndex -= 1;
				}

			}
		}
#endif
	}

}