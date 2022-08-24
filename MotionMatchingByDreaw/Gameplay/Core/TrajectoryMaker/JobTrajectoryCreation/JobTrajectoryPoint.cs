using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public struct JobTrajectoryPoint
	{
		public float StepTime; // time between points
		public float TimeFactor;
		public float3 Position;
		public float3 Velocity;
		public float3 Orientation;

		public JobTrajectoryPoint(float stepTime, float timeFactor, float3 position, float3 velocity, float3 orientation)
		{
			StepTime = stepTime;
			TimeFactor = timeFactor;
			Position = position;
			Velocity = velocity;
			Orientation = orientation;
		}
	}


	public struct NativeTrajectoryCreator
	{

		public static float CalculateTimeFactor(float time, float maxTime)
		{
			float percentage = time / maxTime;
			float doublePercentage = percentage * percentage;
			float factor = doublePercentage * doublePercentage +
							doublePercentage * percentage +
							doublePercentage;

			return factor;
		}

		public static void CreateNativeTrajector(
			NativeArray<JobTrajectoryPoint> trajectoryPoints,
			float3 objectPosition,
			ref float3 lastObjectPosition,
			float3 objectForward,
			float3 strafeForward,
			float3 desiredVelocity,
			float acceleration,
			float stiffnes,
			float bias,
			bool strafe
			)
		{

			float3 lastPositionDelta = objectPosition - lastObjectPosition;
			lastObjectPosition = objectPosition;

			int lastIndex = trajectoryPoints.Length - 1;
			float lastPointFactor = trajectoryPoints[lastIndex].TimeFactor;
			float3 pointLastDelta = float3.zero;


			for (int i = 0; i < trajectoryPoints.Length; i++)
			{
				JobTrajectoryPoint point = trajectoryPoints[i];
				point.Position += lastPositionDelta;
				float3 currentDelta;

				if (i == 0)
				{
					currentDelta = point.Position - objectPosition;
				}
				else
				{
					currentDelta = point.Position - trajectoryPoints[i - 1].Position + pointLastDelta;
				}

				float finalFactor = Mathf.Lerp(point.TimeFactor, lastPointFactor, stiffnes);
				finalFactor = (1f + bias * finalFactor) * acceleration * point.StepTime;

				float3 desiredPointDeltaPosition = desiredVelocity * point.StepTime;

				float3 finalDelta = float3Extension.MoveFloat3WithSpeed(currentDelta, desiredPointDeltaPosition, finalFactor, Time.deltaTime);

				if (i == 0)
				{
					float3 newPos = objectPosition + finalDelta;
					pointLastDelta = newPos - point.Position;
					point.Position = newPos;


					point.Velocity = (point.Position - objectPosition) / point.StepTime;
					point.Orientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						point.Position,
						objectPosition
						);
				}
				else
				{
					JobTrajectoryPoint previewPoint = trajectoryPoints[i - 1];

					float3 newPos = previewPoint.Position + finalDelta;
					pointLastDelta = newPos - point.Position;

					point.Position = newPos;
					point.Velocity = (point.Position - previewPoint.Position) / point.StepTime;
					point.Orientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						point.Position,
						previewPoint.Position
						);
				}

				trajectoryPoints[i] = point;
			}
		}

		private static float3 CalculateFinalOrientation(
			bool strafe,
			float3 strafeForward,
			float3 objectForward,
			float3 currentPointPosition,
			float3 previewPointPosition
		)
		{
			if (strafe)
			{
				return strafeForward;
			}
			else
			{
				float3 finalOrientation = currentPointPosition - previewPointPosition;

				if (finalOrientation.Equals(float3.zero))
				{
					return objectForward;
				}
				else
				{
					return math.normalize(finalOrientation);
				}
			}
		}

		private static float3 CheckCollision(
			float3 desiredDeltaPos,
			float3 castStart,
			float capsuleHeight,
			float capsuleRadius,
			// must be greater than 0, otherwise errors will occur
			float capsuleDeltaFromObstacle,
			LayerMask mask,
			ref bool isColliding
			)
		{
			//capsuleRadius -= capsuleRadiusReduction;

			RaycastHit hit;
			if (Physics.CapsuleCast(
						castStart + (float3)Vector3.up * capsuleRadius,
						castStart + (float3)Vector3.up * (capsuleHeight - capsuleRadius),
						capsuleRadius,
						desiredDeltaPos,
						out hit,
						math.length(desiredDeltaPos) + 2f * capsuleDeltaFromObstacle,
						mask
						))
			{
				isColliding = true;

				float3 normal = hit.normal;
				normal.y = 0f;
				normal = math.normalize(normal);

				float3 hitPoint = hit.point;
				hitPoint.y = castStart.y;

				float3 vectorToProject = (castStart + desiredDeltaPos) - hitPoint;

				return (float3)Vector3.ProjectOnPlane(vectorToProject, normal) + normal * (capsuleRadius + capsuleDeltaFromObstacle) + hitPoint - castStart;
			}
			else
			{
				isColliding = false;
				return desiredDeltaPos;
			}
		}

	}
}


