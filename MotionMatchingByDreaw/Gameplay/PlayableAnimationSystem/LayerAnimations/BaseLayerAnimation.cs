using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public abstract class BaseLayerAnimation
	{
		[SerializeField]
		[Min(0.0001f)]
		public float SpeedMultiplayer = 1f;
		[SerializeField]
		public float StartTime = 0f;
		[SerializeField]
		public float StartBlendTime = 0.25f;
		[SerializeField]
		public float EndTime = float.MaxValue;
		[SerializeField]
		public float EndBlendTime = 0.25f;
		[SerializeField]
		public bool ChangeLayerWeight;

		// EVENTS
		[SerializeField]
		[HideInInspector]
		public UnityEvent StartPlayingEvent;
		[SerializeField]
		[HideInInspector]
		//[Tooltip("Float parameter is current animation time.")]
		public AnimationUpdateEvent UpdateEvent;
		[SerializeField]
		[HideInInspector]
		//[Tooltip("Float parameter is current animation time.")]
		public AnimationUpdateEvent LateUpdateEvent;
		[SerializeField]
		[HideInInspector]
		public UnityEvent EndPlayingEvent;

		public abstract bool IsValid { get; }
		public abstract AnimationClip GetAnimationClip();

		public abstract void Update_Internal(PlayableSecondaryAnimationLayer layer);
		public abstract void OnBeginPlay_Internal(PlayableSecondaryAnimationLayer layer);


		public virtual void OnBeginPlay()
		{
			StartPlayingEvent?.Invoke();
		}

		public virtual void OnEndPlaying()
		{
			EndPlayingEvent?.Invoke();
		}


		public virtual void Update(float animationTime)
		{
			UpdateEvent?.Invoke(animationTime);
		}

		public virtual void LateUpdate(float animationTime)
		{
			LateUpdateEvent?.Invoke(animationTime);
		}
	}
}