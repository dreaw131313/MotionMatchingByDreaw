using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class LayerAnimationWithEvents : BaseLayerAnimation
	{
		[SerializeField]
		public MotionMatchingLayerAnimation Clip;

		public override bool IsValid => Clip != null && Clip.IsValid;

		public override AnimationClip GetAnimationClip()
		{
			return Clip.m_Animation;
		}

		public override void OnBeginPlay_Internal(PlayableSecondaryAnimationLayer layer)
		{
			Clip.SelectEvent(
				layer.GetCurrentClipTime(),
				ref layer.m_CurrentEvent,
				ref layer.m_ShouldFireAnimationEvents,
				ref layer.m_AnimationEventTime,
				ref layer.m_CurrentAnimationEventIndex
				);
		}

		public override void Update_Internal(PlayableSecondaryAnimationLayer layer)
		{
			if (layer.m_ShouldFireAnimationEvents)
			{
				Clip.InvokeEvents(
					layer.m_MotionMatchingComponent,
					ref layer.m_CurrentEvent,
					layer.GetCurrentClipTime(),
					ref layer.m_ShouldFireAnimationEvents,
					ref layer.m_CurrentAnimationEventIndex,
					ref layer.m_AnimationEventTime
					);
			}
		}
	}
}