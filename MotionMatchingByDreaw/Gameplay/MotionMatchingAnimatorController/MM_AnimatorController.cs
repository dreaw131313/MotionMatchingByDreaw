using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "MotionMatchingAnimatorController", menuName = "Motion Matching/Motion Matching Animator Controller")]
	public class MM_AnimatorController : ScriptableObject
	{
		[SerializeField]
		public List<MotionMatchingLayer> layers = new List<MotionMatchingLayer>();
		[SerializeField]
		public List<string> TriggersNames = new List<string>();
		[SerializeField]
		public List<BoolParameter> BoolParameters = new List<BoolParameter>();
		[SerializeField]
		public List<IntParameter> IntParamters = new List<IntParameter>();
		[SerializeField]
		public List<FloatParameter> FloatParamaters = new List<FloatParameter>();

		[SerializeField]
		public List<SecondaryLayerData> SecondaryLayers;

		public Dictionary<string, int> FloatsIndexes { get; private set; } = null;
		public Dictionary<string, int> IntsIndexes { get; private set; } = null;
		public Dictionary<string, int> BoolsIndexes { get; private set; } = null;
		public Dictionary<string, int> TriggersIndexes { get; private set; } = null;

		internal void OnEnable()
		{
#if UNITY_EDITOR
			if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

			for (int i = 0; i < layers.Count; i++)
			{
				layers[i].Initialize();
			}

			if (FloatParamaters.Count > 0)
			{
				FloatsIndexes = new Dictionary<string, int>(FloatParamaters.Count);
				for (int i = 0; i < FloatParamaters.Count; i++)
				{
					FloatsIndexes.Add(FloatParamaters[i].Name, i);
				}
			}

			if (IntParamters.Count > 0)
			{
				IntsIndexes = new Dictionary<string, int>(IntParamters.Count);
				for (int i = 0; i < IntParamters.Count; i++)
				{
					IntsIndexes.Add(IntParamters[i].Name, i);
				}
			}

			if (BoolParameters.Count > 0)
			{
				BoolsIndexes = new Dictionary<string, int>(BoolParameters.Count);
				for (int i = 0; i < BoolParameters.Count; i++)
				{
					BoolsIndexes.Add(BoolParameters[i].Name, i);
				}
			}

			if (TriggersNames.Count > 0)
			{
				TriggersIndexes = new Dictionary<string, int>(TriggersNames.Count);
				for (int i = 0; i < TriggersNames.Count; i++)
				{
					TriggersIndexes.Add(TriggersNames[i], i);
				}
			}
		}

		public float[] GetTrajectoryPointsTimes()
		{
			return layers[0].states[0].MotionData.TrajectoryTimes;
		}

		public int GetPoseBonesCount()
		{
			return layers[0].states[0].MotionData.PoseBonesCount;
		}

		public string[] GetStatesNamesInLayer(int layerIndex = 0)
		{
			int statesCount = layers[layerIndex].states.Count;
			string[] statesNames = new string[statesCount];

			for (int i = 0; i < statesCount; i++)
			{
				statesNames[i] = layers[layerIndex].states[i].Name;
			}

			return statesNames;
		}


		#region Editor fields and methods
#if UNITY_EDITOR

		public void UnsubscribeMotionMatchingComponentFromMotionGroups(MotionMatchingComponent motionMatching)
		{
			for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
			{
				MotionMatchingLayer layer = layers[layerIndex];
				for (int stateIndex = 0; stateIndex < layer.states.Count; stateIndex++)
				{
					if (layer.states[stateIndex].MotionData != null)
					{
						layer.states[stateIndex].MotionData.UnsubscribeByMotionMatchingComponent(motionMatching);
					}
				}
			}
		}

		[SerializeField]
		public int SelectedSequenceIndex;

		public MM_AnimatorController()
		{

		}

		public void AddLayer(string name, AvatarMask mask)
		{
			string newName = name;
			int counter = 0;
			if (name == "")
			{
				newName = "New Layer";
				name = newName;
			}
			for (int i = 0; i < this.layers.Count; i++)
			{
				if (layers[i].name == newName)
				{
					counter++;
					newName = name + counter.ToString();
					i = 0;
				}
			}
			int newIndex = this.layers.Count;
			layers.Add(new MotionMatchingLayer(newName, newIndex));
			this.layers[newIndex].avatarMask = mask;
		}

		public void RemoveLayerAt(int index)
		{
			if (index >= layers.Count || index < 0)
			{
				Debug.Log("Can not remove layer");
			}
			if (index == (layers.Count - 1))
			{
				layers.RemoveAt(index);
				return;
			}
			else
			{
				layers.RemoveAt(index);
				for (int i = index; i < layers.Count; i++)
				{
					layers[i].index = i;
				}
			}
		}

		public bool RemoveLayer(string layerName)
		{
			int index = -1;
			for (int i = 0; i < layers.Count; i++)
			{
				if (layers[i].name == layerName)
				{
					index = i;
					break;
				}
			}
			if (index == -1)
			{
				return false;
			}
			else
			{
				RemoveLayerAt(index);
				return true;
			}
		}

		public void RenameLayer(int layerIndex, string newLayerName)
		{
			string newName = newLayerName;
			int counter = 0;
			for (int i = 0; i < layers.Count; i++)
			{
				if (newLayerName == layers[i].name && i != layerIndex)
				{
					counter++;
					newName = newLayerName + counter.ToString();
					i = 0;
				}
			}
			layers[layerIndex].name = newName;
		}

		public bool AddBool(string name)
		{
			string currentName = name;
			string newName = currentName;
			int counter = 0;
			for (int i = 0; i < BoolParameters.Count; i++)
			{
				if (BoolParameters[i].Name == newName)
				{
					counter++;
					newName = currentName + counter.ToString();
					i = 0;
				}
			}
			BoolParameters.Add(new BoolParameter(newName, false));
			return true;
		}

		public void AddInt(string name)
		{
			string currentName = name;
			string newName = currentName;
			int counter = 0;
			for (int i = 0; i < IntParamters.Count; i++)
			{
				if (IntParamters[i].Name == newName)
				{
					counter++;
					newName = currentName + counter.ToString();
					i = 0;
				}
			}
			IntParamters.Add(new IntParameter(newName, 0));
		}

		public void AddFloat(string name)
		{
			string currentName = name;
			string newName = currentName;
			int counter = 0;
			for (int i = 0; i < FloatParamaters.Count; i++)
			{
				if (FloatParamaters[i].Name == newName)
				{
					counter++;
					newName = currentName + counter.ToString();
					i = 0;
				}
			}
			FloatParamaters.Add(new FloatParameter(newName, 0f));
		}

		public void AddTrigger(string name)
		{
			string currentName = name;
			string newName = currentName;
			int counter = 0;
			for (int i = 0; i < TriggersNames.Count; i++)
			{
				if (TriggersNames[i] == newName)
				{
					counter++;
					newName = currentName + counter.ToString();
					i = 0;
				}
			}
			TriggersNames.Add(newName);
		}

		public void UpdateMotionGroupInAllState()
		{
			foreach (MotionMatchingLayer layer in layers)
			{
				foreach (MotionMatchingState state in layer.states)
				{
					if (state.MotionData != null)
					{
						state.MotionData.UpdateFromAnimationData();
					}
				}
			}
		}

#endif
		#endregion
	}

	[System.Serializable]
	public struct BoolParameter
	{
		public string Name;
		public bool Value;

		public BoolParameter(string name, bool value)
		{
			Name = name;
			Value = value;
		}
	}

	[System.Serializable]
	public struct IntParameter
	{
		public string Name;
		public int Value;

		public IntParameter(string name, int value)
		{
			Name = name;
			Value = value;
		}
	}

	[System.Serializable]
	public struct FloatParameter
	{
		public string Name;
		public float Value;

		public FloatParameter(string name, float value)
		{
			Name = name;
			Value = value;
		}
	}
}