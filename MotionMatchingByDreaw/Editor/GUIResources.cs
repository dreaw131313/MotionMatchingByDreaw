using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	public static class GUIResources
	{
		// Textures
		private static Texture2D mediumTexture_0 = null;
		private static Texture2D mediumTexture_1 = null;
		private static Texture2D mediumTexture_2 = null;
		private static Texture2D graphBackgorundTexture = null;
		private static Texture2D mainMenuTexture = null;
		private static Texture2D resizeTexture = null;
		private static Texture2D redTexture = null;
		private static Texture2D selectedConnectionTexture = null;
		private static Texture2D normalContectionTexture = null;
		private static Texture2D portalConnectionTexture = null;
		private static Texture2D contactConnectionTexture = null;

		// Button styles:
		//private static GUIStyle moveButtonStyle = null;
		//private static GUIStyle rotateButtonStyle = null;


		// header styles texture
		private static Texture2D darkHeaderTexture = null;
		private static Texture2D mediumHeaderTexture = null;
		private static Texture2D lightHeaderTexture = null;

		// GUI header Styles
		private static int sm_FontSize = 12;
		private static int md_FontSize = 14;
		private static int lg_FontSize = 24;
		private static int vlg_FontSize = 32;

		private static int sm_HederHeight = 24;
		private static int md_HederHeight = 28;
		private static int lg_HederHeight = 40;
		private static int vlg_HederHeight = 50;
		// header tabulator
		private static float headerTab = 15f;
		//Dark
		private static GUIStyle darkHeaderStyle_sm = null;
		private static GUIStyle darkHeaderStyle_md = null;
		private static GUIStyle darkHeaderStyle_lg = null;
		private static GUIStyle darkHeaderStyle_vlg = null;

		//Medium
		private static GUIStyle mediumHeaderStyle_sm = null;
		private static GUIStyle mediumHeaderStyle_md = null;
		private static GUIStyle mediumHeaderStyle_lg = null;
		private static GUIStyle mediumHeaderStyle_vlg = null;

		//light
		private static GUIStyle lightHeaderStyle_sm = null;
		private static GUIStyle lightHeaderStyle_md = null;
		private static GUIStyle lightHeaderStyle_lg = null;
		private static GUIStyle lightHeaderStyle_vlg = null;

		// buttons
		private static GUIStyle button_md = null;


		// Animator graph states styles
		private static GUIStyle normalNode = null;
		private static GUIStyle portalNode = null;
		private static GUIStyle selectedNode = null;
		private static GUIStyle startNode = null;
		//private static GUIStyle alwaysNode = null;
		private static GUIStyle contactNode = null;

		private static GUIStyle inputPoint = null;
		private static GUIStyle outputPoint = null;

		private static GUIStyle nodeText = null;

		private static GUIStyle selectionArea = null;

		// Text
		//private static GUIStyle text_md = null;

		private static GUIStyle transitionCountTextStyle = null;



		#region Textures
		public static Texture2D GetMediumTexture_0()
		{
			if (mediumTexture_0 == null)
			{
				mediumTexture_0 = new Texture2D(1, 1);
				mediumTexture_0.SetPixel(1, 1, new Color(0.55f, 0.55f, 0.55f));
				mediumTexture_0.Apply();
			}

			return mediumTexture_0;
		}

		public static Texture2D GetMediumTexture_1()
		{
			if (mediumTexture_1 == null)
			{
				mediumTexture_1 = new Texture2D(1, 1);
				mediumTexture_1.SetPixel(1, 1, new Color(0.65f, 0.65f, 0.65f));
				mediumTexture_1.Apply();
			}

			return mediumTexture_1;
		}

		public static Texture2D GetMediumTexture_2()
		{
			if (mediumTexture_2 == null)
			{
				mediumTexture_2 = new Texture2D(1, 1);
				mediumTexture_2.SetPixel(1, 1, new Color(0.75f, 0.75f, 0.75f));
				mediumTexture_2.Apply();
			}

			return mediumTexture_2;
		}


		public static Texture2D GetGraphSpaceTexture()
		{
			if (graphBackgorundTexture == null)
			{
				graphBackgorundTexture = new Texture2D(1, 1);
				graphBackgorundTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f));
				graphBackgorundTexture.Apply();
			}
			return graphBackgorundTexture;
		}

		public static Texture2D GetMainMenuTexture()
		{
			if (mainMenuTexture == null)
			{
				mainMenuTexture = new Texture2D(1, 1);
				mainMenuTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
				mainMenuTexture.Apply();
			}
			return mainMenuTexture;
		}

		public static Texture2D GetResizeTexture()
		{
			if (resizeTexture == null)
			{
				resizeTexture = new Texture2D(1, 1);
				resizeTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
				resizeTexture.Apply();
			}
			return resizeTexture;
		}

		public static Texture2D GetRedTexture()
		{
			if (redTexture == null)
			{
				redTexture = new Texture2D(1, 1);
				redTexture.SetPixel(0, 0, new Color(0.5f, 0.0f, 0.0f));
				redTexture.Apply();
			}
			return redTexture;
		}

		// Header Styles Texture
		public static Texture2D GetDarkTexture()
		{
			if (darkHeaderTexture == null)
			{
				darkHeaderTexture = new Texture2D(1, 1);
				darkHeaderTexture.SetPixel(0, 0, new Color(0.45f, 0.45f, 0.45f));
				darkHeaderTexture.Apply();
			}
			return darkHeaderTexture;
		}


		public static Texture2D GetLightTexture()
		{
			if (lightHeaderTexture == null)
			{
				lightHeaderTexture = new Texture2D(1, 1);
				lightHeaderTexture.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f));
				lightHeaderTexture.Apply();
			}
			return lightHeaderTexture;
		}

		public static Texture2D GetNormalConnectionTexture()
		{
			if (normalContectionTexture == null)
			{
				normalContectionTexture = new Texture2D(1, 1);
				normalContectionTexture.SetPixel(0, 0, Color.white);
				normalContectionTexture.Apply();
			}
			return normalContectionTexture;
		}

		public static Texture2D GetPortalConnectionTexture()
		{
			if (portalConnectionTexture == null)
			{
				portalConnectionTexture = new Texture2D(1, 1);
				portalConnectionTexture.SetPixel(0, 0, Color.green);
				portalConnectionTexture.Apply();
			}
			return portalConnectionTexture;
		}

		public static Texture2D GetContactConnectionTexture()
		{
			if (contactConnectionTexture == null)
			{
				contactConnectionTexture = new Texture2D(1, 1);
				contactConnectionTexture.SetPixel(0, 0, Color.gray);
				contactConnectionTexture.Apply();
			}
			return contactConnectionTexture;
		}

		public static Texture2D GetSelectedConnectionTexture()
		{
			if (selectedConnectionTexture == null)
			{
				selectedConnectionTexture = new Texture2D(1, 1);
				selectedConnectionTexture.SetPixel(0, 0, Color.yellow);
				selectedConnectionTexture.Apply();
			}
			return selectedConnectionTexture;
		}

		#endregion

		#region Header styles
		//Dark
		public static GUIStyle GetDarkHeaderStyle_SM()
		{
			if (darkHeaderStyle_sm == null || darkHeaderTexture == null)
			{
				darkHeaderStyle_sm = new GUIStyle();
				darkHeaderStyle_sm.fixedHeight = sm_HederHeight;
				darkHeaderStyle_sm.contentOffset = new Vector2(headerTab, 4);
				darkHeaderStyle_sm.normal.background = GetDarkTexture();
				darkHeaderStyle_sm.fontSize = sm_FontSize;
			}
			if (darkHeaderStyle_sm.normal.background != GetDarkTexture())
			{
				darkHeaderStyle_sm.normal.background = GetDarkTexture();
			}
			return darkHeaderStyle_sm;
		}

		public static GUIStyle GetDarkHeaderStyle_MD()
		{
			if (darkHeaderStyle_md == null || darkHeaderTexture == null)
			{
				darkHeaderStyle_md = new GUIStyle();
				darkHeaderStyle_md.fixedHeight = md_HederHeight;
				darkHeaderStyle_md.contentOffset = new Vector2(headerTab, 6);
				darkHeaderStyle_md.normal.background = GetDarkTexture();
				darkHeaderStyle_md.fontSize = md_FontSize;
			}
			if (darkHeaderStyle_md.normal.background != GetDarkTexture())
			{
				darkHeaderStyle_md.normal.background = GetDarkTexture();
			}
			return darkHeaderStyle_md;
		}

		public static GUIStyle GetDarkHeaderStyle_LG()
		{
			if (darkHeaderStyle_lg == null || darkHeaderTexture == null)
			{
				darkHeaderStyle_lg = new GUIStyle();
				darkHeaderStyle_lg.fixedHeight = lg_HederHeight;
				darkHeaderStyle_lg.contentOffset = new Vector2(headerTab, 6);
				darkHeaderStyle_lg.normal.background = GetDarkTexture();
				darkHeaderStyle_lg.fontSize = lg_FontSize;
			}

			if (darkHeaderStyle_lg.normal.background != GetDarkTexture())
			{
				darkHeaderStyle_lg.normal.background = GetDarkTexture();
			}
			return darkHeaderStyle_lg;
		}

		public static GUIStyle GetDarkHeaderStyle_VLG()
		{
			if (darkHeaderStyle_vlg == null || darkHeaderTexture == null)
			{
				darkHeaderStyle_vlg = new GUIStyle();
				darkHeaderStyle_vlg.fixedHeight = vlg_HederHeight;
				darkHeaderStyle_vlg.contentOffset = new Vector2(headerTab, 6);
				darkHeaderStyle_vlg.normal.background = GetDarkTexture();
				darkHeaderStyle_vlg.fontSize = vlg_FontSize;
			}

			if (darkHeaderStyle_vlg.normal.background != GetDarkTexture())
			{
				darkHeaderStyle_vlg.normal.background = GetDarkTexture();
			}
			return darkHeaderStyle_vlg;
		}

		//Medium
		public static GUIStyle GetMediumHeaderStyle_SM()
		{
			if (mediumHeaderStyle_sm == null || mediumHeaderTexture == null)
			{
				mediumHeaderStyle_sm = new GUIStyle();
				mediumHeaderStyle_sm.fixedHeight = sm_HederHeight;
				mediumHeaderStyle_sm.contentOffset = new Vector2(headerTab, 4);
				mediumHeaderStyle_sm.normal.background = GetMediumTexture_1();
				mediumHeaderStyle_sm.fontSize = sm_FontSize;
			}

			if (mediumHeaderStyle_sm.normal.background != GetMediumTexture_1())
			{
				mediumHeaderStyle_sm.normal.background = GetMediumTexture_1();
			}

			return mediumHeaderStyle_sm;
		}

		public static GUIStyle GetMediumHeaderStyle_MD()
		{
			if (mediumHeaderStyle_md == null || mediumHeaderTexture == null)
			{
				mediumHeaderStyle_md = new GUIStyle();
				mediumHeaderStyle_md.fixedHeight = md_HederHeight;
				mediumHeaderStyle_md.contentOffset = new Vector2(headerTab, 6);
				mediumHeaderStyle_md.normal.background = GetMediumTexture_1();
				mediumHeaderStyle_md.fontSize = md_FontSize;
			}
			if (mediumHeaderStyle_md.normal.background != GetMediumTexture_1())
			{
				mediumHeaderStyle_md.normal.background = GetMediumTexture_1();
			}
			return mediumHeaderStyle_md;
		}

		public static GUIStyle GetMediumHeaderStyle_LG()
		{
			if (mediumHeaderStyle_lg == null || mediumHeaderTexture == null)
			{
				mediumHeaderStyle_lg = new GUIStyle();
				mediumHeaderStyle_lg.fixedHeight = lg_HederHeight;
				mediumHeaderStyle_lg.contentOffset = new Vector2(headerTab, 6);
				mediumHeaderStyle_lg.normal.background = GetMediumTexture_1();
				mediumHeaderStyle_lg.fontSize = lg_FontSize;
			}
			if (mediumHeaderStyle_lg.normal.background != GetMediumTexture_1())
			{
				mediumHeaderStyle_lg.normal.background = GetMediumTexture_1();
			}
			return mediumHeaderStyle_lg;
		}

		public static GUIStyle GetMediumHeaderStyle_VLG()
		{
			if (mediumHeaderStyle_vlg == null || mediumHeaderTexture == null)
			{
				mediumHeaderStyle_vlg = new GUIStyle();
				mediumHeaderStyle_vlg.fixedHeight = vlg_HederHeight;
				mediumHeaderStyle_vlg.contentOffset = new Vector2(headerTab, 6);
				mediumHeaderStyle_vlg.normal.background = GetMediumTexture_1();
				mediumHeaderStyle_vlg.fontSize = vlg_FontSize;
			}

			if (mediumHeaderStyle_vlg.normal.background != GetMediumTexture_1())
			{
				mediumHeaderStyle_vlg.normal.background = GetMediumTexture_1();
			}
			return mediumHeaderStyle_vlg;
		}

		//Light
		public static GUIStyle GetLightHeaderStyle_SM()
		{
			if (lightHeaderStyle_sm == null || lightHeaderTexture == null)
			{
				lightHeaderStyle_sm = new GUIStyle();
				lightHeaderStyle_sm.fixedHeight = sm_HederHeight;
				lightHeaderStyle_sm.contentOffset = new Vector2(headerTab, 4);
				lightHeaderStyle_sm.normal.background = GetLightTexture();
				lightHeaderStyle_sm.fontSize = sm_FontSize;
			}
			if (lightHeaderStyle_sm.normal.background != GetLightTexture())
			{
				lightHeaderStyle_sm.normal.background = GetLightTexture();
			}

			return lightHeaderStyle_sm;
		}

		public static GUIStyle GetLightHeaderStyle_MD()
		{
			if (lightHeaderStyle_md == null || lightHeaderTexture == null)
			{
				lightHeaderStyle_md = new GUIStyle();
				lightHeaderStyle_md.fixedHeight = md_HederHeight;
				lightHeaderStyle_md.contentOffset = new Vector2(headerTab, 6);
				lightHeaderStyle_md.normal.background = GetLightTexture();
				lightHeaderStyle_md.fontSize = md_FontSize;
			}
			if (lightHeaderStyle_md.normal.background != GetLightTexture())
			{
				lightHeaderStyle_md.normal.background = GetLightTexture();
			}
			return lightHeaderStyle_md;
		}

		public static GUIStyle GetLightHeaderStyle_LG()
		{
			if (lightHeaderStyle_lg == null || lightHeaderTexture == null)
			{
				lightHeaderStyle_lg = new GUIStyle();
				lightHeaderStyle_lg.fixedHeight = lg_HederHeight;
				lightHeaderStyle_lg.contentOffset = new Vector2(headerTab, 6);
				lightHeaderStyle_lg.normal.background = GetLightTexture();
				lightHeaderStyle_lg.fontSize = lg_FontSize;
			}
			if (lightHeaderStyle_lg.normal.background != GetLightTexture())
			{
				lightHeaderStyle_lg.normal.background = GetLightTexture();
			}
			return lightHeaderStyle_lg;
		}

		public static GUIStyle GetLightHeaderStyle_VLG()
		{
			if (lightHeaderStyle_vlg == null || lightHeaderTexture == null)
			{
				lightHeaderStyle_vlg = new GUIStyle();
				lightHeaderStyle_vlg.fixedHeight = vlg_HederHeight;
				lightHeaderStyle_vlg.contentOffset = new Vector2(headerTab, 6);
				lightHeaderStyle_vlg.normal.background = GetMediumTexture_1();
				lightHeaderStyle_vlg.fontSize = vlg_FontSize;
			}

			if (lightHeaderStyle_vlg.normal.background != GetLightTexture())
			{
				lightHeaderStyle_vlg.normal.background = GetLightTexture();
			}
			return lightHeaderStyle_vlg;
		}
		#endregion

		#region buttons
		public static GUIStyle Button_MD()
		{
			if (button_md == null)
			{
				button_md = new GUIStyle("button");
				button_md.fontSize = 16;
			}
			return button_md;
		}

		#endregion


		#region Node syles
		public static GUIStyle NormalNodeStyle()
		{
			if (normalNode == null)
			{
				normalNode = new GUIStyle();
				normalNode.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node5.png") as Texture2D;
				normalNode.border = new RectOffset(10, 10, 10, 10);
			}
			return normalNode;
		}

		public static GUIStyle PortalNodeStyle()
		{
			if (portalNode == null)
			{
				portalNode = new GUIStyle();
				portalNode.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node4.png") as Texture2D;
				portalNode.border = new RectOffset(10, 10, 10, 10);
			}
			return portalNode;
		}

		public static GUIStyle SelectedNodeStyle()
		{
			if (selectedNode == null)
			{
				selectedNode = new GUIStyle();
				selectedNode.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node2 on.png") as Texture2D;
				selectedNode.border = new RectOffset(10, 10, 10, 10);
			}
			return selectedNode;
		}

		public static GUIStyle StartNodeStyle()
		{
			if (startNode == null)
			{
				startNode = new GUIStyle();
				startNode.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node6.png") as Texture2D;
				startNode.border = new RectOffset(10, 10, 10, 10);
			}
			return startNode;
		}

		public static GUIStyle ContactNodeStyle()
		{
			if (contactNode == null)
			{
				contactNode = new GUIStyle();
				contactNode.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node3.png") as Texture2D;
				contactNode.border = new RectOffset(10, 10, 10, 10);
			}
			return contactNode;
		}

		public static GUIStyle InputPointStyle()
		{
			if (inputPoint == null)
			{
				inputPoint = new GUIStyle();
				inputPoint.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
				inputPoint.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
				inputPoint.border = new RectOffset(4, 4, 12, 12);
			}

			return inputPoint;
		}

		public static GUIStyle OutputPointStyle()
		{
			if (outputPoint == null)
			{
				outputPoint = new GUIStyle();
				outputPoint.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
				outputPoint.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
				outputPoint.border = new RectOffset(4, 4, 12, 12);
			}

			return outputPoint;
		}

		public static GUIStyle NodeTextStyle()
		{
			if (nodeText == null)
			{
				int styleFontSize = 20;
				TextAnchor alingment = TextAnchor.MiddleCenter;
				nodeText = new GUIStyle();
				nodeText.fontSize = styleFontSize;
				nodeText.alignment = alingment;
			}
			return nodeText;
		}
		#endregion

		public static GUIStyle GetSelectionArea()
		{
			if (selectionArea == null)
			{
				selectionArea = new GUIStyle();
				selectionArea.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/rectangletoolselection.png") as Texture2D;
			}
			return selectionArea;
		}


		public static GUIStyle GetTransitionCountTextStyle()
		{
			if (transitionCountTextStyle == null)
			{
				int styleFontSize = 13;
				TextAnchor alingment = TextAnchor.MiddleCenter;
				transitionCountTextStyle = new GUIStyle();
				transitionCountTextStyle.fontSize = styleFontSize;
				transitionCountTextStyle.alignment = alingment;
			}
			return transitionCountTextStyle;
		}

		#region Transition textures

		#endregion

		#region button textures
		#endregion
	}
}