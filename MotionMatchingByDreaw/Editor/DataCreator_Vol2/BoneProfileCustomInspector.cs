using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(BonesProfile))]
	public class BoneProfileCustomInspector : Editor
	{
		Transform t;

		public override void OnInspectorGUI()
		{
			BonesProfile profile = target as BonesProfile;
			if (profile == null) return;

			const float horizontalMargin = 15;
			const float verticalMargin = 5;

			GUILayout.BeginVertical();
			{
				profile.Mask = EditorGUILayout.ObjectField("Bone mask", profile.Mask, typeof(AvatarMask), false) as AvatarMask;
				GUILayout.Space(verticalMargin * 2);

				GUILayout.Label("Mask bones:");
				if (profile.BoneSettings != null && profile.BoneSettings.Count > 0)
				{
					for (int i = 0; i < profile.BoneSettings.Count; i++)
					{
						BoneProfileSettings settings = profile.BoneSettings[i];

						GUILayout.Space(verticalMargin);
						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(horizontalMargin);
							GUILayout.Label($"{i + 1}.");
							GUILayout.Space(5);
							GUILayout.Label($"{settings.BoneName}");
							GUILayout.FlexibleSpace();
							settings.VelocityCalculationType = (BoneVelocityCalculationType)EditorGUILayout.EnumPopup(
								settings.VelocityCalculationType
								);
						}
						GUILayout.EndHorizontal();

						profile.BoneSettings[i] = settings;
					}
				}
				else
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(horizontalMargin);
						GUILayout.Label("Bones list is empty, try validate.");
					}
					GUILayout.EndHorizontal();
				}

				GUILayout.Space(verticalMargin * 2);

				if (GUILayout.Button("Validate"))
				{
					profile.Validate();
				}
			}
			GUILayout.EndVertical();
		}


	}
}