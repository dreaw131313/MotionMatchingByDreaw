using MotionMatching.Gameplay;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class Transition
	{
#if UNITY_EDITOR
		[SerializeField]
		public State_SO FromState;
		[SerializeField]
		public State_SO ToState;
		[SerializeField]
		public int PortalToStateIndex = -1;

		public bool IsToPortal => PortalToStateIndex >= 0;
#endif

		[SerializeField]
		public int nextStateIndex;
		[SerializeField]
		public List<TransitionOptions> options;


		public Transition()
		{
			options = new List<TransitionOptions>();
		}

		public Transition(int toState)
		{
			this.nextStateIndex = toState;
			options = new List<TransitionOptions>();
		}

		//public Transition(int fromState, int toState)
		//{
		//	this.nextStateIndex = toState;
		//	options = new List<TransitionOptions>();
		//}

		//public Transition(int fromState, int toState, int portalID)
		//{
		//	this.nextStateIndex = toState;
		//	options = new List<TransitionOptions>();

		//	PortalID = portalID;

		//	toPortal = true;
		//}

		public int ShouldTransitionBegin(
			float currentAnimLocalTime,
			float currentAnimGlobalTime,
			MotionMatchingComponent motionMatchingComponent,
			MotionMatchingStateType currentStateType,
			MotionMatchingDataInfo dataInfo,
			NativeMotionGroup motionGroup
			)
		{
			for (int i = 0; i < options.Count; i++)
			{
				if (options[i].ShouldTranistionFromThisOption(
					currentAnimLocalTime,
					currentAnimGlobalTime,
					motionMatchingComponent,
					currentStateType,
					dataInfo,
					motionGroup
					))
				{
					return i;
				}
			}
			return -1;
		}
	}


	[System.Serializable]
	public class TransitionOptions
	{
		[SerializeField]
		public int WhenCanCheckingSection = 1;
		[SerializeField]
		public int WhereCanFindingBestPoseSection = 1;
		[SerializeField]
		public float BlendTime = 0.35f;
		[SerializeField]
		public bool StartOnExitTime = false;
		[SerializeField]
		public bool PerformFinding = true;

		[SerializeField]
		public bool TransitionAfterSectionStart = false; // przejscie nastepuje jezeli czas globalny animacji jest wiekszy od stary pierwszego interwału wybranej sekcji, opcja ta jest aktywna w stanach innych niż motion matching

		// transition to Motion matching state
		[SerializeField]
		public int SectionAfterFirstFinding = 1;

		[SerializeField]
		public List<ConditionBool> boolConditions = new List<ConditionBool>();
		[SerializeField]
		public List<ConditionInt> intConditions = new List<ConditionInt>();
		[SerializeField]
		public List<ConditionFloat> floatConditions = new List<ConditionFloat>();
		[SerializeField]
		public List<ConditionTrigger> TriggerConditions = new List<ConditionTrigger>();

		public TransitionOptions(string name)
		{
			this.StartOnExitTime = false;
			//this.Name = name;
			BlendTime = 0.3f;
		}

		public bool ShouldTranistionFromThisOption(
			float currentAnimLocalTime,
			float currentAnimGlobalTime,
			MotionMatchingComponent motionMatchingComponent,
			MotionMatchingStateType currentStateType,
			MotionMatchingDataInfo dataInfo,
			NativeMotionGroup motionGroup
			)
		{
			if (currentStateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				if (WhenCanCheckingSection >= motionGroup.SectionsDependencies.SectionsCount)
				{
					Debug.LogError($"Wrong \"WhenCanCheckingSection\" index in transition which use \"{motionGroup.SectionsDependencies.name}\" sections depenecies asset!");
					return false;
				}
#endif
				if (TransitionAfterSectionStart)
				{
					DataSection section = dataInfo.Sections[WhenCanCheckingSection];
					if (section.timeIntervals.Count == 0) return false;
					float2 firstInterval = section.timeIntervals[0];

					if (currentAnimGlobalTime < firstInterval.x)
					{
						return false;
					}
				}
				else
				{
					if (!dataInfo.Sections[WhenCanCheckingSection].Contain(currentAnimLocalTime))
					{
						if (
							(StartOnExitTime && currentAnimGlobalTime < (dataInfo.Length - (BlendTime))) ||
							!StartOnExitTime
							)
						{
							return false;
						}
					}
				}
			}


			for (int conditionIndex = 0; conditionIndex < TriggerConditions.Count; conditionIndex++)
			{
				if (!TriggerConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
				{
					return false;
				}
			}

			for (int conditionIndex = 0; conditionIndex < boolConditions.Count; conditionIndex++)
			{
				if (!boolConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
				{
					return false;
				}
			}

			for (int conditionIndex = 0; conditionIndex < floatConditions.Count; conditionIndex++)
			{
				if (!floatConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
				{
					return false;
				}
			}

			for (int conditionIndex = 0; conditionIndex < intConditions.Count; conditionIndex++)
			{
				if (!intConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
				{
					return false;
				}
			}

			return true;
		}

	}
}