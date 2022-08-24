using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class MotionMatchingLayer
	{
		// Serialized values
		[SerializeField]
		public string name;
		[SerializeField]
		public int index;
		[SerializeField]
		public AvatarMask avatarMask = null;
		[SerializeField]
		public int startStateIndex = -1;
		[SerializeField]
		public int StartClipIndex;
		[SerializeField]
		public float StartClipTime;
		[SerializeField]
		public bool passIK;
		[SerializeField]
		public bool footPassIK = false;
		[SerializeField]
		public bool isAdditive = false;
		[SerializeField]
		public List<MotionMatchingState> states = new List<MotionMatchingState>();

		[SerializeField]
		public List<MotionMatchingStateFeatures> m_MotionMatchingStateFeatures;
		[SerializeField]
		public List<SingleAnimationStateFeatures> m_SingleAnimationStateFeatures;
		[SerializeField]
		public List<ContactStateFeatures> m_ContactStateFeatures;

		public Dictionary<string, int> StateIndexes;

		public MotionMatchingLayer(string name, int index)
		{
			this.name = name;
			this.index = index;

#if UNITY_EDITOR
			fold = true;
			zoom = 1f;
			sequences.Add("Default");
#endif
		}

		public MotionMatchingState GetStateWithName(string name)
		{
			for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
			{
				if (states[stateIndex].Name == name)
				{
					return states[stateIndex];
				}
			}
			return null;
		}

		public int GetSingleTrajectoryPointsCount()
		{
			foreach (MotionMatchingState state in states)
			{
				int pointsCount = 0;
				if (state != null)
				{
					pointsCount = state.GetTrajectoryPointsCount();
				}
				if (pointsCount != 0)
				{
					return pointsCount;
				}
			}

			return 0;
		}

		public int GetPoseBonesCount()
		{
			foreach (MotionMatchingState state in states)
			{
				int bonesCount = 0;
				if (state != null)
				{
					bonesCount = state.GetPoseBonesCount();
				}
				if (bonesCount != 0)
				{
					return bonesCount;
				}
			}

			return 0;
		}

		public void Initialize()
		{
			if (StateIndexes == null)
			{
				StateIndexes = new Dictionary<string, int>();
				for (int i = 0; i < states.Count; i++)
				{
					StateIndexes.Add(states[i].Name, i);
				}
			}
		}

		public string[] GetStatesNames()
		{
			if (states == null || states.Count == 0) return null;

			string[] names = new string[states.Count];

			for (int i = 0; i < states.Count; i++)
			{
				names[i] = states[i].Name;
			}

			return names;
		}

#if UNITY_EDITOR
		[SerializeField]
		public bool fold;
		[SerializeField]
		public float zoom;
		[SerializeField]
		public List<MotionMatchingNode> nodes = new List<MotionMatchingNode>();
		[SerializeField]
		public List<string> sequences = new List<string>();

		private static float nodeH = 80f;
		private static float nodeW = 250f;

		// Adding and removing state
		public void AddState(
			string stateName,
			MotionMatchingStateType type,
			int stateID,
			Vector2 nodePosition,
			string sequenceName
			)
		{
			string newName = stateName;
			int counter = 0;
			if (states == null)
			{
				states = new List<MotionMatchingState>();
			}
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].Name == newName)
				{
					counter++;
					newName = stateName + counter.ToString();
					i = 0;
				}
			}
			int stateIndex = states.Count;

			int featuresIndex = 0;
			switch (type)
			{
				case MotionMatchingStateType.MotionMatching:
					{
						if (m_MotionMatchingStateFeatures == null)
						{
							m_MotionMatchingStateFeatures = new List<MotionMatchingStateFeatures>();
						}

						featuresIndex = m_MotionMatchingStateFeatures.Count;
						m_MotionMatchingStateFeatures.Add(new MotionMatchingStateFeatures());
					}
					break;
				case MotionMatchingStateType.SingleAnimation:
					{
						if (m_SingleAnimationStateFeatures == null)
						{
							m_SingleAnimationStateFeatures = new List<SingleAnimationStateFeatures>();
						}

						featuresIndex = m_SingleAnimationStateFeatures.Count;
						m_SingleAnimationStateFeatures.Add(new SingleAnimationStateFeatures());
					}
					break;
				case MotionMatchingStateType.ContactAnimationState:
					{
						if (m_ContactStateFeatures == null)
						{
							m_ContactStateFeatures = new List<ContactStateFeatures>();
						}

						featuresIndex = m_ContactStateFeatures.Count;
						m_ContactStateFeatures.Add(new ContactStateFeatures());
					}
					break;
			}

			states.Add(new MotionMatchingState(newName, type, stateIndex, stateID, featuresIndex));
			if (nodes == null)
			{
				nodes = new List<MotionMatchingNode>();
			}
			string title = "";
			switch (type)
			{
				case MotionMatchingStateType.MotionMatching:
					title = "Motion Matching:";
					break;
				case MotionMatchingStateType.SingleAnimation:
					title = "Single Animation:";
					break;
				case MotionMatchingStateType.ContactAnimationState:
					title = "Contact State:";
					break;
			}
			nodes.Add(new MotionMatchingNode(
				new Rect(nodePosition, new Vector2(nodeW, nodeH)),
				type == MotionMatchingStateType.ContactAnimationState ? MotionMatchingNodeType.Contact : MotionMatchingNodeType.State,
				title,
				stateID,
				stateIndex,
				sequenceName
				));
		}

		public string MakeStateNameUnique(string stateName, int stateIndexToExclude)
		{
			string[] statesNames = GetStatesNames();

			if (stateName != null)
			{
				string newName = stateName;

				int index = 0;

				for (int i = 0; i < statesNames.Length; i++)
				{
					if (statesNames[i] == newName && i != stateIndexToExclude)
					{
						i = 0;
						index += 1;
						newName = $"{stateName}_({index})";
					}
				}

				return newName;
			}
			else
			{
				return stateName;
			}
		}

		private void RemoveStateAndClear(int stateIndex)
		{
			int removedFeaturesIndex = states[stateIndex].StateFeaturesIndex;
			MotionMatchingStateType stateType = states[stateIndex].stateType;

			foreach (MotionMatchingState state in states)
			{
				if (state.Index == stateIndex)
				{
					continue;
				}

				state.UpdateTransitinoAfterStateRemove(stateIndex);

				// removing features:
				if (state.stateType == stateType && state.StateFeaturesIndex > removedFeaturesIndex)
				{
					state.StateFeaturesIndex = state.StateFeaturesIndex - 1;
				}
			}

			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes[i].stateIndex == stateIndex)
				{
					nodes.RemoveAt(i);
					i--;
				}
			}
			states.RemoveAt(stateIndex);
			switch (stateType)
			{
				case MotionMatchingStateType.MotionMatching:
					{
						m_MotionMatchingStateFeatures.RemoveAt(removedFeaturesIndex);
					}
					break;
				case MotionMatchingStateType.SingleAnimation:
					{
						m_SingleAnimationStateFeatures.RemoveAt(removedFeaturesIndex);
					}
					break;
				case MotionMatchingStateType.ContactAnimationState:
					{
						m_ContactStateFeatures.RemoveAt(removedFeaturesIndex);
					}
					break;
			}
		}

		private void RemakeStateIndexes(int removedState)
		{
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].Index == this.startStateIndex)
				{
					this.startStateIndex = i;
				}
				states[i].Index = i;
				for (int j = 0; j < nodes.Count; j++)
				{
					if (nodes[j].ID == states[i].nodeID)
					{
						nodes[j].stateIndex = i;
					}
				}

				foreach (Transition t in states[i].Transitions)
				{
					t.fromStateIndex = i;
				}
			}
		}

		public bool RemoveState(int stateIndex)
		{
			if (0 <= stateIndex && stateIndex < states.Count)
			{
				RemoveStateAndClear(stateIndex);
				RemakeStateIndexes(stateIndex);

				if (startStateIndex == stateIndex)
				{
					startStateIndex = GetIndexOfFirstStateDiffrentType(MotionMatchingStateType.ContactAnimationState);
				}

				foreach (MotionMatchingNode node in nodes)
				{
					if (node.nodeType == MotionMatchingNodeType.Portal && stateIndex <= node.stateIndex)
					{
						node.stateIndex -= 1;
					}
				}

				return true;
			}

			return false;
		}

		public string GetStateName(int index)
		{
			return states[index].Name;
		}

		public MotionMatchingStateType GetStateType(int index)
		{
			return states[index].stateType;
		}

		// Making and removing transition between states
		public bool MakeTransition(int fromStateIndex, int toStateIndex, int nodeID, bool isPortal)
		{
			if (fromStateIndex >= 0 &&
				fromStateIndex < states.Count &&
				toStateIndex >= 0 &&
				toStateIndex < states.Count &&
				fromStateIndex != toStateIndex)
			{
				foreach (Transition t in states[fromStateIndex].Transitions)
				{
					if (t.nextStateIndex == toStateIndex)
					{
						//Debug.Log("cos nie tak");
						return false;
					}
				}
				if (!isPortal)
				{
					foreach (Transition t in states[toStateIndex].Transitions)
					{
						if (t.nextStateIndex == fromStateIndex && !t.toPortal)
						{
							//Debug.Log("cos nie tak 2");
							return false;
						}
					}
				}
				states[fromStateIndex].AddTransition(states[toStateIndex], nodeID, isPortal);
				return true;
			}
			return false;
		}

		public bool MakeTransition(string fromState, string toState, int stateID, bool isPortal)
		{
			int f = -1;
			int t = -1;
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].Name == fromState)
				{
					f = i;
				}
				if (states[i].Name == toState)
				{
					t = i;
				}
			}

			return MakeTransition(f, t, stateID, isPortal);
		}

		public bool RemoveTransition(int fromStateIndex, int toStateIndex)
		{
			if (fromStateIndex >= 0 && fromStateIndex < states.Count && toStateIndex >= 0 && toStateIndex < states.Count)
			{
				for (int i = 0; i < states[fromStateIndex].Transitions.Count; i++)
				{
					if (states[fromStateIndex].Transitions[i].nextStateIndex == toStateIndex)
					{
						states[fromStateIndex].Transitions.RemoveAt(i);
						i--;
					}
				}

				return true;
			}
			return false;
		}

		public bool RemoveTransition(string fromState, string toState)
		{
			int f = -1;
			int t = -1;
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].Name == fromState)
				{
					f = i;
				}
				if (states[i].Name == toState)
				{
					t = i;
				}
			}

			return RemoveTransition(f, t);
		}

		public void UpdateTransitionOptions(int stateIndex)
		{
			if (states[stateIndex].stateType == MotionMatchingStateType.SingleAnimation)
			{
				foreach (MotionMatchingState state in states)
				{
					if (state.Index == stateIndex)
					{
						continue;
					}

					//state.UpdateTransitions(state);
				}
			}
		}

		public void UpdateAllTransitionsOptions()
		{
			for (int i = 0; i < states.Count; i++)
			{
				UpdateTransitionOptions(i);
			}
		}

		// Portal state
		public void AddPortal(Vector2 nodePosition, int nodeID, string sequenceName)
		{
			nodes.Add(new MotionMatchingNode(
				new Rect(nodePosition, new Vector2(nodeW, nodeH)),
				MotionMatchingNodeType.Portal,
				"Portal:",
				nodeID,
				-1,
				sequenceName
				));
		}

		public bool SetPortalState(int portalNodeIndex, int portalStateIndex)
		{
			if (nodes[portalNodeIndex].nodeType != MotionMatchingNodeType.Portal)
			{
				return false;
			}

			nodes[portalNodeIndex].stateIndex = portalStateIndex;

			foreach (MotionMatchingState state in states)
			{
				bool updateT = false;
				foreach (Transition t in state.Transitions)
				{
					if (t.toPortal && t.nodeID == nodes[portalNodeIndex].ID)
					{
						t.nextStateIndex = portalStateIndex;
						updateT = true;
						break;
					}
				}
				if (updateT)
				{
					//state.UpdateTransitions();
				}
			}

			return false;
		}

		public bool SetPortalState2(int portalNodeID, int portalStateIndex)
		{
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes[i].ID == portalNodeID)
				{
					SetPortalState(i, portalStateIndex);
					return true;
				}
			}

			return false;
		}

		public bool RemovePortal(int portalNodeID)
		{
			int portalIndex = 0;
			for (int i = 0; i < nodes.Count; i++)
			{
				if (portalNodeID == nodes[i].ID)
				{
					portalIndex = i;
					break;
				}
			}
			if (nodes[portalIndex].nodeType != MotionMatchingNodeType.Portal)
			{
				return false;
			}
			foreach (MotionMatchingState s in states)
			{
				for (int i = 0; i < s.Transitions.Count; i++)
				{
					if (s.Transitions[i].nodeID == nodes[portalIndex].ID)
					{
						s.Transitions.RemoveAt(i);
						i--;
					}
				}
			}

			nodes.RemoveAt(portalIndex);
			return true;
		}

		public int GetMaxNodeID()
		{
			int maxID = 0;
			foreach (MotionMatchingNode n in nodes)
			{
				if (n.ID > maxID)
				{
					maxID = n.ID;
				}
			}
			return maxID;
		}

		// Setting Layer options
		public bool SetStartState(int startStateIndex)
		{
			if (startStateIndex == this.startStateIndex ||
				startStateIndex < 0 ||
				startStateIndex >= states.Count)
			{
				return false;
			}

			this.startStateIndex = startStateIndex;

			return false;
		}

		public void MoveNodeOnTop(int index)
		{
			nodes.Add(nodes[index]);
			nodes.RemoveAt(index);
		}

		public int GetStateIndexEditorOnly(string stateName)
		{
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].Name == stateName)
				{
					return i;
				}
			}
			return -1;
		}

		public int GetIndexOfFirstStateType(MotionMatchingStateType type)
		{
			for (int i = 0; i < this.states.Count; i++)
			{
				if (this.states[i].stateType == type)
				{
					return i;
				}
			}
			return -1;
		}

		public int GetIndexOfFirstStateDiffrentType(MotionMatchingStateType type)
		{
			for (int i = 0; i < this.states.Count; i++)
			{
				if (this.states[i].stateType != type)
				{
					return i;
				}
			}
			return -1;
		}

		public bool AddSequence(string name)
		{
			if (!sequences.Contains(name) && !name.Equals(""))
			{
				sequences.Add(name);
				return true;
			}
			return false;
		}

		public bool RemoveSequence(string name)
		{
			if (sequences.Count <= 1)
			{
				return false;
			}

			int sequenceIndex = 0;
			for (int i = 0; i < sequences.Count; i++)
			{
				if (sequences[i].Equals(name))
				{
					sequenceIndex = i;
				}
			}
			if (sequences.Contains(name))
			{
				sequences.Remove(name);

				string beforeDeletedSequenceName = sequences[Mathf.Clamp(sequenceIndex - 1, 0, sequences.Count - 1)];

				Vector2 startPosition = Vector2.zero;
				Vector2 smallestPos = Vector2.zero;

				foreach (MotionMatchingNode n in nodes)
				{
					if (n.Sequence.Equals(beforeDeletedSequenceName))
					{
						if (n.rect.x < startPosition.x)
						{
							startPosition.x = n.rect.x;
						}
						if (n.rect.y > startPosition.y)
						{
							startPosition.y = n.rect.y;
						}
					}

					if (n.Sequence.Equals(name))
					{
						if (n.rect.x < smallestPos.x)
						{
							smallestPos.x = n.rect.x;
						}
						if (n.rect.y < smallestPos.y)
						{
							smallestPos.y = n.rect.y;
						}
					}
				}

				Vector2 delta = startPosition - smallestPos;

				foreach (MotionMatchingNode n in nodes)
				{
					if (n.Sequence.Equals(name))
					{
						n.Move(delta);
						n.Sequence = beforeDeletedSequenceName;
					}
				}


				return true;
			}
			return false;
		}

		public void RenameSequence(string oldName, string newName)
		{
			for (int i = 0; i < sequences.Count; i++)
			{
				if (sequences[i] == oldName)
				{
					sequences[i] = newName;
					break;
				}
			}

			foreach (MotionMatchingNode n in nodes)
			{
				if (n.Sequence.Equals(oldName))
				{
					n.Sequence = newName;
				}
			}
		}


#endif
	}

#if UNITY_EDITOR
	public enum MotionMatchingNodeType
	{
		State,
		Portal,
		Contact,

	}

	[System.Serializable]
	public class MotionMatchingNode
	{
		[SerializeField]
		public Rect rect;
		[SerializeField]
		public Rect input;
		[SerializeField]
		public Rect output;
		[SerializeField]
		public MotionMatchingNodeType nodeType;
		[SerializeField]
		public int ID;
		[SerializeField]
		public int stateIndex;
		[SerializeField]
		public string title;
		[SerializeField]
		public string Sequence = "Default";

		public static float pointsW = 15f;
		public static float pointsH = 25f;
		public static float pointsMoveFactor = 0.5f;

		public MotionMatchingNode(
			Rect rect, 
			MotionMatchingNodeType nodeType, 
			string title, 
			int nodeID, 
			int stateIndex, 
			string sequenceName
			)
		{
			Sequence = sequenceName;

			this.rect = rect;
			this.nodeType = nodeType;
			this.ID = nodeID;
			this.stateIndex = stateIndex;
			input = new Rect(
					rect.x - pointsW + 0.5f * pointsW,
					rect.y + rect.height / 2f - pointsH / 2f,
					pointsW,
					pointsH
				);
			output = new Rect(
					rect.x + rect.width - 0.5f * pointsW,
					rect.y + rect.height / 2f - pointsH / 2f,
					pointsW,
					pointsH
				);
			this.title = title;
		}

		public void Move(Vector2 delta)
		{
			rect.position += delta;
			input.Set(
					rect.x - pointsW + 0.5f * pointsW,
					rect.y + rect.height / 2f - pointsH / 2f,
					pointsW,
					pointsH
				);
			output.Set(
					rect.x + rect.width - 0.5f * pointsW,
					rect.y + rect.height / 2f - pointsH / 2f,
					pointsW,
					pointsH
				);
		}

		public void Draw(
			bool startNode,
			bool isSelected,
			string name,
			GUIStyle selected,
			GUIStyle start,
			GUIStyle normal,
			GUIStyle portal,
			GUIStyle contact,
			GUIStyle inputS,
			GUIStyle outputS,
			GUIStyle textStyle
			)
		{
			if (isSelected)
			{
				float incresing = 3f;
				Rect selectionRect = new Rect(
					this.rect.x - incresing,
					this.rect.y - incresing,
					this.rect.width + 2 * incresing,
					this.rect.height + 2 * incresing
					);
				GUI.Box(selectionRect, "", selected);
			}

			switch (nodeType)
			{
				case MotionMatchingNodeType.State:
					GUI.Box(this.input, "", inputS);
					GUI.Box(this.output, "", outputS);

					GUIStyle stateStyle;
					if (startNode)
					{
						stateStyle = start;
					}
					else
					{
						stateStyle = normal;
					}
					GUILayout.BeginArea(this.rect, stateStyle);
					GUILayout.Space(rect.height / 6f);
					GUILayout.BeginHorizontal();
					GUILayout.Label(title, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label(name, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.EndArea();
					break;
				case MotionMatchingNodeType.Portal:
					GUI.Box(this.input, "", inputS);
					GUILayout.BeginArea(this.rect, portal);
					GUILayout.Space(rect.height / 6f);
					GUILayout.BeginHorizontal();
					GUILayout.Label(title, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label(name, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.EndArea();
					break;
				case MotionMatchingNodeType.Contact:
					GUI.Box(this.output, "", outputS);
					GUILayout.BeginArea(this.rect, contact);
					GUILayout.Space(rect.height / 6f);
					GUILayout.BeginHorizontal();
					GUILayout.Label(title, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label(name, textStyle);
					GUILayout.EndHorizontal();
					GUILayout.EndArea();
					break;
			}

		}
	}
#endif
}
