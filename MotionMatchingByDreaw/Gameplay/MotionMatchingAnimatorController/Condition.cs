using MotionMatching.Gameplay;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public enum ConditionType : int
	{
		Equal,
		Less,
		Greater,
		GreaterEqual,
		LessEqual,
		Diffrent,
	}

	public enum BoolConditionType : int
	{
		IsTrue,
		IsFalse
	}

	[System.Serializable]
	public struct ConditionFloat
	{
		[SerializeField]
		public int CheckingValueIndex;
		[SerializeField]
		public float ConditionValue;
		[SerializeField]
		public ConditionType CheckType;
		[SerializeField]
		public bool UseOtherFloatAsConditionValue;
		[SerializeField]
		public int ConditionValueIndex;

		public ConditionFloat(int checkingValueIndex = 0)
		{
			CheckingValueIndex = 0;
			ConditionValue = 0f;
			CheckType = ConditionType.Greater;
			UseOtherFloatAsConditionValue = false;
			ConditionValueIndex = 0;
		}

		public bool CalculateCondition(MotionMatchingComponent motionMatchingComponent)
		{

			bool condition = false;

			float[] floats = motionMatchingComponent.ConditionFloats;

#if UNITY_EDITOR
			if (floats == null ||
				CheckingValueIndex >= floats.Length || CheckingValueIndex < 0 ||
				UseOtherFloatAsConditionValue && (ConditionValueIndex >= floats.Length || ConditionValueIndex < 0))
			{
				Debug.LogError($"In transition from {motionMatchingComponent.GetCurrentState().m_DataState.Name} \"float\" condition have invalid settings");
				return false;
			}
#endif

			if (UseOtherFloatAsConditionValue)
			{
				switch (CheckType)
				{
					case ConditionType.Equal:
						condition = floats[CheckingValueIndex] == floats[ConditionValueIndex];
						break;
					case ConditionType.Less:
						condition = floats[CheckingValueIndex] < floats[ConditionValueIndex];
						break;
					case ConditionType.Greater:
						condition = floats[CheckingValueIndex] > floats[ConditionValueIndex];
						break;
					case ConditionType.GreaterEqual:
						condition = floats[CheckingValueIndex] >= floats[ConditionValueIndex];
						break;
					case ConditionType.LessEqual:
						condition = floats[CheckingValueIndex] <= floats[ConditionValueIndex];
						break;
					case ConditionType.Diffrent:
						condition = floats[CheckingValueIndex] != floats[ConditionValueIndex];
						break;
				}
			}
			else
			{
				switch (CheckType)
				{
					case ConditionType.Equal:
						condition = floats[CheckingValueIndex] == ConditionValue;
						break;
					case ConditionType.Less:
						condition = floats[CheckingValueIndex] < ConditionValue;
						break;
					case ConditionType.Greater:
						condition = floats[CheckingValueIndex] > ConditionValue;
						break;
					case ConditionType.GreaterEqual:
						condition = floats[CheckingValueIndex] >= ConditionValue;
						break;
					case ConditionType.LessEqual:
						condition = floats[CheckingValueIndex] <= ConditionValue;
						break;
					case ConditionType.Diffrent:
						condition = floats[CheckingValueIndex] != ConditionValue;
						break;
				}
			}

			return condition;
		}
	}

	[System.Serializable]
	public struct ConditionInt
	{
		[SerializeField]
		public int CheckingValueIndex;
		[SerializeField]
		public int ConditionValue;
		[SerializeField]
		public ConditionType CheckType;
		[SerializeField]
		public bool UseOtherFloatAsConditionValue;
		[SerializeField]
		public int ConditionValueIndex;

		public ConditionInt(int checkingValueIndex = 0)
		{
			CheckingValueIndex = 0;
			ConditionValue = 0;
			CheckType = ConditionType.Greater;
			UseOtherFloatAsConditionValue = false;
			ConditionValueIndex = 0;
		}

		public bool CalculateCondition(MotionMatchingComponent motionMatchingComponent)
		{
			bool condition = false;

			int[] ints = motionMatchingComponent.ConditionInts;

#if UNITY_EDITOR
			if (ints == null ||
				CheckingValueIndex >= ints.Length || CheckingValueIndex < 0 ||
				UseOtherFloatAsConditionValue && (ConditionValueIndex >= ints.Length || ConditionValueIndex < 0))
			{
				Debug.LogError($"In transition from {motionMatchingComponent.GetCurrentState().m_DataState.Name} \"int\" condition have invalid settings");
				return false;
			}
#endif

			if (UseOtherFloatAsConditionValue)
			{
				switch (CheckType)
				{
					case ConditionType.Equal:
						condition = ints[CheckingValueIndex] == ints[ConditionValueIndex];
						break;
					case ConditionType.Less:
						condition = ints[CheckingValueIndex] < ints[ConditionValueIndex];
						break;
					case ConditionType.Greater:
						condition = ints[CheckingValueIndex] > ints[ConditionValueIndex];
						break;
					case ConditionType.GreaterEqual:
						condition = ints[CheckingValueIndex] >= ints[ConditionValueIndex];
						break;
					case ConditionType.LessEqual:
						condition = ints[CheckingValueIndex] <= ints[ConditionValueIndex];
						break;
					case ConditionType.Diffrent:
						condition = ints[CheckingValueIndex] != ints[ConditionValueIndex];
						break;
				}
			}
			else
			{
				switch (CheckType)
				{
					case ConditionType.Equal:
						condition = ints[CheckingValueIndex] == ConditionValue;
						break;
					case ConditionType.Less:
						condition = ints[CheckingValueIndex] < ConditionValue;
						break;
					case ConditionType.Greater:
						condition = ints[CheckingValueIndex] > ConditionValue;
						break;
					case ConditionType.GreaterEqual:
						condition = ints[CheckingValueIndex] >= ConditionValue;
						break;
					case ConditionType.LessEqual:
						condition = ints[CheckingValueIndex] <= ConditionValue;
						break;
					case ConditionType.Diffrent:
						condition = ints[CheckingValueIndex] != ConditionValue;
						break;
				}
			}

			return condition;
		}

	}

	[System.Serializable]
	public struct ConditionBool
	{
		[SerializeField]
		public int CheckingValueIndex;
		[SerializeField]
		public BoolConditionType CheckType;

		public ConditionBool(int checkingValueIndex = 0)
		{
			CheckType = BoolConditionType.IsTrue;
			CheckingValueIndex = -1;
		}

		public bool CalculateCondition(MotionMatchingComponent motionMatchingComponent)
		{
			bool condition = false;

#if UNITY_EDITOR
			if (motionMatchingComponent.ConditionBools == null ||
				CheckingValueIndex >= motionMatchingComponent.ConditionBools.Length || CheckingValueIndex < 0)
			{
				Debug.LogError($"In transition from {motionMatchingComponent.GetCurrentState().m_DataState.Name} \"bool\" condition have invalid settings!");
				return false;
			}
#endif

			switch (CheckType)
			{
				case BoolConditionType.IsFalse:
					condition = !motionMatchingComponent.ConditionBools[CheckingValueIndex];
					break;
				case BoolConditionType.IsTrue:
					condition = motionMatchingComponent.ConditionBools[CheckingValueIndex];
					break;
			}

			return condition;
		}
	}

	[System.Serializable]
	public struct ConditionTrigger
	{
		[SerializeField]
		public int CheckingValueIndex;

		public ConditionTrigger(int checkingValueIndex = 0)
		{
			CheckingValueIndex = -1;
		}

		public bool CalculateCondition(MotionMatchingComponent motionMatchingComponent)
		{
#if UNITY_EDITOR
			if (motionMatchingComponent.ConditionTriggers == null ||
				CheckingValueIndex >= motionMatchingComponent.ConditionTriggers.Length || CheckingValueIndex < 0)
			{
				Debug.LogError($"In transition from {motionMatchingComponent.GetCurrentState().m_DataState.Name} \"Trigger\" condition have invalid settings!");
				return false;
			}
#endif


			return motionMatchingComponent.ConditionTriggers[CheckingValueIndex].value;
		}
	}

}
