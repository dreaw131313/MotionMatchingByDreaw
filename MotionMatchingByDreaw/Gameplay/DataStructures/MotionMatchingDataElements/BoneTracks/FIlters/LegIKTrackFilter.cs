using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName ="LegIKTrackFilter", menuName = "Motion Matching/Data/Tracks/Filters/Leg IK Filter")]
	public class LegIKTrackFilter : BoneTrackFilter
	{
		[SerializeField]
		float minYDeltaToRoot = 0.15f;

		[SerializeField]
		bool useVelocity = false;
		[SerializeField]
		[Min(0f)]
		private float minBoneVel;

		[SerializeField]
		private float minIntervalLength = 0.2f;

		public override bool CheckFilter(GameObject gameObject, Transform bone, Vector3 boneVelocity)
		{
			float deltaY = bone.position.y - gameObject.transform.position.y;

			if (useVelocity)
			{
				float speed = boneVelocity.magnitude;
				if(deltaY <= minYDeltaToRoot && speed <= minBoneVel)
				{
					return true;
				}
			}
			else if (deltaY <= minYDeltaToRoot)
			{
				return true;
			}

			return false;
		}

		public override BoneTrackData EditData(BoneTrackData boneData)
		{
			return boneData;
		}

		public override void EditIntervals(ref List<BoneTrackInterval> intervals)
		{
			for (int i = 0; i < intervals.Count; i++)
			{
				float2 interval = intervals[i].TimeInterval;

				if(interval.y - interval.x < minIntervalLength)
				{
					intervals.RemoveAt(i);
					i--;
				}
			}
		}
	}
}