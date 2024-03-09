using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public class MotionMatchingLayer_SO : ScriptableObject
	{
		// Serialized values
		[SerializeField]
		public string Name;
		[SerializeField]
		public int Index;
		[SerializeField]
		public AvatarMask AvatarMask = null;

		[SerializeField]
		public StartStateData StartStateData;

		[SerializeField]
		public bool PassIK;
		[SerializeField]
		public bool FootPassIK = false;
		[SerializeField]
		public bool IsAdditive = false;
		[SerializeField]
		public List<State_SO> States;

		// Runtime:
		public Dictionary<string, int> StateIndexes;

		public void Initialize()
		{
			if (StateIndexes == null)
			{
				StateIndexes = new Dictionary<string, int>();
				for (int i = 0; i < States.Count; i++)
				{
					StateIndexes.Add(States[i].Name, i);
				}
			}
		}


		public State_SO GetStartState()
		{
			if (StartStateData != null)
			{
				return StartStateData.StartState;
			}
			return null;
		}

		public LogicMotionMatchingLayer CreateLogicLayer(MotionMatchingComponent motionMatching, PlayableAnimationSystem playableAnimationSystem)
		{
			return new LogicMotionMatchingLayer(this, motionMatching, playableAnimationSystem);
		}

		public int GetPoseBonesCount()
		{
			return States[0].GetPoseBonesCount();
		}

#if UNITY_EDITOR

		[SerializeField]
		public List<PortalToState> Portals;

		[SerializeField]
		public bool Fold;

		[SerializeField]
		public Vector2 MoveOffset;


		[SerializeField]
		private int SequenceIDGenerator = 0;
		[SerializeField]
		public int SelectedSequenceIndex = 0;
		[SerializeField]
		private List<SequenceDescription> sequences;

		public List<SequenceDescription> Sequences
		{
			get
			{
				if (sequences == null)
				{
					sequences = new List<SequenceDescription>();
				}
				if (sequences.Count == 0)
				{
					AddSequence("Default");
					SelectedSequenceIndex = 0;
				}
				return sequences;
			}
		}

		public StateClass AddState<StateClass>()
			where StateClass : State_SO
		{
			StateClass state = CreateInstance<StateClass>();
			string stateName = typeof(StateClass).Name;

			stateName = MakeNewUniqueStateName(stateName);
			state.Name = stateName;
			state.name = typeof(StateClass).Name;
			state.Index = States.Count;

			States.Add(state);
			AssetDatabase.AddObjectToAsset(state, this);

			EditorUtility.SetDirty(this);
			EditorUtility.SetDirty(state);

			return state;
		}

		public bool RemoveState(string stateName)
		{
			for (int i = 0; i < States.Count; i++)
			{
				State_SO state = States[i];

				if (state.Name != stateName)
				{
					AssetDatabase.RemoveObjectFromAsset(state);
					ScriptableObject.DestroyImmediate(state);

					States.RemoveAt(i);

					ValidateStatesIndexes();
					ValidateStateTransitions();

					EditorUtility.SetDirty(this);
					return true;
				}
			}

			return false;
		}

		public bool RemoveState(State_SO stateToRemove)
		{
			for (int i = 0; i < States.Count; i++)
			{
				State_SO state = States[i];

				if (state == stateToRemove)
				{
					RemoveTransitionsToState(stateToRemove);
					RemoveAllPortalsToState(stateToRemove);

					AssetDatabase.RemoveObjectFromAsset(state);
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(state));

					States.RemoveAt(i);

					ValidateStatesIndexes();
					ValidateStateTransitions();

					EditorUtility.SetDirty(this);
					return true;
				}
			}

			return false;
		}

		public string[] GetStatesNames()
		{
			if (States == null || States.Count == 0) return null;

			string[] names = new string[States.Count];

			for (int i = 0; i < States.Count; i++)
			{
				names[i] = States[i].Name;
			}

			return names;
		}

		public string MakeNewUniqueStateName(string newStateName)
		{
			if (States != null)
			{
				string newName = newStateName;

				int index = 0;

				for (int i = 0; i < States.Count; i++)
				{
					if (States[i].Name == newName)
					{
						i = 0;
						index += 1;
						newName = $"{newStateName}_({index})";
					}
				}
				return newName;
			}
			else
			{
				return newStateName;
			}
		}

		public string MakeStateNameUniqueForState(State_SO forState, string newStateName)
		{
			if (newStateName != null)
			{
				string newName = newStateName;

				int index = 0;

				for (int i = 0; i < States.Count; i++)
				{
					if (States[i] != forState && States[i].Name == newName)
					{
						i = 0;
						index += 1;
						newName = $"{newStateName}_({index})";
					}
				}

				return newName;
			}
			else
			{
				return newStateName;
			}
		}

		public void ValidateStatesIndexes()
		{
			if (States != null)
			{
				for (int i = 0; i < States.Count; i++)
				{
					States[i].Index = i;
				}
			}
		}

		public void ValidateStateTransitions()
		{
			for (int i = 0; i < States.Count; i++)
			{
				States[i].ValidateTransitions();
				EditorUtility.SetDirty(States[i]);
			}
		}

		private void RemoveTransitionsToState(State_SO stateToRemove)
		{
			foreach (var state in States)
			{
				state.RemoveTransition(stateToRemove);
			}
		}

		public void SetStartState(State_SO state)
		{
			if (StartStateData == null) StartStateData = new StartStateData();

			if (state == StartStateData.StartState) return;

			StartStateData.StartState = state;
			StartStateData.StartClipIndex = 0;
			StartStateData.StartClipTime = 0;
		}

		public void RemoveTransition(Transition transitionToRemove)
		{
			foreach (var state in States)
			{
				if (state.Transitions == null) return;

				state.RemoveTransition(transitionToRemove);
			}
		}

		public PortalToState CreatePortal(Vector2 position)
		{
			PortalToState portal = new PortalToState();
			portal.Node = new StateNode(position);

			if (Portals == null) Portals = new List<PortalToState>();

			Portals.Add(portal);

			return portal;
		}

		public void RemovePortal(PortalToState portal)
		{
			if (portal == null || Portals == null) return;

			int portalIndex = Portals.IndexOf(portal);
			if (portalIndex < 0) return;

			RemoveTransitionsToPortal(portal, portalIndex);

			Portals.RemoveAt(portalIndex);
		}

		private void RemovePortal(int portalIndex)
		{
			if (Portals == null) return;
			if (0 <= portalIndex && portalIndex < Portals.Count)
			{
				PortalToState portal = Portals[portalIndex];
				RemoveTransitionsToPortal(portal, portalIndex);

				Portals.RemoveAt(portalIndex);
			}
		}

		private void RemoveTransitionsToPortal(PortalToState portal, int portalIndex)
		{
			foreach (var state in States)
			{
				state.RemoveTranstionToPortal(portal, portalIndex);
			}
		}

		private void RemoveAllPortalsToState(State_SO state)
		{
			if (Portals == null) return;
			for (int portalIndex = 0; portalIndex < Portals.Count; portalIndex++)
			{
				PortalToState portal = Portals[portalIndex];
				if (portal.State == state)
				{
					RemovePortal(portalIndex);
					portalIndex -= 1;
				}
			}
		}

		public bool AddSequence(string newSequenceName)
		{
			foreach (var seq in sequences)
			{
				if (seq.Name == newSequenceName)
				{
					return false;
				}
			}

			SequenceIDGenerator += 1;
			sequences.Add(new SequenceDescription(newSequenceName, SequenceIDGenerator));
			return true;
		}

		public bool RemoveSequence(string sequence)
		{
			if (Sequences == null) return false;

			for (int i = 0; i < Sequences.Count; i++)
			{
				var seq = Sequences[i];
				if (seq.Name == sequence)
				{
					Sequences.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public int GetSequenceIndex(string sequence)
		{
			for (int i = 0; i < Sequences.Count; i++)
			{
				var seq = Sequences[i];
				if (seq.Name == sequence)
				{
					return i;
				}
			}

			return -1;
		}

		public bool IsSequenceEmpty(string sequence)
		{
			int seqIndex = GetSequenceIndex(sequence);
			if(seqIndex == -1)
			{
				return true;
			}

			int sequenceID = Sequences[seqIndex].ID;

			foreach (var state in States)
			{
				if(state.SequenceID == sequenceID)
				{
					return false;
				}
			}

			return true;
		}
#endif



	}

	[System.Serializable]
	public class StartStateData
	{
		[SerializeField]
		public State_SO StartState;
		[SerializeField]
		public int StartClipIndex;
		[SerializeField]
		public float StartClipTime;
	}

	[System.Serializable]
	public class SequenceDescription
	{
		public string Name = "Default";
		public int ID;

		public SequenceDescription(string sequenceName, int id)
		{
			Name = sequenceName;
			ID = id;
		}
	}
}