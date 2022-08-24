using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(DataCreatorTrajectorySettings))]
	public class DataCreatorTrajectorySettingsCustomInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Generate Times"))
			{
				DataCreatorTrajectorySettings times = target as DataCreatorTrajectorySettings;

				if (times != null)
				{
					if (times == null)
					{
						times.TrajectoryTimes = new List<float>();
					}
					else
					{
						times.TrajectoryTimes.Clear();
					}

					// past points
					if (times.PastPointsCount > 0 && times.MinTime < 0)
					{
						float delta = times.MinTime / times.PastPointsCount;
						for (int i = 0; i < times.PastPointsCount; i++)
						{
							times.TrajectoryTimes.Add(times.MinTime - i * delta);
						}
					}

					// future times
					if (times.FuterPointsCount > 0 && times.MaxTime > 0)
					{
						float delta = times.MaxTime / times.FuterPointsCount;
						for (int i = 1; i < times.FuterPointsCount; i++)
						{
							times.TrajectoryTimes.Add(delta * i);
						}
						times.TrajectoryTimes.Add(times.MaxTime);
					}
				}
			}
		}

	}
}