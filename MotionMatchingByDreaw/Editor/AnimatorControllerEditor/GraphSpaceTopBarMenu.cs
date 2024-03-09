using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{

	public class GraphSpaceTopBarMenu : AnimatorEditorSpace
	{
		MotionMatchingLayer_SO SelectedLayer
		{
			get
			{
				if (Animator == null) return null;
				return Animator.SelectedLayer;
			}
		}

		SequenceDescription SelectedSequence
		{
			get
			{
				if (SelectedLayer == null || SelectedLayer.Sequences == null || SelectedLayer.Sequences.Count == 0) return null;


				return SelectedLayer.Sequences[Mathf.Clamp(SelectedLayer.SelectedSequenceIndex, 0, SelectedLayer.Sequences.Count - 1)];
			}
		}

		const float HorizontaMargin = 2f;
		string newSequenceName;


		TopBarGraphSpaceState currentState = TopBarGraphSpaceState.None;

		Rect stateRect;
		OnePixelColorTexture customMenuTexture = new OnePixelColorTexture(0.9f, 0.9f, 0.9f);


		public bool IsMenuAvtive => currentState != TopBarGraphSpaceState.None;

		public override void OnChangeAnimatorAsset()
		{

		}

		public override void OnEnable()
		{
			currentState = TopBarGraphSpaceState.None;
		}

		public override void PerfomrOnGUI(Event e)
		{
			OnGUI(e);
		}

		protected override void OnGUI(Event e)
		{
			if (SelectedLayer == null) return;

			GUILayout.BeginArea(Position);
			{
				GUILayout.Space(1f);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(HorizontaMargin);
					DrawSequenceSelection();
					GUILayout.Space(HorizontaMargin);
					if (GUILayout.Button("Edit", GUILayout.Height(20f), GUILayout.Width(50f)))
					{
						currentState = TopBarGraphSpaceState.EditSequence;

						newSequenceName = new string(SelectedSequence.Name.ToCharArray());
					}

					GUILayout.Space(HorizontaMargin);
					if (GUILayout.Button("+", GUILayout.Height(20f), GUILayout.Width(20f)))
					{
						newSequenceName = "Sequence";
						currentState = TopBarGraphSpaceState.AddSequence;
					}

					GUILayout.Space(HorizontaMargin);
					if (GUILayout.Button("-", GUILayout.Height(20f), GUILayout.Width(20f)) && SelectedLayer.Sequences.Count > 1)
					{
						if (SelectedLayer.Sequences.Count > 1 && SelectedLayer.IsSequenceEmpty(SelectedSequence.Name))
						{
							currentState = TopBarGraphSpaceState.RemoveSequence;
						}
					}

					GUILayout.Space(HorizontaMargin);
					if (GUILayout.Button("Move nodes", GUILayout.Height(20f), GUILayout.Width(80f)))
					{
						if (SelectedLayer.Sequences.Count > 1 &&
							(Editor.graphMenu.SelectedStates.Count > 0 || Editor.graphMenu.SelectedPortals.Count > 0 || Editor.graphMenu.SelectedState != null || Editor.graphMenu.SelectedPortal != null))
						{
							currentState = TopBarGraphSpaceState.ChangeSelectedNodesSequence;
							moveNodesDestinationSequenceIndex = SelectedLayer.SelectedSequenceIndex;
						}
					}

					GUILayout.Space(HorizontaMargin);
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndArea();


			stateRect = new Rect(
				Position.x + 10,
				Position.y + Position.height + 10,
				250f,
				66f
				);

			if (currentState != TopBarGraphSpaceState.None)
			{
				GUI.DrawTexture(stateRect, customMenuTexture.Texture);
			}

			stateRect = MMGUIUtility.ShrinkRect(stateRect, 3f);

			switch (currentState)
			{
				case TopBarGraphSpaceState.AddSequence:
					{
						GUILayout.BeginArea(stateRect);
						{
							AddSequenceMenu();
						}
						GUILayout.EndArea();
					}
					break;
				case TopBarGraphSpaceState.RemoveSequence:
					{
						GUILayout.BeginArea(stateRect);
						{
							RemoveSequenceMenu();
						}
						GUILayout.EndArea();
					}
					break;
				case TopBarGraphSpaceState.EditSequence:
					{
						GUILayout.BeginArea(stateRect);
						{
							EditSequenceMenu();
						}
						GUILayout.EndArea();
					}
					break;
				case TopBarGraphSpaceState.ChangeSelectedNodesSequence:
					{
						GUILayout.BeginArea(stateRect);
						{
							ChangeSelectedNodesSequenceMenu();
						}
						GUILayout.EndArea();
					}
					break;
			}

		}

		private void DrawSequenceSelection()
		{
			string[] sequencesNames = new string[SelectedLayer.Sequences.Count];

			for (int i = 0; i < SelectedLayer.Sequences.Count; i++)
			{
				var seq = SelectedLayer.Sequences[i];
				sequencesNames[i] = seq.Name;
			}


			int newSelectedIndex = EditorGUILayout.Popup(SelectedLayer.SelectedSequenceIndex, sequencesNames);

			if (SelectedLayer.SelectedSequenceIndex != newSelectedIndex &&
				0 <= newSelectedIndex && newSelectedIndex < SelectedLayer.Sequences.Count)
			{
				SelectedLayer.SelectedSequenceIndex = newSelectedIndex;

				Editor.graphMenu.ShowNodes();
			}
		}


		private void AddSequenceMenu()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.Label("Adding sequence:");
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Name");
					newSequenceName = EditorGUILayout.TextField(newSequenceName);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Cancel"))
					{
						currentState = TopBarGraphSpaceState.None;
					}

					if (GUILayout.Button("Add"))
					{
						SelectedLayer.AddSequence(newSequenceName);
						currentState = TopBarGraphSpaceState.None;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		private void RemoveSequenceMenu()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.Label("Remove sequence:");
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.Label(SelectedSequence.Name);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Cancel"))
					{
						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}

					if (GUILayout.Button("Remove"))
					{
						if (SelectedLayer.RemoveSequence(SelectedSequence.Name))
						{
							SelectedLayer.SelectedSequenceIndex -= 1;
						}
						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}


		int moveNodesDestinationSequenceIndex = 0;

		private void EditSequenceMenu()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.Label($"Edit sequence \"{SelectedSequence.Name}\"");
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("New name");
					newSequenceName = EditorGUILayout.TextField(newSequenceName);
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Cancel"))
					{
						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}

					if (GUILayout.Button("Apply"))
					{
						SelectedSequence.Name = newSequenceName;
						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		private void ChangeSelectedNodesSequenceMenu()
		{
			GUILayout.BeginVertical();
			{
				int selectedNodesCount = Editor.graphMenu.SelectedStates.Count + Editor.graphMenu.SelectedPortals.Count;

				if (Editor.graphMenu.SelectedState != null) selectedNodesCount += 1;
				if (Editor.graphMenu.SelectedPortal != null) selectedNodesCount += 1;

				GUILayout.Label($"Moving {selectedNodesCount} nodes to sequnece:");
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Sequence");

					string[] sequencesNames = new string[SelectedLayer.Sequences.Count];

					for (int i = 0; i < SelectedLayer.Sequences.Count; i++)
					{
						var seq = SelectedLayer.Sequences[i];
						sequencesNames[i] = seq.Name;
					}

					moveNodesDestinationSequenceIndex = EditorGUILayout.Popup(moveNodesDestinationSequenceIndex, sequencesNames);

				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Cancel"))
					{
						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}

					if (GUILayout.Button("Move nodes"))
					{
						int newID = SelectedLayer.Sequences[moveNodesDestinationSequenceIndex].ID;

						if (Editor.graphMenu.SelectedState != null) Editor.graphMenu.SelectedState.SequenceID = newID;
						if (Editor.graphMenu.SelectedPortal != null) Editor.graphMenu.SelectedPortal.SequenceID = newID;

						foreach (var state in Editor.graphMenu.SelectedStates)
						{
							state.SequenceID = newID;
						}

						foreach (var portal in Editor.graphMenu.SelectedPortals)
						{
							portal.SequenceID = newID;
						}

						currentState = TopBarGraphSpaceState.None;

						Editor.Repaint();
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}
	}

	public enum TopBarGraphSpaceState
	{
		None = 0,
		AddSequence,
		RemoveSequence,
		EditSequence,
		ChangeSelectedNodesSequence
	}

}