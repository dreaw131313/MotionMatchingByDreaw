using MotionMatching.Gameplay;
using MotionMatching.Tools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class AnimatorControllerEditorLeftMenuSpace : AnimatorEditorSpace
	{
		Vector2 scrollPosition;

		// Margins:
		const float VerticalMargin = 5f;

		public override void OnEnable()
		{
			OnChangeAnimatorAsset();
		}

		public override void OnChangeAnimatorAsset()
		{
			if (Animator == null) return;

			primaryLayersRList = new ReorderableList(Animator.Layers, typeof(MotionMatchingLayer_SO), false, true, true, false);
			secondaryLayersRList = new ReorderableList(Animator.SecondaryLayers, typeof(SecondaryLayerData), true, true, true, true);

			InitParamtersList();
		}

		protected override void OnGUI(Event e)
		{
			Rect rect = MMGUIUtility.MakeMargins(
				Position,
				AnimatorControllerEditor.MenuHorizontalMargin,
				AnimatorControllerEditor.ResizeMargin,
				AnimatorControllerEditor.MenuVerticalMargin,
				AnimatorControllerEditor.MenuVerticalMargin
				);

			GUILayout.BeginArea(rect);
			{
				OnGUIInternal(e);
			}
			GUILayout.EndArea();
		}

		private void OnGUIInternal(Event e)
		{
			DrawAnimatorField();
			GUILayout.Space(VerticalMargin);

			if (Animator == null) return;

			DrawMenuSelection();
			GUILayout.Space(VerticalMargin);

			switch (CurrentMenu)
			{
				case MenuType.Layers:
					{
						DrawLayersMenu();
					}
					break;
				case MenuType.Paramters:
					{
						DrawParametersMenu();
					}
					break;
			}
		}


		private void DrawAnimatorField()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Animator");
				MotionMatchingAnimator_SO buffor = Editor.Animator;

				Editor.Animator = (MotionMatchingAnimator_SO)EditorGUILayout.ObjectField(Editor.Animator, typeof(MotionMatchingAnimator_SO), false);

				if (buffor != Editor.Animator)
				{
					//Editor.Animator = buffor;

					Editor.leftMenu.OnChangeAnimatorAsset();
					Editor.graphMenu.OnChangeAnimatorAsset();
					Editor.rightMenu.OnChangeAnimatorAsset();
				}
			}
			GUILayout.EndHorizontal();
		}

		private enum MenuType
		{
			Layers,
			Paramters
		}

		MenuType CurrentMenu = MenuType.Layers;

		private enum LayersType
		{
			Main,
			Secondary
		}

		LayersType CurrentLayersType;


		private void DrawMenuSelection()
		{
			GUILayout.BeginHorizontal();
			{
				CurrentMenu = (MenuType)EditorGUILayout.EnumPopup(CurrentMenu);
			}
			GUILayout.EndHorizontal();
		}

		#region LayersMenu

		ReorderableList primaryLayersRList;
		ReorderableList secondaryLayersRList;

		private void DrawLayersMenu()
		{
			//GUILayout.BeginHorizontal();
			//{
			//	CurrentLayersType = (LayersType)EditorGUILayout.EnumPopup(CurrentLayersType);
			//}
			//GUILayout.EndHorizontal();

			//GUILayout.Space(VerticalMargin);

			GUILayout.Space(VerticalMargin);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			{

				PrimaryLayersOnGUI();
				GUILayout.Space(10);
				SecondaryLayersOnGUI();
			}
			EditorGUILayout.EndScrollView();
		}

		private void PrimaryLayersOnGUI()
		{
			if (primaryLayersRList != null)
			{
				HandleLayerReordableList(primaryLayersRList);
				primaryLayersRList.DoLayoutList();
			}

			for (int idx = 0; idx < Animator.Layers.Count; idx++)
			{
				Animator.Layers[idx].Index = idx;
				EditorUtility.SetDirty(Animator.Layers[idx]);
			}
		}

		private void SecondaryLayersOnGUI()
		{
			if (secondaryLayersRList != null)
			{
				HandleSecondaryLayer(secondaryLayersRList, Animator.SecondaryLayers);
				secondaryLayersRList.DoLayoutList();
			}
		}

		private void HandleLayerReordableList(ReorderableList list)
		{
			//list.elementHeight = 40f;

			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Primary layers");
			};

			list.onAddCallback = (ReorderableList reorderableList) =>
			{
				if (Animator.Layers.Count == 0)
				{
					Animator.AddLayer("New Layer", null);
				}
			};

			list.onReorderCallback = (ReorderableList reorderableList) =>
			{
				for (int i = 0; i < Animator.Layers.Count; i++)
				{
					Animator.Layers[i].Index = i;
				}
			};


			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				Event e = Event.current;

				if (e.type == EventType.MouseDown && e.button == 0)
				{
					Animator.SelectedLayer = Animator.Layers[index];
					Editor.Repaint();
				}

				float lineHeight = 20f;
				Rect nameRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
				float space = 5f;
				Animator.Layers[index].Fold = EditorGUI.Foldout(nameRect, Animator.Layers[index].Fold, Animator.Layers[index].Name, true);

				if (Animator.Layers[index].Fold)
				{
					Rect changeNameRect = new Rect(
						rect.x,
						nameRect.y + nameRect.height + space,
						rect.width,
						lineHeight
						);
					Rect avatarMaskRect = new Rect(
						rect.x,
						changeNameRect.y + changeNameRect.height + space,
						rect.width,
						lineHeight
						);
					Rect IKPassRect = new Rect(
						rect.x,
						avatarMaskRect.y + avatarMaskRect.height + space,
						rect.width,
						lineHeight
						);
					Rect FootIKPassRect = new Rect(
						rect.x,
						IKPassRect.y + IKPassRect.height + space,
						rect.width,
						lineHeight
						);
					Rect IsAdditiveSRect = new Rect(
						rect.x,
						FootIKPassRect.y + FootIKPassRect.height + space,
						rect.width,
						lineHeight
						);


					Animator.Layers[index].Name = EditorGUI.TextField(changeNameRect, Animator.Layers[index].Name);

					Animator.Layers[index].AvatarMask = (AvatarMask)EditorGUI.ObjectField(
						avatarMaskRect,
						Animator.Layers[index].AvatarMask,
						typeof(AvatarMask),
						true
						);
					Animator.Layers[index].PassIK = EditorGUI.Toggle(IKPassRect, new GUIContent("Pass IK"), Animator.Layers[index].PassIK);
					Animator.Layers[index].FootPassIK = EditorGUI.Toggle(FootIKPassRect, new GUIContent("Foot IK"), Animator.Layers[index].FootPassIK);
					Animator.Layers[index].IsAdditive = EditorGUI.Toggle(IsAdditiveSRect, new GUIContent("Additive layer"), Animator.Layers[index].IsAdditive);

				}
			};


			list.elementHeightCallback = (int index) =>
			{
				if (Animator.Layers[index].Fold)
				{
					return 6 * 25;
				}
				else
				{
					return 20f;
				}
			};

			list.onMouseUpCallback = (ReorderableList list) =>
			{
				Animator.SelectedLayer = Animator.Layers[list.index];
			};
		}

		private void HandleSecondaryLayer(ReorderableList list, List<SecondaryLayerData> secondaryLayers)
		{
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Secondary layers");
			};

			float lineHeight = 30f;

			list.elementHeightCallback = (int index) =>
			{
				if (secondaryLayers.Count == 0)
				{
					return 20;
				}
				else if (!secondaryLayers[index].m_IsFolded)
				{
					return 20;
				}
				else
				{
					return 4 * lineHeight;
				}

			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				float lineHeight = 20f;
				Rect nameRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
				float space = 5f;
				secondaryLayers[index].m_IsFolded = EditorGUI.Foldout(
					nameRect,
					secondaryLayers[index].m_IsFolded,
					secondaryLayers[index].name,
					true
					);

				if (secondaryLayers[index].m_IsFolded)
				{
					Rect changeNameRect = new Rect(
						rect.x,
						nameRect.y + nameRect.height + space,
						rect.width,
						lineHeight
						);
					Rect avatarMaskRect = new Rect(
						rect.x,
						changeNameRect.y + changeNameRect.height + space,
						rect.width,
						lineHeight
						);
					Rect IKPassRect = new Rect(
						rect.x,
						avatarMaskRect.y + avatarMaskRect.height + space,
						rect.width,
						lineHeight
						);
					Rect IsAdditiveSRect = new Rect(
						rect.x,
						IKPassRect.y + IKPassRect.height + space,
						rect.width,
						lineHeight
						);


					secondaryLayers[index].name = EditorGUI.TextField(changeNameRect, secondaryLayers[index].name);

					secondaryLayers[index].Mask = (AvatarMask)EditorGUI.ObjectField(
						avatarMaskRect,
						secondaryLayers[index].Mask,
						typeof(AvatarMask),
						true
						);
					secondaryLayers[index].PassIK = EditorGUI.Toggle(IKPassRect, new GUIContent("Pass IK"), secondaryLayers[index].PassIK);
					secondaryLayers[index].IsAdditive = EditorGUI.Toggle(IsAdditiveSRect, new GUIContent("Additive layer"), secondaryLayers[index].IsAdditive);

				}
			};

			list.onAddCallback = (ReorderableList list) =>
			{
				SecondaryLayerData layerData = new SecondaryLayerData();
				layerData.name = "<SecondaryLayer>";
				secondaryLayers.Add(layerData);
			};

		}
		#endregion

		#region Parameters menu

		ReorderableList boolParamtersRList;
		ReorderableList intParamtersRList;
		ReorderableList floatParamtersRList;
		ReorderableList triggersParamtersRList;

		private void InitParamtersList()
		{
			if (Animator == null) return;

			if (Animator.BoolParameters == null) Animator.BoolParameters = new List<BoolParameter>();
			boolParamtersRList = new ReorderableList(Animator.BoolParameters, typeof(BoolParameter), false, true, true, true);

			if (Animator.IntParameters == null) Animator.IntParameters = new List<IntParameter>();
			intParamtersRList = new ReorderableList(Animator.IntParameters, typeof(IntParameter), false, true, true, true);

			if (Animator.FloatParamaters == null) Animator.FloatParamaters = new List<FloatParameter>();
			floatParamtersRList = new ReorderableList(Animator.FloatParamaters, typeof(FloatParameter), false, true, true, true);

			if (Animator.TriggersNames == null) Animator.TriggersNames = new List<string>();
			triggersParamtersRList = new ReorderableList(Animator.TriggersNames, typeof(string), false, true, true, true);
		}

		private void DrawParametersMenu()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			{
				// Bool parameters:
				HandleBoolParamterList(boolParamtersRList);
				boolParamtersRList.DoLayoutList();


				GUILayout.Space(VerticalMargin);

				// Int pramaters:

				HandleIntParamterList(intParamtersRList);
				intParamtersRList.DoLayoutList();

				GUILayout.Space(VerticalMargin);

				// Float parameters:
				HandleFloatParamterList(floatParamtersRList);
				floatParamtersRList.DoLayoutList();

				GUILayout.Space(VerticalMargin);

				// Trigger prameters:
				HandleTriggersParamterList(triggersParamtersRList);
				triggersParamtersRList.DoLayoutList();
			}
			EditorGUILayout.EndScrollView();
		}

		private void HandleBoolParamterList(ReorderableList list)
		{
			// type 0 - bools; 1 - ints; 2 - floats;
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Bools");
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				Animator.AddBool("New bool");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string currentName;
				string newName;
				int counter = 0;

				float neededWidth = rect.width - 10f;

				Rect drawRect = new Rect(
					rect.x + 5,
					rect.y + 0.1f * rect.height,
					neededWidth * 0.75f - 5f,
					0.8f * rect.height
					);

				Rect valueRect = new Rect(
					drawRect.x + drawRect.width + 5f,
					drawRect.y,
					neededWidth - drawRect.width,
					drawRect.height
					);

				valueRect.x = rect.x + rect.width - 5f - 20f;
				valueRect.width = 20f;

				drawRect.width = valueRect.x - drawRect.x - 5f;

				BoolParameter parameter = Animator.BoolParameters[index];
				if (!isActive)
				{
					EditorGUI.LabelField(drawRect, parameter.Name);
				}
				else
				{
					parameter.Name = EditorGUI.DelayedTextField(drawRect, parameter.Name);

					currentName = parameter.Name;
					newName = currentName;
					counter = 0;
					for (int i = 0; i < Animator.BoolParameters.Count; i++)
					{
						if (Animator.BoolParameters[i].Name == newName && i != index)
						{
							counter++;
							newName = currentName + counter.ToString();
							i = 0;
						}
					}

					parameter.Name = newName;
				}


				parameter.Value = EditorGUI.Toggle(valueRect, parameter.Value);

				Animator.BoolParameters[index] = parameter;

			};

			list.onReorderCallback = (ReorderableList rlist) =>
			{
			};

			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				int removedIndex = list.index;

				Animator.RemoveBool(Animator.BoolParameters[removedIndex].Name);
			};
		}


		private void HandleIntParamterList(ReorderableList list)
		{
			// type 0 - bools; 1 - ints; 2 - floats;
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Ints");
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				Animator.AddInt("New int");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string currentName;
				string newName;
				int counter = 0;

				float neededWidth = rect.width - 10f;

				Rect drawRect = new Rect(
					rect.x + 5,
					rect.y + 0.1f * rect.height,
					neededWidth * 0.75f - 5f,
					0.8f * rect.height
					);

				Rect valueRect = new Rect(
					drawRect.x + drawRect.width + 5f,
					drawRect.y,
					neededWidth - drawRect.width,
					drawRect.height
					);

				{
					IntParameter parameter = Animator.IntParameters[index];

					if (!isActive)
					{
						EditorGUI.LabelField(drawRect, Animator.IntParameters[index].Name);
					}
					else
					{

						parameter.Name = EditorGUI.DelayedTextField(drawRect, parameter.Name);

						currentName = parameter.Name;
						newName = currentName;
						counter = 0;
						for (int i = 0; i < Animator.IntParameters.Count; i++)
						{
							if (Animator.IntParameters[i].Name == newName && i != index)
							{
								counter++;
								newName = currentName + counter.ToString();
								i = 0;
							}
						}

						parameter.Name = newName;
					}

					parameter.Value = EditorGUI.IntField(valueRect, parameter.Value);
					Animator.IntParameters[index] = parameter;
				}

			};

			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				int removedIndex = list.index;
				Animator.IntParameters.RemoveAt(removedIndex);

			};
		}

		private void HandleFloatParamterList(ReorderableList list)
		{
			// type 0 - bools; 1 - ints; 2 - floats;
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Floats");
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				Animator.AddFloat("New float");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string currentName;
				string newName;
				int counter = 0;

				float neededWidth = rect.width - 10f;

				Rect drawRect = new Rect(
					rect.x + 5,
					rect.y + 0.1f * rect.height,
					neededWidth * 0.75f - 5f,
					0.8f * rect.height
					);

				Rect valueRect = new Rect(
					drawRect.x + drawRect.width + 5f,
					drawRect.y,
					neededWidth - drawRect.width,
					drawRect.height
					);


				FloatParameter parameter = Animator.FloatParamaters[index];

				if (!isActive)
				{
					EditorGUI.LabelField(drawRect, Animator.FloatParamaters[index].Name);
				}
				else
				{

					parameter.Name = EditorGUI.DelayedTextField(drawRect, parameter.Name);

					currentName = parameter.Name;
					newName = currentName;
					counter = 0;
					for (int i = 0; i < Animator.FloatParamaters.Count; i++)
					{
						if (Animator.FloatParamaters[i].Name == newName && i != index)
						{
							counter++;
							newName = currentName + counter.ToString();
							i = 0;
						}
					}

					parameter.Name = newName;
				}


				parameter.Value = EditorGUI.FloatField(valueRect, parameter.Value);
				Animator.FloatParamaters[index] = parameter;

			};

			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				int removedIndex = list.index;

				Animator.RemoveFloat(Animator.FloatParamaters[removedIndex].Name);
			};
		}

		private void HandleTriggersParamterList(ReorderableList list)
		{
			// type 0 - bools; 1 - ints; 2 - floats;
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Triggers");
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				Animator.AddTrigger("New Trigger");
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				string currentName;
				string newName;
				int counter = 0;

				float neededWidth = rect.width - 10f;

				Rect drawRect = new Rect(
					rect.x + 5,
					rect.y + 0.1f * rect.height,
					neededWidth * 0.75f - 5f,
					0.8f * rect.height
					);

				Rect valueRect = new Rect(
					drawRect.x + drawRect.width + 5f,
					drawRect.y,
					neededWidth - drawRect.width,
					drawRect.height
					);


				if (!isActive)
				{
					EditorGUI.LabelField(drawRect, Animator.TriggersNames[index]);
				}
				else
				{
					Animator.TriggersNames[index] = EditorGUI.DelayedTextField(drawRect, Animator.TriggersNames[index]);

					currentName = Animator.TriggersNames[index];
					newName = currentName;
					counter = 0;
					for (int i = 0; i < Animator.TriggersNames.Count; i++)
					{
						if (Animator.TriggersNames[i] == newName && i != index)
						{
							counter++;
							newName = currentName + counter.ToString();
							i = 0;
						}
					}
					Animator.TriggersNames[index] = newName;
				}
			};

			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				int removedIndex = list.index;

				Animator.RemoveTrigger(Animator.TriggersNames[removedIndex]);
			};
		}
		#endregion
	}
}