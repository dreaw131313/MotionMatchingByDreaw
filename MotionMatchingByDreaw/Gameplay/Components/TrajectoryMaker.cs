using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using MotionMatching.Tools;
#endif

namespace MotionMatching.Gameplay
{
	[DisallowMultipleComponent]
	public class TrajectoryMaker : MonoBehaviour, ITrajectoryCreator
	{
		[Space]
		[Header("TRAJECTORY OPTIONS")]
		[SerializeField]
		public bool CreateTrajectory = true;

		[SerializeField]
		private TrajectoryCreationSettings creationSettings;
		[Header("PAST TRAJECTORY OPTIONS:")]
		[SerializeField]
		[Min(0.001f)]
		private float trajectoryRecordUpdateTime = 0.033f;
		[SerializeField]
		private bool keepRecordedTrajectoryFlat;
		//[SerializeField]
		//private bool trajectoryWithCollision = false;

		[Header("COLLISION TRAJECTORY SETTINGS:")]
		[SerializeField]
		private float capsuleHeight = 1.7f;
		[SerializeField]
		private float capsuleRadius = 0.3f;
		[SerializeField]
		private float capsuleDeltaFromObstacle = 0.05f;
		[SerializeField]
		private LayerMask collisionMask;


		public TrajectoryMaker()
		{
			creationSettings = new TrajectoryCreationSettings(
				TrajectoryCreationType.Smooth,
				1.5f,
				0.1f,
				1f,
				3f,
				4f,
				4f,
				false,
				PastTrajectoryType.Recorded
				);
		}

		// Components
		private MotionMatchingComponent motionMatchingComponent;
		private Transform animatedObject;

		private TrajectoryCreatorPoint[] trajectoryPoints_O;

		private int firstIndexWithFutureTime;
		private float recordTimer;

		//private List<RecordedTrajectoryPoint> recordedTrajectoryPoints;
		private CircularList<RecordedTrajectoryPoint> recordedTrajectoryPoints;
		private float trajectoryRecordUpdateTimeInternal;
		//private TrajectoryPoint[] recordedPoints;
		private float3 bufforPosition;
		private bool havePastTrajectory = false;

		private bool[] pointsCollisions = null;

		public PastTrajectoryType PastTrajectoryCreationType
		{
			get => creationSettings.PastTrajectoryCreationType;
			set
			{
				OnPastTrajectoryTypeChange(value);
			}
		}

		public float TrajectoryRecordUpdateTime { get => trajectoryRecordUpdateTime; }
		public float CapsuleHeight { get => capsuleHeight; set => capsuleHeight = value; }
		public float CapsuleRadius { get => capsuleRadius; set => capsuleRadius = value; }
		public LayerMask CollisionMask { get => collisionMask; set => collisionMask = value; }
		public Vector3 Velocity { get; private set; } = Vector3.zero;
		public float VelocityMagnitude { get; private set; } = 0f;
		//Holds information whether trajectory points from first with future time to last are actually colliding(true if collides, false if not).
		public bool[] PointsCollisions { get => pointsCollisions; private set => pointsCollisions = value; }
		public TrajectoryCreationSettings CreationSettings { get => creationSettings; }
		public int FirstIndexWithFutureTime { get => firstIndexWithFutureTime; }
		public bool KeepRecordedTrajectoryFlat { get => keepRecordedTrajectoryFlat; set => keepRecordedTrajectoryFlat = value; }

		// Need be seted by user
		[HideInInspector]
		[System.NonSerialized]
		public Vector3 Input;
		[HideInInspector]
		[System.NonSerialized]
		public Vector3 StrafeDirection;

		private void Awake()
		{
			recordTimer = 0f;
			Input = Vector3.zero;
		}

		private void LateUpdate()
		{
#if UNITY_EDITOR
			if (motionMatchingComponent == null ||
				motionMatchingComponent != null && motionMatchingComponent.motionMatchingController == null)
			{
				return;
			}
			OnTrajectoryCreationSettingsChange();

			trajectoryRecordUpdateTimeInternal = trajectoryRecordUpdateTime;
#endif

			if (CreateTrajectory)
			{
				CreatePastTrajectory();
				CreateFutureTrajectory();
				Velocity = trajectoryPoints_O[firstIndexWithFutureTime].Velocity;
				VelocityMagnitude = Velocity.magnitude;
			}
		}

		public void CreatePastTrajectory()
		{
			if (havePastTrajectory)
			{
				switch (creationSettings.PastTrajectoryCreationType)
				{
					case PastTrajectoryType.Recorded:
						{
							RecordPastTimeTrajectory_Deprecated(
									trajectoryRecordUpdateTimeInternal,
									Time.deltaTime,
									ref recordTimer,
									animatedObject.position,
									GetCurrentForward()
								);
						}
						break;
					case PastTrajectoryType.CopyFromCurrentData:
						if (motionMatchingComponent != null)
						{
							motionMatchingComponent.SetPastPointsFromData(0);
						}
						break;
				}
			}
		}

		public void CreateFutureTrajectory()
		{
			switch (creationSettings.CreationType)
			{
				case TrajectoryCreationType.Constant:
					{
						bufforPosition = transform.position;
						CreateConstantTrajectory_O(
							transform.position,
							StrafeDirection,
							Input * creationSettings.MaxSpeed,
							creationSettings.Strafe,
							firstIndexWithFutureTime
							);
#if UNITY_EDITOR
						for (int i = 0; i < pointsCollisions.Length; i++)
						{
							pointsCollisions[i] = false;
						}

						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							TrajectoryCreatorPoint point = trajectoryPoints_O[i];

							point.NoCollisionPosition = point.Position;

							trajectoryPoints_O[i] = point;
						}
#endif
					}
					break;
				case TrajectoryCreationType.ConstantWithCollision:
					{
						bufforPosition = transform.position;

						CreateConstantCollisionTrajectory_O(
							transform.position,
							StrafeDirection,
							Input * creationSettings.MaxSpeed,
							firstIndexWithFutureTime,
							creationSettings.Strafe,
							CollisionMask
							);
					}
					break;
				case TrajectoryCreationType.Smooth:
					{
						CreateFutureSmoothTrajectory_O(
							transform.position,
							ref bufforPosition,
							StrafeDirection,
							Input * creationSettings.MaxSpeed,
							firstIndexWithFutureTime,
							creationSettings.Strafe
							);

#if UNITY_EDITOR
						for (int i = 0; i < pointsCollisions.Length; i++)
						{
							pointsCollisions[i] = false;
						}

						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							TrajectoryCreatorPoint point = trajectoryPoints_O[i];

							point.NoCollisionPosition = point.Position;

							trajectoryPoints_O[i] = point;
						}
#endif
					}
					break;
				case TrajectoryCreationType.SmoothWithCollision:
					{
						CreateSmoothCollisionTrajectory_O(
							transform.position,
							ref bufforPosition,
							StrafeDirection,
							Input * creationSettings.MaxSpeed,
							firstIndexWithFutureTime,
							creationSettings.Strafe,
							collisionMask
							);
					}
					break;
			}
		}

		public void SetTrajectory(Trajectory trajectory)
		{
#if UNITY_EDITOR
			if (trajectory.Length != trajectoryPoints_O.Length)
			{
				Debug.LogError(string.Format("Wrong number of points seted to Trajectory Creator in object {0}!. Trajectory creator create only {1} points, trajectory which is seted have {2} points", this.name, this.trajectoryPoints_O.Length, trajectory.Length));
				return;
			}
#endif

			for (int i = 0; i < trajectoryPoints_O.Length; i++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[i];
				TrajectoryPoint tPoint = trajectory.GetPoint(i);
				point.Position = tPoint.Position;
				point.Orientation = tPoint.Orientation;
				point.Velocity = tPoint.Velocity;
				trajectoryPoints_O[i] = point;
			}

		}

		private Vector3 GetCurrentForward()
		{
			if (creationSettings.Strafe)
			{
				return StrafeDirection;
			}

			if (animatedObject != null)
			{
				return animatedObject.forward;
			}

			if (trajectoryPoints_O != null)
			{
				return trajectoryPoints_O[firstIndexWithFutureTime].Orientation;
			}

			return transform.forward;
		}

		public void SetMototionMatchingComponent(MotionMatchingComponent motionMatching)
		{
			this.motionMatchingComponent = motionMatching;
		}

		public void SetTrajectorySettings(TrajectoryCreationSettings settings)
		{
			SetCreationType(settings.CreationType);
			OnPastTrajectoryTypeChange(settings.PastTrajectoryCreationType);
			creationSettings = settings;
			OnTrajectoryCreationSettingsChange();
		}

		public void SetZeroTrajectory()
		{
			Input = Vector3.zero;
			bufforPosition = transform.position;
			float3 forward = GetCurrentForward();

			if (trajectoryPoints_O == null) return;

			for (int i = 0; i < trajectoryPoints_O.Length; i++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[i];

				point.Position = transform.position;
				point.Velocity = Vector3.zero;
				point.Orientation = forward;

				trajectoryPoints_O[i] = point;
			}
		}

		public void SetCreationType(TrajectoryCreationType creationType)
		{
			if (creationSettings.CreationType == creationType) return;

			creationSettings.CreationType = creationType;
			OnChangeCreationType();
		}

		private void OnChangeCreationType()
		{
			switch (creationSettings.CreationType)
			{
				case TrajectoryCreationType.Constant:
					{
						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							pointsCollisions[i - firstIndexWithFutureTime] = false;
						}
					}
					break;
				case TrajectoryCreationType.ConstantWithCollision:
					{
						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							TrajectoryCreatorPoint point = trajectoryPoints_O[i];
							point.NoCollisionPosition = point.Position;
							trajectoryPoints_O[i] = point;
						}
					}
					break;
				case TrajectoryCreationType.Smooth:
					{
						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							pointsCollisions[i - firstIndexWithFutureTime] = false;
						}
					}
					break;
				case TrajectoryCreationType.SmoothWithCollision:
					{

						for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
						{
							TrajectoryCreatorPoint point = trajectoryPoints_O[i];
							point.NoCollisionPosition = point.Position;
							trajectoryPoints_O[i] = point;
						}
					}
					break;
			}
		}

		private void OnPastTrajectoryTypeChange(PastTrajectoryType newType)
		{
			if (creationSettings.PastTrajectoryCreationType == newType)
			{
				return;
			}
			creationSettings.PastTrajectoryCreationType = newType;

			if (newType == PastTrajectoryType.Recorded && havePastTrajectory)
			{
				int pastPointsCount = firstIndexWithFutureTime;
				if (pastPointsCount == 0)
				{
					return;
				}


				int pastPointIndex = firstIndexWithFutureTime - 1;
				float acumulationTime = -trajectoryRecordUpdateTime;

				Vector3 lerpPos = transform.position;
				float pointTime = 0;

				TrajectoryCreatorPoint pastPoint = trajectoryPoints_O[pastPointIndex];

				float conditionValue = trajectoryPoints_O[pastPointIndex].Time - trajectoryRecordUpdateTime;
				for (int i = recordedTrajectoryPoints.Count - 1; i >= 0; i--)
				{
					if (acumulationTime <= pastPoint.Time)
					{
						recordedTrajectoryPoints.SetItem(
							i,
							new RecordedTrajectoryPoint(
								pastPoint.Position,
								pastPoint.Velocity,
								pastPoint.Orientation,
								pastPoint.Time
							));

						pointTime = pastPoint.Time;
						lerpPos = trajectoryPoints_O[pastPointIndex].Position;
						pastPointIndex--;

						if (pastPointIndex < 0)
						{
							return;
						}

						pastPoint = trajectoryPoints_O[pastPointIndex];
					}
					else
					{
						float factor = (pointTime - acumulationTime) / (pointTime - pastPoint.Time);
						recordedTrajectoryPoints.SetItem(
							i,
							new RecordedTrajectoryPoint(
								Vector3.Lerp(lerpPos, pastPoint.Position, factor),
								pastPoint.Velocity,
								pastPoint.Orientation,
								acumulationTime
							));
					}

					acumulationTime -= trajectoryRecordUpdateTime;
				}
			}
		}

		#region ITrajectoryCreator implementation
		public void GetTrajectoryToMotionMatchingComponent(ref NativeArray<TrajectoryPoint> trajectoryInWorldSpace, int trajectoryCount)
		{
			for (int i = 0; i < trajectoryCount; i++)
			{
				trajectoryInWorldSpace[i] = new TrajectoryPoint(trajectoryPoints_O[i].Position, trajectoryPoints_O[i].Velocity, trajectoryPoints_O[i].Orientation);
			}
		}

		public void InitializeTrajectoryCreator(MotionMatchingComponent mmc)
		{
			motionMatchingComponent = mmc;
			animatedObject = mmc.transform;
			bufforPosition = animatedObject.position;
			StrafeDirection = animatedObject.forward;
			firstIndexWithFutureTime = mmc.FirstIndexWithFutureTime;

			InitializeTrajectory(mmc);
			havePastTrajectory = trajectoryPoints_O[0].Time < 0;


			//finalFactors = new float[collisionTrajectory.Length - firstIndexWithFutureTime];
			pointsCollisions = new bool[trajectoryPoints_O.Length - firstIndexWithFutureTime];

			trajectoryRecordUpdateTimeInternal = trajectoryRecordUpdateTime;

			if (trajectoryPoints_O[0].Time < 0)
			{
				recordedTrajectoryPoints = new CircularList<RecordedTrajectoryPoint>(Mathf.CeilToInt((trajectoryPoints_O[0].Time / trajectoryRecordUpdateTime) * 1.5f));
			}

		}

		public TrajectoryPoint GetTrajectoryPoint(int index)
		{
			return new TrajectoryPoint(
				trajectoryPoints_O[index].Position,
				trajectoryPoints_O[index].Velocity,
				trajectoryPoints_O[index].Orientation
				);
		}

		public void SetTrajectoryFromNativeContainer(ref NativeArray<TrajectoryPoint> trajectory, Transform inSpace = null)
		{
			if (inSpace != null)
			{
				for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
				{
					TrajectoryPoint tp = trajectory[i];
					tp.TransformToWorldSpace(inSpace);

					TrajectoryCreatorPoint point = trajectoryPoints_O[i];

					point.Position = tp.Position;
					point.Velocity = tp.Velocity;
					point.Orientation = tp.Orientation;
					trajectoryPoints_O[i] = point;
				}
			}
			else
			{
				for (int i = firstIndexWithFutureTime; i < trajectoryPoints_O.Length; i++)
				{
					TrajectoryPoint tp = trajectory[i];
					TrajectoryCreatorPoint point = trajectoryPoints_O[i];
					point.Position = tp.Position;
					point.Velocity = tp.Velocity;
					point.Orientation = tp.Orientation;
					trajectoryPoints_O[i] = point;
				}
			}
		}

		public void SetPastAnimationTrajectoryFromMotionMatchingComponent(ref NativeArray<TrajectoryPoint> trajectory, int firstIndexWithFutureTime)
		{
			for (int i = 0; i < firstIndexWithFutureTime; i++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[i];
				TrajectoryPoint tp = trajectory[i];
				tp.TransformToWorldSpace(animatedObject.transform);
				point.Position = tp.Position;
				point.Velocity = tp.Velocity;
				point.Orientation = tp.Orientation;

				trajectoryPoints_O[i] = point;
			}
		}

		public Vector3 GetTrajectoryMakerPosition()
		{
			return transform.position;
		}

		public Vector3 GetVelocity()
		{
			return Velocity;
		}

		#endregion

		#region New trajectory creation

		private void InitializeTrajectory(MotionMatchingComponent mmc)
		{
			trajectoryPoints_O = new TrajectoryCreatorPoint[mmc.TrajectoryPointsCount];
			float[] pointsTimes = mmc.GetTrajectoryPointsTimes();
			for (int i = 0; i < mmc.TrajectoryPointsCount; i++)
			{
				TrajectoryCreatorPoint point = new TrajectoryCreatorPoint();
				point.Time = pointsTimes[i];

				if (i == firstIndexWithFutureTime)
				{
					point.StepTime = pointsTimes[i];
				}
				else if (i > firstIndexWithFutureTime)
				{
					point.StepTime = pointsTimes[i] - pointsTimes[i - 1];
				}
				else
				{
					point.StepTime = 0;
				}

				point.AccelerationFactor = CalculatePointFactor(
					point.Time,
					creationSettings.MaxTimeToCalculateFactor,
					creationSettings.Bias,
					creationSettings.Acceleration,
					point.StepTime,
					creationSettings.Stiffness
					);

				point.DecelerationFactor = CalculatePointFactor(
					point.Time,
					creationSettings.MaxTimeToCalculateFactor,
					creationSettings.Bias,
					creationSettings.Deceleration,
					point.StepTime,
					creationSettings.Stiffness
					);


				point.Position = transform.position;
				point.NoCollisionPosition = transform.position;
				point.Orientation = mmc.transform.forward;
				point.Velocity = Vector3.zero;

				trajectoryPoints_O[i] = point;
			}
		}

		private void OnTrajectoryCreationSettingsChange()
		{
			int size = trajectoryPoints_O.Length;
			for (int i = firstIndexWithFutureTime; i < size; i++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[i];

				point.AccelerationFactor = CalculatePointFactor(
					point.Time,
					creationSettings.MaxTimeToCalculateFactor,
					creationSettings.Bias,
					creationSettings.Acceleration,
					point.StepTime,
					creationSettings.Stiffness
					);

				point.DecelerationFactor = CalculatePointFactor(
					point.Time,
					creationSettings.MaxTimeToCalculateFactor,
					creationSettings.Bias,
					creationSettings.Deceleration,
					point.StepTime,
					creationSettings.Stiffness
					);

				trajectoryPoints_O[i] = point;
			}
		}

		private float CalculatePointFactor(
			float currentPointTime,
			float lastPointTime,
			float bias,
			float acceleration,
			float stepTime,
			float percentageFactor
			)
		{
			float percentage = Mathf.Lerp(currentPointTime / lastPointTime, 1f, percentageFactor);
			float factor1 = 1f + 3 * percentage * percentage * bias;
			float finalFactor = factor1 * acceleration * stepTime;

			return finalFactor;
		}

		private static float3 CalculateFinalOrientation(
			bool strafe,
			float3 strafeForward,
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

		private void CreateFutureSmoothTrajectory_O(
			float3 objectPosition,
			ref float3 objectPositionBuffor,
			float3 strafeForward,
			float3 desiredVel,
			int firstIndexWithFutureTime,
			bool shouldStrafe
		)
		{
			float desVelSqrLength = math.lengthsq(desiredVel);
			bool useDeccelerationFactor = desVelSqrLength < math.lengthsq(trajectoryPoints_O[firstIndexWithFutureTime].Velocity);

			float3 lastPositionDelta = objectPosition - objectPositionBuffor;
			objectPositionBuffor = objectPosition;

			float3 pointLastdelta = float3.zero;
			float deltaTime = Time.deltaTime;

			int size = trajectoryPoints_O.Length;

			for (int pointIndex = firstIndexWithFutureTime; pointIndex < size; pointIndex++)
			{
				TrajectoryCreatorPoint currentPoint = trajectoryPoints_O[pointIndex];
				currentPoint.Position += lastPositionDelta;
				float currentAccelerationFactor = useDeccelerationFactor ? currentPoint.DecelerationFactor : currentPoint.AccelerationFactor;

				float3 currentDelta;
				if (pointIndex == firstIndexWithFutureTime)
				{
					currentDelta = currentPoint.Position - objectPosition;
				}
				else
				{
					currentDelta = currentPoint.Position - trajectoryPoints_O[pointIndex - 1].Position + pointLastdelta;
				}

				float3 desiredPointDeltaPosition = desiredVel * currentPoint.StepTime;

				float3 finalDelta = float3Extension.MoveTowards(currentDelta, desiredPointDeltaPosition, currentAccelerationFactor * deltaTime);

				float3 newPosition = pointIndex != firstIndexWithFutureTime ?
					trajectoryPoints_O[pointIndex - 1].Position + finalDelta
					: objectPosition + finalDelta;
				pointLastdelta = newPosition - currentPoint.Position;

				currentPoint.Position = newPosition;

				currentPoint.Velocity = pointIndex != firstIndexWithFutureTime ?
					(currentPoint.Position - trajectoryPoints_O[pointIndex - 1].Position) / currentPoint.StepTime
					: (currentPoint.Position - objectPosition) / currentPoint.StepTime;

				currentPoint.Orientation = CalculateFinalOrientation(
					shouldStrafe,
					strafeForward,
					currentPoint.Position,
					pointIndex == firstIndexWithFutureTime ? objectPosition : trajectoryPoints_O[pointIndex - 1].Position,
					currentPoint.Orientation
					);

				trajectoryPoints_O[pointIndex] = currentPoint;
			}
		}

		private void CreateConstantTrajectory_O(
			float3 objectPosition,
			float3 strafeForward,
			float3 desiredVel,
			bool strafe,
			int firstIndexWithFutureTime
			)
		{
			for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectoryPoints_O.Length; pointIndex++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[pointIndex];

				point.Position = point.Time * desiredVel + objectPosition;
				point.Velocity = desiredVel;
				point.Orientation = CalculateFinalOrientation(
						strafe,
						strafeForward,
						point.Position,
						objectPosition,
						point.Orientation
						);

				trajectoryPoints_O[pointIndex] = point;
			}
		}

		private void CreateSmoothCollisionTrajectory_O(
			float3 objectPosition,
			ref float3 objectPositionBuffor,
			float3 strafeForward,
			float3 desiredVel,
			int firstIndexWithFutureTime,
			bool shouldStrafe,
			LayerMask collisionMask
		)
		{
			float desVelSqrLength = math.lengthsq(desiredVel);
			bool useDeccelerationFactor = desVelSqrLength < math.lengthsq(trajectoryPoints_O[firstIndexWithFutureTime].Velocity);

			float3 lastPositionDelta = objectPosition - objectPositionBuffor;
			objectPositionBuffor = objectPosition;


			float3 pointLastdelta = float3.zero;
			float deltaTime = Time.deltaTime;

			int size = trajectoryPoints_O.Length;
			float3 castStart = objectPosition;

			for (int pointIndex = firstIndexWithFutureTime; pointIndex < size; pointIndex++)
			{
				TrajectoryCreatorPoint currentPoint = trajectoryPoints_O[pointIndex];
				currentPoint.NoCollisionPosition += lastPositionDelta;
				currentPoint.Position += lastPositionDelta;

				float currentAccelerationFactor = useDeccelerationFactor ? currentPoint.DecelerationFactor : currentPoint.AccelerationFactor;

				float3 currentDelta;
				float3 currentCollisionDelta;
				if (pointIndex == firstIndexWithFutureTime)
				{
					currentDelta = currentPoint.NoCollisionPosition - objectPosition;
					currentCollisionDelta = currentPoint.Position - objectPosition;
				}
				else
				{
					currentDelta = currentPoint.NoCollisionPosition - trajectoryPoints_O[pointIndex - 1].NoCollisionPosition + pointLastdelta;
					currentCollisionDelta = currentPoint.Position - trajectoryPoints_O[pointIndex - 1].Position + pointLastdelta;
				}

				float3 desiredPointDeltaPosition = desiredVel * currentPoint.StepTime;

				float3 finalDelta = float3Extension.MoveTowards(currentDelta, desiredPointDeltaPosition, currentAccelerationFactor * deltaTime);

				float3 newPosition = pointIndex != firstIndexWithFutureTime ?
					trajectoryPoints_O[pointIndex - 1].NoCollisionPosition + finalDelta
					: objectPosition + finalDelta;
				pointLastdelta = newPosition - currentPoint.NoCollisionPosition;

				currentPoint.NoCollisionPosition = newPosition;

				bool isColliding = false;
				float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
					   currentPoint.NoCollisionPosition - objectPosition :
					   currentPoint.NoCollisionPosition - trajectoryPoints_O[pointIndex - 1].NoCollisionPosition;

				#region Checking collision
				float3 startDesiredDeltaPos_C = CheckCollision(
					colisionCheckDelta,
					castStart,
					capsuleHeight,
					capsuleRadius,
					capsuleDeltaFromObstacle,
					collisionMask,
					ref isColliding,
					true
				);

				float3 collisionPosition;
				if (isColliding)
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = true;
					float3 finaldesiredDeltaPos_C = CheckCollision(
							startDesiredDeltaPos_C,
							castStart,
							capsuleHeight,
							capsuleRadius,
							capsuleDeltaFromObstacle,
							collisionMask,
							ref isColliding,
							false
						);

					float3 finalDelta_C = float3Extension.MoveTowards(currentCollisionDelta, finaldesiredDeltaPos_C, 2f * currentAccelerationFactor * Time.deltaTime);
					//float3 finalDelta_C = finaldesiredDeltaPos_C;

					collisionPosition = pointIndex != firstIndexWithFutureTime ?
						trajectoryPoints_O[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;
				}
				else
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = false;

					float3 finalDelta_C = float3Extension.MoveTowards(currentCollisionDelta, startDesiredDeltaPos_C, currentAccelerationFactor * Time.deltaTime);
					//float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, startDesiredDeltaPos_C, finalFactor, Time.deltaTime);

					collisionPosition = pointIndex != firstIndexWithFutureTime ?
						trajectoryPoints_O[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;
				}
				#endregion
				currentPoint.Position = collisionPosition;


				currentPoint.Velocity = pointIndex != firstIndexWithFutureTime ?
					(currentPoint.Position - trajectoryPoints_O[pointIndex - 1].Position) / currentPoint.StepTime
					: (currentPoint.Position - objectPosition) / currentPoint.StepTime;

				currentPoint.Orientation = CalculateFinalOrientation(
					shouldStrafe,
					strafeForward,
					currentPoint.Position,
					pointIndex == firstIndexWithFutureTime ? objectPosition : trajectoryPoints_O[pointIndex - 1].Position,
					currentPoint.Orientation
					);

				castStart = currentPoint.Position;

				trajectoryPoints_O[pointIndex] = currentPoint;
			}
		}

		private void CreateConstantCollisionTrajectory_O(
			float3 objectPosition,
			float3 strafeForward,
			float3 desiredVel,
			int firstIndexWithFutureTime,
			bool shouldStrafe,
			LayerMask collisionMask
		)
		{
			int size = trajectoryPoints_O.Length;
			float3 castStart = objectPosition;
			for (int pointIndex = firstIndexWithFutureTime; pointIndex < size; pointIndex++)
			{
				TrajectoryCreatorPoint currentPoint = trajectoryPoints_O[pointIndex];
				currentPoint.NoCollisionPosition = currentPoint.Time * desiredVel + objectPosition;

				bool isColliding = false;
				float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
					   currentPoint.NoCollisionPosition - objectPosition :
					   currentPoint.NoCollisionPosition - trajectoryPoints_O[pointIndex - 1].NoCollisionPosition;

				#region Checking collision
				float3 startDesiredDeltaPos_C = CheckCollision(
					colisionCheckDelta,
					castStart,
					capsuleHeight,
					capsuleRadius,
					capsuleDeltaFromObstacle,
					collisionMask,
					ref isColliding,
					true
				);

				float3 collisionPosition;
				if (isColliding)
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = true;
					float3 finaldesiredDeltaPos_C = CheckCollision(
							startDesiredDeltaPos_C,
							castStart,
							capsuleHeight,
							capsuleRadius,
							capsuleDeltaFromObstacle,
							collisionMask,
							ref isColliding,
							false
						);

					float3 finalDelta_C = finaldesiredDeltaPos_C;

					collisionPosition = pointIndex != firstIndexWithFutureTime ?
						trajectoryPoints_O[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;
				}
				else
				{
					pointsCollisions[pointIndex - firstIndexWithFutureTime] = false;
					float3 finalDelta_C = startDesiredDeltaPos_C;

					collisionPosition = pointIndex != firstIndexWithFutureTime ?
						trajectoryPoints_O[pointIndex - 1].Position + finalDelta_C
						: objectPosition + finalDelta_C;
				}
				#endregion
				currentPoint.Position = collisionPosition;


				currentPoint.Velocity = pointIndex != firstIndexWithFutureTime ?
					(currentPoint.Position - trajectoryPoints_O[pointIndex - 1].Position) / currentPoint.StepTime
					: (currentPoint.Position - objectPosition) / currentPoint.StepTime;

				currentPoint.Orientation = CalculateFinalOrientation(
					shouldStrafe,
					strafeForward,
					currentPoint.Position,
					pointIndex == firstIndexWithFutureTime ? objectPosition : trajectoryPoints_O[pointIndex - 1].Position,
					currentPoint.Orientation
					);

				castStart = currentPoint.Position;

				trajectoryPoints_O[pointIndex] = currentPoint;
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

		void RecordPastTimeTrajectory_Deprecated(
			float updateTime,
			float timeDeltaTime,
			ref float recordTimer,
			float3 objectPosition,
			float3 objectForward
			)
		{
			float maxRecordedTime = trajectoryPoints_O[0].Time - 0.05f;
			if (maxRecordedTime >= 0)
			{
				return;
			}
			int goalPointIndex = 0;
			RecordedTrajectoryPoint buffor;
			for (int i = 0; i < recordedTrajectoryPoints.Count; i++)
			{
				TrajectoryCreatorPoint point = trajectoryPoints_O[goalPointIndex];
				if (point.Time < 0)
				{
					RecordedTrajectoryPoint recordedPoint = recordedTrajectoryPoints.GetItem(i);
					if (recordedPoint.futureTime >= point.Time)
					{
						if (i == 0)
						{
							point.Position = recordedPoint.position;
							point.Velocity = recordedPoint.velocity;
							point.Orientation = recordedPoint.orientation;
						}
						else
						{
							RecordedTrajectoryPoint previewRecordedPoint = recordedTrajectoryPoints.GetItem(i - 1);
							float factor = (point.Time - previewRecordedPoint.futureTime) / (recordedPoint.futureTime - previewRecordedPoint.futureTime);
							RecordedTrajectoryPoint lerpedPoint = RecordedTrajectoryPoint.Lerp(previewRecordedPoint, recordedPoint, factor);

							point.Position = lerpedPoint.position;
							point.Velocity = lerpedPoint.velocity;
							point.Orientation = lerpedPoint.orientation;
						}
						if (keepRecordedTrajectoryFlat)
						{
							point.Position.y = objectPosition.y;
						}
						trajectoryPoints_O[goalPointIndex] = point;

						goalPointIndex++;
					}
				}
				buffor = recordedTrajectoryPoints.GetItem(i);
				buffor.futureTime -= timeDeltaTime;
				recordedTrajectoryPoints.SetItem(i, buffor);
				if (buffor.futureTime < maxRecordedTime && i == 0)
				{
					recordedTrajectoryPoints.RemoveItemFromStart();
					i--;
				}
			}

			recordTimer += timeDeltaTime;
			if (recordTimer < updateTime)
			{
				return;
			}

			float3 velocity;
			if (recordedTrajectoryPoints.Count > 1)
			{
				RecordedTrajectoryPoint point = recordedTrajectoryPoints.GetItem(recordedTrajectoryPoints.Count - 1);
				velocity = (objectPosition - point.position) / recordTimer;
			}
			else
			{
				velocity = new float3(0, 0, 0);
			}
			recordTimer = 0f;

			recordedTrajectoryPoints.AddItem(
				new RecordedTrajectoryPoint(
					objectPosition,
					velocity,
					objectForward,
					0f
					));
		}

		#endregion

#if UNITY_EDITOR
		[Header("DEBUG")]
		[SerializeField]
		public bool drawDebug = true;
		[SerializeField]
		public float pointRadius = 0.04f;
		[SerializeField]
		private bool drawRecordedPoints = false;
		[SerializeField]
		private bool drawTrajecotryVelocities = false;

		private void OnDrawGizmos()
		{
			if (motionMatchingComponent == null ||
				motionMatchingComponent != null && motionMatchingComponent.motionMatchingController == null)
			{
				return;
			}
			if (Application.isPlaying)
			{
				if (drawDebug)
				{
					Gizmos.color = Color.green;
					MM_Gizmos.DrawTrajectory(
						this.transform.position,
						GetCurrentForward(),
						trajectoryPoints_O,
						true,
						pointRadius,
						0.3f
						);

					Gizmos.color = Color.cyan;
					Gizmos.DrawSphere(this.transform.position + Vector3.up * pointRadius, pointRadius);

					if (drawTrajecotryVelocities)
					{
						Gizmos.color = Color.yellow;

						MM_Gizmos.DrawCreatorTrajectoryVelocities(trajectoryPoints_O, 0.1f);
					}
				}

				if (drawRecordedPoints)
				{
					float recordTrajectoryPointRadius = pointRadius / 2f;
					Gizmos.color = Color.blue;
					for (int i = 0; i < recordedTrajectoryPoints.Count; i++)
					{
						Gizmos.DrawSphere(
							(Vector3)recordedTrajectoryPoints.GetItem(i).position + Vector3.up * recordTrajectoryPointRadius,
							recordTrajectoryPointRadius
							);
					}
				}
			}
		}
#endif
	}


	public struct TrajectoryCreatorPoint
	{
		public float Time;
		public float StepTime;
		public float AccelerationFactor;
		public float DecelerationFactor;
		public float3 Position;
		public float3 Velocity;
		public float3 Orientation;
		public float3 NoCollisionPosition;
	}

	public class CircularList<Element> where Element : struct
	{
		Element[] Elements;
		public int Capacity { get => Elements.Length; }

		public int Count { get; private set; }
		int newItemPosition;
		public int StartPosition { get; private set; }

		public CircularList(int size)
		{
			Elements = new Element[Mathf.Abs(size)];
			newItemPosition = 0;
			StartPosition = 0;
			Count = 0;
		}


		public void AddItem(Element item)
		{
			if (Count == Capacity)
			{
				return;
			}
			Count++;
			Elements[newItemPosition] = item;
			newItemPosition = (newItemPosition + 1) % Capacity;
		}

		public bool RemoveItemFromStart()
		{
			if (Count == 0)
			{
				return false;
			}
			Count--;
			StartPosition = (StartPosition + 1) % Capacity;
			return true;
		}

		public Element GetItem(int index)
		{
			if (index >= Count)
			{
				throw new System.IndexOutOfRangeException();
			}

			int desiredIndex = (StartPosition + index) % Capacity;

			return Elements[desiredIndex];
		}

		public void SetItem(int index, Element item)
		{
			if (index >= Count)
			{
				throw new System.IndexOutOfRangeException();
			}

			int desiredIndex = (StartPosition + index) % Capacity;
			Elements[desiredIndex] = item;
		}

		public void Reset()
		{
			newItemPosition = 0;
			StartPosition = 0;
			Count = 0;
		}



	}
}