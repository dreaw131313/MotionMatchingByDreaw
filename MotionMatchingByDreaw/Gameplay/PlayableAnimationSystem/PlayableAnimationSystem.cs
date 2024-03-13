using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Gameplay
{
	public enum AnimationPlayType
	{
		PlayNormal,
		BeginNewBlendingGroup,
		PlayToCurrentBlendingGroup
	}

	public enum MecanimAnimatorControllerConnectionPlace
	{
		OnBegin,
		OnEnd
	}

	public class PlayableAnimationSystem
	{
		private Animator animator;
		public PlayableGraph Graph;
		private string name;
		public AnimationLayerMixerPlayable layerMixer;

		private bool isMecanimControllerAttached = false;

		internal AnimatorControllerPlayable mecanimPlayable;

		Playable playableToPlay;

		public float MecanimWeight
		{
			get
			{
				if (isMecanimControllerAttached)
				{
					return layerMixer.GetInputWeight(mecanimLayerIndex);
				}
				return 0;
			}
		}

		private int mecanimLayerIndex;

		public List<PlayableAnimationLayerData> layers { get; private set; }
		public List<PlayableSecondaryAnimationLayer> SecondaryLayers { get; private set; }
		public Dictionary<string, PlayableSecondaryAnimationLayer> SecondaryLayersDictionary { get; private set; }
		public bool IsMecanimControllerAttached { get => isMecanimControllerAttached; private set => isMecanimControllerAttached = value; }
		public ref Playable PlayableToPlay { get => ref playableToPlay; }

		public PlayableAnimationSystem(GameObject gameObject, string name)
		{
			isMecanimControllerAttached = false;
			animator = gameObject.GetComponent<Animator>();
			if (animator == null)
			{
				throw new System.Exception(string.Format("Game object {0} have no Animator Component!", gameObject.name));
			}
			this.name = name;
			layers = new List<PlayableAnimationLayerData>();
			SecondaryLayers = new List<PlayableSecondaryAnimationLayer>();
		}

		public void SetupGraph()
		{
			Graph = PlayableGraph.Create(name);
			Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			layerMixer = AnimationLayerMixerPlayable.Create(Graph, 0);
		}

		public void SetupWithDeliveredGraph(ref PlayableGraph graph)
		{
			Graph = graph;
			layerMixer = AnimationLayerMixerPlayable.Create(Graph, 0);
		}

		public void Initialize(
			List<SecondaryLayerData> secondaryLayersData,
			MotionMatchingComponent owner,
			//mecanim
			RuntimeAnimatorController mecanimController,
			AvatarMask avatarMask,
			bool isMecanimLayerAdditive,
			MecanimAnimatorControllerConnectionPlace mecanimPlace
			)
		{
			for (int i = 0; i < layers.Count; i++)
			{
				layers[i].Initialize(animator, ref layerMixer, ref Graph);
				layerMixer.SetInputWeight(i, 1f);
				if (layers[i].CurrentAvatarMask)
				{
					layerMixer.SetLayerMaskFromAvatarMask((uint)i, layers[i].CurrentAvatarMask);
					layerMixer.SetLayerMaskFromAvatarMask((uint)i, layers[i].CurrentAvatarMask);
				}
			}

			if (mecanimController != null && mecanimPlace == MecanimAnimatorControllerConnectionPlace.OnBegin)
			{
				ConnectAnimatorController(
					mecanimController,
					avatarMask,
					isMecanimLayerAdditive
					);
			}

			if (secondaryLayersData != null)
			{
				SecondaryLayersDictionary = new Dictionary<string, PlayableSecondaryAnimationLayer>();
				for (int i = 0; i < secondaryLayersData.Count; i++)
				{
					SecondaryLayers.Add(new PlayableSecondaryAnimationLayer(
						layerMixer.GetInputCount(),
						i,
						this.animator,
						secondaryLayersData[i],
						this,
						owner
						));
					SecondaryLayers[i].Initialize();
					SecondaryLayersDictionary.Add(secondaryLayersData[i].name, SecondaryLayers[i]);
				}
			}

			if (mecanimController != null && mecanimPlace == MecanimAnimatorControllerConnectionPlace.OnEnd)
			{
				ConnectAnimatorController(
					mecanimController,
					avatarMask,
					isMecanimLayerAdditive
					);
			}


			playableToPlay = layerMixer;
		}


		public void AttachCustomPlayables(List<CustomPlayablesSupport> customPlayables)
		{
			for (int i = 0; i < customPlayables.Count; i++)
			{
				if (customPlayables[i] != null)
				{
					Playable[] playables = customPlayables[i].CreateCustomPlayables(animator, ref Graph);

					if (playables != null)
					{
						for (int playableIndex = 0; playableIndex < playables.Length; playableIndex++)
						{
							Playable p = playables[i];
							if (!p.IsNull())
							{
								p.AddInput(playableToPlay, 0, 1f);
								playableToPlay = p;
							}
						}
					}
				}
			}
		}

		public void StartPlaying()
		{
			//AnimationPlayableUtilities.Play(animator, playableToPlay, Graph);



			AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(Graph, "AnimationClip", animator);
			playableOutput.SetSourcePlayable(playableToPlay, 0);
			Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			Graph.Play();

			//AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(Graph, "MotionMatchingOutput", animator);
			//playableOutput.SetSourcePlayable(playableToPlay, 0);

			////AnimationPlayableGraphExtensions.

			//Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			//Graph.Play();
		}

		public void Update(float deltaTime)
		{
			for (int i = 0; i < layers.Count; i++)
			{
				layers[i].Update(deltaTime);
			}
			for (int i = 0; i < SecondaryLayers.Count; i++)
			{
				SecondaryLayers[i].Update(deltaTime);
			}
		}

		public void LateUpdate()
		{
			for (int i = 0; i < SecondaryLayers.Count; i++)
			{
				SecondaryLayers[i].LateUpdate();
			}
		}

		public void Destory()
		{
			if (Graph.IsValid())
			{
				Graph.Destroy();
			}
		}

		// Layers:
		public void AddLayer(PlayableAnimationLayerData layerData)
		{
			layers.Add(layerData);
		}

		private void PlayMotionMatchingDataInfo(
			MotionMatchingDataInfo dataInfo,
			float time,
			float blendTime,
			float minWeightToAchive,
			bool playableIK,
			bool footIK,
			int blendingGroupID,
			int layerIndex
			)
		{
			//layers[layerIndex].PlayMotionMatchingDataInfo(
			//	dataInfo,
			//	ref Graph,
			//	blendTime,
			//	time,
			//	minWeightToAchive,
			//	blendingGroupID,
			//	playableIK,
			//	footIK
			//	);
		}

		public void StopAnimation(int layerIndex)
		{
			layers[layerIndex].StopAnimation();
		}

		public void SetLayerAvatarMask(uint layerIndex, AvatarMask mask)
		{
			if (layers[(int)layerIndex].CurrentAvatarMask == mask)
			{
				return;
			}

			layers[(int)layerIndex].CurrentAvatarMask = mask;
			layerMixer.SetLayerMaskFromAvatarMask(layerIndex, mask);
		}

		public void SetLayerWeight(int layerIndex, float weight)
		{
			layerMixer.SetInputWeight(layerIndex, weight);
		}

		public void SetLayerAdditive(uint layerIndex, bool isAdditive)
		{
			layerMixer.SetLayerAdditive(layerIndex, isAdditive);
		}

		public bool IsLayerAdditive(uint layerIndex)
		{
			return layerMixer.IsLayerAdditive(layerIndex);
		}

		private void ConnectAnimatorController(
			RuntimeAnimatorController mecanimController,
			AvatarMask avatarMask,
			bool isMecanimLayerAdditive
			)
		{
			isMecanimControllerAttached = true;
			mecanimPlayable = AnimatorControllerPlayable.Create(this.Graph, mecanimController);
			mecanimLayerIndex = layerMixer.GetInputCount();

			layerMixer.AddInput(mecanimPlayable, 0, 0f);

			layerMixer.SetLayerAdditive((uint)mecanimLayerIndex, isMecanimLayerAdditive);
			if (avatarMask != null)
			{
				layerMixer.SetLayerMaskFromAvatarMask((uint)mecanimLayerIndex, avatarMask);
			}
		}

		public void SetMecanimAvatarMask(AvatarMask avatarMask)
		{
			if (!isMecanimControllerAttached || avatarMask == null) { return; }

			layerMixer.SetLayerMaskFromAvatarMask((uint)mecanimLayerIndex, avatarMask);
		}

		public void SetAnimatorLayerAdditive(bool isAdditive)
		{
			if (!isMecanimControllerAttached) return;


			layerMixer.SetLayerAdditive((uint)mecanimLayerIndex, isAdditive);
		}

		public bool SetMecanimControllerWeight(float weight)
		{
			if (!isMecanimControllerAttached)
			{
				return false;
			}

			layerMixer.SetInputWeight(mecanimLayerIndex, Mathf.Clamp01(weight));

			return true;
		}

		public void PausePlayableGraph()
		{
			if (Graph.IsValid())
			{
				Graph.Stop();
			}
		}

		public void ResumePlayableGraph()
		{
			if (Graph.IsValid())
			{
				Graph.Play();
			}
		}

		public void SetSpeed(float speedMultiplier, int layerIndex = 0)
		{
			layers[layerIndex].StatesMixer.SetSpeed(speedMultiplier);
		}

		public float GetSpeedMultiplier(int layerIndex = 0)
		{
			return (float)layers[layerIndex].StatesMixer.GetSpeed();
		}


		public float GetLayerWeight(int index)
		{
			return layerMixer.GetInputWeight(index);
		}

		public void StopAllAnimationsInSecondaryLayers()
		{
			for (int i = 0; i < SecondaryLayers.Count; i++)
			{
				SecondaryLayers[i].StopPlayingAnimation();
			}
		}

	}


	public struct OverrideAnimationInfo
	{
		public bool IsSeted;
		public bool CanBeInterupted;
		public float StartTime;
		public float StartBlendTime;
		public float EndTime;
		public float EndBlendTime;


		public OverrideAnimationInfo(
			bool IsSeted,
			bool CanBeInterupted,
			float StartTime,
			float StartBlendTime,
			float EndTime,
			float EndBlendTime
			)
		{
			this.IsSeted = IsSeted;
			this.CanBeInterupted = CanBeInterupted;
			this.StartTime = StartTime;
			this.StartBlendTime = StartBlendTime;
			this.EndTime = EndTime;
			this.EndBlendTime = EndBlendTime;
		}

	}
}