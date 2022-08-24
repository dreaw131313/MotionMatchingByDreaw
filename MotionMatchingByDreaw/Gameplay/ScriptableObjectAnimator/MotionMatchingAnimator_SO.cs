using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "MotionMatchingAnimator_SO", menuName = "Motion Matching/MotionMatchingAnimator_SO")]
	public class MotionMatchingAnimator_SO : ScriptableObject
	{
		[SerializeField]
		public List<MotionMatchingLayer_SO> Layers = new List<MotionMatchingLayer_SO>();
		[SerializeField]
		public List<string> TriggersNames = new List<string>();
		[SerializeField]
		public List<BoolParameter> BoolParameters = new List<BoolParameter>();
		[SerializeField]
		public List<IntParameter> IntParameters = new List<IntParameter>();
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

			for (int i = 0; i < Layers.Count; i++)
			{
				Layers[i].Initialize();
			}

			if (FloatParamaters.Count > 0)
			{
				FloatsIndexes = new Dictionary<string, int>(FloatParamaters.Count);
				for (int i = 0; i < FloatParamaters.Count; i++)
				{
					FloatsIndexes.Add(FloatParamaters[i].Name, i);
				}
			}

			if (IntParameters.Count > 0)
			{
				IntsIndexes = new Dictionary<string, int>(IntParameters.Count);
				for (int i = 0; i < IntParameters.Count; i++)
				{
					IntsIndexes.Add(IntParameters[i].Name, i);
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
			return Layers[0].States[0].MotionData.TrajectoryTimes;
		}

		public int GetPoseBonesCount()
		{
			return Layers[0].States[0].MotionData.PoseBonesCount;
		}

		public string[] GetStatesNamesInLayer(int layerIndex = 0)
		{
			int statesCount = Layers[layerIndex].States.Count;
			string[] statesNames = new string[statesCount];

			for (int i = 0; i < statesCount; i++)
			{
				statesNames[i] = Layers[layerIndex].States[i].Name;
			}

			return statesNames;
		}



		#region Editor fields and methods

#if UNITY_EDITOR
		[Header("EDITOR ONLY FIEDLS:")]
		[SerializeField]
		public int SelectedSequenceIndex;

		[SerializeField]
		private MotionMatchingLayer_SO selectedLayer;
		[SerializeField]
		private State_SO selectedState;

		public MotionMatchingLayer_SO SelectedLayer { get => selectedLayer; set => selectedLayer = value; }
		public State_SO SelectedState
		{
			get => selectedState;

			set
			{
				selectedState = value;
			}
		}

		[SerializeField]
		private float zoom = 1f;


		public float Zoom { get => zoom; set => zoom = Mathf.Clamp(value, 0.001f, 1000f); }

		public void UnsubscribeMotionMatchingComponentFromMotionGroups(MotionMatchingComponent motionMatching)
		{
			for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
			{
				MotionMatchingLayer_SO layer = Layers[layerIndex];
				for (int stateIndex = 0; stateIndex < layer.States.Count; stateIndex++)
				{
					if (layer.States[stateIndex].MotionData != null)
					{
						layer.States[stateIndex].MotionData.UnsubscribeByMotionMatchingComponent(motionMatching);
					}
				}
			}
		}


		public MotionMatchingLayer_SO AddLayer(string name, AvatarMask mask)
		{
			string newName = name;
			int counter = 0;
			if (name == "")
			{
				newName = "New Layer";
				name = newName;
			}
			for (int i = 0; i < this.Layers.Count; i++)
			{
				if (Layers[i].name == newName)
				{
					counter++;
					newName = name + counter.ToString();
					i = 0;
				}
			}

			MotionMatchingLayer_SO layer = CreateInstance<MotionMatchingLayer_SO>();
			layer.name = typeof(MotionMatchingLayer_SO).Name;
			layer.AvatarMask = mask;
			layer.Name = name;

			AssetDatabase.AddObjectToAsset(layer, this);

			Layers.Add(layer);

			return layer;
		}

		public void RemoveLayerAt(int index)
		{
			if (index >= Layers.Count || index < 0)
			{
				Debug.Log("Can not remove layer");
			}
			if (index == (Layers.Count - 1))
			{
				Layers.RemoveAt(index);
				return;
			}
			else
			{
				Layers.RemoveAt(index);
				for (int i = index; i < Layers.Count; i++)
				{
					Layers[i].Index = i;
				}
			}
		}

		public bool RemoveLayer(string layerName)
		{
			int index = -1;
			for (int i = 0; i < Layers.Count; i++)
			{
				if (Layers[i].name == layerName)
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
			for (int i = 0; i < Layers.Count; i++)
			{
				if (newLayerName == Layers[i].name && i != layerIndex)
				{
					counter++;
					newName = newLayerName + counter.ToString();
					i = 0;
				}
			}
			Layers[layerIndex].name = newName;
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
			for (int i = 0; i < IntParameters.Count; i++)
			{
				if (IntParameters[i].Name == newName)
				{
					counter++;
					newName = currentName + counter.ToString();
					i = 0;
				}
			}
			IntParameters.Add(new IntParameter(newName, 0));
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

		public void RemoveBool(string name)
		{
			int boolIndex = -1;
			for (int i = 0; i < BoolParameters.Count; i++)
			{
				if (BoolParameters[i].Name.Equals(name))
				{
					boolIndex = i;
				}
			}
			if (boolIndex == -1) return;

			BoolParameters.RemoveAt(boolIndex);

			foreach (var layer in Layers)
			{
				foreach (var state in layer.States)
				{
					foreach (var transition in state.Transitions)
					{
						foreach (var option in transition.options)
						{
							for (int idx = 0; idx < option.boolConditions.Count; idx++)
							{
								if (option.boolConditions[idx].CheckingValueIndex == boolIndex)
								{
									option.boolConditions.RemoveAt(idx);
									idx -= 1;
								}
								else if (option.boolConditions[idx].CheckingValueIndex > boolIndex)
								{
									ConditionBool conditionBool = option.boolConditions[idx];
									conditionBool.CheckingValueIndex -= 1;
									option.boolConditions[idx] = conditionBool;
								}
							}
						}
					}
				}
			}
		}

		public void RemoveInt(string name)
		{
			int intIndex = -1;
			for (int i = 0; i < IntParameters.Count; i++)
			{
				if (IntParameters[i].Name.Equals(name))
				{
					intIndex = i;
				}
			}
			if (intIndex == -1) return;

			IntParameters.RemoveAt(intIndex);

			foreach (var layer in Layers)
			{
				foreach (var state in layer.States)
				{
					foreach (var transition in state.Transitions)
					{
						foreach (var option in transition.options)
						{
							for (int idx = 0; idx < option.intConditions.Count; idx++)
							{
								if (option.intConditions[idx].CheckingValueIndex == intIndex)
								{
									option.intConditions.RemoveAt(idx);
									idx -= 1;
								}
								else if (option.intConditions[idx].CheckingValueIndex > intIndex)
								{
									ConditionInt conditionInt = option.intConditions[idx];
									conditionInt.CheckingValueIndex -= 1;
									option.intConditions[idx] = conditionInt;
								}
							}
						}
					}
				}
			}
		}

		public void RemoveFloat(string name)
		{
			int floatIndex = -1;
			for (int i = 0; i < FloatParamaters.Count; i++)
			{
				if (FloatParamaters[i].Name.Equals(name))
				{
					floatIndex = i;
				}
			}
			if (floatIndex == -1) return;

			FloatParamaters.RemoveAt(floatIndex);

			foreach (var layer in Layers)
			{
				foreach (var state in layer.States)
				{
					foreach (var transition in state.Transitions)
					{
						foreach (var option in transition.options)
						{
							for (int idx = 0; idx < option.floatConditions.Count; idx++)
							{
								if (option.floatConditions[idx].CheckingValueIndex == floatIndex)
								{
									option.floatConditions.RemoveAt(idx);
									idx -= 1;
								}
								else if (option.floatConditions[idx].CheckingValueIndex > floatIndex)
								{
									ConditionFloat conditionFloat = option.floatConditions[idx];
									conditionFloat.CheckingValueIndex -= 1;
									option.floatConditions[idx] = conditionFloat;
								}
							}
						}
					}
				}
			}
		}

		public void RemoveTrigger(string name)
		{
			int triggerIndex = -1;
			for (int i = 0; i < TriggersNames.Count; i++)
			{
				if (TriggersNames[i].Equals(name))
				{
					triggerIndex = i;
				}
			}
			if (triggerIndex == -1) return;

			TriggersNames.RemoveAt(triggerIndex);

			foreach (var layer in Layers)
			{
				foreach (var state in layer.States)
				{
					foreach (var transition in state.Transitions)
					{
						foreach (var option in transition.options)
						{
							for (int idx = 0; idx < option.TriggerConditions.Count; idx++)
							{
								if (option.TriggerConditions[idx].CheckingValueIndex == triggerIndex)
								{
									option.TriggerConditions.RemoveAt(idx);
									idx -= 1;
								}
								else if (option.TriggerConditions[idx].CheckingValueIndex > triggerIndex)
								{
									ConditionTrigger conditionTrigger = option.TriggerConditions[idx];
									conditionTrigger.CheckingValueIndex -= 1;
									option.TriggerConditions[idx] = conditionTrigger;
								}
							}
						}
					}
				}
			}
		}

		public void UpdateMotionGroupInAllState()
		{
			foreach (MotionMatchingLayer_SO layer in Layers)
			{
				foreach (State_SO state in layer.States)
				{
					if (state.MotionData != null)
					{
						state.MotionData.UpdateFromAnimationData();
					}
				}
			}
		}


		public void ValidateElementIndexes()
		{
			if (Layers == null) return;

			for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
			{
				MotionMatchingLayer_SO layer = Layers[layerIndex];
				layer.Index = layerIndex;

				layer.ValidateStatesIndexes();
			}
		}


		public static void CreateFromOldVersion(MM_AnimatorController old)
		{
			if (old == null) return;

			MotionMatchingAnimator_SO animator = CreateInstance<MotionMatchingAnimator_SO>();
			animator.name = old.name + "_New";


			string pathToOldAsset = AssetDatabase.GetAssetPath(old);
			string newAssetPath = pathToOldAsset.Substring(0, pathToOldAsset.Length - old.name.Length - 6);
			newAssetPath += animator.name + ".asset";

			AssetDatabase.CreateAsset(animator, newAssetPath);


			// parameters
			animator.BoolParameters = new List<BoolParameter>();
			foreach (var boolP in old.BoolParameters)
			{
				animator.BoolParameters.Add(boolP);
			}

			animator.IntParameters = new List<IntParameter>();
			foreach (var intP in old.IntParamters)
			{
				animator.IntParameters.Add(intP);
			}

			animator.FloatParamaters = new List<FloatParameter>();
			foreach (var floatP in old.FloatParamaters)
			{
				animator.FloatParamaters.Add(floatP);
			}

			animator.TriggersNames = new List<string>();
			foreach (var triggerP in old.TriggersNames)
			{
				animator.TriggersNames.Add(triggerP);
			}

			// secondary layers
			animator.SecondaryLayers = new List<SecondaryLayerData>();

			for (int sLayerIdx = 0; sLayerIdx < old.SecondaryLayers.Count; sLayerIdx++)
			{
				animator.SecondaryLayers.Add(old.SecondaryLayers[sLayerIdx]);
			}

			// layers
			animator.Layers = new List<MotionMatchingLayer_SO>();

			for (int layerIndex = 0; layerIndex < old.layers.Count; layerIndex++)
			{
				MotionMatchingLayer oldLayer = old.layers[layerIndex];
				MotionMatchingLayer_SO newLayer = animator.AddLayer(oldLayer.name, null);

				newLayer.PassIK = oldLayer.passIK;
				newLayer.FootPassIK = oldLayer.footPassIK;
				newLayer.AvatarMask = oldLayer.avatarMask;
				newLayer.IsAdditive = oldLayer.isAdditive;

				Dictionary<string, int> oldSequences = new Dictionary<string, int>();
				for (int sequenceIdx = 0; sequenceIdx < oldLayer.sequences.Count; sequenceIdx++)
				{
					oldSequences.Add(oldLayer.sequences[sequenceIdx], sequenceIdx);
				}

				// sequences:
				newLayer.Sequences = new List<SequenceDescription>();
				foreach (var seq in oldLayer.sequences)
				{
					newLayer.AddSequence(seq);
				}


				Dictionary<int, MotionMatchingNode> oldNodes = new Dictionary<int, MotionMatchingNode>();

				for (int nodeIdx = 0; nodeIdx < oldLayer.nodes.Count; nodeIdx++)
				{
					oldNodes.Add(oldLayer.nodes[nodeIdx].ID, oldLayer.nodes[nodeIdx]);
				}


				newLayer.States = new List<State_SO>();

				// creating states:
				for (int stateIndex = 0; stateIndex < oldLayer.states.Count; stateIndex++)
				{
					MotionMatchingState oldState = oldLayer.states[stateIndex];

					State_SO newState = null;

					switch (oldState.stateType)
					{
						case MotionMatchingStateType.MotionMatching:
							{
								var mmState = newLayer.AddState<MotionMatchingState_SO>();
								mmState.Features = oldLayer.m_MotionMatchingStateFeatures[oldState.StateFeaturesIndex];

								newState = mmState;
							}
							break;
						case MotionMatchingStateType.SingleAnimation:
							{
								var saState = newLayer.AddState<SingleAnimationState_SO>();
								saState.Features = oldLayer.m_SingleAnimationStateFeatures[oldState.StateFeaturesIndex];

								newState = saState;
							}
							break;
						case MotionMatchingStateType.ContactAnimationState:
							{
								var cState = newLayer.AddState<ContactState_SO>();
								cState.Features = oldLayer.m_ContactStateFeatures[oldState.StateFeaturesIndex];

								newState = cState;
							}
							break;
					}

					newState.Node = new StateNode(oldNodes[oldState.nodeID].rect.position / 2f);
					newState.Name = oldState.Name;
					newState.TrajectoryCorrection = oldState.TrajectoryCorrection;
					newState.Index = stateIndex;
					newState.Tag = oldState.Tag;
					newState.StartSection = oldState.StartSection;
					newState.MotionData = oldState.MotionData;
					newState.SpeedMultiplier = oldState.SpeedMultiplier;

					var oldStateSequence = oldNodes[oldState.nodeID].Sequence;

					newState.SequenceID = newLayer.Sequences[oldSequences[oldStateSequence]].ID;
				}

				// creating portals:

				int portalIndex = 0;
				Dictionary<int, int> portalsIndexes = new Dictionary<int, int>(); // key id, value index
				for (int nodeIndex = 0; nodeIndex < oldLayer.nodes.Count; nodeIndex++)
				{
					MotionMatchingNode node = oldLayer.nodes[nodeIndex];

					if (node.nodeType != MotionMatchingNodeType.Portal) continue;

					PortalToState portal = newLayer.CreatePortal(node.rect.position / 2f);

					portal.State = newLayer.States[node.stateIndex];
					portalsIndexes.Add(node.ID, portalIndex);
					portal.SequenceID = newLayer.Sequences[oldSequences[node.Sequence]].ID;

					portalIndex += 1;
				}

				// setup start state:
				newLayer.StartStateData = new StartStateData();
				newLayer.StartStateData.StartState = newLayer.States[oldLayer.startStateIndex];
				newLayer.StartStateData.StartClipIndex = oldLayer.StartClipIndex;
				newLayer.StartStateData.StartClipTime = oldLayer.StartClipTime;

				// creating transition:

				for (int stateIndex = 0; stateIndex < oldLayer.states.Count; stateIndex++)
				{
					MotionMatchingState oldState = oldLayer.states[stateIndex];
					State_SO newState = newLayer.States[stateIndex];

					newState.Transitions = new List<Transition>();

					for (int transitinoIdx = 0; transitinoIdx < oldState.Transitions.Count; transitinoIdx++)
					{
						Transition oldTransition = oldState.Transitions[transitinoIdx];

						Transition newTransition = new Transition();
						newTransition.nextStateIndex = oldTransition.nextStateIndex;
						newTransition.FromState = newState;
						newTransition.ToState = newLayer.States[oldTransition.nextStateIndex];
						if (oldTransition.toPortal)
						{
							newTransition.PortalToStateIndex = portalsIndexes[oldTransition.nodeID];
						}

						foreach (var tOption in oldTransition.options)
						{
							newTransition.options.Add(tOption);
						}

						newState.Transitions.Add(newTransition);
					}
				}
			}


			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
#endif
		#endregion
	}
}