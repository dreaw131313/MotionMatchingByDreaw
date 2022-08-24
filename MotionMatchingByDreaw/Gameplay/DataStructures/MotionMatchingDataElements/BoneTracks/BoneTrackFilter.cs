using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public abstract class BoneTrackFilter : ScriptableObject
	{
		public abstract bool CheckFilter(GameObject gameObject, Transform bone, Vector3 boneVelocity);

		public abstract BoneTrackData EditData(BoneTrackData boneData);

		public abstract void EditIntervals(ref List<BoneTrackInterval> intervals);
	}
}