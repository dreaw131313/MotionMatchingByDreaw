using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using MotionMatching.Gameplay;

namespace MotionMatching.Tools
{
	public class AnimatorGraphEditor : EditorWindow
	{
		[SerializeField]
		private MM_AnimatorController animator;

		Rect leftSpace;
		Rect middleSpace;
		Rect rightSpace;

		Rect leftAreaLayout;
		Rect rightAreaLayout;

		bool resizing1 = false;
		bool resizing2 = false;

		Vector2 leftScroll = Vector2.zero;
		Vector2 rightScroll = Vector2.zero;

		float leftWidthFactor = 0.2f;
		float rightWidthFactor = 0.4f;

		float margin = 10;

		LayersAndParametersSpace animatorOptions;
		GraphSpaceNEW graphSpace;
		ElementOptionView elementOptions;

		[OnOpenAsset()]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			MM_AnimatorController contr;
			try
			{
				contr = (MM_AnimatorController)EditorUtility.InstanceIDToObject(instanceID);
			}
			catch (System.Exception)
			{
				return false;
			}

			if (EditorWindow.HasOpenInstances<AnimatorGraphEditor>())
			{
				EditorWindow.GetWindow<AnimatorGraphEditor>().SetAsset(contr);
				EditorWindow.GetWindow<AnimatorGraphEditor>().Repaint();
				return true;
			}

			AnimatorGraphEditor.ShowWindow();
			EditorWindow.GetWindow<AnimatorGraphEditor>().SetAsset(contr);
			EditorWindow.GetWindow<AnimatorGraphEditor>().Repaint();

			return true;
		}

		//[MenuItem("MotionMatching/Motion Matching Graph Editor", priority = 3)]
		public static void ShowWindow()
		{
			AnimatorGraphEditor editor = EditorWindow.GetWindow<AnimatorGraphEditor>();
			editor.position = new Rect(new Vector2(100, 100), new Vector2(800, 600));
			editor.titleContent = new GUIContent("Motion Matching Graph");
		}

		private void SetAsset(MM_AnimatorController asset)
		{
			this.animator = asset;
		}

		private void OnEnable()
		{
			InitRects();
			leftAreaLayout = new Rect();
			rightAreaLayout = new Rect();

			animatorOptions = new LayersAndParametersSpace(this.animator);
			graphSpace = new GraphSpaceNEW();
			elementOptions = new ElementOptionView();
			Undo.undoRedoPerformed += UndoRedoCallback;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoCallback;
		}

		private void UndoRedoCallback()
		{
			this.Repaint();
		}

		private void OnGUI()
		{
			GUI.DrawTexture(leftSpace, GUIResources.GetMediumTexture_1());
			GUI.DrawTexture(middleSpace, GUIResources.GetGraphSpaceTexture());
			GUI.DrawTexture(rightSpace, GUIResources.GetMediumTexture_1());


			FitRects();
			Resizing(Event.current);

			leftWidthFactor = leftSpace.width / this.position.width;
			rightWidthFactor = rightSpace.width / this.position.width;

			Event e = Event.current;

			graphSpace.Draw(middleSpace);
			DrawLeftSpace(e);
			DrawRightSpace(e);

			graphSpace.UserInput(e, middleSpace, this);

			if (animator != null)
			{
				EditorUtility.SetDirty(animator);
				//Object[] undoObjects = { this, animator };
				//Undo.RecordObjects(undoObjects, "graph editor change!");
				//Undo.RecordObject(this, "animator change");
			}

			Repaint();
		}

		private void Update()
		{
			animatorOptions.SetAnimator(this.animator);
			graphSpace.SetAnimatorAndLayerIndex(animatorOptions.animator, animatorOptions.selectedLayerIndex);

			//if (animator == null)
			//{
			//    elementOptions.SetNeededReferences(
			//        this.animator,
			//        null,
			//        null,
			//        null
			//        );
			//}
			//else
			//{
			elementOptions.SetNeededReferences(
				this.animator,
				this.animator == null ? null : animatorOptions.selectedLayerIndex >= 0 && animatorOptions.selectedLayerIndex < animator.layers.Count ? animator.layers[animatorOptions.selectedLayerIndex] : null,
				graphSpace.selectedNode,
				graphSpace.selectedTransition
				);
			// }

			if (animator != null)
			{
				Undo.RecordObject(this, "Some Random text");
				EditorUtility.SetDirty(this);
			}
		}

		private void InitRects()
		{
			leftSpace = new Rect(
				0,
				0,
				this.position.width * 0.2f,
				this.position.height
				);
			middleSpace = new Rect(
				leftSpace.x + leftSpace.width,
				0,
				this.position.width * 0.6f,
				this.position.height
				);
			rightSpace = new Rect(
				middleSpace.x + middleSpace.width,
				0,
				this.position.width * 0.2f,
				this.position.height
				);
		}

		private void Resizing(Event e)
		{
			float leftSpaceResizing_1 = 10f;
			float rightSpaceResizing_1 = leftSpace.width < 10f ? 10f : 0f;

			float leftSpaceResizing_2 = 0f;
			float rightSpaceResizing_2 = 10f;
			GUILayoutElements.ResizingRectsHorizontal(
				this,
				ref leftSpace,
				ref middleSpace,
				e,
				ref resizing1,
				leftSpaceResizing_1,
				rightSpaceResizing_1
				);
			GUILayoutElements.ResizingRectsHorizontal(
				this,
				ref middleSpace,
				ref rightSpace,
				e,
				ref resizing2,
				leftSpaceResizing_2,
				rightSpaceResizing_2
				);
		}

		private void FitRects()
		{
			leftSpace.width = leftWidthFactor * this.position.width;
			middleSpace.width = (1f - leftWidthFactor - rightWidthFactor) * this.position.width;
			rightSpace.width = rightWidthFactor * this.position.width;

			leftSpace.height = this.position.height;
			middleSpace.height = this.position.height;
			rightSpace.height = this.position.height;

			middleSpace.x = leftSpace.x + leftSpace.width;
			rightSpace.x = middleSpace.x + middleSpace.width;


		}

		private void DrawLeftSpace(Event e)
		{
			leftAreaLayout.Set(
				leftSpace.x + margin,
				leftSpace.y + margin,
				leftSpace.width - 2 * margin,
				leftSpace.height - margin
				);
			GUILayout.BeginArea(leftAreaLayout);
			//leftScroll = GUILayout.BeginScrollView(leftScroll);
			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent("Controller"), GUILayout.MaxWidth(100));
			this.animator = (MM_AnimatorController)EditorGUILayout.ObjectField(
				this.animator,
				typeof(MM_AnimatorController),
				true
				);
			GUILayout.EndHorizontal();

			if (this.animator != null)
			{
				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Update motion group in all states"))
					{
						this.animator.UpdateMotionGroupInAllState();
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("Create so animator"))
					{
						MotionMatchingAnimator_SO.CreateFromOldVersion(animator);
					}
				}
				GUILayout.EndHorizontal();

			}



			animatorOptions.Draw(leftAreaLayout, ref leftWidthFactor, this);

			GUILayout.Space(margin);

			//GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawMiddleSpace(Event e)
		{
			graphSpace.Draw(middleSpace);

		}

		private void DrawRightSpace(Event e)
		{
			rightAreaLayout.Set(
				   rightSpace.x + margin,
				   rightSpace.y + margin,
				   rightSpace.width - margin,
				   rightSpace.height - margin
				   );
			GUILayout.BeginArea(rightAreaLayout);
			rightScroll = GUILayout.BeginScrollView(rightScroll);
			elementOptions.Draw();
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

	}
}
