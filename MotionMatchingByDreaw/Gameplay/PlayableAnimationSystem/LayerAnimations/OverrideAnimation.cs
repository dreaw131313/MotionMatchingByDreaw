using UnityEngine;
using UnityEngine.Events;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class AnimationUpdateEvent : UnityEvent<float> { }

	[System.Serializable]
	public class OverrideAnimation : BaseLayerAnimation
	{
		[SerializeField]
		public AnimationClip Clip;

		public OverrideAnimation()
		{
			SpeedMultiplayer = 1f;
			StartTime = 0f;
			StartBlendTime = 0.25f;
			EndTime = float.MaxValue;
			EndBlendTime = 0.25f;
			ChangeLayerWeight = true;
		}

		public OverrideAnimation(
			AnimationClip clip,
			float startTime,
			float startBlendTime,
			float endTime,
			float endBlendTime,
			bool canBeInterupted = true
			)
		{
			this.Clip = clip;
			this.StartTime = startTime;
			this.StartBlendTime = startBlendTime;
			this.EndTime = endTime;
			this.EndBlendTime = endBlendTime;
		}

		public override bool IsValid => Clip != null;

		public override AnimationClip GetAnimationClip()
		{
			return Clip;
		}

		public override void OnBeginPlay_Internal(PlayableSecondaryAnimationLayer layer)
		{

		}

		public override void Update_Internal(PlayableSecondaryAnimationLayer layer)
		{

		}
	}
}
