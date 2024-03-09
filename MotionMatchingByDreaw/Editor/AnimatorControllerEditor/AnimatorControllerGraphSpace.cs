using MotionMatching.Gameplay;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class AnimatorControllerGraphSpace : AnimatorEditorSpace
	{
		GraphEditorState currentState = GraphEditorState.None;

		// CONTEXT:

		public MotionMatchingLayer_SO SelectedLayer
		{
			get
			{
				if (Animator == null) return null;
				return Animator.SelectedLayer;
			}
		}
		public State_SO SelectedState
		{
			get => Animator.SelectedState;
			set
			{
				if (SelectedState != value)
				{
					Animator.SelectedState = value;
					m_SelectedPortal = null;

					m_SelectedTransition = null;
				}
			}
		}


		private Transition m_SelectedTransition;
		public Transition SelectedTransition
		{
			get => m_SelectedTransition;
			set
			{
				if (m_SelectedTransition != value)
				{
					m_SelectedTransition = value;

					Animator.SelectedState = null;
					m_SelectedPortal = null;

					SelectedStates.Clear();
				}
			}
		}



		private PortalToState m_SelectedPortal;
		public PortalToState SelectedPortal
		{
			get => m_SelectedPortal;
			set
			{
				if (m_SelectedPortal != value)
				{
					m_SelectedPortal = value;

					m_SelectedTransition = null;
					Animator.SelectedState = null;
					SelectedStates.Clear();
				}
			}
		}

		public int SelectedSequenceID
		{
			get
			{
				if (SelectedLayer == null
					|| SelectedLayer.Sequences == null
					|| SelectedLayer.SelectedSequenceIndex < 0
					|| SelectedLayer.SelectedSequenceIndex >= SelectedLayer.Sequences.Count)
				{
					return -1;
				}
				return SelectedLayer.Sequences[SelectedLayer.SelectedSequenceIndex].ID;
			}
		}


		public override void OnEnable()
		{

		}

		public override void OnChangeAnimatorAsset()
		{

		}

		public override void PerfomrOnGUI(Event e)
		{
			OnGUI(e);
		}

		protected override void OnGUI(Event e)
		{
			if (Animator == null || SelectedLayer == null) return;

			Rect notZoomedArea = Position;


			bool canPerformInputLogic = !Editor.TopBarMenu.IsMenuAvtive;

			if (notZoomedArea.Contains(e.mousePosition) && canPerformInputLogic)
			{
				ChangeZoom(e);
			}



			Position = EditorZoomArea.Begin(Animator.Zoom, Position);
			{
				DrawFullWireFrame();

				if (canPerformInputLogic)
				{

					switch (currentState)
					{
						case GraphEditorState.None:
							{
								if (!StartMovingSelectedStates(e))
								{
									if (!SelectingState(e))
									{
										if (!SelectingPortal(e))
										{
											if (!SelectingTransition(e))
											{
												CheckShouldStartSelection(e);
											}
										}
										else
										{
											CheckShouldMoveSelectedPortal(e);
										}
									}
									else
									{
										MoveSelectedStateInput(e);
									}
								}


								MoveAllNodes(e);
							}
							break;
						case GraphEditorState.SelectingNodes:
							{
								StatesSelectionUpdate(e);
							}
							break;
						case GraphEditorState.MakingTransition:
							{
								MakingTransitionUpdate(e);
							}
							break;
						case GraphEditorState.MovingSelectedNodes:
							{
								MoveSelectedNodes(e);
							}
							break;
						case GraphEditorState.MovingSelectedNode:
							{
								MoveSelectedNodeUpdate(e);
							}
							break;
						case GraphEditorState.MovingSelectedPortal:
							{
								MoveSelectedPortalUpdate(e);
							}
							break;
					}
				}



				//SelectingTransitionTest(e);
				//DrawingTestTransitions();

				DrawAllTransitions();
				DrawPortals();
				DrawStatesNodes();



			}
			EditorZoomArea.End();

			if (canPerformInputLogic)
			{
				ContextMenuInput(e, notZoomedArea);
			}
		}


		#region Wire frame 

		private const float bigWireframeSpacing = 300;
		private const float bigWireframeThicknes = 3f;

		private const float smallWireframeSpacing = 10f;
		private const float smallWireframeThicknes = 1f;

		Vector2 wireframeOffset = Vector2.zero;

		private void DrawFullWireFrame()
		{
			float scaledSmallpacing = smallWireframeSpacing;
			float scaledBigSpacing = bigWireframeSpacing;

			if (Animator != null && SelectedLayer != null)
			{
				wireframeOffset = SelectedLayer.MoveOffset;
			}

			Vector2 delta = new Vector2(
				wireframeOffset.x % scaledBigSpacing,
				wireframeOffset.y % scaledBigSpacing
				);

			DrawWireFrame(scaledSmallpacing, smallWireframeThicknes / Animator.Zoom, delta);
			DrawWireFrame(scaledBigSpacing, bigWireframeThicknes / Animator.Zoom, delta);

		}

		private void DrawWireFrame(
			float wireframeSpacing,
			float lineThicknes,
			Vector2 delta
			)
		{
			float lineWidth = Position.width + 2 * Mathf.Abs(bigWireframeSpacing);
			float lineheight = Position.height + 2 * Mathf.Abs(bigWireframeSpacing);

			int horizontalLinesCount = Mathf.CeilToInt(lineheight / wireframeSpacing);
			int verticalLinesCount = Mathf.CeilToInt(lineWidth / wireframeSpacing);

			for (int i = 0; i < horizontalLinesCount + 1; i++)
			{
				Rect rect = new Rect(
					-bigWireframeSpacing,
					i * wireframeSpacing - lineThicknes / 2f - bigWireframeSpacing,
					lineWidth,
					lineThicknes
					);

				rect.position += delta;

				GUI.DrawTexture(rect, Editor.GraphWireFrameTexture.Texture);
			}

			for (int i = 0; i < verticalLinesCount + 1; i++)
			{
				Rect rect = new Rect(
					i * wireframeSpacing - lineThicknes / 2f - bigWireframeSpacing,
					-bigWireframeSpacing,
					lineThicknes,
					lineheight
					);

				rect.position += delta;

				GUI.DrawTexture(rect, Editor.GraphWireFrameTexture.Texture);
			}
		}


		#endregion


		#region Zoom
		Vector2 zoomPivot;
		float minZoom = 0.25f;
		float maxZoom = 3;

		private void ChangeZoom(Event e)
		{
			//if (currentState == GraphEditorState.SelectingNodes) return;

			if (e.type == EventType.ScrollWheel)
			{
				if (e.delta.y != 0)
				{
					float scrollSensitivity = 0.02f;
					float oldZoom = Animator.Zoom;
					Animator.Zoom -= scrollSensitivity * Animator.Zoom * e.delta.y;
					Animator.Zoom = Mathf.Clamp(Animator.Zoom, minZoom, maxZoom);

					if (oldZoom != Animator.Zoom)
					{
						// On CHange zoom:
						RenitNodeTextStyle();

						OnZoomChangePositions(oldZoom, Animator.Zoom, e.mousePosition);

					}
					e.Use();
					Editor.Repaint();
				}
			}
		}

		private void OnZoomChangePositions(float oldZoom, float newZoom, Vector2 mousePosInOldZoom)
		{
			Vector2 oldMouse = mousePosInOldZoom - Position.position;

			Vector2 newMousePos = oldMouse * newZoom / oldZoom;

			Vector2 delta = (newMousePos - oldMouse) / newZoom;


			// updating layer delta
			SelectedLayer.MoveOffset -= delta;
			SelectedLayer.MoveOffset = new Vector2(
				SelectedLayer.MoveOffset.x % bigWireframeSpacing,
				SelectedLayer.MoveOffset.y % bigWireframeSpacing
				);


			// Updating nodes positions
			foreach (var state in SelectedLayer.States)
			{
				state.Node.Position -= delta;
			}

			if (SelectedLayer.Portals != null)
			{
				foreach (var portal in SelectedLayer.Portals)
				{
					portal.Node.Position -= delta;
				}
			}

			// Selection start position
			startSelectionMousePosition -= delta;
		}

		#endregion

		#region Context menu

		bool shouldCreateContextMenu = false;

		Vector2 createStateMousePos;

		private void CreateContextMenu(Event e)
		{
			GenericMenu menu = new GenericMenu();

			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Show nodes"), false, ShowNodes);
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Add State/Motion Matching"), false, AddState_CM<MotionMatchingState_SO>);
			menu.AddItem(new GUIContent("Add State/Single Animation"), false, AddState_CM<SingleAnimationState_SO>);
			menu.AddItem(new GUIContent("Add State/Contact"), false, AddState_CM<ContactState_SO>);
			menu.AddSeparator("Add State/");
			menu.AddItem(new GUIContent("Add State/Portal"), false, AddPortal);

			if (SelectedState == null && SelectedPortal == null)
			{
				menu.AddDisabledItem(new GUIContent("Remove state"));
			}
			else
			{
				menu.AddItem(new GUIContent("Remove state"), false, RemoveSelectedState_CM);
			}

			menu.AddSeparator("");

			if (CanSetStartNode())
			{
				menu.AddItem(new GUIContent("Set as start node"), false, SetAsStartNode);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Set as start node"));
			}

			menu.AddSeparator("");


			if (SelectedState != null)
			{
				menu.AddItem(new GUIContent("Make transtion"), false, StartMakingTransition);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Make transtion"));
			}

			if (SelectedTransition != null)
			{
				menu.AddItem(new GUIContent("Remove transtion"), false, RemoveTransition);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Remove transtion"));
			}


			menu.AddSeparator("");

			e.Use();
			Editor.Repaint();
			menu.ShowAsContext();
		}

		private void ContextMenuInput(Event e, Rect rect)
		{
			if (e.type == EventType.MouseDown)
			{
				if (rect.Contains(e.mousePosition) && e.button == 1)
				{
					createStateMousePos = e.mousePosition;

					CreateContextMenu(e);
					Editor.Repaint();
				}
			}


			//if (shouldCreateContextMenu)
			//{
			//	createStateMousePos = e.mousePosition;
			//	CreateContextMenu(e);
			//	shouldCreateContextMenu = false;
			//}
			//else if (e.type == EventType.MouseDown && e.button == 1)
			//{
			//	if (rect.Contains(e.mousePosition) && e.button == 1)
			//	{
			//		shouldCreateContextMenu = true;
			//		Editor.Repaint();
			//	}
			//}

		}


		public void ShowNodes()
		{
			int statesCountToMove = 0;
			Vector2 positionsSum = Vector2.zero;

			Vector2 halfNodeSize = nodeSize / 2f;


			foreach (var state in SelectedLayer.States)
			{
				if (state.SequenceID != SelectedSequenceID) continue;

				positionsSum += state.Node.Position + halfNodeSize;
				statesCountToMove += 1;
			}


			foreach (var portal in SelectedLayer.Portals)
			{
				if (portal.SequenceID != SelectedSequenceID) continue;

				positionsSum += portal.Node.Position + halfNodeSize;
				statesCountToMove += 1;
			}

			if (statesCountToMove == 0)
			{
				return;
			}


			Vector2 avarageStatesPositions = positionsSum / statesCountToMove;

			Vector2 delta = Position.size / 2f - avarageStatesPositions;

			MoveStatesWithWireFrame(delta);
		}

		private void AddState_CM<StateClass>() where StateClass : State_SO
		{
			StateClass state = SelectedLayer.AddState<StateClass>();

			state.Node = new StateNode(Vector2.zero);
			state.SequenceID = SelectedSequenceID;

			Vector2 localMousePosition = (createStateMousePos - Position.position) / Animator.Zoom;

			state.Node.Position = GetStateNewPos(localMousePosition);
			Editor.Repaint();
		}

		private void RemoveSelectedState_CM()
		{
			if (SelectedState != null)
			{
				SelectedLayer.RemoveState(SelectedState);
				SelectedState = null;
			}
			else if (SelectedPortal != null)
			{
				SelectedLayer.RemovePortal(SelectedPortal);
				SelectedPortal = null;
			}

			Editor.Repaint();
		}


		private void AddPortal()
		{
			PortalToState portal = SelectedLayer.CreatePortal(createStateMousePos);
			portal.SequenceID = SelectedSequenceID;

			Vector2 localMousePosition = (createStateMousePos - Position.position) / Animator.Zoom;

			portal.Node.Position = GetStateNewPos(localMousePosition);
		}

		private bool CanSetStartNode()
		{
			if (SelectedLayer != null && SelectedState != null && SelectedState.StateType != MotionMatchingStateType.ContactAnimationState)
			{
				if (SelectedLayer.StartStateData != null && SelectedLayer.StartStateData.StartState == SelectedState)
				{
					return false;
				}
				else
				{
					return true;
				}

			}
			return false;
		}
		private void SetAsStartNode()
		{
			if (CanSetStartNode())
			{
				SelectedLayer.SetStartState(SelectedState);
			}
		}


		#region Making Transition

		State_SO makingTransitionFromState;
		Color transitionCreationColor = new Color(0.96f, 0.46f, 0.42f);
		private void StartMakingTransition()
		{
			makingTransitionFromState = SelectedState;

			currentState = GraphEditorState.MakingTransition;
		}

		private void MakingTransitionUpdate(Event e)
		{
			Vector2 transitionFrom = makingTransitionFromState.Node.Position + nodeSize / 2f;

			float transitionThicknes = transitionLineThickens;

			DrawTransitionBetweenPoints(transitionFrom, e.mousePosition, transitionCreationColor, transitionThicknes);

			if (e.type == EventType.MouseDown && e.button == 0)
			{
				OnEndMakingTransition(e);
			}

			Editor.Repaint();
		}

		private void OnEndMakingTransition(Event e)
		{
			if (SelectedLayer.States != null)
			{
				foreach (var state in SelectedLayer.States)
				{
					if (state.StateType == MotionMatchingStateType.ContactAnimationState) continue;

					if (state.SequenceID != SelectedSequenceID) continue;
					Rect stateRect = new Rect(state.Node.Position, nodeSize);

					if (stateRect.Contains(e.mousePosition))
					{
						makingTransitionFromState.AddTransitionToState(state);
						break;
					}
				}
			}


			if (SelectedLayer.Portals != null)
			{
				for (int idx = 0; idx < SelectedLayer.Portals.Count; idx++)
				{
					PortalToState portal = SelectedLayer.Portals[idx];
					if (portal.State == null) continue;

					if (portal.SequenceID != SelectedSequenceID) continue;
					Rect stateRect = new Rect(portal.Node.Position, nodeSize);

					if (stateRect.Contains(e.mousePosition))
					{
						makingTransitionFromState.AddTransitionToPortal(portal, idx);
						break;
					}
				}

			}

			currentState = GraphEditorState.None;
			makingTransitionFromState = null;
			Editor.Repaint();
		}

		private void RemoveTransition()
		{
			SelectedLayer.RemoveTransition(SelectedTransition);

			SelectedTransition = null;

			Editor.Repaint();
		}

		#endregion

		#endregion


		#region States
		public const float SelectedNodeThicknes = 3f;

		GUIStyle nodeTextStyle = null;
		float nodeFontSize = 8f;



		Vector2 nodeSize = new Vector2(140, 30);

		private void InitNodeTextStyle()
		{
			if (nodeTextStyle == null)
			{
				RenitNodeTextStyle();
			}
		}

		private void RenitNodeTextStyle()
		{
			int styleFontSize = Mathf.FloorToInt(nodeFontSize);
			TextAnchor alingment = TextAnchor.MiddleCenter;
			nodeTextStyle = new GUIStyle();
			nodeTextStyle.fontSize = styleFontSize;
			nodeTextStyle.alignment = alingment;
		}

		private void DrawStatesNodes()
		{
			InitNodeTextStyle();

			if (SelectedLayer != null)
			{
				if (SelectedLayer.States == null)
				{
					SelectedLayer.States = new List<State_SO>();
				}

				for (int stateIdx = 0; stateIdx < SelectedLayer.States.Count; stateIdx++)
				{
					State_SO state = SelectedLayer.States[stateIdx];

					if (state.SequenceID != SelectedSequenceID) continue;
					if (state == SelectedState) continue;

					DrawStateNode(state);
				}

				if (SelectedState != null && SelectedState.SequenceID == SelectedSequenceID)
				{
					DrawStateNode(SelectedState);
				}
			}
		}

		private void DrawStateNode(State_SO state)
		{
			if (state == null) return;



			Rect inRect = new Rect(
				(state.Node.Position),
				nodeSize
				);




			if (state == SelectedState || SelectedStates.Contains(state))
			{
				Rect selectionRect = MMGUIUtility.ShrinkRect(inRect, 1);
				GUI.DrawTexture(selectionRect, Editor.SelectedStateTexture.Texture);
			}

			Rect stateRect = MMGUIUtility.ShrinkRect(inRect, SelectedNodeThicknes);

			if (SelectedLayer.GetStartState() == state)
			{
				GUI.DrawTexture(stateRect, Editor.StartStateTexture.Texture);
			}
			else
			{
				switch (state.StateType)
				{
					case MotionMatchingStateType.MotionMatching:
					case MotionMatchingStateType.SingleAnimation:
						{
							GUI.DrawTexture(stateRect, Editor.NormalStateTexture.Texture);
						}
						break;
					case MotionMatchingStateType.ContactAnimationState:
						{
							GUI.DrawTexture(stateRect, Editor.ContactStateTexture.Texture);
						}
						break;
				}
			}

			GUI.Label(inRect, new GUIContent(state.Name), nodeTextStyle);
		}


		private bool SelectingState(Event e)
		{
			if (e.type == EventType.MouseDown && (e.button == 0 /*|| e.button == 1*/))
			{
				Vector2 mousePos = e.mousePosition;

				for (int idx = SelectedLayer.States.Count - 1; idx >= 0; idx--)
				{
					State_SO state = SelectedLayer.States[idx];

					if (state.SequenceID != SelectedSequenceID) continue;

					if (TrySelectState(state, mousePos))
					{
						Editor.Repaint();
						return true;
					}
				}

				SelectedState = null;
				Editor.Repaint();
			}

			return false;
		}


		private bool TrySelectState(State_SO state, Vector2 mousePos)
		{
			if (state == null) return false;


			if (IsPositionOverNode(state.Node.Position, mousePos))
			{
				SelectedState = state;
				return true;
			}

			return false;
		}

		private bool IsPositionOverNode(Vector2 nodePos, Vector2 mousePos)
		{
			//mousePos *= Zoom;
			Rect nodeRect = new Rect(
				nodePos,
				nodeSize
				);

			return nodeRect.Contains(mousePos);
		}


		//private void SetStatePosition(State_SO state, Vector2 desiredPos)
		//{
		//	Vector2 finalPos = desiredPos;

		//	Vector2 offset = SelectedLayer.MoveOffset;
		//	offset.x = offset.x % smallWireframeSpacing;
		//	offset.y = offset.y % smallWireframeSpacing;

		//	finalPos.x = Mathf.RoundToInt(finalPos.x / smallWireframeSpacing) * smallWireframeSpacing;
		//	finalPos.y = Mathf.RoundToInt(finalPos.y / smallWireframeSpacing) * smallWireframeSpacing;

		//	state.Node.Position = finalPos + offset;
		//}

		private Vector2 GetStateNewPos(Vector2 desiredPos)
		{
			Vector2 finalPos = desiredPos;

			Vector2 offset = SelectedLayer.MoveOffset;
			offset.x = offset.x % smallWireframeSpacing;
			offset.y = offset.y % smallWireframeSpacing;

			finalPos.x = Mathf.RoundToInt(finalPos.x / smallWireframeSpacing) * smallWireframeSpacing;
			finalPos.y = Mathf.RoundToInt(finalPos.y / smallWireframeSpacing) * smallWireframeSpacing;

			return finalPos + offset;
		}

		// Moving all nodes:

		bool isMovingAllStates = false;
		Vector2 startMovingAllStatePos;
		Color transitionsColor = new Color(1f, 1f, 1f);

		private void MoveAllNodes(Event e)
		{
			if (e.type == EventType.MouseDown && e.button == 2)
			{
				isMovingAllStates = true;
				startMovingAllStatePos = e.mousePosition;
			}
			else if (e.type == EventType.MouseUp || !IsMouseInside(e.mousePosition))
			{
				isMovingAllStates = false;
			}
			else if (isMovingAllStates)
			{
				Vector2 delta = e.mousePosition - startMovingAllStatePos;


				startMovingAllStatePos = e.mousePosition;

				MoveStatesWithWireFrame(delta);

				Editor.Repaint();
			}
		}

		private void MoveStatesWithWireFrame(Vector2 delta)
		{
			SelectedLayer.MoveOffset += delta;

			SelectedLayer.MoveOffset = new Vector2(
				SelectedLayer.MoveOffset.x % bigWireframeSpacing,
				SelectedLayer.MoveOffset.y % bigWireframeSpacing
				);

			foreach (var state in SelectedLayer.States)
			{
				//if (state.SequenceID != SelectedSequenceID) continue;

				state.Node.Position += delta;
			}

			if (SelectedLayer.Portals != null)
			{
				foreach (var portal in SelectedLayer.Portals)
				{
					//if (portal.SequenceID != SelectedSequenceID) continue;

					portal.Node.Position += delta;
				}
			}
		}

		#endregion

		#region Moving selected node

		Vector2 movingStateOffset;

		private void OnStartMovingSelectedNode(Event e)
		{
			currentState = GraphEditorState.MovingSelectedNode;

			movingStateOffset = e.mousePosition - SelectedState.Node.Position;

			movingStateOffset.x = Mathf.Round(movingStateOffset.x / smallWireframeSpacing) * smallWireframeSpacing;
			movingStateOffset.y = Mathf.Round(movingStateOffset.y / smallWireframeSpacing) * smallWireframeSpacing;

			Editor.Repaint();

		}


		private void MoveSelectedStateInput(Event e)
		{
			if (SelectedState == null)
			{
				return;
			}

			if (e.type == EventType.MouseDown && (e.button == 0 /*|| e.button == 1*/))
			{
				if (IsPositionOverNode(SelectedState.Node.Position, e.mousePosition))
				{
					OnStartMovingSelectedNode(e);
				}
			}

		}

		private void MoveSelectedNodeUpdate(Event e)
		{
			if (e.button != 2)
			{
				if (e.type == EventType.MouseUp || !IsMouseInside(e.mousePosition))
				{
					OnEndMovingSelectedNode();
				}
				else if (e.type == EventType.MouseDrag)
				{
					Vector2 offset = SelectedLayer.MoveOffset;
					offset.x = offset.x % smallWireframeSpacing;
					offset.y = offset.y % smallWireframeSpacing;
					Vector2 finalPos = e.mousePosition - movingStateOffset - offset;
					SelectedState.Node.Position = GetStateNewPos(finalPos);
					Editor.Repaint();

				}
			}

			Editor.Repaint();
		}

		private void OnEndMovingSelectedNode()
		{
			currentState = GraphEditorState.None;
			Editor.Repaint();
		}

		#endregion

		#region Portals
		Vector2 deltaToSelectedPortal;

		PortalMarkGUIStyle portalMarkerStyle = new PortalMarkGUIStyle(6f);

		private void DrawPortal(PortalToState portal)
		{
			if (portal == null) return;

			Rect inRect = new Rect(
				(portal.Node.Position),
				nodeSize
				);


			if (portal == SelectedPortal || SelectedPortals.Contains(portal))
			{
				Rect selectionRect = MMGUIUtility.ShrinkRect(inRect, 1);
				GUI.DrawTexture(selectionRect, Editor.SelectedStateTexture.Texture);
			}

			Rect portalRect = MMGUIUtility.ShrinkRect(inRect, SelectedNodeThicknes);

			GUI.DrawTexture(portalRect, Editor.PortalStateTexture.Texture);

			Rect portalMarkRect = MMGUIUtility.ShrinkRect(portalRect, 1f);
			GUI.Label(portalMarkRect, new GUIContent("Portal"), portalMarkerStyle.Style);

			if (portal.State != null)
			{
				GUI.Label(portalRect, new GUIContent(portal.State.Name), nodeTextStyle);
			}
			else
			{
				GUI.Label(portalRect, new GUIContent("Empty"), nodeTextStyle);
			}
		}

		private void DrawPortals()
		{
			if (SelectedLayer.Portals == null) return;

			foreach (var portal in SelectedLayer.Portals)
			{
				if (portal.SequenceID != SelectedSequenceID) continue;
				DrawPortal(portal);
			}
		}

		private bool SelectingPortal(Event e)
		{
			if (SelectedLayer.Portals == null) return false;

			if (e.type == EventType.MouseDown && e.button == 0)
			{
				foreach (var portal in SelectedLayer.Portals)
				{
					if (portal.SequenceID != SelectedSequenceID) continue;


					if (IsPositionOverNode(portal.Node.Position, e.mousePosition))
					{
						SelectedPortal = portal;

						Editor.Repaint();
						return true;
					}
				}
				SelectedPortal = null;
				Editor.Repaint();
			}

			return false;
		}


		private void CheckShouldMoveSelectedPortal(Event e)
		{
			if (SelectedPortal == null)
			{
				return;
			}

			if (e.type == EventType.MouseDown && (e.button == 0 /*|| e.button == 1*/))
			{
				if (IsPositionOverNode(SelectedPortal.Node.Position, e.mousePosition))
				{
					OnStartMovingSelectedPortal(e);
				}
			}
		}

		private void OnStartMovingSelectedPortal(Event e)
		{
			currentState = GraphEditorState.MovingSelectedPortal;

			deltaToSelectedPortal = e.mousePosition - SelectedPortal.Node.Position;
			Editor.Repaint();
		}

		private void OnEndMovingSelectedPortal()
		{
			currentState = GraphEditorState.None;
		}

		private void MoveSelectedPortalUpdate(Event e)
		{
			if (e.type == EventType.MouseUp && e.button == 0)
			{
				OnEndMovingSelectedPortal();
			}
			else if (e.type == EventType.MouseDrag || !IsMouseInside(e.mousePosition))
			{
				Vector2 offset = SelectedLayer.MoveOffset;
				offset.x = offset.x % smallWireframeSpacing;
				offset.y = offset.y % smallWireframeSpacing;
				Vector2 finalPos = e.mousePosition - deltaToSelectedPortal - offset;
				SelectedPortal.Node.Position = GetStateNewPos(finalPos);
				Editor.Repaint();
			}

			Editor.Repaint();
		}



		#endregion

		#region Drawing transition
		const float transitionLineThickens = 2f;

		float transitionArrowBackDist = 6f;
		float arrowWidth = 3f;


		float distanceBetweenTransitions = 4f;
		float distToTransitionToSelect = 3f;


		TransitionColorTexture transitionTexture = new TransitionColorTexture(1f, 1f, 1f, 1f, true, 5);

		Color normaltransitionColor = new Color(1f, 1, 1);
		Color selectedTransitionColor = new Color(122f / 255f, 209f / 255f, 255f / 255f);


		private void DrawTransitionBetweenStates(
			State_SO from,
			State_SO to,
			Color color,
			float lineThickens
			)
		{
			Rect firstRect = new Rect(from.Node.Position, nodeSize);
			Rect secondRect = new Rect(to.Node.Position, nodeSize);

			Vector2 fCenter = firstRect.center;
			Vector2 tCenter = secondRect.center;


			Vector2 delta = tCenter - fCenter;

			if (delta == Vector2.zero) return;

			Vector2 perpendicularDelta = Vector2.Perpendicular(delta).normalized * distanceBetweenTransitions;

			Vector2 start = fCenter + perpendicularDelta;
			Vector2 end = tCenter + perpendicularDelta;

			DrawTransitionBetweenPoints(start, end, color, lineThickens);
		}

		private void DrawTransitionBetweenPoints(Vector2 from, Vector2 to, Color color, float lineThickens)
		{
			Vector2 delta = to - from;

			if (delta == Vector2.zero) return;

			Vector2 start = from;
			Vector2 end = to;
			Handles.DrawBezier(
				start,
				end,
				start,
				end,
				color,
				transitionTexture.Texture,
				lineThickens
				);

			float delatMagnitude = delta.magnitude;
			Vector2 normalizedDelta = delta.normalized;
			Vector2 perpendicularDeltaNormalized = Vector2.Perpendicular(normalizedDelta);

			//Vector2 firstArmDir = RotateVector(-delta, transitionAngle).normalized * transitionDirArrowArmsLenght;
			//Vector2 secondArmDir = RotateVector(-delta, -transitionAngle).normalized * transitionDirArrowArmsLenght;

			Vector2 transitionCenter = from + delta / 2f + normalizedDelta * transitionArrowBackDist / 2f;


			Vector2 firstArrowArmEnd = transitionCenter - normalizedDelta * transitionArrowBackDist + perpendicularDeltaNormalized * arrowWidth;
			Vector2 secondArrowArmEnd = transitionCenter - normalizedDelta * transitionArrowBackDist - perpendicularDeltaNormalized * arrowWidth;

			Handles.DrawBezier(
				firstArrowArmEnd,
				transitionCenter,
				transitionCenter,
				firstArrowArmEnd,
				color,
				transitionTexture.Texture,
				lineThickens
				);

			Handles.DrawBezier(
				secondArrowArmEnd,
				transitionCenter,
				transitionCenter,
				secondArrowArmEnd,
				color,
				transitionTexture.Texture,
				lineThickens
				);

		}

		private void DrawTransitionsBetwenNodes(Vector2 firsNodePos, Vector2 secondNodePos, Color color, float lineThickens)
		{
			Rect firstRect = new Rect(firsNodePos, nodeSize);
			Rect secondRect = new Rect(secondNodePos, nodeSize);

			Vector2 fCenter = firstRect.center;
			Vector2 tCenter = secondRect.center;


			Vector2 delta = tCenter - fCenter;

			if (delta == Vector2.zero) return;

			Vector2 perpendicularDelta = Vector2.Perpendicular(delta).normalized * distanceBetweenTransitions;

			Vector2 start = fCenter + perpendicularDelta;
			Vector2 end = tCenter + perpendicularDelta;

			DrawTransitionBetweenPoints(start, end, color, lineThickens);
		}

		Vector2 RotateVector(Vector2 vector, float angle)
		{
			return Quaternion.Euler(0, 0, angle) * vector;
		}

		private bool IsMouseOverTransition(Vector2 fromNode, Vector2 toNode, Vector2 mousePos)
		{
			Rect firstRect = new Rect(fromNode, nodeSize);
			Rect secondRect = new Rect(toNode, nodeSize);

			Vector2 fCenter = firstRect.center;
			Vector2 tCenter = secondRect.center;


			Vector2 delta = tCenter - fCenter;

			if (delta == Vector2.zero) return false;

			Vector2 perpendicularDelta = Vector2.Perpendicular(delta).normalized * distanceBetweenTransitions;

			Vector2 start = fCenter + perpendicularDelta;
			Vector2 end = tCenter + perpendicularDelta;

			Rect fullTransitionRect = new Rect(
				start.x < end.x ? start.x : end.x,
				start.y < end.y ? start.y : end.y,
				Mathf.Abs(delta.x),
				Mathf.Abs(delta.y)
				);

			fullTransitionRect = MMGUIUtility.AddOutlineToRect(fullTransitionRect, distToTransitionToSelect);

			if (!fullTransitionRect.Contains(mousePos)) return false;

			if (start.x == end.x)
			{
				return Mathf.Abs(mousePos.x - start.x) <= distToTransitionToSelect;
			}
			else if (start.y == end.y)
			{
				return Mathf.Abs(mousePos.y - start.y) <= distToTransitionToSelect;
			}
			else
			{
				StraightLineFromPoints line = new StraightLineFromPoints(start, end);
				float distToLine = line.DistToLine(mousePos);

				return distToLine <= distToTransitionToSelect;
			}
		}


		private void DrawAllTransitions()
		{
			float transitionThicknes = transitionLineThickens;

			if (SelectedLayer.States == null) return;

			foreach (var state in SelectedLayer.States)
			{
				if (state.SequenceID != SelectedSequenceID) continue;
				if (state.Transitions == null) continue;

				foreach (var transition in state.Transitions)
				{
					Color transitionColor = transition == SelectedTransition ? selectedTransitionColor : normaltransitionColor;

					if (transition.IsToPortal)
					{
						PortalToState portal = SelectedLayer.Portals[transition.PortalToStateIndex];
						DrawTransitionsBetwenNodes(state.Node.Position, portal.Node.Position, transitionColor, transitionThicknes);
					}
					else
					{
						DrawTransitionBetweenStates(state, transition.ToState, transitionColor, transitionThicknes);
					}
				}
			}
		}

		private bool SelectingTransition(Event e)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				Vector2 mousePos = e.mousePosition;

				foreach (var state in SelectedLayer.States)
				{
					if (state.SequenceID != SelectedSequenceID) continue;


					if (state.Transitions == null) continue;
					foreach (var transition in state.Transitions)
					{
						if (transition.IsToPortal)
						{
							PortalToState portal = SelectedLayer.Portals[transition.PortalToStateIndex];
							if (IsMouseOverTransition(state.Node.Position, portal.Node.Position, mousePos))
							{
								e.Use();
								SelectedTransition = transition;
								return true;
							}
						}
						else if (IsMouseOverTransition(state.Node.Position, transition.ToState.Node.Position, mousePos))
						{
							e.Use();
							SelectedTransition = transition;
							return true;
						}
					}
				}

				if (SelectedLayer.Portals != null)
				{

				}

				SelectedTransition = null;
			}
			return false;
		}
		#endregion

		#region Selecting multiple states
		public List<State_SO> SelectedStates = new List<State_SO>();
		List<Vector2> selectedStatesMoveOffsets = new List<Vector2>();

		public List<PortalToState> SelectedPortals = new List<PortalToState>();
		List<Vector2> selectedStatesMoveOffsetsToPortals = new List<Vector2>();


		Vector2 startSelectionMousePosition;
		float startSelectionZoom;

		float selectionOutlineThicknes = 1f;


		OnePixelColorTexture selectionMultipleNodesTexture = new OnePixelColorTexture(0.4f, 0.71f, 1f, 0.25f);
		OnePixelColorTexture selectionMultipleNodesOutlineTexture = new OnePixelColorTexture(1f, 1f, 1f);

		private bool CheckShouldStartSelection(Event e)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				startSelectionMousePosition = e.mousePosition;
				OnStartSelecting(e);
				return true;
			}

			return false;
		}

		private void OnStartSelecting(Event e)
		{
			currentState = GraphEditorState.SelectingNodes;
			startSelectionMousePosition = e.mousePosition;
			startSelectionZoom = Animator.Zoom;
		}

		private void StatesSelectionUpdate(Event e)
		{
			SelectedStates.Clear();

			Vector2 oldMousePos = startSelectionMousePosition;

			Vector2 rectPos = oldMousePos;

			Vector2 selectionRectSize = e.mousePosition - oldMousePos;



			Rect selectionRect = new Rect(
				e.mousePosition.x < rectPos.x ? e.mousePosition.x : rectPos.x,
				e.mousePosition.y < rectPos.y ? e.mousePosition.y : rectPos.y,
				Mathf.Abs(selectionRectSize.x),
				Mathf.Abs(selectionRectSize.y)
				);

			float outline = selectionOutlineThicknes / Animator.Zoom;

			Rect upOutline = new Rect(
				selectionRect.position + new Vector2(-outline, -outline),
				new Vector2(selectionRect.width + 2f * outline, outline)
				);

			Rect downOutline = new Rect(
				new Vector2(selectionRect.position.x - outline, selectionRect.position.y + selectionRect.height),
				new Vector2(upOutline.width, outline)
				);
			Rect leftOutline = new Rect(
				new Vector2(selectionRect.position.x - outline, selectionRect.position.y),
				new Vector2(outline, selectionRect.height)
				);
			Rect rightOutline = new Rect(
				new Vector2(selectionRect.position.x + selectionRect.width, selectionRect.position.y),
				new Vector2(outline, selectionRect.height)
				);


			SelectedStates.Clear();
			foreach (var state in SelectedLayer.States)
			{
				if (state.SequenceID != SelectedSequenceID) continue;

				Rect stateRect = new Rect(state.Node.Position, nodeSize);

				if (stateRect.Overlaps(selectionRect))
				{
					SelectedStates.Add(state);
				}
			}


			SelectedPortals.Clear();
			if (SelectedLayer.Portals != null)
			{
				foreach (var portal in SelectedLayer.Portals)
				{
					if (portal.SequenceID != SelectedSequenceID) continue;

					Rect portalRect = new Rect(portal.Node.Position, nodeSize);

					if (portalRect.Overlaps(selectionRect))
					{
						SelectedPortals.Add(portal);
					}
				}
			}




			GUI.DrawTexture(selectionRect, selectionMultipleNodesTexture.Texture);
			GUI.DrawTexture(upOutline, selectionMultipleNodesOutlineTexture.Texture);
			GUI.DrawTexture(downOutline, selectionMultipleNodesOutlineTexture.Texture);
			GUI.DrawTexture(leftOutline, selectionMultipleNodesOutlineTexture.Texture);
			GUI.DrawTexture(rightOutline, selectionMultipleNodesOutlineTexture.Texture);

			if (e.type == EventType.MouseUp && e.button == 0)
			{
				OnEndStatesSelection();
			}

			Editor.Repaint();
		}

		private void OnEndStatesSelection()
		{
			currentState = GraphEditorState.None;
			Editor.Repaint();
		}


		private bool StartMovingSelectedStates(Event e)
		{
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (SelectedStates != null && SelectedStates.Count > 0)
				{
					foreach (var state in SelectedStates)
					{
						if (state.SequenceID != SelectedSequenceID) continue;

						Rect stateRect = new Rect(state.Node.Position, nodeSize);

						if (stateRect.Contains(e.mousePosition))
						{
							OnStartMovingNodes(e);
							return true;
						}
					}
				}

				if (SelectedPortals != null && SelectedPortals.Count > 0)
				{
					foreach (var portal in SelectedPortals)
					{
						if (portal.SequenceID != SelectedSequenceID) continue;
						Rect portalRect = new Rect(portal.Node.Position, nodeSize);

						if (portalRect.Contains(e.mousePosition))
						{
							OnStartMovingNodes(e);
							return true;
						}
					}
				}
			}

			return false;
		}

		private void OnStartMovingNodes(Event e)
		{
			currentState = GraphEditorState.MovingSelectedNodes;

			if (selectedStatesMoveOffsets == null) selectedStatesMoveOffsets = new List<Vector2>();
			selectedStatesMoveOffsets.Clear();

			foreach (var state in SelectedStates)
			{
				Vector2 offset = e.mousePosition - state.Node.Position;

				offset.x = Mathf.Round(offset.x / smallWireframeSpacing) * smallWireframeSpacing;
				offset.y = Mathf.Round(offset.y / smallWireframeSpacing) * smallWireframeSpacing;


				selectedStatesMoveOffsets.Add(offset);
			}

			selectedStatesMoveOffsetsToPortals.Clear();
			foreach (var portal in SelectedPortals)
			{
				Vector2 offset = e.mousePosition - portal.Node.Position;

				offset.x = Mathf.Round(offset.x / smallWireframeSpacing) * smallWireframeSpacing;
				offset.y = Mathf.Round(offset.y / smallWireframeSpacing) * smallWireframeSpacing;


				selectedStatesMoveOffsetsToPortals.Add(offset);
			}
		}

		private void MoveSelectedNodes(Event e)
		{
			Vector2 offsetFromLayer = SelectedLayer.MoveOffset;
			offsetFromLayer.x = offsetFromLayer.x % smallWireframeSpacing;
			offsetFromLayer.y = offsetFromLayer.y % smallWireframeSpacing;


			for (int i = 0; i < SelectedStates.Count; i++)
			{
				Vector2 currentStateOffset = selectedStatesMoveOffsets[i];
				State_SO currentState = SelectedStates[i];

				Vector2 finalPos = e.mousePosition - currentStateOffset - offsetFromLayer;

				currentState.Node.Position = GetStateNewPos(finalPos);
			}


			for (int i = 0; i < SelectedPortals.Count; i++)
			{
				Vector2 currentStateOffset = selectedStatesMoveOffsetsToPortals[i];
				PortalToState currentPortal = SelectedPortals[i];

				Vector2 finalPos = e.mousePosition - currentStateOffset - offsetFromLayer;

				currentPortal.Node.Position = GetStateNewPos(finalPos);
			}

			if (e.type == EventType.MouseUp && e.button == 0 || !IsMouseInside(e.mousePosition))
			{
				OnEndMovingNodes();
			}

			Editor.Repaint();
		}

		private void OnEndMovingNodes()
		{
			currentState = GraphEditorState.None;
		}
		#endregion

		private bool IsMouseInside(Vector2 mousePos)
		{
			Rect contains = Position;
			contains.position = Vector2.zero;
			return contains.Contains(mousePos);
		}
	}

	public enum GraphEditorState
	{
		None = 0,
		SelectingNodes,
		MakingTransition,

		MovingSelectedNode,
		MovingSelectedNodes,
		MovingSelectedPortal
	}

	public struct StraightLineFromPoints
	{
		float A;
		float B;

		public StraightLineFromPoints(Vector2 p1, Vector2 p2)
		{
			p1.y *= -1;
			p2.y *= -1;

			if (p1.x > p2.x)
			{
				Vector2 buffor = p1;
				p1 = p2;
				p2 = buffor;
			}

			A = (p1.y - p2.y) / (p1.x - p2.x);
			B = p1.y - A * p1.x;
		}

		public float DistToLine(Vector2 fromPoint)
		{
			return Mathf.Abs(A * fromPoint.x + fromPoint.y + B) / Mathf.Sqrt(A * A + 1);
		}
	}

	public class PortalMarkGUIStyle
	{
		float fontSize;
		GUIStyle m_Style;

		public PortalMarkGUIStyle(float fontSize)
		{
			this.fontSize = fontSize;
		}

		public GUIStyle Style
		{
			get
			{
				if (m_Style == null)
				{
					int styleFontSize = Mathf.FloorToInt(fontSize);
					TextAnchor alingment = TextAnchor.UpperLeft;
					m_Style = new GUIStyle();
					m_Style.fontSize = styleFontSize;
					m_Style.alignment = alingment;
				}

				return m_Style;
			}
		}
	}


	public class TransitionColorTexture
	{
		Color color;
		Texture2D texture;

		bool aa;
		int level;

		public Texture2D Texture
		{
			get
			{
				if (texture == null)
				{
					texture = new Texture2D(1, level + 2);
					texture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0f));
					texture.SetPixel(0, level + 1, new Color(1f, 1f, 1f, 0f));
					for (int i = 0; i < level; i++)
					{
						texture.SetPixel(0, i + 1, color);
					}
					texture.Apply();
				}
				return texture;
			}
		}

		public TransitionColorTexture(float r, float g, float b, float a = 1f, bool aa = false, int level = 10)
		{
			this.level = level;
			this.aa = aa;
			this.color = new Color(r, g, b, a);
		}
	}
}