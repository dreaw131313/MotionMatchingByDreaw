
using UnityEngine;
using MotionMatching.Gameplay.Core;

#if UNITY_EDITOR
using UnityEditorInternal;

using UnityEditor;
#endif

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public class MotionMatchingStateFeatures
	{
		[SerializeField]
		public float updateInterval;
		[SerializeField]
		public float blendTime;
		[SerializeField]
		[Range(0.01f, 1f)]
		public float maxClipDeltaTime;
		[SerializeField]
		public int maxBlendedClipCount;
		[SerializeField]
		[Range(0.0f, 1.0f)]
		public float minWeightToAchive; // waga która musi zostać osiągnięta zanim jej waga znowu zacznie zmierzać do zera. Nie jest to ta sama wartość którą posiada animacja w PlayableGraph, jest ona osiągana podczas obliczania wag wszystkich animacji przez w danym MMLogicState, które są podawane dla Playable graph w postaci znormalizowanej (Same pozostają w postaci nie znormalizowanej).
		public MotionMatchingStateFeatures()
		{
		}
	}

	[System.Serializable]
	public class SingleAnimationStateFeatures
	{
		[SerializeField]
		public bool loop;
		[SerializeField]
		public int loopCountBeforeStop = 1;
		[SerializeField]
		public SingleAnimationUpdateType updateType;
		[SerializeField]
		public float blendTime;
		[SerializeField]
		public bool CanBlendToTheSameAnimation;

		[SerializeField]
		public SingleAnimationFindingType AnimationFindingType = SingleAnimationFindingType.FindInAll;
		[SerializeField]
		[Min(0)]
		public int AnimationIndexToFind = 0;

		public SingleAnimationStateFeatures()
		{
			loop = true;
			updateType = SingleAnimationUpdateType.PlaySelected;
			blendTime = 0.35f;
			CanBlendToTheSameAnimation = false;
			AnimationFindingType = SingleAnimationFindingType.FindInAll;
		}
	}

	[System.Serializable]
	public class ContactStateFeatures
	{
		[SerializeField]
		public ContactStateType contactStateType = ContactStateType.NormalContacts;



		//[SerializeField]
		//public float contactPointsWeight = 1f;
		[SerializeField]
		public ContactPointPositionCorrectionType PositionCorrectionType = ContactPointPositionCorrectionType.MovePosition;
		[SerializeField]
		public bool MoveToContactWhenOnContact = true;
		[SerializeField]
		public AnimationCurve LerpPositionCurve;
		[SerializeField]
		public ContactRotatationType RotationType = ContactRotatationType.RotateToConatct;
		[SerializeField]
		public bool UseDirBetweenContactsToCorrectRotation = true;


		// Adapt stuff:
		[SerializeField]
		public bool Adapt = false;
		[SerializeField]
		public LayerMask AdaptLayerMask;
		[SerializeField]
		public float BackDeltaForAdapting = 0.5f;
		[SerializeField]
		public float AdaptRaycastLength = 1f;

		// Recently played clips stuff:
		[SerializeField]
		public bool NotSearchInRecentClips = false;
		[SerializeField]
		[Min(0)]
		public int RemeberedRecentlyPlayedClipsCount = 0;
		[SerializeField]
		[Min(0)]
		public float TimeToResetRecentlyPlayedClips = 10; // in seconds

		// Time scaling
		[SerializeField]
		public bool UseTimeScaling = false;
		[SerializeField]
		public float MinTimeSpeedMultiplier = 0.8f;
		[SerializeField]
		public float MaxTimeSpeedMultiplier = 1.2f;
		[SerializeField]
		public ContactStateTimeScalingPositionMask TimeScalingPositionMask = ContactStateTimeScalingPositionMask.X | ContactStateTimeScalingPositionMask.Y | ContactStateTimeScalingPositionMask.Z;

		public ContactStateFeatures()
		{
			Adapt = false;
			PositionCorrectionType = ContactPointPositionCorrectionType.MovePosition;
			MinTimeSpeedMultiplier = 0.8f;
		}

#if UNITY_EDITOR
		public void DrawEditorGUI(NativeMotionGroup motionGroup)
		{
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(10);
				if (Application.isPlaying)
				{
					EditorGUILayout.EnumPopup("Contact state type", contactStateType);
				}
				else
				{
					contactStateType = (ContactStateType)EditorGUILayout.EnumPopup("Contact state type", contactStateType);
				}
			}
			GUILayout.EndHorizontal();

			DrawNormalContactStateFeatures(motionGroup);
			GUILayout.Space(5);
		}

		public void DrawNormalContactStateFeatures(NativeMotionGroup motionGroup)
		{
			if (contactStateType == ContactStateType.NormalContacts)
			{

				//GUILayout.BeginHorizontal();
				//GUILayout.Space(10);
				//contactMovementType = (ContactStateMovemetType)EditorGUILayout.EnumPopup(new GUIContent("Contact type"), contactMovementType);
				//GUILayout.EndHorizontal();

				GUILayout.Space(5);
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(10);
						PositionCorrectionType = (ContactPointPositionCorrectionType)EditorGUILayout.EnumPopup(new GUIContent("Position correction"), PositionCorrectionType);
					}
					GUILayout.EndHorizontal();

					if (PositionCorrectionType == ContactPointPositionCorrectionType.MovePosition)
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(10);
							MoveToContactWhenOnContact = EditorGUILayout.Toggle("Move when on contact", MoveToContactWhenOnContact);
						}
						GUILayout.EndHorizontal();
					}

					if (PositionCorrectionType == ContactPointPositionCorrectionType.LerpWithCurve)
					{
						if (LerpPositionCurve == null)
						{
							LerpPositionCurve = new AnimationCurve();
						}
						GUILayout.BeginHorizontal();
						GUILayout.Space(20);
						LerpPositionCurve = EditorGUILayout.CurveField("Lerp curve", LerpPositionCurve);
						GUILayout.EndHorizontal();

						AnimationCurveExtensions.ClampAnimationCurve(ref LerpPositionCurve, 0f, 1f, 0f, 1f);
					}

				}
				GUILayout.Space(5);

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					RotationType = (ContactRotatationType)EditorGUILayout.EnumPopup(new GUIContent("Rotate to contact type"), RotationType);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.Label(new GUIContent("Use dir between contacts to correct rot"));

					UseDirBetweenContactsToCorrectRotation = EditorGUILayout.Toggle(
						UseDirBetweenContactsToCorrectRotation
						);
				}
				GUILayout.EndHorizontal();


				if (motionGroup == null)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(10);
						GUILayout.Label("You must set motion group!");
					}
					GUILayout.EndHorizontal();
				}

				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label("ADAPTING POITNS:", EditorStyles.boldLabel);
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						Adapt = EditorGUILayout.Toggle(new GUIContent("Adapt movemet"), Adapt);
						GUILayout.EndHorizontal();

						if (Adapt)
						{
							GUILayout.BeginHorizontal();
							//AdaptLayerMask = EditorGUILayout.LayerField(new GUIContent("Layer mask"), AdaptLayerMask);
							LayerMask bufforMask = EditorGUILayout.MaskField(
								new GUIContent("Layer mask"),
								 InternalEditorUtility.LayerMaskToConcatenatedLayersMask(AdaptLayerMask),
								InternalEditorUtility.layers
								);

							AdaptLayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(bufforMask);
							GUILayout.EndHorizontal();

							GUILayout.BeginHorizontal();
							BackDeltaForAdapting = EditorGUILayout.FloatField(new GUIContent("Back delta for adapting"), BackDeltaForAdapting);
							GUILayout.EndHorizontal();

							GUILayout.BeginHorizontal();
							AdaptRaycastLength = EditorGUILayout.FloatField(new GUIContent("Adapt raycast length"), AdaptRaycastLength);
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label("TIME SCALING:", EditorStyles.boldLabel);
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						UseTimeScaling = EditorGUILayout.Toggle(
							new GUIContent("Use time scaling"),
							UseTimeScaling
							);
						GUILayout.EndHorizontal();

						if (UseTimeScaling)
						{
							GUILayout.BeginHorizontal();
							MinTimeSpeedMultiplier = EditorGUILayout.Slider(
								new GUIContent("Min time multiplier"),
								MinTimeSpeedMultiplier,
								0.01f,
								2f
								);
							GUILayout.EndHorizontal();

							GUILayout.BeginHorizontal();
							MaxTimeSpeedMultiplier = EditorGUILayout.Slider(
								new GUIContent("Max time multiplier"),
								MaxTimeSpeedMultiplier,
								0.01f,
								2f
								);
							GUILayout.EndHorizontal();

							GUILayout.BeginHorizontal();
							TimeScalingPositionMask = (ContactStateTimeScalingPositionMask)EditorGUILayout.EnumFlagsField(
								new GUIContent("Time scaling axis"),
								TimeScalingPositionMask
								);
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}

			if (motionGroup != null && motionGroup.MotionDataInfos.Count > 1)
			{
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label("RECENTLY PLAYED CLIPS:", EditorStyles.boldLabel);
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						NotSearchInRecentClips = EditorGUILayout.Toggle(
							new GUIContent("Not search recent clips"),
							NotSearchInRecentClips);
						GUILayout.EndHorizontal();

						if (NotSearchInRecentClips)
						{
							GUILayout.BeginHorizontal();
							//GUILayout.Label(new GUIContent("Remebered clips count"));
							RemeberedRecentlyPlayedClipsCount = EditorGUILayout.IntSlider(
								new GUIContent("Remebered clips count"),
								RemeberedRecentlyPlayedClipsCount,
								1,
								motionGroup.MotionDataInfos.Count - 1
								);
							GUILayout.EndHorizontal();

							GUILayout.BeginHorizontal();
							//GUILayout.Label(new GUIContent("Remebered clips count"));
							TimeToResetRecentlyPlayedClips = EditorGUILayout.FloatField(
								new GUIContent("How long remember clips"),
								TimeToResetRecentlyPlayedClips
								);
							TimeToResetRecentlyPlayedClips = Mathf.Clamp(TimeToResetRecentlyPlayedClips, 0f, float.MaxValue);
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}



		}
#endif
	}
}
