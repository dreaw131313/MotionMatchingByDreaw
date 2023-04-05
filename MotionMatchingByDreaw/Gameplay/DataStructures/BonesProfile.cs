using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[CreateAssetMenu(fileName = "MotionMatchingBonesProfile", menuName = "Motion Matching/Creators/Bones Profile")]
	public class BonesProfile : ScriptableObject
	{
		[SerializeField]
		public AvatarMask Mask;
		[SerializeField]
		public List<BoneProfileSettings> BoneSettings;


		public bool IsValid => Mask != null;
#if UNITY_EDITOR
		public void Validate()
		{
			if (Mask == null)
			{
				return;
			}

			if (BoneSettings == null) BoneSettings = new List<BoneProfileSettings>();

			List<BoneProfileSettings> newBoneSettings = new List<BoneProfileSettings>();

			for (int i = 0; i < Mask.transformCount; i++)
			{
				if (Mask.GetTransformActive(i))
				{
					string path = Mask.GetTransformPath(i);
					if (path.Length == 0)
					{
						continue;
					}

					int lastIndexofSlash = path.LastIndexOf('/');
					string name = path.Substring(lastIndexofSlash + 1);

					newBoneSettings.Add(new BoneProfileSettings(name, path, BoneVelocityCalculationType.GlobalToLocal));
				}
			}

			for (int newBoneIndex = 0; newBoneIndex < newBoneSettings.Count; newBoneIndex++)
			{
				BoneProfileSettings newBone = newBoneSettings[newBoneIndex];
				for (int oldBoneIndex = 0; oldBoneIndex < BoneSettings.Count; oldBoneIndex++)
				{
					BoneProfileSettings oldBone = BoneSettings[oldBoneIndex];

					if (newBone.BoneName.Equals(oldBone.BoneName))
					{
						newBone.VelocityCalculationType = oldBone.VelocityCalculationType;
						newBoneSettings[newBoneIndex] = newBone;
					}
				}
			}

			BoneSettings.Clear();
			BoneSettings = newBoneSettings;


			EditorUtility.SetDirty(this);
			//AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(this)));

			AssetDatabase.SaveAssets();
		}

		public BonCalculationSettingsProfile[] GetProfilesWithTransforms(Transform root)
		{
			if (!IsValid)
			{
				return null;
			}

			Validate();

			if (BoneSettings.Count == 0)
			{
				return null;
			}
			BonCalculationSettingsProfile[] array = new BonCalculationSettingsProfile[BoneSettings.Count];

			for (int i = 0; i < array.Length; i++)
			{
				Transform t = root.Find(BoneSettings[i].Path);

				if (t == null)
				{
					throw new System.Exception($"Failed to find bone transform \"{BoneSettings[i].BoneName}\" on skeleton with root \"{root.name}\".");
				}

				array[i] = new BonCalculationSettingsProfile(
					BoneSettings[i].VelocityCalculationType,
					root.Find(BoneSettings[i].Path)
					);
			}

			return array;
		}
#endif
	}

	[System.Serializable]
	public struct BoneProfileSettings
	{
		[SerializeField]
		public string BoneName;
		[SerializeField]
		public string Path;
		[SerializeField]
		public BoneVelocityCalculationType VelocityCalculationType;

		public BoneProfileSettings(string boneName, string path, BoneVelocityCalculationType velocityCalculationType)
		{
			BoneName = boneName;
			Path = path;
			VelocityCalculationType = velocityCalculationType;
		}
	}

	public struct BonCalculationSettingsProfile
	{
		public BoneVelocityCalculationType VelocityCalculationType;
		public Transform Bone;

		public BonCalculationSettingsProfile(BoneVelocityCalculationType velocityCalculationType, Transform bone)
		{
			VelocityCalculationType = velocityCalculationType;
			Bone = bone;
		}
	}

	public enum BoneVelocityCalculationType
	{
		Local,
		GlobalToLocal,
	}
}