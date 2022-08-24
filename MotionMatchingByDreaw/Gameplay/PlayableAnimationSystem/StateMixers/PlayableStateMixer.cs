using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public class PlayableStateMixer
	{
		PlayableGraph graph;

		public AnimationMixerPlayable stateMixer;
		private float lastAnimationBlendTime;

		private List<PlayableLayerBlendedAnimationInfo> blendedAnimationsInfos;

		public OnRemoveAnimationClipPlayable OnRemoveClipPlayable;
		public OnClearMixerInputs OnRemovingAllPlayables;

		public PlayableStateMixer(PlayableGraph graph)
		{
			this.graph = graph;
			blendedAnimationsInfos = new List<PlayableLayerBlendedAnimationInfo>();
		}

		public void OnStartPlay(ref AnimationMixerPlayable mixer)
		{
			stateMixer = mixer;
		}

		// when state weight is 0
		public void OnBlendEnd(ref AnimationMixerPlayable mixer)
		{
			for (int i = 0; i < mixer.GetInputCount(); i++)
			{
				mixer.GetInput(i).Destroy();
			}
		}

		public void PerformBlends(float deltaTime)
		{
			BlendAnimationsWithRemovingZeroWeightsClips(deltaTime);
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

			float lastBlendTIme = lastAnimationBlendTime == 0 ? 0.001f : lastAnimationBlendTime;

			for (int inputIndex = 0; inputIndex < lastIndex; inputIndex++)
			{
				PlayableLayerBlendedAnimationInfo currentInfo = blendedAnimationsInfos[inputIndex];
				if ((currentInfo.MinWeightToAchive <= currentInfo.CurrentWeight || lastInfo.StateID != currentInfo.StateID) &&
					currentInfo.BlendingSpeed > 0)
				{

					currentInfo.BlendingSpeed = -(currentInfo.CurrentWeight / lastBlendTIme);
				}

				currentInfo.CurrentWeight = Mathf.Clamp01(currentInfo.CurrentWeight + (currentInfo.BlendingSpeed * deltaTime));

				if (currentInfo.CurrentWeight == 0)
				{
					stateMixer.GetInput(inputIndex).Destroy();
					OnRemoveClipPlayable?.Invoke(inputIndex);

					int size = lastIndex + 1;
					for (int changedInputIndex = inputIndex + 1; changedInputIndex < size; changedInputIndex++)
					{
						Playable clip = stateMixer.GetInput(changedInputIndex);
						stateMixer.DisconnectInput(changedInputIndex);
						stateMixer.ConnectInput(changedInputIndex - 1, clip, 0);
						blendedAnimationsInfos[changedInputIndex - 1] = blendedAnimationsInfos[changedInputIndex];
					}

					blendedAnimationsInfos.RemoveAt(lastIndex);

					stateMixer.SetInputCount(lastIndex);
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
				stateMixer.SetInputWeight(i, blendedAnimationsInfos[i].CurrentWeight / weightSum);
			}
		}

		public float GetLastAnimationTime()
		{
			return (float)stateMixer.GetInput(stateMixer.GetInputCount() - 1).GetTime();
		}

		public float GetMixerInputTime(int inputIndex)
		{
			return (float)stateMixer.GetInput(inputIndex).GetTime();
		}

		public int GetInputCount()
		{
			return stateMixer.GetInputCount();
		}

		public float GetInputWeight(int inputIndex)
		{
			return stateMixer.GetInputWeight(inputIndex);
		}

		public float GetSpeedMultiplier()
		{
			return (float)stateMixer.GetSpeed();
		}

		public void PlayAnimation(
			AnimationClip animation,
			float blendTime,
			float time,
			float minWeightToAchive,
			int blendingGroupID,
			bool playableIK = false,
			bool footIK = false
			)
		{
			lastAnimationBlendTime = blendTime;

			float startAnimWeight;
			if (blendTime == 0 || stateMixer.GetInputCount() == 0)
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

				startAnimWeight = 1f;
			}
			else
			{
				blendedAnimationsInfos.Add(new PlayableLayerBlendedAnimationInfo(
					1f / blendTime,
					0f,
					minWeightToAchive,
					blendingGroupID)
					);

				startAnimWeight = 0f;
			}

			AnimationClipPlayable animationClip = AnimationClipPlayable.Create(graph, animation);
			animationClip.SetApplyFootIK(footIK);
			animationClip.SetApplyPlayableIK(playableIK);

			animationClip.SetTime(time);
			animationClip.SetTime(time);


			stateMixer.AddInput(
				animationClip,
				0,
				startAnimWeight
				);

			//int inputPort = animationMixer.GetInputCount() - 1;

			//animationMixer.GetInput(inputPort).SetTime(time);
			//animationMixer.GetInput(inputPort).SetTime(time);
			//animationMixer.GetInput(inputPort).SetTime(time);
		}

		public void PlayMotionMatchingDataInfo(
			MotionMatchingDataInfo dataInfo,
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
			float blendTime,
			float time,
			float minWeightToAchive,
			int blendingGroupID,
			bool playableIK = false,
			bool footIK = false
			)
		{
			lastAnimationBlendTime = blendTime;
			PlayableLayerBlendedAnimationInfo blendedAnimationInfo;

			if (blendTime == 0 || stateMixer.GetInputCount() == 0)
			{
				blendedAnimationsInfos.Clear();
				StopAnimation();

				OnRemovingAllPlayables?.Invoke();

				blendedAnimationInfo = new PlayableLayerBlendedAnimationInfo(
					float.MaxValue,
					1f,
					minWeightToAchive,
					blendingGroupID
					);
			}
			else
			{

				blendedAnimationInfo = new PlayableLayerBlendedAnimationInfo(
					1f / blendTime,
					0,
					minWeightToAchive,
					blendingGroupID
					);
			}

			AnimationMixerPlayable mixer = AnimationMixerPlayable.Create(
				graph,
				dataInfo.Clips.Count
				);

			for (int i = 0; i < dataInfo.Clips.Count; i++)
			{
				AnimationClipPlayable clip = AnimationClipPlayable.Create(graph, dataInfo.Clips[i]);

				mixer.AddInput(clip, 0, dataInfo.BlendTreeWeights[i]);

				clip.SetApplyFootIK(footIK);
				clip.SetApplyPlayableIK(playableIK);
				clip.SetTime(time);
				clip.SetTime(time);
			}


			blendedAnimationsInfos.Add(blendedAnimationInfo);
			stateMixer.AddInput(
				mixer,
				0,
				blendedAnimationInfo.CurrentWeight
				);
		}


		public void StopAnimation()
		{
			blendedAnimationsInfos.Clear();

			int inputCount = stateMixer.GetInputCount();

			if (inputCount > 0)
			{
				for (int i = 0; i < inputCount; i++)
				{
					stateMixer.GetInput(i).Destroy();
				}

				stateMixer.SetInputCount(0);
			}
		}


		public void SetSpeed(double speed)
		{
			stateMixer.SetSpeed(speed);
		}
	}
}