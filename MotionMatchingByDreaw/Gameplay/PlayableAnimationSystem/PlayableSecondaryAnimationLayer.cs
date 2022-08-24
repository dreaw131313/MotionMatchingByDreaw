using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public class PlayableSecondaryAnimationLayer
	{
		internal int layerMixerInputIndex;
		private SecondaryLayerData layerData;
		private PlayableAnimationSystem animationSystem;
		private Animator animator;
		public MotionMatchingComponent m_MotionMatchingComponent;


		private IEnumerator layerWeightChangeCoroutine;
		public AnimationMixerPlayable animationMixer;


		List<PlayableLayerBlendedAnimationInfo> blendedAnimationsInfos = new List<PlayableLayerBlendedAnimationInfo>();
		List<OverrideAnimationPlayingData> animationPlayingData = new List<OverrideAnimationPlayingData>();

		//bool shouldPlayAnimation = false;
		BaseLayerAnimation animationToPlay;
		private int currentPriority;

		bool shouldAnimationBeStoppedInstantly = false;

		// Invoking animation events:
		public MotionMatchingAnimationEvent m_CurrentEvent;
		public bool m_ShouldFireAnimationEvents;
		public float m_AnimationEventTime;
		public int m_CurrentAnimationEventIndex;


		public int LayerMixerInputIndex { get => layerMixerInputIndex; private set => layerMixerInputIndex = value; } // input index in animation output playable
		public int Index { get; private set; }

		public PlayableSecondaryAnimationLayer(
			int layerMixerInputIndex,
			int index,
			Animator animator,
			SecondaryLayerData layerData,
			PlayableAnimationSystem animationSystem,
			MotionMatchingComponent motionMatching
			)
		{
			Index = index;
			this.layerMixerInputIndex = layerMixerInputIndex;
			this.animator = animator;
			this.layerData = layerData;
			this.animationSystem = animationSystem;
			this.m_MotionMatchingComponent = motionMatching;
		}

		public void Initialize()
		{
			animationMixer = AnimationMixerPlayable.Create(animationSystem.Graph, 0);
			animationSystem.layerMixer.AddInput(animationMixer, 0, 0f);

			if (layerData.Mask != null)
			{
				animationSystem.layerMixer.SetLayerMaskFromAvatarMask((uint)layerMixerInputIndex, layerData.Mask);
			}
			animationSystem.layerMixer.SetLayerAdditive((uint)layerMixerInputIndex, layerData.IsAdditive);



			//shouldPlayAnimation = false;
			animationToPlay = null;
			currentPriority = int.MinValue;
			shouldAnimationBeStoppedInstantly = false;
		}

		public void Update(float deltaTime)
		{
			StopAnimationInstantly();
			PlayAnimation();

			BlendAnimations_Old(deltaTime);
			SetAnimationsWeight();
			RemoveZeroWeightsInput();

			if (animationPlayingData.Count > 0)
			{
				int lastIndex = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData currentAnim = animationPlayingData[lastIndex];
				if (!currentAnim.WasEndEventInvoked)
				{
					float animTime = (float)animationMixer.GetInput(lastIndex).GetTime();
					currentAnim.Anim.Update(animTime);
				}
			}
		}

		public void LateUpdate()
		{
			if (animationPlayingData.Count > 0)
			{
				int lastIndex = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData currentAnim = animationPlayingData[lastIndex];
				if (!currentAnim.WasEndEventInvoked)
				{
					float animTime = (float)animationMixer.GetInput(lastIndex).GetTime();
					currentAnim.Anim.LateUpdate(animTime);
					currentAnim.Anim.Update_Internal(this);
				}
			}
		}

		public void SetLayerWeight(
			float weight,
			float changeTime
			)
		{
			if (layerWeightChangeCoroutine != null)
			{
				m_MotionMatchingComponent.StopCoroutine(layerWeightChangeCoroutine);
			}

			if (animationSystem.layerMixer.GetInputWeight(LayerMixerInputIndex) == weight)
			{
				return;
			}

			if (changeTime == 0)
			{
				animationSystem.layerMixer.SetInputWeight(layerMixerInputIndex, weight);
				return;
			}

			layerWeightChangeCoroutine = ChangeWeightCoroutine(weight, changeTime, layerMixerInputIndex, animationSystem);

			m_MotionMatchingComponent.StartCoroutine(layerWeightChangeCoroutine);
		}

		private IEnumerator ChangeWeightCoroutine(
			float weight,
			float changeTime,
			int layerIndex,
			PlayableAnimationSystem animationSystem
			)
		{
			float currentWeight = animationSystem.layerMixer.GetInputWeight(layerIndex);
			float speed = (weight - currentWeight) / changeTime;

			while (animationSystem.layerMixer.GetInputWeight(layerIndex) != weight)
			{
				float newWeight = animationSystem.layerMixer.GetInputWeight(layerIndex) + speed * Time.deltaTime;
				if (speed < 0)
				{
					newWeight = Mathf.Clamp(newWeight, weight, currentWeight);
				}
				else
				{
					newWeight = Mathf.Clamp(newWeight, currentWeight, weight);
				}
				animationSystem.layerMixer.SetInputWeight(layerIndex, newWeight);

				yield return null;
			}
		}

		public bool SetAnimationToPlay(BaseLayerAnimation animation, int priority = int.MinValue)
		{
			if (animationToPlay == animation)
			{
				return false;
			}

			if (animationToPlay != null && priority < currentPriority)
			{
				return false;
			}

			animationToPlay = animation;
			currentPriority = priority;

			return true;
		}

		private void PlayAnimation()
		{
			if (animationToPlay != null)
			{
				OverrideAnimationPlayingData anim = new OverrideAnimationPlayingData(animationToPlay);
				animationToPlay = null;

				float weight = 0.00001f;
				if (anim.StartBlendTime == 0)
				{
					int lastIndex = animationPlayingData.Count - 1;
					if (animationPlayingData.Count > 0)
					{
						if (animationMixer.GetInput(lastIndex).GetTime() < animationPlayingData[lastIndex].EndTime)
						{
							animationPlayingData[lastIndex].Anim.OnEndPlaying();
						}

						for (int i = 0; i < animationPlayingData.Count; i++)
						{
							animationMixer.GetInput(i).Destroy();
						}

						blendedAnimationsInfos.Clear();
						animationPlayingData.Clear();
						animationMixer.SetInputCount(0);
					}

					blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
						1f / anim.StartBlendTime,
						1,
						0f,
						-1
						));

					weight = 1f;
				}
				else
				{
					blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
						1f / anim.StartBlendTime,
						weight,
						0f,
						-1
						));
				}

				int lastAnimIndex = animationPlayingData.Count - 1;
				if (animationPlayingData.Count > 0 &&
					animationMixer.GetInput(lastAnimIndex).GetTime() < animationPlayingData[lastAnimIndex].EndTime)
				{
					animationPlayingData[animationPlayingData.Count - 1].Anim.OnEndPlaying();
				}

				animationPlayingData.Add(anim);

				AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(animationSystem.Graph, anim.Anim.GetAnimationClip());
				clipPlayable.SetTime(anim.StartTime);
				clipPlayable.SetTime(anim.StartTime);
				clipPlayable.SetApplyFootIK(false);
				clipPlayable.SetApplyPlayableIK(false);
				clipPlayable.SetSpeed(anim.SpeedMultiplayer);


				animationMixer.AddInput(clipPlayable, 0, weight);



				if (anim.ChangeLayerWeight)
				{
					SetLayerWeight(1f, anim.StartBlendTime);
				}

				anim.Anim.OnBeginPlay_Internal(this);
				anim.Anim.OnBeginPlay();


				currentPriority = int.MinValue;
				//shouldPlayAnimation = false;
			}
		}

		private void BlendAnimations_Old(float deltaTime)
		{
			if (blendedAnimationsInfos.Count == 0)
			{
				return;
			}

			OverrideAnimationPlayingData currentAnim = animationPlayingData[animationPlayingData.Count - 1];

			int lastIndex = blendedAnimationsInfos.Count - 1;
			float weightSum = 0f;
			for (int i = 0; i < lastIndex; i++)
			{
				PlayableLayerBlendedAnimationInfo info = blendedAnimationsInfos[i];
				if (info.MinWeightToAchive <= info.CurrentWeight &&
					info.BlendingSpeed > 0)
				{
					info.BlendingSpeed = -(info.CurrentWeight / currentAnim.StartBlendTime);
				}
				info.CurrentWeight = Mathf.Clamp01(info.CurrentWeight + (info.BlendingSpeed * deltaTime));
				weightSum += info.CurrentWeight;
				blendedAnimationsInfos[i] = info;
			}

			PlayableLayerBlendedAnimationInfo lastInfo = blendedAnimationsInfos[lastIndex];
			OverrideAnimationPlayingData animationData = animationPlayingData[lastIndex];

			float currentAnimationTime = (float)animationMixer.GetInput(lastIndex).GetTime();

			if (currentAnimationTime > animationData.EndTime)
			{
				if (!animationData.WasEndEventInvoked)
				{
					animationData.WasEndEventInvoked = true;

					SetLayerWeight(0f, animationData.EndBlendTime);
					animationData.Anim.OnEndPlaying();

					animationPlayingData[lastIndex] = animationData;
				}

				lastInfo.CurrentWeight = Mathf.Clamp01(lastInfo.CurrentWeight - (deltaTime / animationData.EndBlendTime));
			}
			else
			{
				lastInfo.CurrentWeight = Mathf.Clamp01(lastInfo.CurrentWeight + lastInfo.BlendingSpeed * deltaTime);
			}

			blendedAnimationsInfos[lastIndex] = lastInfo;

		}

		private void SetAnimationsWeight()
		{
			if (blendedAnimationsInfos.Count == 0)
			{
				return;
			}

			int blendedAnimationsCount = blendedAnimationsInfos.Count;

			float weightSum = 0f;
			for (int i = 0; i < blendedAnimationsCount; i++)
			{
				weightSum += blendedAnimationsInfos[i].CurrentWeight;
			}

			for (int i = 0; i < blendedAnimationsCount; i++)
			{
				if (weightSum != 0)
				{
					animationMixer.SetInputWeight(i, blendedAnimationsInfos[i].CurrentWeight / weightSum);
				}
				else
				{
					animationMixer.SetInputWeight(i, blendedAnimationsInfos[i].CurrentWeight);
				}
			}


			//OverrideAnimationPlayingData lastData = animationPlayingData[blendedAnimationsCount - 1];

			//if (lastData.ChangeLayerWeight)
			//{
			//	if (blendedAnimationsCount > 1)
			//	{
			//		animationSystem.SetLayerWeight(layerMixerInputIndex, Mathf.Clamp01(weightSum));
			//	}
			//	else
			//	{
			//		animationMixer.SetInputWeight(0, 1f);
			//		animationSystem.SetLayerWeight(layerMixerInputIndex, blendedAnimationsInfos[0].CurrentWeight);
			//	}
			//}
		}

		private void RemoveZeroWeightsInput()
		{
			int size = animationMixer.GetInputCount();

			for (int i = 0; i < size; i++)
			{
				float weight = blendedAnimationsInfos[0].CurrentWeight;
				if (weight <= 0 /*&& i < (size - 1)*/)
				{
					animationMixer.GetInput(i).Destroy();
					blendedAnimationsInfos.RemoveAt(i);
					animationPlayingData.RemoveAt(i);

					for (int j = i + 1; j < size; j++)
					{
						// double localTime = ((AnimationClipPlayable)mixer.GetInput(j)).GetTime();
						float _weight = animationMixer.GetInputWeight(j);
						Playable clip = animationMixer.GetInput(j);
						// clip.SetTime(localTime);
						animationMixer.DisconnectInput(j);
						animationMixer.ConnectInput(j - 1, clip, 0);
						animationMixer.SetInputWeight(j - 1, _weight);
					}
					i--;
					size--;
					animationMixer.SetInputCount(size);
				}
			}
		}

		public void StopPlayingAnimation()
		{
			if (animationPlayingData.Count > 0)
			{
				animationToPlay = null;
				//shouldPlayAnimation = false;
				currentPriority = int.MinValue;
				shouldAnimationBeStoppedInstantly = true;
			}
		}

		private void StopAnimationInstantly()
		{
			if (shouldAnimationBeStoppedInstantly)
			{
				shouldAnimationBeStoppedInstantly = false;
				for (int i = 0; i < blendedAnimationsInfos.Count; i++)
				{
					animationMixer.GetInput(i).Destroy();
				}

				animationMixer.SetInputCount(0);

				int lastIndex = animationPlayingData.Count - 1;
				animationPlayingData[lastIndex].Anim.OnEndPlaying();

				animationSystem.SetLayerWeight(LayerMixerInputIndex, 0f);

				blendedAnimationsInfos.Clear();
				animationPlayingData.Clear();
			}
		}

		public void StopPlayingAnimationWithBlend(float blendTime)
		{
			if (animationPlayingData.Count > 0)
			{
				int lastIndex = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData data = animationPlayingData[lastIndex];

				data.EndTime = (float)animationMixer.GetInput(lastIndex).GetTime();
				data.EndBlendTime = blendTime;
				animationPlayingData[lastIndex] = data;
			}
		}

		public bool IsAnyAnimationPlaying()
		{
			return animationPlayingData.Count > 0;
		}

		public bool IsAnimationPlaying(BaseLayerAnimation animation)
		{
			if (animationPlayingData.Count > 0)
			{
				int index = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData data = animationPlayingData[index];
				return data.Anim == animation;
			}

			return false;
		}

		public bool IsCurrentAnimationBlendingOut()
		{
			if (animationPlayingData.Count > 0)
			{
				int index = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData data = animationPlayingData[index];
				return data.EndTime <= animationMixer.GetInput(index).GetTime();
			}

			return false;
		}

		public void IsAnimationBlendingOut(BaseLayerAnimation animation, out bool isAnimationPlaying, out bool isAnimationBlendingOut)
		{
			if (animationPlayingData.Count > 0)
			{
				int index = animationPlayingData.Count - 1;
				OverrideAnimationPlayingData data = animationPlayingData[index];

				isAnimationPlaying = data.Anim == animation;
				isAnimationBlendingOut = isAnimationPlaying ? data.EndTime <= animationMixer.GetInput(index).GetTime() : false;
			}
			else
			{
				isAnimationPlaying = false;
				isAnimationBlendingOut = false;
			}
		}

		public float GetWeight()
		{
			return animationSystem.GetLayerWeight(layerMixerInputIndex);
		}

		public float GetCurrentClipTime()
		{
			if (blendedAnimationsInfos.Count == 0)
			{
				return -1;
			}

			return (float)animationMixer.GetInput(blendedAnimationsInfos.Count - 1).GetTime();
		}
	}


	[System.Serializable]
	public class SecondaryLayerData
	{
		[SerializeField]
		public string name;
		[SerializeField]
		public bool IsAdditive;
		[SerializeField]
		public bool PassIK;
		[SerializeField]
		public AvatarMask Mask;

#if UNITY_EDITOR
		public bool m_IsFolded;
#endif
	}


	public struct OverrideAnimationPlayingData
	{
		public float SpeedMultiplayer;
		public float StartTime;
		public float StartBlendTime;
		public float EndTime;
		public float EndBlendTime;
		public BaseLayerAnimation Anim;
		public bool WasEndEventInvoked;
		public bool ChangeLayerWeight;

		public OverrideAnimationPlayingData(BaseLayerAnimation animation)
		{
			SpeedMultiplayer = animation.SpeedMultiplayer;
			StartTime = animation.StartTime;
			StartBlendTime = animation.StartBlendTime;
			EndTime = animation.EndTime;
			EndBlendTime = animation.EndBlendTime;
			Anim = animation;
			WasEndEventInvoked = false;
			ChangeLayerWeight = animation.ChangeLayerWeight;
		}

	}


}
