using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{

	public class MotionMatchingSystemSettings : ScriptableObject
	{
		[SerializeField]
		private List<ThreadUsageSettings> threadsToUse;
		[Space(5)]
		[SerializeField]
		[Min(1)]
		[Tooltip("This value will be clamped to SystemInfo.processorCount-1")]
		private int defaultThreadCountToUse;

		[Space(10)]
		[SerializeField]
		[Min(1)]
		private int maxFramesPerThread;

		private int? threadCountToUse = null;
		public int MaxFramesPerThread { get => maxFramesPerThread; private set => maxFramesPerThread = value; }
		public int? ThreadCountToUse
		{
			get
			{
				if (threadCountToUse == null)
				{
					SelectThreadCount();
				}
				return threadCountToUse;
			}
			private set => threadCountToUse = value;
		}

		private void SelectThreadCount()
		{
			int availableThreads = SystemInfo.processorCount;
			int maxThreads = Mathf.Clamp(availableThreads - 1, 1, int.MaxValue);

			threadCountToUse = Mathf.Clamp(defaultThreadCountToUse, 1, maxThreads);

			if (threadsToUse != null && threadsToUse.Count > 0)
			{
				for (int i = 0; i < threadsToUse.Count; i++)
				{
					ThreadUsageSettings settings = threadsToUse[i];
					if (settings.AvailableThreads == availableThreads)
					{
						threadCountToUse = Mathf.Clamp(settings.ThreadsToUse, 1, maxThreads);
					}
				}
			}
		}


#if UNITY_EDITOR
		private const string settingsSaveDirctory = "MotionMatchingByDreawAssetSettings";
		private const string settingsAssetName = "MotionMatchingByDreawSettings.asset";
		private static MotionMatchingSystemSettings m_Settings = null;

		[MenuItem("Generate Settings", menuItem = "MotionMatching/Settings/Generate Settings", priority = 100)]
		private static void GenerateSettings()
		{
			MotionMatchingSystemSettings settings = GetOrCreateSettings();
		}

		[MenuItem("Select settings asset", menuItem = "MotionMatching/Settings/Select settings asset", priority = 100)]
		private static void SelectSettingsAsset()
		{
			EditorGUIUtility.PingObject(GetOrCreateSettings());
		}

		public static MotionMatchingSystemSettings GetOrCreateSettings()
		{
			if (m_Settings != null)
			{
				return m_Settings;
			}

			string assetPath = $"Assets/{settingsSaveDirctory}/{settingsAssetName}";

			m_Settings = AssetDatabase.LoadAssetAtPath<MotionMatchingSystemSettings>(assetPath);
			if (m_Settings == null)
			{
				string absolutePath = $"{Application.dataPath}/{settingsSaveDirctory}/{settingsAssetName}";
				string assetFolderAbsolutePath = $"{Application.dataPath}/{settingsSaveDirctory}";
				if (!Directory.Exists(assetFolderAbsolutePath))
				{
					Directory.CreateDirectory(assetFolderAbsolutePath);
				}

				m_Settings = ScriptableObject.CreateInstance<MotionMatchingSystemSettings>();
				m_Settings.CreateDefaultSettings();
				AssetDatabase.CreateAsset(m_Settings, assetPath);
				EditorUtility.SetDirty(m_Settings);
				AssetDatabase.SaveAssets();
			}

			return m_Settings;
		}

		private void CreateDefaultSettings()
		{
			defaultThreadCountToUse = 8;
			maxFramesPerThread = 2000;

			threadsToUse = new List<ThreadUsageSettings>();
			threadsToUse.Add(new ThreadUsageSettings(1, 1));
			threadsToUse.Add(new ThreadUsageSettings(2, 1));
			threadsToUse.Add(new ThreadUsageSettings(3, 1));
			threadsToUse.Add(new ThreadUsageSettings(4, 2));
			threadsToUse.Add(new ThreadUsageSettings(6, 3));
			threadsToUse.Add(new ThreadUsageSettings(8, 4));
			threadsToUse.Add(new ThreadUsageSettings(12, 6));
			threadsToUse.Add(new ThreadUsageSettings(16, 8));
			threadsToUse.Add(new ThreadUsageSettings(20, 10));

		}
#endif
	}

	[System.Serializable]
	public struct ThreadUsageSettings
	{
		[Min(1)]
		public int AvailableThreads;
		[Min(1)]
		[Tooltip("This value will be clamped to SystemInfo.processorCount-1")]
		public int ThreadsToUse;

		public ThreadUsageSettings(int availableThreads, int threadsToUse)
		{
			AvailableThreads = availableThreads;
			ThreadsToUse = threadsToUse;
		}
	}

}