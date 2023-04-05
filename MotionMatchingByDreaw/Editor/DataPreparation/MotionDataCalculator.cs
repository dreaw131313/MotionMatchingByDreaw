using MotionMatching.Gameplay;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace MotionMatching.Tools
{
	public class MotionDataCalculator
	{
		public static MotionMatchingData CalculateNormalData(
			GameObject go,
			PreparingDataPlayableGraph graph,
			AnimationClip clip,
			BonCalculationSettingsProfile[] bonesMask,
			int sampling,
			bool loop,
			Transform root,
			List<float> trajectoryStepTimes,
			bool blendToYourself,
			bool findInYourself
			)
		{
			if (!graph.IsValid())
			{
				graph.Initialize(go);
			}

			Transform goTransform = go.transform;
			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.identity;

			#region Need floats
			float frameTime = 1f / (float)sampling;
			int numberOfFrames = Mathf.FloorToInt(clip.length / frameTime) + 1;
			#endregion


			MotionMatchingData data = ScriptableObject.CreateInstance<MotionMatchingData>();
			data.InitialSetup(
				clip,
				sampling,
				clip.name,
				loop,
				clip.length,
				findInYourself,
				blendToYourself,
				AnimationDataType.SingleAnimation
				);

			data.BonesNames = new List<string>();
			for (int i = 0; i < bonesMask.Length; i++)
			{
				data.BonesNames.Add(bonesMask[i].Bone.name);
			}

			Gameplay.FrameData frameBuffer;
			BoneData boneBuffer;
			PoseData poseBuffor;
			Trajectory trajectoryBuffor;

			NeedValueToCalculateData[] previuData = new NeedValueToCalculateData[bonesMask.Length];
			NeedValueToCalculateData[] nextData = new NeedValueToCalculateData[bonesMask.Length];

			graph.AddClipPlayable(clip);
			graph.SetMixerInputWeight(0, 1f);
			graph.SetMixerInputTimeInPlace(0, 0f);

			graph.Evaluate(frameTime);


			for (int frameIndex = 0; frameIndex < numberOfFrames; frameIndex++)
			{
				Vector3 previousPos = goTransform.position;
				for (int i = 0; i < bonesMask.Length; i++)
				{
					previuData[i] = GetValuesFromTransform(bonesMask[i].Bone, root);
				}

				graph.Evaluate(frameTime);

				Vector3 nextPos = goTransform.position;

				float currentCheckingTime = frameIndex * frameTime;

				//Debug.Log((float)animator.GetMixerInputTime(0) - clip.length);

				for (int i = 0; i < bonesMask.Length; i++)
				{
					nextData[i] = GetValuesFromTransform(bonesMask[i].Bone, root);
				}

				poseBuffor = new PoseData(bonesMask.Length);
				for (int i = 0; i < bonesMask.Length; i++)
				{
					//float3 velocity = BoneData.CalculateVelocity(previuData[i].localPosition, nextData[i].localPosition, frameTime);

					float3 velocity = float3.zero;

					switch (bonesMask[i].VelocityCalculationType)
					{
						case BoneVelocityCalculationType.Local:
							{
								velocity = BoneData.CalculateVelocity(previuData[i].localPosition, nextData[i].localPosition, frameTime);
							}
							break;
						case BoneVelocityCalculationType.GlobalToLocal:
							{
								velocity = root.InverseTransformDirection((nextData[i].globalPosition - previuData[i].globalPosition) / frameTime);
							}
							break;
					}


					float3 localPosition = previuData[i].localPosition;
					quaternion orientation = previuData[i].localRotation;
					boneBuffer = new BoneData(localPosition, velocity);
					poseBuffor.SetBone(boneBuffer, i);
				}

				trajectoryBuffor = new Trajectory(trajectoryStepTimes.Count);

				Vector3 globalVel = (nextPos - previousPos) / frameTime;
				Vector3 localVel = goTransform.InverseTransformDirection(globalVel);

				frameBuffer = new Gameplay.FrameData(
					frameIndex,
					currentCheckingTime,
					trajectoryBuffor,
					poseBuffor,
					new FrameSections(true),
					localVel
					);
				data.AddFrame(frameBuffer);
			}



			float clipGlobalStart;
			Vector2 clipStartAndStop;
			float recordingClipTime;

			if (trajectoryStepTimes[0] < 0)
			{
				clipGlobalStart = trajectoryStepTimes[0];

				clipStartAndStop = new Vector2(-clipGlobalStart, -clipGlobalStart + clip.length);
			}
			else
			{
				clipGlobalStart = 0;
				clipStartAndStop = new Vector2(0, clip.length);
			}

			if (trajectoryStepTimes[trajectoryStepTimes.Count - 1] > 0)
			{
				recordingClipTime = clipStartAndStop.y + trajectoryStepTimes[trajectoryStepTimes.Count - 1] + 0.1f;
			}
			else
			{
				recordingClipTime = clipStartAndStop.y + 0.1f;
			}

			int samplesPerSecond = 100;
			float deltaTime = 1f / (float)samplesPerSecond;
			int dataCount = Mathf.CeilToInt(recordingClipTime / deltaTime);
			NeedValueToCalculateData[] recordData = new NeedValueToCalculateData[dataCount];

			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.identity;
			graph.SetMixerInputTimeInPlace(0, clipGlobalStart);

			recordData[0] = new NeedValueToCalculateData(
				go.transform.position,
				go.transform.forward,
				go.transform.rotation,
				go.transform.position
				);

			for (int i = 0; i < dataCount; i++)
			{
				graph.Evaluate(deltaTime);
				recordData[i] = new NeedValueToCalculateData(
					go.transform.position,
					go.transform.forward,
					go.transform.rotation,
					go.transform.position
					);
			}

			//clearing graph from all animations
			graph.ClearMainMixerInput();

			MotionDataCalculator.CalculateTrajectoryPointsFromRecordData(
				data,
				recordData,
				recordingClipTime,
				deltaTime,
				clipStartAndStop,
				trajectoryStepTimes
				);


			data.usedFrameCount = data.numberOfFrames;


			data.trajectoryPointsTimes = new List<float>();

			for (int i = 0; i < trajectoryStepTimes.Count; i++)
			{
				data.trajectoryPointsTimes.Add(trajectoryStepTimes[i]);
			}


			//data.curves.Clear();
			return data;
		}

		public static MotionMatchingData CalculateBlendTreeData(
			string name,
			GameObject go,
			PreparingDataPlayableGraph graph,
			AnimationClip[] clips,
			BonCalculationSettingsProfile[] bonesMask,
			List<Vector2> bonesWeights,
			Transform root,
			List<float> trajectoryStepTimes,
			float[] weightsForClips,
			int sampling,
			bool loop,
			bool blendToYourself,
			bool findInYourself
			)
		{
			if (!graph.IsValid())
			{
				graph.Initialize(go);
			}

			Transform goTransform = go.transform;
			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.identity;

			#region need floats
			float frameTime = 1f / (float)sampling;
			int numberOfFrames = Mathf.FloorToInt(clips[0].length / frameTime) + 1;
			#endregion

			float weightSum = 0f;
			for (int i = 0; i < weightsForClips.Length; i++)
			{
				weightSum += weightsForClips[i];
			}
			for (int i = 0; i < weightsForClips.Length; i++)
			{
				weightsForClips[i] = weightsForClips[i] / weightSum;
			}

			MotionMatchingData data = ScriptableObject.CreateInstance<MotionMatchingData>();
			data.InitialSetup(
				clips,
				weightsForClips,
				sampling,
				name,
				loop,
				clips[0].length,
				findInYourself,
				blendToYourself,
				AnimationDataType.BlendTree
				);

			data.BonesNames = new List<string>();
			for (int i = 0; i < bonesMask.Length; i++)
			{
				data.BonesNames.Add(bonesMask[i].Bone.name);
			}

			Gameplay.FrameData frameBuffer;
			BoneData boneBuffer;
			PoseData poseBuffor;
			Trajectory trajectoryBuffor;

			NeedValueToCalculateData[] previewBoneData = new NeedValueToCalculateData[bonesMask.Length];
			NeedValueToCalculateData[] nextBoneData = new NeedValueToCalculateData[bonesMask.Length];

			for (int i = 0; i < clips.Length; i++)
			{
				graph.AddClipPlayable(clips[i]);
				graph.SetMixerInputTime(i, 0f);
				graph.SetMixerInputWeight(i, weightsForClips[i]);
			}

			graph.Evaluate(frameTime);

			int frameIndex = 0;
			float currentCheckingTime = 0f;

			// FramesCalculation
			for (; frameIndex < numberOfFrames; frameIndex++)
			{
				Vector3 previousPos = goTransform.position;

				for (int i = 0; i < bonesMask.Length; i++)
				{
					previewBoneData[i] = GetValuesFromTransform(bonesMask[i].Bone, root);
				}

				graph.Evaluate(frameTime);
				currentCheckingTime = frameIndex * frameTime;

				Vector3 nextPos = goTransform.position;

				for (int i = 0; i < bonesMask.Length; i++)
				{
					nextBoneData[i] = GetValuesFromTransform(bonesMask[i].Bone, root);
				}

				poseBuffor = new PoseData(bonesMask.Length);
				for (int i = 0; i < bonesMask.Length; i++)
				{
					float2 boneWeight = bonesWeights[i];
					//float3 velocity = BoneData.CalculateVelocity(previewBoneData[i].localPosition, nextBoneData[i].localPosition, frameTime);

					float3 velocity = float3.zero;

					switch (bonesMask[i].VelocityCalculationType)
					{
						case BoneVelocityCalculationType.Local:
							{
								velocity = BoneData.CalculateVelocity(previewBoneData[i].localPosition, nextBoneData[i].localPosition, frameTime);
							}
							break;
						case BoneVelocityCalculationType.GlobalToLocal:
							{
								velocity = root.InverseTransformDirection((nextBoneData[i].globalPosition - previewBoneData[i].globalPosition) / frameTime);
							}
							break;
					}


					float3 localPosition = previewBoneData[i].localPosition;
					quaternion orientation = previewBoneData[i].localRotation;
					boneBuffer = new BoneData(localPosition, velocity);
					poseBuffor.SetBone(boneBuffer, i);
				}

				trajectoryBuffor = new Trajectory(trajectoryStepTimes.Count);

				Vector3 globalVel = (nextPos - previousPos) / frameTime;
				Vector3 localVel = goTransform.InverseTransformDirection(globalVel);

				frameBuffer = new Gameplay.FrameData(
					frameIndex,
					currentCheckingTime,
					trajectoryBuffor,
					poseBuffor,
					new FrameSections(true),
					localVel
					);
				data.AddFrame(frameBuffer);
			}

			// Trajectory calculations
			float clipGlobalStart;
			Vector2 clipStartAndStop;
			float recordingClipTime;

			if (trajectoryStepTimes[0] < 0)
			{
				clipGlobalStart = trajectoryStepTimes[0];

				clipStartAndStop = new Vector2(-clipGlobalStart, -clipGlobalStart + clips[0].length);
			}
			else
			{
				clipGlobalStart = 0;
				clipStartAndStop = new Vector2(0, clips[0].length);
			}

			if (trajectoryStepTimes[trajectoryStepTimes.Count - 1] > 0)
			{
				recordingClipTime = clipStartAndStop.y + trajectoryStepTimes[trajectoryStepTimes.Count - 1] + 0.1f;
			}
			else
			{
				recordingClipTime = clipStartAndStop.y + 0.1f;
			}

			int samplesPerSecond = 100;
			float deltaTime = 1f / (float)samplesPerSecond;
			int dataCount = Mathf.CeilToInt(recordingClipTime / deltaTime);
			NeedValueToCalculateData[] recordData = new NeedValueToCalculateData[dataCount];

			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.identity;

			for (int i = 0; i < graph.GetMixerInputCount(); i++)
			{
				graph.SetMixerInputTimeInPlace(i, clipGlobalStart);
			}

			recordData[0] = new NeedValueToCalculateData(
				go.transform.position,
				go.transform.forward,
				go.transform.rotation,
				go.transform.position
				);

			for (int i = 0; i < dataCount; i++)
			{
				graph.Evaluate(deltaTime);
				recordData[i] = new NeedValueToCalculateData(
					go.transform.position,
					go.transform.forward,
					go.transform.rotation,
					go.transform.position
					);
			}

			//clearing graph from all animations
			graph.ClearMainMixerInput();

			MotionDataCalculator.CalculateTrajectoryPointsFromRecordData(
				data,
				recordData,
				recordingClipTime,
				deltaTime,
				clipStartAndStop,
				trajectoryStepTimes
				);

			data.usedFrameCount = data.numberOfFrames;

			data.trajectoryPointsTimes = new List<float>();

			for (int i = 0; i < trajectoryStepTimes.Count; i++)
			{
				data.trajectoryPointsTimes.Add(trajectoryStepTimes[i]);
			}

			return data;
		}

		public static void CalculateTrajectoryPointsFromRecordData(
			MotionMatchingData clip,
			NeedValueToCalculateData[] recordData,
			float recordDataLength,
			float recordStep,
			Vector2 startAndStopOfClip,
			List<float> trajectoryStepTimes
			)
		{
			Matrix4x4 frameMatrix;
			int firstFrameIndex = Mathf.FloorToInt(startAndStopOfClip.x / recordStep);

			//Debug.Log("first frame index "+firstFrameIndex);
			for (int fIndex = 0; fIndex < clip.frames.Count; fIndex++)
			{
				int frameIndexInRecordData = firstFrameIndex + Mathf.FloorToInt(clip.frames[fIndex].localTime / recordStep);
				frameMatrix = Matrix4x4.TRS(
					recordData[frameIndexInRecordData].localPosition,
					recordData[frameIndexInRecordData].localRotation,
					Vector3.one
					);


				Gameplay.FrameData bufforFrame = clip.frames[fIndex];
				// Debug.Log(frameIndexInRecordData);
				for (int i = 0; i < trajectoryStepTimes.Count; i++)
				{
					int pointIndex = Mathf.FloorToInt(trajectoryStepTimes[i] / recordStep);
					int recordDataIndex = frameIndexInRecordData + pointIndex;
					recordDataIndex = recordDataIndex < 0 ? 0 : recordDataIndex;
					NeedValueToCalculateData currentRecord = recordData[recordDataIndex];


					Vector3 pointPos = frameMatrix.inverse.MultiplyPoint3x4(currentRecord.localPosition);
					Vector3 pointVel;

					if (trajectoryStepTimes[i] < 0)
					{
						//	pointVel = frameMatrix.inverse.MultiplyVector(
						//		(recordData[frameIndexInRecordData].localPosition - currentRecord.localPosition) / Mathf.Abs(trajectoryStepTimes[i])

						NeedValueToCalculateData nextRecord = recordData[recordDataIndex + 1];
						pointVel = frameMatrix.inverse.MultiplyVector(
							(nextRecord.localPosition - currentRecord.localPosition) / recordStep
							);
					}
					else
					{
						//	pointVel = frameMatrix.inverse.MultiplyVector(
						//		(currentRecord.localPosition - recordData[frameIndexInRecordData].localPosition) / Mathf.Abs(trajectoryStepTimes[i])

						NeedValueToCalculateData previewRecord = recordData[recordDataIndex - 1];
						pointVel = frameMatrix.inverse.MultiplyVector(
							(currentRecord.localPosition - previewRecord.localPosition) / recordStep
							);
					}

					Vector3 pointOrientation = frameMatrix.inverse.MultiplyVector(currentRecord.localOrientation);
					bufforFrame.trajectory.SetPoint(
							new TrajectoryPoint(
								pointPos,
								pointVel,
								pointOrientation
							),
							i
						);
				}
				clip.frames[fIndex] = bufforFrame;
			}
		}

		public static void CalculateContactPoints(
			MotionMatchingData data,
			MotionMatchingContact[] contactPoints,
			PreparingDataPlayableGraph playableGraph,
			GameObject gameObject
			)
		{
			for (int i = 0; i < data.contactPoints.Count; i++)
			{
				MotionMatchingContact cp = data.contactPoints[i];
				cp.contactNormal = math.normalize(cp.contactNormal);
				data.contactPoints[i] = cp;
			}

			Vector3 startPos = gameObject.transform.position;
			Quaternion startRot = gameObject.transform.rotation;
			float deltaTime = data.frameTime;
			Matrix4x4 frameMatrix;

			NeedValueToCalculateData[] recordedData = new NeedValueToCalculateData[data.numberOfFrames];
			Vector3[] cpPos = new Vector3[contactPoints.Length];
			Vector3[] cpNormals = new Vector3[contactPoints.Length];
			Vector3[] cpForwards = new Vector3[contactPoints.Length];

			if (playableGraph != null)
			{
				playableGraph.Destroy();
			}

			playableGraph = new PreparingDataPlayableGraph();
			playableGraph.Initialize(gameObject);

			playableGraph.CreateAnimationDataPlayables(data);


			// RecordingData
			float currentTime = 0f;
			float currentDeltaTime = deltaTime;
			int contactPointIndex = 0;
			for (int i = 0; i < data.numberOfFrames; i++)
			{
				recordedData[i] = new NeedValueToCalculateData(
					gameObject.transform.position,
					gameObject.transform.forward,
					gameObject.transform.rotation,
					gameObject.transform.position
					);

				currentTime += deltaTime;
				if (contactPointIndex < contactPoints.Length && currentTime >= contactPoints[contactPointIndex].startTime)
				{
					float buforDeltaTime = currentTime - contactPoints[contactPointIndex].startTime;
					currentDeltaTime = deltaTime - buforDeltaTime;

					playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);

					cpPos[contactPointIndex] = gameObject.transform.TransformPoint(contactPoints[contactPointIndex].position);
					cpNormals[contactPointIndex] = gameObject.transform.TransformDirection(contactPoints[contactPointIndex].contactNormal);
					cpForwards[contactPointIndex] = gameObject.transform.forward;
					contactPointIndex++;

					playableGraph.EvaluateMotionMatchgData(data, buforDeltaTime);

					currentDeltaTime = deltaTime;
				}
				else
				{
					playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);
				}

			}

			// calcualationData
			for (int i = 0; i < data.numberOfFrames; i++)
			{
				frameMatrix = Matrix4x4.TRS(
					recordedData[i].localPosition,
					recordedData[i].localRotation,
					Vector3.one
					);

				Gameplay.FrameData currentFrame = data.frames[i];

				currentFrame.contactPoints = new FrameContact[cpPos.Length];
				for (int j = 0; j < cpPos.Length; j++)
				{
					Vector3 pos = frameMatrix.inverse.MultiplyPoint3x4(cpPos[j]);
					Vector3 norDir = frameMatrix.inverse.MultiplyVector(cpNormals[j]);
					Vector3 forw = frameMatrix.inverse.MultiplyVector(cpForwards[j]);
					FrameContact cp = new FrameContact(
						pos,
						norDir,
						false
						);
					currentFrame.contactPoints[j] = cp;
				}

				data.frames[i] = currentFrame;
			}

			gameObject.transform.position = startPos;
			gameObject.transform.rotation = startRot;

			if (data.contactPoints.Count >= 2)
			{
				for (int i = 0; i < contactPoints.Length - 1; i++)
				{
					Vector3 firstPoint = data.GetContactPointInTime(i, data.contactPoints[i].startTime).position;
					Vector3 secondPoint = data.GetContactPointInTime(i + 1, data.contactPoints[i].startTime).position;

					Vector3 dir = secondPoint - firstPoint;
					dir.y = 0;

					MotionMatchingContact c = data.contactPoints[i];
					c.rotationFromForwardToNextContactDir = Quaternion.FromToRotation(dir, Vector3.forward);
					data.contactPoints[i] = c;
				}
			}

			playableGraph.ClearMainMixerInput();
			playableGraph.Destroy();
		}

		public static void CalculateImpactPoints(
			MotionMatchingData data,
			MotionMatchingContact[] contactPoints,
			PreparingDataPlayableGraph playableGraph,
			GameObject gameObject
			)
		{
			// Normalizacja kierunków kontaktów
			for (int i = 0; i < data.contactPoints.Count; i++)
			{
				MotionMatchingContact cp = data.contactPoints[i];
				cp.contactNormal = math.normalize(cp.contactNormal);
				data.contactPoints[i] = cp;
			}

			// Pobrani początkowych wartości game objectu
			Vector3 startPos = gameObject.transform.position;
			Quaternion startRot = gameObject.transform.rotation;


			float deltaTime = data.frameTime;
			Matrix4x4 frameMatrix;


			NeedValueToCalculateData[] recordedData = new NeedValueToCalculateData[data.numberOfFrames];
			Vector3[] cpPos = new Vector3[contactPoints.Length];
			Vector3[] cpNormals = new Vector3[contactPoints.Length];
			Vector3[] cpForwards = new Vector3[contactPoints.Length];

			if (playableGraph != null)
			{
				playableGraph.Destroy();
			}

			playableGraph = new PreparingDataPlayableGraph();
			playableGraph.Initialize(gameObject);

			playableGraph.CreateAnimationDataPlayables(data);


			// RecordingData
			float currentTime = 0f;
			float currentDeltaTime = deltaTime;
			int contactPointIndex = 0;
			for (int i = 0; i < data.numberOfFrames; i++)
			{
				recordedData[i] = new NeedValueToCalculateData(
					gameObject.transform.position,
					gameObject.transform.forward,
					gameObject.transform.rotation,
					gameObject.transform.position
					);

				currentTime += deltaTime;
				if (contactPointIndex < contactPoints.Length && currentTime >= contactPoints[contactPointIndex].startTime)
				{
					float buforDeltaTime = currentTime - contactPoints[contactPointIndex].startTime;
					currentDeltaTime = deltaTime - buforDeltaTime;

					playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);

					cpPos[contactPointIndex] = gameObject.transform.TransformPoint(contactPoints[contactPointIndex].position);
					cpNormals[contactPointIndex] = gameObject.transform.TransformDirection(contactPoints[contactPointIndex].contactNormal);
					cpForwards[contactPointIndex] = gameObject.transform.forward;
					contactPointIndex++;

					playableGraph.EvaluateMotionMatchgData(data, buforDeltaTime);

					currentDeltaTime = deltaTime;
				}
				else
				{
					playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);
				}

			}

			// calcualationData
			for (int i = 0; i < data.numberOfFrames; i++)
			{
				frameMatrix = Matrix4x4.TRS(
					recordedData[i].localPosition,
					recordedData[i].localRotation,
					Vector3.one
					);

				Gameplay.FrameData currentFrame = data.frames[i];

				for (int impactIndex = 0; impactIndex < data.contactPoints.Count; impactIndex++)
				{
					if (data.contactPoints[impactIndex].IsContactInTime(currentFrame.localTime))
					{
						currentFrame.contactPoints = new FrameContact[1];
						Vector3 pos = frameMatrix.inverse.MultiplyPoint3x4(cpPos[impactIndex]);
						Vector3 norDir = frameMatrix.inverse.MultiplyVector(cpNormals[impactIndex]);
						Vector3 forw = frameMatrix.inverse.MultiplyVector(cpForwards[impactIndex]);
						FrameContact cp = new FrameContact(
							pos,
							norDir,
							false
							);
						currentFrame.contactPoints[0] = cp;
						break;
					}
					else
					{
						currentFrame.contactPoints = new FrameContact[0];
					}
				}
				if (data.contactPoints.Count == 0)
				{
					currentFrame.contactPoints = new FrameContact[0];
				}
				data.frames[i] = currentFrame;
			}

			gameObject.transform.position = startPos;
			gameObject.transform.rotation = startRot;

			//if (data.contactPoints.Count >= 2)
			//{
			//    Vector3 firstPoint = data.GetContactPoint(0, data.contactPoints[0].startTime).position;
			//    Vector3 secondPoint = data.GetContactPoint(1, data.contactPoints[0].startTime).position;

			//    Vector3 dir = secondPoint - firstPoint;
			//    dir.y = 0;
			//    data.fromFirstToSecondContactRot = Quaternion.FromToRotation(dir, Vector3.forward);
			//}
			//else
			//{
			//    data.fromFirstToSecondContactRot = Quaternion.identity;
			//}

			playableGraph.ClearMainMixerInput();
			playableGraph.Destroy();
		}


		public static NeedValueToCalculateData GetValuesFromTransform(Transform t, Transform root)
		{
			//return new NeedValueToCalculateData(root.InverseTransformPoint(t.position), root.InverseTransformDirection(t.forward), t.localRotation);


			NeedValueToCalculateData data = new NeedValueToCalculateData();

			data.localPosition = root.InverseTransformPoint(t.position);
			data.localOrientation = root.InverseTransformDirection(t.forward);
			data.localRotation = t.localRotation;

			data.globalPosition = t.position;
			return data;



		}


		public static bool CreateTracksIntervals(
			GameObject go,
			PreparingDataPlayableGraph graph,
			AnimationClip clip,
			BoneTrackSettings settings,
			BoneTrack track
			)
		{
			track.Intervals = new List<BoneTrackInterval>();

			graph.Destroy();
			graph.Initialize(go);

			graph.AddClipPlayable(clip);
			graph.SetMixerInputWeight(0, 1f);
			graph.SetMixerInputTimeInPlace(0, 0f);

			float samplingTime = 0.01f;

			Transform conditionBone = RecursiveFindChild(go.transform, settings.ConditionBoneName);
			if (conditionBone == null)
			{
				Debug.LogError($"In game object \"{go.name}\" not exit bone with name \"{settings.ConditionBoneName}\"!");
				return false;
			}

			Vector3 boneVel;

			if (clip.isLooping)
			{
				graph.SetMixerInputTimeInPlace(0, -samplingTime);

				Vector3 pos = conditionBone.position;
				graph.Evaluate(samplingTime);
				boneVel = (conditionBone.position - pos) / samplingTime;
			}
			else
			{
				graph.SetMixerInputTimeInPlace(0, 0f);

				Vector3 pos = conditionBone.position;
				graph.Evaluate(samplingTime);
				boneVel = (conditionBone.position - pos) / samplingTime;
				graph.SetMixerInputTimeInPlace(0, 0f);
			}


			float samplesCount = Mathf.FloorToInt(clip.length / samplingTime);

			bool laststate = false;

			float intervalStart = 0f;
			float intervalEnd = 0f;

			for (int i = 0; i < samplesCount; i++)
			{
				Vector3 bonePos = conditionBone.transform.position;
				bool currentState = settings.Filter.CheckFilter(go, conditionBone, boneVel);

				if (currentState != laststate)
				{
					laststate = currentState;
					if (currentState)
					{
						intervalStart = i * samplingTime;
					}
					else
					{
						intervalEnd = (i - 1) * samplingTime;

						track.Intervals.Add(new BoneTrackInterval(
							0f,
							new float2(intervalStart, intervalEnd)
							));
					}
				}

				graph.Evaluate(samplingTime);
				boneVel = (conditionBone.position - bonePos) / samplingTime;
			}

			if (laststate)
			{
				track.Intervals.Add(new BoneTrackInterval(
							0f,
							new float2(intervalStart, clip.length)
							));
			}

			settings.Filter.EditIntervals(ref track.Intervals);


			return true;
		}

		public static bool CreateBoneTrackData(
			GameObject go,
			PreparingDataPlayableGraph graph,
			AnimationClip clip,
			BoneTrack track
			)
		{
			if (track.Intervals.Count == 0)
			{
				return false;
			}

			track.SamplingTime = track.TrackSettings.SamplingTime;

			track.Intervals.Sort((i1, i2) =>
			{
				if (i1.TimeInterval.x < i2.TimeInterval.x)
				{
					return -1;
				}
				else if (i1.TimeInterval.x > i2.TimeInterval.x)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			});

			for (int i = 0; i < track.Intervals.Count; i++)
			{
				BoneTrackInterval trackInterval = track.Intervals[i];
				trackInterval.StarTime = i == 0 ? 0f : track.Intervals[i - 1].TimeInterval.y;
			}

			graph.Destroy();
			graph.Initialize(go);

			graph.AddClipPlayable(clip);
			graph.SetMixerInputWeight(0, 1f);
			graph.SetMixerInputTimeInPlace(0, 0f);

			float samplingTime = track.TrackSettings.SamplingTime;

			Transform dataBone = RecursiveFindChild(go.transform, track.TrackSettings.DataBoneName);

			if (dataBone == null)
			{
				Debug.LogError($"In game object \"{go.name}\" not exit bone with name \"{track.TrackSettings.DataBoneName}\"!");
				return false;
			}

			Vector3 boneVel;

			if (clip.isLooping)
			{
				graph.SetMixerInputTimeInPlace(0, -samplingTime);

				Vector3 pos = dataBone.position;
				graph.Evaluate(samplingTime);
				boneVel = (dataBone.position - pos) / samplingTime;
			}
			else
			{
				graph.SetMixerInputTimeInPlace(0, 0f);

				Vector3 pos = dataBone.position;
				graph.Evaluate(samplingTime);
				boneVel = (dataBone.position - pos) / samplingTime;
				graph.SetMixerInputTimeInPlace(0, 0f);
			}

			graph.Evaluate(samplingTime);

			foreach (BoneTrackInterval trackInterval in track.Intervals)
			{
				float intervalTime = trackInterval.TimeInterval.y - trackInterval.StarTime;
				int sampledDataCount = Mathf.FloorToInt(intervalTime / track.TrackSettings.SamplingTime) + 1;

				TransformData[] recordedObjectData = new TransformData[sampledDataCount];
				TransformData lastObjectData;
				BoneTrackData recordedBoneData = new BoneTrackData();

				graph.SetMixerInputTimeInPlace(0, trackInterval.StarTime);
				graph.Evaluate(samplingTime);

				bool wasBoneRecorded = false;

				#region recording interval data
				for (int i = 0; i < sampledDataCount; i++)
				{
					recordedObjectData[i] = new TransformData(go.transform.position, go.transform.rotation);

					float currentTime = trackInterval.StarTime + i * samplingTime;

					if (currentTime + samplingTime > trackInterval.TimeInterval.x && !wasBoneRecorded)
					{
						wasBoneRecorded = true;
						float recordBoneDeltaTime = trackInterval.TimeInterval.x - currentTime;
						graph.Evaluate(recordBoneDeltaTime);
						recordedBoneData = new BoneTrackData(dataBone.position);
						graph.Evaluate(currentTime + samplingTime - trackInterval.TimeInterval.x);
					}
					else if (i < sampledDataCount - 1)
					{
						graph.Evaluate(samplingTime);
					}
				}
				#endregion


				#region calculating data from recorded data
				float lastSampleTime = (sampledDataCount - 1) * samplingTime;
				if (lastSampleTime < intervalTime)
				{
					trackInterval.UseLastData = true;

					float aditionalSampleDelta = intervalTime - lastSampleTime;
					graph.Evaluate(aditionalSampleDelta);
					lastObjectData = new TransformData(go.transform.position, go.transform.rotation);


					Matrix4x4 transformationMatrix = Matrix4x4.TRS(
						lastObjectData.Position,
						lastObjectData.Rotation,
						Vector3.one
						);
					trackInterval.LastData = new BoneTrackData(transformationMatrix.inverse.MultiplyPoint(recordedBoneData.Postion));
				}
				else
				{
					trackInterval.UseLastData = false;
				}

				trackInterval.Data = new BoneTrackData[sampledDataCount];

				for (int i = 0; i < sampledDataCount; i++)
				{
					TransformData transformData = recordedObjectData[i];
					Matrix4x4 transformationMatrix = Matrix4x4.TRS(
						transformData.Position,
						transformData.Rotation,
						Vector3.one
						);

					trackInterval.Data[i] = new BoneTrackData(
						transformationMatrix.inverse.MultiplyPoint(recordedBoneData.Postion)
						);
				}
				#endregion
			}


			track.HaveLastInterval = false;
			if (clip.isLooping)
			{
				BoneTrackInterval lastCheckedInterval = track.Intervals[track.Intervals.Count - 1];

				if (lastCheckedInterval.TimeInterval.y < clip.length)
				{
					float remainTime = clip.length - lastCheckedInterval.TimeInterval.y;

					if (remainTime > track.SamplingTime)
					{
						track.HaveLastInterval = true;

						track.LastInterval = new BoneTrackInterval(
							lastCheckedInterval.TimeInterval.y,
							new float2(
								clip.length + track.Intervals[0].TimeInterval.x,
								clip.length + track.Intervals[0].TimeInterval.y
								)
							);

						#region calculating last interval data

						BoneTrackInterval trackInterval = track.LastInterval;

						float intervalTime = trackInterval.TimeInterval.y - trackInterval.StarTime;
						int sampledDataCount = Mathf.FloorToInt(intervalTime / track.TrackSettings.SamplingTime) + 1;

						TransformData[] recordedObjectData = new TransformData[sampledDataCount];
						TransformData lastObjectData;
						BoneTrackData recordedBoneData = new BoneTrackData();

						graph.SetMixerInputTimeInPlace(0, trackInterval.StarTime);
						graph.Evaluate(samplingTime);

						bool wasBoneRecorded = false;

						#region recording interval data
						for (int i = 0; i < sampledDataCount; i++)
						{
							recordedObjectData[i] = new TransformData(go.transform.position, go.transform.rotation);

							float currentTime = trackInterval.StarTime + i * samplingTime;

							if (currentTime + samplingTime > trackInterval.TimeInterval.x && !wasBoneRecorded)
							{
								wasBoneRecorded = true;
								float recordBoneDeltaTime = trackInterval.TimeInterval.x - currentTime;
								graph.Evaluate(recordBoneDeltaTime);
								recordedBoneData = new BoneTrackData(dataBone.position);
								graph.Evaluate(currentTime + samplingTime - trackInterval.TimeInterval.x);
							}
							else if (i < sampledDataCount - 1)
							{
								graph.Evaluate(samplingTime);
							}
						}
						#endregion


						#region calculating data from recorded data
						float lastSampleTime = (sampledDataCount - 1) * samplingTime;
						if (lastSampleTime < intervalTime)
						{
							trackInterval.UseLastData = true;

							float aditionalSampleDelta = intervalTime - lastSampleTime;
							graph.Evaluate(aditionalSampleDelta);
							lastObjectData = new TransformData(go.transform.position, go.transform.rotation);


							Matrix4x4 transformationMatrix = Matrix4x4.TRS(
								lastObjectData.Position,
								lastObjectData.Rotation,
								Vector3.one
								);
							trackInterval.LastData = new BoneTrackData(transformationMatrix.inverse.MultiplyPoint(recordedBoneData.Postion));
						}
						else
						{
							trackInterval.UseLastData = false;
						}

						trackInterval.Data = new BoneTrackData[sampledDataCount];

						for (int i = 0; i < sampledDataCount; i++)
						{
							TransformData transformData = recordedObjectData[i];
							Matrix4x4 transformationMatrix = Matrix4x4.TRS(
								transformData.Position,
								transformData.Rotation,
								Vector3.one
								);

							trackInterval.Data[i] = new BoneTrackData(
								transformationMatrix.inverse.MultiplyPoint(recordedBoneData.Postion)
								);
						}
						#endregion

						#endregion
					}
				}
			}


			return true;

		}

		private static Transform RecursiveFindChild(Transform root, string name)
		{
			Transform t = root.Find(name);
			if (t != null) return t;

			foreach (Transform child in root)
			{
				t = RecursiveFindChild(child, name);
				if (t != null)
				{
					return t;
				}
			}

			return null;
		}
	}

	public struct NeedValueToCalculateData
	{
		public Vector3 localPosition;
		public Vector3 localOrientation;
		public Quaternion localRotation;


		public Vector3 globalPosition;


		//public NeedValueToCalculateData() { }


		public NeedValueToCalculateData(Vector3 pos, Vector3 orientation, Quaternion rot, Vector3 globalPos)
		{
			this.localPosition = pos;
			this.localRotation = rot;
			this.localOrientation = orientation;
			this.globalPosition = globalPos;
		}

	}

	public struct TransformData
	{
		public Vector3 Position;
		public Quaternion Rotation;

		public TransformData(Vector3 position, Quaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}
	}
}
