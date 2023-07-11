using MotionMatching.Gameplay;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static MotionMatching.Tools.MotionMatchingDataEditor;

namespace MotionMatching.Tools
{
	[System.Serializable]
	public class MMDataEditorLeftMenu : MMDataEditorMenuBaseClass
	{

		Vector2 scroll = Vector2.zero;

		// Timelien rect settings
		Rect timelineRect;
		Rect animationTimeRect;
		Rect currentAnimationTimeRect;

		public const float TimelineHeight = 20f;

		private const float MinAmiationDeltaTime = 0.001f;
		public float realAnimationTime = 0f;
		public float AnimationTime
		{
			get
			{
				return realAnimationTime;
			}
			set
			{
				realAnimationTime = value;
			}
		}

		public const float TimelineHorizontalDefaultOffset = 80f;
		public const float VerticalOfssetFromTimeline = 10f;
		public float TimelineOffset = 0f;

		public const float PixelsPerSeconds = 80f;
		public const int spacesInSecond = 5;

		public const float SpacefromTimeline = 15f;

		public const float sectionsDownMenuHeight = 35f;

		SectionDrawer sectionDrawer;

		public override void OnEnable()
		{
			timelineRect = new Rect(0, 0, 100, 0);

			ResetZoom();

			sectionDrawer = new SectionDrawer();

			PlayableGraphOnEnable();
		}

		public override void OnGUI(
			Event e,
			Rect rect
			)
		{
			GUILayout.BeginArea(rect);
			{
				if (editor.EditedData != null)
				{

					mainRect = rect;
					DrawAnimationTimelineBackground(e, rect);
					DrawTimelineLines(rect);

					switch (editor.CurrentTool)
					{
						case MotionMatchingDataEditingTool.Sections:
							{
								DrawSections(rect, e);
							}
							break;
						case MotionMatchingDataEditingTool.Contacts:
							{
								DrawContact(rect, e);
							}
							break;
						case MotionMatchingDataEditingTool.Curves:
							{
								DrawCurves(rect, e);
							}
							break;
						case MotionMatchingDataEditingTool.BoneTracks:
							{
								DrawBoneTracks(rect, e);
							}
							break;
						case MotionMatchingDataEditingTool.AnimationEvents:
							{
								DrawEventsGUILayout(rect, e);
							}
							break;
						case MotionMatchingDataEditingTool.AnimationSpeedCurve:
							{
								DrawAnimationSpeedCurveOption(rect, e);
							}
							break;
					}

					DrawCurrentAnimationTimeRect(rect);

					isZoomBlocked = false;
					TimelineInput(e, rect);
				}


				//DrawMenuFrame(editor.LeftMenuRect, rect);

				AnimationUpdateLoop();
				AnimationUpdate();
			}
			GUILayout.EndArea();
		}

		#region Sections menu
		private void DrawSections(Rect rect, Event e)
		{
			if (sectionDrawer.section == null) return;

			if (sectionDrawer.section.timeIntervals == null)
			{
				sectionDrawer.section.timeIntervals = new List<float2>();
			}

			Rect layoutRect = rect;
			layoutRect.y = timelineRect.y + timelineRect.height + VerticalOfssetFromTimeline;
			layoutRect.height = rect.height - sectionsDownMenuHeight - timelineRect.y - timelineRect.height - VerticalOfssetFromTimeline;

			GUILayout.BeginArea(layoutRect);
			{
				scroll = GUILayout.BeginScrollView(scroll);
				{
					if (sectionDrawer.section.timeIntervals.Count == 0)
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(20f);
							GUILayout.Label($"Selected section \"{sectionDrawer.section.sectionName}\" have no intervals!");
						}
						GUILayout.EndHorizontal();
					}
					else
					{
						Rect animationRectToSectionDrawing = animationTimeRect;
						animationRectToSectionDrawing.x -= rect.x;

						sectionDrawer.SetData(
							editor,
							rect,
							Zoom,
							animationRectToSectionDrawing,
							editor.TimelineAnimationTimeTexture,
							editor.TimelineBackgroundTexture
							);
						float sliderHeight = 15f;
						float controllHeight = 20f;

						sectionDrawer.Draw(e, sliderHeight, controllHeight);

					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndArea();


			Rect layoutsBorderRect = new Rect(
				layoutRect.x,
				layoutRect.y + layoutRect.height,
				layoutRect.width,
				4f
				);
			GUI.DrawTexture(layoutsBorderRect, editor.LayoutBorderTexture);
			Rect secondLayoutRect = new Rect(
				layoutRect.x,
				layoutsBorderRect.y + layoutsBorderRect.height,
				layoutRect.width,
				sectionsDownMenuHeight - layoutsBorderRect.height
				);
			GUILayout.BeginArea(secondLayoutRect);
			{
				DrawIntervalsButtons();
			}
			GUILayout.EndArea();
		}

		private void DrawIntervalsButtons()
		{
			GUILayout.Space(3f);
			GUILayout.BeginHorizontal();
			{
				float margin = 10f;
				float buttonHeight = 24;
				GUILayout.Space(margin);
				if (GUILayout.Button("Sort intervals", GUILayout.Height(buttonHeight)))
				{
					sectionDrawer.Section.timeIntervals.Sort(delegate (float2 x, float2 y)
					{
						if (x.x < y.x)
						{
							return -1;
						}
						else if (x.x > y.x)
						{
							return 1;
						}
						return 0;
					});
				}
				GUILayout.Space(margin);
				if (GUILayout.Button("Add interval", GUILayout.Height(buttonHeight)))
				{
					if (sectionDrawer.Section.timeIntervals == null)
					{
						sectionDrawer.Section.timeIntervals = new List<float2>();
					}

					sectionDrawer.Section.timeIntervals.Add(new float2(0, editor.EditedData.animationLength));
				}
				GUILayout.Space(margin);
				if (GUILayout.Button("Remove interval", GUILayout.Height(buttonHeight)))
				{
					DataSection section = sectionDrawer.section;
					if (section != null)
					{
						if (sectionDrawer.IsIntervalSelected &&
							0 <= sectionDrawer.SelectedInterval && sectionDrawer.SelectedInterval < section.timeIntervals.Count)
						{
							section.timeIntervals.RemoveAt(sectionDrawer.SelectedInterval);
							sectionDrawer.SelectedInterval = -1;
							sectionDrawer.IsIntervalSelected = false;
						}
					}
				}
				GUILayout.Space(margin);

				if (GUILayout.Button("Clear intervals", GUILayout.Height(buttonHeight)))
				{
					DataSection section = sectionDrawer.section;
					if (section != null)
					{
						section.timeIntervals.Clear();
					}
				}
				GUILayout.Space(margin);
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region Curves
		public MotionMatchingDataCurve SelectedCurve { get; set; }

		private void DrawCurves(Rect rect, Event e)
		{
			if (SelectedCurve == null) return;



			Rect controllsRect = new Rect(
				rect.x,
				timelineRect.y + timelineRect.height + VerticalOfssetFromTimeline,
				rect.width,
				50f
				);

			GUILayout.BeginArea(controllsRect);
			{
				GUILayout.BeginHorizontal();
				{
					float mainMargin = 20f;
					GUILayout.Space(mainMargin);
					GUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Name", GUILayout.Width(100f));
							SelectedCurve.Name = EditorGUILayout.TextField(SelectedCurve.Name);
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Key value", GUILayout.Width(100f));
							SelectedCurve.currentKeyValue_editorOnly = EditorGUILayout.FloatField(
								SelectedCurve.currentKeyValue_editorOnly,
								GUILayout.Width(100f)
								);

							if (GUILayout.Button("SetKey", GUILayout.Width(100)))
							{
								for (int i = 0; i < SelectedCurve.Curve.keys.Length; i++)
								{
									Keyframe keyframe = SelectedCurve.Curve.keys[i];
									if (keyframe.time == AnimationTime)
									{
										SelectedCurve.Curve.RemoveKey(i);
										break;
									}
								}
								SelectedCurve.Curve.AddKey(new Keyframe(AnimationTime, SelectedCurve.currentKeyValue_editorOnly));
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
					GUILayout.Space(mainMargin);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();

			Rect curveRect = animationTimeRect;
			curveRect.y = controllsRect.y + controllsRect.height;
			curveRect.height = rect.height - controllsRect.height - controllsRect.y - VerticalOfssetFromTimeline;
			curveRect.width = animationTimeRect.width;

			DrawCurveNormalLayout(curveRect, SelectedCurve);
		}

		private void DrawCurveNormalLayout(Rect rect, MotionMatchingDataCurve curve)
		{
			if (curve.Curve.length == 1)
			{
				Keyframe keyframe = curve.Curve[0];
				curve.Curve.AddKey(0f, keyframe.value);
				curve.Curve.AddKey(editor.EditedData.animationLength, keyframe.value);
			}
			else if (curve.Curve.length >= 2)
			{
				Keyframe first = curve.Curve[0];
				Keyframe last = curve.Curve[curve.Curve.length - 1];

				if (first.time != 0)
				{
					curve.Curve.AddKey(0f, first.value);
				}
				if (first.time != editor.EditedData.animationLength)
				{
					curve.Curve.AddKey(editor.EditedData.animationLength, last.value);
				}
			}
			else if (curve.Curve.length == 0)
			{
				curve.Curve.AddKey(0f, 0f);
				curve.Curve.AddKey(editor.EditedData.animationLength, 0f);
			}

			for (int i = 0; i < curve.Curve.length; i++)
			{
				Keyframe key = curve.Curve[i];

				if (key.time < 0 || editor.EditedData.animationLength < key.time)
				{
					curve.Curve.RemoveKey(i);
					i--;
				}
			}

			curve.Curve = EditorGUI.CurveField(rect, curve.Curve);
		}

		#endregion

		#region Events:

		public MotionMatchingAnimationEvent SelectedEvent { get; private set; }
		List<Rect> eventsControllsRects = new List<Rect>();
		private void DrawEventsGUILayout(Rect rect, Event e)
		{
			Rect layoutRect = rect;
			layoutRect.y = timelineRect.y + timelineRect.height;
			layoutRect.height = rect.height - sectionsDownMenuHeight - timelineRect.height - timelineRect.y;// - VerticalOfssetFromTimeline;

			#region sorting events
			if (editor.EditedData.AnimationEvents != null)
			{
				editor.EditedData.AnimationEvents.Sort((x, y) =>
				{
					if (x == null && y != null)
					{
						return 1;
					}
					else if (y == null && x != null)
					{
						return -1;
					}
					else if (x == null && y == null)
					{
						return 0;
					}
					else if (x.EventTime < y.EventTime)
					{
						return -1;
					}
					else if (x.EventTime > y.EventTime)
					{
						return 1;
					}
					return 0;
				});
			}
			#endregion

			GUILayout.BeginArea(layoutRect);
			{
				scroll = GUILayout.BeginScrollView(scroll);
				{
					DrawEventsFirstLayout(layoutRect);
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndArea();



			Rect layoutsBorderRect = new Rect(
				layoutRect.x,
				layoutRect.y + layoutRect.height,
				layoutRect.width,
				4f
				);
			GUI.DrawTexture(layoutsBorderRect, editor.LayoutBorderTexture);
			Rect secondLayoutRect = new Rect(
				layoutRect.x,
				layoutsBorderRect.y + layoutsBorderRect.height,
				layoutRect.width,
				sectionsDownMenuHeight - layoutsBorderRect.height
				);
			GUILayout.BeginArea(secondLayoutRect);
			{
				DrawDownEventsMenu();
			}
			GUILayout.EndArea();
		}

		private void DrawEventsFirstLayout(Rect layoutRect)
		{
			const float eventGUIHeight = 30f;

			const float controllHeight = 23f;

			const float eventTimeRectWidth = 16f;

			if (eventsControllsRects == null) { eventsControllsRects = new List<Rect>(); }
			else { eventsControllsRects.Clear(); }

			GUILayout.Space(10);
			GUILayout.Label("", GUILayout.Height(eventGUIHeight));
			Rect lastRect = GUILayoutUtility.GetLastRect();

			MotionMatchingAnimationEvent lastEvent = null;

			float margin = 5f;

			if (editor.EditedData.AnimationEvents == null)
			{
				editor.EditedData.AnimationEvents = new List<MotionMatchingAnimationEvent>();
			}

			for (int eventIndex = 0; eventIndex < editor.EditedData.AnimationEvents.Count; eventIndex++)
			{
				MotionMatchingAnimationEvent animationEvent = editor.EditedData.AnimationEvents[eventIndex];

				Rect eventTimeRect = new Rect(
						animationTimeRect.x + animationTimeRect.width * animationEvent.EventTime / editor.EditedData.animationLength - eventTimeRectWidth / 2f - mainRect.x,
						lastRect.y + (eventGUIHeight - controllHeight),
						eventTimeRectWidth,
						controllHeight
						);

				if (lastEvent != null)
				{
					GUILayout.Label("", GUILayout.Height(eventGUIHeight + margin * 2f - 2.1f));
					Rect lastEventTimeRect = eventsControllsRects[eventIndex * 3 - 3];
					Rect timeUpdateRect = eventsControllsRects[eventIndex * 3 - 2];
					if (lastEvent.EventTime == animationEvent.EventTime)
					{
						eventTimeRect.y = timeUpdateRect.y + timeUpdateRect.height + margin;
					}
					else if (WidthOverlap(eventTimeRect, lastEventTimeRect))
					{
						eventTimeRect.y = timeUpdateRect.y + timeUpdateRect.height + margin;
					}
				}

				Rect lineToTimeline = new Rect(
					animationTimeRect.x + animationTimeRect.width * animationEvent.EventTime / editor.EditedData.animationLength - mainRect.x,
					0f,
					1f,
					eventTimeRect.y + eventTimeRect.height
					);

				GUI.DrawTexture(lineToTimeline, editor.WhiteTexture);
				if (animationEvent == SelectedEvent)
				{
					GUI.DrawTexture(lineToTimeline, editor.SelectedEventTexture);
				}

				//float updateTImeRectWidt = 17f;
				Rect eventTimeUpdateRect = new Rect(
					eventTimeRect.x,
					eventTimeRect.y + eventTimeRect.height,
					eventTimeRect.width,
					controllHeight / 2f
					);


				Rect eventLabelRect = new Rect(
					eventTimeRect.x + eventTimeRect.width,
					eventTimeRect.y,
					layoutRect.width - eventTimeRect.x - eventTimeRect.width,
					eventTimeRect.height
					);



				eventsControllsRects.Add(eventTimeRect);
				eventsControllsRects.Add(eventTimeUpdateRect);
				eventsControllsRects.Add(eventLabelRect);


				lastEvent = animationEvent;
			}


			int rectIndex = 0;
			for (int eventIndex = 0; eventIndex < editor.EditedData.AnimationEvents.Count; eventIndex++)
			{
				MotionMatchingAnimationEvent animationEvent = editor.EditedData.AnimationEvents[eventIndex];

				if (GUI.Button(eventsControllsRects[rectIndex], "|"))
				{
					if (SelectedEvent == animationEvent)
					{
						SelectedEvent = null;
					}
					else
					{
						SelectedEvent = animationEvent;
					}
					//animationEvent.EventTime = AnimationTime;
				}
				if (SelectedEvent == animationEvent)
				{
					GUI.DrawTexture(eventsControllsRects[rectIndex], editor.SelectedEventTexture);
				}

				if (AnimationTime != animationEvent.EventTime)
				{



					if (GUI.Button(
						eventsControllsRects[rectIndex + 1],
						"^"
						))
					{
						animationEvent.EventTime = AnimationTime;
					}
				}

				if (SelectedEvent == animationEvent)
				{
					GUI.Label(eventsControllsRects[rectIndex + 2], $"{animationEvent.Name} - {animationEvent.EventTime} s");
				}

				rectIndex += 3;
			}
		}

		private void DrawDownEventsMenu()
		{
			float buttonSpace = 10f;
			float buttonHeight = 25f;
			GUILayout.Space(2.5f);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(buttonSpace);
				if (GUILayout.Button("Add event", GUILayout.Height(buttonHeight)))
				{
					editor.EditedData.AnimationEvents.Add(new MotionMatchingAnimationEvent("<AnimationEvent>", AnimationTime));
					SelectedEvent = editor.EditedData.AnimationEvents[editor.EditedData.AnimationEvents.Count - 1];
				}
				GUILayout.Space(buttonSpace);
				if (GUILayout.Button("Remove event", GUILayout.Height(buttonHeight)))
				{
					if (SelectedEvent != null)
					{
						int selectedEventIndex = editor.EditedData.AnimationEvents.IndexOf(SelectedEvent);
						editor.EditedData.AnimationEvents.Remove(SelectedEvent);

						if (editor.EditedData.AnimationEvents.Count > 0)
						{
							if (editor.EditedData.AnimationEvents.Count <= selectedEventIndex)
							{
								SelectedEvent = editor.EditedData.AnimationEvents[editor.EditedData.AnimationEvents.Count - 1];
							}
							else if (editor.EditedData.AnimationEvents.Count > 0)
							{
								SelectedEvent = editor.EditedData.AnimationEvents[selectedEventIndex];
							}
						}
						else
						{
							SelectedEvent = null;
						}
					}
				}
				GUILayout.Space(buttonSpace);
				if (GUILayout.Button("Clear events", GUILayout.Height(buttonHeight)))
				{
					editor.EditedData.AnimationEvents.Clear();
					SelectedEvent = null;
				}

				GUILayout.Space(buttonSpace);
			}
			GUILayout.EndHorizontal();
		}

		#endregion

		#region Contacts

		private void DrawContact(Rect rect, Event e)
		{
			Rect layoutRect = rect;
			layoutRect.y = timelineRect.y + timelineRect.height + VerticalOfssetFromTimeline;
			layoutRect.height = rect.height - /*sectionsDownMenuHeight -*/ timelineRect.height - VerticalOfssetFromTimeline;

			Rect startMoveTimeRect = new Rect();
			GUILayout.BeginArea(layoutRect);
			{
				if (0 <= editor.RightMenuEditor.SelectedContactIndex &&
					editor.RightMenuEditor.SelectedContactIndex < editor.EditedData.contactPoints.Count)
				{
					DrawContactGUI(layoutRect, e, out startMoveTimeRect);
				}
			}
			GUILayout.EndArea();

			if (0 <= editor.RightMenuEditor.SelectedContactIndex &&
				editor.RightMenuEditor.SelectedContactIndex < editor.EditedData.contactPoints.Count)
			{
				MotionMatchingContact contact = editor.EditedData.contactPoints[editor.RightMenuEditor.SelectedContactIndex];

				Rect startMoveToTimeLineToTimelineRect = new Rect(
					animationTimeRect.x + contact.StartMoveToContactTime / editor.EditedData.animationLength * animationTimeRect.width,
					animationTimeRect.y + animationTimeRect.height,
					1f,
					startMoveTimeRect.y + layoutRect.y - (animationTimeRect.y + animationTimeRect.height)
					);


				GUI.DrawTexture(startMoveToTimeLineToTimelineRect, editor.BlackTexture);
			}


			//Rect layoutsBorderRect = new Rect(
			//	layoutRect.x,
			//	layoutRect.y + layoutRect.height,
			//	layoutRect.width,
			//	4f
			//	);
			//GUI.DrawTexture(layoutsBorderRect, layoutBorderTexture);
			//Rect secondLayoutRect = new Rect(
			//	layoutRect.x,
			//	layoutsBorderRect.y + layoutsBorderRect.height,
			//	layoutRect.width,
			//	sectionsDownMenuHeight - layoutsBorderRect.height
			//	);
			//GUILayout.BeginArea(secondLayoutRect);
			//{
			//	DrawContactsDownLayout();
			//}
			//GUILayout.EndArea();
		}

		private void DrawContactGUI(Rect rect, Event e, out Rect startMoveTimeRect)
		{
			Rect animationTimeRect = this.animationTimeRect;
			animationTimeRect.x -= mainRect.x;

			float verticalMargin = 5f;

			MotionMatchingContact contact = editor.EditedData.contactPoints[editor.RightMenuEditor.SelectedContactIndex];

			Rect lastLayoutRect = DrawCustomRangeSlider(rect.width, ref contact.startTime, ref contact.endTime, false);

			float buttonWidth = 14f;

			//#region contact range interval
			//float deltaToFitWithTimeline = 5f;
			//float valueWidth = 50f;

			//float sliderHeight = 15f;
			//float controllHeight = 20f;
			//float sliderYDelta = -3f;

			//float intervalHeight = sliderHeight + controllHeight;

			//GUILayout.Label("", GUILayout.Height(intervalHeight), GUILayout.Width(1f));

			//Rect lastLayoutRect = GUILayoutUtility.GetLastRect();

			//Rect backgroundRect = new Rect(
			//	0,
			//	lastLayoutRect.y,
			//	rect.width,
			//	lastLayoutRect.height
			//	);

			//GUI.DrawTexture(backgroundRect, editor.TimelineBackgroundTexture);

			//Rect timeRect = new Rect(
			//	animationTimeRect.x,
			//	lastLayoutRect.y,
			//	animationTimeRect.width,
			//	lastLayoutRect.height
			//	);
			//GUI.DrawTexture(timeRect, editor.TimelineAnimationTimeTexture);


			//Rect startValue = new Rect(
			//	animationTimeRect.x + contact.startTime / editor.EditedData.animationLength * animationTimeRect.width - valueWidth - buttonWidth - deltaToFitWithTimeline + mainRect.x,
			//	lastLayoutRect.y + sliderHeight,
			//	valueWidth,
			//	controllHeight
			//	);


			//Rect startButton = new Rect(
			//	startValue.x + startValue.width,
			//	startValue.y,
			//	buttonWidth,
			//	controllHeight
			//	);
			////startValue.x = Mathf.Clamp(mainRect.x, mainRect.)


			//Rect endButton = new Rect(
			//	animationTimeRect.x + contact.endTime / editor.EditedData.animationLength * animationTimeRect.width - deltaToFitWithTimeline + mainRect.x,
			//	lastLayoutRect.y + sliderHeight,
			//	buttonWidth,
			//	controllHeight
			//	);

			//Rect endValue = new Rect(
			//	endButton.x + buttonWidth,
			//	lastLayoutRect.y + sliderHeight,
			//	valueWidth,
			//	controllHeight
			//	);


			//Rect minMaxSliderRect = new Rect(
			//	animationTimeRect.x - deltaToFitWithTimeline,
			//	lastLayoutRect.y + sliderYDelta,
			//	animationTimeRect.width + deltaToFitWithTimeline * 2f,
			//	15
			//	);

			//EditorGUI.MinMaxSlider(
			//	minMaxSliderRect,
			//	ref contact.startTime,
			//	ref contact.endTime,
			//	0,
			//	editor.EditedData.animationLength
			//	);

			//contact.startTime = Mathf.Clamp(
			//	EditorGUI.FloatField(startValue, contact.startTime),
			//	0,
			//	contact.endTime
			//	);

			//if (GUI.Button(startButton, "|"))
			//{
			//	contact.startTime = Mathf.Clamp(
			//		editor.LeftMenuEditor.AnimationTime,
			//		0,
			//		contact.endTime
			//		);
			//}

			//contact.endTime = Mathf.Clamp(
			//	EditorGUI.FloatField(endValue, contact.endTime),
			//	contact.startTime,
			//	editor.EditedData.animationLength
			//	);

			//if (GUI.Button(endButton, "|"))
			//{
			//	contact.endTime = Mathf.Clamp(
			//		editor.LeftMenuEditor.AnimationTime,
			//		contact.startTime,
			//		editor.EditedData.animationLength
			//		);
			//}
			//#endregion

			#region start move time 
			GUILayout.Space(5f);
			GUILayout.Label("", GUILayout.Height(30f));
			lastLayoutRect = GUILayoutUtility.GetLastRect();

			startMoveTimeRect = new Rect(
				animationTimeRect.x + contact.StartMoveToContactTime / editor.EditedData.animationLength * animationTimeRect.width - buttonWidth / 2f,
				lastLayoutRect.y,
				buttonWidth,
				20f
				);
			Rect startMoveTimeRectDescription = new Rect(
				startMoveTimeRect.x + startMoveTimeRect.width,
				lastLayoutRect.y,
				200f,
				startMoveTimeRect.height
				);


			if (GUI.Button(startMoveTimeRect, "|"))
			{
				contact.StartMoveToContactTime = Mathf.Clamp(
							AnimationTime,
							0f,
							contact.startTime
							);
				editor.Repaint();
			}

			GUI.Label(startMoveTimeRectDescription, "Start move to contact time");

			#endregion


			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(TimelineHorizontalDefaultOffset);
				GUILayout.BeginVertical();
				{
					//GUILayout.Space(10);
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Name", GUILayout.Width(50f));
						contact.Name_EditorOnly = EditorGUILayout.TextField(contact.Name_EditorOnly, GUILayout.MaxWidth(200f));

						GUILayout.FlexibleSpace();

						if (GUILayout.Button("Reset orienation"))
						{
							contact.rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.down);
							contact.contactNormal = Vector3.down;

						}
					}
					GUILayout.EndHorizontal();


					GUILayout.Space(verticalMargin);

					GUILayout.BeginHorizontal();
					{
						contact.StartMoveToContactTime = Mathf.Clamp(
							EditorGUILayout.FloatField("Start move to contact time", contact.StartMoveToContactTime, GUILayout.Width(500)),
							0f,
							contact.startTime
							);
					}
					GUILayout.EndHorizontal();

					GUILayout.Space(verticalMargin);
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Position", GUILayout.Width(50f));
						contact.position = EditorGUILayout.Vector3Field("", contact.position);

						//GUILayout.Space(verticalMargin);
						//GUILayout.Label("Rotation", GUILayout.Width(40f));

						//Vector4 vector = new Vector4(
						//	contact.rotation.x,
						//	contact.rotation.y,
						//	contact.rotation.z,
						//	contact.rotation.w
						//	);
						//vector = EditorGUILayout.Vector4Field("", vector);

						//contact.rotation = new Quaternion(
						//	vector.x,
						//	vector.y,
						//	vector.z,
						//	vector.w
						//	);
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(verticalMargin);
				}
				GUILayout.EndVertical();
				GUILayout.Space(TimelineHorizontalDefaultOffset);
			}
			GUILayout.EndHorizontal();


			editor.EditedData.contactPoints[editor.RightMenuEditor.SelectedContactIndex] = contact;
		}

		#endregion

		#region BoneTracks
		public int BoneTrackSelectedIntervalIndex { get; private set; } = -1;
		public bool IsBoneTrackIntervalSelected { get; private set; } = false;

		private void DrawBoneTracks(Rect rect, Event e)
		{
			BoneTrack track = editor.RightMenuEditor.SelectedBoneTrack;

			if (track == null) return;
			if (track.Intervals == null) track.Intervals = new List<BoneTrackInterval>();

			float spaceBetweenIntervals = 5f;

			Rect layoutRect = rect;
			layoutRect.y = timelineRect.y + timelineRect.height + VerticalOfssetFromTimeline;
			layoutRect.height = rect.height - timelineRect.y - timelineRect.height - VerticalOfssetFromTimeline;

			GUILayout.BeginArea(layoutRect);
			{
				GUILayout.BeginVertical();
				{
					GUILayout.Space(VerticalOfssetFromTimeline);

					scroll = GUILayout.BeginScrollView(scroll);
					{

						for (int intervalIndex = 0; intervalIndex < track.Intervals.Count; intervalIndex++)
						{
							BoneTrackInterval interval = track.Intervals[intervalIndex];

							Rect intervalRect = DrawCustomRangeSlider(layoutRect.width, ref interval.TimeInterval.x, ref interval.TimeInterval.y, BoneTrackSelectedIntervalIndex == intervalIndex);
							GUILayout.Space(spaceBetweenIntervals);

							switch (e.type)
							{
								case EventType.MouseDown:
									{
										if (intervalRect.Contains(e.mousePosition))
										{
											if (IsBoneTrackIntervalSelected)
											{
												if (intervalIndex == BoneTrackSelectedIntervalIndex)
												{
													IsBoneTrackIntervalSelected = false;
													BoneTrackSelectedIntervalIndex = -1;
												}
												else
												{
													BoneTrackSelectedIntervalIndex = intervalIndex;
												}
											}
											else
											{
												IsBoneTrackIntervalSelected = true;
												BoneTrackSelectedIntervalIndex = intervalIndex;
											}
											editor.Repaint();
										}
									}
									break;
							}


							track.Intervals[intervalIndex] = interval;
						}


						for (int intervalIndex = 0; intervalIndex < track.Intervals.Count; intervalIndex++)
						{
							BoneTrackInterval interval = track.Intervals[intervalIndex];
							interval.StarTime = intervalIndex == 0 ? 0f : track.Intervals[intervalIndex - 1].TimeInterval.y;
						}
					}
					GUILayout.EndScrollView();
					GUILayout.FlexibleSpace();


					float spaceBetweenButtons = 5f;
					GUILayout.Space(spaceBetweenButtons);


					float buttonHeight = 25f;
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(spaceBetweenButtons);
						if (GUILayout.Button("Add interval", GUILayout.Height(buttonHeight)))
						{
							if (track.Intervals.Count > 0)
							{
								track.Intervals.Add(new BoneTrackInterval(
									0f,
									new float2(track.Intervals[track.Intervals.Count - 1].TimeInterval.y, editor.EditedData.animationLength)
									));
							}
							else
							{
								track.Intervals.Add(new BoneTrackInterval(
									0f,
									new float2(0f, editor.EditedData.animationLength)
									));
							}
						}

						GUILayout.Space(spaceBetweenButtons);
						if (GUILayout.Button("Remove interval", GUILayout.Height(buttonHeight)))
						{
							if (0 <= BoneTrackSelectedIntervalIndex && BoneTrackSelectedIntervalIndex < track.Intervals.Count)
							{
								track.Intervals.RemoveAt(BoneTrackSelectedIntervalIndex);
								BoneTrackSelectedIntervalIndex -= 1;
							}
						}

						GUILayout.Space(spaceBetweenButtons);
						if (GUILayout.Button("Sort intervals", GUILayout.Height(buttonHeight)))
						{
							track.Intervals.Sort((x, y) =>
							{
								if (x.TimeInterval.x < y.TimeInterval.x)
								{
									return -1;
								}
								return 1;
							});
						}

						GUILayout.Space(spaceBetweenButtons);
						if (GUILayout.Button("Clear intervals", GUILayout.Height(buttonHeight)))
						{
							track.Intervals.Clear();
						}
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(spaceBetweenButtons / 2f);
				}
				GUILayout.EndVertical();


			}
			GUILayout.EndArea();


		}
		#endregion

		public void DrawAnimationTimelineBackground(
			Event e,
			Rect rect
			)
		{
			timelineRect = new Rect(
				rect.x,
				rect.y,
				rect.width,
				TimelineHeight
				);

			GUI.DrawTexture(timelineRect, editor.TimelineBackgroundTexture);
		}

		private void DrawCurrentAnimationTimeRect(
			Rect rect
			)
		{
			if (editor.EditedData != null)
			{
				float animationLengthInPixels = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

				//currentAnimationTimeRect = new Rect(
				//	rect.x + animationLengthInPixels * (AnimationTime / editor.EditedData.animationLength) + TimelineOffset,
				//	rect.y,
				//	1,
				//	rect.height
				//	);

				currentAnimationTimeRect = new Rect(
					TimelineOffset + rect.x + animationLengthInPixels * (AnimationTime / editor.EditedData.animationLength),
					timelineRect.y,
					1,
					rect.height
					);

				switch (editor.CurrentTool)
				{
					case MotionMatchingDataEditingTool.Sections:
						{
							if (sectionDrawer.section != null)
							{
								currentAnimationTimeRect.height = rect.height - sectionsDownMenuHeight - timelineRect.y;
							}
						}
						break;
					case MotionMatchingDataEditingTool.Contacts:
						{
						}
						break;
					case MotionMatchingDataEditingTool.Curves:
						{

						}
						break;
					case MotionMatchingDataEditingTool.BoneTracks:
						{

						}
						break;
					case MotionMatchingDataEditingTool.AnimationEvents:
						{
							currentAnimationTimeRect.height = rect.height - sectionsDownMenuHeight - timelineRect.y;
						}
						break;
				}


				GUI.DrawTexture(currentAnimationTimeRect, editor.TimelinePointerTexture);


				float width = 50f;
				Rect floatFieldRect = new Rect(
					currentAnimationTimeRect.x - width / 2f,
					currentAnimationTimeRect.y - 5f - 20f,
					width,
					20f
					);

				AnimationTime = Mathf.Clamp(
					EditorGUI.FloatField(floatFieldRect, AnimationTime),
					0,
					editor.EditedData.animationLength
					);
			}
		}

		public void DrawMenuFrame(Rect fullRect, Rect withMargin)
		{

			//Rect upFrame = new Rect(
			//	fullRect.x,
			//	fullRect.y,
			//	fullRect.width, 
			//	MotionMatchingDataEditor_New.AreasMargin
			//	);
			Rect downFrame = new Rect(
				fullRect.x,
				fullRect.y + withMargin.height,
				fullRect.width,
				MotionMatchingDataEditor.AreasMargin
				);
			Rect leftFrame = new Rect(
				fullRect.x,
				withMargin.y,
				AreasMargin,
				withMargin.height
				);
			Rect rightFrame = new Rect(
				fullRect.x + AreasMargin + withMargin.width,
				withMargin.y,
				AreasMargin,
				withMargin.height
				);

			//GUI.DrawTexture(upFrame, editor.TopBarTexture);
			//GUI.DrawTexture(downFrame, editor.TopBarTexture);
			GUI.DrawTexture(leftFrame, editor.TopBarTexture);
			GUI.DrawTexture(rightFrame, editor.TopBarTexture);
		}


		bool isChangingAnimationTime = false;
		bool isMoving = false;
		float isMovingMouseStartXPos;
		float oldOffset;
		float timelineZoomStepFactor = 0.05f;
		public const float MinZoom = 0.001f;
		public const float MaxZoom = 100f;
		public float Zoom { get; set; } = 1f;
		bool isZoomBlocked = false;

		private void TimelineInput(
			Event e,
			Rect rect
			)

		{
			int controlId = GUIUtility.GetControlID(FocusType.Passive);

			switch (e.GetTypeForControl(controlId))
			{
				case EventType.MouseDown:
					{
						if (timelineRect.Contains(e.mousePosition) && editor.EditedData != null && e.button == 0)
						{
							GUIUtility.hotControl = controlId;
							isChangingAnimationTime = true;

							float animationLengthInPixels = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

							Vector2 mousePos = e.mousePosition;
							AnimationTime = Mathf.Clamp(
								((mousePos.x - rect.x - TimelineOffset)) / animationLengthInPixels * editor.EditedData.animationLength,
								0,
								editor.EditedData.animationLength
								);

							editor.Repaint();
						}
						else if (e.button == 2)
						{
							GUIUtility.hotControl = controlId;
							isMoving = true;
							isMovingMouseStartXPos = e.mousePosition.x;
							oldOffset = TimelineOffset;
						}
					}
					break;
				case EventType.MouseDrag:
					{
						if (isChangingAnimationTime && editor.EditedData != null)
						{
							editor.Repaint();

							float animationDeltaTime = e.delta.x / (PixelsPerSeconds * Zoom) * Mathf.Clamp01(AnimationPlaybackSpeed);

							AnimationTime = Mathf.Clamp(
								AnimationTime + animationDeltaTime,
								0,
								editor.EditedData.animationLength
								);

						}
					}
					break;
				case EventType.MouseUp:
					{
						if (e.button == 0)
						{
							isChangingAnimationTime = false;
						}
						else if (e.button == 2)
						{
							isMoving = false;
						}
					}
					break;
				case EventType.MouseLeaveWindow:
					{

					}
					break;
				case EventType.ScrollWheel:
					{
						if (rect.Contains(e.mousePosition))
						{
							float y = e.delta.y;

							if (y < 0)
							{
								isZoomBlocked = true;
								Zoom += (timelineZoomStepFactor * Zoom);
								editor.Repaint();

								float animationTimeBehindMouseFactor = (e.mousePosition.x - animationTimeRect.x) / animationTimeRect.width;
								float newAnimationRectWidth = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

								float timelineOffsetDelta = animationTimeBehindMouseFactor * newAnimationRectWidth;

								TimelineOffset -= (timelineOffsetDelta - (e.mousePosition.x - animationTimeRect.x));

							}
							else if (y > 0)
							{
								isZoomBlocked = true;
								Zoom -= (timelineZoomStepFactor * Zoom);
								editor.Repaint();

								float animationTimeBehindMouseFactor = (e.mousePosition.x - animationTimeRect.x) / animationTimeRect.width;
								float newAnimationRectWidth = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

								float timelineOffsetDelta = animationTimeBehindMouseFactor * newAnimationRectWidth;

								TimelineOffset -= (timelineOffsetDelta - (e.mousePosition.x - animationTimeRect.x));
							}

						}
					}
					break;
			}

			if (isChangingAnimationTime && editor.EditedData != null)
			{
				//editor.Repaint();

				float animationLengthInPixels = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

				//Vector2 mousePos = e.mousePosition;
				//AnimationTime = Mathf.Clamp(
				//	((mousePos.x - rect.x - TimelineOffset)) / animationLengthInPixels * editor.EditedData.animationLength,
				//	0,
				//	editor.EditedData.animationLength
				//	);
			}
			else if (isMoving)
			{
				editor.Repaint();

				TimelineOffset = oldOffset + (e.mousePosition.x - isMovingMouseStartXPos);


			}

		}

		public void AddDeltaToZoom(float delta)
		{
			if (isZoomBlocked) return;

			Zoom += delta;

			if (editor.EditedData != null)
			{
				float X = mainRect.x + mainRect.width / 2f;


				float animationTimeBehindMouseFactor = (X - animationTimeRect.x) / animationTimeRect.width;
				float newAnimationRectWidth = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

				float timelineOffsetDelta = animationTimeBehindMouseFactor * newAnimationRectWidth;

				TimelineOffset -= (timelineOffsetDelta - (X - animationTimeRect.x));
			}
		}

		private void DrawTimelineLines(
			Rect rect
			)
		{
			if (editor.EditedData != null)
			{
				float width = editor.EditedData.animationLength * PixelsPerSeconds * Zoom;

				float minOffset;
				if (width < rect.width - 2f * TimelineHorizontalDefaultOffset)
				{
					minOffset = TimelineHorizontalDefaultOffset;
				}
				else
				{
					minOffset = rect.width - TimelineHorizontalDefaultOffset - width;
				}

				TimelineOffset = Mathf.Clamp(
					TimelineOffset,
					minOffset,
					TimelineHorizontalDefaultOffset
					);

				float x = timelineRect.x + TimelineOffset;

				animationTimeRect = new Rect(
					x,
					timelineRect.y,
					width,
					timelineRect.height - 1
					);

				GUI.DrawTexture(animationTimeRect, editor.TimelineAnimationTimeTexture);

				Rect animationTimeDownLine = new Rect(
					timelineRect.x + TimelineOffset,
					animationTimeRect.y + animationTimeRect.height,
					editor.EditedData.animationLength * PixelsPerSeconds * Zoom,
					1f
					);

				GUI.DrawTexture(animationTimeDownLine, editor.BlackTexture);



				Rect mainLine = new Rect(0, timelineRect.y + timelineRect.height * 0.2f, 1, timelineRect.height * 0.8f);
				Rect secondaryLine = new Rect(0, timelineRect.y + timelineRect.height * 0.65f, 1, timelineRect.height * 0.35f);

				//float animationLengthInPixels = editor.EditedData.animationLength * PixelsPerSeconds * editor.TopBarEditor.Scale;
				float secondInPixels = PixelsPerSeconds * Zoom;

				float timeInterval = PixelsPerSeconds / secondInPixels;

				if (timeInterval >= 1)
				{
					timeInterval = Mathf.Ceil(timeInterval);
				}
				else if (timeInterval >= 0.1f)
				{
					timeInterval = Mathf.Ceil(timeInterval / 0.1f) * 0.1f;
				}
				else
				{
					timeInterval = Mathf.Ceil(timeInterval / 0.01f) * 0.01f;
				}


				int intervalsToDraw = Mathf.FloorToInt(editor.EditedData.animationLength / timeInterval);

				float pixelsForTimeInterval = secondInPixels * timeInterval;
				float pixelsForSecondarySpace = pixelsForTimeInterval / spacesInSecond;

				float minX = mainRect.x;
				float maxX = mainRect.x + mainRect.width;

				for (int i = 0; i < intervalsToDraw + 1; i++)
				{
					mainLine.x = rect.x + i * pixelsForTimeInterval + TimelineOffset;

					if (minX < mainLine.x && mainLine.x < maxX)
					{
						GUI.DrawTexture(mainLine, editor.BlackTexture);
					}
					Rect secondsRect = new Rect(
							mainLine.x + 1f,
							animationTimeRect.y - 4f,
							60f,
							20f
							);

					if (minX < secondsRect.x + secondsRect.width && mainLine.x < maxX)
					{
						GUI.Label(secondsRect, $"{i * timeInterval}");
					}

					if (i == intervalsToDraw)
					{
						float lastTimeAfterSecond = editor.EditedData.animationLength - i * timeInterval;

						float stepTime = 1f / (float)spacesInSecond * timeInterval;
						int steps = Mathf.CeilToInt(lastTimeAfterSecond / stepTime);

						for (int step = 1; step < steps; step++)
						{
							secondaryLine.x = mainLine.x + step * pixelsForSecondarySpace;
							if (minX < secondaryLine.x && secondaryLine.x < maxX)
							{
								GUI.DrawTexture(secondaryLine, editor.BlackTexture);
							}
						}
					}
					else
					{
						for (int sLine = 1; sLine < spacesInSecond; sLine++)
						{
							secondaryLine.x = mainLine.x + sLine * pixelsForSecondarySpace;

							if (minX < secondaryLine.x && secondaryLine.x < maxX)
							{
								GUI.DrawTexture(secondaryLine, editor.BlackTexture);
							}
						}
					}

				}
			}
		}

		#region utils:

		#endregion

		public void ResetZoom()
		{
			if (editor.EditedData != null)
			{
				isZoomBlocked = true;
				TimelineOffset = TimelineHorizontalDefaultOffset;
				float desiredAnimationTimeInPixels = mainRect.width - 2 * TimelineHorizontalDefaultOffset;
				float realAnimationTimeInPixels = editor.EditedData.animationLength * PixelsPerSeconds;

				Zoom = desiredAnimationTimeInPixels / realAnimationTimeInPixels;
				editor.Repaint();
			}
		}

		public void SetSectionToDraw(DataSection newSection)
		{
			sectionDrawer.Section = newSection;
			editor.Repaint();
		}


		#region Playing animation

		private float maxAnimationUpdateStep = 0.01f;
		public PreparingDataPlayableGraph PlayableGraph;
		public bool IsAnimationPlaying { get; set; } = false;
		float animationTimeBuffor;
		float lastApplicationTime;

		public float AnimationPlaybackSpeed { get; set; } = 1f;

		public bool LockPlaybackPosition { get; set; }
		public bool LockPlaybackRotation { get; set; }

		private void PlayableGraphOnEnable()
		{
			PlayableGraph = new PreparingDataPlayableGraph();
		}

		public void StartPreviewAnimation(float inTime)
		{
			if (editor.EditedData == null || editor.CurrentGameObject == null)
			{
				return;
			}


			if (PlayableGraph != null)
			{
				PlayableGraph.Destroy();
			}

			AnimationTime = inTime;
			animationTimeBuffor = inTime;

			PlayableGraph = new PreparingDataPlayableGraph();
			PlayableGraph.Initialize(editor.CurrentGameObject);
			PlayableGraph.CreateAnimationDataPlayables(editor.EditedData, inTime);
		}

		public void StartPlayingAnimation()
		{
			if (editor.EditedData == null || editor.CurrentGameObject == null)
			{
				return;
			}
			StartPreviewAnimation(AnimationTime);
			lastApplicationTime = Time.realtimeSinceStartup;
			IsAnimationPlaying = true;
		}

		public void StopPlayingAnimation()
		{
			IsAnimationPlaying = false;
		}

		private void AnimationUpdateLoop()
		{
			if (IsAnimationPlaying)
			{
				float deltaTime = Time.realtimeSinceStartup - lastApplicationTime;
				lastApplicationTime = Time.realtimeSinceStartup;

				if (editor.EditedData.UseAnimationSpeedCurve)
				{
					float speedMultiplier = editor.EditedData.AnimationSpeedCurve.Evaluate(AnimationTime);
					deltaTime *= speedMultiplier;
				}

				AnimationTime += (deltaTime * AnimationPlaybackSpeed);

				if (AnimationTime > editor.EditedData.animationLength)
				{
					AnimationTime = AnimationTime % editor.EditedData.animationLength;
					StartPreviewAnimation(AnimationTime);
				}
			}
		}

		private void AnimationUpdate()
		{
			if (editor.EditedData != null &&
				editor.CurrentGameObject != null &&
				PlayableGraph != null &&
				PlayableGraph.IsValid() &&
				PlayableGraph.IsDataValid(editor.EditedData) &&
				animationTimeBuffor != AnimationTime)
			{

				Vector3 currentPos = editor.CurrentGameObject.transform.position;
				Quaternion curretnRot = editor.CurrentGameObject.transform.rotation;

				float deltTime = AnimationTime - animationTimeBuffor;
				animationTimeBuffor = AnimationTime;



				if (Mathf.Abs(deltTime) > maxAnimationUpdateStep)
				{
					int deltas = Mathf.CeilToInt(Mathf.Abs(deltTime) / maxAnimationUpdateStep);
					float finalDelta = deltTime / (float)deltas;

					for (int i = 0; i < deltas; i++)
					{
						PlayableGraph.Evaluate(finalDelta);
					}
				}
				else
				{
					PlayableGraph.Evaluate(deltTime);
				}

				editor.Repaint();

				if (LockPlaybackPosition)
				{
					editor.CurrentGameObject.transform.position = currentPos;
				}

				if (LockPlaybackRotation)
				{
					editor.CurrentGameObject.transform.rotation = curretnRot;
				}
			}
		}

		#endregion

		#region Animation speed curve

		float animationSpeedCurveKeyValue;

		private void DrawAnimationSpeedCurveOption(Rect rect, Event e)
		{
			Rect controllsRect = new Rect(
				rect.x,
				timelineRect.y + timelineRect.height + VerticalOfssetFromTimeline,
				rect.width,
				25f
				);

			if (editor.EditedData.AnimationSpeedCurve == null)
			{
				editor.EditedData.AnimationSpeedCurve = new AnimationCurve();
			}

			AnimationCurve curve = editor.EditedData.AnimationSpeedCurve;


			GUILayout.BeginArea(controllsRect);
			{
				GUILayout.BeginHorizontal();
				{
					float mainMargin = 20f;
					GUILayout.Space(mainMargin);
					GUILayout.BeginVertical();
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(50f);
							GUILayout.Label("Key value", GUILayout.Width(100f));
							animationSpeedCurveKeyValue = EditorGUILayout.FloatField(
								animationSpeedCurveKeyValue,
								GUILayout.Width(100f)
								);

							if (GUILayout.Button("SetKey", GUILayout.Width(100)))
							{
								for (int i = 0; i < curve.keys.Length; i++)
								{
									Keyframe keyframe = curve.keys[i];
									if (keyframe.time == AnimationTime)
									{
										curve.RemoveKey(i);
										break;
									}
								}
								curve.AddKey(new Keyframe(AnimationTime, animationSpeedCurveKeyValue));
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
					GUILayout.Space(mainMargin);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();

			Rect curveRect = animationTimeRect;
			curveRect.y = controllsRect.y + controllsRect.height;
			curveRect.height = rect.height - controllsRect.height - controllsRect.y - VerticalOfssetFromTimeline;
			curveRect.width = animationTimeRect.width;

			DrawAnimationSpeedCurve(curveRect, curve);
		}

		private void DrawAnimationSpeedCurve(Rect rect, AnimationCurve curve)
		{
			if (curve.length == 1)
			{
				Keyframe keyframe = curve[0];
				curve.AddKey(0f, keyframe.value);
				curve.AddKey(editor.EditedData.animationLength, keyframe.value);
			}
			else if (curve.length >= 2)
			{
				Keyframe first = curve[0];
				Keyframe last = curve[curve.length - 1];

				if (first.time != 0)
				{
					curve.AddKey(0f, first.value);
				}
				if (first.time != editor.EditedData.animationLength)
				{
					curve.AddKey(editor.EditedData.animationLength, last.value);
				}
			}
			else if (curve.length == 0)
			{
				curve.AddKey(0f, 0f);
				curve.AddKey(editor.EditedData.animationLength, 0f);
			}

			for (int i = 0; i < curve.length; i++)
			{
				Keyframe key = curve[i];

				if (key.time < 0 || editor.EditedData.animationLength < key.time)
				{
					curve.RemoveKey(i);
					i--;
				}
			}

			curve = EditorGUI.CurveField(rect, curve);
		}

		#endregion

		public void OnDataChange()
		{
			SelectedCurve = null;
			SelectedEvent = null;
			sectionDrawer.Section = null;


			PlayableGraph?.Destroy();
			ResetZoom();


			AnimationTime = 0f;
			IsAnimationPlaying = false;
		}

		public void OnGameObjectChange()
		{
			PlayableGraph?.Destroy();
			IsAnimationPlaying = false;
		}


		static bool WidthOverlap(Rect first, Rect second)
		{
			if (first.x < second.x && second.x < first.x + first.width ||
				first.x < second.x + second.width && second.x + second.width < first.x + first.width)
			{

				return true;
			}


			return false;
		}

		private Rect DrawCustomRangeSlider(
			float rectWidth,
			ref float lValue,
			ref float rValue,
			bool isSelected
			)
		{
			#region contact range interval
			float deltaToFitWithTimeline = 5f;
			float valueWidth = 50f;

			float buttonWidth = 14f;

			float sliderHeight = 15f;
			float controllHeight = 20f;
			float sliderYDelta = -3f;

			float intervalHeight = sliderHeight + controllHeight;

			GUILayout.Label("", GUILayout.Height(intervalHeight), GUILayout.Width(1f));

			Rect lastLayoutRect = GUILayoutUtility.GetLastRect();

			Rect backgroundRect = new Rect(
				0,
				lastLayoutRect.y,
				rectWidth,
				lastLayoutRect.height
				);

			GUI.DrawTexture(backgroundRect, editor.TimelineBackgroundTexture);

			Rect timeRect = new Rect(
				animationTimeRect.x,
				lastLayoutRect.y,
				animationTimeRect.width,
				lastLayoutRect.height
				);
			GUI.DrawTexture(timeRect, editor.TimelineAnimationTimeTexture);

			if (isSelected)
			{
				GUI.DrawTexture(timeRect, editor.SelectedIntervalTexture);
			}


			Rect startValue = new Rect(
				animationTimeRect.x + lValue / editor.EditedData.animationLength * animationTimeRect.width - valueWidth - buttonWidth /*- deltaToFitWithTimeline*/ + mainRect.x,
				lastLayoutRect.y + sliderHeight,
				valueWidth,
				controllHeight
				);


			Rect startButton = new Rect(
				startValue.x + startValue.width,
				startValue.y,
				buttonWidth,
				controllHeight
				);
			//startValue.x = Mathf.Clamp(mainRect.x, mainRect.)


			Rect endButton = new Rect(
				animationTimeRect.x + rValue / editor.EditedData.animationLength * animationTimeRect.width /*- deltaToFitWithTimeline*/ + mainRect.x,
				lastLayoutRect.y + sliderHeight,
				buttonWidth,
				controllHeight
				);

			Rect endValue = new Rect(
				endButton.x + buttonWidth,
				lastLayoutRect.y + sliderHeight,
				valueWidth,
				controllHeight
				);


			Rect minMaxSliderRect = new Rect(
				animationTimeRect.x - deltaToFitWithTimeline,
				lastLayoutRect.y + sliderYDelta,
				animationTimeRect.width + deltaToFitWithTimeline * 2f,
				15
				);

			EditorGUI.MinMaxSlider(
				minMaxSliderRect,
				ref lValue,
				ref rValue,
				0,
				editor.EditedData.animationLength
				);

			lValue = Mathf.Clamp(
				EditorGUI.FloatField(startValue, lValue),
				0,
				rValue
				);

			if (GUI.Button(startButton, "|"))
			{
				lValue = Mathf.Clamp(
					editor.LeftMenuEditor.AnimationTime,
					0,
					rValue
					);
			}

			rValue = Mathf.Clamp(
				EditorGUI.FloatField(endValue, rValue),
				lValue,
				editor.EditedData.animationLength
				);

			if (GUI.Button(endButton, "|"))
			{
				rValue = Mathf.Clamp(
					editor.LeftMenuEditor.AnimationTime,
					lValue,
					editor.EditedData.animationLength
					);
			}
			#endregion

			return backgroundRect;
		}
	}

	public class SectionDrawer
	{
		public DataSection section;
		public DataSection Section
		{
			get => section;
			set
			{
				IsIntervalSelected = false;
				SelectedInterval = -1;
				section = value;
			}
		}

		public bool IsIntervalSelected { get; set; } = false;
		public int SelectedInterval { get; set; } = 0;

		public enum IntervalSelectionType
		{
			Start,
			Middle,
			End
		}

		IntervalSelectionType selectionType;

		Rect menuRect;
		float scale;
		MotionMatchingDataEditor editor;
		Rect animationTimeRect;

		Texture intervalBackground;
		Texture timelineBackgroundTexture;

		public void SetData(
			MotionMatchingDataEditor editor,
			Rect menuRect,
			float scale,
			Rect animationTimeRect,
			Texture intervalBackground,
			Texture timelineBackground
			)
		{
			this.editor = editor;
			this.scale = scale;
			this.menuRect = menuRect;
			this.animationTimeRect = animationTimeRect;

			this.intervalBackground = intervalBackground;
			this.timelineBackgroundTexture = timelineBackground;
		}

		public void Draw(
			Event e,
			float sliderHeight,
			float controllsHeight
			)
		{
			if (editor.EditedData == null || section == null) return;

			if (editor.EditedData != null && section != null &&
				!(editor.EditedData.sections.Contains(section) || section == editor.EditedData.neverChecking || section == editor.EditedData.notLookingForNewPose))
			{
				return;
			}

			float valueWidth = 50f;
			float buttonWidth = 13f;
			float verticalMarigin = 10f;
			float intervalHeight = sliderHeight + controllsHeight;

			if (section.timeIntervals != null)
			{
				for (int i = 0; i < section.timeIntervals.Count; i++)
				{
					GUILayout.Label("", GUILayout.Height(intervalHeight), GUILayout.Width(1f));

					Rect lastLayoutRect = GUILayoutUtility.GetLastRect();

					Rect backgroundRect = new Rect(
						0,
						lastLayoutRect.y,
						menuRect.width,
						lastLayoutRect.height
						);

					GUI.DrawTexture(backgroundRect, timelineBackgroundTexture);

					Rect timeRect = new Rect(
						animationTimeRect.x,
						lastLayoutRect.y,
						animationTimeRect.width,
						lastLayoutRect.height
						);
					GUI.DrawTexture(timeRect, intervalBackground);

					if (SelectedInterval == i && IsIntervalSelected)
					{
						GUI.DrawTexture(backgroundRect, editor.SelectedIntervalTexture);
					}

					float2 interval = section.timeIntervals[i];
					{
						Rect startValue = new Rect(
							animationTimeRect.x + interval.x / editor.EditedData.animationLength * animationTimeRect.width - valueWidth - buttonWidth,
							lastLayoutRect.y + sliderHeight,
							valueWidth,
							controllsHeight
							);

						Rect startButton = new Rect(
							startValue.x + startValue.width,
							startValue.y,
							buttonWidth,
							controllsHeight
							);
						//startValue.x = Mathf.Clamp(mainRect.x, mainRect.)


						Rect endButton = new Rect(
							animationTimeRect.x + interval.y / editor.EditedData.animationLength * animationTimeRect.width,
							lastLayoutRect.y + sliderHeight,
							buttonWidth,
							controllsHeight
							);

						Rect endValue = new Rect(
							endButton.x + buttonWidth,
							lastLayoutRect.y + sliderHeight,
							valueWidth,
							controllsHeight
							);


						Rect minMaxSliderRect = new Rect(
							animationTimeRect.x - 5,
							lastLayoutRect.y - 3,
							animationTimeRect.width + 10,
							sliderHeight
							);

						EditorGUI.MinMaxSlider(minMaxSliderRect, ref interval.x, ref interval.y, 0, editor.EditedData.animationLength);

						interval.x = Mathf.Clamp(
							EditorGUI.FloatField(startValue, interval.x),
							0,
							interval.y
							);

						if (GUI.Button(startButton, "|"))
						{
							interval.x = Mathf.Clamp(
								editor.LeftMenuEditor.AnimationTime,
								0,
								interval.y
								);
						}

						interval.y = Mathf.Clamp(
							EditorGUI.FloatField(endValue, interval.y),
							interval.x,
							editor.EditedData.animationLength
							);

						if (GUI.Button(endButton, "|"))
						{
							interval.y = Mathf.Clamp(
								editor.LeftMenuEditor.AnimationTime,
								interval.x,
								editor.EditedData.animationLength
								);
						}

						SelectingIntervalIndex(e, backgroundRect, menuRect, i);

					}
					section.timeIntervals[i] = interval;
					GUILayout.Space(verticalMarigin);
				}
			}
			GUILayout.Space(verticalMarigin);
		}

		private void SelectingIntervalIndex(Event e, Rect inputRect, Rect mainRect, int currentIntervalIndex)
		{
			switch (e.type)
			{
				case EventType.MouseDown:
					{
						if (inputRect.Contains(e.mousePosition))
						{
							if (IsIntervalSelected)
							{
								if (currentIntervalIndex == SelectedInterval)
								{
									IsIntervalSelected = false;
									SelectedInterval = -1;
								}
								else
								{
									SelectedInterval = currentIntervalIndex;
								}
							}
							else
							{
								IsIntervalSelected = true;
								SelectedInterval = currentIntervalIndex;
							}
							editor.Repaint();
						}
					}
					break;
			}
		}
	}
}