using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class MotionMatchingGraphDebuger : EditorWindow
	{
		#region Create window staticFunction
		[MenuItem("Motion Matching Graph Debuger", menuItem = "MotionMatching/Motion Matching Graph Debuger", priority = 4)]
		public static void CreateGraphDebugerWindow()
		{
			MotionMatchingGraphDebuger editor = EditorWindow.GetWindow<MotionMatchingGraphDebuger>();
			editor.titleContent = new GUIContent("Motion Matching Graph Debuger");
			editor.position = new Rect(100, 100, 800, 600);
		}
		#endregion

		GameObject go = null;
		GameObject goBuffor = null;
		MotionMatchingComponent mmc;
		int maxPassedStates = 10;

		List<State_SO> debugStates;

		float lineHeight = 25f;
		Vector2 scroll = Vector2.zero;

		private void OnGUI()
		{
			DrawGameObjectPicker();
			Rect windowRect = new Rect(0, lineHeight, this.position.width, this.position.height - lineHeight);
			GUILayout.BeginArea(windowRect);
			{
				scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(this.position.width));
				{
					if (Application.isPlaying && mmc != null)
					{
						ManageDebugStatesList();
						GUILayout.BeginHorizontal();
						{
							GUILayout.BeginVertical();
							{
								DrawStates();
							}
							GUILayout.EndVertical();

							GUILayout.BeginVertical(GUILayout.Width(this.position.width / 3f));
							{
								DrawBools();
								DrawInts();
								DrawFloats();
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndHorizontal();
						Repaint();
					}
					else if (debugStates != null)
					{
						debugStates.Clear();
					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndArea();

		}

		private void DrawGameObjectPicker()
		{
			Rect r = new Rect(0, 0, this.position.width, lineHeight);
			GUI.DrawTexture(r, GUIResources.GetMediumTexture_1());

			GUILayout.BeginArea(r);
			{
				GUILayout.Space(3f);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(5);
					GUILayout.Label("Game object", GUILayout.Width(80));
					go = (GameObject)EditorGUILayout.ObjectField(go, typeof(GameObject), true, GUILayout.Width(150));
					if (goBuffor != go)
					{
						goBuffor = go;
						if (debugStates != null)
						{
							debugStates.Clear();
						}
					}
					if (go != null)
					{
						mmc = go.GetComponent<MotionMatchingComponent>();
						if (mmc == null)
						{
							mmc = go.GetComponentInChildren<MotionMatchingComponent>();
						}
						if (mmc == null)
						{
							mmc = go.GetComponentInParent<MotionMatchingComponent>();
						}
					}
					else
					{
						mmc = null;
					}

					GUILayout.Space(10);

					GUILayout.Label("Max passed states", GUILayout.Width(110));
					maxPassedStates = EditorGUILayout.IntField(maxPassedStates, GUILayout.Width(30));
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();
		}

		private void ManageDebugStatesList()
		{
			if (debugStates == null)
			{
				debugStates = new List<State_SO>();
			}

			State_SO currentState = mmc.GetCurrentDataState_EditorOnly();

			if (debugStates.Count == 0)
			{
				debugStates.Add(currentState);
				scroll.y = float.MaxValue;
			}
			if (debugStates[debugStates.Count - 1] != currentState)
			{
				debugStates.Add(currentState);
				scroll.y = float.MaxValue;
			}
			if (debugStates.Count > maxPassedStates)
			{
				debugStates.RemoveAt(0);
			}

		}

		private void DrawStates()
		{
			GUILayout.Space(10);
			for (int i = 0; i < debugStates.Count; i++)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.BeginHorizontal(GUILayout.Width(50));
					{
						GUILayoutElements.DrawHeader("=>", GUIResources.GetDarkHeaderStyle_SM());
					}
					GUILayout.EndHorizontal();
					GUILayout.Space(10);
					GUILayout.BeginHorizontal();
					{
						DrawDebugedStates(i);
						GUILayout.Space(10);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(5);
			}

			Rect lr = GUILayoutUtility.GetLastRect();
			EditorGUILayout.Space(lr.width);
		}

		private void DrawDebugedStates(int index)
		{
			State_SO state = debugStates[index];

			//GUILayout.Label(state.GetName());
			if (index == debugStates.Count - 1)
			{
				GUILayoutElements.DrawHeader(state.Name, GUIResources.GetLightHeaderStyle_SM());
			}
			else
			{
				GUILayoutElements.DrawHeader(state.Name, GUIResources.GetMediumHeaderStyle_SM());
			}
		}

		private void DrawBools()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayoutElements.DrawHeader("Bool parameters:", GUIResources.GetMediumHeaderStyle_SM());
				GUILayout.Space(5);
			}
			GUILayout.EndHorizontal();

			if (mmc.ConditionBoolsEditorOnly != null)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginVertical();
					{
						foreach (var name in mmc.motionMatchingController.BoolParameters)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								GUILayout.Label(name.Name);
								//EditorGUILayout.Toggle(pair.Value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						foreach (bool value in mmc.ConditionBoolsEditorOnly)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								EditorGUILayout.Toggle(" ", value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawInts()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayoutElements.DrawHeader("Int parameters:", GUIResources.GetMediumHeaderStyle_SM());
				GUILayout.Space(5);
			}
			GUILayout.EndHorizontal();

			if (mmc.ConditionIntsEditorOnly != null)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginVertical();
					{
						foreach (var name in mmc.motionMatchingController.IntParameters)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								GUILayout.Label(name.Name);
								//EditorGUILayout.Toggle(pair.Value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						foreach (int value in mmc.ConditionIntsEditorOnly)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								EditorGUILayout.IntField(" ", value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawFloats()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayoutElements.DrawHeader("Float parameters:", GUIResources.GetMediumHeaderStyle_SM());
				GUILayout.Space(5);
			}
			GUILayout.EndHorizontal();

			if (mmc.ConditionFloatsEditorOnly != null)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginVertical();
					{
						foreach (var name in mmc.motionMatchingController.FloatParamaters)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								GUILayout.Label(name.Name);
								//EditorGUILayout.Toggle(pair.Value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						foreach (float value in mmc.ConditionFloatsEditorOnly)
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(20);
								EditorGUILayout.FloatField(" ", value);
							}
							GUILayout.EndHorizontal();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
		}

	}
}