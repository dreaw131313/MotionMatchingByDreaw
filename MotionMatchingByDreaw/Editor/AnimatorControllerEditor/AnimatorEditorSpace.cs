using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MotionMatching.Tools
{
	public abstract class AnimatorEditorSpace
	{
		public Rect Position;
		public float WidthFactor;


		public AnimatorControllerEditor Editor { get; set; }

		public MotionMatchingAnimator_SO Animator
		{
			get
			{
				return Editor.Animator;
			}
		}

		public virtual void PerfomrOnGUI(Event e)
		{
			GUILayout.BeginArea(Position);
			{
				OnGUI(e);
			}
			GUILayout.EndArea();
		}

		protected abstract void OnGUI(Event e);

		public abstract void OnEnable();

		public abstract void OnChangeAnimatorAsset();

	}
}