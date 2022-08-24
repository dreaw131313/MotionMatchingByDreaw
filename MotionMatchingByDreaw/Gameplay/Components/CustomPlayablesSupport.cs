using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public abstract class CustomPlayablesSupport : MonoBehaviour
	{
		public abstract Playable[] CreateCustomPlayables(Animator animator, ref PlayableGraph graph);
	}
}