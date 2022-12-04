using MotionMatching.Gameplay.Jobs;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


#if UNITY_EDITOR
using MotionMatching.Tools;
#endif

namespace MotionMatching.Gameplay
{

	public class LogicContactState : LogicState
	{
		private bool isFindingRunning;
		private float transitionBlendTime;

		private int destinationContactIndex;
		private bool isDestinationContactReached;
		private bool isCurrentContactValid = false;
		private MotionMatchingContact currentAnimContact;
		private FrameContact currentFrameContact;

		private const float minDeltaTimeToPerformMovement = 0.001f;

		private NativeList<int> recentlyPlayedClipsIndexes;
		private float lastEnterToStateTime = 0;

		private Vector3 startPositionLerp;
		private bool isStartLerpPositionValid = false;

		public ContactStateFeatures Features;

		bool isStartMovingToContactCallbackPerformed = false;

		public LogicContactState(
			ContactState_SO state,
			MotionMatchingComponent component,
			LogicMotionMatchingLayer logicLayer,
			PlayableAnimationSystem animationSystem
			) :
			base(state, component, logicLayer, animationSystem)
		{
			Features = state.Features;

			if (Features.NotSearchInRecentClips)
			{
				recentlyPlayedClipsIndexes = new NativeList<int>(Features.RemeberedRecentlyPlayedClipsCount, Allocator.Persistent);
			}
			else
			{
				recentlyPlayedClipsIndexes = new NativeList<int>(0, Allocator.Persistent);
			}

		}

		public override void Awake()
		{
			base.Awake();

		}

		public override void Start()
		{
			base.Start();

			throw new System.Exception($"Contact state \"{this.m_DataState.Name} cannot be start state!\"");

		}

		public override void Enter(SwitchStateInfo switchStateInfo)
		{
			base.Enter(switchStateInfo);

			UpdateSectionMask();

			isCurrentContactValid = false;
			isStartMovingToContactCallbackPerformed = false;

			isFindingRunning = true;
			transitionBlendTime = switchStateInfo.BlendTime;

			m_CurrentClipGlobalTime = 0;
			m_CurrentClipLocalTime = 0;

			motionMatching.UpdataCurrentInputTrajectory(CurrentMotionGroup.TrajectoryCostWeight);
			m_LogicLayer.PrepareAndStartPoseCalculationJob();

			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.ContactStateJob.BatchSize = batchSize;
			m_LogicLayer.ContactStateJob.ContactsCount = this.CurrentMotionGroup.MotionDataInfos[0].ContactPoints.Count;
			m_LogicLayer.ContactStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.ContactStateJob.CurrentPose = m_LogicLayer.CurrentPose;

			m_LogicLayer.MultiplayContactsByWeightFromNativeMotionGroup(CurrentMotionGroup.ContactsCostWeight);

			m_LogicLayer.ContactStateJob.CurrentContactPoints = m_LogicLayer.localSpaceContacts;
			m_LogicLayer.ContactStateJob.SectionMask = m_CurrentSectionMask;

			// Recently played clips


			if (!Features.NotSearchInRecentClips)
			{
				recentlyPlayedClipsIndexes.Clear();
			}
			else
			{
				float deltaTimeFromLastEnterToState = Time.time - lastEnterToStateTime;
				if (deltaTimeFromLastEnterToState >= Features.TimeToResetRecentlyPlayedClips ||
					recentlyPlayedClipsIndexes.Length >= (CurrentMotionGroup.MotionDataInfos.Count - 1))
				{
					lastEnterToStateTime = Time.time;
					recentlyPlayedClipsIndexes.Clear();
				}
			}

			m_LogicLayer.ContactStateJob.RecentlyPlayedClipsIndexes = this.recentlyPlayedClipsIndexes;
			//

			m_LogicLayer.ContactStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.ContactStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.ContactStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.ContactStateJob.Contacts = CurrentMotionGroup.Contacts;
			m_LogicLayer.ContactStateJob.Outputs = m_LogicLayer.JobsOutput;

			m_LogicLayer.ContactJobHandle = m_LogicLayer.ContactStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize,
				m_LogicLayer.poseCalculationJobHandle
				);

			//JobHandle.ScheduleBatchedJobs();
			//CompleteContactJob();
		}

		public override void Exit()
		{
			base.Exit();

		}

		public override void FixedUpdate()
		{
			if (isFindingRunning)
			{
				return;
			}
			base.FixedUpdate();
		}

		public override void Update()
		{
			if (isFindingRunning)
			{
				if (!CompleteContactJob())
				{
					return;
				}
			}
			base.Update();

			if (Features.UseTimeScaling &&
				isCurrentContactValid
				&& m_CurrentClipGlobalTime < CurrentClipInfo.Length &&
				m_CurrentClipLocalTime >= currentAnimContact.StartMoveToContactTime &&
				m_CurrentClipLocalTime < currentAnimContact.startTime)
			{
				HandleTimeScaling();
			}
		}

		public override void LateUpdate()
		{
			if (isFindingRunning)
			{
				return;
			}

			base.LateUpdate();

			if (m_IsPlayingCurrentStateAnimation)
			{
				m_CurrentClipGlobalTime = StateMixer.GetLastAnimationTime();
				if (m_CurrentClipGlobalTime > CurrentClipInfo.Length)
				{
					m_CurrentClipLocalTime = CurrentClipInfo.Length;
				}
				else
				{
					m_CurrentClipLocalTime = m_CurrentClipGlobalTime % CurrentClipInfo.Length;
				}
			}

			if (isCurrentContactValid && m_CurrentClipGlobalTime < CurrentClipInfo.Length)
			{
				OnContactReach();
				ChangeContactStateIndex();

				switch (Features.RotationType)
				{
					case ContactRotatationType.RotateOnContact:
						{
							RotateOnContacts();
						}
						break;
					case ContactRotatationType.RotateToConatct:
						{
							RotateToContactPoint();
						}
						break;
					case ContactRotatationType.RotateToAndOnContact:
						{
							RotateToContactPoint();
							RotateOnContacts();
						}
						break;
				}

				MoveBetweenContactPoints();
				if (Features.MoveToContactWhenOnContact && Features.PositionCorrectionType == ContactPointPositionCorrectionType.MovePosition)
				{
					MoveToContactWhenOnContact();
				}

			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			if (recentlyPlayedClipsIndexes.IsCreated)
			{
				recentlyPlayedClipsIndexes.Dispose();
			}
		}

		internal override void CompleteScheduledJobs()
		{
			m_LogicLayer.ContactJobHandle.Complete();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>True if job is completed else false.</returns>
		private bool CompleteContactJob()
		{
			//if (!m_LogicLayer.ContactJobHandle.IsCompleted)
			//{
			//	return false;
			//}

			isFindingRunning = false;
			m_LogicLayer.ContactJobHandle.Complete();



			MotionMatchingJobOutput output = JoinJobsOutput();

			PlayAnimation(output, transitionBlendTime, 0f);
			HandleRecentlyPlayedClips(output.FrameClipIndex);
			GetDestinationContactIndex();

			for (int i = 0; i < Behaviors.Count; i++)
			{
				Behaviors[i].OnCompleteEnterFindingJob();
			}

			if (Features.Adapt)
			{
				AdaptPoints();
			}
			return true;
		}

		private void GetDestinationContactIndex()
		{
			isStartMovingToContactCallbackPerformed = false;
			isCurrentContactValid = false;
			for (int i = 0; i < CurrentClipInfo.ContactPoints.Count; i++)
			{
				if (m_CurrentClipLocalTime < CurrentClipInfo.ContactPoints[i].startTime)
				{
					destinationContactIndex = i;
					currentAnimContact = CurrentClipInfo.ContactPoints[i];
					isCurrentContactValid = true;
					isDestinationContactReached = false;
					isStartLerpPositionValid = false;

					for (int behaviorIndex = 0; behaviorIndex < Behaviors.Count; behaviorIndex++)
					{
						Behaviors[behaviorIndex].OnDestinationContactPointChange(destinationContactIndex);
					}
					break;
				}
			}
		}

		void MoveBetweenContactPoints() // called in late update
		{
			if (currentAnimContact.StartMoveToContactTime <= m_CurrentClipLocalTime && m_CurrentClipLocalTime <= currentAnimContact.startTime)
			{
				if (!isStartMovingToContactCallbackPerformed)
				{
					isStartMovingToContactCallbackPerformed = true;
					for (int i = 0; i < Behaviors.Count; i++)
					{
						Behaviors[i].OnBeginMovingToContactPoint(destinationContactIndex);
					}
				}

				switch (Features.PositionCorrectionType)
				{
					case ContactPointPositionCorrectionType.MovePosition:
						{
							MoveToDestinationContact();
						}
						break;
					case ContactPointPositionCorrectionType.LerpPosition:
						{
							ChangeStartLerpPosition();
							LerpToDestinationContact();
						}
						break;
					case ContactPointPositionCorrectionType.LerpWithCurve:
						{
							ChangeStartLerpPosition();
							LerpWithCurveToDestinationPoint();
						}
						break;
				}
			}
		}

		private void ChangeContactStateIndex()
		{
			if (m_CurrentClipLocalTime >= currentAnimContact.endTime)
			{
				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].OnEndContact(destinationContactIndex);
				}

				destinationContactIndex += 1;


				if (destinationContactIndex >= CurrentClipInfo.ContactPoints.Count)
				{
					isCurrentContactValid = false;
					return;
				}

				currentAnimContact = CurrentClipInfo.ContactPoints[destinationContactIndex];
				isCurrentContactValid = true;

				isStartMovingToContactCallbackPerformed = false;
				isDestinationContactReached = false;
				isStartLerpPositionValid = false;

				for (int i = 0; i < Behaviors.Count; i++)
				{
					Behaviors[i].OnDestinationContactPointChange(destinationContactIndex);
				}
			}
		}

		private void OnContactReach()
		{
			if (destinationContactIndex < CurrentClipInfo.ContactPoints.Count)
			{
				if (m_CurrentClipLocalTime >= currentAnimContact.startTime &&
					!isDestinationContactReached)
				{
					isDestinationContactReached = true;
					for (int i = 0; i < this.Behaviors.Count; i++)
					{
						Behaviors[i].OnAchiveContactPoint(destinationContactIndex);
					}
				}
			}
		}

		private void MoveToDestinationContact()
		{
			float timeToDstContact = currentAnimContact.startTime - m_CurrentClipLocalTime;

			if (timeToDstContact < minDeltaTimeToPerformMovement)
			{
				return;
			}

			CurrentMotionGroup.GetContactInTime(
				ref currentFrameContact,
				m_CurrentClipLocalTime,
				m_CurrentClipIndex,
				destinationContactIndex
				);

			FrameContact GlobalSpaceContact = new FrameContact(
				transform.TransformPoint(currentFrameContact.position),
				transform.TransformDirection(currentFrameContact.normal),
				false
				);


			float3 deltaPosition = m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position - GlobalSpaceContact.position;
			float3 velocity = deltaPosition / timeToDstContact;


			transform.position = transform.position + (Vector3)(velocity * Time.deltaTime);
		}

		private void LerpToDestinationContact()
		{
			//float timeToDstContact = currentAnimContact.startTime - m_CurrentClipLocalTime;

			////if (timeToDstContact < minDeltaTimeToPerformMovement)
			////{
			////	return;
			////}

			CurrentMotionGroup.GetContactInTime(
				ref currentFrameContact,
				m_CurrentClipLocalTime,
				m_CurrentClipIndex,
				destinationContactIndex
				);

			FrameContact GlobalSpaceContact = new FrameContact(
				transform.TransformPoint(currentFrameContact.position),
				transform.TransformDirection(currentFrameContact.normal),
				false
				);



			float factor = Mathf.Clamp01(
				(m_CurrentClipLocalTime - currentAnimContact.StartMoveToContactTime)
				/
				(currentAnimContact.startTime - currentAnimContact.StartMoveToContactTime));

			Vector3 contactPosDelta = m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position - GlobalSpaceContact.position;
			Vector3 desiredObjectPos = transform.position + contactPosDelta;

			transform.position = Vector3.Lerp(startPositionLerp, desiredObjectPos, factor);
		}

		private void LerpWithCurveToDestinationPoint()
		{
			//float timeToDstContact = currentAnimContact.endTime - m_CurrentClipLocalTime;

			//if (timeToDstContact < minDeltaTimeToPerformMovement)
			//{
			//	return;
			//}

			CurrentMotionGroup.GetContactInTime(
				ref currentFrameContact,
				m_CurrentClipLocalTime,
				m_CurrentClipIndex,
				destinationContactIndex
				);

			FrameContact GlobalSpaceContact = new FrameContact(
				transform.TransformPoint(currentFrameContact.position),
				transform.TransformDirection(currentFrameContact.normal),
				false
				);


			float factor =
				(m_CurrentClipLocalTime - currentAnimContact.StartMoveToContactTime)
				/
				(currentAnimContact.startTime - currentAnimContact.StartMoveToContactTime);


			Vector3 contactPosDelta = m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position - GlobalSpaceContact.position;
			Vector3 desiredObjectPos = transform.position + contactPosDelta;

			factor = Mathf.Clamp01(Features.LerpPositionCurve.Evaluate(factor));

			transform.position = Vector3.Lerp(startPositionLerp, desiredObjectPos, factor);
		}

		private void RotateOnContacts()
		{
			if (m_CurrentClipLocalTime > currentAnimContact.startTime &&
				m_CurrentClipLocalTime <= currentAnimContact.endTime)
			{
				float deltaTime = currentAnimContact.endTime - m_CurrentClipLocalTime;
				if (deltaTime < minDeltaTimeToPerformMovement)
				{
					return;
				}

				Quaternion desiredRotation = Quaternion.LookRotation(
					m_LogicLayer.globalSpaceContacts[destinationContactIndex].forward,
					Vector3.up
					);

				float angleToDesiredRotation = Quaternion.Angle(transform.rotation, desiredRotation);
				float rotationSpeed = angleToDesiredRotation / deltaTime;

				transform.rotation = Quaternion.RotateTowards(
					transform.rotation,
					desiredRotation,
					rotationSpeed * Time.deltaTime
					);
			}
		}

		private void RotateToContactPoint()
		{
			if (currentAnimContact.StartMoveToContactTime <= m_CurrentClipLocalTime && m_CurrentClipLocalTime < currentAnimContact.startTime)
			{
				float rotationTime = Mathf.Abs((
					currentAnimContact.startTime - m_CurrentClipLocalTime) / m_DataState.SpeedMultiplier);

				if (rotationTime < minDeltaTimeToPerformMovement)
				{
					return;
				}

				int index = destinationContactIndex;
				int nextIndex = index + 1;
				Vector3 contactsDirection;

				if (nextIndex < CurrentClipInfo.ContactPoints.Count && Features.UseDirBetweenContactsToCorrectRotation)
				{
					contactsDirection = m_LogicLayer.globalSpaceContacts[nextIndex].frameContact.position - m_LogicLayer.globalSpaceContacts[index].frameContact.position;

					contactsDirection = Vector3.ProjectOnPlane(contactsDirection, Vector3.up).normalized;
					contactsDirection = CurrentClipInfo.ContactPoints[index].rotationFromForwardToNextContactDir * contactsDirection;
				}
				else
				{
					contactsDirection = m_LogicLayer.globalSpaceContacts[index].forward;
					contactsDirection = Vector3.ProjectOnPlane(contactsDirection, Vector3.up).normalized;
				}

				Quaternion rotationOnContact = Quaternion.LookRotation(contactsDirection, Vector3.up);

				float degreeSpeed = Quaternion.Angle(this.transform.rotation, rotationOnContact) / rotationTime;

				transform.rotation = Quaternion.RotateTowards(
					transform.rotation,
					rotationOnContact,
					degreeSpeed * Time.deltaTime
					);
			}
		}

		public override void RunTestJob()
		{
			int batchSize = CurrentMotionGroup.FramesPerThread;
			m_LogicLayer.ContactStateJob.BatchSize = batchSize;
			m_LogicLayer.ContactStateJob.ContactsCount = this.CurrentMotionGroup.MotionDataInfos[0].ContactPoints.Count;
			//logicLayer.ContactStateJob.TrajectoryWeight = dataState.TrajectoryCostWeight;
			//logicLayer.ContactStateJob.PoseWeight = dataState.PoseCostWeight;
			//m_LogicLayer.ContactStateJob.ContactsWeight = Features.contactPointsWeight;
			//m_LogicLayer.ContactStateJob.ContactsCostType = Features.contactCostType;
			m_LogicLayer.ContactStateJob.CurrentTrajectory = motionMatching.InputLocalTrajectory;
			m_LogicLayer.ContactStateJob.CurrentPose = m_LogicLayer.CurrentPose;

			for (int i = 0; i < this.CurrentMotionGroup.MotionDataInfos[0].ContactPoints.Count; i++)
			{
				m_LogicLayer.localSpaceContacts.Add(new FrameContact());
			}

			m_LogicLayer.ContactStateJob.CurrentContactPoints = m_LogicLayer.localSpaceContacts;
			m_LogicLayer.ContactStateJob.SectionMask = m_CurrentSectionMask;

			//
			m_LogicLayer.ContactStateJob.RecentlyPlayedClipsIndexes = this.recentlyPlayedClipsIndexes;
			//

			m_LogicLayer.ContactStateJob.Frames = CurrentMotionGroup.Frames;
			m_LogicLayer.ContactStateJob.TrajectoryPoints = CurrentMotionGroup.TrajectoryPoints;
			m_LogicLayer.ContactStateJob.Bones = CurrentMotionGroup.Bones;
			m_LogicLayer.ContactStateJob.Contacts = CurrentMotionGroup.Contacts;
			m_LogicLayer.ContactStateJob.Outputs = m_LogicLayer.JobsOutput;


			m_LogicLayer.ContactJobHandle = m_LogicLayer.ContactStateJob.ScheduleBatch(
				CurrentMotionGroup.Frames.Length,
				batchSize
				);
		}

		public void SetGlobalContactPoints(ref SwitchStateContact[] contacts)
		{
			for (int i = 0; i < contacts.Length; i++)
			{
				m_LogicLayer.globalSpaceContacts[i] = contacts[i];
			}
		}

		public void SetGlobalContactPoints(List<SwitchStateContact> contacts)
		{
			for (int i = 0; i < contacts.Count; i++)
			{
				m_LogicLayer.globalSpaceContacts[i] = contacts[i];
			}
		}

		private void AdaptPoints()
		{
			if (m_LogicLayer.globalSpaceContacts.Count <= 1) return;

			int StartContactIndex = 0;

			// rotation on adapt start point:
			SwitchStateContact startContact = m_LogicLayer.globalSpaceContacts[0];
			SwitchStateContact secondContact = m_LogicLayer.globalSpaceContacts[1];

			Vector3 contactsDirection = secondContact.frameContact.position - startContact.frameContact.position;
			contactsDirection = Vector3.ProjectOnPlane(contactsDirection, Vector3.up);

			if (contactsDirection == Vector3.zero)
			{
				contactsDirection = startContact.forward;
			}

			contactsDirection = CurrentClipInfo.ContactPoints[StartContactIndex].rotationFromForwardToNextContactDir * contactsDirection;
			Quaternion rotationOnContact = Quaternion.LookRotation(contactsDirection, Vector3.up);

			Matrix4x4 pointMatrix = Matrix4x4.TRS(
				m_LogicLayer.globalSpaceContacts[StartContactIndex].frameContact.position,
				rotationOnContact,
				Vector3.one
				);

			FrameContact startAdaptContact = new FrameContact();
			CurrentMotionGroup.GetContactInTime(
				ref startAdaptContact,
				CurrentClipInfo.ContactPoints[StartContactIndex].startTime,
				m_CurrentClipIndex,
				StartContactIndex
				);

			Vector3 startPosition =
				(Vector3)m_LogicLayer.globalSpaceContacts[StartContactIndex].frameContact.position -
				(pointMatrix.MultiplyPoint3x4(startAdaptContact.position)
				-
				(Vector3)m_LogicLayer.globalSpaceContacts[StartContactIndex].frameContact.position
				);



			Matrix4x4 matrix = Matrix4x4.TRS(startPosition, rotationOnContact, Vector3.one);

			for (int i = StartContactIndex + 1; i < m_LogicLayer.globalSpaceContacts.Count; i++)
			{
				SwitchStateContact buffor = m_LogicLayer.globalSpaceContacts[i];
				FrameContact localContact = new FrameContact();
				CurrentMotionGroup.GetContactInTime(
					ref localContact,
					CurrentClipInfo.ContactPoints[StartContactIndex].startTime,
					m_CurrentClipIndex,
					i
					);

				Vector3 worldSpacePos = matrix.MultiplyPoint3x4(localContact.position);
				Vector3 worldInverseNormal = matrix.MultiplyVector(localContact.normal);

				// perform raycast for geting addapted point
				Vector3 startRaycast = worldSpacePos - worldInverseNormal * Features.BackDeltaForAdapting;

				RaycastHit hit;
				if (Physics.Raycast(startRaycast, worldInverseNormal, out hit, Features.AdaptRaycastLength, Features.AdaptLayerMask))
				{
					buffor.frameContact.position = hit.point;
				}
				else
				{
					buffor.frameContact.position = worldSpacePos;
				}

				m_LogicLayer.globalSpaceContacts[i] = buffor;
			}
		}

		private void HandleRecentlyPlayedClips(int currentPlayedAnimationIndex)
		{
			if (recentlyPlayedClipsIndexes.Length >= Features.RemeberedRecentlyPlayedClipsCount)
			{
				while (recentlyPlayedClipsIndexes.Length >= Features.RemeberedRecentlyPlayedClipsCount && recentlyPlayedClipsIndexes.Length > 0)
				{
					recentlyPlayedClipsIndexes.RemoveAt(0);
				}
			}

			recentlyPlayedClipsIndexes.Add(currentPlayedAnimationIndex);
		}

		private void HandleTimeScaling()
		{
			FrameContact contact = new FrameContact();
			CurrentMotionGroup.GetContactInTime(
				   ref contact,
				   m_CurrentClipLocalTime,
				   m_CurrentClipIndex,
				   destinationContactIndex
				   );

			FrameContact globalSpaceContact = new FrameContact(
				transform.TransformPoint(contact.position),
				transform.TransformDirection(contact.normal),
				false
				);


			Vector3 deltaToSetedContact = Vector3.zero;
			Vector3 deltaToAnimationContact = Vector3.zero;

			if ((Features.TimeScalingPositionMask & ContactStateTimeScalingPositionMask.X) != 0)
			{
				deltaToSetedContact.x = transform.position.x - m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position.x;
				deltaToAnimationContact.x = transform.position.x - globalSpaceContact.position.x;
			}
			if ((Features.TimeScalingPositionMask & ContactStateTimeScalingPositionMask.Y) != 0)
			{
				deltaToSetedContact.y = transform.position.y - m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position.y;
				deltaToAnimationContact.y = transform.position.y - globalSpaceContact.position.y;
			}
			if ((Features.TimeScalingPositionMask & ContactStateTimeScalingPositionMask.Z) != 0)
			{
				deltaToSetedContact.z = transform.position.z - m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position.z;
				deltaToAnimationContact.z = transform.position.z - globalSpaceContact.position.z;
			}

			float distanceToSetedContact = deltaToSetedContact.magnitude;
			float distanceToAnimationContact = deltaToAnimationContact.magnitude;

			if (distanceToAnimationContact != 0 && distanceToSetedContact != 0)
			{
				float speedMultiplier = Mathf.Clamp(
					distanceToAnimationContact / distanceToSetedContact,
					Features.MinTimeSpeedMultiplier,
					Features.MaxTimeSpeedMultiplier
					);

				StateMixer.SetSpeed(speedMultiplier);
			}
		}

		private void MoveToContactWhenOnContact()
		{
			if (currentAnimContact.startTime < m_CurrentClipLocalTime && m_CurrentClipLocalTime <= currentAnimContact.endTime)
			{
				MovePositionOnContact();
			}
		}

		private void MovePositionOnContact()
		{
			float timeToDstContact = currentAnimContact.endTime - m_CurrentClipLocalTime;

			if (timeToDstContact < minDeltaTimeToPerformMovement)
			{
				return;
			}

			CurrentMotionGroup.GetContactInTime(
				ref currentFrameContact,
				m_CurrentClipLocalTime,
				m_CurrentClipIndex,
				destinationContactIndex
				);

			FrameContact GlobalSpaceContact = new FrameContact(
				transform.TransformPoint(currentFrameContact.position),
				transform.TransformDirection(currentFrameContact.normal),
				false
				);


			float3 deltaPosition = m_LogicLayer.globalSpaceContacts[destinationContactIndex].frameContact.position - GlobalSpaceContact.position;
			float3 velocity = deltaPosition / timeToDstContact;


			transform.position = transform.position + (Vector3)(velocity * Time.deltaTime);
		}

		private void ChangeStartLerpPosition()
		{
			if (currentAnimContact.StartMoveToContactTime <= m_CurrentClipLocalTime && !isStartLerpPositionValid)
			{
				isStartLerpPositionValid = true;
				startPositionLerp = transform.position;
			}

		}
#if UNITY_EDITOR
		public override void OnDrawGizmos()
		{

			int index = 0;
			foreach (SwitchStateContact contact in m_LogicLayer.globalSpaceContacts)
			{
				// animation contacts
				FrameContact animContact = new FrameContact();
				CurrentMotionGroup.GetContactInTime(
					   ref animContact,
					   m_CurrentClipLocalTime,
					   m_CurrentClipIndex,
					   index
					   );

				FrameContact globalSpaceContact = new FrameContact(
					transform.TransformPoint(animContact.position),
					transform.TransformDirection(animContact.normal),
					false
					);

				index++;

				Gizmos.color = Color.yellow;
				Gizmos.DrawWireSphere(globalSpaceContact.position, 0.05f);


				// seted contacts
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(contact.frameContact.position, 0.05f);
				Gizmos.color = Color.blue;
				MM_Gizmos.DrawArrow(
					contact.frameContact.position,
					contact.frameContact.position + contact.frameContact.normal,
					0.1f
					);
				Gizmos.color = Color.green;
				MM_Gizmos.DrawArrow(
					contact.frameContact.position,
					(Vector3)contact.frameContact.position + contact.forward,
					0.1f
					);
			}
		}

#endif
		public override bool ShouldPerformMotionMatchingLooking()
		{
			return false;
		}
	}
}