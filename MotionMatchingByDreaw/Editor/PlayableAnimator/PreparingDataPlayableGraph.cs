using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MotionMatching.Tools
{
	public class PreparingDataPlayableGraph
	{
		public GameObject go;

		public PlayableGraph graph;
		public AnimationMixerPlayable mixer;
		private AnimationPlayableOutput animationOutput;

		MotionMatchingData currentMMData;

		public PreparingDataPlayableGraph()
		{

		}

		public bool IsValid()
		{
			if (!graph.IsValid() || mixer.IsNull() || animationOutput.IsOutputNull())
			{
				return false;
			}
			return true;
		}

		public void Initialize(GameObject go)
		{
			this.go = go;
			graph = PlayableGraph.Create("Preapare Data Graph");
			mixer = AnimationMixerPlayable.Create(graph, 0, false);
			animationOutput = AnimationPlayableOutput.Create(graph, "Animation Output", go.GetComponent<Animator>());
			animationOutput.SetSourcePlayable(mixer);
			graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
		}

		public void Destroy()
		{
			if (graph.IsValid())
			{
				graph.Destroy();
			}
		}

		public void Evaluate(float deltaTime)
		{
			if (this.IsValid())
			{
				graph.Evaluate(deltaTime);
			}
		}

		public void AddClipPlayable(AnimationClip animation)
		{
			AnimationClipPlayable playable = AnimationClipPlayable.Create(this.graph, animation);
			mixer.AddInput(playable, 0, 0f);
		}

		public void AddClipPlayable(AnimationClip animation, float time, float weight)
		{
			AnimationClipPlayable playable = AnimationClipPlayable.Create(this.graph, animation);
			mixer.AddInput(playable, 0, weight);
			playable.SetTime(time);
			playable.SetTime(time);
		}

		public void ClearMainMixerInput()
		{
			for (int i = 0; i < mixer.GetInputCount(); i++)
			{
				mixer.GetInput(i).Destroy();
			}
			mixer.SetInputCount(0);
		}

		public void SetMixerInputTime(int mixerInput, float time)
		{
			mixer.GetInput(mixerInput).SetTime(time);
		}

		public void SetMixerInputTimeInPlace(int mixerInput, float time)
		{
			mixer.GetInput(mixerInput).SetTime(time);
			mixer.GetInput(mixerInput).SetTime(time);
		}

		public void SetMixerInputWeight(int mixerInput, float weight)
		{
			mixer.SetInputWeight(mixerInput, weight);
		}

		public void RemoveZeroWeightsInput()
		{
			int size = mixer.GetInputCount();
			for (int i = 0; i < size; i++)
			{
				if (mixer.GetInputWeight(i) <= 0)
				{
					mixer.GetInput(i).Destroy();
					for (int j = i + 1; j < size; j++)
					{
						// double localTime = ((AnimationClipPlayable)mixer.GetInput(j)).GetTime();
						float _weight = mixer.GetInputWeight(j);
						Playable clip = mixer.GetInput(j);
						// clip.SetTime(localTime);
						mixer.DisconnectInput(j);
						mixer.ConnectInput(j - 1, clip, 0);
						mixer.SetInputWeight(j - 1, _weight);
					}
					i--;
					size--;
					mixer.SetInputCount(size);
				}
			}
		}

		public int GetMixerInputCount()
		{
			return mixer.GetInputCount();
		}

		public float GetMixerInputTime(int inputIndex)
		{
			return (float)mixer.GetInput(inputIndex).GetTime();
		}

		public float GetMixerInputWeight(int inputIndex)
		{
			return mixer.GetInputWeight(inputIndex);
		}

		public bool CreateAnimationDataPlayables(MotionMatchingData data, float time = 0f)
		{
			currentMMData = data;
			if (this.IsValid())
			{
				this.ClearMainMixerInput();
				switch (data.dataType)
				{
					case AnimationDataType.SingleAnimation:
						AddClipPlayable(data.clips[0], time, 1f);
						break;
					case AnimationDataType.BlendTree:
						for (int i = 0; i < data.blendTreeWeights.Length; i++)
						{
							AddClipPlayable(data.clips[i], time, data.blendTreeWeights[i]);
						}
						break;
				}

				this.Evaluate(0);
				return true;
			}

			return false;
		}

		public void EvaluateMotionMatchgData(MotionMatchingData data, float deltaTime, int sequenceUpdateLoops = 4)
		{
			if (this.IsValid())
			{
				switch (data.dataType)
				{
					case AnimationDataType.SingleAnimation:
						this.Evaluate(deltaTime);
						break;
					case AnimationDataType.BlendTree:
						this.Evaluate(deltaTime);
						break;
				}
			}
		}

		public bool IsDataValid(MotionMatchingData data)
		{
			return currentMMData == data;
		}
	}
}
