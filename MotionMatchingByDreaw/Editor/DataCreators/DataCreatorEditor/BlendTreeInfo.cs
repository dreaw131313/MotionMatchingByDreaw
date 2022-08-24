using System.Collections.Generic;
using UnityEngine;
using System;

namespace MotionMatching.Tools
{
    [System.Serializable]
    public class BlendTreeInfo
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public List<AnimationClip> clips = new List<AnimationClip>();
        [SerializeField]
        public float preview = 0f;
        [SerializeField]
        public bool fold;
        [SerializeField]
        public List<float> clipsWeights;
        [SerializeField]
        public bool findInYourself = true;
        [SerializeField]
        public bool blendToYourself = false;
        [SerializeField]
        public int spaces = 0;
        [SerializeField]
        public bool useSpaces = false;


        public BlendTreeInfo(string name)
        {
            this.name = name;
            clipsWeights = new List<float>();
        }

        public void AddClip(AnimationClip clip)
        {
            clips.Add(clip);
            clipsWeights.Add(1f);
            //foreach (ClipsWeights w in weights)
            //{
            //    w.weights.Add(1f);
            //}
        }

        public void RemoveClip(int index)
        {
            clips.RemoveAt(index);
            clipsWeights.RemoveAt(index);
            //foreach (ClipsWeights w in weights)
            //{
            //    w.weights.RemoveAt(index);
            //}
        }

        public void ClearClips()
        {
            clips.Clear();
            clipsWeights.Clear();
        }

        public float GetMaxClipTime()
        {
            float result = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null)
                {
                    return 0;
                }
                if (result < clips[i].length)
                {
                    result = clips[i].length;
                }
            }
            return result;
        }

        public float[] GetNormalizedWeights()
        {
            float[] normWs = new float[clipsWeights.Count];

            float weightSum = 0f;
            for (int i = 0; i < clipsWeights.Count; i++)
            {
                weightSum += clipsWeights[i];
            }

            for (int i = 0; i < clipsWeights.Count; i++)
            {
                normWs[i] = clipsWeights[i] / weightSum;
            }

            return normWs;
        }

        public bool IsValid()
        {
            if (clips.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null)
                {
                    return false;
                }
            }
            return true;
        }

        public void CreateGraphFor(
            GameObject go,
            PreparingDataPlayableGraph graph
            )
        {
            if (go == null)
            {
                Debug.LogWarning("Game object for animator is null!");
                return;
            }

            if (!IsValid())
            {
                Debug.LogWarning("Some Blend Tree animations clips are null!");
                return;
            }

            if (graph == null)
            {
                graph = new PreparingDataPlayableGraph();
            }

            if (!graph.IsValid())
            {
                graph.Initialize(go);
            }

            graph.ClearMainMixerInput();

            float[] normWeights = GetNormalizedWeights();

            for (int i = 0; i < clips.Count; i++)
            {
                graph.AddClipPlayable(clips[i]);
                graph.SetMixerInputTimeInPlace(i, 0f);
                graph.SetMixerInputWeight(i, normWeights[i]);
            }
        }

        public float GetLength()
        {
            return clips[0].length;
        }

    }

    [System.Serializable]
    public class ClipsWeights
    {
        [SerializeField]
        public List<float> weights = new List<float>();
        [SerializeField]
        public string name;
        [SerializeField]
        public bool fold = false;

        public ClipsWeights(string name, int weightsCount = 0)
        {
            this.name = name;
            for (int i = 0; i < weightsCount; i++)
            {
                weights.Add(1f);
            }
        }

    }
}
