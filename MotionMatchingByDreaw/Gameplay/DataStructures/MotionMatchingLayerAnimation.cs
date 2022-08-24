using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "MotionMatchingLayerAnimation", menuName = "Motion Matching/Data/Layer Animation")]
	public class MotionMatchingLayerAnimation : ScriptableObject
	{
		[SerializeField]
		public AnimationClip m_Animation;

		[SerializeField]
		public List<MotionMatchingAnimationEvent> m_MotionMatchingEvents;

		public LayerAniamtionType AniamtionType => LayerAniamtionType.NormalAnimation;

		public bool IsValid => m_Animation != null;


		public void SelectEvent(
			float time,
			ref MotionMatchingAnimationEvent currentEvent,
			ref bool shouldFireAnimationEvents,
			ref float animationEventTime,
			ref int currentAnimationEventIndex
			)
		{
			float clipLength = m_Animation.length;

			if (time > clipLength && !m_Animation.isLooping)
			{
				shouldFireAnimationEvents = false;
				currentEvent = null;
				animationEventTime = float.MaxValue;
				currentAnimationEventIndex = -1;
				return;
			}

			float localTime = time % clipLength;
			int loops = Mathf.FloorToInt(time / clipLength);
			float aditionalEventTimeDelta = loops * clipLength;

			if (m_MotionMatchingEvents != null && m_MotionMatchingEvents.Count > 0)
			{
				int eventsCount = m_MotionMatchingEvents.Count;
				for (int i = 0; i < eventsCount; i++)
				{
					MotionMatchingAnimationEvent checkedEvent = m_MotionMatchingEvents[i];
					if (checkedEvent.EventTime >= localTime)
					{
						shouldFireAnimationEvents = true;
						animationEventTime = checkedEvent.EventTime + aditionalEventTimeDelta;
						currentEvent = checkedEvent;
						currentAnimationEventIndex = i;
						return;
					}
				}

				if (m_Animation.isLooping)
				{
					shouldFireAnimationEvents = true;
					currentEvent = m_MotionMatchingEvents[0];
					animationEventTime = currentEvent.EventTime + Mathf.CeilToInt(time / clipLength)* clipLength;
					currentAnimationEventIndex = 0;
					return;
				}
			}

			shouldFireAnimationEvents = false;
			currentEvent = null;
			animationEventTime = float.MaxValue;
				currentAnimationEventIndex = -1;
		}

		public void InvokeEvents(
			MotionMatchingComponent motionMatching,
			// const data:
			ref MotionMatchingAnimationEvent currentEvent,
			float currentGlobalTime,
			// changing data
			ref bool shouldFireAnimationEvents,
			ref int currentAnimationEventIndex,
			ref float animationEventTime
			)
		{
			if (m_MotionMatchingEvents != null && m_MotionMatchingEvents.Count > 0 && shouldFireAnimationEvents)
			{
				if (m_Animation.isLooping)
				{
					while (currentGlobalTime >= animationEventTime)
					{
						motionMatching.InvokeAnimationEvent(currentEvent.Name);

						currentAnimationEventIndex = (currentAnimationEventIndex + 1) % m_MotionMatchingEvents.Count;
						currentEvent = m_MotionMatchingEvents[currentAnimationEventIndex];

						if (currentAnimationEventIndex == 0)
						{
							animationEventTime = Mathf.Ceil(currentGlobalTime / m_Animation.length) * m_Animation.length + currentEvent.EventTime;
						}
						else
						{
							animationEventTime = Mathf.Floor(currentGlobalTime / m_Animation.length) * m_Animation.length + currentEvent.EventTime;
						}
					}
				}
				else
				{

					float currentLocalTime = currentGlobalTime % m_Animation.length;
					while (currentLocalTime >= currentEvent.EventTime)
					{
						motionMatching.InvokeAnimationEvent(currentEvent.Name);

						currentAnimationEventIndex = (currentAnimationEventIndex + 1) % m_MotionMatchingEvents.Count;

						if (currentAnimationEventIndex == 0)
						{
							shouldFireAnimationEvents = false;
							break;
						}
						else
						{
							currentEvent = m_MotionMatchingEvents[currentAnimationEventIndex];
						}
					}
				}
			}
		}
	}


	public enum LayerAniamtionType
	{
		NormalAnimation
	}
}