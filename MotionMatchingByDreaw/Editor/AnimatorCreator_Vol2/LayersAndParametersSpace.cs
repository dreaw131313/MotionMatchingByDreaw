using MotionMatching.Gameplay;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MotionMatching.Tools
{
	public class LayersAndParametersSpace
	{
		public MM_AnimatorController animator;
		Vector2 scroll = Vector2.zero;
		ToolbarOption toolbarOption = 0;


		string[] toolBarStrings =
		{
			"Layers",
			"Parameters"
		};

		private enum ToolbarOption
		{
			Layers = 0,
			Parameters = 1
		}

		string[] layersTypeStrings = { "Primary", "Seconadry" };

		private enum LayersType
		{
			Primary = 0,
			Secondary = 1
		}

		LayersType layersType = LayersType.Primary;

		ReorderableList layerList = null;
		ReorderableList floats = null;
		ReorderableList ints = null;
		ReorderableList bools = null;
		ReorderableList triggers = null;

		ReorderableList secondaryLayers = null;

		public int selectedLayerIndex = -1;

		private bool foldSpace;
		private float foldingSpeed = 0.2f;

		int currentAnimatorID = int.MaxValue;

		public LayersAndParametersSpace(MM_AnimatorController animator)
		{
			this.animator = animator;
		}

		public void SetAnimator(MM_AnimatorController animator)
		{
			this.animator = animator;
			if (this.animator != null)
			{
				if (currentAnimatorID != this.animator.GetInstanceID())
				{
					currentAnimatorID = this.animator.GetInstanceID();

					layerList = new ReorderableList(animator.layers, typeof(MotionMatchingLayer), false, true, true, false);
					floats = new ReorderableList(animator.FloatParamaters, typeof(string), false, true, true, true);
					ints = new ReorderableList(animator.IntParamters, typeof(string), false, true, true, true);
					bools = new ReorderableList(animator.BoolParameters, typeof(string), false, true, true, true);
					triggers = new ReorderableList(animator.TriggersNames, typeof(string), false, true, true, true);
				}
			}
		}

		public void Draw(Rect rect, ref float resizeFactor, EditorWindow window)
		{
			//GUI.DrawTexture(rect, GUIResources.GetMediumTexture_1());
			//GUILayout.BeginArea(rect);

			if (animator != null)
			{
				GUILayout.Space(5);
				GUILayout.BeginHorizontal();

				if (GUILayout.Button("<", GUILayout.Width(20)))
				{
					foldSpace = true;
					foldingSpeed = resizeFactor / 1.5f;
				}

				GUILayout.Space(5);

				int option = (int)toolbarOption;
				option = GUILayout.Toolbar(option, toolBarStrings, GUILayout.MaxWidth(rect.width));
				toolbarOption = (ToolbarOption)option;

				if (foldSpace)
				{
					resizeFactor -= (foldingSpeed * Time.deltaTime);
					resizeFactor = Mathf.Clamp(resizeFactor, 0f, float.MaxValue);
					window.Repaint();
					if (resizeFactor == 0)
					{
						foldSpace = false;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				scroll = EditorGUILayout.BeginScrollView(scroll);
				switch (toolbarOption)
				{
					case ToolbarOption.Layers:
						DrawLayers(rect, ref window);
						break;
					case ToolbarOption.Parameters:
						DrawValues();
						break;
				}

				EditorGUILayout.EndScrollView();
			}

			//GUILayout.EndArea();
		}

		private void DrawLayers(Rect rect, ref EditorWindow window)
		{
			int option = (int)layersType;
			option = GUILayout.Toolbar(option, layersTypeStrings);
			layersType = (LayersType)option;

			GUILayout.Space(5);

			switch (layersType)
			{
				case LayersType.Primary:
					{
						DrawPrimaryLayers(rect, ref window);
					}
					break;
				case LayersType.Secondary:
					{
						DrawSecondaryLayers();
					}
					break;
			}
		}

		private void DrawPrimaryLayers(Rect rect, ref EditorWindow window)
		{
			if (layerList == null)
			{
				layerList = new ReorderableList(animator.layers, typeof(MotionMatchingLayer));
			}
			HandleLayerReordableList(layerList, "Layers");
			layerList.DoLayoutList();
			GetingSelectedLayer();
		}

		private void DrawSecondaryLayers()
		{
			if (animator.SecondaryLayers == null)
			{
				animator.SecondaryLayers = new List<SecondaryLayerData>();
				secondaryLayers = new ReorderableList(animator.SecondaryLayers, typeof(SecondaryLayerData));
			}
			else if (secondaryLayers != null && animator.SecondaryLayers != secondaryLayers.list)
			{
				secondaryLayers = new ReorderableList(animator.SecondaryLayers, typeof(SecondaryLayerData));
			}
			if (secondaryLayers == null)
			{
				secondaryLayers = new ReorderableList(animator.SecondaryLayers, typeof(SecondaryLayerData));
			}
			HandleSecondaryLayer(secondaryLayers, animator.SecondaryLayers);
			secondaryLayers.DoLayoutList();
		}

		private void GetingSelectedLayer()
		{
			if (animator.layers.Count > 0 && !(layerList.index >= 0 && layerList.index < layerList.count && layerList.count > 0))
			{
				layerList.index = 0;
			}
			if (layerList.index >= 0 && layerList.index < layerList.count && layerList.count > 0)
			{
				selectedLayerIndex = layerList.index;
			}
			else
			{
				selectedLayerIndex = -1;
			}
		}

		private void HandleLayerReordableList(ReorderableList list, string header)
		{
			//list.elementHeight = 40f;

			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, header);
			};

			list.onAddCallback = (ReorderableList reorderableList) =>
			{
				if (animator.layers.Count == 0)
				{
					animator.AddLayer("New Layer", null);
				}
			};

			list.onReorderCallback = (ReorderableList reorderableList) =>
			{
				for (int i = 0; i < animator.layers.Count; i++)
				{
					animator.layers[i].index = i;
				}
			};


			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				float lineHeight = 20f;
				Rect nameRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
				float space = 5f;
				animator.layers[index].fold = EditorGUI.Foldout(nameRect, animator.layers[index].fold, animator.layers[index].name, true);

				if (animator.layers[index].fold)
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


					animator.layers[index].name = EditorGUI.TextField(changeNameRect, animator.layers[index].name);

					animator.layers[index].avatarMask = (AvatarMask)EditorGUI.ObjectField(
						avatarMaskRect,
						animator.layers[index].avatarMask,
						typeof(AvatarMask),
						true
						);
					animator.layers[index].passIK = EditorGUI.Toggle(IKPassRect, new GUIContent("Pass IK"), animator.layers[index].passIK);
					animator.layers[index].footPassIK = EditorGUI.Toggle(FootIKPassRect, new GUIContent("Foot IK"), animator.layers[index].footPassIK);
					animator.layers[index].isAdditive = EditorGUI.Toggle(IsAdditiveSRect, new GUIContent("Additive layer"), animator.layers[index].isAdditive);

				}
			};


			list.elementHeightCallback = (int index) =>
			{
				if (animator.layers[index].fold)
				{
					return 6 * 25;
				}
				else
				{
					return 20f;
				}
			};
		}

		private void CreateGenericMenu()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Add Layer"), false, GenericMenuCallback, LeftRectGMOptions.AddLayer);
			if (layerList.index >= 0 && layerList.index < layerList.count)
			{
				menu.AddItem(new GUIContent("Edit Layer"), false, GenericMenuCallback, LeftRectGMOptions.EditLayer);
				menu.AddItem(new GUIContent("Remove Layer"), false, GenericMenuCallback, LeftRectGMOptions.RemoveLayer);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Edit Layer"));
				menu.AddDisabledItem(new GUIContent("Remove Layer"));
			}
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Collapse Inactive Layers"), false, GenericMenuCallback, LeftRectGMOptions.CollapseInactiveLayers);


			menu.ShowAsContext();
		}

		private void GenericMenuCallback(object action)
		{
			switch (action)
			{
				case LeftRectGMOptions.AddLayer:
					animator.AddLayer("New Layer", null);
					break;
				case LeftRectGMOptions.RemoveLayer:
					try
					{
						animator.RemoveLayerAt(layerList.index);
					}
					catch (Exception)
					{

					}
					break;
				case LeftRectGMOptions.EditLayer:
					if (selectedLayerIndex < animator.layers.Count && selectedLayerIndex >= 0)
					{
						animator.layers[selectedLayerIndex].fold = true;
					}
					break;
				case LeftRectGMOptions.CollapseInactiveLayers:
					for (int i = 0; i < layerList.count; i++)
					{
						if (i != layerList.index)
						{
							animator.layers[i].fold = false;
						}
					}
					break;
			}
		}

		private void DrawValues()
		{
			GUILayout.BeginVertical();

			HandleStringList(bools, "Bools", 0);
			bools.DoLayoutList();
			//if (GUILayout.Button("Clear bools"))
			//{
			//	animator.boolNames.Clear();
			//}
			GUILayout.Space(5);


			HandleStringList(triggers, "Triggers", 3);
			triggers.DoLayoutList();


			GUILayout.Space(5);

			HandleStringList(ints, "Ints", 1);
			ints.DoLayoutList();

			//if (GUILayout.Button("Clear ints"))
			//{
			//	animator.intNames.Clear();
			//}

			GUILayout.Space(5);

			HandleStringList(floats, "Floats", 2);
			floats.DoLayoutList();

			//if (GUILayout.Button("Clear floats"))
			//{
			//	animator.floatNames.Clear();
			//}

			GUILayout.EndVertical();
		}

		private void HandleStringList(ReorderableList list, string name, int type)
		{
			// type 0 - bools; 1 - ints; 2 - floats;
			list.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, name);
			};

			list.onAddCallback = (ReorderableList rlist) =>
			{
				switch (type)
				{
					case 0:
						animator.AddBool("New bool");
						break;
					case 1:
						animator.AddInt("New int");
						break;
					case 2:
						animator.AddFloat("New float");
						break;
					case 3:
						animator.AddTrigger("New Trigger");
						break;
				}
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

				switch (type)
				{
					case 0:
						{
							valueRect.x = rect.x + rect.width - 5f - 20f;
							valueRect.width = 20f;

							drawRect.width = valueRect.x - drawRect.x - 5f;

							BoolParameter parameter = animator.BoolParameters[index];
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
								for (int i = 0; i < animator.BoolParameters.Count; i++)
								{
									if (animator.BoolParameters[i].Name == newName && i != index)
									{
										counter++;
										newName = currentName + counter.ToString();
										i = 0;
									}
								}

								parameter.Name = newName;
							}


							parameter.Value = EditorGUI.Toggle(valueRect, parameter.Value);

							animator.BoolParameters[index] = parameter;
						}
						break;
					case 1:
						{
							IntParameter parameter = animator.IntParamters[index];

							if (!isActive)
							{
								EditorGUI.LabelField(drawRect, animator.IntParamters[index].Name);
							}
							else
							{

								parameter.Name = EditorGUI.DelayedTextField(drawRect, parameter.Name);

								currentName = parameter.Name;
								newName = currentName;
								counter = 0;
								for (int i = 0; i < animator.IntParamters.Count; i++)
								{
									if (animator.IntParamters[i].Name == newName && i != index)
									{
										counter++;
										newName = currentName + counter.ToString();
										i = 0;
									}
								}

								parameter.Name = newName;
							}

							parameter.Value = EditorGUI.IntField(valueRect, parameter.Value);
							animator.IntParamters[index] = parameter;
						}
						break;
					case 2:
						{
							FloatParameter parameter = animator.FloatParamaters[index];

							if (!isActive)
							{
								EditorGUI.LabelField(drawRect, animator.FloatParamaters[index].Name);
							}
							else
							{

								parameter.Name = EditorGUI.DelayedTextField(drawRect, parameter.Name);

								currentName = parameter.Name;
								newName = currentName;
								counter = 0;
								for (int i = 0; i < animator.FloatParamaters.Count; i++)
								{
									if (animator.FloatParamaters[i].Name == newName && i != index)
									{
										counter++;
										newName = currentName + counter.ToString();
										i = 0;
									}
								}

								parameter.Name = newName;
							}


							parameter.Value = EditorGUI.FloatField(valueRect, parameter.Value);
							animator.FloatParamaters[index] = parameter;
						}
						break;
					case 3:
						{
							if (!isActive)
							{
								EditorGUI.LabelField(drawRect, animator.TriggersNames[index]);
							}
							else
							{
								animator.TriggersNames[index] = EditorGUI.DelayedTextField(drawRect, animator.TriggersNames[index]);

								currentName = animator.TriggersNames[index];
								newName = currentName;
								counter = 0;
								for (int i = 0; i < animator.TriggersNames.Count; i++)
								{
									if (animator.TriggersNames[i] == newName && i != index)
									{
										counter++;
										newName = currentName + counter.ToString();
										i = 0;
									}
								}
								animator.TriggersNames[index] = newName;
							}
						}
						break;
				}
			};

			list.onReorderCallback = (ReorderableList rlist) =>
			{

			};

			list.onRemoveCallback = (ReorderableList rlist) =>
			{
				int removedIndex = list.index;

				switch (type)
				{
					case 0:
						{
							foreach (MotionMatchingLayer layer in animator.layers)
							{
								foreach (MotionMatchingState state in layer.states)
								{
									foreach (MotionMatching.Gameplay.Transition transition in state.Transitions)
									{
										foreach (TransitionOptions option in transition.options)
										{
											for (int i = 0; i < option.boolConditions.Count; i++)
											{
												ConditionBool condition = option.boolConditions[i];

												if (removedIndex == condition.CheckingValueIndex)
												{
													condition.CheckingValueIndex = -1;
												}
												else if (removedIndex < condition.CheckingValueIndex)
												{
													condition.CheckingValueIndex -= 1;
												}

												option.boolConditions[i] = condition;
											}
										}
									}
								}
							}
							animator.BoolParameters.RemoveAt(removedIndex);
						}
						break;
					case 1:
						{
							foreach (MotionMatchingLayer layer in animator.layers)
							{
								foreach (MotionMatchingState state in layer.states)
								{
									foreach (MotionMatching.Gameplay.Transition transition in state.Transitions)
									{
										foreach (TransitionOptions option in transition.options)
										{
											for (int i = 0; i < option.intConditions.Count; i++)
											{
												ConditionInt condition = option.intConditions[i];

												if (condition.CheckingValueIndex == removedIndex)
												{
													condition.CheckingValueIndex = -1;
												}
												else if (removedIndex < condition.CheckingValueIndex)
												{
													condition.CheckingValueIndex -= 1;
												}

												if (condition.ConditionValueIndex == removedIndex)
												{
													condition.ConditionValueIndex = -1;
												}
												else if (removedIndex < condition.ConditionValueIndex)
												{
													condition.ConditionValueIndex -= 1;
												}

												option.intConditions[i] = condition;
											}
										}
									}
								}
							}
							animator.IntParamters.RemoveAt(removedIndex);
						}
						break;
					case 2:
						{
							foreach (MotionMatchingLayer layer in animator.layers)
							{
								foreach (MotionMatchingState state in layer.states)
								{
									foreach (MotionMatching.Gameplay.Transition transition in state.Transitions)
									{
										foreach (TransitionOptions option in transition.options)
										{
											for (int i = 0; i < option.floatConditions.Count; i++)
											{
												ConditionFloat condition = option.floatConditions[i];

												if (condition.CheckingValueIndex == removedIndex)
												{
													condition.CheckingValueIndex = -1;
												}
												else if (removedIndex < condition.CheckingValueIndex)
												{
													condition.CheckingValueIndex -= 1;
												}

												if (condition.ConditionValueIndex == removedIndex)
												{
													condition.ConditionValueIndex = -1;
												}
												else if (removedIndex < condition.ConditionValueIndex)
												{
													condition.ConditionValueIndex -= 1;
												}

												option.floatConditions[i] = condition;
											}
										}
									}
								}
							}
							animator.FloatParamaters.RemoveAt(removedIndex);
						}
						break;
					case 3:
						{
							foreach (MotionMatchingLayer layer in animator.layers)
							{
								foreach (MotionMatchingState state in layer.states)
								{
									foreach (MotionMatching.Gameplay.Transition transition in state.Transitions)
									{
										foreach (TransitionOptions option in transition.options)
										{
											for (int i = 0; i < option.TriggerConditions.Count; i++)
											{
												ConditionTrigger condition = option.TriggerConditions[i];

												if (condition.CheckingValueIndex == removedIndex)
												{
													condition.CheckingValueIndex = -1;
												}
												else if (removedIndex < condition.CheckingValueIndex)
												{
													condition.CheckingValueIndex -= 1;
												}

												option.TriggerConditions[i] = condition;
											}
										}
									}
								}
							}
							animator.TriggersNames.RemoveAt(removedIndex);
						}
						break;
				}
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
	}

	public enum LayerView
	{
		LayersList,
		AddLayerWindow
	}

	public enum LeftRectGMOptions
	{
		AddLayer,
		RemoveLayer,
		EditLayer,
		CollapseInactiveLayers
	}
}