using UnityEngine;

namespace MotionMatching.Tools
{
	public static class MMGUIUtility
	{
		public static Rect MakeMargins(
			Rect forRect,
			float leftMargin,
			float rightMargin,
			float topMargin,
			float bottomMargin
			)
		{
			Vector2 position = forRect.position;
			position.x += leftMargin;
			position.y += topMargin;

			forRect.position = position;
			forRect.width -= leftMargin + rightMargin;
			forRect.height -= topMargin + bottomMargin;

			return forRect;
		}


		public static Rect MakeMargins(
			Rect forRect,
			float horizontalMargin,
			float verticalMargin
			)
		{
			Vector2 position = forRect.position;
			position.x += horizontalMargin;
			position.y += verticalMargin;

			forRect.position = position;
			forRect.width -= 2f * horizontalMargin;
			forRect.height -= 2f * verticalMargin;

			return forRect;
		}

		public static Rect AddThicknessToRect(Rect forRect, float thicknes)
		{
			float halfThickens = thicknes / 2f;
			forRect.position -= new Vector2(halfThickens, halfThickens);
			forRect.width += thicknes;
			forRect.height += thicknes;

			return forRect;
		}

		public static Rect AddOutlineToRect(Rect forRect, float thicknes)
		{
			float halfThickens = thicknes;
			forRect.position -= new Vector2(halfThickens, halfThickens);
			forRect.width += thicknes * 2f;
			forRect.height += thicknes * 2f;

			return forRect;
		}

		public static Rect ShrinkRect(Rect forRect, float shrinkAmount)
		{
			float doubleShrink = 2f * shrinkAmount;
			forRect.position += new Vector2(shrinkAmount, shrinkAmount);
			forRect.width -= doubleShrink;
			forRect.height -= doubleShrink;

			return forRect;
		}

	}


	public static class GUIRotateArea
	{
		static float lastRotationAngle;
		static Vector2 lastAroundPoint;
		static float lastZoom;

		static bool isStarted;

		public static void BeginArea(float rotationAngle, Vector2 aroundPoint, float zoom = 1f)
		{
			if (isStarted)
			{
				throw new System.Exception("Cannot begin next \"GUIRotateArea\" before ending previous!");
			}

			lastRotationAngle = rotationAngle;
			lastAroundPoint = aroundPoint;
			lastZoom = zoom;
			isStarted = true;

			GUIUtility.RotateAroundPivot(lastRotationAngle, lastAroundPoint * lastZoom);

		}


		public static void EndArea()
		{
			isStarted = false;
			GUIUtility.RotateAroundPivot(-lastRotationAngle, lastAroundPoint * lastZoom);
		}
	}



}