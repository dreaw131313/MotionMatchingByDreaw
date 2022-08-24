using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public delegate void OnRemoveAnimationClipPlayable(int removedMixerInput);
	public delegate void OnClearMixerInputs();


	public class PlayableAnimationLayerData
	{
		protected Animator animator;
		private AnimationMixerPlayable animationMixer; // only for getting not for setting
		private List<PlayableLayerBlendedAnimationInfo> blendedAnimationsInfos;

		private float lastAnimationBlendTime;

		public OnRemoveAnimationClipPlayable OnRemoveClipPlayable;
		public OnClearMixerInputs OnRemovingAllPlayables;

		private IEnumerator layerWeightChangeCoroutine;

		public AvatarMask CurrentAvatarMask = null;

		public AnimationMixerPlayable StatesMixer { get => animationMixer; }

		public PlayableAnimationLayerData(AvatarMask mask)
		{
			this.CurrentAvatarMask = mask;
			blendedAnimationsInfos = new List<PlayableLayerBlendedAnimationInfo>();
		}

		public void Initialize(Animator animator, ref AnimationLayerMixerPlayable layerMixer, ref PlayableGraph playableGraph)
		{
			this.animator = animator;
			blendedAnimationsInfos = new List<PlayableLayerBlendedAnimationInfo>();
			//blendingGroups = new List<BlendigGroupData>();

			animationMixer = AnimationMixerPlayable.Create(playableGraph, 0);

			layerMixer.AddInput(animationMixer, 0, 1f);
		}

		public void Update(float deltaTime)
		{
			BlendAnimationsWithRemovingZeroWeightsClips(deltaTime);
			//BlendAnimations_Old(deltaTime);
			//RemoveZeroWeightsInput();
		}

		private void PlayAnimation(
			AnimationClip animation,
			ref PlayableGraph playableGraph,
			float blendTime,
			float time,
			float minWeightToAchive,
			int blendingGroupID,
			bool playableIK = false,
			bool footIK = false
			)
		{
			lastAnimationBlendTime = blendTime;
			if (blendTime == 0 || animationMixer.GetInputCount() == 0)
			{
				blendedAnimationsInfos.Clear();
				StopAnimation();

				OnRemovingAllPlayables?.Invoke();

				blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
					float.MaxValue,
					1f,
					minWeightToAchive,
					blendingGroupID
					));
			}
			else
			{
				float startAnimationWeight = 0.0001f;

				blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
					1f / blendTime,
					startAnimationWeight,
					minWeightToAchive,
					blendingGroupID)
					);
			}

			AnimationClipPlayable animationClip = AnimationClipPlayable.Create(playableGraph, animation);
			animationClip.SetApplyFootIK(footIK);
			animationClip.SetApplyPlayableIK(playableIK);
			animationClip.SetTime(time);
			animationClip.SetTime(time);

			animationMixer.AddInput(
				animationClip,
				0,
				0.0001f
				);

			//int inputPort = animationMixer.GetInputCount() - 1;

			//animationMixer.GetInput(inputPort).SetTime(time);
			//animationMixer.GetInput(inputPort).SetTime(time);
			//animationMixer.GetInput(inputPort).SetTime(time);
		}

		private void PlayMotionMatchingDataInfo(
			MotionMatchingDataInfo dataInfo,
			ref PlayableGraph playableGraph,
			float blendTime,
			float time,
			float minWeightToAchive,
			int blendingGroupID,
			bool playableIK = false,
			bool footIK = false
			)
		{
			switch (dataInfo.DataType)
			{
				case AnimationDataType.SingleAnimation:
					{
						PlayAnimation(
							dataInfo.Clips[0],
							ref playableGraph,
							blendTime,
							time,
							minWeightToAchive,
							blendingGroupID,
							playableIK,
							footIK
							);
					}
					break;
				case AnimationDataType.BlendTree:
					{
						PlayeBlendTreeData(
							dataInfo,
							ref playableGraph,
							blendTime,
							time,
							minWeightToAchive,
							blendingGroupID,
							playableIK,
							footIK
						);
					}
					break;
			}
		}

		private void PlayeBlendTreeData(
			MotionMatchingDataInfo dataInfo,
			ref PlayableGraph playableGraph,
			float blendTime,
			float time,
			float minWeightToAchive,
			int blendingGroupID,
			bool playableIK = false,
			bool footIK = false
			)
		{
			lastAnimationBlendTime = blendTime;
			if (blendTime == 0 || animationMixer.GetInputCount() == 0)
			{
				blendedAnimationsInfos.Clear();
				StopAnimation();

				OnRemovingAllPlayables?.Invoke();

				blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
					float.MaxValue,
					1f,
					minWeightToAchive,
					blendingGroupID
					));
			}
			else
			{
				float startAnimationWeight = 0;

				blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
					1f / blendTime,
					startAnimationWeight,
					minWeightToAchive,
					blendingGroupID)
					);
			}

			AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(
				playableGraph,
				dataInfo.Clips.Count
				);

			for (int i = 0; i < dataInfo.Clips.Count; i++)
			{
				AnimationClipPlayable clip = AnimationClipPlayable.Create(playableGraph, dataInfo.Clips[i]);

				mixer.AddInput(clip, 0, dataInfo.BlendTreeWeights[i]);

				clip.SetApplyFootIK(footIK);
				clip.SetApplyPlayableIK(playableIK);
				clip.SetTime(time);
				clip.SetTime(time);
			}

			animationMixer.AddInput(
				mixer,
				0,
				0
				);
		}

		private int GetMixerInputCount()
		{
			return animationMixer.GetInputCount();
		}

		public void StopAnimation()
		{
			blendedAnimationsInfos.Clear();

			for (int i = 0; i < animationMixer.GetInputCount(); i++)
			{
				animationMixer.GetInput(i).Destroy();
			}

			animationMixer.SetInputCount(0);
		}

		private void BlendAnimations_Old(float deltaTime)
		{
			if (blendedAnimationsInfos.Count == 0)
			{
				return;
			}

			int lastIndex = blendedAnimationsInfos.Count - 1;
			float weightSum = 0f;
			for (int i = 0; i < lastIndex; i++)
			{
				PlayableLayerBlendedAnimationInfo info = blendedAnimationsInfos[i];
				if (info.MinWeightToAchive <= info.CurrentWeight &&
					info.BlendingSpeed > 0)
				{
					info.BlendingSpeed = -(info.CurrentWeight / lastAnimationBlendTime);
				}
				info.CurrentWeight = Mathf.Clamp01(info.CurrentWeight + (info.BlendingSpeed * deltaTime));
				weightSum += info.CurrentWeight;
				blendedAnimationsInfos[i] = info;
			}

			PlayableLayerBlendedAnimationInfo lastInfo = blendedAnimationsInfos[lastIndex];
			if (lastInfo.CurrentWeight < 1f)
			{
				lastInfo.CurrentWeight = Mathf.Clamp01(lastInfo.CurrentWeight + lastInfo.BlendingSpeed * deltaTime);
				weightSum += lastInfo.CurrentWeight;
				blendedAnimationsInfos[lastIndex] = lastInfo;
			}
			else
			{
				weightSum += 1f;
			}

			for (int i = 0; i < blendedAnimationsInfos.Count; i++)
			{
				animationMixer.SetInputWeight(i, blendedAnimationsInfos[i].CurrentWeight / weightSum);
			}
		}

		private void RemoveZeroWeightsInput()
		{
			int size = animationMixer.GetInputCount();

			for (int currentInputIndex = 0; currentInputIndex < size; currentInputIndex++)
			{
				if (animationMixer.GetInputWeight(currentInputIndex) <= 0 && currentInputIndex < (size - 1))
				{
					animationMixer.GetInput(currentInputIndex).Destroy();
					blendedAnimationsInfos.RemoveAt(currentInputIndex);

					OnRemoveClipPlayable?.Invoke(currentInputIndex);

					for (int changedInputIndex = currentInputIndex + 1; changedInputIndex < size; changedInputIndex++)
					{
						// double localTime = ((AnimationClipPlayable)mixer.GetInput(j)).GetTime();
						float _weight = animationMixer.GetInputWeight(changedInputIndex);
						Playable clip = animationMixer.GetInput(changedInputIndex);
						// clip.SetTime(localTime);
						animationMixer.DisconnectInput(changedInputIndex);
						animationMixer.ConnectInput(changedInputIndex - 1, clip, 0);
						animationMixer.SetInputWeight(changedInputIndex - 1, _weight);
					}
					currentInputIndex--;
					size--;
					animationMixer.SetInputCount(size);
				}
			}
		}

		private void BlendAnimationsWithRemovingZeroWeightsClips(float deltaTime)
		{
			if (blendedAnimationsInfos.Count == 0)
			{
				return;
			}

			int lastIndex = blendedAnimationsInfos.Count - 1;
			PlayableLayerBlendedAnimationInfo lastInfo = blendedAnimationsInfos[lastIndex];
			float weightSum = 0f;

			for (int inputIndex = 0; inputIndex < lastIndex; inputIndex++)
			{
				PlayableLayerBlendedAnimationInfo currentInfo = blendedAnimationsInfos[inputIndex];
				if ((currentInfo.MinWeightToAchive <= currentInfo.CurrentWeight || lastInfo.StateID != currentInfo.StateID) &&
					currentInfo.BlendingSpeed > 0)
				{
					currentInfo.BlendingSpeed = -(currentInfo.CurrentWeight / lastAnimationBlendTime);
				}

				currentInfo.CurrentWeight = Mathf.Clamp01(currentInfo.CurrentWeight + (currentInfo.BlendingSpeed * deltaTime));

				if (currentInfo.CurrentWeight == 0)
				{
					animationMixer.GetInput(inputIndex).Destroy();
					OnRemoveClipPlayable?.Invoke(inputIndex);

					int size = lastIndex + 1;
					for (int changedInputIndex = inputIndex + 1; changedInputIndex < size; changedInputIndex++)
					{
						Playable clip = animationMixer.GetInput(changedInputIndex);
						animationMixer.DisconnectInput(changedInputIndex);
						animationMixer.ConnectInput(changedInputIndex - 1, clip, 0);
						blendedAnimationsInfos[changedInputIndex - 1] = blendedAnimationsInfos[changedInputIndex];
					}

					blendedAnimationsInfos.RemoveAt(lastIndex);

					animationMixer.SetInputCount(lastIndex);
					inputIndex--;
					lastIndex--;
				}
				else
				{
					weightSum += currentInfo.CurrentWeight;
					blendedAnimationsInfos[inputIndex] = currentInfo;
				}
			}

			if (lastInfo.CurrentWeight < 1f)
			{
				lastInfo.CurrentWeight = Mathf.Clamp01(lastInfo.CurrentWeight + lastInfo.BlendingSpeed * deltaTime);
				weightSum += lastInfo.CurrentWeight;
				blendedAnimationsInfos[lastIndex] = lastInfo;
			}
			else
			{
				weightSum += 1f;
			}

			for (int i = 0; i < blendedAnimationsInfos.Count; i++)
			{
				animationMixer.SetInputWeight(i, blendedAnimationsInfos[i].CurrentWeight / weightSum);
			}
		}

		private float GetMixerInputTime(int index)
		{
			return (float)animationMixer.GetInput(index).GetTime();
		}

		private float GetMixerInputWeight(int index)
		{
			return animationMixer.GetInputWeight(index);
		}


		public void SetLayerWeight(
			MonoBehaviour owner,
			float weight,
			float changeTime,
			int layerIndex,
			PlayableAnimationSystem animationSystem
			)
		{
			if (layerWeightChangeCoroutine != null)
			{
				owner.StopCoroutine(layerWeightChangeCoroutine);
			}

			if (changeTime == 0)
			{
				animationSystem.layerMixer.SetInputWeight(layerIndex, weight);
				return;
			}
			layerWeightChangeCoroutine = ChangeWeightCoroutine(weight, changeTime, layerIndex, animationSystem);

			owner.StartCoroutine(layerWeightChangeCoroutine);
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

		public AnimationMixerPlayable PlayStateMixer(
			float blendTime,
			PlayableGraph graph,
			int stateID
			)
		{
			AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(graph, 0);

			PlayableLayerBlendedAnimationInfo animationInfo;
			lastAnimationBlendTime = blendTime;
			if (blendTime == 0 || animationMixer.GetInputCount() == 0)
			{
				blendedAnimationsInfos.Clear();
				StopAnimation();

				OnRemovingAllPlayables?.Invoke();

				animationInfo = new PlayableLayerBlendedAnimationInfo(
					float.MaxValue,
					1f,
					0f,
					stateID
					);
			}
			else
			{
				animationInfo = new PlayableLayerBlendedAnimationInfo(
					1f / blendTime,
					0f,
					0f,
					stateID
					);
			}

			blendedAnimationsInfos.Add(animationInfo);
			animationMixer.AddInput(
				mixer,
				0,
				animationInfo.CurrentWeight
				);

			return mixer;
		}
	}

	public struct PlayableLayerBlendedAnimationInfo
	{
		public float BlendingSpeed;
		public float CurrentWeight;
		public float MinWeightToAchive;
		public int StateID;

		public PlayableLayerBlendedAnimationInfo(
			float blendingSpeed,
			float currentWeight,
			float minWeightToAchive,
			int stateID
			)
		{
			BlendingSpeed = blendingSpeed;
			CurrentWeight = currentWeight;
			MinWeightToAchive = minWeightToAchive;
			StateID = stateID;
		}
	}
}
