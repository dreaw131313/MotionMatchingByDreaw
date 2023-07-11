using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MotionMatching.Tools.MotionMatchingDataEditor;

namespace MotionMatching.Tools
{
	public class MMDataEditorTopBar : MMDataEditorMenuBaseClass
	{
		const float buttonWidth = 30f;
		const float marginBetweenButtons = 2f;

		public bool ShouldAnimationPlay { get; private set; }

		public override void OnEnable()
		{
			base.OnEnable();
		}


		public override void OnGUI(
			Event e,
			Rect rect
			)
		{
			GUILayout.BeginArea(rect);
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(marginBetweenButtons);
						if (GUILayout.Button("Preview", GUILayout.Width(60f)))
						{
							editor.LeftMenuEditor.StartPreviewAnimation(0f);
						}

						GUILayout.Space(marginBetweenButtons);

						if (!editor.LeftMenuEditor.IsAnimationPlaying)
						{
							if (GUILayout.Button("▶", GUILayout.Width(buttonWidth)))
							{
								editor.LeftMenuEditor.StartPlayingAnimation();
							}
						}
						else
						{
							if (GUILayout.Button("||", GUILayout.Width(buttonWidth)))
							{
								editor.LeftMenuEditor.StopPlayingAnimation();
							}
						}

						GUILayout.Space(marginBetweenButtons);

						DrawLockingButtons();
						GUILayout.Space(marginBetweenButtons);

						float lastLabelWidth = EditorGUIUtility.labelWidth;
						EditorGUIUtility.labelWidth = 100f;
						editor.LeftMenuEditor.AnimationPlaybackSpeed = EditorGUILayout.FloatField(
							"Playback speed",
							Mathf.Clamp(editor.LeftMenuEditor.AnimationPlaybackSpeed, 0.01f, 10f)
							);
						EditorGUIUtility.labelWidth = lastLabelWidth;

					}
					GUILayout.EndHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.BeginHorizontal();
					{
						float zoom = editor.LeftMenuEditor.Zoom;


						//GUILayout.Label("Zoom", GUILayout.Width(40f));
						//zoom = Mathf.CeilToInt(zoom / 0.001f) * 0.001f;
						//zoom = EditorGUILayout.Slider(
						//	zoom,
						//	MMDataEditorLeftMenu.MinZoom,
						//	MMDataEditorLeftMenu.MaxZoom,
						//	GUILayout.MaxWidth(200f)
						//	);

						float labelWidth = EditorGUIUtility.labelWidth;
						EditorGUIUtility.labelWidth = 40f;

						zoom = Mathf.Clamp(EditorGUILayout.FloatField("Zoom", zoom), 0.001f, 1000);

						EditorGUIUtility.labelWidth = labelWidth;

						float deltaZoom = zoom - editor.LeftMenuEditor.Zoom;

						if (editor.EditedData != null)
						{
							if (GUILayout.Button("Reset", GUILayout.Width(60f)))
							{
								editor.LeftMenuEditor.ResetZoom();
							}
						}

						editor.LeftMenuEditor.AddDeltaToZoom(deltaZoom);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		private void DrawLockingButtons()
		{
			if (GUILayout.Button("Lock position"))
			{
				editor.LeftMenuEditor.LockPlaybackPosition = !editor.LeftMenuEditor.LockPlaybackPosition;
			}

			if (editor.LeftMenuEditor.LockPlaybackPosition)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				GUI.DrawTexture(lastRect, editor.LockingEnabledTexture);
			}

			if (GUILayout.Button("Lock rotation"))
			{
				editor.LeftMenuEditor.LockPlaybackRotation = !editor.LeftMenuEditor.LockPlaybackRotation;
			}

			if (editor.LeftMenuEditor.LockPlaybackRotation)
			{
				Rect lastRect = GUILayoutUtility.GetLastRect();
				GUI.DrawTexture(lastRect, editor.LockingEnabledTexture);
			}
		}
	}
}