using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MotionMatching.Gameplay
{

	[System.Serializable]
	public class MotionMatchingData : ScriptableObject
	{
		//[SerializeField]
		//public new string name;
		[SerializeField]
		public AnimationDataType dataType;
		[SerializeField]
		public List<AnimationClip> clips;
		[SerializeField]
		public float frameRate;
		[SerializeField]
		public float frameTime;
		[SerializeField]
		public float animationLength;
		[SerializeField]
		public bool isLooping;
		[SerializeField]
		public bool blendToYourself = true;
		[SerializeField]
		public bool findInYourself = true;
		[SerializeField]
		public List<FrameData> frames = new List<FrameData>();
		[SerializeField]
		public List<float> trajectoryPointsTimes = new List<float>();
		[SerializeField]
		public List<string> BonesNames = new List<string>();
		[SerializeField]
		public List<MotionMatchingDataCurve> Curves = new List<MotionMatchingDataCurve>();

		#region BlendTree fields
		[SerializeField]
		public float[] blendTreeWeights;
		#endregion

		#region Sections
		[SerializeField]
		public List<DataSection> sections;
		[SerializeField]
		public DataSection neverChecking;
		[SerializeField]
		public DataSection notLookingForNewPose;
		[SerializeField]
		public int usedFrameCount;
		#endregion

		#region ContactPoints
		[SerializeField]
		public ContactStateType contactsType = ContactStateType.NormalContacts;
		[SerializeField]
		public List<MotionMatchingContact> contactPoints = new List<MotionMatchingContact>();
		#endregion

		#region Bone Tracks
		[SerializeField]
		public List<BoneTrack> BoneTracks;
		#endregion

		#region Properties 

		public int numberOfFrames
		{
			get
			{
				return frames.Count;
			}
			private set { }
		}

		public FrameData this[int index]
		{
			get { return frames[index]; }
		}

		#endregion


		#region static fields
		public static readonly int maxSectionsCounts = 32;
		#endregion

		#region Animation events:
		public List<MotionMatchingAnimationEvent> AnimationEvents;
		#endregion

		#region ANIMATION SPEED CURVE:
		[SerializeField]
		public bool UseAnimationSpeedCurve = false;
		[SerializeField]
		public AnimationCurve AnimationSpeedCurve;
		#endregion

		public MotionMatchingData()
		{
		}

		public void InitialSetup(
			AnimationClip clip,
			float frameRate,
			string name,
			bool seemless,
			float length,
			bool findInYourself,
			bool blendToYourself,
			AnimationDataType type
			)
		{
			clips = new List<AnimationClip>();
			this.clips.Add(clip);
			this.name = name + "_MM";
			this.frameRate = frameRate;
			this.isLooping = seemless;
			this.animationLength = length;
			this.frameTime = 1f / this.frameRate;
			this.blendToYourself = blendToYourself;
			this.findInYourself = findInYourself;
			this.dataType = type;

			sections = new List<DataSection>();

			sections.Add(new DataSection("Always"));
			sections[0].timeIntervals.Add(new float2(0f, this.animationLength));
			neverChecking = new DataSection("Never checking");
			notLookingForNewPose = new DataSection("Not looking for new pose");

			Curves = new List<MotionMatchingDataCurve>();
		}

		public void InitialSetup(
			AnimationClip[] clips,
			float[] weightsForClips,
			float frameRate,
			string name,
			bool seemless,
			float length,
			bool findInYourself,
			bool blendToYourself,
			AnimationDataType type
			)
		{
			// Basic
			this.clips = new List<AnimationClip>(clips);
			this.name = name + "_MM";
			this.frameRate = frameRate;
			this.isLooping = seemless;
			this.animationLength = length;
			this.frameTime = 1f / this.frameRate;
			this.blendToYourself = blendToYourself;
			this.findInYourself = findInYourself;
			// Blend tree
			this.blendTreeWeights = weightsForClips;
			this.dataType = type;

			// Sections
			sections = new List<DataSection>();
			sections.Add(new DataSection("Always"));
			sections[0].timeIntervals.Add(new float2(0f, this.animationLength));
			neverChecking = new DataSection("Never checking");
			notLookingForNewPose = new DataSection("Not looking for new pose");

		}

		public void InitialSetup(
			AnimationClip[] clips,
			Vector3[] animationSeqInfos,
			float frameRate,
			string name,
			bool seemless,
			float length,
			bool findInYourself,
			bool blendToYourself,
			AnimationDataType type
			)
		{
			this.clips = new List<AnimationClip>(clips);
			this.name = name + "_MM";
			this.frameRate = frameRate;
			this.isLooping = seemless;
			this.animationLength = length;
			this.frameTime = 1f / this.frameRate;
			this.blendToYourself = blendToYourself;
			this.findInYourself = findInYourself;
			this.dataType = type;

			sections = new List<DataSection>();
			sections.Add(new DataSection("Always"));
			sections[0].timeIntervals.Add(new float2(0f, this.animationLength));
			neverChecking = new DataSection("Never checking");
			notLookingForNewPose = new DataSection("Not looking for new pose");

		}

		public void UpdateFromOther(MotionMatchingData newData, string newName, bool overrideTrajectory)
		{
			this.name = newName;
			this.clips = newData.clips;
			this.frameRate = newData.frameRate;
			this.frameTime = newData.frameTime;
			this.animationLength = newData.animationLength;
			this.isLooping = newData.isLooping;
			this.dataType = newData.dataType;
			//this.endLocalPosition = newData.endLocalPosition;
			//this.deltaRotStartEnd = newData.deltaRotStartEnd;
			//this.startLocalPosition = newData.startLocalPosition;
			//this.deltaRotEndStart = newData.deltaRotEndStart;
			//this.curves = newData.curves;
			if (!overrideTrajectory && frames.Count == newData.frames.Count)
			{
				for (int i = 0; i < frames.Count; i++)
				{
					FrameData f = frames[i];
					FrameData newFrame = newData.frames[i];

					f.pose = newFrame.pose;
					f.localTime = newFrame.localTime;
					f.sections = newFrame.sections;
					f.contactPoints = newFrame.contactPoints;

					frames[i] = f;
				}
			}
			else
			{
				this.frames = newData.frames;
			}
			this.usedFrameCount = newData.numberOfFrames;
			this.blendTreeWeights = newData.blendTreeWeights;
			//this.sections = newData.sections;


			this.blendToYourself = newData.blendToYourself;
			findInYourself = newData.findInYourself;

			//this.neverChecking = newData.neverChecking;
			this.trajectoryPointsTimes = newData.trajectoryPointsTimes;

			this.BonesNames.Clear();
			for (int i = 0; i < newData.BonesNames.Count; i++)
			{
				this.BonesNames.Add(newData.BonesNames[i]);
			}

			for (int i = 0; i < frames.Count; i++)
			{
				FrameData fd = frames[i];
				fd.sections.sections = 0;
				frames[i] = fd;
			}

			for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
			{
				DataSection section = sections[sectionIndex];

				for (int i = 0; i < frames.Count; i++)
				{
					FrameData fd = frames[i];
					fd.sections.SetSection(sectionIndex, section.Contain(frames[i].localTime));
					frames[i] = fd;
				}
			}

		}

		public void AddFrame(FrameData frame)
		{
			frames.Add(frame);
		}

		public void RemoveFrame(int frameNumber)
		{
			if (frameNumber > frames.Count)
			{
				return;
			}
			frames.RemoveAt(frameNumber - 1);
		}

		public float GetLocalTime(float actuallTime, float animSpeedMulti = 1f)
		{
			float actuallAnimTime = actuallTime * animSpeedMulti;
			int timeLoops = Mathf.FloorToInt(actuallAnimTime / animationLength);
			return actuallAnimTime - timeLoops * animationLength;
		}

		#region Finding Trajector point in time based on animation curves
		/*
        public void CreateFutureLocalPositionFromStartToEnd()
        {
            Matrix4x4 m;
            Vector3 startPos = GetStartPositionXY();
            Quaternion startRot = GetStartRotationY();
            Vector3 endPos = GetEndPositionXY();
            Quaternion endRot = GetEndRotationY();
            deltaRotStartEnd = endRot * Quaternion.Inverse(startRot);
            m = Matrix4x4.TRS(startPos, startRot, Vector3.one);

            endLocalPosition = m.inverse.MultiplyPoint3x4(endPos);
        }

        public void CreatePastLocalPositionFromEndToSTart()
        {
            Matrix4x4 m;
            Vector3 startPos = GetStartPositionXY();
            Quaternion startRot = GetStartRotationY();
            Vector3 endPos = GetEndPositionXY();
            Quaternion endRot = GetEndRotationY();
            deltaRotEndStart = startRot * Quaternion.Inverse(endRot);
            m = Matrix4x4.TRS(endPos, endRot, Vector3.one);

            startLocalPosition = m.inverse.MultiplyPoint3x4(startPos);
        }

        public TrajectoryPoint GetFutureTrajectoryPoint(float futureTime, float currentTime, Transform root, float deltaTime, float animSpeedMulti = 1f)
        {
            if (curves.Count == 0)
            {
                throw new System.Exception("Array of curves is empty!");
            }
            float trajectoryPointFutureTime = futureTime;
            Vector3 fPos = Vector3.zero;
            Quaternion fRot = root.rotation;

            float actuallAnimTime = currentTime * animSpeedMulti;
            //float frameTime = 1f / frameRate;

            // obliczenie czasu lokalnego nie wiekszego niz dlugosc animacji
            int timeLoops = Mathf.FloorToInt(actuallAnimTime / length);
            float localTime = actuallAnimTime - timeLoops * length;

            // potrzebne macierze:
            Matrix4x4 m = new Matrix4x4();    // macierz transformacji pomiędzy pozycjami i rotacjiami z krzywych animacji
            Matrix4x4 fm = new Matrix4x4();   // macierz transformacji przyszłej pozycji (wykorzystuemy tu fPos i fRot)
                                              // ptrzebne wektory;
            Vector3 actualPos; // pozycja pobrana z krzywych 
            Vector3 futurePos; // pozycja pobrana z krzywych 
            Vector3 localFuturePos; // przyszła pozycja lokalna wzgledem actualPos i actualrot
                                    // potrzebne quaterniony
            Quaternion actualRot; // rotacja pobrana z krzywych
            Quaternion futureRot; // rotacja pobrana z krzywych
            Quaternion deltaRotation; // delta pomiedzy actualRot i futureRot

            float ft = trajectoryPointFutureTime + localTime;
            if (ft > length)
            {
                // pobranie pozycji i rotacji z krzywych animacji w local time
                actualPos = GetPositionInTimeXY(localTime);
                actualRot = GetRotationInTimeY(localTime);
                // stworzenie macierzy transformacji
                m.SetTRS(actualPos, actualRot, Vector3.one);
                // pobranie pozycji i rotacji z krzywych animacji na koncu animacji
                futurePos = GetEndPositionXY();
                futureRot = GetEndRotationY();
                // Obliczenie rozncy rotacji
                deltaRotation = futureRot * Quaternion.Inverse(actualRot);
                // Obliczenie przyszłej pozycji w local space
                localFuturePos = m.inverse.MultiplyPoint3x4(futurePos);
                // Obliczenie nowej rotacji
                fRot *= deltaRotation;
                // obliczenie nowej pozycji
                fPos = root.TransformPoint(localFuturePos);
                fm = Matrix4x4.TRS(fPos, fRot, Vector3.one);
                // obliczenie pozostałego czasu do sprawdzenia pozycji
                trajectoryPointFutureTime -= (length - localTime);
                // obliczenie ile razy futureTime przekracza długość animacji
                int FTLoops = Mathf.FloorToInt(trajectoryPointFutureTime / length);
                if (FTLoops > 0)
                {
                    for (int i = 0; i < FTLoops; i++)
                    {
                        fPos = fm.MultiplyPoint3x4(endLocalPosition);
                        fRot *= deltaRotStartEnd;
                        fm.SetTRS(fPos, fRot, Vector3.one);
                    }
                    trajectoryPointFutureTime -= (FTLoops * length);
                    if (trajectoryPointFutureTime > 0)
                    {
                        actualPos = GetStartPositionXY();
                        actualRot = GetStartRotationY();
                        futurePos = GetPositionInTimeXY(trajectoryPointFutureTime);
                        futureRot = GetRotationInTimeY(trajectoryPointFutureTime);
                        m.SetTRS(actualPos, actualRot, Vector3.one);
                        localFuturePos = m.inverse.MultiplyPoint3x4(futurePos);
                        deltaRotation = futureRot * Quaternion.Inverse(actualRot);
                        fRot *= deltaRotation;
                        fPos = fm.MultiplyPoint3x4(localFuturePos);
                    }
                }
                else
                {
                    if (trajectoryPointFutureTime > 0)
                    {
                        actualPos = GetStartPositionXY();
                        actualRot = GetStartRotationY();
                        futurePos = GetPositionInTimeXY(trajectoryPointFutureTime);
                        futureRot = GetRotationInTimeY(trajectoryPointFutureTime);
                        m = Matrix4x4.TRS(actualPos, actualRot, Vector3.one);
                        localFuturePos = m.inverse.MultiplyPoint3x4(futurePos);
                        deltaRotation = futureRot * Quaternion.Inverse(actualRot);
                        fRot *= deltaRotation;
                        fPos = fm.MultiplyPoint3x4(localFuturePos);
                    }
                }
            }
            else
            {
                actualPos = GetPositionInTimeXY(localTime);
                actualRot = GetRotationInTimeY(localTime);
                futurePos = GetPositionInTimeXY(ft);
                futureRot = GetRotationInTimeY(ft);
                m = Matrix4x4.TRS(actualPos, actualRot, Vector3.one);
                localFuturePos = m.inverse.MultiplyPoint3x4(futurePos);
                deltaRotation = futureRot * Quaternion.Inverse(actualRot);
                fRot *= deltaRotation;
                fPos = root.TransformPoint(localFuturePos);
            }

            fm.SetTRS(fPos, fRot, Vector3.one);

            Vector3 vel = fm.MultiplyVector(GetVelocityInTimeXY(localTime, deltaTime));

            return new TrajectoryPoint(fPos, vel, Vector3.zero, futureTime);
        }

        public TrajectoryPoint GetPastTrajectoryPoint(float pastTime, float currentTime, Transform root, float deltaTime, float animSpeedMulti = 1f)
        {
            if (curves.Count == 0)
            {
                throw new System.Exception("Array of curves is empty!");
            }

            float trajectoryPointPastTime = pastTime;
            Vector3 fPos = Vector3.zero;
            Quaternion fRot = root.rotation;

            float actuallAnimTime = currentTime * animSpeedMulti;
            //float frameTime = 1f / frameRate;

            // obliczenie czasu lokalnego nie wiekszego niz dlugosc animacji
            int timeLoops = Mathf.FloorToInt(actuallAnimTime / length);
            float localTime = actuallAnimTime - timeLoops * length;

            // potrzebne macierze:
            Matrix4x4 m = new Matrix4x4();    // macierz transformacji pomiędzy pozycjami i rotacjiami z krzywych animacji
            Matrix4x4 fm = new Matrix4x4();   // macierz transformacji przyszłej pozycji (wykorzystuemy tu fPos i fRot)
                                              // ptrzebne wektory;
            Vector3 actualPos; // pozycja pobrana z krzywych 
            Vector3 pastPos; // pozycja pobrana z krzywych 
            Vector3 localPastPos; // przyszła pozycja lokalna wzgledem actualPos i actualrot
                                  // potrzebne quaterniony
            Quaternion actualRot; // rotacja pobrana z krzywych
            Quaternion pastRot; // rotacja pobrana z krzywych
            Quaternion deltaRotation; // delta pomiedzy actualRot i futureRot

            float ft = trajectoryPointPastTime + localTime;
            if (ft < 0f)
            {
                // pobranie pozycji i rotacji z krzywych animacji w local time
                actualPos = GetPositionInTimeXY(localTime);
                actualRot = GetRotationInTimeY(localTime);
                // stworzenie macierzy transformacji
                m.SetTRS(actualPos, actualRot, Vector3.one);
                // pobranie pozycji i rotacji z krzywych animacji na poczatku animacji
                pastPos = GetStartPositionXY();
                pastRot = GetStartRotationY();
                // Obliczenie rozncy rotacji
                deltaRotation = pastRot * Quaternion.Inverse(actualRot);
                // Obliczenie przyszłej pozycji w local space
                localPastPos = m.inverse.MultiplyPoint3x4(pastPos);
                // Obliczenie nowej rotacji
                fRot *= deltaRotation;
                // obliczenie nowej pozycji
                fPos = root.TransformPoint(localPastPos);
                fm = Matrix4x4.TRS(fPos, fRot, Vector3.one);
                // obliczenie pozostałego czasu do sprawdzenia pozycji
                trajectoryPointPastTime += (localTime);
                // obliczenie ile razy futureTime przekracza długość animacji
                int FTLoops = Mathf.FloorToInt(Mathf.Abs(trajectoryPointPastTime) / length);
                if (FTLoops > 0)
                {
                    for (int i = 0; i < FTLoops; i++)
                    {
                        fPos = fm.MultiplyPoint3x4(startLocalPosition);
                        fRot *= deltaRotEndStart;
                        fm.SetTRS(fPos, fRot, Vector3.one);
                    }
                    trajectoryPointPastTime += (FTLoops * length);
                    if (trajectoryPointPastTime < 0)
                    {
                        actualPos = GetEndPositionXY();
                        actualRot = GetEndRotationY();
                        pastPos = GetPositionInTimeXY(trajectoryPointPastTime + length);
                        pastRot = GetRotationInTimeY(trajectoryPointPastTime + length);
                        m.SetTRS(actualPos, actualRot, Vector3.one);
                        localPastPos = m.inverse.MultiplyPoint3x4(pastPos);
                        deltaRotation = pastRot * Quaternion.Inverse(actualRot);
                        fRot *= deltaRotation;
                        fPos = fm.MultiplyPoint3x4(localPastPos);
                    }
                }
                else
                {
                    if (trajectoryPointPastTime < 0)
                    {
                        actualPos = GetEndPositionXY();
                        actualRot = GetEndRotationY();
                        pastPos = GetPositionInTimeXY(trajectoryPointPastTime + length);
                        pastRot = GetRotationInTimeY(trajectoryPointPastTime + length);
                        m = Matrix4x4.TRS(actualPos, actualRot, Vector3.one);
                        localPastPos = m.inverse.MultiplyPoint3x4(pastPos);
                        deltaRotation = pastRot * Quaternion.Inverse(actualRot);
                        fRot *= deltaRotation;
                        fPos = fm.MultiplyPoint3x4(localPastPos);
                    }
                }
            }
            else
            {
                actualPos = GetPositionInTimeXY(localTime);
                actualRot = GetRotationInTimeY(localTime);
                pastPos = GetPositionInTimeXY(ft);
                pastRot = GetRotationInTimeY(ft);
                m = Matrix4x4.TRS(actualPos, actualRot, Vector3.one);
                localPastPos = m.inverse.MultiplyPoint3x4(pastPos);
                deltaRotation = pastRot * Quaternion.Inverse(actualRot);
                fRot *= deltaRotation;
                fPos = root.TransformPoint(localPastPos);
            }

            fm.SetTRS(fPos, fRot, Vector3.one);

            Vector3 vel = fm.MultiplyVector(GetVelocityInTimeXY(localTime, deltaTime));

            return new TrajectoryPoint(fPos, vel, Vector3.zero, pastTime);
        }
        
        private Vector3 GetPositionInTime(float time)
        {
            return new Vector3(curves[0].Evaluate(time),
                                curves[1].Evaluate(time),
                                curves[2].Evaluate(time));
        }

        private Vector3 GetStartPosition()
        {
            return new Vector3(curves[0].Evaluate(0f),
                                curves[1].Evaluate(0f),
                                curves[2].Evaluate(0f));
        }

        private Vector3 GetLastVPosition()
        {
            return new Vector3(curves[0].Evaluate(length),
                                curves[1].Evaluate(length),
                                curves[2].Evaluate(length));
        }

        private Quaternion GetRotationInTime(float time)
        {
            return new Quaternion(curves[3].Evaluate(time),
                                    curves[4].Evaluate(time),
                                    curves[5].Evaluate(time),
                                    curves[6].Evaluate(time)).normalized;
        }

        private Quaternion GetStartRotation()
        {
            return new Quaternion(curves[3].Evaluate(0f),
                                    curves[4].Evaluate(0f),
                                    curves[5].Evaluate(0f),
                                    curves[6].Evaluate(0f)).normalized;
        }

        private Quaternion GetEndRotation()
        {
            return new Quaternion(curves[3].Evaluate(length),
                                    curves[4].Evaluate(length),
                                    curves[5].Evaluate(length),
                                    curves[6].Evaluate(length)).normalized;
        }

        private Vector3 GetPositionInTimeXY(float time)
        {
            return new Vector3(curves[0].Evaluate(time),
                                0f,
                                curves[2].Evaluate(time));
        }

        private Vector3 GetStartPositionXY()
        {
            return new Vector3(curves[0].Evaluate(0f),
                                0f,
                                curves[2].Evaluate(0f));
        }

        private Vector3 GetEndPositionXY()
        {
            return new Vector3(curves[0].Evaluate(length),
                                0f,
                                curves[2].Evaluate(length));
        }

        private Quaternion GetRotationInTimeY(float time)
        {
            return new Quaternion(0f,
                                    curves[4].Evaluate(time),
                                    0f,
                                    curves[6].Evaluate(time)).normalized;
        }

        private Quaternion GetStartRotationY()
        {
            return new Quaternion(0f,
                                    curves[4].Evaluate(0f),
                                    0f,
                                    curves[6].Evaluate(0f)).normalized;
        }

        private Quaternion GetEndRotationY()
        {
            return new Quaternion(0f,
                                    curves[4].Evaluate(length),
                                    0f,
                                    curves[6].Evaluate(length)).normalized;
        }

        private Vector3 GetVelocityInTime(float time)
        {
            Vector3 vel;
            if (time < Time.deltaTime)
            {
                vel = (GetPositionInTime(Time.deltaTime) - GetStartPosition()) / Time.deltaTime;
                return vel;
            }
            vel = (GetPositionInTime(time) - GetPositionInTime(time - Time.deltaTime)) / Time.deltaTime;
            return vel;
        }

        public Vector3 GetVelocityInTimeXY(float time, float deltaTime)
        {
            float localTime = GetLocalTime(time);
            Vector3 vel;
            Vector3 actual;
            Vector3 previu;
            Quaternion actualRot;
            if (time < deltaTime)
            {
                actual = GetPositionInTimeXY(deltaTime);
                previu = GetStartPositionXY();
                actualRot = GetRotationInTimeY(deltaTime);
            }
            else
            {
                actual = GetPositionInTimeXY(localTime);
                previu = GetPositionInTimeXY(localTime - deltaTime);
                actualRot = GetRotationInTimeY(localTime);
            }
            vel = (actual - previu) / deltaTime;

            Matrix4x4 m = Matrix4x4.TRS(actual, actualRot, Vector3.one);
            return m.inverse.MultiplyVector(vel);
        }
        */
		#endregion

		public void GetPoseInTime(ref PoseData buffor, float actuallTime, float animSpeedMulti = 1f)
		{
			float localTime = this.GetLocalTime(actuallTime) * animSpeedMulti;
			//float frameTime = 1f / frameRate;
			//int timeLoops = Mathf.FloorToInt(actuallAnimTime / length);
			//float localTime = actuallAnimTime - timeLoops * length;

			int backFrame = Mathf.FloorToInt(localTime / frameTime);
			int nextFrame;
			if (backFrame == frames.Count - 1)
			{
				if (isLooping)
				{
					nextFrame = 0;
				}
				else
				{
					nextFrame = backFrame;
				}
			}
			else
			{
				nextFrame = backFrame + 1;
			}

			float lerpFactor = (localTime - backFrame * frameTime) / frameTime;

			FrameData.GetLerpedPose(ref buffor, this.frames[backFrame], this.frames[nextFrame], lerpFactor);
		}

		public void GetTrajectoryInTime(ref Trajectory buffor, float actuallTime, float animSpeedMulti = 1f)
		{
			float localTime = this.GetLocalTime(actuallTime) * animSpeedMulti;

			int backFrame = Mathf.FloorToInt(localTime / frameTime);
			int nextFrame = backFrame == (frames.Count - 1) ? 0 : backFrame + 1;
			float lerpFactor = (localTime - backFrame * frameTime) / frameTime;
			FrameData.GetLerpedTrajectory(ref buffor, this.frames[backFrame], this.frames[nextFrame], lerpFactor);
		}

		public FrameData GetClossestFrame(float time)
		{
			float wantedFrameTime = Mathf.Clamp(time, 0f, this.animationLength);

			int frameIndex = Mathf.FloorToInt(wantedFrameTime / frameTime);

			frameIndex = Mathf.Clamp(frameIndex, 0, numberOfFrames - 1);
			return this[frameIndex];
		}

		#region Section
		public DataSection GetSection(int index)
		{
			return sections[index];
		}

		public bool CanLookingForNewPose(float localTime)
		{
			return !notLookingForNewPose.Contain(localTime);
		}

		public bool CanUseFrame(int frameIndex)
		{
			return !this.neverChecking.Contain(this[frameIndex].localTime);
		}

		public void SetSectionInterval(int sectionIndex, int intervalIndex, float2 interval)
		{
			if (sectionIndex < 0 || sectionIndex >= sections.Count)
			{
				Debug.LogWarning(string.Format("Wrong section index. {0} MotionMatching data have only {1} sections!", this.name, sections.Count));
				return;
			}

			if (intervalIndex < 0 || intervalIndex >= sections[sectionIndex].timeIntervals.Count)
			{
				Debug.LogWarning(string.Format("Wrong interval index. Section {0}. of {1} MotionMatching data have only {2} intervals!", this.name, sections.Count, sections[sectionIndex].timeIntervals.Count));
			}

			sections[sectionIndex].SetTimeInterval(intervalIndex, interval);

			for (int i = 0; i < frames.Count; i++)
			{
				bool result = frames[i].localTime >= interval.x && frames[i].localTime <= interval.y;
				frames[i].sections.SetSection(sectionIndex, result);
			}
		}

		public void AddSectionInterval(int sectionIndex, int intervalIndex, float2 interval)
		{
			sections[sectionIndex].timeIntervals.Add(interval);
			this.SetSectionInterval(sectionIndex, intervalIndex, interval);
		}

		public void ClearSections()
		{
			while (sections.Count > 1)
			{
				sections.RemoveAt(1);
			}

			for (int i = 0; i < frames.Count; i++)
			{
				FrameData frameData = frames[i];
				frameData.sections.SetSection(0, sections[0].Contain(frameData.localTime));
				frames[i] = frameData;
			}
		}
		#endregion

		#region Contact points
		public void GetContactPoints(
			ref List<FrameContact> points,
			float currentLocalTime,
			float animSpeedMulti = 1f
			)
		{
			int backFrame = Mathf.FloorToInt(currentLocalTime / frameTime);
			int nextFrame = backFrame == (frames.Count - 1) ? frames.Count - 1 : backFrame + 1;
			float lerpFactor = (currentLocalTime - backFrame * frameTime) / frameTime;

			points.Clear();

			for (int i = 0; i < this[backFrame].contactPoints.Length; i++)
			{
				points.Add(FrameContact.Lerp(
					this[backFrame].contactPoints[i],
					this[nextFrame].contactPoints[i],
					lerpFactor
					));
			}
		}

		public FrameContact GetContactPointInTime(int index, float currentLocalTime, float animSpeedMulti = 1f)
		{
			int backFrame = Mathf.FloorToInt(currentLocalTime / frameTime);
			int nextFrame = backFrame == (frames.Count - 1) ? frames.Count - 1 : backFrame + 1;
			float lerpFactor = (currentLocalTime - backFrame * frameTime) / frameTime;

			return FrameContact.Lerp(
					this[backFrame].contactPoints[index],
					this[nextFrame].contactPoints[index],
					lerpFactor
					);
		}

		public float GetContactStartTime(int contactIndex)
		{
			return contactPoints[contactIndex].startTime;
		}

		public float GetContactEndTime(int contactIndex)
		{
			return contactPoints[contactIndex].endTime;
		}

		public void SortContacts()
		{
			if (contactPoints != null && contactPoints.Count > 0)
			{
				contactPoints.Sort();
			}
		}
		#endregion

		#region Animation events
		public void AddAnimationEvent(MotionMatchingAnimationEvent animationEvent)
		{
			if (AnimationEvents == null)
			{
				AnimationEvents = new List<MotionMatchingAnimationEvent>();
			}

			AnimationEvents.Add(animationEvent);
		}
		#endregion

#if UNITY_EDITOR
		[SerializeField]
		public bool sectionFold = false;
		[SerializeField]
		public bool basicOptionsFold = false;
		[SerializeField]
		public bool additionalOptionsFold = false;
		[SerializeField]
		public bool contactPointsFold = false;

		public void ValidateData()
		{
			ValidateSections();
		}

		private void ValidateSections()
		{
			for (int i = 0; i < frames.Count; i++)
			{
				FrameData fd = frames[i];
				fd.sections.sections = 0;
				frames[i] = fd;
			}

			for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
			{
				DataSection section = sections[sectionIndex];

				for (int i = 0; i < frames.Count; i++)
				{
					FrameData fd = frames[i];
					fd.sections.SetSection(sectionIndex, section.Contain(frames[i].localTime));
					frames[i] = fd;
				}
			}

			EditorUtility.SetDirty(this);
		}

		public static void CopyDataSectionTimeIntervals(MotionMatchingData from, MotionMatchingData to, int sectionIndex)
		{

		}

		public static void CopyNeverCheckingTimeIntervals(MotionMatchingData from, MotionMatchingData to)
		{

		}

		public static void CopyNeverLookingForNewPoseTimeIntervals(MotionMatchingData from, MotionMatchingData to)
		{
			if (!from || !to)
			{
				Debug.LogWarning("Cannont copy NeverLookingForNewPose time intervals from or to null MotionMatchingData!");
				return;
			}

			to.notLookingForNewPose = from.notLookingForNewPose;

			EditorUtility.SetDirty(to);
		}
#endif
	}
}
