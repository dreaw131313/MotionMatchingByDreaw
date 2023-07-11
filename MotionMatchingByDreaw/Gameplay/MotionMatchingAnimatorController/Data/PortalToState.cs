using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class PortalToState
	{
		[SerializeField]
		public State_SO State;
		[SerializeField]
		public StateNode Node;

		[SerializeField]
		public int SequenceID = 0;
	}
}