using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using MotionMatching.Gameplay.Jobs;
using Unity.Jobs;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using MotionMatching.Tools;
#endif

namespace MotionMatching.Gameplay
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Animator))]
	public sealed class MotionMatchingComponent : MonoBehaviour
	{
		//[Header("Needed components:")]
		[SerializeField]
		[Tooltip("If not setted, gets own GameObject.")]
		private GameObject trajectoryCreatorOwner;
		[SerializeField]
		private GameObject rootMotionOverrideGameObject;
		IMotionMatchingRootMotionOverride rootMotionOverride;

		[Space]
		[SerializeField]
		private bool initWithOwnPlayableGraph = true;

		private bool haveOwnGraph = false;
		[Space]

		//[Header("Animator controllers:")]
		[SerializeField]
		public MotionMatchingAnimator_SO motionMatchingController;

		[Space]

		//[Header("Custom Playables Support")]
		[SerializeField]
		private List<CustomPlayablesSupport> customPlayables;

		//[Header("Mecanim:")]
		[Space]
		[SerializeField]
		private MecanimAnimatorControllerConnectionPlace mecanimConnectionPlace = MecanimAnimatorControllerConnectionPlace.OnEnd;
		[SerializeField]
		private RuntimeAnimatorController mecanimAnimator;
		[SerializeField]
		private AvatarMask mecanimAvatarMask;
		[SerializeField]
		private bool isAnimatorLayerAdditive = false;

		public ref AnimatorControllerPlayable MecanimPlayable
		{
			get
			{
				return ref animationSystem.mecanimPlayable;
			}
		}

		IEnumerator mecanimChangingWeightCoroutine;

		[Space]

		//[Header("Trajectory correction:")]
		[SerializeField]
		private TrajectoryCorrectionSettings trajectoryCorrection;


		// Events
		/// <summary>
		/// First parameter from state
		/// Second parameter to state
		/// </summary>
		private UnityEvent<LogicState, LogicState> m_SwitchStateEvent;
		public UnityEvent<LogicState, LogicState> SwitchStateEvent
		{
			get
			{
				if (m_SwitchStateEvent == null)
				{
					m_SwitchStateEvent = new UnityEvent<LogicState, LogicState>();
				}
				return m_SwitchStateEvent;
			}
		}

		private float testedSquareSpeed;

		private bool shouldCorrectTrajectory = true;


		#region Animation events
		Dictionary<string, MotionMatchingComponentAnimationEvent> animationEvents;
		#endregion

		public Vector3 StrafeDirection { get; set; } = Vector3.forward;
		public int FirstIndexWithFutureTime { get; private set; } = 0;

		// Components
		private Animator animatorComponent;
		internal PlayableAnimationSystem animationSystem;

		// Logic Elements

		internal NativeArray<TrajectoryPoint> InputGlobalTrajectory;


		internal NativeArray<TrajectoryPoint> InputLocalTrajectory;
		internal NativeArray<TrajectoryPoint> AnimationTrajectory;
#if UNITY_EDITOR
		private NativeArray<TrajectoryPoint> nativeGizmosTrajectory;
#endif

		internal TransformTrajectoryToLocalSpaceJob TransformTrajectoryJob = new TransformTrajectoryToLocalSpaceJob();
		internal JobHandle TransformTrajectoryJobHandle;

		private List<LogicMotionMatchingLayer> logicLayers;

		private Dictionary<string, int> layerIndexes;


		private Vector3 m_Velocity = Vector3.zero;
		public Vector3 Velocity { get => m_Velocity; }

		public bool PerformStateBehaviorsCallbacks { get; set; } = true;

		internal float[] ConditionFloats = null;
		internal int[] ConditionInts = null;
		internal bool[] ConditionBools = null;
		internal StateSwitchTrigger[] ConditionTriggers = null;

		// private int[] settedTriggersIndexes = null;
		// private int nextIndexInSettedTriggersArray = 0;

		private List<int> settedTriggers;

#if UNITY_EDITOR
		public float[] ConditionFloatsEditorOnly { get => ConditionFloats; }
		public int[] ConditionIntsEditorOnly { get => ConditionInts; }
		public bool[] ConditionBoolsEditorOnly { get => ConditionBools; }
#endif

		#region events:
		/// <summary>
		/// Paramters
		///		First - Native motion group
		///		Second - MotionMatchingDataInfo representing animation
		///		Third - index on animation in NativeMotionGroup
		/// </summary>
		private OnPlayAnimationInMotionMatching m_OnPlayMotionMatchingAnimation;
		public OnPlayAnimationInMotionMatching OnPlayMotionMatchingAnimation
		{
			get
			{
				if (m_OnPlayMotionMatchingAnimation == null)
				{
					m_OnPlayMotionMatchingAnimation = new OnPlayAnimationInMotionMatching();
				}

				return m_OnPlayMotionMatchingAnimation;
			}
		}
		#endregion


		private float[] trajectoryPointsTimes;

		public int TrajectoryPointsCount
		{
			get
			{
				return trajectoryPointsTimes.Length;
			}
		}

		public PlayableAnimationSystem AnimationSystem { get => animationSystem; private set => animationSystem = value; }
		public bool ShouldCorrectTrajectory { get => shouldCorrectTrajectory; set => shouldCorrectTrajectory = value; }

		private bool isGraphStopped = false;

		//private DirectorUpdateMode timeUpdateMode = DirectorUpdateMode.GameTime;

		private ITrajectoryCreator trajectoryCreator;
		public ITrajectoryCreator TrajectoryCreator { get => trajectoryCreator; }

		Transform _Transform;

		bool applayVelOnAnimPlay = false;
		Vector3 positionBetwenAnimPlay;
		Vector3 velocityBeforePlayNewAnimation;

		internal void OnPlayNewAnim()
		{
			applayVelOnAnimPlay = true;
			positionBetwenAnimPlay = _Transform.position;
			velocityBeforePlayNewAnimation = animatorComponent.velocity;
		}


		private void Awake()
		{
			haveOwnGraph = initWithOwnPlayableGraph;

			_Transform = transform;

			shouldCorrectTrajectory = true;
			if (rootMotionOverrideGameObject == null) rootMotionOverrideGameObject = gameObject;
			rootMotionOverride = rootMotionOverrideGameObject.GetComponent<IMotionMatchingRootMotionOverride>();

			if (motionMatchingController == null)
			{
				throw new System.Exception(string.Format("MotionMatchingController in MotionMatching Component on GameObject {0} is null!", this.gameObject.name));
			}


			FirstIndexWithFutureTime = -1;
			trajectoryPointsTimes = motionMatchingController.GetTrajectoryPointsTimes();
			InputLocalTrajectory = new NativeArray<TrajectoryPoint>(trajectoryPointsTimes.Length, Allocator.Persistent);
			InputGlobalTrajectory = new NativeArray<TrajectoryPoint>(trajectoryPointsTimes.Length, Allocator.Persistent);
			AnimationTrajectory = new NativeArray<TrajectoryPoint>(trajectoryPointsTimes.Length, Allocator.Persistent);

#if UNITY_EDITOR
			nativeGizmosTrajectory = new NativeArray<TrajectoryPoint>(trajectoryPointsTimes.Length, Allocator.Persistent);
#endif

			for (int i = 0; i < trajectoryPointsTimes.Length; i++)
			{
				if (FirstIndexWithFutureTime == -1 && trajectoryPointsTimes[i] > 0)
				{
					FirstIndexWithFutureTime = i;
					break;
				}
			}

			// Seting Trajectory owner
			if (trajectoryCreatorOwner == null)
			{
				trajectoryCreatorOwner = this.gameObject;
			}

			// Geting needed Components
			animatorComponent = GetComponent<Animator>();
			trajectoryCreator = trajectoryCreatorOwner.GetComponent<ITrajectoryCreator>();

			// Playable animation system Initialization

			animationSystem = new PlayableAnimationSystem(
				this.gameObject,
				string.Format("{0} - MotionMatching graph)", this.gameObject.name)
				);

			logicLayers = new List<LogicMotionMatchingLayer>();
			InitParamtersDictionary();
			InitializeLayerDictionary();
			InitializeAnimationEvents();

			if (haveOwnGraph)
			{
				animationSystem.SetupGraph();
				InitializeLogicGraph(_Transform);
				InitializeAnimationSystem();

				trajectoryCreator.InitializeTrajectoryCreator(this);

				animationSystem.StartPlaying();
			}

			PerformStateBehaviorsCallbacks = true;
		}

		void Start()
		{
#if UNITY_EDITOR
			if (motionMatchingController == null)
			{
				return;
			}
#endif

			testedSquareSpeed = trajectoryCorrection.MinSpeedToPerformCorrection * trajectoryCorrection.MinSpeedToPerformCorrection;
			animatorComponent.applyRootMotion = true;
			if (haveOwnGraph)
			{
				StartLogicGraph();
			}
		}

		private void OnEnable()
		{
			if (!isGraphStopped && haveOwnGraph)
			{
				animationSystem.ResumePlayableGraph();
			}
		}

		private void OnDisable()
		{
			if (!isGraphStopped && haveOwnGraph)
			{
				animationSystem.PausePlayableGraph();
			}
		}

		private void FixedUpdate()
		{
#if UNITY_EDITOR
			if (motionMatchingController == null)
			{
				return;
			}
#endif

			if (!isGraphStopped)
			{
				for (int i = 0; i < logicLayers.Count; i++)
				{
					logicLayers[i].FixedUpdate();
				}
			}
		}

		void Update()
		{
#if UNITY_EDITOR
			if (motionMatchingController == null)
			{
				return;
			}
#endif

			if (!isGraphStopped)
			{
				for (int i = 0; i < logicLayers.Count; i++)
				{
					logicLayers[i].Update();
				}

				animationSystem.Update(Time.deltaTime);
			}
		}

		private void OnAnimatorMove()
		{
			Quaternion rotation = animatorComponent.rootRotation;
			Vector3 position;

			if (applayVelOnAnimPlay)
			{
				applayVelOnAnimPlay = false;
				position = positionBetwenAnimPlay + velocityBeforePlayNewAnimation * Time.deltaTime;
				m_Velocity = velocityBeforePlayNewAnimation;
			}
			else
			{
				m_Velocity = animatorComponent.velocity;
				position = animatorComponent.rootPosition;
			}

			if (!isGraphStopped && shouldCorrectTrajectory)
			{
				if ((logicLayers[0].IsTrajectorryCorrectionEnabledInCurrentState() || trajectoryCorrection.ForceTrajectoryCorrection) &&
				GetCurrentStateType() != MotionMatchingStateType.ContactAnimationState)
				{
#if UNITY_EDITOR
					testedSquareSpeed = trajectoryCorrection.MinSpeedToPerformCorrection * trajectoryCorrection.MinSpeedToPerformCorrection;
#endif
					rotation = PerformTrajectoryCorrection(position, rotation);
				}
			}

			if (rootMotionOverride != null)
			{
				rootMotionOverride.PerformRootMotionMovement(position, rotation);
			}
			else
			{
				_Transform.SetPositionAndRotation(position, rotation);
			}
		}

		private void LateUpdate()
		{
#if UNITY_EDITOR
			if (motionMatchingController == null)
			{
				return;
			}
#endif

			if (!isGraphStopped)
			{
				for (int i = 0; i < logicLayers.Count; i++)
				{
					logicLayers[i].LateUpdate();
				}
				animationSystem.LateUpdate();
			}
			InvalidateTriggers();
		}

		public int GetCurrentStateIndex(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentDataState.Index;
		}

		public Playable InitForCustomPlayableGraph(ref PlayableGraph customGraph)
		{
			animationSystem.SetupWithDeliveredGraph(ref customGraph);
			InitializeLogicGraph(_Transform);
			InitializeAnimationSystem();

			trajectoryCreator.InitializeTrajectoryCreator(this);
			return animationSystem.PlayableToPlay;
		}

		private void OnDestroy()
		{
#if UNITY_EDITOR
			if (motionMatchingController == null)
			{
				return;
			}

			motionMatchingController.UnsubscribeMotionMatchingComponentFromMotionGroups(this);
#endif

			for (int i = 0; i < logicLayers.Count; i++)
			{
				logicLayers[i].OnDestroy();
			}

			if (haveOwnGraph)
			{
				animationSystem.Destory();
			}

			InputLocalTrajectory.Dispose();
			InputGlobalTrajectory.Dispose();
			AnimationTrajectory.Dispose();

#if UNITY_EDITOR
			nativeGizmosTrajectory.Dispose();
#endif
		}

		/// <summary>
		/// Force finding new best place only in Contact state with contactStateType setetd to Impact.
		/// </summary>
		/// <param name="layerName"></param>
		/// <returns>Returns true when find forcing is successful happen.</returns>
		//		public bool ForceAnimationFinding(string layerName)
		//		{
		//#if UNITY_EDITOR
		//			if (!layerIndexes.ContainsKey(layerName))
		//			{
		//				throw new System.Exception(string.Format("Layer with name {0} not exist!", layerName));
		//			}
		//#endif
		//			throw new System.Exception("Not implemented");
		//		}

		/// <summary>
		/// Force finding new best place only in Contact state with contactStateType setetd to Impact.
		/// </summary>
		/// <param name="layerName"></param>
		/// <returns>Returns true when find forcing is successful happen.</returns>
		//public bool ForceAnimationFinding(int layerIndex)
		//{
		//	throw new System.Exception("Not implemented");
		//}

		/// <summary>
		/// Force finding new best place only in Contact state with contactStateType setetd to Impact.
		/// </summary>
		/// <param name="layerName"></param>
		/// <returns>Returns true when find forcing is successful happen.</returns>
		//public void ForceAnimationFinding()
		//{
		//	throw new System.Exception("Not implemented");
		//}

		public void PausePlayableGraph()
		{
			if (animationSystem != null)
			{
				if (haveOwnGraph) animationSystem.PausePlayableGraph();
				isGraphStopped = true;
			}
		}

		public void ResumePlayableGraph()
		{
			if (animationSystem != null)
			{
				if (haveOwnGraph) animationSystem.ResumePlayableGraph();
				isGraphStopped = false;
			}
		}

		public void WaitForJobsEnd()
		{
			TransformTrajectoryJobHandle.Complete();

			logicLayers[0].WaitForJobsEnd();
		}

		public bool SetFindingNextAnimationActive(bool isFindingActive, int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.SetFindingNextAnimationActive(isFindingActive);
		}

		#region State Switching

		public bool SwitchToSingleAnimationState(
			string stateName,
			float blendTime,
			string searchedSectionName = null,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif
			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
#if UNITY_EDITOR

				if (!dataState.MotionData.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{dataState.MotionData.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
					return false;
				}

#else
				if (!dataState.MotionData.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					return false;
				}

				// sectionIndex = dataState.MotionData.SectionIndexes[searchedSectionName];
#endif
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToSingleAnimationState(
			int stateID,
			float blendTime,
			string searchedSectionName = null,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
#if UNITY_EDITOR
				if (!dataState.MotionData.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{dataState.MotionData.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
					return false;
				}
#else
				if (!dataState.MotionData.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					return false;
				}

				// sectionIndex = dataState.MotionData.SectionIndexes[searchedSectionName];
#endif
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToSingleAnimationState_WithIDs(
			int stateID,
			float blendTime,
			int sectionIndex = 0,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif
			if (sectionIndex > 0)
			{
#if UNITY_EDITOR

				if (dataState.MotionData.SectionIndexes.Count <= sectionIndex)
				{
					Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{dataState.MotionData.name}\" section can have max index of {dataState.MotionData.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
					return false;
				}

#else
				if (dataState.MotionData.SectionIndexes.Count <= sectionIndex)
				{
					return false;
				}
#endif
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}


		public bool SwitchToSingleAnimationState_WithIDs(
			string stateName,
			float blendTime,
			int sectionIndex = 0,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif
			if (sectionIndex > 0)
			{
#if UNITY_EDITOR

				if (dataState.MotionData.SectionIndexes.Count <= sectionIndex)
				{
					Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{dataState.MotionData.name}\" section can have max index of {dataState.MotionData.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
					return false;
				}

#else
				if (dataState.MotionData.SectionIndexes.Count <= sectionIndex)
				{
					return false;
				}
#endif
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToMotionMatchingState(
			string stateName,
			float blendTime,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			//LogicMotionMatchingLayer logicLayer = logicLayers[layerIndex];


			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

			if (dataState.StateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToMotionMatchingState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				false,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToMotionMatchingState(
			int stateID,
			float blendTime,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			LogicMotionMatchingLayer logicLayer = logicLayers[layerIndex];

			int stateIndex = stateID;
			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

			if (dataState.StateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToMotionMatchingState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
#if UNITY_EDITOR
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
					return false;
				}
#else
				if (!dataState.MotionData.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
					return false;
				}
#endif
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				false,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToMotionMatchingState_WithIDs(
			int stateID,
			float blendTime,
			int sectionIndex = 0,
			string groupName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			LogicMotionMatchingLayer logicLayer = logicLayers[layerIndex];

			int stateIndex = stateID;
			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

			if (dataState.StateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToMotionMatchingState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}

			NativeMotionGroup motionGroup = dataState.MotionData;


			if (motionGroup.SectionIndexes.Count <= sectionIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{motionGroup.name}\" section can have max index of {motionGroup.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
#endif
				return false;
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				false,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToContactState(
			string stateName,
			float blendTime,
			List<SwitchStateContact> contactPoints,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}
			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactState(
			string stateName,
			float blendTime,
			SwitchStateContact[] contactPoints,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(ref contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactState(
			int stateID,
			float blendTime,
			List<SwitchStateContact> contactPoints,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactState(
			int stateID,
			float blendTime,
			SwitchStateContact[] contactPoints,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(ref contactPoints);
			}

			return switchStateInfoResult;
		}


		public bool SwitchToContactState_WithIDs(
			int stateID,
			float blendTime,
			List<SwitchStateContact> contactPoints,
			int sectionIndex = 0,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;


			if (motionGroup.SectionIndexes.Count <= sectionIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{motionGroup.name}\" section can have max index of {motionGroup.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
#endif
				return false;
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactState_WithIDs(
			int stateID,
			float blendTime,
			SwitchStateContact[] contactPoints,
			int sectionIndex = -1,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;


			if (motionGroup.SectionIndexes.Count <= sectionIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{motionGroup.name}\" section can have max index of {motionGroup.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
#endif
				return false;
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(ref contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToImpactState(
			string stateName,
			float blendTime,
			SwitchStateImpact impact,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;


			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}
			else
			{
				LogicImpactState impactState = logicState as LogicImpactState;
				if (impactState == null)
				{
#if UNITY_EDITOR
					Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to Contact state with contact state type \"Normal contacts\"!");
#endif
					return false;
				}
			}

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToImpactState(
			string stateName,
			float blendTime,
			SwitchStateImpact impact,
			int sectionIndex = 0,
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;


			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}
			else
			{
				LogicImpactState impactState = logicState as LogicImpactState;
				if (impactState == null)
				{
#if UNITY_EDITOR
					Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to Contact state with contact state type \"Normal contacts\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToImpactState(
			int stateID,
			float blendTime,
			SwitchStateImpact impact,
			string searchedSectionName = null,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;


			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}
			else
			{
				LogicImpactState impactState = logicState as LogicImpactState;
				if (impactState == null)
				{
#if UNITY_EDITOR
					Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to Contact state with contact state type \"Normal contacts\"!");
#endif
					return false;
				}
			}

			NativeMotionGroup motionGroup = dataState.MotionData;

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				if (!motionGroup.SectionIndexes.TryGetValue(searchedSectionName, out sectionIndex))
				{
#if UNITY_EDITOR
					Debug.LogError($"In motion matching controller {motionMatchingController.name} in state \"{dataState.Name}\" in motion group \"{motionGroup.name}\" section dependencies have no section with name \"{searchedSectionName}\"!");
#endif
					return false;
				}
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToImpactState_WithIDs(
			int stateID,
			float blendTime,
			SwitchStateImpact impact,
			int sectionIndex = -1,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;


			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}
			else
			{
				LogicImpactState impactState = logicState as LogicImpactState;
				if (impactState == null)
				{
#if UNITY_EDITOR
					Debug.LogError($"Method \"SwitchToImpactState\" from MotionMatching component cannot switch to Contact state with contact state type \"Normal contacts\"!");
#endif
					return false;
				}
			}

			NativeMotionGroup motionGroup = dataState.MotionData;

			if (motionGroup.SectionIndexes.Count <= sectionIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In state \"{dataState.Name}\" in motion matching controller {motionMatchingController.name} in motion group \"{motionGroup.name}\" section can have max index of {motionGroup.SectionIndexes.Count - 1}, desired index is {sectionIndex}!");
#endif
				return false;
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}


		public SwitchStateInfo GetNextStateInfo(int layerIndex = 0)
		{
			return logicLayers[layerIndex].StateSwitchInfo;
		}

		public bool PerformFindingInSingleAnimationState(
			string stateName,
			float blendTime,
			string searchedSectionName = null
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (!logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				sectionIndex = motionMatchingController.Layers[layerIndex].States[stateIndex].MotionData.SectionIndexes[searchedSectionName];
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}

		public bool PerformFindingInSingleAnimationState(
			int stateID,
			float blendTime,
			string searchedSectionName = null
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				sectionIndex = motionMatchingController.Layers[layerIndex].States[stateIndex].MotionData.SectionIndexes[searchedSectionName];
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}

		public bool PerformFindingInSingleAnimationState_WithIDs(
			int stateID,
			float blendTime,
			int sectionID = 0
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			int sectionIndex = sectionID;

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}


		public bool PerformFindingInSingleAnimationState_WithIDs(
			string stateName,
			float blendTime,
			int sectionID = 0
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			int sectionIndex = sectionID;

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}

		public bool PerformFindingInImpactState(
			string stateName,
			float blendTime,
			SwitchStateImpact impact,
			string searchedSectionName = null
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}


			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				sectionIndex = motionMatchingController.Layers[layerIndex].States[stateIndex].MotionData.SectionIndexes[searchedSectionName];
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}

		public bool PerformFindingInImpactState(
			string stateName,
			float blendTime,
			SwitchStateImpact impact,
			int sectionIndex
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}


			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}

		public bool PerformFindingInImpactState(
			int stateID,
			float blendTime,
			SwitchStateImpact impact,
			string searchedSectionName = null
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}


			int sectionIndex = 0;
			if (searchedSectionName != null)
			{
				sectionIndex = motionMatchingController.Layers[layerIndex].States[stateIndex].MotionData.SectionIndexes[searchedSectionName];
			}

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}

		public bool PerformFindingInImpactState_WithIDs(
			int stateID,
			float blendTime,
			SwitchStateImpact impact,
			int sectionID = 0
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}


			int sectionIndex = sectionID;

			int sectionMask = 1 << sectionIndex;
			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}

		#endregion

		#region switching states with section mask
		public bool SwitchToSingleAnimationStateWithSectionMask(
			string stateName,
			float blendTime,
			int sectionMask,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}


			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToSingleAnimationStateWithSectionMask(
			int stateID,
			float blendTime,
			int sectionMask,
			bool performFinding = true, // if true job with finding new pose will be started else first valid animation will be played
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.SingleAnimation)
			{
				Debug.LogError($"Method \"SwitchToSingleAnimationState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif



			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				performFinding,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}


		public bool SwitchToMotionMatchingStateWithSectionMask(
			string stateName,
			float blendTime,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;

			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

			if (dataState.StateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToMotionMatchingState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				false,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}

		public bool SwitchToMotionMatchingStateWithSectionMask(
			int stateID,
			float blendTime,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			LogicMotionMatchingLayer logicLayer = logicLayers[layerIndex];

			int stateIndex = stateID;
			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

			if (dataState.StateType != MotionMatchingStateType.MotionMatching)
			{
#if UNITY_EDITOR
				Debug.LogError($"Method \"SwitchToMotionMatchingState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
#endif
				return false;
			}

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				false,
				sectionMask,
				priority
				);

			return logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
		}


		public bool SwitchToContactStateWithSectionMask(
			string stateName,
			float blendTime,
			List<SwitchStateContact> contactPoints,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}


			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactStateWithSectionMask(
			string stateName,
			float blendTime,
			SwitchStateContact[] contactPoints,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			NativeMotionGroup motionGroup = dataState.MotionData;

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(ref contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactStateWithSectionMask(
			int stateID,
			float blendTime,
			List<SwitchStateContact> contactPoints,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(contactPoints);
			}

			return switchStateInfoResult;
		}

		public bool SwitchToContactStateWithSectionMask(
			int stateID,
			float blendTime,
			SwitchStateContact[] contactPoints,
			int sectionMask,
			uint priority = 2
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			LogicState logicState = logicLayers[layerIndex].logicStates[stateIndex];

			if (logicState.m_IsBlockedToEnter)
			{
				return false;
			}

			State_SO dataState = logicState.m_DataState;

#if UNITY_EDITOR
			if (dataState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				Debug.LogError($"Method \"SwitchToContactState\" from MotionMatching component cannot switch to states with type {dataState.StateType}!");
				return false;
			}
#endif

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				priority
				);

			bool switchStateInfoResult = logicLayers[layerIndex].SetSwitchStateInfo(switchInfo);
			if (switchStateInfoResult)
			{
				logicLayers[layerIndex].SetContactPoints(ref contactPoints);
			}

			return switchStateInfoResult;
		}

		#endregion

		#region perform next finding with section mask

		public bool PerformFindingInSingleAnimationStateWithSectionMask(
			string stateName,
			float blendTime,
			int sectionMask
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}


			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}

		public bool PerformFindingInSingleAnimationStateWithSectionMask(
			int stateID,
			float blendTime,
			int sectionMask
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);

			return logicLayers[layerIndex].OnReenterToState(switchInfo);
		}

		public bool PerformFindingInImpactStateWithSectionMask(
			string stateName,
			float blendTime,
			SwitchStateImpact impact,
			int sectionMask
			)
		{
			int layerIndex = 0;
			MotionMatchingLayer_SO dataLayer = motionMatchingController.Layers[layerIndex];

			int stateIndex;

			if (!dataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return false;
			}

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}

		public bool PerformFindingInImpactStateWithSectionMask(
			int stateID,
			float blendTime,
			SwitchStateImpact impact,
			int sectionMask
			)
		{
			int layerIndex = 0;
			int stateIndex = stateID;

			if (!logicLayers[layerIndex].logicStates[stateIndex].m_IsBlockedToEnter)
			{
				return false;
			}

			SwitchStateInfo switchInfo = new SwitchStateInfo(
				stateIndex,
				sectionMask,
				blendTime,
				true,
				true,
				sectionMask,
				uint.MaxValue
				);


			logicLayers[layerIndex].SetImpact(impact);

			return logicLayers[layerIndex].OnReenterToState(switchInfo); ;
		}


		#endregion


		#region State Behavior
		public void AddBehaviorToState(string stateName, MotionMatchingStateBehavior behavior, int layerIndex = 0)
		{
			int stateIndex;

			if (!logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return;
			}


			logicLayers[layerIndex].logicStates[stateIndex].AddBehavior(behavior);
		}

		public void AddBehaviorToState(int stateID, MotionMatchingStateBehavior behavior, int layerIndex = 0)
		{
			if (stateID < 0 || stateID >= motionMatchingController.Layers[layerIndex].States.Count)
			{
				return;
			}

			logicLayers[layerIndex].logicStates[stateID].AddBehavior(behavior);
		}

		public List<LogicState> GetAllLogicStatesWithTag(MotionMatchingStateTag tag, int layerIndex = 0)
		{
			List<LogicState> list = new List<LogicState>();

			for (int i = 0; i < logicLayers[layerIndex].logicStates.Count; i++)
			{
				if (logicLayers[layerIndex].logicStates[i].m_DataState.Tag == tag)
				{
					list.Add(logicLayers[layerIndex].logicStates[i]);
				}
			}

			return list;
		}

		public void RemoveBehaviorFromState(string stateName, MotionMatchingStateBehavior behavior, int layerIndex = 0)
		{
			int stateIndex;

			if (!logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return;
			}


			logicLayers[layerIndex].logicStates[stateIndex].RemoveBehavior(behavior);
		}

		public void RemoveBehaviorFromState(int stateID, MotionMatchingStateBehavior behavior, int layerIndex = 0)
		{
			if (stateID < 0 || stateID >= logicLayers[layerIndex].logicStates.Count)
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with id \"{stateID}\"!");
#endif
				return;
			}

			logicLayers[layerIndex].logicStates[stateID].RemoveBehavior(behavior);
		}

		public void ClearBehaviorInState(string stateName, MotionMatchingStateBehavior behavior, int layerIndex = 0)
		{
			int stateIndex;
			if (!logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return;
			}
			logicLayers[layerIndex].logicStates[stateIndex].ClearBehaviors();
		}

		#endregion

		#region Secondary animation playing

		public bool PlaySecondaryAnimation(
			BaseLayerAnimation overrideAnimation,
			int layerIndex,
			int priority = 0
			)
		{

			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
#endif
				return false;
			}

			return animationSystem.SecondaryLayers[layerIndex].SetAnimationToPlay(overrideAnimation, priority); ;
		}

		public bool PlaySecondaryAnimation(
			BaseLayerAnimation overrideAnimation,
			string layerName,
			int priority = 0
			)
		{
			PlayableSecondaryAnimationLayer layer;

			if (!animationSystem.SecondaryLayersDictionary.TryGetValue(layerName, out layer))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
#endif
				return false;
			}

			return layer.SetAnimationToPlay(overrideAnimation, priority);
		}

		public void SetSecondaryLayerWeight(int layerIndex, float weight, float changeTime)
		{
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
#endif
				return;
			}

			animationSystem.SecondaryLayers[layerIndex].SetLayerWeight(weight, changeTime);
		}

		public void SetSecondaryLayerWeight(string layerName, float weight, float changeTime)
		{
			PlayableSecondaryAnimationLayer layer;

			if (!animationSystem.SecondaryLayersDictionary.TryGetValue(layerName, out layer))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
#endif
				return;
			}

			layer.SetLayerWeight(weight, changeTime);

		}

		public void StopPlayingSecondaryAnimation(int layerIndex)
		{
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
#endif
				return;
			}
			animationSystem.SecondaryLayers[layerIndex].StopPlayingAnimation();
		}

		public void StopPlayingSecondaryAnimation(string layerName)
		{
			PlayableSecondaryAnimationLayer layer;

			if (!animationSystem.SecondaryLayersDictionary.TryGetValue(layerName, out layer))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
#endif
				return;
			}

			layer.StopPlayingAnimation();
		}

		public void StopPlayingSecondaryAnimationWithBlend(int layerIndex, float blendTime)
		{
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
#endif
				return;
			}
			animationSystem.SecondaryLayers[layerIndex].StopPlayingAnimationWithBlend(blendTime);
		}

		public void StopPlayingSecondaryAnimationWithBlend(string layerName, float blendTime)
		{
			PlayableSecondaryAnimationLayer layer;

			if (!animationSystem.SecondaryLayersDictionary.TryGetValue(layerName, out layer))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
#endif
				return;
			}

			layer.StopPlayingAnimationWithBlend(blendTime);
		}

		public int GetSecondaryLayerIndex(string layerName)
		{
			PlayableSecondaryAnimationLayer layer;

			if (!animationSystem.SecondaryLayersDictionary.TryGetValue(layerName, out layer))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
#endif
				return int.MaxValue;
			}

			return layer.Index;
		}

		public bool IsAnyAnimationPlayingOnSecondaryLayer(int layerIndex)
		{
#if UNITY_EDITOR
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
				return false;
			}
#endif
			return animationSystem.SecondaryLayers[layerIndex].IsAnyAnimationPlaying();
		}

		public bool IsAnyAnimationPlayingOnSecondaryLayer(string layerName)
		{
#if UNITY_EDITOR
			if (!animationSystem.SecondaryLayersDictionary.ContainsKey(layerName))
			{
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
				return false;
			}
#endif


			return animationSystem.SecondaryLayersDictionary[layerName].IsAnyAnimationPlaying();
		}

		public bool IsAnimationPlayingInSecondaryLayer(int layerIndex, BaseLayerAnimation animation)
		{
#if UNITY_EDITOR
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
				return false;
			}
#endif
			return animationSystem.SecondaryLayers[layerIndex].IsAnimationPlaying(animation);
		}

		public bool IsAnimationPlayingInSecondaryLayer(string layerName, BaseLayerAnimation animation)
		{
#if UNITY_EDITOR
			if (!animationSystem.SecondaryLayersDictionary.ContainsKey(layerName))
			{
				Debug.LogError($"In MottionMatchingComponent on game object {gameObject.name} not exist secondary layer with name {layerName}!");
				return false;
			}
#endif
			return animationSystem.SecondaryLayersDictionary[layerName].IsAnimationPlaying(animation);
		}

		public bool IsCurrentAnimationBlendingOutFromSecondaryLayer(int layerIndex)
		{
#if UNITY_EDITOR
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");
				return false;
			}
#endif

			return animationSystem.SecondaryLayers[layerIndex].IsCurrentAnimationBlendingOut();
		}


		public void IsAnimationBlendingOutFromSecondaryLayer(int layerIndex, BaseLayerAnimation animation, out bool isAnimationPlaying, out bool isAnimationBlendingOut)
		{
#if UNITY_EDITOR
			if (layerIndex < 0 || animationSystem.SecondaryLayers.Count <= layerIndex)
			{
				Debug.LogError($"In MotionMatchingComponent on game object {gameObject.name} exist only {animationSystem.SecondaryLayers.Count} secondary layers!");

				isAnimationPlaying = false;
				isAnimationBlendingOut = false;

				return;
			}
#endif

			animationSystem.SecondaryLayers[layerIndex].IsAnimationBlendingOut(animation, out isAnimationPlaying, out isAnimationBlendingOut);
		}

		public void StopPlayingAnySecondaryAnimation()
		{
			animationSystem.StopAllAnimationsInSecondaryLayers();
		}

		public bool StopAnimationIfIsPlaying(int layerIndex, BaseLayerAnimation animation, bool withBlend)
		{
			if (IsAnimationPlayingInSecondaryLayer(layerIndex, animation))
			{
				if (withBlend)
				{
					StopPlayingSecondaryAnimation(layerIndex);
				}
				else
				{
					StopPlayingSecondaryAnimationWithBlend(layerIndex, animation.EndBlendTime);
				}
				return true;
			}

			return false;
		}


		public bool StopAnimationIfIsPlaying(string layerName, BaseLayerAnimation animation, bool withBlend)
		{
			return StopAnimationIfIsPlaying(GetSecondaryLayerIndex(layerName), animation, withBlend);
		}
		#endregion

		public TrajectoryCorrectionSettings GetTrajectoryCorrectionSettings()
		{
			return trajectoryCorrection;
		}

		#region Animator mecanim
		public void SetMecanimAnimatorWeight(float weight)
		{
			if (mecanimChangingWeightCoroutine != null)
			{
				StopCoroutine(mecanimChangingWeightCoroutine);
				mecanimChangingWeightCoroutine = null;
			}

			animationSystem.SetMecanimControllerWeight(weight);
		}

		public void SetMecanimAnimatorWeight(float weight, float inTime)
		{
			if (!animationSystem.IsMecanimControllerAttached)
			{
				return;
			}

			if (mecanimChangingWeightCoroutine != null)
			{
				StopCoroutine(mecanimChangingWeightCoroutine);
				mecanimChangingWeightCoroutine = null;
			}

			if (inTime == 0)
			{
				animationSystem.SetMecanimControllerWeight(weight);
			}

			mecanimChangingWeightCoroutine = MeacnimChangingWeightCoroutineFunction(weight, inTime);
			StartCoroutine(mecanimChangingWeightCoroutine);
		}


		private IEnumerator MeacnimChangingWeightCoroutineFunction(float weight, float inTime)
		{
			float speed = Mathf.Abs(animationSystem.MecanimWeight - weight) / inTime;


			while (weight != animationSystem.MecanimWeight)
			{
				float newWeight = Mathf.MoveTowards(animationSystem.MecanimWeight, weight, speed * Time.deltaTime);
				animationSystem.SetMecanimControllerWeight(newWeight);
				yield return null;
			}
		}


		public void SetMecanimAvatarMask(AvatarMask avatarMask)
		{
			animationSystem.SetMecanimAvatarMask(avatarMask);
		}

		#endregion

		#region Getting states:

		public State_SO GetDataState(string name, int layerIndex = 0)
		{
			int index;
			if (motionMatchingController.Layers[layerIndex].StateIndexes.TryGetValue(name, out index))
			{
				return motionMatchingController.Layers[layerIndex].States[index];
			}

			return null;
		}

		public LogicState GetLogicState(string name, int layerIndex = 0)
		{
			int index;
			if (motionMatchingController.Layers[layerIndex].StateIndexes.TryGetValue(name, out index))
			{
				return logicLayers[layerIndex].logicStates[index];
			}

			return null;
		}

		#endregion

		#region Animator parameters

		public float GetFloat(string name)
		{
			return this.ConditionFloats[motionMatchingController.FloatsIndexes[name]];
		}

		public bool GetBool(string name)
		{
			return this.ConditionBools[motionMatchingController.BoolsIndexes[name]];
		}

		public int GetInt(string name)
		{
			return this.ConditionInts[motionMatchingController.IntsIndexes[name]];
		}

		public int GetFloatID(string name)
		{
			return motionMatchingController.FloatsIndexes[name];
		}

		public int GetBoolID(string name)
		{
			return motionMatchingController.BoolsIndexes[name];
		}

		public int GetIntID(string name)
		{
			return motionMatchingController.IntsIndexes[name];
		}

		public int GetTriggerID(string name)
		{
			return motionMatchingController.TriggersIndexes[name];
		}

		public void SetBool(string boolName, bool value)
		{
			this.ConditionBools[motionMatchingController.BoolsIndexes[boolName]] = value;
		}

		public void SetBool(int boolID, bool value)
		{
			this.ConditionBools[boolID] = value;
		}

		public void SetInt(string intName, int value)
		{
			this.ConditionInts[motionMatchingController.IntsIndexes[intName]] = value;
		}

		public void SetInt(int intID, int value)
		{
			ConditionInts[intID] = value;
		}

		public void SetFloat(int floatID, float value)
		{
			this.ConditionFloats[floatID] = value;
		}

		public void SetFloat(string floatName, float value)
		{
			this.ConditionFloats[motionMatchingController.FloatsIndexes[floatName]] = value;
		}

		public void SetTrigger(string triggerName)
		{
			int index = motionMatchingController.TriggersIndexes[triggerName];
			StateSwitchTrigger trigger = ConditionTriggers[index];
			if (!trigger.value)
			{
				trigger.value = true;
				ConditionTriggers[index] = trigger;
				settedTriggers.Add(index);
			}
		}

		public void SetTrigger(int triggerID)
		{
			StateSwitchTrigger trigger = ConditionTriggers[triggerID];
			if (!trigger.value)
			{
				trigger.value = true;
				ConditionTriggers[triggerID] = trigger;
				settedTriggers.Add(triggerID);
			}
		}

		internal void InvalidateTriggers()
		{
			if (settedTriggers != null && settedTriggers.Count > 0)
			{
				int count = settedTriggers.Count;

				for (int i = 0; i < count; i++)
				{
					int index = settedTriggers[i];
					StateSwitchTrigger trigger = ConditionTriggers[index];
					trigger.value = false;
					ConditionTriggers[index] = trigger;
				}

				settedTriggers.Clear();
			}
		}
		#endregion

		#region Geters
		public bool IsLookingForNewPose()
		{
			return logicLayers[0].CurrentLogicState.ShouldPerformMotionMatchingLooking();
		}

		public int GetPoseBoneCount()
		{
			return motionMatchingController.Layers[0].States[0].MotionData.NormalizedBonesWeights.Count;
		}

		public MotionMatchingStateTag GetCurrentStateTag()
		{
			return logicLayers[0].CurrentDataState.Tag;
		}

		public MotionMatchingStateTag GetStateTag(int stateIndex, int layerIndex = 0)
		{
			return logicLayers[layerIndex].logicStates[stateIndex].m_DataState.Tag;
		}

		public string[] GetStatesNamesInLayer()
		{
			int statesCount = motionMatchingController.Layers[0].States.Count;
			string[] statesNames = new string[statesCount];

			for (int i = 0; i < statesCount; i++)
			{
				statesNames[i] = motionMatchingController.Layers[0].States[i].Name;
			}

			return statesNames;
		}

		public bool CanEnterToState(string stateName, int layerIndex = 0)
		{
			int stateIndex;
			if (!logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
				return false;
			}


			return logicLayers[0].logicStates[stateIndex].m_IsBlockedToEnter;
		}

		public MotionMatchingStateType GetCurrentStateType()
		{
			return logicLayers[0].GetCurrentStateType();
		}

		public bool IsInOrWillSwitchToState(string stateName, int layerIndex = 0)
		{
			if (TryGetStateIndex(stateName, out int stateID))
			{
				return IsInOrWillSwitchToState(stateID, layerIndex);
			}

			return false;
		}

		public bool IsInOrWillSwitchToState(int stateID, int layerIndex = 0)
		{
			LogicMotionMatchingLayer layer = logicLayers[layerIndex];
			if (layer.CurrentLogicState.Index == stateID)
			{
				return true;
			}

			SwitchStateInfo switchStateInfo = layer.StateSwitchInfo;

			if (switchStateInfo.ShouldSwitch && switchStateInfo.NextStateIndex == stateID)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool IsInOrWillSwitchToState(out bool isInState, out bool willSwitchToState, string stateName, int layerIndex = 0)
		{
			if (TryGetStateIndex(stateName, out int stateID))
			{
				return IsInOrWillSwitchToState(out isInState, out willSwitchToState, stateID);
			}
			isInState = false;
			willSwitchToState = false;
			return false;
		}

		public bool IsInOrWillSwitchToState(out bool isInState, out bool willSwitchToState, int stateID, int layerIndex = 0)
		{
			LogicMotionMatchingLayer layer = logicLayers[layerIndex];
			if (layer.CurrentLogicState.Index == stateID)
			{
				isInState = true;
				willSwitchToState = false;
				return true;
			}

			isInState = false;

			SwitchStateInfo switchStateInfo = layer.StateSwitchInfo;

			if (switchStateInfo.ShouldSwitch && switchStateInfo.NextStateIndex == stateID)
			{
				willSwitchToState = true;
				return true;
			}
			else
			{
				willSwitchToState = false;
				return false;
			}
		}

		//public MotionMatchingStateType GetCurrentStateType(string layerName)
		//{
		//	throw new System.Exception("Not implemented");
		//}

		/// <summary>
		/// Get animation trajectory of  the most up-to-date played animation. Position, velocity and orientation are in local space of animated object transform.
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <returns></returns>
		//public Trajectory GetCurrentAnimationTrajectory(int layerIndex = 0)
		//{
		//	throw new System.Exception("Not implemented");
		//}

		/// <summary>
		/// Get animation trajectory of  the most up-to-date played animation. Position, velocity and orientation are in local space of animated object transform.
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <returns></returns>
		//private Trajectory GetCurrentAnimationTrajectory(string layerName)
		//{
		//	throw new System.Exception("Not implemented");
		//}

		//public Trajectory GetTrajectorySample(string layerName)
		//{
		//    return this.animatorController.layers[layerIndexes[layerName]].states[0].animationData[0][0].trajectory;
		//}

		//public PoseData GetCurrentPoseData(int layerIndex)
		//{
		//    return logicLayers[layerIndex].GetCurrentState().GetCurrentPose();
		//}

		//public PoseData GetCurrentPoseData(string layerName)
		//{
		//    return logicLayers[layerIndexes[layerName]].GetCurrentState().GetCurrentPose();
		//}

		private int GetFirstPointIndexWithFutureTime()
		{
			for (int i = 0; i < trajectoryPointsTimes.Length; i++)
			{
				if (trajectoryPointsTimes[i] >= 0f)
				{
					return i;
				}
			}

			return -1;
		}

		public float[] GetTrajectoryPointsTimes()
		{
			return motionMatchingController.GetTrajectoryPointsTimes();
		}

		public void GetWorldSpacePose(ref BoneData[] poseBuffor)
		{
			logicLayers[0].GetCurrentPose(ref poseBuffor);
		}

		public void GetWorldSpaceAnimationTrajectory(ref TrajectoryPoint[] trajectory)
		{
			NativeArray<TrajectoryPoint> bufforTrajectory = new NativeArray<TrajectoryPoint>(TrajectoryPointsCount, Allocator.Temp);

			int layerIndex = 0;
			logicLayers[layerIndex].SetTrajectoryFromMotionGroup(ref bufforTrajectory);

			for (int i = 0; i < bufforTrajectory.Length; i++)
			{
				TrajectoryPoint tp = bufforTrajectory[i] / logicLayers[layerIndex].CurrentLogicState.CurrentMotionGroup.TrajectoryCostWeight;
				tp.TransformToWorldSpace(_Transform);
				trajectory[i] = tp;
			}

			bufforTrajectory.Dispose();
		}

		public void GetWorldSpaceInputTrajectory(ref TrajectoryPoint[] trajectory)
		{
			for (int i = 0; i < InputGlobalTrajectory.Length; i++)
			{
				trajectory[i] = InputGlobalTrajectory[i];
			}
		}

		#region Sections
		public bool IsCurrentAnimationInSection(string sectionName, int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.IsInSection(sectionName);
		}

		public bool IsCurrentAnimationInSection(int sectionIndex, int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.IsInSection(sectionIndex);
		}

		public SectionAcces GetSectionAccesInCurrentAnimation(string sectionName, int layerIndex = 0)
		{
			NativeMotionGroup group = logicLayers[layerIndex].CurrentLogicState.CurrentMotionGroup;

			int sectionIndex;
			if (group.SectionIndexes.TryGetValue(sectionName, out sectionIndex))
			{

				return new SectionAcces(logicLayers[layerIndex].CurrentLogicState.CurrentClipInfo.Sections[sectionIndex]);
			}

			return new SectionAcces(null);
		}

		public SectionAcces GetSectionAccesInCurrentAnimation(int sectionIndex, int layerIndex = 0)
		{
			return new SectionAcces(logicLayers[layerIndex].CurrentLogicState.CurrentClipInfo.Sections[sectionIndex]);
		}
		#endregion
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public float GetCurrentClipLocalTime(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.m_CurrentClipLocalTime;
		}

		/// <summary>
		/// Can return time of animation from not current state, couse motion matching system waits for job to end, and after job complete current time will be updated.
		/// </summary>
		/// <returns>Global animation time from last played clip.</returns>
		public float GetCurrentClipGlobalTime(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.m_CurrentClipGlobalTime;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time">Time of last played animation will be assigned to this value.</param>
		/// <returns><see langword="false"/> if last plyed clip local time is not from current state, else <see langword="true"/>.</returns>
		public bool TryGetValidCurrentStateLocalClipTime(out float time, int layerIndex = 0)
		{
			time = logicLayers[layerIndex].CurrentLogicState.m_CurrentClipLocalTime;
			if (logicLayers[layerIndex].CurrentLogicState.m_IsPlayingCurrentStateAnimation)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time">Time of last played animation will be assigned to this value.</param>
		/// <returns><see langword="false"/> if last plyed clip local time is not from current state, else <see langword="true"/>.</returns>
		public bool TryGetValidCurrentStateGlobalClipTime(out float time, int layerIndex = 0)
		{
			time = logicLayers[layerIndex].CurrentLogicState.m_CurrentClipGlobalTime;
			if (logicLayers[layerIndex].CurrentLogicState.m_IsPlayingCurrentStateAnimation)
			{
				return true;
			}

			return false;
		}

		public bool IsPlayingCurrentStateAnimation(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.m_IsPlayingCurrentStateAnimation;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Reference to SectionDependencies object used in current state by current motion group. Can return null if SectionDepenedcies are not seted in motion group.</returns>
		public SectionsDependencies GetSectionsDependenciesInCurrentState(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState.CurrentMotionGroup.SectionsDependencies;
		}

		public bool TryGetStateIndex(string stateName, out int stateIndex, int layerIndex = 0)
		{
			if (logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
				return true;
			}

			stateIndex = -1;
			return false;
		}

		public int GetStateIndex(string stateName, int layerIndex = 0)
		{
			if (logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out int stateIndex))
			{
				return stateIndex;
			}

#if UNITY_EDITOR
			Debug.LogError($"In animator \"{motionMatchingController.name}\" not exist state with name \"{stateName}\"!", this);
#endif

			return -1;
		}

		public LogicState GetCurrentState(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentLogicState;
		}


		public ITrajectoryCreator GetTrajectoryCreator()
		{
			return trajectoryCreator;
		}
		#endregion

		#region Setters

		public void SetAnimationSpeedMultiplier(float speedMultiplier, int layerIndex = 0)
		{
			animationSystem.SetSpeed(speedMultiplier, layerIndex);
		}

		public void SetTrajectoryCorrectionSettings(TrajectoryCorrectionSettings settings)
		{
			trajectoryCorrection = settings;
			testedSquareSpeed = trajectoryCorrection.MinSpeedToPerformCorrection * trajectoryCorrection.MinSpeedToPerformCorrection;
		}

		public void SetTrajectoryCorrectionType(TrajectoryCorrectionType correctionType)
		{
			trajectoryCorrection.CorrectionType = correctionType;
		}

		#region changing state section mask

		/// <summary>
		/// Changes section mask to use only one section with index coresponding to section with <paramref name="sectionName"/>.
		/// </summary>
		/// <param name="sectionName"></param>
		public void SetCurrentStateSection(string sectionName)
		{
			logicLayers[0].CurrentLogicState.SetCurrentSectionIndex(sectionName);
		}

		/// <summary>
		/// Changes section mask to use only one section with index <paramref name="sectionIndex"/>.
		/// </summary>
		/// <param name="sectionName"></param>
		public void SetCurrentStateSection(int sectionIndex)
		{
			logicLayers[0].CurrentLogicState.SetCurrentSectionIndex(sectionIndex);
		}

		public void SetCurrentStateSectionMask(int sectionMask)
		{
			logicLayers[0].CurrentLogicState.SetCurrentSectionMask(sectionMask);
		}

		#endregion

		public void SetCurrentStateDefaultSectionMask()
		{
			logicLayers[0].CurrentLogicState.SetCurrentDefaultSectionMask();
		}

		public void SetLayerWeight(int layerIndex, float weight)
		{
			throw new System.Exception("Not implemented");
		}

		public void SetLayerWeight(string layerName, float weight)
		{
			throw new System.Exception("Not implemented");
		}

		//public bool SetContactPointPosition(string layerName, float3 position)
		//{
		//    int layerIndex = -1;

		//    for (int i = 0; i < logicLayers.Count; i++)
		//    {
		//        if (logicLayers[i].IsNameEqual(layerName))
		//        {
		//            layerIndex = i;
		//            break;
		//        }
		//    }

		//    if (layerIndex != -1)
		//    {
		//        return logicLayers[layerIndex].SetContactPointPosition(position);
		//    }
		//    else
		//    {
		//        return false;
		//    }
		//}

		public void SetLayerAdditive(uint layerIndex, bool isAdditive)
		{
			throw new System.Exception("Not implemented");
		}

		public void SetLayerAdditive(string layerName, bool isAdditive)
		{
			throw new System.Exception("Not implemented");
		}

		/// <summary>
		/// Sets past points(which times is lesser than 0) for forwarded trajectory. Setted position, velocity and orientation are in world space.
		/// </summary>
		/// <param name="trajectory"></param>
		/// <param name="layerIndex"></param>
		public void SetPastPointsFromData(int layerIndex = 0)
		{
			logicLayers[layerIndex].SetPastTrajectoryFromMotionGroup(ref AnimationTrajectory);

			trajectoryCreator.SetPastAnimationTrajectoryFromMotionMatchingComponent(
				ref AnimationTrajectory,
				FirstIndexWithFutureTime
				);
		}

		#endregion

		#region Component Initialization
		private void InitializeAnimationSystem()
		{
			animationSystem.Initialize(
				motionMatchingController != null ? motionMatchingController.SecondaryLayers : null,
				this,
				mecanimAnimator,
				mecanimAvatarMask,
				isAnimatorLayerAdditive,
				mecanimConnectionPlace
				);
			for (int i = 0; i < motionMatchingController.Layers.Count; i++)
			{
				animationSystem.SetLayerAdditive((uint)i, motionMatchingController.Layers[i].IsAdditive);
			}

			animationSystem.AttachCustomPlayables(customPlayables);

		}
		private void InitParamtersDictionary()
		{
			if (motionMatchingController.BoolParameters.Count > 0)
			{
				ConditionBools = new bool[motionMatchingController.BoolParameters.Count];

				for (int i = 0; i < ConditionBools.Length; i++)
				{
					ConditionBools[i] = motionMatchingController.BoolParameters[i].Value;
				}
			}

			if (motionMatchingController.IntParameters.Count > 0)
			{
				ConditionInts = new int[motionMatchingController.IntParameters.Count];

				for (int i = 0; i < ConditionInts.Length; i++)
				{
					ConditionInts[i] = motionMatchingController.IntParameters[i].Value;
				}
			}

			if (motionMatchingController.FloatParamaters.Count > 0)
			{
				ConditionFloats = new float[motionMatchingController.FloatParamaters.Count];

				for (int i = 0; i < ConditionFloats.Length; i++)
				{
					ConditionFloats[i] = motionMatchingController.FloatParamaters[i].Value;
				}
			}

			if (motionMatchingController.TriggersNames.Count > 0)
			{
				ConditionTriggers = new StateSwitchTrigger[motionMatchingController.TriggersNames.Count];
				for (int i = 0; i < motionMatchingController.TriggersNames.Count; i++)
				{
					ConditionTriggers[i] = new StateSwitchTrigger(false);
				}

				settedTriggers = new List<int>();
				settedTriggers.Capacity = ConditionTriggers.Length;
			}
		}
		private void InitializeLayerDictionary()
		{
			layerIndexes = new Dictionary<string, int>();
			for (int i = 0; i < motionMatchingController.Layers.Count; i++)
			{
				layerIndexes.Add(motionMatchingController.Layers[i].name, i);
			}
		}

		private void InitializeLogicGraph(Transform gameObjectTransform)
		{
			for (int layerIndex = 0; layerIndex < motionMatchingController.Layers.Count; layerIndex++)
			{
				animationSystem.AddLayer(new PlayableAnimationLayerData(motionMatchingController.Layers[layerIndex].AvatarMask));

				MotionMatchingLayer_SO layer = motionMatchingController.Layers[layerIndex];
				LogicMotionMatchingLayer logicLayer = layer.CreateLogicLayer(this, animationSystem);

				int maxJobsOutputCount = 0;
				for (int stateIndex = 0; stateIndex < layer.States.Count; stateIndex++)
				{
					State_SO state = layer.States[stateIndex];

#if UNITY_EDITOR
					if (state.MotionData == null)
					{
						Debug.LogError($"In MotionMatchingComponent on game object \"{gameObject.name}\" in animator \"{motionMatchingController.name}\" in state \"{state.Name}\" native motion group is null!", this);
					}
					else if (!state.MotionData.IsBinaryDataLoaded)
					{
						Debug.LogError($"In MotionMatchingComponent on game object \"{gameObject.name}\" in animator \"{motionMatchingController.name}\" in state \"{state.Name}\" binary data for native motion group \"{state.MotionData.name}\" is not loaded!", this);
					}
					else
#endif
					{
#if UNITY_EDITOR
						state.MotionData.SubscribeByMotionMatchingComponent(this);
#endif
						int maxGroupJobsOutputCount = state.MotionData.JobsCount;
						if (maxJobsOutputCount < maxGroupJobsOutputCount)
						{
							maxJobsOutputCount = maxGroupJobsOutputCount;
						}

						logicLayer.logicStates.Add(state.CreateLogicState(this, logicLayer, animationSystem));
					}

				}

				logicLayer.InitializeJobsOutputArray(maxJobsOutputCount);

#if UNITY_EDITOR
				//logicLayer.DoAndCompleteFirstJobs();
#endif

				logicLayers.Add(logicLayer);
			}
		}

		public void StartLogicGraph()
		{
			for (int layerIndex = 0; layerIndex < logicLayers.Count; layerIndex++)
			{
				logicLayers[layerIndex].BeginMotionMatchingStateMachine();
			}
		}

		private void InitializeAnimationEvents()
		{
			animationEvents = new Dictionary<string, MotionMatchingComponentAnimationEvent>();
		}
		#endregion

		internal void UpdataCurrentInputTrajectory(float motionGroupTrajectoryWeight = 1f)
		{
			if (trajectoryCreator == null)
			{
				return;
			}
			trajectoryCreator.GetTrajectoryToMotionMatchingComponent(ref InputGlobalTrajectory, InputGlobalTrajectory.Length);

			//for (int i = 0; i < InputGlobalTrajectory.Length; i++)
			//{
			//	TrajectoryPoint tp = InputGlobalTrajectory[i];
			//	tp.TransformToLocalSpace(this.transform);
			//	InputLocalTrajectory[i] = tp * motionGroupTrajectoryWeight;
			//}
			TransformTrajectoryJob.Rotation = _Transform.rotation;
			TransformTrajectoryJob.Position = _Transform.position;
			TransformTrajectoryJob.GlobalSpaceTrajectory = InputGlobalTrajectory;
			TransformTrajectoryJob.LocalSpaceTrajectory = InputLocalTrajectory;
			// This weight should be aquired in separate job per layer, for now this weight is aquired from layer 0
			// TODO: jesli kiedykolwiek beda implementowane warstwy tak aby mozna bylo w kilku naraz posiadać stany typu MotionMatching waga ta powinna byc ustawiana na kazda warstwe (dla kazdego stanu, ale raczej takie warstwy nie sa w planie) 

			NativeMotionGroup group = logicLayers[0].CurrentLogicState.CurrentMotionGroup;
			TransformTrajectoryJob.MotionGroupTrajectoryWeight = group.TrajectoryCostWeight;
			TransformTrajectoryJob.MotionGroupTrajectoryPositionWeight = group.TrajectoryPositionWeight;
			TransformTrajectoryJob.MotionGroupTrajectoryVelocityWeight = group.TrajectoryVelocityWeight;
			TransformTrajectoryJob.MotionGroupTrajectoryOrientationWeight = group.TrajectoryOrientationWeight;


			TransformTrajectoryJobHandle = TransformTrajectoryJob.ScheduleBatch(InputGlobalTrajectory.Length, InputGlobalTrajectory.Length);
		}

		public void SetAnimationTrajectoryToTrajectoryCreator()
		{
			int layerIndex = 0;
#if UNITY_EDITOR
			if (trajectoryCreator == null)
			{
				throw new System.Exception($"Motion Matching Component on game object \"{this.gameObject.name}\" have no setted Trajectory Creator!");
			}
#endif

			logicLayers[layerIndex].SetTrajectoryFromMotionGroup(ref AnimationTrajectory);
			trajectoryCreator.SetTrajectoryFromNativeContainer(ref AnimationTrajectory, _Transform);
		}

		#region Trajectory corrections method

		private Quaternion PerformTrajectoryCorrection(Vector3 position, Quaternion currentRotation)
		{
			Vector3 animatorVel = Velocity;
			animatorVel.y = 0f;
			if (animatorVel.sqrMagnitude < testedSquareSpeed)
			{
				return currentRotation;
			}

			switch (trajectoryCorrection.CorrectionType)
			{
				case TrajectoryCorrectionType.Constant:
					{
						//Vector3 desiredDirrection = trajectoryCreator.GetVelocity();
						Vector3 desiredDirrection;
						Vector3 currentDir;

						LogicState state = logicLayers[0].CurrentLogicState;
						NativeMotionGroup motionGroup = state.CurrentMotionGroup;

						if (state.m_IsPlayingCurrentStateAnimation)
						{
							Vector3 desiredPos = transform.TransformPoint(motionGroup.GetTrajectoryPointPosition(
								state.m_CurrentClipIndex,
								state.m_CurrentClipLocalTime,
								FirstIndexWithFutureTime
								));

							currentDir = desiredPos - position;
							desiredDirrection = (Vector3)trajectoryCreator.GetTrajectoryPoint(FirstIndexWithFutureTime).Position - position;
						}
						else
						{
							currentDir = animatorVel;
							desiredDirrection = trajectoryCreator.GetVelocity();
						}

						return ConstantTrajectoryCorrection(
								currentRotation,
								currentDir,
								desiredDirrection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MaxAngleSpeed
								);
					}
				case TrajectoryCorrectionType.Progresive:
					{
						//Vector3 desiredDirrection = trajectoryCreator.GetVelocity();
						Vector3 desiredDirrection;
						Vector3 currentDir;

						LogicState state = logicLayers[0].CurrentLogicState;
						NativeMotionGroup motionGroup = state.CurrentMotionGroup;

						if (state.m_IsPlayingCurrentStateAnimation)
						{
							Vector3 desiredPos = transform.TransformPoint(motionGroup.GetTrajectoryPointPosition(
								state.m_CurrentClipIndex,
								state.m_CurrentClipLocalTime,
								FirstIndexWithFutureTime
								));

							currentDir = desiredPos - position;
							desiredDirrection = (Vector3)trajectoryCreator.GetTrajectoryPoint(FirstIndexWithFutureTime).Position - position;
						}
						else
						{
							currentDir = animatorVel;
							desiredDirrection = trajectoryCreator.GetVelocity();
						}

						return ProgresiveTrajectoryCorrection(
								currentRotation,
								currentDir,
								desiredDirrection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MinAngleSpeed,
								trajectoryCorrection.MaxAngleSpeed
								);

						//ProgresiveTrajectoryCorrection(
						//	TrajectoryCorrection.MinAngleSpeed,
						//	TrajectoryCorrection.MaxAngleSpeed,
						//	TrajectoryCorrection.MinAngle,
						//	TrajectoryCorrection.MaxAngle
						//	);
					}
				case TrajectoryCorrectionType.MatchOrientationConstant:
					{
						Vector3 currentDirrection = _Transform.forward;
						Vector3 desiredDirrection = trajectoryCreator.GetTrajectoryPoint(FirstIndexWithFutureTime).Orientation;

						return ConstantTrajectoryCorrection(
								currentRotation,
								currentDirrection,
								desiredDirrection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MaxAngleSpeed
								);

						//OrientationConstantCorrection();
					}
				case TrajectoryCorrectionType.MatchOrientationProgresive:
					{
						Vector3 currentDirrection = _Transform.forward;
						Vector3 desiredDirrection = trajectoryCreator.GetTrajectoryPoint(FirstIndexWithFutureTime).Orientation;

						return ProgresiveTrajectoryCorrection(
								currentRotation,
								currentDirrection,
								desiredDirrection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MinAngleSpeed,
								trajectoryCorrection.MaxAngleSpeed
								);
					}
				case TrajectoryCorrectionType.StrafeConstant:
					{
						Vector3 currentDirrection = _Transform.forward;

						return ConstantTrajectoryCorrection(
								currentRotation,
								currentDirrection,
								StrafeDirection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MaxAngleSpeed
								);

					}
				case TrajectoryCorrectionType.StarfeProgresive:
					{
						Vector3 currentDirrection = _Transform.forward;

						return ProgresiveTrajectoryCorrection(
								currentRotation,
								currentDirrection,
								StrafeDirection,
								trajectoryCorrection.MinAngle,
								trajectoryCorrection.MaxAngle,
								trajectoryCorrection.MinAngleSpeed,
								trajectoryCorrection.MaxAngleSpeed
								);

					}
			}

			return currentRotation;
		}


		private Quaternion ConstantTrajectoryCorrection(
			Quaternion currentRotation,
			Vector3 currentDirrection,
			Vector3 desiredDirrection,
			float minAngle,
			float maxAngle,
			float speed
			)
		{
			currentDirrection.y = 0;
			desiredDirrection.y = 0;

			float angle = Mathf.Abs(Vector3.Angle(desiredDirrection, currentDirrection));

			if (minAngle <= angle && angle <= maxAngle)
			{
				//float angleDelta = speed * Time.deltaTime;
				//float angleFactor = angleDelta / angle;
				//Quaternion deltaRot = Quaternion.FromToRotation(currentDirrection, Vector3.Lerp(currentDirrection, desiredDirrection, angleFactor));

				//transform.rotation = transform.rotation * deltaRot;


				Quaternion deltaRot = Quaternion.FromToRotation(currentDirrection, desiredDirrection);
				Quaternion desiredRotation = currentRotation * deltaRot;
				return Quaternion.RotateTowards(
												currentRotation,
												desiredRotation,
												speed * Time.deltaTime
												);
			}

			return currentRotation;
		}

		private Quaternion ProgresiveTrajectoryCorrection(
			Quaternion currentRotation,
			Vector3 currentDirrection,
			Vector3 desiredDirrection,
			float minAngle,
			float maxAngle,
			float minAngleSpeed,
			float maxAngleSpeed
			)
		{
			currentDirrection.y = 0;
			desiredDirrection.y = 0;

			float angle = Vector3.Angle(desiredDirrection, currentDirrection);

			if (minAngle < angle && angle < maxAngle)
			{
				Quaternion deltaRot = Quaternion.FromToRotation(currentDirrection, desiredDirrection);
				Quaternion desiredRotation = currentRotation * deltaRot;
				float speedFactor = (angle - minAngle) / (maxAngle - minAngle);
				float rotationSpeed = Mathf.Lerp(minAngleSpeed, maxAngleSpeed, speedFactor);
				return Quaternion.RotateTowards(
					currentRotation,
					desiredRotation,
					rotationSpeed * Time.deltaTime
					);
			}

			return currentRotation;
		}

		public MotionMatchingStateType GetStateType(string stateName, int layerIndex = 0)
		{
			int stateIndex;
			if (!logicLayers[layerIndex].DataLayer.StateIndexes.TryGetValue(stateName, out stateIndex))
			{
#if UNITY_EDITOR
				Debug.LogError($"In MotionMachingComponent on game object {gameObject.name} in motion matching animator \"{motionMatchingController.name}\" there are no exist state with name \"{stateName}\"!");
#endif
				return MotionMatchingStateType.Undefined;
			}

			return motionMatchingController.Layers[layerIndex].States[stateIndex].StateType;
		}

		public float3 GetAnimationTrajecotryPointPosition(int index)
		{
			return logicLayers[0].GetCurrentAnimationTrajectoryPointPosition(index);
		}

		#endregion


		#region Animation events
		public void AddFunctionToEvent(string eventName, MotionMatchingAnimationEventDelegate function)
		{
			if (animationEvents == null)
			{
				InitializeAnimationEvents();
			}

			MotionMatchingComponentAnimationEvent animationEvent;

			if (animationEvents.TryGetValue(eventName, out animationEvent))
			{
				animationEvent.AddFunction(function);
			}
			else
			{
				animationEvent = new MotionMatchingComponentAnimationEvent();
				animationEvent.AddFunction(function);
				animationEvents.Add(eventName, animationEvent);
			}
		}

		public bool RemoveFunctionFromEvent(string eventName, MotionMatchingAnimationEventDelegate function)
		{
			if (animationEvents == null)
			{
				return false;
			}

			int hashCode = eventName.GetHashCode();
			MotionMatchingComponentAnimationEvent animationEvent;

			if (animationEvents.TryGetValue(eventName, out animationEvent))
			{
				animationEvent.RemoveFunction(function);
				return true;
			}

			return false;
		}

		internal void InvokeAnimationEvent(in string eventName)
		{
			MotionMatchingComponentAnimationEvent animationEvent;
			if (animationEvents.TryGetValue(eventName, out animationEvent))
			{
				animationEvent.Invoke();
			}
		}
		#endregion


#if UNITY_EDITOR
		[Header("DEBUG")]
		[SerializeField]
		private bool drawAnimationTrajectory = true;
		[SerializeField]
		private bool drawBonesPositions = false;
		[SerializeField]
		private bool drawBoneVelocities = false;
		[SerializeField]
		private bool drawCharacterForward = false;
		[SerializeField]
		private bool drawStrafeDirrection = false;
		[SerializeField]
		private bool drawAnimationVelocity = false;
		[Range(0.01f, 0.1f)]
		[SerializeField]
		private float pointRadius = 0.05f;

		private BoneData[] gizmosPose = null;
		private TrajectoryPoint[] gizmosTrajectory;

		public State_SO GetCurrentDataState_EditorOnly(int layerIndex = 0)
		{
			return logicLayers[layerIndex].CurrentDataState;
		}


		#region Drawing Gizmos
		private void DrawTrajectory()
		{
			if (drawAnimationTrajectory)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(this.transform.position + Vector3.up * pointRadius, pointRadius);

				logicLayers[0].SetTrajectoryFromMotionGroup(ref nativeGizmosTrajectory);
				Gizmos.color = Color.red;
				if (gizmosTrajectory == null)
				{
					gizmosTrajectory = new TrajectoryPoint[TrajectoryPointsCount];
				}


				NativeMotionGroup group = logicLayers[0].CurrentLogicState.CurrentMotionGroup;

				for (int i = 0; i < gizmosTrajectory.Length; i++)
				{
					TrajectoryPoint tp = nativeGizmosTrajectory[i] / group.TrajectoryCostWeight;
					tp.Position /= group.TrajectoryPositionWeight;
					tp.Velocity /= group.TrajectoryVelocityWeight;
					tp.Orientation /= group.TrajectoryOrientationWeight;

					gizmosTrajectory[i] = tp;

					gizmosTrajectory[i] = gizmosTrajectory[i].TransformToWorldSpaceAndReturn(this.transform);
				}



				MM_Gizmos.DrawTrajectory(
					trajectoryPointsTimes,
					this.transform.position,
					this.transform.forward,
					gizmosTrajectory,
					true,
					pointRadius,
					0.3f
					);
			}
		}

		private void DrawPose()
		{
			if (drawBonesPositions || drawBoneVelocities)
			{
				if (gizmosPose == null)
				{
					gizmosPose = new BoneData[this.GetPoseBoneCount()];
				}

				logicLayers[0].GetCurrentPose(ref gizmosPose);
				for (int i = 0; i < gizmosPose.Length; i++)
				{
					BoneData bone = gizmosPose[i];
					Vector3 pos = this.transform.TransformPoint(bone.localPosition);
					Vector3 vel = this.transform.TransformDirection(bone.velocity);

					if (drawBonesPositions)
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawWireSphere(pos, 0.05f);
					}
					if (drawBoneVelocities)
					{
						Gizmos.color = Color.yellow;
						MM_Gizmos.DrawArrow(pos, pos + vel, 0.05f);
					}
				}
			}
		}

		private void OnDrawGizmos()
		{
			if (motionMatchingController == null)
			{
				return;
			}
			if (Application.isPlaying)
			{
				if (logicLayers.Count == 0) return;

				DrawTrajectory();
				DrawPose();

				logicLayers[0].CurrentLogicState.OnDrawGizmos();

				Gizmos.color = Color.blue;
				//MM_Gizmos.DrawArrow(transform.position + Vector3.up, transform.forward, 1f, 0.33f);

				if (drawCharacterForward)
				{
					Gizmos.color = Color.blue;
					MM_Gizmos.DrawArrow(transform.position, transform.forward, 2f, 0.3f);
				}

				if (drawStrafeDirrection)
				{
					if (StrafeDirection != Vector3.zero)
					{
						Gizmos.color = Color.yellow;
						MM_Gizmos.DrawArrow(transform.position, StrafeDirection, 2f, 0.3f);
					}
				}

				if (drawAnimationVelocity)
				{
					Gizmos.color = Color.magenta;
					float currentSpeed = Velocity.magnitude;
					MM_Gizmos.DrawArrow(transform.position, Velocity.normalized, currentSpeed, currentSpeed * 0.2f);
				}

			}
		}
		#endregion
#endif
	}



	/// <summary>
	/// Paramters
	///		First - Native motion group
	///		Second - MotionMatchingDataInfo representing animation
	///		Third - index on animation in NativeMotionGroup
	///		Fourth - czas rozpoczęcia animacji
	/// </summary>
	public class OnPlayAnimationInMotionMatching : UnityEvent<NativeMotionGroup, MotionMatchingDataInfo, int, float> { }

}