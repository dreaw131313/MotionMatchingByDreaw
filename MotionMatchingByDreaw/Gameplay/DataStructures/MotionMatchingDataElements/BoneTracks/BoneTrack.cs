using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class BoneTrack
	{
		public List<BoneTrackInterval> Intervals;
		public bool HaveLastInterval = false;
		public BoneTrackInterval LastInterval;
		public float SamplingTime;

#if UNITY_EDITOR
		[SerializeField]
		public bool DrawGizmos;
		[SerializeField]
		public BoneTrackSettings TrackSettings;

		public BoneTrack(BoneTrackSettings settings)
		{
			SamplingTime = settings.SamplingTime;
			TrackSettings = settings;
		}

		public BoneTrack(BoneTrack other)
		{
			this.Intervals = new List<BoneTrackInterval>(other.Intervals);
		}

#endif

		public BoneTrackAccesData GetTrackAccesData(float animationLocalTime)
		{
			for (int i = 0; i < Intervals.Count; i++)
			{
				BoneTrackInterval trackInterval = Intervals[i];

				if (trackInterval.TimeInterval.y >= animationLocalTime && trackInterval.Data != null && trackInterval.Data.Length > 0)
				{
					float deltaTime = animationLocalTime - trackInterval.StarTime;
					int sampleIndex = Mathf.FloorToInt(deltaTime / SamplingTime);
					float modulo = deltaTime % SamplingTime;
					if (modulo > 0)
					{
						if (sampleIndex < trackInterval.Data.Length - 1)
						{
							float3 pos1 = trackInterval.Data[sampleIndex].Postion;
							float3 pos2 = trackInterval.Data[sampleIndex + 1].Postion;

							return new BoneTrackAccesData(
								trackInterval.TimeInterval.x,
								trackInterval.TimeInterval.y,
								new BoneTrackData(
									math.lerp(pos1, pos2, modulo / SamplingTime)
									),
								true
								);
						}
						else if (trackInterval.UseLastData)
						{
							float3 pos1 = trackInterval.Data[sampleIndex].Postion;
							float3 pos2 = trackInterval.LastData.Postion;

							return new BoneTrackAccesData(
								trackInterval.TimeInterval.x,
								trackInterval.TimeInterval.y,
								new BoneTrackData(
									math.lerp(pos1, pos2, modulo / SamplingTime)
									),
								true
								);
						}
					}
					else
					{
						return new BoneTrackAccesData(
								trackInterval.TimeInterval.x,
								trackInterval.TimeInterval.y,
								new BoneTrackData(
									trackInterval.Data[sampleIndex].Postion
									),
								true
								);
					}
				}
			}

			if (HaveLastInterval)
			{
				if (LastInterval.TimeInterval.y >= animationLocalTime)
				{
					float deltaTime = animationLocalTime - LastInterval.StarTime;
					int sampleIndex = Mathf.FloorToInt(deltaTime / SamplingTime);
					float modulo = deltaTime % SamplingTime;

					if (modulo > 0)
					{
						if (sampleIndex < LastInterval.Data.Length - 2)
						{
							float3 pos1 = LastInterval.Data[sampleIndex].Postion;
							float3 pos2 = LastInterval.Data[sampleIndex + 1].Postion;

							return new BoneTrackAccesData(
								LastInterval.TimeInterval.x,
								LastInterval.TimeInterval.y,
								new BoneTrackData(
									math.lerp(pos1, pos2, modulo / SamplingTime)
									),
								true
								);
						}
						else if (LastInterval.UseLastData)
						{
							float3 pos1 = LastInterval.Data[sampleIndex].Postion;
							float3 pos2 = LastInterval.LastData.Postion;

							return new BoneTrackAccesData(
								LastInterval.TimeInterval.x,
								LastInterval.TimeInterval.y,
								new BoneTrackData(
									math.lerp(pos1, pos2, modulo / SamplingTime)
									),
								true
								);
						}
					}
					else
					{
						return new BoneTrackAccesData(
								LastInterval.TimeInterval.x,
								LastInterval.TimeInterval.y,
								new BoneTrackData(
									LastInterval.Data[sampleIndex].Postion
									),
								true
								);
					}
				}
			}

			return BoneTrackAccesData.CreatInvalidData();
		}

	}


	[System.Serializable]
	public struct BoneTrackData
	{
		[SerializeField]
		public float3 Postion;

		public BoneTrackData(float3 position)
		{
			Postion = position;
		}
	}


	[System.Serializable]
	public class BoneTrackInterval
	{
		public float StarTime;
		public float2 TimeInterval;
		public BoneTrackData[] Data;
		public bool UseLastData = false;
		public BoneTrackData LastData;

		public BoneTrackInterval(BoneTrackInterval other)
		{
			StarTime = other.StarTime;
			TimeInterval = other.TimeInterval;

			if (other.Data != null && other.Data.Length > 0)
			{
				Data = new BoneTrackData[other.Data.Length];
				for (int i = 0; i < Data.Length; i++)
				{
					Data[i] = other.Data[i];
				}
			}
			else
			{
				Data = null;
			}

			UseLastData = other.UseLastData;
			LastData = other.LastData;
		}

		public BoneTrackInterval(float startTime, float2 timeInterval)
		{
			StarTime = startTime;
			TimeInterval = timeInterval;
		}

	}

	public struct BoneTrackAccesData
	{
		public float Start;
		public float End;
		public BoneTrackData Data;
		private bool isValid;


		public BoneTrackAccesData(float start, float end, BoneTrackData data, bool isValid)
		{
			Start = start;
			End = end;
			Data = data;
			this.isValid = isValid;
		}

		public static BoneTrackAccesData CreatInvalidData()
		{
			return new BoneTrackAccesData(-1, -1, new BoneTrackData(), false);
		}

		public bool IsValid { get => isValid; private set => isValid = value; }
	}

	public struct BoneTrackAcces
	{
		private BoneTrack boneTrack;
		private BoneTrackInterval currentInterval;

		int currentIntervalIndex;
		bool isInLastInterval;

		public BoneTrackAcces(BoneTrack boneTrack) : this()
		{
			this.boneTrack = boneTrack;

			IsValid = boneTrack != null;
		}

		public bool IsValid { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="animationLocalTime"></param>
		public void ResetToTime(float animationLocalTime)
		{
			currentInterval = null;
			for (int i = 0; i < boneTrack.Intervals.Count; i++)
			{
				if (boneTrack.Intervals[i].TimeInterval.y > animationLocalTime)
				{
					currentIntervalIndex = i;
					currentInterval = boneTrack.Intervals[i];
					break;
				}
			}

			if (currentInterval == null)
			{
				if (boneTrack.HaveLastInterval && boneTrack.LastInterval.TimeInterval.y > animationLocalTime)
				{
					currentInterval = boneTrack.LastInterval;
					currentIntervalIndex = -1;
					isInLastInterval = true;
				}
				else
				{
					currentIntervalIndex %= boneTrack.Intervals.Count;
					currentInterval = boneTrack.Intervals[currentIntervalIndex];
					isInLastInterval = false;
				}
			}
		}

		public void Update(float animationLocalTime)
		{
			if (animationLocalTime < currentInterval.StarTime)
			{
				ResetToTime(animationLocalTime);
			}
			else
			{
				while (currentInterval.TimeInterval.y < animationLocalTime)
				{
					currentIntervalIndex = currentIntervalIndex + 1;
					if (currentIntervalIndex >= boneTrack.Intervals.Count)
					{
						if (boneTrack.HaveLastInterval && boneTrack.LastInterval.TimeInterval.y > animationLocalTime)
						{
							currentInterval = boneTrack.LastInterval;
							currentIntervalIndex = -1;
							isInLastInterval = true;
						}
						else
						{
							currentIntervalIndex %= boneTrack.Intervals.Count;
							currentInterval = boneTrack.Intervals[currentIntervalIndex];
							isInLastInterval = false;
						}
						break;
					}
					else
					{
						currentInterval = boneTrack.Intervals[currentIntervalIndex];
					}

				}
			}
		}

		public BoneTrackAccesData GetTrackAccesData(float animationLocalTime)
		{
			Update(animationLocalTime);

			if (currentInterval == null) return BoneTrackAccesData.CreatInvalidData();

			float samplingTime = boneTrack.SamplingTime;
			float deltaTime = animationLocalTime - currentInterval.StarTime;
			int sampleIndex = Mathf.FloorToInt(deltaTime / samplingTime);

			if (sampleIndex >= currentInterval.Data.Length || deltaTime < 0) return BoneTrackAccesData.CreatInvalidData();

			float modulo = deltaTime % samplingTime;

			if (modulo > 0)
			{
				if (sampleIndex < currentInterval.Data.Length - 2)
				{
					float3 pos1 = currentInterval.Data[sampleIndex].Postion;
					float3 pos2 = currentInterval.Data[sampleIndex + 1].Postion;

					return new BoneTrackAccesData(
						currentInterval.TimeInterval.x,
						currentInterval.TimeInterval.y,
						new BoneTrackData(
							math.lerp(pos1, pos2, modulo / samplingTime)
							),
						true
						);
				}
				else if (currentInterval.UseLastData)
				{
					float3 pos1 = currentInterval.Data[sampleIndex].Postion;
					float3 pos2 = currentInterval.LastData.Postion;

					return new BoneTrackAccesData(
						currentInterval.TimeInterval.x,
						currentInterval.TimeInterval.y,
						new BoneTrackData(
							math.lerp(pos1, pos2, modulo / samplingTime)
							),
						true
						);
				}
			}

			return new BoneTrackAccesData(
					currentInterval.TimeInterval.x,
					currentInterval.TimeInterval.y,
					new BoneTrackData(
						currentInterval.Data[sampleIndex].Postion
						),
						true
					);
		}
	}


	[System.Serializable]
	public class BoneTrackDataInterval
	{
		public BoneTrackData[] Data;

	}

}