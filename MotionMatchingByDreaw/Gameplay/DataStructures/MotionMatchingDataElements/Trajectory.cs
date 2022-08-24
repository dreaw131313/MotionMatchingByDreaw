using System.Collections.Generic;
using System.Security.Principal;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public enum TrajectoryCreationType
	{
		Constant,
		ConstantWithCollision,
		Smooth,
		SmoothWithCollision
	}

	[System.Serializable]
	public struct Trajectory
	{
		[SerializeField]
		public TrajectoryPoint[] points;

		public int Length
		{
			get
			{
				return points.Length;
			}
			private set
			{
			}
		}

		public Trajectory(int length)
		{
			points = new TrajectoryPoint[length];
		}

		public Trajectory(TrajectoryPoint[] points)
		{
			this.points = points;
		}

		public bool IsValid()
		{
			if (points != null)
			{
				return true;
			}
			return false;
		}

		public float CalculateCost(Trajectory toTrajectory)
		{
			float cost = 0f;
			for (int i = 0; i < Length; i++)
			{
				cost += points[i].CalculateCost(toTrajectory.GetPoint(i));
			}
			return cost;
		}

		public void SetPoint(TrajectoryPoint point, int index)
		{
			points[index].Set(point);
		}

		public TrajectoryPoint GetPoint(int index)
		{
			return points[index];
		}

		public static void Lerp(ref Trajectory buffor, Trajectory first, Trajectory next, float factor)
		{
			for (int i = 0; i < first.Length; i++)
			{
				buffor.SetPoint(TrajectoryPoint.Lerp(first.GetPoint(i), next.GetPoint(i), factor), i);
			}
		}

		public void TransformToLocalSpace(Transform localSpace)
		{
			for (int i = 0; i < Length; i++)
			{
				this.points[i].TransformToLocalSpace(localSpace);
			}
		}

		public void TransformToWorldSpace(Transform localSpace)
		{
			for (int i = 0; i < Length; i++)
			{
				this.points[i].TransformToWorldSpace(localSpace);
			}
		}

		private static float CalculateFinalFactor(
			float currentPointTime,
			float lastPointTime,
			float bias,
			float acceleration,
			float stepTime,
			float desSpeed,
			float percentageFactor,
			float3 currentDelta,
			float3 desiredPointDeltaPosition
			)
		{
			float percentage = Mathf.Lerp(currentPointTime / lastPointTime, 1f, percentageFactor);
			//float factor1 = 1f + bias * percentage * percentage + 0.5f * percentage;
			float doublePercentage = percentage * percentage;
			float factor1 = 1f +
							bias * (
							doublePercentage * doublePercentage +
							doublePercentage * percentage +
							doublePercentage
							);

			float deltaMag = math.length(currentDelta - desiredPointDeltaPosition);
			float factor2 = 1f;
			float pointPartSpeed = desSpeed * stepTime;

			//if (deltaMag > pointPartSpeed * 1.1 && pointPartSpeed > 0)
			//{
			//	factor2 = (deltaMag + pointPartSpeed) / deltaMag * sharpTurnMultiplier;
			//}

			float finalFactor = factor1 * factor2 * acceleration * stepTime;

			return finalFactor;
		}

		public static void CreateTrajectory(
			TrajectoryPoint[] trajectory,
			float[] pointsTimes,
			float3 objectPosition,
			ref float3 objectPositionBuffor,
			float3 objectForward,
			float3 strafeForward,
			float3 desiredVel,
			float acceleration,
			float bias,
			float stiffnes,
			float maxTimeToCalculateFactor,
			bool strafe,
			int firstIndexWithFutureTime
			)
		{
			float3 lastPositionDelta = objectPosition - objectPositionBuffor;
			objectPositionBuffor = objectPosition;

			float desSpeed = math.length(desiredVel);
			float stepTime;
			float3 currentDelta;
			float3 pointLastdelta = float3.zero;

			for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory.Length; pointIndex++)
			{
				TrajectoryPoint currentPoint = trajectory[pointIndex];
				currentPoint.Position = currentPoint.Position + lastPositionDelta;


				if (pointIndex == (firstIndexWithFutureTime))
				{
					stepTime = pointsTimes[pointIndex];
					currentDelta = currentPoint.Position - objectPosition;
				}
				else
				{
					stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
					currentDelta = currentPoint.Position - trajectory[pointIndex - 1].Position + pointLastdelta;
				}
				float3 desiredPointDeltaPosition = desiredVel * stepTime;

				float finalFactor = CalculateFinalFactor(
					pointsTimes[pointIndex],
					maxTimeToCalculateFactor,
					bias,
					acceleration,
					stepTime,
					desSpeed,
					stiffnes,
					currentDelta,
					desiredPointDeltaPosition
					);

				float3 finalDelta = float3Extension.MoveTowards(currentDelta, desiredPointDeltaPosition, finalFactor * Time.deltaTime);
				//float3 finalDelta = Vector3.MoveTowards(currentDelta, desiredPointDeltaPosition, finalFactor * Time.deltaTime);


				float3 newPosition = pointIndex != firstIndexWithFutureTime ?
					trajectory[pointIndex - 1].Position + finalDelta
					: objectPosition + finalDelta;
				pointLastdelta = newPosition - currentPoint.Position;

				currentPoint.Position = newPosition;

				currentPoint.Velocity = pointIndex != firstIndexWithFutureTime ?
					(currentPoint.Position - trajectory[pointIndex - 1].Position) / stepTime
					: (currentPoint.Position - objectPosition) / stepTime;

				currentPoint.Orientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						currentPoint.Position,
						pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory[pointIndex - 1].Position,
						currentPoint.Orientation
						);

				trajectory[pointIndex] = currentPoint;
			}
		}

		public static void ApplayCollisionOnTrajectory(
			TrajectoryPoint[] collisionTrajectory,
			TrajectoryPoint[] trajectory,
			float3 objectPosition,
			int firstIndexWithFutureTime,
			// needed creation settings
			bool strafe,
			float3 strafeForward,
			float3 objectForward,
			// collision studd
			float capsuleHeight,
			float capsuleRadius,
			LayerMask mask,
			bool orientationFromCollisionTrajectory,
			float capsuleDeltaFromObstacle,
			ref bool[] pointsCollisions
			)
		{

			float3 previousPosition = objectPosition;

			for (int i = firstIndexWithFutureTime; i < trajectory.Length; i++)
			{
				bool isColliding = false;

				TrajectoryPoint currentPoint = trajectory[i];
				TrajectoryPoint currentCollisionPoint = collisionTrajectory[i];

				float3 colisionCheckDelta = currentPoint.Position - previousPosition;


				float3 firstCollisionPosition = CheckCollision(
					colisionCheckDelta,
					previousPosition,
					capsuleHeight,
					capsuleRadius,
					capsuleDeltaFromObstacle,
					mask,
					ref isColliding,
					true
					);

				if (isColliding)
				{
					pointsCollisions[i - firstIndexWithFutureTime] = true;
					float3 finaldesiredDeltaPos_C = CheckCollision(
						firstCollisionPosition,
						previousPosition,
						capsuleHeight,
						capsuleRadius,
						capsuleDeltaFromObstacle,
						mask,
						ref isColliding,
						false
						);

					currentPoint.Position = previousPosition + finaldesiredDeltaPos_C;
				}
				else
				{
					pointsCollisions[i - firstIndexWithFutureTime] = false;
					currentPoint.Position = previousPosition + firstCollisionPosition;
				}

				if (orientationFromCollisionTrajectory)
				{
					currentPoint.Orientation = CalculateFinalOrientation(
											strafe,
											strafeForward,
											objectForward,
											currentPoint.Position,
											previousPosition,
											currentPoint.Orientation
											);
				}


				previousPosition = currentPoint.Position;
				trajectory[i] = currentPoint;
			}

		}


		public static void RecordPastTimeTrajectory_Deprecated(
			TrajectoryPoint[] trajectory,
			float[] pointsTimes,
			float updateTime,
			float timeDeltaTime,
			ref float recordTimer,
			ref List<RecordedTrajectoryPoint> recordedTrajectory,
			float3 objectPosition,
			float3 objectForward
			)
		{
			if (recordedTrajectory == null)
			{
				recordedTrajectory = new List<RecordedTrajectoryPoint>();
			}
			float maxRecordedTime = pointsTimes[0] - 0.05f;
			if (maxRecordedTime >= 0)
			{
				return;
			}
			int goalPointIndex = 0;
			RecordedTrajectoryPoint buffor;
			for (int i = 0; i < recordedTrajectory.Count; i++)
			{
				if (pointsTimes[goalPointIndex] < 0)
				{
					if (recordedTrajectory[i].futureTime >= pointsTimes[goalPointIndex])
					{
						if (i == 0)
						{
							trajectory[goalPointIndex] = new TrajectoryPoint(
								recordedTrajectory[i].position,
								recordedTrajectory[i].velocity,
								recordedTrajectory[i].orientation
								);
						}
						else
						{
							float factor =
								(pointsTimes[goalPointIndex] - recordedTrajectory[i - 1].futureTime)
								/
								(recordedTrajectory[i].futureTime - recordedTrajectory[i - 1].futureTime);
							RecordedTrajectoryPoint lerpedPoint = RecordedTrajectoryPoint.Lerp(recordedTrajectory[i - 1], recordedTrajectory[i], factor);
							trajectory[goalPointIndex] = new TrajectoryPoint(
								lerpedPoint.position,
								lerpedPoint.velocity,
								lerpedPoint.orientation
								);
						}


						goalPointIndex++;
					}
				}
				buffor = recordedTrajectory[i];
				buffor.futureTime -= timeDeltaTime;
				recordedTrajectory[i] = buffor;
				if (recordedTrajectory[i].futureTime < maxRecordedTime)
				{
					recordedTrajectory.RemoveAt(i);
					i--;
					continue;
				}
			}

			recordTimer += Time.deltaTime;
			if (recordTimer < updateTime)
			{
				return;
			}

			float3 velocity;
			if (recordedTrajectory.Count > 1)
			{
				velocity = (objectPosition - recordedTrajectory[recordedTrajectory.Count - 1].position) / recordTimer;
			}
			else
			{
				velocity = new float3(0, 0, 0);
			}
			recordTimer = 0f;
			recordedTrajectory.Add(
				new RecordedTrajectoryPoint(
					objectPosition,
					velocity,
					objectForward,
					0f
					));
		}


		public static void RecordPastTrajectory_Optimized_NotWorkCorrectly(
			ref TrajectoryPoint[] recordePoints,
			float3 objectPosition,
			float3 objectForward,
			ref float timer,
			ref int counter,
			ref int lastIndex,
			float updateTime
			)
		{
			timer += Time.deltaTime;
			if (timer < updateTime)
			{
				return;
			}
			timer = 0f;

			TrajectoryPoint editedPoint = recordePoints[counter];
			editedPoint.Position = objectPosition;
			editedPoint.Orientation = objectForward;
			editedPoint.Velocity = (objectPosition - recordePoints[lastIndex].Position) / updateTime;
			lastIndex = counter;
			recordePoints[counter] = editedPoint;
			counter = (counter + 1) % recordePoints.Length;
		}

		public void EvaluatePastPoints_NotWorking(
			float[] pointTimes,
			TrajectoryPoint[] recordedPoints,
			int currentCounter
			)
		{
			points[0] = recordedPoints[currentCounter];

			for (int i = 1; i < points.Length; i++)
			{
				if (pointTimes[i] < 0)
				{
					int pointIndex = Mathf.FloorToInt((pointTimes[i] / pointTimes[0]) * recordedPoints.Length);
					int shiftedPointIndex = (pointIndex + currentCounter) % recordedPoints.Length;
					points[i] = recordedPoints[shiftedPointIndex];
				}
				else
				{
					break;
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
			ref bool isColliding,
			bool firsHit
			)
		{
			//capsuleRadius -= capsuleRadiusReduction;

			RaycastHit hit;
			float deltaLength = math.length(desiredDeltaPos);
			if (Physics.CapsuleCast(
						castStart + (float3)Vector3.up * capsuleRadius,
						castStart + (float3)Vector3.up * (capsuleHeight - capsuleRadius),
						capsuleRadius,
						math.normalize(desiredDeltaPos),
						out hit,
						deltaLength + 2 * capsuleDeltaFromObstacle + capsuleRadius,
						mask
						))
			{
				if (hit.distance <= deltaLength)
				{
					isColliding = true;

					float3 normal = hit.normal;
					normal.y = 0f;
					normal = math.normalize(normal);

					float3 hitPoint = hit.point;
					hitPoint.y = castStart.y;

					float3 vectorToProject = (castStart + desiredDeltaPos) - hitPoint;
					if (firsHit)
					{
						return (float3)Vector3.ProjectOnPlane(vectorToProject, normal) + normal * (capsuleRadius + capsuleDeltaFromObstacle) + hitPoint - castStart;
					}
					else
					{
						return normal * (capsuleRadius + capsuleDeltaFromObstacle) + hitPoint - castStart;
					}
				}
			}
			isColliding = false;
			return desiredDeltaPos;
		}

		private static float3 CalculateFinalOrientation(
			bool strafe,
			float3 strafeForward,
			float3 objectForward,
			float3 currentPointPosition,
			float3 previewPointPosition,
			float3 currentOrientation
			)
		{
			if (strafe)
			{
				return strafeForward;
			}
			else
			{
				float3 finalOrientation = currentPointPosition - previewPointPosition;

				if (math.lengthsq(finalOrientation) < 0.0001f)
				{
					return currentOrientation;
				}
				else
				{
					return math.normalize(finalOrientation);
				}
			}
		}


		public static void CreateCollisionTrajectory(
			TrajectoryPoint[] trajectory_C,
			TrajectoryPoint[] trajectory_NC,
			float[] pointsTimes,
			float3 objectPosition,
			ref float3 objectPositionBuffor,
			// Orientation
			float3 objectForward,
			float3 strafeForward,
			// Trajectory working settings
			float3 desiredVel,
			float acceleration,
			float bias,
			float stiffnes,
			float maxTimeToCalculateFactor,
			bool strafe,
			//
			int firstIndexWithFutureTime,
			//Collsions settings
			float capsuleHeight,
			float capsuleRadius,
			LayerMask mask,
			bool orientationFromCollisionTrajectory,
			float capsuleDeltaFromObstacle,
			ref bool[] pointsCollisions
			)
		{
			float3 lastPositionDelta = objectPosition - objectPositionBuffor;
			objectPositionBuffor = objectPosition;

			float desSpeed = math.length(desiredVel);
			float stepTime;
			float3 currentDelta_NC;
			float3 currentDelta_C;
			float3 pointLastdelta_NC = float3.zero;
			float3 pointLastdelta_C = float3.zero;
			float3 castStart = objectPosition;

			for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory_NC.Length; pointIndex++)
			{
				TrajectoryPoint currentPoint = trajectory_NC[pointIndex];
				TrajectoryPoint currentCollisionPoint = trajectory_C[pointIndex];

				currentPoint.Position = currentPoint.Position + lastPositionDelta;
				currentCollisionPoint.Position = currentCollisionPoint.Position + lastPositionDelta;


				if (pointIndex == firstIndexWithFutureTime)
				{
					stepTime = pointsTimes[pointIndex];
					currentDelta_NC = currentPoint.Position - objectPosition;
					currentDelta_C = currentCollisionPoint.Position - objectPosition;

				}
				else
				{
					stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
					currentDelta_NC = currentPoint.Position - trajectory_NC[pointIndex - 1].Position + pointLastdelta_NC;
					currentDelta_C = currentCollisionPoint.Position - trajectory_C[pointIndex - 1].Position + pointLastdelta_C;
				}
				float3 desiredDeltaPosition_NC = desiredVel * stepTime;

				float finalFactor = CalculateFinalFactor(
					pointsTimes[pointIndex],
					maxTimeToCalculateFactor,
					bias,
					acceleration,
					stepTime,
					desSpeed,
					stiffnes,
					currentDelta_NC,
					desiredDeltaPosition_NC
					);

				float3 finalDelta_NC = float3Extension.MoveFloat3WithSpeed(currentDelta_NC, desiredDeltaPosition_NC, finalFactor, Time.deltaTime);

				float3 newPosition_NC = pointIndex != firstIndexWithFutureTime ?
					trajectory_NC[pointIndex - 1].Position + finalDelta_NC
					: objectPosition + finalDelta_NC;
				pointLastdelta_NC = newPosition_NC - currentPoint.Position;

				#region Orientation calculation

				float3 finalOrientation;

				if (orientationFromCollisionTrajectory)
				{
					finalOrientation = CalculateFinalOrientation(
											strafe,
											strafeForward,
											objectForward,
											currentCollisionPoint.Position,
											pointIndex == firstIndexWithFutureTime ? objectPosition : currentCollisionPoint.Position,
											currentCollisionPoint.Orientation
											);
				}
				else
				{
					finalOrientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						currentPoint.Position,
						pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC[pointIndex - 1].Position,
						currentCollisionPoint.Orientation
						);
				}


				#endregion

				currentPoint.Position = newPosition_NC;

				bool isColliding = false;
				float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
					   currentPoint.Position - objectPosition :
					   currentPoint.Position - trajectory_NC[pointIndex - 1].Position;

				float3 startDesiredDeltaPos_C = CheckCollision(
					colisionCheckDelta,
					castStart,
					capsuleHeight,
					capsuleRadius,
					capsuleDeltaFromObstacle,
					mask,
					ref isColliding,
					true
					);

				float3 newPosition_C;
				if (isColliding)
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = true;
					float3 finaldesiredDeltaPos_C = CheckCollision(
						startDesiredDeltaPos_C,
						castStart,
						capsuleHeight,
						capsuleRadius,
						capsuleDeltaFromObstacle,
						mask,
						ref isColliding,
						false
						);

					float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, finaldesiredDeltaPos_C, 2f * finalFactor, Time.deltaTime);
					//float3 finalDelta_C = finaldesiredDeltaPos_C;

					newPosition_C = pointIndex != firstIndexWithFutureTime ?
						trajectory_C[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;

				}
				else
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = false;

					float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, startDesiredDeltaPos_C, finalFactor, Time.deltaTime);
					//float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, startDesiredDeltaPos_C, finalFactor, Time.deltaTime);

					newPosition_C = pointIndex != firstIndexWithFutureTime ?
						trajectory_C[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;
				}

				pointLastdelta_C = newPosition_C - currentCollisionPoint.Position;

				float3 newVelocity_C = pointIndex != firstIndexWithFutureTime ?
					(currentCollisionPoint.Position - trajectory_C[pointIndex - 1].Position) / stepTime
					: (currentCollisionPoint.Position - objectPosition) / stepTime;

				currentCollisionPoint.Position = newPosition_C;
				currentCollisionPoint.Velocity = newVelocity_C;
				currentCollisionPoint.Orientation = finalOrientation;
				castStart = currentCollisionPoint.Position;

				trajectory_NC[pointIndex] = currentPoint;
				trajectory_C[pointIndex] = currentCollisionPoint;
			}
		}

		public static void CreateConstantTrajectory(
			TrajectoryPoint[] trajectory,
			float3 objectPosition,
			float3 objectForward,
			float3 strafeForward,
			float3 desiredVel,
			float maxSpeed,
			bool strafe,
			int firstIndexWithFutureTime
			)
		{
			float speedStep = maxSpeed / (trajectory.Length - firstIndexWithFutureTime);
			float3 velDir;
			if (math.lengthsq(desiredVel) <= 0.0001f)
			{
				velDir = float3.zero;
			}
			else
			{
				velDir = math.normalize(desiredVel);
			}
			for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory.Length; pointIndex++)
			{
				float3 newPosition = velDir * speedStep * (float)(pointIndex - firstIndexWithFutureTime + 1) + objectPosition;
				float3 newVelocity = desiredVel;
				float3 newOrientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						trajectory[pointIndex].Position,
						pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory[pointIndex - 1].Position,
						trajectory[pointIndex].Orientation
						);

				trajectory[pointIndex] = new TrajectoryPoint(newPosition, newVelocity, newOrientation);
			}
		}

		public static void CreateConstantTrajectoryWithCollision(
			TrajectoryPoint[] trajectory_C,
			TrajectoryPoint[] trajectory_NC,
			float[] pointsTimes,
			float3 objectPosition,
			float3 objectForward,
			float3 strafeForward,
			float3 desiredVel,
			float maxSpeed,
			bool strafe,
			int firstIndexWithFutureTime,
			float capsuleHeight,
			float capsuleRadius,
			float capsuleDeltaFromObstacle,
			LayerMask mask,
			bool orientationFromCollisionTrajectory,
			ref bool[] pointsCollisions
			)
		{
			float speedStep = maxSpeed / (trajectory_NC.Length - firstIndexWithFutureTime);
			float3 velDir;

			if (math.lengthsq(desiredVel) <= 0.0001f)
			{
				velDir = float3.zero;
			}
			else
			{
				velDir = math.normalize(desiredVel);
			}

			float stepTime;
			for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory_NC.Length; pointIndex++)
			{
				if (pointIndex == firstIndexWithFutureTime)
				{
					stepTime = pointsTimes[pointIndex];

				}
				else
				{
					stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
				}

				float3 newPosition = velDir * speedStep * (float)(pointIndex - firstIndexWithFutureTime + 1) + objectPosition;
				float3 newVelocity = desiredVel;
				float3 newOrientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						trajectory_NC[pointIndex].Position,
						pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC[pointIndex - 1].Position,
						trajectory_C[pointIndex].Orientation
						);
				trajectory_NC[pointIndex] = new TrajectoryPoint(newPosition, newVelocity, newOrientation);
				float3 finalOrientation;
				if (orientationFromCollisionTrajectory)
				{
					finalOrientation = CalculateFinalOrientation(
											strafe,
											strafeForward,
											objectForward,
											trajectory_C[pointIndex].Position,
											pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_C[pointIndex - 1].Position,
											trajectory_C[pointIndex].Orientation
											);
				}
				else
				{
					finalOrientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						objectForward,
						trajectory_NC[pointIndex].Position,
						pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC[pointIndex - 1].Position,
						trajectory_C[pointIndex].Orientation
						);
				}

				bool isColliding = false;
				float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
					   trajectory_NC[pointIndex].Position - objectPosition :
					   trajectory_NC[pointIndex].Position - trajectory_NC[pointIndex - 1].Position;
				float3 colisionCheckStart = pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_C[pointIndex - 1].Position; //trajectory_C.GetPoint(pointIndex - 1).position,

				float3 startDesiredDeltaPos_C = CheckCollision(
					colisionCheckDelta,
					colisionCheckStart,
					capsuleHeight,
					capsuleRadius,
					capsuleDeltaFromObstacle,
					mask,
					ref isColliding,
					true
					);

				float3 newPosition_C;
				if (isColliding)
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = true;
					float3 finaldesiredDeltaPos_C = CheckCollision(
						startDesiredDeltaPos_C,
						colisionCheckStart,
						capsuleHeight,
						capsuleRadius,
						capsuleDeltaFromObstacle,
						mask,
						ref isColliding,
						false
						);

					newPosition_C = colisionCheckStart + finaldesiredDeltaPos_C;
				}
				else
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = false;
					newPosition_C = colisionCheckStart + startDesiredDeltaPos_C;
				}
				float3 newVelocity_C = pointIndex != firstIndexWithFutureTime ?
					(trajectory_C[pointIndex].Position - trajectory_C[pointIndex - 1].Position) / stepTime
					: (trajectory_C[pointIndex].Position - objectPosition) / stepTime;

				trajectory_C[pointIndex] = new TrajectoryPoint(newPosition_C, newVelocity_C, finalOrientation);
			}
		}
	}

	[System.Serializable]
	public struct TrajectoryPoint
	{
		[SerializeField]
		public float3 Position;
		[SerializeField]
		public float3 Velocity;
		[SerializeField]
		public float3 Orientation;

		public static TrajectoryPoint operator +(TrajectoryPoint x, TrajectoryPoint y)
		{
			return new TrajectoryPoint(
				x.Position + y.Position,
				x.Velocity + y.Velocity,
				math.normalize(x.Orientation + y.Orientation)
				);
		}

		public static TrajectoryPoint operator *(float x, TrajectoryPoint y)
		{
			return new TrajectoryPoint(
				x * y.Position,
				x * y.Velocity,
				y.Orientation
				);
		}

		public static TrajectoryPoint operator *(TrajectoryPoint y, float x)
		{
			return new TrajectoryPoint(
				x * y.Position,
				x * y.Velocity,
				y.Orientation
				);
		}

		public static TrajectoryPoint operator /(TrajectoryPoint y, float x)
		{
			return new TrajectoryPoint(
				y.Position / x,
				y.Velocity / x,
				y.Orientation
				);
		}

		public TrajectoryPoint(float3 position, float3 velocity, float3 orientation)
		{
			this.Position = position;
			this.Velocity = velocity;
			this.Orientation = orientation;
		}

		public TrajectoryPoint(TrajectoryPoint point)
		{
			Position = new float3(point.Position.x, point.Position.y, point.Position.z);
			Velocity = new float3(point.Velocity.x, point.Velocity.y, point.Velocity.z);
			Orientation = new float3(point.Orientation.x, point.Orientation.y, point.Orientation.z);
		}

		[BurstCompile]
		public static TrajectoryPoint LerpPiont(TrajectoryPoint first, TrajectoryPoint next, float factor)
		{
			return new TrajectoryPoint(
					math.lerp(first.Position, next.Position, factor),
					math.lerp(first.Velocity, next.Velocity, factor),
					math.lerp(first.Orientation, next.Orientation, factor)
					);
		}

		[BurstCompile]
		public void Set(float3 position, float3 velocity, float3 orientation)
		{
			this.Position = position;
			this.Velocity = velocity;
			this.Orientation = orientation;
		}
		[BurstCompile]
		public void Set(TrajectoryPoint point)
		{
			this.Position = point.Position;
			this.Velocity = point.Velocity;
			this.Orientation = point.Orientation;
		}

		[BurstCompile]
		public void SetPosition(float3 position)
		{
			this.Position = position;
		}

		[BurstCompile]
		public void SetVelocity(float3 velocity)
		{
			this.Velocity = velocity;
		}

		[BurstCompile]
		public void SetOrientation(float3 orientation)
		{
			this.Orientation = orientation;
		}

		public void TransformToLocalSpace(Transform localSpace)
		{
			this.Position = localSpace.InverseTransformPoint(this.Position);
			this.Velocity = localSpace.InverseTransformDirection(this.Velocity);
			this.Orientation = localSpace.InverseTransformDirection(this.Orientation);
		}

		public void TransformToWorldSpace(Transform localSpace)
		{
			this.Position = localSpace.TransformPoint(this.Position);
			this.Velocity = localSpace.TransformDirection(this.Velocity);
			this.Orientation = localSpace.TransformDirection(this.Orientation);
		}

		public TrajectoryPoint TransformToWorldSpaceAndReturn(Transform localSpace)
		{
			this.Position = localSpace.TransformPoint(this.Position);
			this.Velocity = localSpace.TransformDirection(this.Velocity);
			this.Orientation = localSpace.TransformDirection(this.Orientation);
			return this;
		}

		#region Cost calculation
		[BurstCompile]
		public float CalculateCost(TrajectoryPoint point)
		{
			////float cost = 0;
			////cost += CalculatePositionCost(point);
			////cost += CalculateVelocityCost(point);
			////cost += CalculateOrientationCost(point);

			//float3 posDelta = math.abs(point.Position - Position);
			//float3 velDelta = math.abs(point.Velocity - Velocity);
			//float3 orientDelta = math.abs(point.Orientation - Orientation);
			//float cost =
			//	posDelta.x + posDelta.y + posDelta.z +
			//	velDelta.x + velDelta.y + velDelta.z +
			//	orientDelta.x + orientDelta.y + orientDelta.z;

			float3 posDelta = point.Position - Position;
			float3 velDelta = point.Velocity - Velocity;
			float3 orientDelta = point.Orientation - Orientation;

			return math.lengthsq(posDelta) + math.lengthsq(velDelta) + math.lengthsq(orientDelta);
		}

		[BurstCompile]
		public float CalculatePositionCost(TrajectoryPoint point)
		{
			return math.lengthsq(point.Position - Position);
		}

		[BurstCompile]
		public float CalculateVelocityCost(TrajectoryPoint point)
		{
			return math.lengthsq(point.Velocity - Velocity);
		}

		[BurstCompile]
		public float CalculateOrientationCost(TrajectoryPoint point)
		{
			return math.lengthsq(point.Orientation - Orientation);
		}

		[BurstCompile]
		public float3 CalculateVectorCost(TrajectoryPoint point)
		{
			float3 cost = float3.zero;
			cost += CalculatePositionVectorCost(point);
			cost += CalculateVelocityVectorCost(point);
			cost += CalculateOrientationVectorCost(point);
			return cost;
		}

		[BurstCompile]
		public float3 CalculatePositionVectorCost(TrajectoryPoint point)
		{
			return math.abs(point.Position - Position);
		}

		[BurstCompile]
		public float3 CalculateVelocityVectorCost(TrajectoryPoint point)
		{
			return math.abs(point.Velocity - Velocity);
		}

		[BurstCompile]
		public float3 CalculateOrientationVectorCost(TrajectoryPoint point)
		{
			return math.abs(point.Orientation - Orientation);
		}
		#endregion

		#region Static methods


		[BurstCompile]
		public static TrajectoryPoint Lerp(TrajectoryPoint point1, TrajectoryPoint point2, float factor)
		{
			float3 dir = math.lerp(point1.Orientation, point2.Orientation, factor);
			if (dir.x != 0f && dir.y != 0f && dir.z != 0f)
			{
				dir = math.normalize(dir);
			}
			else
			{
				dir = point1.Orientation;
			}
			return new TrajectoryPoint(
				math.lerp(point1.Position, point2.Position, factor),
				math.lerp(point1.Velocity, point2.Velocity, factor),
				dir
			);
		}

		/// <summary>
		/// Editor only
		/// </summary>
		/// <returns></returns>
		public float[] ToFloatArray()
		{
			float[] array = new float[9];
			array[0] = this.Position.x;
			array[1] = this.Position.y;
			array[2] = this.Position.z;
			array[3] = this.Velocity.x;
			array[4] = this.Velocity.y;
			array[5] = this.Velocity.z;
			array[6] = this.Orientation.x;
			array[7] = this.Orientation.y;
			array[8] = this.Orientation.z;

			return array;
		}

		/// <summary>
		/// Return next startIndex. Editor only.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="startIndex"></param>
		/// <returns>Next start index in array</returns>
		public static int FromArray(out TrajectoryPoint point, ref float[] array, int startIndex = 0)
		{
			point = new TrajectoryPoint(
				new float3(array[startIndex + 0], array[startIndex + 1], array[startIndex + 2]),
				new float3(array[startIndex + 3], array[startIndex + 4], array[startIndex + 5]),
				new float3(array[startIndex + 6], array[startIndex + 7], array[startIndex + 8])
				);
			return startIndex + 9;
		}

		public static int FromArray(out TrajectoryPoint point, ref List<float> array, int startIndex = 0)
		{
			point = new TrajectoryPoint(
				new float3(array[startIndex + 0], array[startIndex + 1], array[startIndex + 2]),
				new float3(array[startIndex + 3], array[startIndex + 4], array[startIndex + 5]),
				new float3(array[startIndex + 6], array[startIndex + 7], array[startIndex + 8])
				);
			return startIndex + 9;
		}

		#endregion
	}
	public struct RecordedTrajectoryPoint
	{
		[SerializeField]
		public float3 position;
		[SerializeField]
		public float3 velocity;
		[SerializeField]
		public float3 orientation;
		[SerializeField]
		public float futureTime;

		public RecordedTrajectoryPoint(float3 position, float3 velocity, float3 orientation, float futureTime)
		{
			this.position = position;
			this.velocity = velocity;
			this.orientation = orientation;
			this.futureTime = futureTime;
		}

		public RecordedTrajectoryPoint(RecordedTrajectoryPoint point)
		{
			position = new float3(point.position.x, point.position.y, point.position.z);
			velocity = new float3(point.velocity.x, point.velocity.y, point.velocity.z);
			orientation = new float3(point.orientation.x, point.orientation.y, point.orientation.z);
			futureTime = point.futureTime;
		}

		[BurstCompile]
		public void Set(float3 position, float3 velocity, float3 orientation, float futureTime)
		{
			this.position = position;
			this.velocity = velocity;
			this.orientation = orientation;
			this.futureTime = futureTime;
		}

		[BurstCompile]
		public void Set(float3 position, float3 velocity, float3 orientation)
		{
			this.position = position;
			this.velocity = velocity;
			this.orientation = orientation;
		}

		[BurstCompile]
		public void Set(RecordedTrajectoryPoint point)
		{
			this.position = point.position;
			this.velocity = point.velocity;
			this.orientation = point.orientation;
			this.futureTime = point.futureTime;
		}

		[BurstCompile]
		public void SetPosition(float3 position)
		{
			this.position = position;
		}

		[BurstCompile]
		public void SetVelocity(float3 velocity)
		{
			this.velocity = velocity;
		}

		[BurstCompile]
		public void SetOrientation(float3 orientation)
		{
			this.orientation = orientation;
		}

		[BurstCompile]
		public void SetFutureTime(float newFutureTime)
		{
			this.futureTime = newFutureTime;
		}

		public void TransformToLocalSpace(Transform localSpace)
		{
			this.position = localSpace.InverseTransformPoint(this.position);
			this.velocity = localSpace.InverseTransformDirection(this.velocity);
			this.orientation = localSpace.InverseTransformDirection(this.orientation);
		}

		public void TransformToWorldSpace(Transform localSpace)
		{
			this.position = localSpace.TransformPoint(this.position);
			this.velocity = localSpace.TransformDirection(this.velocity);
			this.orientation = localSpace.TransformDirection(this.orientation);
		}

		#region Static methods


		[BurstCompile]
		public static RecordedTrajectoryPoint Lerp(RecordedTrajectoryPoint point1, RecordedTrajectoryPoint point2, float factor)
		{
			float3 dir = math.lerp(point1.orientation, point2.orientation, factor);
			if (math.lengthsq(dir) > 0)
			{
				dir = math.normalize(dir);
			}
			else
			{
				dir = point1.orientation;
			}
			return new RecordedTrajectoryPoint(
				math.lerp(point1.position, point2.position, factor),
				math.lerp(point1.velocity, point2.velocity, factor),
				dir,
				math.lerp(point1.futureTime, point2.futureTime, factor)
			);
		}

		#endregion

	}
}