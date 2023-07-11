using MotionMatching.Gameplay;
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class AnimatorControllerEditor : EditorWindow
	{
		#region static stuff

		[MenuItem("sda", menuItem = "MotionMatching/Editors/MM Animator Editor", priority = 2000)]
		private static void ShowWindow()
		{
			AnimatorControllerEditor editor = GetWindow<AnimatorControllerEditor>();
			editor.position = new Rect(new Vector2(100, 100), new Vector2(1280, 720));
			editor.titleContent = new GUIContent("MM Animator");
			editor.Show();
		}

		#endregion
		// Asset:
		[SerializeField]
		public MotionMatchingAnimator_SO Animator;


		// Resizing values:
		bool isResizing = false;
		private int resizingMenuIndex;


		// Constant values:
		public const float MenuHorizontalMargin = 5f;
		public const float MenuVerticalMargin = 5f;

		public const float ResizeMargin = 10f;
		public const float MinResizeFactorValue = 0.1f;

		// textures;

		private void InitTextures()
		{
		}



		private void OnEnable()
		{
			leftMenu.Editor = this;
			rightMenu.Editor = this;
			graphMenu.Editor = this;
			topBarMenu.Editor = this;

			leftMenu.WidthFactor = 0.2f;
			graphMenu.WidthFactor = 0.6f;
			rightMenu.WidthFactor = 0.2f;

			InitTextures();

			leftMenu.OnEnable();
			graphMenu.OnEnable();
			topBarMenu.OnEnable();
			rightMenu.OnEnable();

		}

		private void OnGUI()
		{
			InitTextures();

			Event e = Event.current;
			ManageResizing(e);

			if (Animator != null)
			{
				Animator.ValidateElementIndexes();
			}


			GUI.DrawTexture(graphMenu.Position, GraphMenuTexture.Texture);
			graphMenu.PerfomrOnGUI(e);

			GUI.DrawTexture(topBarMenu.Position, GraphSpaceTopBarBackground.Texture);
			topBarMenu.PerfomrOnGUI(e);

			GUI.DrawTexture(leftMenu.Position, LeftMenuTexture.Texture);
			leftMenu.PerfomrOnGUI(e);

			GUI.DrawTexture(rightMenu.Position, RightMenuTexture.Texture);
			rightMenu.PerfomrOnGUI(e);


			if (Animator != null)
			{
				EditorUtility.SetDirty(Animator);
			}

		}

		#region Left menu
		public OnePixelColorTexture LeftMenuTexture { get; private set; } = new OnePixelColorTexture(new Color(0.7f, 0.7f, 0.7f));
		public AnimatorControllerEditorLeftMenuSpace leftMenu = new AnimatorControllerEditorLeftMenuSpace();

		#endregion


		#region Graph menu
		public OnePixelColorTexture GraphMenuTexture { get; private set; } = new OnePixelColorTexture(new Color(0.4f, 0.4f, 0.4f));
		public OnePixelColorTexture GraphWireFrameTexture { get; private set; } = new OnePixelColorTexture(new Color(0.5f, 0.5f, 0.5f));

		public AnimatorControllerGraphSpace graphMenu = new AnimatorControllerGraphSpace();

		#endregion


		#region Right menu
		public AnimatorControllerEditorRightSpace rightMenu = new AnimatorControllerEditorRightSpace();

		public OnePixelColorTexture RightMenuTexture { get; private set; } = new OnePixelColorTexture(new Color(0.7f, 0.7f, 0.7f));

		public StateNodeTexture NormalStateTexture { get; private set; } = new StateNodeTexture(new Vector2(70, 15), 255, 165, 87);
		public StateNodeTexture ContactStateTexture { get; private set; } = new StateNodeTexture(new Vector2(70, 15), 117, 209, 102);
		public StateNodeTexture PortalStateTexture { get; private set; } = new StateNodeTexture(new Vector2(70, 15), new Color(0.91f, 0.93f, 0.47f));
		public StateNodeTexture StartStateTexture { get; private set; } = new StateNodeTexture(new Vector2(70, 15), 219, 94, 86);
		public OnePixelColorTexture SelectedStateTexture { get; private set; } = new OnePixelColorTexture(new Color(0.22f, 0.87f, 1f, 0.75f));


		#endregion

		#region  graph spacec top bar:
		const float topBarHeight = 22f;
		GraphSpaceTopBarMenu topBarMenu = new GraphSpaceTopBarMenu();
		public OnePixelColorTexture GraphSpaceTopBarBackground { get; private set; } = new OnePixelColorTexture(new Color(0.6f, 0.6f, 0.6f));
		public GraphSpaceTopBarMenu TopBarMenu { get => topBarMenu; }

		#endregion

		private void ManageResizing(Event e)
		{
			if (isResizing)
			{
				if (e.type == EventType.MouseUp && e.button == 0)
				{
					isResizing = false;
				}
				else
				{
					if (resizingMenuIndex == 1) // left menu
					{
						float minWidth = MinResizeFactorValue * position.width;
						float maxWidth = position.width - rightMenu.Position.width - MinResizeFactorValue * position.width;

						leftMenu.Position.width = Mathf.Clamp(e.mousePosition.x, minWidth, maxWidth);
						leftMenu.WidthFactor = leftMenu.Position.width / position.width;

						graphMenu.WidthFactor = 1f - leftMenu.WidthFactor - rightMenu.WidthFactor;
					}
					else if (resizingMenuIndex == 2) // right menu
					{
						float minWidth = leftMenu.Position.x + leftMenu.Position.width + position.width * MinResizeFactorValue;
						float maxWidth = position.width * (1f - MinResizeFactorValue);

						rightMenu.Position.x = Mathf.Clamp(e.mousePosition.x, minWidth, maxWidth);
						rightMenu.Position.width = position.width - rightMenu.Position.x;
						rightMenu.WidthFactor = rightMenu.Position.width / position.width;

						graphMenu.Position.width = rightMenu.Position.x - graphMenu.Position.x;
						graphMenu.WidthFactor = graphMenu.Position.width / position.width;
					}
					else
					{
						isResizing = false;
					}
				}
				this.Repaint();
			}
			else
			{
				int resizingMenu = IsMouseInResizingSpace(e.mousePosition);
				if (e.type == EventType.MouseDown && e.button == 0 && resizingMenu > 0)
				{
					isResizing = true;
					resizingMenuIndex = resizingMenu;
					e.Use();
				}
			}

			leftMenu.Position.position = new Vector2(0f, 0f);
			leftMenu.Position.height = this.position.height + 2f;
			leftMenu.Position.width = this.position.width * leftMenu.WidthFactor;



			float graphOffset = 1f;
			graphMenu.Position.position = new Vector2(leftMenu.Position.position.x - graphOffset + leftMenu.Position.width, topBarMenu.Position.y + topBarHeight - graphOffset * 2);
			graphMenu.Position.height = this.position.height - topBarHeight + graphOffset * 4f;
			graphMenu.Position.width = this.position.width * graphMenu.WidthFactor + graphOffset * 2f;


			topBarMenu.Position = new Rect(
				graphMenu.Position.x,
				0f,
				graphMenu.Position.width,
				topBarHeight
				);

			rightMenu.Position.position = new Vector2(graphMenu.Position.position.x + graphMenu.Position.width, 0f);
			rightMenu.Position.height = this.position.height;
			rightMenu.Position.width = this.position.width * rightMenu.WidthFactor;
		}

		private int IsMouseInResizingSpace(Vector2 mousePos)
		{
			float leftMenuResizeRectStartX = leftMenu.Position.x + leftMenu.Position.width - ResizeMargin;
			float leftMenuResizeRectEndX = leftMenu.Position.x + leftMenu.Position.width;

			if (leftMenuResizeRectStartX <= mousePos.x && mousePos.x <= leftMenuResizeRectEndX)
			{
				return 1;
			}

			float rightMenuResizeRectStartX = rightMenu.Position.x;
			float rightMenuResizeRectEndX = rightMenu.Position.x + ResizeMargin;

			if (rightMenuResizeRectStartX <= mousePos.x && mousePos.x <= rightMenuResizeRectEndX)
			{
				return 2;
			}

			return 0;
		}

		public void SetAsset(MotionMatchingAnimator_SO newAnimator)
		{
			if (newAnimator != Animator)
			{
				Animator = newAnimator;
				//Editor.Animator = buffor;

				leftMenu.OnChangeAnimatorAsset();
				graphMenu.OnChangeAnimatorAsset();
				rightMenu.OnChangeAnimatorAsset();
			}
		}

		[OnOpenAsset()]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			MotionMatchingAnimator_SO contr;
			try
			{
				contr = (MotionMatchingAnimator_SO)EditorUtility.InstanceIDToObject(instanceID);
			}
			catch (System.Exception)
			{
				return false;
			}

			if (EditorWindow.HasOpenInstances<AnimatorControllerEditor>())
			{
				EditorWindow.GetWindow<AnimatorControllerEditor>().SetAsset(contr);
				EditorWindow.GetWindow<AnimatorControllerEditor>().Repaint();
				return true;
			}

			AnimatorControllerEditor.ShowWindow();
			EditorWindow.GetWindow<AnimatorControllerEditor>().SetAsset(contr);
			EditorWindow.GetWindow<AnimatorControllerEditor>().Repaint();

			return true;
		}
	}

	public class OnePixelColorTexture
	{
		Color color;
		Texture2D texture;

		public bool IsTextureNull => texture == null;

		public Texture2D Texture
		{
			get
			{
				if (texture == null)
				{
					texture = new Texture2D(1, 1);
					texture.SetPixel(0, 0, color);
					texture.Apply();
				}
				return texture;
			}
		}

		public OnePixelColorTexture() { }

		public OnePixelColorTexture(Color color)
		{
			this.color = color;
		}

		public OnePixelColorTexture(float r, float g, float b, float a = 1f)
		{
			this.color = new Color(r, g, b, a);
		}

		public OnePixelColorTexture(uint r, uint g, uint b, uint a = 255)
		{
			this.color = new Color(
				(float)r / 255f,
				(float)g / 255f,
				(float)b / 255f,
				(float)a / 255f
				);
		}
	}


	public class StateNodeTexture
	{
		Color color;
		Texture2D texture;
		Vector2 size;

		public Texture2D Texture
		{
			get
			{
				if (texture == null)
				{
					InitTexture();
				}
				return texture;
			}
		}

		public StateNodeTexture() { }

		public StateNodeTexture(Vector2 size, Color color)
		{
			this.size = size;
			this.color = color;
		}

		public StateNodeTexture(Vector2 size, float r, float g, float b, float a = 1f)
		{
			this.size = size;
			this.color = new Color(r, g, b, a);
		}

		public StateNodeTexture(Vector2 size, uint r, uint g, uint b, uint a = 255)
		{
			this.size = size;
			this.color = new Color(
				(float)r / 255f,
				(float)g / 255f,
				(float)b / 255f,
				(float)a / 255f
				);
		}

		private void InitTexture()
		{
			Color outline = color;
			outline *= 0.85f;
			outline.a = 1f;
			texture = new Texture2D((int)size.x, (int)size.y);

			// up
			for (int x = 0; x < size.x; x++)
			{
				texture.SetPixel(x, 0, outline);
			}

			// down
			for (int x = 0; x < size.x; x++)
			{
				texture.SetPixel(x, (int)size.y - 1, outline);
			}

			// left
			for (int y = 1; y < size.y - 1; y++)
			{
				texture.SetPixel(0, y, outline);
			}

			// right
			for (int y = 1; y < size.y - 1; y++)
			{
				texture.SetPixel((int)size.x - 1, y, outline);
			}


			for (int x = 1; x < size.x - 1; x++)
			{
				for (int y = 1; y < size.y - 1; y++)
				{
					texture.SetPixel(x, y, color);
				}
			}




			texture.Apply();
		}
	}

}