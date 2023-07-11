using MotionMatching.Gameplay;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MotionMatching.Tools.MotionMatchingDataEditor;

namespace MotionMatching.Tools
{
	[System.Flags]
	public enum MotionMatchingDataEditorGizmos
	{
		None = 0,
		Trajectory = 1,
		TrajectoryVelocities = 1 << 1,
		Pose = 1 << 2,
		PoseVelocity = 1 << 3,
		Contacts = 1 << 4,
		Tracks = 1 << 5,
		CharacterVelocity = 1 << 6,
	}

	public class MMDataEditorRightMenu : MMDataEditorMenuBaseClass
	{
		MotionMatchingData bufforData;
		GameObject bufforObject;

		const float verticalMargin = 5f;

		public DataSection SelectedSection { get; private set; }

		Vector2 scroll;
		public MotionMatchingDataEditorGizmos GizmosToDraw { get; set; } = MotionMatchingDataEditorGizmos.None;

		public override void OnEnable()
		{
			bufforData = null;
			bufforObject = null;

			SceneView.duringSceneGui += OnSceneGUI;
		}

		public override void OnDisable()
		{
			base.OnDisable();
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		public override void OnDestroy()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			base.OnDestroy();
		}

		public override void OnGUI(
			Event e,
			Rect rect
			)
		{
			GUILayout.BeginArea(rect);
			{
				DrawDataAndGameObjectFields();
				GUILayout.Space(verticalMargin);
				GizmosSelection();
				GUILayout.Space(verticalMargin);
				DrawToolSelection();
				GUILayout.Space(verticalMargin);


				if (editor.EditedData != null)
				{
					switch (editor.CurrentTool)
					{
						case MotionMatchingDataEditingTool.Sections:
							{
								DrawSections(e, rect);
							}
							break;
						case MotionMatchingDataEditingTool.Contacts:
							{
								DrawContacts(e, rect);
							}
							break;
						case MotionMatchingDataEditingTool.Curves:
							{
								DrawCurves(e, rect);
							}
							break;
						case MotionMatchingDataEditingTool.BoneTracks:
							{
								DrawBoneTracks(e, rect);
							}
							break;
						case MotionMatchingDataEditingTool.AnimationEvents:
							{
								DrawEvents();
							}
							break;
						case MotionMatchingDataEditingTool.AnimationSpeedCurve:
							{
								DrawAnimationSpeedCurveOptions();
							}
							break;
					}
				}

			}
			GUILayout.EndArea();
		}

		private void DrawSections(
			Event e,
			Rect rect
			)
		{
			float verticalMargin = 10f;
			float marginBetweenSections = 3;
			float sectionHeight = 25f;
			float labelVertMargin = 2.5f;

			scroll = EditorGUILayout.BeginScrollView(scroll);
			{
				GUILayout.BeginVertical();
				{
					GUILayout.Label("Core sections:");
					{
						DrawSectionGUI(e, rect, editor.EditedData.neverChecking.sectionName, editor.EditedData.neverChecking, sectionHeight, labelVertMargin);
						GUILayout.Space(marginBetweenSections);
						DrawSectionGUI(e, rect, editor.EditedData.notLookingForNewPose.sectionName, editor.EditedData.notLookingForNewPose, sectionHeight, labelVertMargin);
						GUILayout.Space(marginBetweenSections);
						DrawSectionGUI(e, rect, "Always", editor.EditedData.sections[0], sectionHeight, labelVertMargin);
					}

					GUILayout.Space(verticalMargin);
					GUILayout.Label("Data sections:");
					{
						for (int i = 1; i < editor.EditedData.sections.Count; i++)
						{
							DataSection section = editor.EditedData.sections[i];
							DrawSectionGUI(e, rect, section.sectionName, section, sectionHeight, labelVertMargin);
							GUILayout.Space(marginBetweenSections);
						}
					}
				}
				GUILayout.Space(20f);
				GUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();
		}

		private void DrawSectionGUI(Event e, Rect menuRect, string sectionName, DataSection section, float height, float labelUpMargin)
		{
			GUILayout.Label("", GUILayout.Height(height));
			Rect sectionRect = GUILayoutUtility.GetLastRect();
			sectionRect.width = menuRect.width;

			GUI.DrawTexture(sectionRect, editor.SectionNameBackgroundTexture);
			if (section == SelectedSection)
			{
				GUI.DrawTexture(sectionRect, editor.SelectedSectionBackgroundTexture);
			}

			Rect labelRect = sectionRect;
			labelRect.y += labelUpMargin;
			labelRect.height -= 2f * labelUpMargin;
			labelRect.width -= 5;
			labelRect.x += 5;
			GUI.Label(labelRect, $"{sectionName}");

			switch (e.type)
			{
				case EventType.MouseDown:
					{
						if (sectionRect.Contains(e.mousePosition))
						{
							if (SelectedSection == section)
							{
								SelectedSection = null;
							}
							else
							{
								SelectedSection = section;
							}
							editor.LeftMenuEditor.SetSectionToDraw(SelectedSection);
							e.Use();
							editor.Repaint();
						}
					}
					break;
			}
		}


		float eventOffsetMargin = 20f;
		private void DrawEvents()
		{
			if (editor.LeftMenuEditor.SelectedEvent == null) { return; }

			MotionMatchingAnimationEvent animationEvent = editor.LeftMenuEditor.SelectedEvent;

			GUILayout.Label("Selected Event:");
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(eventOffsetMargin);
				GUILayout.BeginVertical();
				{
					animationEvent.Name = EditorGUILayout.TextField("Event name", animationEvent.Name);
					animationEvent.EventTime = Mathf.Clamp(
						EditorGUILayout.FloatField("Event time", animationEvent.EventTime),
						0f,
						editor.EditedData.animationLength
						);


					if (GUILayout.Button("Set current timeline time"))
					{
						animationEvent.EventTime = Mathf.Clamp(
							editor.LeftMenuEditor.AnimationTime,
							0f,
							editor.EditedData.animationLength
							);
					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}

		#region Drawing allaws visible controlls

		private void DrawDataAndGameObjectFields()
		{
			GUILayout.BeginHorizontal();
			{
				editor.CurrentGameObject = EditorGUILayout.ObjectField(editor.CurrentGameObject, typeof(GameObject), true) as GameObject;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				editor.EditedData = EditorGUILayout.ObjectField(editor.EditedData, typeof(MotionMatchingData), true) as MotionMatchingData;
			}
			GUILayout.EndHorizontal();

			if (editor.CurrentGameObject != bufforObject)
			{
				bufforObject = editor.CurrentGameObject;
				OnGameObjectChange();
				editor.Repaint();
			}

			if (editor.EditedData != bufforData)
			{
				bufforData = editor.EditedData;
				OnDataChange();
				editor.Repaint();
			}
		}

		private void GizmosSelection()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Gizmos", GUILayout.Width(60f));
				GizmosToDraw = (MotionMatchingDataEditorGizmos)EditorGUILayout.EnumFlagsField(GizmosToDraw);
			}
			GUILayout.EndHorizontal();
		}

		private void DrawToolSelection()
		{
			GUILayout.BeginHorizontal();
			{
				//editor.CurrentTool = (MotionMatchingDataEditingTool)EditorGUILayout.EnumPopup(editor.CurrentTool);

				/*
				string[] tools = {
								"Sections",
								"Contacts",
								"Curves",
								"Events",
								"Tracks",
								"Speed curve"
				};



				editor.CurrentTool = (MotionMatchingDataEditingTool)GUILayout.Toolbar((int)editor.CurrentTool, tools);
				*/
				editor.CurrentTool = (MotionMatchingDataEditingTool)EditorGUILayout.EnumPopup(editor.CurrentTool);
			}
			GUILayout.EndHorizontal();
		}

		public void OnDataChange()
		{
			editor.LeftMenuEditor.OnDataChange();


			selectedCurve = null;
			SelectedContactIndex = -1;
			SelectedBoneTrackIndex = -1;

			if (SelectedSection != null && editor.EditedData != null)
			{
				string desiredName = SelectedSection.sectionName;
				SelectedSection = null;
				if (desiredName == editor.EditedData.neverChecking.sectionName)
				{
					SelectedSection = editor.EditedData.neverChecking;
					editor.Repaint();
				}
				else if (desiredName == editor.EditedData.notLookingForNewPose.sectionName)
				{
					SelectedSection = editor.EditedData.neverChecking;
					editor.Repaint();
				}
				else
				{
					for (int i = 0; i < editor.EditedData.sections.Count; i++)
					{
						if (desiredName == editor.EditedData.sections[i].sectionName)
						{
							SelectedSection = editor.EditedData.sections[i];

							break;
						}
					}
				}
			}
			else
			{
				SelectedSection = null;
			}
			editor.LeftMenuEditor.SetSectionToDraw(SelectedSection);


		}

		public void OnGameObjectChange()
		{

			editor.LeftMenuEditor.OnGameObjectChange();
		}
		#endregion

		#region DrawCurves
		MotionMatchingDataCurve selectedCurve;

		private void DrawCurves(
			Event e,
			Rect rect
			)
		{
			scroll = EditorGUILayout.BeginScrollView(scroll);
			{
				for (int i = 0; i < editor.EditedData.Curves.Count; i++)
				{
					DrawCurveGUI(e, rect, editor.EditedData.Curves[i], 25f, 2.5f);
					GUILayout.Space(5);
				}

			}
			EditorGUILayout.EndScrollView();

			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal();
				{
					float margin = 5f;
					GUILayout.Space(margin);
					if (GUILayout.Button("Add curve"))
					{
						editor.EditedData.Curves.Add(new MotionMatchingDataCurve("<Animation curve>"));
					}
					GUILayout.Space(margin);
					if (GUILayout.Button("Remove curve"))
					{
						if (selectedCurve != null)
						{
							editor.EditedData.Curves.Remove(selectedCurve);
							selectedCurve = null;
							editor.LeftMenuEditor.SelectedCurve = null;
						}
					}
					GUILayout.Space(margin);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		private void DrawCurveGUI(Event e, Rect menuRect, MotionMatchingDataCurve curve, float labelHeight, float labelVerticalMargin)
		{
			Rect curveLabelRect;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("", GUILayout.Height(labelHeight));

				curveLabelRect = GUILayoutUtility.GetLastRect();
				curveLabelRect.width = menuRect.width;

				GUI.DrawTexture(curveLabelRect, editor.SectionNameBackgroundTexture);

				Rect labelRect = curveLabelRect;
				labelRect.y += labelVerticalMargin;
				labelRect.height -= 2 * labelVerticalMargin;
				labelRect.width -= 5;
				labelRect.x += 5;
				GUI.Label(labelRect, $"{curve.Name}");

				if (selectedCurve == curve)
				{
					GUI.DrawTexture(curveLabelRect, editor.SelectedSectionBackgroundTexture);
				}
			}
			GUILayout.EndHorizontal();


			switch (e.type)
			{
				case EventType.MouseDown:
					{
						if (curveLabelRect.Contains(e.mousePosition))
						{
							if (selectedCurve == curve)
							{
								selectedCurve = null;
							}
							else
							{
								selectedCurve = curve;
							}

							editor.LeftMenuEditor.SelectedCurve = selectedCurve;
							e.Use();
							editor.Repaint();
						}
					}
					break;
			}
		}
		#endregion

		#region Draw Contacts
		public int SelectedContactIndex { get; private set; } = -1;

		private void DrawContacts(
			Event e,
			Rect rect
			)
		{
			float verticalMargin = 5f;
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Sort contacts"))
				{
					editor.EditedData.SortContacts();
				}

				GUILayout.Space(5);

				if (GUILayout.Button("Calculate contacts"))
				{
					editor.LeftMenuEditor.PlayableGraph?.Destroy();

					if (editor.CurrentGameObject == null)
					{
						Debug.LogWarning("Game object in MM Data Editor is NULL!");
					}
					else
					{
						Transform t = editor.CurrentGameObject.transform;
						Vector3 currentPos = t.position;
						Quaternion currentRot = t.rotation;

						editor.EditedData.SortContacts();
						MotionDataCalculator.CalculateContactPoints(
							editor.EditedData,
							editor.EditedData.contactPoints.ToArray(),
							editor.LeftMenuEditor.PlayableGraph,
							editor.CurrentGameObject
							);

						editor.LeftMenuEditor.PlayableGraph = new PreparingDataPlayableGraph();
						editor.LeftMenuEditor.PlayableGraph.Initialize(editor.CurrentGameObject);
						editor.LeftMenuEditor.PlayableGraph.CreateAnimationDataPlayables(editor.EditedData, editor.LeftMenuEditor.AnimationTime);

						t.position = currentPos;
						t.rotation = currentRot;
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(verticalMargin);

			scroll = EditorGUILayout.BeginScrollView(scroll);
			{
				for (int i = 0; i < editor.EditedData.contactPoints.Count; i++)
				{
					DrawContactGUI(e, rect, editor.EditedData.contactPoints[i], i == SelectedContactIndex, i, 25f, 2.5f);
					GUILayout.Space(verticalMargin);
				}
			}
			EditorGUILayout.EndScrollView();

			float horizontalMarign = 10f;
			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Add contact"))
				{
					editor.EditedData.contactPoints.Add(new MotionMatchingContact(editor.LeftMenuEditor.AnimationTime));
				}
				GUILayout.Space(horizontalMarign);
				if (GUILayout.Button("Remove contact"))
				{
					if (SelectedContactIndex >= 0 && SelectedContactIndex < editor.EditedData.contactPoints.Count)
					{
						editor.EditedData.contactPoints.RemoveAt(SelectedContactIndex);
					}

					SelectedContactIndex = -1;
				}
			}
			GUILayout.EndHorizontal();

		}

		private void DrawContactGUI(Event e, Rect menuRect, MotionMatchingContact contact, bool isSelected, int index, float labelHeight, float labelVerticalMargin)
		{
			Rect conatactLabel;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("", GUILayout.Height(labelHeight));

				conatactLabel = GUILayoutUtility.GetLastRect();
				conatactLabel.width = menuRect.width;

				GUI.DrawTexture(conatactLabel, editor.SectionNameBackgroundTexture);

				Rect labelRect = conatactLabel;
				labelRect.y += labelVerticalMargin;
				labelRect.height -= 2 * labelVerticalMargin;
				labelRect.width -= 5;
				labelRect.x += 5;
				GUI.Label(labelRect, $"{contact.Name_EditorOnly}");

				if (isSelected)
				{
					GUI.DrawTexture(conatactLabel, editor.SelectedSectionBackgroundTexture);
				}
			}
			GUILayout.EndHorizontal();

			switch (e.type)
			{
				case EventType.MouseDown:
					{
						if (conatactLabel.Contains(e.mousePosition))
						{
							if (isSelected)
							{
								SelectedContactIndex = -1;
							}
							else
							{
								SelectedContactIndex = index;
							}

							e.Use();
							editor.Repaint();
						}
					}
					break;
			}
		}
		#endregion


		#region Bone tracks

		public int SelectedBoneTrackIndex { get; private set; } = -1;

		public BoneTrack SelectedBoneTrack
		{
			get
			{
				if (editor.EditedData.BoneTracks == null || SelectedBoneTrackIndex < 0 || SelectedBoneTrackIndex >= editor.EditedData.BoneTracks.Count)
				{
					return null;
				}

				return editor.EditedData.BoneTracks[SelectedBoneTrackIndex];
			}
		}

		private void DrawBoneTracks(
			Event e,
			Rect rect
			)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.BeginVertical();
				{
					scroll = EditorGUILayout.BeginScrollView(scroll);
					{
						for (int i = 0; i < editor.EditedData.BoneTracks.Count; i++)
						{
							DrawTrackGUI(e, rect, editor.EditedData.BoneTracks[i], i == SelectedBoneTrackIndex, i, 25f, 2.5f);
							GUILayout.Space(verticalMargin);
						}
					}
					EditorGUILayout.EndScrollView();

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Calculate tracks"))
					{

					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}


		private void DrawTrackGUI(
			Event e,
			Rect menuRect,
			BoneTrack track,
			bool isSelected,
			int index,
			float labelHeight,
			float labelVerticalMargin
			)
		{
			Rect trackLabel;

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("", GUILayout.Height(labelHeight));

				trackLabel = GUILayoutUtility.GetLastRect();
				trackLabel.width = menuRect.width;

				float drawButtonWidth = 40f;

				GUI.DrawTexture(trackLabel, editor.SectionNameBackgroundTexture);

				Rect labelRect = trackLabel;
				labelRect.y += labelVerticalMargin;
				labelRect.height -= 2 * labelVerticalMargin;
				labelRect.width -= 15 + drawButtonWidth;
				labelRect.x += 5;
				GUI.Label(labelRect, $"{track.TrackSettings.TrackName}");

				Rect drawTrackButton = new Rect(
					labelRect.x + labelRect.width,
					labelRect.y,
					drawButtonWidth,
					labelRect.height
					);



				if (isSelected)
				{
					GUI.DrawTexture(trackLabel, editor.SelectedSectionBackgroundTexture);
				}

				if (GUI.Button(drawTrackButton, "Draw"))
				{
					track.DrawGizmos = !track.DrawGizmos;
				}

				if (track.DrawGizmos)
				{
					GUI.DrawTexture(drawTrackButton, editor.LockingEnabledTexture);
				}
			}
			GUILayout.EndHorizontal();

			switch (e.type)
			{
				case EventType.MouseDown:
					{
						if (trackLabel.Contains(e.mousePosition))
						{
							if (isSelected)
							{
								SelectedBoneTrackIndex = -1;
							}
							else
							{
								SelectedBoneTrackIndex = index;
							}

							e.Use();
							editor.Repaint();
						}
					}
					break;
			}

			if (isSelected)
			{
				if (GUILayout.Button("Calculate track intervals"))
				{
					if (editor.CurrentGameObject != null)
					{
						GameObject go = editor.CurrentGameObject;

						Vector3 goPos = go.transform.position;
						Quaternion goRot = go.transform.rotation;

						MotionDataCalculator.CreateTracksIntervals(
							go,
							editor.LeftMenuEditor.PlayableGraph,
							editor.EditedData.clips[0],
							track.TrackSettings,
							track
							);

						EditorUtility.SetDirty(editor.EditedData);

						editor.LeftMenuEditor.PlayableGraph.CreateAnimationDataPlayables(editor.EditedData, editor.LeftMenuEditor.AnimationTime);

						go.transform.SetPositionAndRotation(goPos, goRot);

					}
				}

				if (GUILayout.Button("Calculate track data"))
				{
					GameObject go = editor.CurrentGameObject;

					Vector3 goPos = go.transform.position;
					Quaternion goRot = go.transform.rotation;

					MotionDataCalculator.CreateBoneTrackData(
						go,
						editor.LeftMenuEditor.PlayableGraph,
						editor.EditedData.clips[0],
						track
						);

					EditorUtility.SetDirty(editor.EditedData);

					editor.LeftMenuEditor.PlayableGraph.CreateAnimationDataPlayables(editor.EditedData, editor.LeftMenuEditor.AnimationTime);

					go.transform.SetPositionAndRotation(goPos, goRot);

				}



			}
		}
		#endregion

		#region Animation speed curve

		private void DrawAnimationSpeedCurveOptions()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Use animation speed curve");
				editor.EditedData.UseAnimationSpeedCurve = EditorGUILayout.Toggle(editor.EditedData.UseAnimationSpeedCurve);
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region On scene gui:

		// contacts tools:
		private bool drawPositionManipulator = false;
		private bool drawRotationManipuator = false;

		private const float gizmosCubeSize = 0.05f;
		private const float arrowLength = 0.5f;
		private const float arrowArmLength = 0.15f;

		private bool drawTrack;

		// Tracks on scene gui:

		private void OnSceneGUI(SceneView sceneView)
		{
			if (editor.EditedData == null || editor.CurrentGameObject == null) return;

			MotionMatchingData currentData = editor.EditedData;

			// Drawing contacts gizmo select
			if (editor.CurrentTool == MotionMatchingDataEditingTool.Contacts &&
				SelectedContactIndex >= 0 && SelectedContactIndex < currentData.contactPoints.Count)
			{
				DrawContactsSceneManipulators();

				float length = 66f;
				float height = 30f;
				Rect r = new Rect(
					sceneView.position.width / 2f - length / 2f,
					10f,
					length,
					height
					);
				DrawContactGizmosSelectRect(r);
			}

			DrawGizmos();
		}

		private void DrawContactsSceneManipulators()
		{
			Transform t = editor.CurrentGameObject.transform;
			MotionMatchingContact contact = editor.EditedData.contactPoints[SelectedContactIndex];

			Vector3 contactGlobalPos = t.TransformPoint(contact.position);
			Vector3 contactGlobalDir = t.TransformDirection(contact.contactNormal);

			bool callRepaint = false;

			if (drawPositionManipulator)
			{
				Vector3 posBuffor = Handles.PositionHandle(contactGlobalPos, t.transform.rotation);

				if (posBuffor != contactGlobalPos)
				{
					contactGlobalPos = posBuffor;
					callRepaint = true;
					contact.position = t.InverseTransformPoint(contactGlobalPos);
				}
			}

			if (drawRotationManipuator)
			{
				//rot = Handles.RotationHandle(rot, cp.position);
				if (contact.rotation.x == 0f &&
					contact.rotation.y == 0f &&
					contact.rotation.z == 0f &&
					contact.rotation.w == 0f)
				{
					contact.rotation = Quaternion.identity;
				}

				Quaternion worldRotation = t.rotation * contact.rotation;
				Quaternion worldRotationBuffor = Handles.RotationHandle(worldRotation, contactGlobalPos);

				if (worldRotation != worldRotationBuffor)
				{
					worldRotation = worldRotationBuffor;
					contact.rotation = Quaternion.Inverse(t.rotation) * worldRotation;
					contactGlobalDir = worldRotation * Vector3.forward;
					callRepaint = true;
					contact.contactNormal = t.InverseTransformDirection(contactGlobalDir);
				}
			}

			Handles.color = Color.green;
			Handles.DrawWireCube(contactGlobalPos, Vector3.one * gizmosCubeSize);
			MM_Gizmos.DrawArrowHandles(contactGlobalPos, contactGlobalDir, arrowLength, arrowArmLength);


			editor.EditedData.contactPoints[SelectedContactIndex] = contact;

			if (callRepaint)
			{
				editor.Repaint();
			}
		}

		private void DrawContactGizmosSelectRect(Rect rect)
		{
			Handles.BeginGUI();
			{
				GUILayout.BeginArea(rect);
				{
					GUILayout.BeginHorizontal();
					float width = rect.width / 2f - 3;
					float height = rect.height;

					if (drawPositionManipulator)
					{
						drawPositionManipulator = GUILayout.Toggle(
							drawPositionManipulator,
							EditorGUIUtility.IconContent("MoveTool On"),
							new GUIStyle("Button"),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					else
					{
						drawPositionManipulator = GUILayout.Toggle(
							drawPositionManipulator,
							EditorGUIUtility.IconContent("MoveTool"),
							new GUIStyle("Button"),
							GUILayout.Width(30),
							GUILayout.Height(30)
							);
					}

					if (drawRotationManipuator)
					{
						drawRotationManipuator = GUILayout.Toggle(
							drawRotationManipuator,
							EditorGUIUtility.IconContent("RotateTool On"),
							new GUIStyle("Button"),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					else
					{
						drawRotationManipuator = GUILayout.Toggle(
							drawRotationManipuator,
							EditorGUIUtility.IconContent("RotateTool"),
							new GUIStyle("Button"),
							GUILayout.Width(30),
							GUILayout.Width(width),
							GUILayout.Height(height)
							);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndArea();
			}
			Handles.EndGUI();
		}

		private void DrawGizmos()
		{
			MotionMatchingData data = editor.EditedData;
			Transform transform = editor.CurrentGameObject.transform;
			float animationTime = editor.LeftMenuEditor.AnimationTime;

			if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Contacts) != 0)
			{
				List<FrameContact> cpList = new List<FrameContact>();
				data.GetContactPoints(ref cpList, animationTime);

				Handles.color = Color.red;

				for (int i = 0; i < cpList.Count; i++)
				{
					Vector3 cpPos = transform.TransformPoint(cpList[i].position);

					Handles.DrawWireCube(cpPos, Vector3.one * gizmosCubeSize);
					Vector3 cpRSN = transform.TransformDirection(cpList[i].normal);

					MM_Gizmos.DrawArrowHandles(cpPos, cpRSN.normalized, arrowLength, arrowArmLength);
				}
			}

			if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Pose) != 0 ||
				(GizmosToDraw & MotionMatchingDataEditorGizmos.PoseVelocity) != 0)
			{
				PoseData p = new PoseData(data[0].pose.Count);
				data.GetPoseInTime(ref p, animationTime);
				p.TransformToWorldSpace(transform);


				if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Pose) != 0)
				{
					MM_Gizmos.DrawPose(p, Color.magenta, Color.yellow, gizmosCubeSize);
				}

				if ((GizmosToDraw & MotionMatchingDataEditorGizmos.PoseVelocity) != 0)
				{
					MM_Gizmos.DrawPoseVelocities(p, Color.yellow, 0.1f);
				}
			}

			if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Trajectory) != 0 ||
				(GizmosToDraw & MotionMatchingDataEditorGizmos.TrajectoryVelocities) != 0)
			{
				Handles.color = Color.cyan;
				MM_Gizmos.DrawHandlesWireSphere(transform.position + Vector3.up * gizmosCubeSize, gizmosCubeSize);
				Trajectory trajectory = new Trajectory(data.trajectoryPointsTimes.Count);
				data.GetTrajectoryInTime(ref trajectory, animationTime);
				trajectory.TransformToWorldSpace(transform);

				if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Trajectory) != 0)
				{
					Handles.color = Color.green;
					MM_Gizmos.DrawTrajectory_Handles(
						data.trajectoryPointsTimes.ToArray(),
						transform.position,
						transform.forward,
						trajectory,
						gizmosCubeSize,
						0.2f
						);
				}

				if ((GizmosToDraw & MotionMatchingDataEditorGizmos.TrajectoryVelocities) != 0)
				{
					Handles.color = Color.blue;
					MM_Gizmos.DrawTrajectoryVelocities_Handles(trajectory.points);
				}

			}

			if ((GizmosToDraw & MotionMatchingDataEditorGizmos.Tracks) != 0)
			{
				if (data.BoneTracks != null)
				{
					foreach (BoneTrack track in data.BoneTracks)
					{
						if (track != null && track.DrawGizmos)
						{
							if (track.Intervals != null)
							{
								Handles.color = Color.red;

								float currentTime = editor.LeftMenuEditor.AnimationTime;
								float samplingTime = track.TrackSettings.SamplingTime;
								int floorDataIndex = Mathf.FloorToInt(currentTime / samplingTime);

								BoneTrackAccesData accesData = track.GetTrackAccesData(animationTime);


								MM_Gizmos.DrawHandlesWireSphere(
									editor.CurrentGameObject.transform.TransformPoint(
										accesData.Data.Postion
										),
									0.05f
									);
							}
						}
					}

				}
			}

			if ((GizmosToDraw & MotionMatchingDataEditorGizmos.CharacterVelocity) != 0)
			{
				FrameData frame = data.GetClossestFrame(animationTime);

				if (frame.Velocity != Vector3.zero)
				{
					float velMagnitude = frame.Velocity.magnitude;
					Handles.color = Color.cyan;
					MM_Gizmos.DrawArrow_Handles(
						transform.position,
						transform.TransformDirection(frame.Velocity).normalized,
						velMagnitude,
						0.3f * velMagnitude
						);
				}
			}
		}
		#endregion
	}
}