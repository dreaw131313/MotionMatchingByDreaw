using MotionMatching.Gameplay;
using System;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	public static class GUILayoutElements
	{
		public static bool DrawHeader(string header, GUIStyle headerOpen, GUIStyle headerClosed, ref bool result)
		{
			GUILayout.BeginVertical(result ? headerOpen : headerClosed);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(header, result ? headerOpen : headerClosed))
			{
				result = !result;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			return result;
		}

		public static bool DrawHeader(string header, GUIStyle headerOpen, GUIStyle headerClosed, bool result)
		{
			GUILayout.BeginVertical(result ? headerOpen : headerClosed);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(header, result ? headerOpen : headerClosed))
			{
				result = !result;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			return result;
		}

		public static void DrawHeader(string header, GUIStyle headerStyle)
		{
			GUILayout.BeginVertical(headerStyle);
			GUILayout.BeginHorizontal();
			GUILayout.Label(header, headerStyle);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		public static void DrawGrid(
			Rect area,
			float gridSpacingHorizontal,
			float gridSpacingVertical,
			float gridOpaCity,
			Color gridColor,
			float zoom,
			float thicknes
			)
		{
			int widthDivs = Mathf.CeilToInt(area.width / gridSpacingHorizontal / zoom);
			int heightDivs = Mathf.CeilToInt(area.height / gridSpacingVertical / zoom);

			Handles.BeginGUI();
			Handles.color = gridColor;

			for (int i = 1; i < widthDivs; i++)
			{
				Handles.DrawLine(
					new Vector3(gridSpacingHorizontal * zoom * i, /*-gridSpacingHorizontal*/ 0f, 0) + new Vector3(area.x, area.y),
					new Vector3(gridSpacingHorizontal * zoom * i, area.height, 0) + new Vector3(area.x, area.y),
					thicknes
					);
			}

			for (int i = 1; i < heightDivs; i++)
			{
				Handles.DrawLine(
					new Vector3(/*-gridSpacingVertical*/0f, gridSpacingVertical * zoom * i, 0) + new Vector3(area.x, area.y),
					new Vector3(area.width, gridSpacingVertical * zoom * i, 0) + new Vector3(area.x, area.y),
					thicknes
					);
			}
		}

		public static void MinMaxSlider(
			ref float x,
			ref float y,
			float min,
			float max,
			float floatFieldWidth = 40,
			int digits = 4
			)
		{
			GUILayout.BeginHorizontal();
			x = (float)Math.Round(EditorGUILayout.FloatField(x, GUILayout.Width(floatFieldWidth)), digits);
			EditorGUILayout.MinMaxSlider(ref x, ref y, min, max);
			y = (float)Math.Round(EditorGUILayout.FloatField(y, GUILayout.Width(floatFieldWidth)), digits);
			GUILayout.EndHorizontal();
		}


		public static bool ResizingRectsHorizontal(
			EditorWindow editor,
			ref Rect r1,
			ref Rect r2,
			Event e,
			ref bool resizing,
			float resizeWidthLeft = 5f,
			float resizeWidthRight = 5f,
			float maxWidthFactor = 0.9f
			)
		{
			if (e.mousePosition.x > (r1.x + r1.width - resizeWidthLeft) &&
				e.mousePosition.x < (r2.x + resizeWidthRight) &&
				e.mousePosition.y >= r1.y &&
				e.mousePosition.y <= r1.y + r1.height)
			{
				if (e.button == 0 && e.type == EventType.MouseDown)
				{
					resizing = true;
				}
			}

			if (e.type == EventType.MouseUp)
			{
				resizing = false;
			}


			if (resizing)
			{
				float r2End = r2.x + r2.width;

				if (r1.Contains(e.mousePosition) || r2.Contains(e.mousePosition))
				{
					EditorGUIUtility.AddCursorRect(r1, MouseCursor.ResizeHorizontal);
					EditorGUIUtility.AddCursorRect(r2, MouseCursor.ResizeHorizontal);
				}

				float r1WidthClamp = r2End - editor.position.width * (1f - maxWidthFactor) - r1.x;

				r1.width = Mathf.Clamp(
					e.mousePosition.x - r1.x,
					editor.position.width * (1f - maxWidthFactor),
					r1WidthClamp
					);

				r2.x = r1.x + r1.width;

				r2.width = r2End - r2.x;

				editor.Repaint();
				return true;
			}

			return false;
		}

		public static void PreviewMotionMatchingData(
			MotionMatchingData data,
			ref float animationTime,
			ref float animationTimeBuffor,
			PreparingDataPlayableGraph animator,
			float maxDeltaTime = 0.016667f
			)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(5);
			animationTime = EditorGUILayout.Slider(animationTime, 0f, data.animationLength);
			GUILayout.Space(5);
			GUILayout.EndHorizontal();

			float deltaTime = animationTime - animationTimeBuffor;
			animationTimeBuffor = animationTime;

			float deltaTimeModule = Mathf.Abs(deltaTime);

			if (deltaTime != 0)
			{
				if (animator != null && animator.IsValid())
				{
					if (deltaTimeModule > maxDeltaTime)
					{
						int counter = Mathf.CeilToInt(deltaTimeModule / maxDeltaTime);

						deltaTime = deltaTime / counter;

						for (int i = 0; i < counter; i++)
						{
							animator.EvaluateMotionMatchgData(data, deltaTime);
						}
					}
					else
					{
						animator.EvaluateMotionMatchgData(data, deltaTime);
					}

				}
			}
		}

	}
}