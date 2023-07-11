using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MotionMatching.Tools.MotionMatchingDataEditor;

namespace MotionMatching.Tools
{
	public abstract class MMDataEditorMenuBaseClass
	{
		protected MotionMatchingDataEditor editor;
		protected Rect mainRect;
		public void SetBasics(
			MotionMatchingDataEditor editor,
			Rect mainRect
			)
		{
			this.editor = editor;
			this.mainRect = mainRect;
		}

		public abstract void OnGUI(
			Event e,
			Rect rect
			);

		public virtual void OnEnable()
		{
		}

		public virtual void OnDisable()
		{

		}

		public virtual void OnDestroy()
		{

		}

		public virtual void OnChangeData()
		{

		}

		public virtual void OnChangeGameObject()
		{

		}
	}
}