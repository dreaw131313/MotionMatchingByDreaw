using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace MotionMatching.Tools
{
	public enum EditorTheme
	{
		Light,
		Dark,
	}

	public class MotionMatchingDataEditor : EditorWindow
	{
		[MenuItem("MM Data Editor 2", menuItem = "MotionMatching/Editors/Motion Matching Data Editor", priority = 2)]
		private static void ShowWindow()
		{
			MotionMatchingDataEditor editor = EditorWindow.GetWindow<MotionMatchingDataEditor>();
			editor.titleContent = new GUIContent("MM Data Editor");
			editor.position = new Rect(100, 100, 1000, 300);
		}

		bool isResizing;
		public const float ResizingSize = 3f;
		public const float AreasMargin = 5f;

		const float maxFactor = 0.9f;

		float leftMenuFactor;

		private void OnEnable()
		{
			leftMenuFactor = 0.7F;

			TopBarOnEnable();
			LeftMenuOnEnable();
			OnEnableRightMenu();

		}

		private void OnDisable()
		{
			LeftMenuEditor.OnDisable();
			RightMenuEditor.OnDisable();
			TopBarEditor.OnDisable();
		}

		private void OnDestroy()
		{
			LeftMenuEditor.OnDestroy();
			RightMenuEditor.OnDestroy();
			TopBarEditor.OnDestroy();
		}

		private void OnGUI()
		{
			if(GUI.skin.name == "LightSkin")
			{
				CurrentTheme = EditorThemeType.Light;
			}
			else
			{
				CurrentTheme = EditorThemeType.Dark;
			}

			InitializeTextures();

			Event e = Event.current;

			if (ResizingRectsHorizontal(
				this,
				ref leftMenuRect,
				ref rightMenuRect,
				e,
				ref isResizing,
				ResizingSize,
				ResizingSize,
				maxFactor
				))
			{
				topBarRect.width = LeftMenuRect.width;
				leftMenuFactor = LeftMenuRect.width / position.width;
			}
			else if (ResizingRectsHorizontal(
					this,
					ref topBarRect,
					ref rightMenuRect,
					e,
					ref isResizing,
					ResizingSize,
					ResizingSize,
					maxFactor
					))
			{
				leftMenuRect.width = topBarRect.width;
				leftMenuFactor = LeftMenuRect.width / position.width;
			}
			//else if ()
			//{

			//}


			// left menu
			DrawLeftMenu(e);
			// Top bar
			DrawTopBar(e);
			// right menu
			DrawRightMenu(e);

			if (EditedData != null)
			{
				EditorUtility.SetDirty(EditedData);
			}
		}

		#region common data
		public enum MotionMatchingDataEditingTool
		{
			Sections,
			Contacts,
			Curves,
			AnimationEvents,
			BoneTracks,
			AnimationSpeedCurve
		}

		public MotionMatchingDataEditingTool CurrentTool;
		public MotionMatchingData EditedData;
		public GameObject CurrentGameObject;


		#endregion

		#region Top bar
		Rect topBarRect;
		Rect topBarDrawingRect;

		MMDataEditorTopBar topBarEditor;

		public MMDataEditorTopBar TopBarEditor { get => topBarEditor; private set => topBarEditor = value; }

		const float topBarHeight = 26f;

		private void TopBarOnEnable()
		{
			topBarEditor = new MMDataEditorTopBar();

			TopBarTexture = new Texture2D(1, 1);
			TopBarTexture.SetPixel(0, 0, new Color(0.55f, 0.55f, 0.55f));
			TopBarTexture.Apply();

			topBarRect = new Rect(0, 0, this.position.width * leftMenuFactor, topBarHeight);


			float marign = 3f;
			topBarDrawingRect = new Rect(
				topBarRect.x + marign,
				topBarRect.y + marign,
				topBarRect.width - marign * 2f,
				topBarHeight - marign * 2f
				);

			topBarEditor.SetBasics(
				this,
				topBarDrawingRect
				);
			topBarEditor.OnEnable();
		}

		private void DrawTopBar(Event e)
		{
			topBarRect = new Rect(0, 0, this.position.width * leftMenuFactor, topBarHeight);

			GUI.DrawTexture(topBarRect, TopBarTexture);

			float marign = 3f;
			topBarDrawingRect = new Rect(
				topBarRect.x + marign,
				topBarRect.y + marign,
				topBarRect.width - marign * 2f,
				topBarHeight - marign * 2f
				);

			topBarEditor.SetBasics(
				this,
				topBarDrawingRect
				);
			topBarEditor.OnGUI(
				e,
				topBarDrawingRect
				);
		}

		#endregion

		#region Left menu
		private Rect leftMenuRect;
		public Rect LeftMenuRect { get => leftMenuRect; set => leftMenuRect = value; }
		Rect leftMenuDrawingRect;


		public MMDataEditorLeftMenu LeftMenuEditor { get; private set; }

		private void LeftMenuOnEnable()
		{
			LeftMenuRect = new Rect(
				0,
				topBarRect.y + topBarRect.height,
				leftMenuFactor * position.width,
				position.height - topBarRect.height
				);

			LeftMenuEditor = new MMDataEditorLeftMenu();
			LeftMenuRect = new Rect(
				0,
				topBarRect.y + topBarRect.height,
				position.width * leftMenuFactor,
				position.height - topBarRect.height
				);

			float doubleAreaMargin = 2f * AreasMargin;
			leftMenuDrawingRect = new Rect(
				LeftMenuRect.x + AreasMargin,
				LeftMenuRect.y,
				LeftMenuRect.width - doubleAreaMargin,
				LeftMenuRect.height - AreasMargin
				);

			LeftMenuEditor.SetBasics(
				this,
				leftMenuDrawingRect
				);
			LeftMenuEditor.OnEnable();
		}

		private void DrawLeftMenu(Event e)
		{

			GUI.DrawTexture(LeftMenuRect, LeftMenuTexture);

			LeftMenuRect = new Rect(
				0,
				topBarRect.y + topBarRect.height,
				position.width * leftMenuFactor,
				position.height - topBarRect.height
				);

			float doubleAreaMargin = 2f * AreasMargin;
			leftMenuDrawingRect = new Rect(
				LeftMenuRect.x ,
				LeftMenuRect.y,
				LeftMenuRect.width ,
				LeftMenuRect.height// - AreasMargin
				);

			LeftMenuEditor.SetBasics(
				this,
				leftMenuDrawingRect
				);
			LeftMenuEditor.OnGUI(
				e,
				leftMenuDrawingRect
				);
		}

		#endregion

		#region RightMenu
		Rect rightMenuRect;
		Rect rightMenuDrawingRect;

		public MMDataEditorRightMenu RightMenuEditor { get; private set; }

		private void OnEnableRightMenu()
		{
			rightMenuRect = new Rect(
				LeftMenuRect.x + LeftMenuRect.width,
				0,
				(1F - leftMenuFactor) * position.width,
				position.height
				);

			RightMenuEditor = new MMDataEditorRightMenu();


			rightMenuRect = new Rect(
				LeftMenuRect.x + LeftMenuRect.width,
				0,
				position.width * (1f - leftMenuFactor),
				position.height
				);

			float doubleAreaMargin = 2f * AreasMargin;
			rightMenuDrawingRect = new Rect(
				rightMenuRect.x + AreasMargin,
				rightMenuRect.y + AreasMargin,
				rightMenuRect.width - doubleAreaMargin,
				rightMenuRect.height - doubleAreaMargin
				);

			RightMenuEditor.SetBasics(this, rightMenuDrawingRect);
			RightMenuEditor.OnEnable();
		}

		private void DrawRightMenu(Event e)
		{
			GUI.DrawTexture(rightMenuRect, RightMenuTexture);

			rightMenuRect = new Rect(
				LeftMenuRect.x + LeftMenuRect.width,
				0,
				position.width * (1f - leftMenuFactor),
				position.height
				);

			float doubleAreaMargin = 2f * AreasMargin;
			rightMenuDrawingRect = new Rect(
				rightMenuRect.x + AreasMargin,
				rightMenuRect.y + AreasMargin,
				rightMenuRect.width - doubleAreaMargin,
				rightMenuRect.height - doubleAreaMargin
				);

			RightMenuEditor.SetBasics(this, rightMenuDrawingRect);

			RightMenuEditor.OnGUI(e, rightMenuDrawingRect);
		}

		#endregion

		private bool ResizingRectsHorizontal(
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
				//LeftMenuEditor.ResetZoom();

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


		[OnOpenAsset()]
		public static bool OnOpenMotionMatchingDataAsset(int instanceID, int line)
		{
			MotionMatchingData asset;
			try
			{
				asset = (MotionMatchingData)EditorUtility.InstanceIDToObject(instanceID);
			}
			catch (System.Exception)
			{
				return false;
			}

			if (EditorWindow.HasOpenInstances<MotionMatchingDataEditor>())
			{
				EditorWindow.GetWindow<MotionMatchingDataEditor>().EditedData = asset;
				EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();
				return true;
			}

			MotionMatchingDataEditor.ShowWindow();
			EditorWindow.GetWindow<MotionMatchingDataEditor>().EditedData = asset;
			EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();

			return true;
		}

		#region needed Texture

		public enum EditorThemeType
		{
			Light,
			Dark,
		}

		public EditorThemeType CurrentTheme { get; private set; } = EditorThemeType.Light;

		// Texture layout :
		//	Texture2D
		//	color base value


		// Main editor
		public Texture2D LeftMenuTexture { get; private set; }
		Color LeftMenuTexture_LV = new Color(0.65f, 0.65f, 0.65f);
		Color LeftMenuTexture_BV = new Color(0.35f, 0.35f, 0.35f);

		public Texture2D RightMenuTexture { get; private set; }
		Color RightMenuTexture_LV = new Color(0.8f, 0.8f, 0.8f);
		Color RightMenuTexture_BV = new Color(0.25f, 0.25f, 0.25f);

		public Texture2D TopBarTexture { get; private set; }
		Color TopBarTexture_LV = new Color(0.55f, 0.55f, 0.55f);
		Color TopBarTexture_BV = new Color(0.2f, 0.2f, 0.2f);

		// Top bar

		public Texture2D LockingEnabledTexture { get; private set; } = null;
		private Color LockingEnabledTexture_LV = new Color(1, 0, 0, 0.3f);
		private Color LockingEnabledTexture_BV = new Color(1, 0, 0, 0.3f);

		// Left menu
		public Texture2D TimelineBackgroundTexture { get; private set; }
		private Color TimelineBackgroundTexture_LV = new Color(0.75f, 0.75f, 0.75f);
		private Color TimelineBackgroundTexture_BV = new Color(0.25f, 0.25f, 0.25f);
		public Texture2D TimelineAnimationTimeTexture { get; private set; }
		private Color TimelineAnimationTimeTexture_LV = new Color(0.85f, 0.85f, 0.85f);
		private Color TimelineAnimationTimeTexture_BV = new Color(0.4f, 0.4f, 0.4f);
		public Texture2D TimelinePointerTexture { get; private set; }
		private Color TimelinePointerTexture_LV = new Color(1, 0f, 0f);
		private Color TimelinePointerTexture_BV = new Color(1, 0f, 0f);
		public Texture2D BlackTexture { get; private set; }
		private Color BlackTexture_LV = new Color(0f, 0f, 0f);
		private Color BlackTexture_BV = new Color(0.8f, 0.8f, 0.8f);
		public Texture2D LayoutBorderTexture { get; private set; }
		private Color LayoutBorderTexture_LV = new Color(0.5f, 0.5f, 0.5f);
		private Color LayoutBorderTexture_BV = new Color(0.5f, 0.5f, 0.5f);
		public Texture2D SelectedIntervalTexture { get; private set; }
		private Color SelectedIntervalTexture_LV = new Color(0, 0, 0.8f, 0.15f);
		private Color SelectedIntervalTexture_BV = new Color(0.2f, 0.2f, 1f, 0.3f);
		public Texture2D SelectedEventTexture { get; private set; }
		private Color SelectedEventTexture_LV = new Color(1f, 0f, 0f, 0.3f);
		private Color SelectedEventTexture_BV = new Color(1f, 0f, 0f, 0.3f);
		public Texture2D WhiteTexture { get; private set; }
		private Color WhiteTexture_LV = new Color(1, 1, 1);
		private Color WhiteTexture_BV = new Color(0.1f, 0.1f, 0.1f);

		// Right menu
		public Texture2D SectionNameBackgroundTexture { get; private set; }
		private Color SectionNameBackgroundTexture_LV = new Color(0.9f, 0.9f, 0.9f);
		private Color SectionNameBackgroundTexture_BV = new Color(0.4f, 0.4f, 0.4f);
		public Texture2D SelectedSectionBackgroundTexture { get; private set; }
		private Color SelectedSectionBackgroundTexture_LV = new Color(0, 0, 0.8f, 0.15f);
		private Color SelectedSectionBackgroundTexture_BV = new Color(0.2f, 0.2f, 1f, 0.3f);


		public void InitializeTextures()
		{
			if (LeftMenuTexture == null ||
				RightMenuTexture == null ||
				TopBarTexture == null ||
				LockingEnabledTexture == null ||
				TimelineBackgroundTexture == null ||
				TimelineAnimationTimeTexture == null ||
				TimelinePointerTexture == null ||
				BlackTexture == null ||
				SelectedIntervalTexture == null ||
				SelectedEventTexture == null ||
				WhiteTexture == null ||
				SelectedSectionBackgroundTexture == null ||
				SectionNameBackgroundTexture == null ||
				SelectedSectionBackgroundTexture == null 
				)
			{
				switch (CurrentTheme)
				{
					case EditorThemeType.Light:
						{
							// main editor
							LeftMenuTexture = new Texture2D(1, 1);
							LeftMenuTexture.SetPixel(0, 0, LeftMenuTexture_LV);
							LeftMenuTexture.Apply();

							RightMenuTexture = new Texture2D(1, 1);
							RightMenuTexture.SetPixel(0, 0, RightMenuTexture_LV);
							RightMenuTexture.Apply();

							TopBarTexture = new Texture2D(1, 1);
							TopBarTexture.SetPixel(0, 0, TopBarTexture_LV);
							TopBarTexture.Apply();

							// Top bar
							LockingEnabledTexture = new Texture2D(1, 1);
							LockingEnabledTexture.SetPixel(0, 0, LockingEnabledTexture_LV);
							LockingEnabledTexture.Apply();

							// Left menu:
							TimelineBackgroundTexture = new Texture2D(1, 1);
							TimelineBackgroundTexture.SetPixel(0, 0, TimelineBackgroundTexture_LV);
							TimelineBackgroundTexture.Apply();

							TimelineAnimationTimeTexture = new Texture2D(1, 1);
							TimelineAnimationTimeTexture.SetPixel(0, 0, TimelineAnimationTimeTexture_LV);
							TimelineAnimationTimeTexture.Apply();

							TimelinePointerTexture = new Texture2D(1, 1);
							TimelinePointerTexture.SetPixel(0, 0, TimelinePointerTexture_LV);
							TimelinePointerTexture.Apply();

							BlackTexture = new Texture2D(1, 1);
							BlackTexture.SetPixel(0, 0, BlackTexture_LV);
							BlackTexture.Apply();

							LayoutBorderTexture = new Texture2D(1, 1);
							LayoutBorderTexture.SetPixel(0, 0, LayoutBorderTexture_LV);
							LayoutBorderTexture.Apply();

							SelectedIntervalTexture = new Texture2D(1, 1);
							SelectedIntervalTexture.SetPixel(0, 0, SelectedIntervalTexture_LV);
							SelectedIntervalTexture.Apply();

							SelectedEventTexture = new Texture2D(1, 1);
							SelectedEventTexture.SetPixel(0, 0, SelectedEventTexture_LV);
							SelectedEventTexture.Apply();

							WhiteTexture = new Texture2D(1, 1);
							WhiteTexture.SetPixel(0, 0, WhiteTexture_LV);
							WhiteTexture.Apply();

							// Right menu
							SectionNameBackgroundTexture = new Texture2D(1, 1);
							SectionNameBackgroundTexture.SetPixel(0, 0, SectionNameBackgroundTexture_LV);
							SectionNameBackgroundTexture.Apply();

							SelectedSectionBackgroundTexture = new Texture2D(1, 1);
							SelectedSectionBackgroundTexture.SetPixel(0, 0, SelectedSectionBackgroundTexture_LV);
							SelectedSectionBackgroundTexture.Apply();
						}
						break;
					case EditorThemeType.Dark:
						{
							// main editor
							LeftMenuTexture = new Texture2D(1, 1);
							LeftMenuTexture.SetPixel(0, 0, LeftMenuTexture_BV);
							LeftMenuTexture.Apply();

							RightMenuTexture = new Texture2D(1, 1);
							RightMenuTexture.SetPixel(0, 0, RightMenuTexture_BV);
							RightMenuTexture.Apply();

							TopBarTexture = new Texture2D(1, 1);
							TopBarTexture.SetPixel(0, 0, TopBarTexture_BV);
							TopBarTexture.Apply();

							// Top bar
							LockingEnabledTexture = new Texture2D(1, 1);
							LockingEnabledTexture.SetPixel(0, 0, LockingEnabledTexture_BV);
							LockingEnabledTexture.Apply();

							// Left menu:
							TimelineBackgroundTexture = new Texture2D(1, 1);
							TimelineBackgroundTexture.SetPixel(0, 0, TimelineBackgroundTexture_BV);
							TimelineBackgroundTexture.Apply();

							TimelineAnimationTimeTexture = new Texture2D(1, 1);
							TimelineAnimationTimeTexture.SetPixel(0, 0, TimelineAnimationTimeTexture_BV);
							TimelineAnimationTimeTexture.Apply();

							TimelinePointerTexture = new Texture2D(1, 1);
							TimelinePointerTexture.SetPixel(0, 0, TimelinePointerTexture_BV);
							TimelinePointerTexture.Apply();

							BlackTexture = new Texture2D(1, 1);
							BlackTexture.SetPixel(0, 0, BlackTexture_BV);
							BlackTexture.Apply();

							LayoutBorderTexture = new Texture2D(1, 1);
							LayoutBorderTexture.SetPixel(0, 0, LayoutBorderTexture_BV);
							LayoutBorderTexture.Apply();

							SelectedIntervalTexture = new Texture2D(1, 1);
							SelectedIntervalTexture.SetPixel(0, 0, SelectedIntervalTexture_BV);
							SelectedIntervalTexture.Apply();

							SelectedEventTexture = new Texture2D(1, 1);
							SelectedEventTexture.SetPixel(0, 0, SelectedEventTexture_BV);
							SelectedEventTexture.Apply();

							WhiteTexture = new Texture2D(1, 1);
							WhiteTexture.SetPixel(0, 0, WhiteTexture_BV);
							WhiteTexture.Apply();

							// Right menu
							SectionNameBackgroundTexture = new Texture2D(1, 1);
							SectionNameBackgroundTexture.SetPixel(0, 0, SectionNameBackgroundTexture_BV);
							SectionNameBackgroundTexture.Apply();

							SelectedSectionBackgroundTexture = new Texture2D(1, 1);
							SelectedSectionBackgroundTexture.SetPixel(0, 0, SelectedSectionBackgroundTexture_BV);
							SelectedSectionBackgroundTexture.Apply();
						}
						break;
				}
			}
		}
		#endregion

	}
}