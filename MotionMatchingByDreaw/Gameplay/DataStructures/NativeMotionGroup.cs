using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "NativeMotionGroup", menuName = "Motion Matching/Data/Native Motion Group")]
	public class NativeMotionGroup : ScriptableObject
	{
		#region motion matching settings asset
		[SerializeField]
		private MotionMatchingSystemSettings systemSettings;
		#endregion

		[SerializeField]
		public List<MotionMatchingDataInfo> MotionDataInfos;
		[SerializeField]
		public SectionsDependencies SectionsDependencies;
		[SerializeField]
		public int PoseBonesCount;
		[SerializeField]
		public float[] TrajectoryTimes;
		[SerializeField]
		[Min(0.01f)]
		public float TrajectoryCostWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float PoseCostWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float ContactsCostWeight = 1f;
#if UNITY_EDITOR
		[SerializeField]
		public List<BoneWeightInfo> BonesWeights;
		[SerializeField]
		public bool NormalizeWeights = true;
#endif
		[SerializeField]
		public List<float2> NormalizedBonesWeights; // x - position weight y - velocity weight

		[SerializeField]
		public int ContactPointsCount;
		[SerializeField]
		private string binaryAssetName = string.Empty;
		[SerializeField]
		private bool isBinaryDataLoaded = false;

		#region Runtime
		public NativeArray<FrameDataInfo> Frames;
		public NativeArray<TrajectoryPoint> TrajectoryPoints;
		public NativeArray<BoneData> Bones;
		public NativeArray<FrameContact> Contacts;
		public NativeArray<float2> NativeNormalizedBonesWeights;

		[System.NonSerialized]
		public int JobsCount;
		[System.NonSerialized]
		public int FramesPerThread;

		public Dictionary<string, int> SectionIndexes;
		#endregion

		#region Saving and loading stuff
		//public const string SaveSuffix = "NMGB"; // N - Native M - Motion G - Group B - Binary
		public const string FileExtension = "nmgb"; // n - Native m - Motion g - Group b - Binary
		public const string FileName = "NativeMotionGroups";
		#endregion

		#region Trajectory weights:
		[SerializeField]
		[Min(0.01f)]
		public float TrajectoryPositionWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float TrajectoryVelocityWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float TrajectoryOrientationWeight = 1f;

		#endregion


		#region Bone tracks
		[SerializeField]
		public BoneTracksDescription TracksDescription;

		#endregion

		// Subscribing by MotionMatchingComponent:
		public int SingleTrajectoryPointsCount { get => TrajectoryTimes.Length; }
		public bool IsInitialized { get { return Frames.IsCreated; } }

		public bool IsBinaryDataLoaded { get => isBinaryDataLoaded; set => isBinaryDataLoaded = value; }

		public NativeMotionGroup()
		{
#if UNITY_EDITOR
			BonesWeights = new List<BoneWeightInfo>();
#endif

			MotionDataInfos = new List<MotionMatchingDataInfo>();
			TrajectoryTimes = new float[0];
			NormalizedBonesWeights = new List<float2>();
		}

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (systemSettings == null)
			{
				systemSettings = MotionMatchingSystemSettings.GetOrCreateSettings();
			}

			if (!EditorApplication.isPlayingOrWillChangePlaymode) return;

			m_Subscribers = new HashSet<MotionMatchingComponent>();
#endif
			LoadNativeData();
		}

		private void OnDisable()
		{
			DisposeNativeData();
		}


		/// <summary>
		/// Runtime only.
		/// </summary>
		/// <param name="trajectory"></param>
		/// <param name="inTime"></param>
		/// <param name="clipIndex"></param>
		public void GetTrajectoryInTime(ref NativeArray<TrajectoryPoint> trajectory, float inTime, int clipIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startPointIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + lastFrameIndex * SingleTrajectoryPointsCount;
				for (int i = 0; i < TrajectoryTimes.Length; i++)
				{
					trajectory[i] = TrajectoryPoints[startPointIndex + i];
				}
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + localFrameIndex * SingleTrajectoryPointsCount;
				int secondTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + nextFrameIndex * SingleTrajectoryPointsCount;

				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < TrajectoryTimes.Length; i++)
				{
					trajectory[i] = TrajectoryPoint.Lerp(
						TrajectoryPoints[firstTrajectoryStartIndex + i],
						TrajectoryPoints[secondTrajectoryStartIndex + i],
						factor
						);
				}
			}
		}

		public void GetTrajectoryInTimeWithTrajectoryCostCorrection(ref NativeArray<TrajectoryPoint> trajectory, float inTime, int clipIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startPointIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + lastFrameIndex * SingleTrajectoryPointsCount;
				for (int i = 0; i < TrajectoryTimes.Length; i++)
				{
					trajectory[i] = TrajectoryPoints[startPointIndex + i];
				}
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + localFrameIndex * SingleTrajectoryPointsCount;
				int secondTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + nextFrameIndex * SingleTrajectoryPointsCount;

				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < TrajectoryTimes.Length; i++)
				{
					TrajectoryPoint tp = TrajectoryPoint.Lerp(
						TrajectoryPoints[firstTrajectoryStartIndex + i],
						TrajectoryPoints[secondTrajectoryStartIndex + i],
						factor
						);

					tp.Position /= TrajectoryPositionWeight;
					tp.Velocity /= TrajectoryVelocityWeight;
					tp.Orientation /= TrajectoryOrientationWeight;

					trajectory[i] = tp / TrajectoryCostWeight;
				}
			}
		}

		public void UpdateTrajectoryFromData(ref NativeArray<TrajectoryPoint> trajectory, int trajectoryPointCount, float inTime, int clipIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startPointIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + lastFrameIndex * SingleTrajectoryPointsCount;
				for (int i = 0; i < trajectoryPointCount; i++)
				{
					trajectory[i] = TrajectoryPoints[startPointIndex + i];
				}
			}
			else
			{
				int firstTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + localFrameIndex * SingleTrajectoryPointsCount;
				//int secondTrajectoryStartIndex = firstTrajectoryStartIndex + SingleTrajectoryPointsCount;

				//int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				//float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < trajectoryPointCount; i++)
				{
					trajectory[i] = TrajectoryPoints[firstTrajectoryStartIndex + i];
					//trajectory[i] = TrajectoryPoint.Lerp(
					//	TrajectoryPoints[firstTrajectoryStartIndex + i],
					//	TrajectoryPoints[secondTrajectoryStartIndex + i],
					//	factor
					//	);
				}
			}
		}

		public void UpdateTrajectoryFromDataWithTrajectoryCostCorrection(ref NativeArray<TrajectoryPoint> trajectory, int trajectoryPointCount, float inTime, int clipIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startPointIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + lastFrameIndex * SingleTrajectoryPointsCount;
				for (int i = 0; i < trajectoryPointCount; i++)
				{
					trajectory[i] = TrajectoryPoints[startPointIndex + i];
				}
			}
			else
			{
				int firstTrajectoryStartIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + localFrameIndex * SingleTrajectoryPointsCount;
				//int secondTrajectoryStartIndex = firstTrajectoryStartIndex + SingleTrajectoryPointsCount;

				//int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				//float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < trajectoryPointCount; i++)
				{
					TrajectoryPoint tp = TrajectoryPoints[firstTrajectoryStartIndex + i];
					tp.Position /= TrajectoryPositionWeight;
					tp.Velocity /= TrajectoryVelocityWeight;
					tp.Orientation /= TrajectoryOrientationWeight;
					trajectory[i] = tp / TrajectoryCostWeight;

					//trajectory[i] = TrajectoryPoint.Lerp(
					//	TrajectoryPoints[firstTrajectoryStartIndex + i],
					//	TrajectoryPoints[secondTrajectoryStartIndex + i],
					//	factor
					//	);
				}
			}
		}

		public Vector3 GetTrajectoryPointPosition(int clipIndex, float inTime, int trajectoryIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int pointIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + lastFrameIndex * SingleTrajectoryPointsCount + trajectoryIndex;
				return TrajectoryPoints[pointIndex].Position;
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstTrajectoryIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + localFrameIndex * SingleTrajectoryPointsCount + trajectoryIndex;
				int secondTrajectoryIndex = MotionDataInfos[clipIndex].StartTrajectoryPointIndex + nextFrameIndex * SingleTrajectoryPointsCount + trajectoryIndex;
				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;
				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				return TrajectoryPoint.Lerp(
					TrajectoryPoints[firstTrajectoryIndex],
					TrajectoryPoints[secondTrajectoryIndex],
					factor
					).Position / TrajectoryCostWeight;
			}
		}
		/// <summary>
		/// Runtime only. Can only be used by MotionMatching component.
		/// </summary>
		/// <param name="pose"></param>
		/// <param name="inTime"></param>
		/// <param name="clipIndex"></param>
		/// <param name="bonesInPose"></param>
		public void GetPoseInTime(ref NativeArray<BoneData> pose, float inTime, int clipIndex)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startBoneIndex = MotionDataInfos[clipIndex].StartBoneIndex + lastFrameIndex * PoseBonesCount;
				for (int i = 0; i < PoseBonesCount; i++)
				{
					pose[i] = Bones[startBoneIndex + i];
				}
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstBoneStartIndex = MotionDataInfos[clipIndex].StartBoneIndex + localFrameIndex * PoseBonesCount;
				int secondBoneStartIndex = MotionDataInfos[clipIndex].StartBoneIndex + nextFrameIndex * PoseBonesCount;

				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < PoseBonesCount; i++)
				{
					pose[i] = BoneData.Lerp(
						Bones[firstBoneStartIndex + i],
						Bones[secondBoneStartIndex + i],
						factor
						);
				}
			}
		}

		public void GetContactsInTime(ref NativeList<FrameContact> contacts, float inTime, int clipIndex)
		{
			MotionMatchingDataInfo info = MotionDataInfos[clipIndex];
			int contactsInFrame = info.ContactPoints.Count;
			contacts.Clear();
			float localTime = inTime % info.Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / info.FrameTime);
			int lastFrameIndex = info.FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startContactIndex = info.StartContactIndex + lastFrameIndex * contactsInFrame;
				for (int i = 0; i < contactsInFrame; i++)
				{
					contacts.Add(Contacts[startContactIndex + i]);
				}
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstContactStartIndex = info.StartContactIndex + localFrameIndex * contactsInFrame;
				int secondContactStartIndex = info.StartContactIndex + nextFrameIndex * contactsInFrame;

				int globalFirstFrameIndex = info.StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / info.FrameTime;

				for (int i = 0; i < contactsInFrame; i++)
				{
					contacts.Add(FrameContact.Lerp(
						Contacts[firstContactStartIndex + i],
						Contacts[secondContactStartIndex + i],
						factor
						));
				}
			}
		}

		/// <summary>
		/// Gets contact in localTime
		/// </summary>
		/// <param name="contact"></param>
		/// <param name="inTime"></param>
		/// <param name="clipIndex"></param>
		/// <param name="contactIndex"></param>
		public void GetContactInTime(ref FrameContact contact, float inTime, int clipIndex, int contactIndex)
		{
			MotionMatchingDataInfo info = MotionDataInfos[clipIndex];

			int contactsInFrame = MotionDataInfos[clipIndex].ContactPoints.Count;



			float localTime = inTime % info.Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / info.FrameTime);
			int lastFrameIndex = info.FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startContactIndex = MotionDataInfos[clipIndex].StartContactIndex + lastFrameIndex * contactsInFrame;
				contact = Contacts[startContactIndex + contactIndex];
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstContactStartIndex = info.StartContactIndex + localFrameIndex * contactsInFrame;
				int secondContactStartIndex = info.StartContactIndex + nextFrameIndex * contactsInFrame;

				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / info.FrameTime;

				contact = FrameContact.Lerp(
					Contacts[firstContactStartIndex + contactIndex],
					Contacts[secondContactStartIndex + contactIndex],
					factor
					);

				contact.position = contact.position / this.ContactsCostWeight;
				contact.normal = contact.normal / this.ContactsCostWeight;
			}
		}

		#region Binary asset loading stuff
		private void LoadNativeData()
		{
			// Creating section Indexes
			if (SectionsDependencies != null)
			{
				SectionIndexes = SectionsDependencies.SectionIndexes;
			}


			// Loading binary data
			LoadBinaryData();

			// Selecting best frames count per thread count
			int? availableThreads = systemSettings.ThreadCountToUse;

			if (availableThreads == null)
			{
				Debug.LogError("Available threads in MotionMatchingSystemSettings are not initialized yet. This should not happend. If happend I do not have idea how to fix this. Creator of this shit Dreaw. Cheers!");
			}

			int bestThreadCount = Mathf.CeilToInt((float)Frames.Length / (float)systemSettings.MaxFramesPerThread);
			if (bestThreadCount >= availableThreads)
			{
				JobsCount = availableThreads.Value;
			}
			else
			{
				JobsCount = bestThreadCount;
			}

			FramesPerThread = Mathf.CeilToInt((float)Frames.Length / (float)JobsCount);

			// Initialziing bone weights:
			NativeNormalizedBonesWeights = new NativeArray<float2>(NormalizedBonesWeights.Count, Allocator.Persistent);

			for (int i = 0; i < NativeNormalizedBonesWeights.Length; i++)
			{
				NativeNormalizedBonesWeights[i] = NormalizedBonesWeights[i];
			}
		}

		public void DisposeNativeData()
		{
			if (Frames.IsCreated) Frames.Dispose();
			if (TrajectoryPoints.IsCreated) TrajectoryPoints.Dispose();
			if (Bones.IsCreated) Bones.Dispose();
			if (Contacts.IsCreated) Contacts.Dispose();
			if (NativeNormalizedBonesWeights.IsCreated) NativeNormalizedBonesWeights.Dispose();
		}

		public void LoadBinaryData()
		{
			string path = GetPathToBinaryAsset();
			BinaryMotionDataGroup binaryMotionGroup = BinaryMotionDataGroup.DeserializeBinary(path);

			if (binaryMotionGroup == null)
			{
				isBinaryDataLoaded = false;
#if UNITY_EDITOR
				Debug.LogError(string.Format("Failed to load binary data for NativeMoionGroup \"{0}\" at path \"{1}\"", this.name, AssetDatabase.GetAssetPath(this)));
#endif
			}
			else
			{
				isBinaryDataLoaded = true;
				CreateNativeData(binaryMotionGroup);
			}
		}

		private void CreateNativeData(BinaryMotionDataGroup binaryMotionGroup)
		{
			int startIndex = 0;
			// creating frame data
			//Frames = new NativeArray<FrameDataInfo>(binaryMotionGroup.FramesData.ToArray(), Allocator.Persistent);

			Frames = new NativeArray<FrameDataInfo>(binaryMotionGroup.ClipsIndexes.Length, Allocator.Persistent);

			for (int i = 0; i < binaryMotionGroup.ClipsIndexes.Length; i++)
			{
				Frames[i] = new FrameDataInfo(
					binaryMotionGroup.ClipsIndexes[i],
					binaryMotionGroup.LocalTimes[i],
					new FrameSections(binaryMotionGroup.Sections[i]),
					binaryMotionGroup.NeverChecking[i] == 1 ? true : false
					);
			}


			startIndex = 0;
			// Creating trajectory points:
			TrajectoryPoints = new NativeArray<TrajectoryPoint>(
				binaryMotionGroup.TrajectoryPoints.Length / BinaryMotionDataGroup.FloatsInTrajectoryPoint,
				Allocator.Persistent
				);
			int tpIndex = 0;
			while (startIndex < binaryMotionGroup.TrajectoryPoints.Length)
			{
				TrajectoryPoint tp;
				startIndex = TrajectoryPoint.FromArray(out tp, ref binaryMotionGroup.TrajectoryPoints, startIndex);
				TrajectoryPoints[tpIndex] = tp;
				tpIndex++;
			}


			startIndex = 0;
			// Creating bone data
			Bones = new NativeArray<BoneData>(
				binaryMotionGroup.Bones.Length / BinaryMotionDataGroup.FloatsInBone,
				Allocator.Persistent
				);
			int bdIndex = 0;
			while (startIndex < binaryMotionGroup.Bones.Length)
			{
				BoneData bd;
				startIndex = BoneData.FromArray(out bd, binaryMotionGroup.Bones, startIndex);
				Bones[bdIndex] = bd;
				bdIndex++;
			}

			startIndex = 0;
			// Creating contacts:
			Contacts = new NativeArray<FrameContact>(
				binaryMotionGroup.Contacts.Length / BinaryMotionDataGroup.FloatsInContacts,
				Allocator.Persistent
				);
			int cpIndex = 0;
			while (startIndex < binaryMotionGroup.Contacts.Length)
			{
				FrameContact fc;
				startIndex = FrameContact.FromArray(out fc, binaryMotionGroup.Contacts, startIndex);
				fc.IsImpact = binaryMotionGroup.ContactIsImpacts[cpIndex];
				Contacts[cpIndex] = fc;
				cpIndex++;
			}
		}

		public string GetPathToBinaryAsset()
		{
			return string.Format(
				"{0}/{1}/{2}",
				Application.streamingAssetsPath,
				FileName,
				binaryAssetName
				);

		}
		#endregion

#if UNITY_EDITOR
		[SerializeField]
		public List<MotionMatchingData> AnimationData = new List<MotionMatchingData>();
		[SerializeField]
		public bool FoldMotionMatchingGroups = false;


		#region Trajectory weights:
		[SerializeField]
		public bool NormalizeTrajctoryWeights = true;
		[SerializeField]
		[Min(0.01f)]
		public float BufforTrajectoryPositionWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float BufforTrajectoryVelocityWeight = 1f;
		[SerializeField]
		[Min(0.01f)]
		public float BufforTrajectoryOrientationWeight = 1f;
		#endregion


		public HashSet<MotionMatchingComponent> m_Subscribers;

		public bool SubscribeByMotionMatchingComponent(MotionMatchingComponent motionMatching)
		{
			return m_Subscribers.Add(motionMatching);
		}

		public bool UnsubscribeByMotionMatchingComponent(MotionMatchingComponent motionMatching)
		{
			return m_Subscribers.Remove(motionMatching);
		}

		private void CreateBinaryData()
		{
			string dirrectoryPath = GetDirectoryPath();
			//Debug.Log(dirrectoryPath);
			Directory.CreateDirectory(dirrectoryPath);
			BinaryMotionDataGroup binaryGroup = new BinaryMotionDataGroup(this);
			string path = GetPathToBinaryAsset();

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			CreateBinaryAssetName();
			path = GetPathToBinaryAsset();
			//Debug.Log(path);
			BinaryMotionDataGroup.SerializeBinary(binaryGroup, path);
		}

		private void UpdateFromCurrentAnimationData()
		{
			if (AnimationData == null || AnimationData.Count == 0)
			{
				return;
			}

			if (MotionDataInfos == null)
			{
				MotionDataInfos = new List<MotionMatchingDataInfo>();
			}
			MotionDataInfos.Clear();

			bool isTrajectoryTimesCreated = false;

			int startFrameDataIndex = 0;
			int startTrajectoryPointIndex = 0;
			int startBoneIndex = 0;
			int startContactIndex = 0;

			for (int i = 0; i < AnimationData.Count; i++)
			{
				MotionMatchingData data = AnimationData[i];

				if (data == null)
				{
					Debug.LogError($"Data at index {i} is null in native motion group \"{this.name}\".");
					continue;
				}


				MotionDataInfos.Add(new MotionMatchingDataInfo(
					data,
					startFrameDataIndex,
					startTrajectoryPointIndex,
					startBoneIndex,
					startContactIndex
					));

				startFrameDataIndex += data.frames.Count;
				startTrajectoryPointIndex += (data.frames.Count * data[0].trajectory.Length);
				startBoneIndex += (data.frames.Count * data[0].pose.bones.Length);
				startContactIndex += (data.frames.Count * data[0].contactPoints.Length);

				if (!isTrajectoryTimesCreated)
				{
					isTrajectoryTimesCreated = true;

					TrajectoryTimes = new float[data.trajectoryPointsTimes.Count];
					for (int tpIndex = 0; tpIndex < TrajectoryTimes.Length; tpIndex++)
					{
						TrajectoryTimes[tpIndex] = data.trajectoryPointsTimes[tpIndex];
					}
					PoseBonesCount = data[0].pose.bones.Length;
					ContactPointsCount = data.contactPoints.Count;

					UpdateBoneWeights(data);
				}
			}


			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssetIfDirty(this);
		}

		public void UpdateFromAnimationData()
		{
			if (Application.isPlaying && m_Subscribers != null && m_Subscribers.Count > 0)
			{
				Dictionary<MotionMatchingComponent, bool> currentStates = new Dictionary<MotionMatchingComponent, bool>();
				foreach (MotionMatchingComponent mmc in m_Subscribers)
				{
					currentStates.Add(mmc, mmc.enabled);
					mmc.enabled = false;
					mmc.WaitForJobsEnd();
				}

				DisposeNativeData();

				UpdateFromCurrentAnimationData();
				CreateBinaryData();

				LoadNativeData();

				foreach (MotionMatchingComponent mmc in m_Subscribers)
				{
					mmc.enabled = currentStates[mmc];
				}
			}
			else
			{
				UpdateFromCurrentAnimationData();
				CreateBinaryData();
			}
		}

		public void UpdateBoneWeights(MotionMatchingData data)
		{
			List<string> dataBones;
			if (AnimationData.Count != 0)
			{
				dataBones = data.BonesNames;
			}
			else
			{
				dataBones = new List<string>();
				for (int i = 0; i < BonesWeights.Count; i++)
				{
					dataBones.Add(BonesWeights[i].BoneName);
				}
			}

			List<BoneWeightInfo> newBoneWeights = new List<BoneWeightInfo>();
			foreach (string name in dataBones)
			{
				newBoneWeights.Add(new BoneWeightInfo(name, 1f, 1f));
			}

			for (int i = 0; i < newBoneWeights.Count; i++)
			{
				foreach (BoneWeightInfo oldInfo in BonesWeights)
				{
					if (oldInfo.BoneName.Equals(newBoneWeights[i].BoneName))
					{
						newBoneWeights[i] = oldInfo;
					}
				}
			}

			BonesWeights = newBoneWeights;

			// Calculating normalized weights:

			// calculating bones weight sum;
			//float positionWeightSum = 0f;
			//float velocityWeightSum = 0f;

			//foreach (BoneWeightInfo info in BonesWeights)
			//{
			//	positionWeightSum += info.PositionWeight;
			//	velocityWeightSum += info.VelocityWeight;
			//}

			//float weightMinSum = BonesWeights.Count * 2;
			//NormalizedBonesWeights = new List<float2>();
			//for (int i = 0; i < BonesWeights.Count; i++)
			//{
			//	float positionWeight = BonesWeights[i].PositionWeight;//BonesWeights[i].PositionWeight / positionWeightSum) * weightMinSum;
			//	float velocityWeight = BonesWeights[i].VelocityWeight;//(BonesWeights[i].VelocityWeight / velocityWeightSum) * weightMinSum;
			//	NormalizedBonesWeights.Add(new float2(positionWeight, velocityWeight));
			//}


			NormalizedBonesWeights.Clear();
			if (NormalizeWeights)
			{
				float positionWeightSum = 0;
				float velocityWeightSum = 0;

				foreach (BoneWeightInfo weightInfo in this.BonesWeights)
				{
					positionWeightSum += weightInfo.PositionWeight;
					velocityWeightSum += weightInfo.VelocityWeight;
				}

				for (int i = 0; i < this.BonesWeights.Count; i++)
				{
					BoneWeightInfo b = this.BonesWeights[i];
					b.PositionWeight = b.PositionWeight / positionWeightSum * this.BonesWeights.Count;
					b.VelocityWeight = b.VelocityWeight / velocityWeightSum * this.BonesWeights.Count;
					NormalizedBonesWeights.Add(new float2(b.PositionWeight, b.VelocityWeight));
				}
			}
			else
			{
				for (int i = 0; i < this.BonesWeights.Count; i++)
				{
					NormalizedBonesWeights.Add(new float2(
						this.BonesWeights[i].PositionWeight,
						this.BonesWeights[i].VelocityWeight
						));
				}
			}
		}

		public string GetDirectoryPath()
		{
			return string.Format(
				"{0}/{1}",
				Application.streamingAssetsPath,
				FileName
				);
		}

		public void CreateBinaryAssetName()
		{
			//int stringID = 0;
			//for (int i = 0; i < this.name.Length; i++)
			//{
			//	int letter = this.name[i];

			//	stringID += letter;
			//}

			//binaryAssetName = $"{stringID}.{FileExtension}";

			binaryAssetName = $"{this.name}.{FileExtension}";

			string path = string.Format(
				"{0}/{1}/{2}",
				Application.streamingAssetsPath,
				FileName,
				binaryAssetName
				);

			int index = 1;
			while (File.Exists(path))
			{
				binaryAssetName = $"{this.name}_{index}.{FileExtension}";

				path = string.Format(
					"{0}/{1}/{2}",
					Application.streamingAssetsPath,
					FileName,
					binaryAssetName
					);

				index += 1;
			}
		}

		public static NativeMotionGroup[] GetAllExsitingNativeMotionGroupsInProject_EditorOnly()
		{
			string[] groupsGUIDS = AssetDatabase.FindAssets("t:NativeMotionGroup");

			List<NativeMotionGroup> motionGroups = new List<NativeMotionGroup>();

			foreach (string guid in groupsGUIDS)
			{
				NativeMotionGroup group = AssetDatabase.LoadAssetAtPath<NativeMotionGroup>(AssetDatabase.GUIDToAssetPath(guid));

				if (group != null)
				{
					motionGroups.Add(group);
				}
			}

			return motionGroups.ToArray();
		}

#endif
		/// <summary>
		/// Each BoneData in pose array must be initialized.
		/// </summary>
		public void GetCurrentPoseInTimeWithWeight(ref BoneData[] pose, int clipIndex, float inTime, float weight = 1f)
		{
			float localTime = inTime % MotionDataInfos[clipIndex].Length;
			int localFrameIndex = Mathf.FloorToInt(localTime / MotionDataInfos[clipIndex].FrameTime);
			int lastFrameIndex = MotionDataInfos[clipIndex].FrameDataCount - 1;

			if (localFrameIndex >= lastFrameIndex)
			{
				int startBoneIndex = MotionDataInfos[clipIndex].StartBoneIndex + lastFrameIndex * PoseBonesCount;
				for (int i = 0; i < PoseBonesCount; i++)
				{
					BoneData bd = pose[i];

					bd.localPosition = bd.localPosition / NormalizedBonesWeights[i].x / PoseCostWeight;
					bd.velocity = bd.velocity / NormalizedBonesWeights[i].y / PoseCostWeight;

					pose[i] = bd + weight * Bones[startBoneIndex + i];
				}
			}
			else
			{
				int nextFrameIndex = localFrameIndex + 1;
				int firstBoneStartIndex = MotionDataInfos[clipIndex].StartBoneIndex + localFrameIndex * PoseBonesCount;
				int secondBoneStartIndex = MotionDataInfos[clipIndex].StartBoneIndex + nextFrameIndex * PoseBonesCount;

				int globalFirstFrameIndex = MotionDataInfos[clipIndex].StartFrameDataIndex + localFrameIndex;

				float factor = (localTime - Frames[globalFirstFrameIndex].localTime) / MotionDataInfos[clipIndex].FrameTime;

				for (int i = 0; i < PoseBonesCount; i++)
				{
					BoneData bd = pose[i];
					BoneData lerpedBoneData = BoneData.Lerp(
						Bones[firstBoneStartIndex + i],
						Bones[secondBoneStartIndex + i],
						factor
						);

					lerpedBoneData.localPosition = lerpedBoneData.localPosition / NormalizedBonesWeights[i].x / PoseCostWeight;
					lerpedBoneData.velocity = lerpedBoneData.velocity / NormalizedBonesWeights[i].y / PoseCostWeight;
					pose[i] = bd + weight * lerpedBoneData;
				}
			}
		}
	}



	[System.Serializable]
	public class MotionMatchingDataInfo
	{
		[SerializeField]
		public AnimationDataType DataType;
		[SerializeField]
		public List<AnimationClip> Clips;
		[SerializeField]
		public float FrameTime;
		[SerializeField]
		public float Length;
		[SerializeField]
		public bool IsLooping;
		[SerializeField]
		public bool BlendToYourself = true;
		[SerializeField]
		public bool FindInYourself = true;
		[SerializeField]
		public List<MotionMatchingDataCurve> Curves;
		[SerializeField]
		public List<MotionMatchingAnimationEvent> AnimationEvents;

		#region BlendTree fields
		[SerializeField]
		public float[] BlendTreeWeights;
		#endregion

		#region Animations Sequence fields
		// x - where animation start playing
		// y - where animation start blending to next
		// z - blend time to next animation
		#endregion


		#region Sections
		[SerializeField]
		public List<DataSection> Sections;
		[SerializeField]
		public DataSection NeverChecking;
		[SerializeField]
		public DataSection NotLookingForNewPose;
		#endregion

		#region ContactPoints
		[SerializeField]
		public ContactStateType ContactsType = ContactStateType.NormalContacts;
		[SerializeField]
		public List<MotionMatchingContact> ContactPoints = new List<MotionMatchingContact>();
		#endregion


		#region For jobs and geting trajectory contacts and pose
		[SerializeField]
		public int StartFrameDataIndex;
		[SerializeField]
		public int FrameDataCount;
		[SerializeField]
		public int StartTrajectoryPointIndex;
		[SerializeField]
		public int TrajectoryPointCount;
		[SerializeField]
		public int StartBoneIndex;
		[SerializeField]
		public int BonesCount;
		[SerializeField]
		public int StartContactIndex;
		[SerializeField]
		public int ContactsCount;
		#endregion

		#region BoneTracks
		[SerializeField]
		public List<BoneTrack> Tracks;
		#endregion


		#region ANIMATION SPEED CURVE:
		[SerializeField]
		public bool UseAnimationSpeedCurve = false;
		[SerializeField]
		public AnimationCurve AnimationSpeedCurve;
		#endregion

		public MotionMatchingDataInfo()
		{
		}

#if UNITY_EDITOR
		public MotionMatchingDataInfo(
			MotionMatchingData data,
			int startFrameDataIndex,
			int startTrajectoryPointIndex,
			int startBoneIndex,
			int startContactIndex
			)
		{
			data.ValidateData();

			DataType = data.dataType;
			Clips = new List<AnimationClip>(data.clips);
			FrameTime = data.frameTime;
			Length = data.animationLength;
			FrameTime = data.frameTime;
			IsLooping = data.isLooping;
			BlendToYourself = data.blendToYourself;
			FindInYourself = data.findInYourself;
			Curves = new List<MotionMatchingDataCurve>(data.Curves);
			BlendTreeWeights = data.blendTreeWeights;
			Sections = new List<DataSection>(data.sections);
			NeverChecking = data.neverChecking;
			NotLookingForNewPose = data.notLookingForNewPose;
			ContactsType = data.contactsType;
			ContactPoints = new List<MotionMatchingContact>(data.contactPoints);

			if (data.AnimationEvents != null)
			{
				data.AnimationEvents.Sort();
				AnimationEvents = new List<MotionMatchingAnimationEvent>(data.AnimationEvents);
			}

			StartFrameDataIndex = startFrameDataIndex;
			FrameDataCount = data.frames.Count;
			StartTrajectoryPointIndex = startTrajectoryPointIndex;
			TrajectoryPointCount = FrameDataCount * data[0].trajectory.Length;
			StartBoneIndex = startBoneIndex;
			BonesCount = FrameDataCount * data[0].pose.Count;
			StartContactIndex = startContactIndex;
			ContactsCount = FrameDataCount * data[0].contactPoints.Length;

			if (data.BoneTracks != null)
			{
				Tracks = new List<BoneTrack>(data.BoneTracks);
			}
			else
			{
				Tracks = new List<BoneTrack>();
			}


			UseAnimationSpeedCurve = data.UseAnimationSpeedCurve;
			AnimationSpeedCurve = data.AnimationSpeedCurve;
		}
#endif

		public BoneTrackAcces GetTrackAcces(int index)
		{
			if (Tracks != null && index >= 0 && index < Tracks.Count)
			{
				return new BoneTrackAcces(Tracks[index]);
			}

			return new BoneTrackAcces(null);
		}
	}


	[System.Serializable]
	public class BinaryMotionDataGroup
	{
		//[SerializeField]
		//public List<FrameDataInfo> FramesData;
		//[SerializeField]
		//public List<float> TrajectoryPoints;
		//[SerializeField]
		//public List<float> Bones;
		//[SerializeField]
		//public List<float> Contacts;

		//[SerializeField]
		//public FrameDataInfo[] FramesData;

		#region FRAME DATA
		[SerializeField]
		public int[] ClipsIndexes;
		[SerializeField]
		public float[] LocalTimes;
		[SerializeField]
		public uint[] Sections;
		[SerializeField]
		public int[] NeverChecking; // 1 - true, 0 false

		#endregion

		[SerializeField]
		public float[] TrajectoryPoints;
		[SerializeField]
		public float[] Bones;
		[SerializeField]
		public float[] Contacts;
		[SerializeField]
		public bool[] ContactIsImpacts;

		public const int FloatsInTrajectoryPoint = 9;
		public const int FloatsInBone = 6;
		public const int FloatsInContacts = 6;

		public BinaryMotionDataGroup()
		{

		}

#if UNITY_EDITOR
		public BinaryMotionDataGroup(NativeMotionGroup nativeGroup)
		{
			// FrameData:
			int framesCount = 0;
			int trajectoryPointsCount = 0;
			int bonesCount = 0;
			int contactCount = 0;

			foreach (MotionMatchingData data in nativeGroup.AnimationData)
			{
				framesCount += data.frames.Count;
				trajectoryPointsCount += (data.frames.Count * data.frames[0].trajectory.Length);
				bonesCount += (data.frames.Count * data.frames[0].pose.bones.Length);
				contactCount += (data.frames.Count * data.frames[0].contactPoints.Length);
			}

			//FramesData = new List<FrameDataInfo>();
			//TrajectoryPoints = new List<float>();
			//Bones = new List<float>();
			//Contacts = new List<float>();

			//FramesData = new FrameDataInfo[framesCount];
			ClipsIndexes = new int[framesCount];
			LocalTimes = new float[framesCount];
			Sections = new uint[framesCount];
			NeverChecking = new int[framesCount];

			TrajectoryPoints = new float[trajectoryPointsCount * FloatsInTrajectoryPoint];
			Bones = new float[bonesCount * FloatsInBone];
			Contacts = new float[contactCount * FloatsInContacts];
			ContactIsImpacts = new bool[contactCount];

			int dataIndex = 0;
			int currentFrame = 0;
			int currenttrajectoryPoint = 0;
			int currentBone = 0;
			int currentContact = 0;
			int currentContactIsImpact = 0;

			foreach (MotionMatchingData data in nativeGroup.AnimationData)
			{
				foreach (FrameData frame in data.frames)
				{
					ClipsIndexes[currentFrame] = dataIndex;
					LocalTimes[currentFrame] = frame.localTime;
					Sections[currentFrame] = frame.sections.sections;
					NeverChecking[currentFrame] = data.neverChecking.Contain(frame.localTime) ? 1 : 0;
					currentFrame++;


					// TrajectoryPoints
					for (int tpIndex = 0; tpIndex < frame.trajectory.Length; tpIndex++)
					{
						TrajectoryPoint tp = frame.trajectory.points[tpIndex];

						tp.Position *= nativeGroup.TrajectoryPositionWeight;
						tp.Velocity *= nativeGroup.TrajectoryVelocityWeight;
						tp.Orientation *= nativeGroup.TrajectoryOrientationWeight;

						tp *= nativeGroup.TrajectoryCostWeight;

						float[] array = tp.ToFloatArray();
						for (int i = 0; i < array.Length; i++)
						{
							//TrajectoryPoints.Add(array[i]);
							TrajectoryPoints[currenttrajectoryPoint] = array[i];
							currenttrajectoryPoint++;
						}
					}

					// Bones
					for (int bIndex = 0; bIndex < frame.pose.bones.Length; bIndex++)
					{
						BoneData bd = frame.pose.bones[bIndex];
						bd.localPosition = nativeGroup.NormalizedBonesWeights[bIndex].x * bd.localPosition;
						bd.velocity = nativeGroup.NormalizedBonesWeights[bIndex].y * bd.velocity;
						float[] array = bd.ToFloatArray();
						for (int i = 0; i < array.Length; i++)
						{
							//Bones.Add(array[i] * nativeGroup.PoseCostWeight);
							Bones[currentBone] = array[i] * nativeGroup.PoseCostWeight;
							currentBone++;
						}
					}

					// Contacts
					for (int cpIndex = 0; cpIndex < frame.contactPoints.Length; cpIndex++)
					{
						float[] array = frame.contactPoints[cpIndex].ToFloatArray();
						for (int i = 0; i < array.Length; i++)
						{
							//Contacts.Add(array[i]);
							Contacts[currentContact] = array[i] * nativeGroup.ContactsCostWeight;
							currentContact++;
						}

						MotionMatchingContact contact = data.contactPoints[cpIndex];

						ContactIsImpacts[currentContactIsImpact] = contact.startTime <= frame.localTime && frame.localTime <= contact.endTime;
						currentContactIsImpact += 1;
					}
				}

				//incrementing current data index
				dataIndex++;
			}
		}

		public static void SerializeBinary(BinaryMotionDataGroup binaryMotionGroup, string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}


			//FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			FileStream fileStream = File.Create(path);

			BinaryFormatter formatter = new BinaryFormatter();

			try
			{
				formatter.Serialize(fileStream, binaryMotionGroup);
			}
			catch (SerializationException e)
			{
				Debug.LogError(e.Message);
			}

			fileStream.Close();
		}
#endif
		public static BinaryMotionDataGroup DeserializeBinary(string path)
		{
			try
			{

				// Fix by AB user of discord:
#if UNITY_ANDROID
				var loadingRequest = UnityWebRequest.Get(path);
				loadingRequest.SendWebRequest();
				while (!loadingRequest.isDone && !loadingRequest.isNetworkError && !loadingRequest.isHttpError);
				MemoryStream fileStream = new MemoryStream(loadingRequest.downloadHandler.data);
#else
				FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
#endif

				BinaryFormatter formatter = new BinaryFormatter();

				BinaryMotionDataGroup binaryGroup = (BinaryMotionDataGroup)formatter.Deserialize(fileStream);

				fileStream.Close();
				return binaryGroup;

			}
			catch (SystemException e)
			{
				Debug.LogError(e.Message);
			}

			return null;
		}

	}

	[System.Serializable]
	public struct BoneWeightInfo
	{
		[SerializeField]
		public string BoneName;
		[SerializeField]
		public float PositionWeight;
		[SerializeField]
		public float VelocityWeight;

		public BoneWeightInfo(string boneName, float boneWeight, float velocityWeight)
		{
			BoneName = boneName;
			PositionWeight = boneWeight;
			VelocityWeight = velocityWeight;
		}

		public BoneWeightInfo(BoneWeightInfo other)
		{
			this.BoneName = other.BoneName;
			this.PositionWeight = other.PositionWeight;
			this.VelocityWeight = other.VelocityWeight;
		}
	}

	//#if UNITY_EDITOR
	//	public class PreprocessBuildNativeMotionGroup : IPreprocessBuildWithReport
	//	{
	//		public int callbackOrder { get { return 0; } }

	//		public void OnPreprocessBuild(BuildReport report)
	//		{
	//			string path = $"{Application.streamingAssetsPath}/{NativeMotionGroup.FileName}";
	//			if (Directory.Exists(path))
	//			{
	//				Directory.Delete(path, true);
	//			}

	//			NativeMotionGroup[] groups = Resources.FindObjectsOfTypeAll<NativeMotionGroup>();
	//			string updatedMotionGroups = "Updated motion groups list:";
	//			int index = 1;
	//			foreach (NativeMotionGroup g in groups)
	//			{
	//				g.UpdateFromAnimationData();
	//				AssetDatabase.Refresh();
	//				updatedMotionGroups += string.Format("\n\t{0}.  {1}", index, g.name);
	//				updatedMotionGroups += string.Format("\n\t\t{0}", g.GetPathToBinaryAsset());
	//				index++;
	//			}
	//			Debug.Log(updatedMotionGroups);
	//		}
	//	}
	//#endif
}