using MotionMatching.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class CreatorBasicOption
	{
		public static void DrawOptions(DataCreator creator)
		{
			float space = 5f;
			GUILayout.Space(space);
			GUILayoutElements.DrawHeader(
				"Options",
				GUIResources.GetLightHeaderStyle_MD()
				);
				DrawBasicOptions(creator);

			GUILayout.Space(space);
			GUILayoutElements.DrawHeader(
				"Bones used to motion matching",
				GUIResources.GetLightHeaderStyle_MD()
				);

				DrawNeededBones(creator);

			GUILayout.Space(space);
			GUILayoutElements.DrawHeader(
				"Trajectory Times",
				GUIResources.GetLightHeaderStyle_MD()
				);

				DrawTrajectoryTimes(creator);
		}

		private static void DrawBasicOptions(DataCreator creator)
		{
			creator.gameObjectTransform = (Transform)EditorGUILayout.ObjectField(new GUIContent("Game Object transform"), creator.gameObjectTransform, typeof(Transform), true);
			GUILayout.Space(5);

			creator.posesPerSecond = Mathf.Clamp(
				EditorGUILayout.IntField(new GUIContent("Poses per second"), creator.posesPerSecond),
				1,
				1000000
				);
			GUILayout.Space(5);
			creator.OverrideTrajectory = EditorGUILayout.Toggle(
				new GUIContent("Override trajectory"),
				creator.OverrideTrajectory
				);
			GUILayout.Space(5);

			creator.findInYourself = EditorGUILayout.Toggle(new GUIContent("Find in yourself"), creator.findInYourself);
			GUILayout.Space(5);

			creator.blendToYourself = EditorGUILayout.Toggle(new GUIContent("Blending to yourself"), creator.blendToYourself);
			GUILayout.Space(5);

			GUILayout.Label("Only for single clips:");
			GUILayout.BeginHorizontal();
			GUILayout.Space(25);
			GUILayout.BeginVertical();
			creator.cutTimeFromStart = EditorGUILayout.FloatField(
				new GUIContent("Cut time from start"),
				creator.cutTimeFromStart
				);
			creator.cutTimeToEnd = EditorGUILayout.FloatField(
				new GUIContent("Cut time to end"),
				creator.cutTimeToEnd
				);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();


			GUILayout.Space(5);


		}

		private static void DrawNeededBones(DataCreator creator)
		{
			GUILayout.BeginHorizontal();
			creator.BonesMask = (BonesProfile)EditorGUILayout.ObjectField(
				new GUIContent("Skeleton Mask"),
				creator.BonesMask,
				typeof(BonesProfile),
				true
				);
			GUILayout.EndHorizontal();
			GUILayout.Space(5);

			int activeBones = 0;

			if (creator.bonesWeights == null)
			{
				creator.bonesWeights = new List<Vector2>();
			}
			else if (creator.BonesMask != null)
			{
				if (creator.gameObjectTransform != null)
				{
					for (int i = 0; i < creator.BonesMask.Mask.transformCount; i++)
					{
						if (creator.BonesMask.Mask.GetTransformActive(i))
						{
							if (creator.gameObjectTransform.Find(creator.BonesMask.Mask.GetTransformPath(i)) != null)
							{
								string name = creator.gameObjectTransform.Find(creator.BonesMask.Mask.GetTransformPath(i)).name;

								if (name != creator.gameObjectTransform.name)
								{
									activeBones++;
									GUILayoutElements.DrawHeader(string.Format("{0}", name), GUIResources.GetLightHeaderStyle_SM());
									if ((creator.bonesWeights.Count) < activeBones)
									{
										creator.bonesWeights.Add(Vector2.one);
									}

									//float posW = creator.bonesWeights[activeBones - 1].x;
									//float velW = creator.bonesWeights[activeBones - 1].y;

									//GUILayout.BeginHorizontal();
									//GUILayout.Label(new GUIContent("Position"), GUILayout.Width(75));
									//posW = EditorGUILayout.Slider(
									//    posW, 0, 1f
									//    );
									//GUILayout.EndHorizontal();

									//GUILayout.BeginHorizontal();
									//GUILayout.Label(new GUIContent("Velocity"), GUILayout.Width(75));
									//velW = EditorGUILayout.Slider(
									//    velW, 0, 1f
									//    );
									//GUILayout.EndHorizontal();

									//creator.bonesWeights[activeBones - 1] = new Vector2(posW, velW);
									GUILayout.Space(5);
								}
							}
							else
							{
								Debug.LogWarning("Game object transform is wrong");
								return;
							}
						}
					}

					if (creator.bonesWeights.Count > activeBones)
					{
						for (; creator.bonesWeights.Count > activeBones;)
						{
							creator.bonesWeights.RemoveAt(creator.bonesWeights.Count - 1);
						}
					}

				}
			}

			if (activeBones > 10)
			{
				GUILayout.Label("Max number of matched bones is equal 10");
			}
		}

		private static void DrawTrajectoryTimes(DataCreator creator)
		{
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(5);
				GUILayout.Label("Trajectory times");

				creator.trajectorySettings = (DataCreatorTrajectorySettings)EditorGUILayout.ObjectField(
					creator.trajectorySettings,
					typeof(DataCreatorTrajectorySettings),
					true
					);
				GUILayout.Space(5);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);

			if (creator.trajectorySettings != null)
			{
				for(int i = 0; i < creator.trajectorySettings.TrajectoryTimes.Count; i++)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(30);
						GUILayout.Label($"{i + 1}.\t{ creator.trajectorySettings.TrajectoryTimes[i]}");
					}
					GUILayout.EndHorizontal();
				}

				return;
			}

			if (creator.trajectoryStepTimes == null)
			{
				creator.trajectoryStepTimes = new List<float>();
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Trajectory Time"))
			{
				if (creator.trajectoryStepTimes.Count < 10)
				{
					if (creator.trajectoryStepTimes.Count == 0)
					{
						creator.trajectoryStepTimes.Add(0);
					}
					else
					{
						creator.trajectoryStepTimes.Add(creator.trajectoryStepTimes[creator.trajectoryStepTimes.Count - 1] + 0.33f);
					}
				}
				else
				{
					Debug.LogWarning("Max number of trajectorySteps is equal 10");
				}
			}
			if (GUILayout.Button("Sort Trajectory"))
			{
				creator.trajectoryStepTimes.Sort();
			}
			GUILayout.EndHorizontal();
			for (int i = 0; i < creator.trajectoryStepTimes.Count; i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				GUILayout.Label(
					new GUIContent(string.Format("Time {0}", i + 1)),
					GUILayout.Width(75)
					);
				creator.trajectoryStepTimes[i] = EditorGUILayout.FloatField(
					creator.trajectoryStepTimes[i]
					);

				if (GUILayout.Button("X", GUILayout.Width(25)))
				{
					creator.trajectoryStepTimes.RemoveAt(i);
					i--;
				}
				GUILayout.Space(10);
				GUILayout.EndHorizontal();
			}
		}

		public static void DrawAnimationList(DataCreator creator)
		{
			GUILayout.Space(5);
			GUILayout.Label("Drag And droop animations here", GUIResources.GetLightHeaderStyle_MD());
			Event e = Event.current;


			Rect dropRect = GUILayoutUtility.GetLastRect();
			if (dropRect.Contains(e.mousePosition))
			{
				if (Event.current.type == EventType.DragUpdated)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
				}
				else if (Event.current.type == EventType.DragPerform)
				{
					bool correctData = true;
					List<AnimationClip> newData = new List<AnimationClip>();
					for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
					{
						try
						{
							newData.Add((AnimationClip)DragAndDrop.objectReferences[i]);
						}
						catch (Exception)
						{
							correctData = false;
							break;
						}
					}

					if (correctData)
					{
						for (int i = 0; i < newData.Count; i++)
						{
							creator.clips.Add(newData[i]);
						}
					}
					Event.current.Use();
				}
			}

			GUILayout.Space(10);

			for (int i = 0; i < creator.clips.Count; i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(10);
				EditorGUILayout.ObjectField(creator.clips[i], typeof(AnimationClip), false);
				if (GUILayout.Button("X", GUILayout.Width(25)))
				{
					creator.clips.RemoveAt(i);
					i--;
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Clear"))
			{
				creator.clips.Clear();
			}
			if (GUILayout.Button("Remove nulls"))
			{
				for (int i = 0; i < creator.clips.Count; i++)
				{
					if (creator.clips[i] == null)
					{
						creator.clips.RemoveAt(i);
						i--;
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
		}

	}
}
