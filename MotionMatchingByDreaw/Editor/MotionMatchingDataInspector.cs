using MotionMatching.Gameplay;
using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(MotionMatchingData))]
	public class MotionMatchingDataInspector : Editor
	{
		MotionMatchingData data;
		Vector2 scroll;

		// FOLDS
		bool drawRawOptions;

		private void OnEnable()
		{
			data = (MotionMatchingData)this.target;
			scroll = Vector2.zero;
		}

		public override void OnInspectorGUI()
		{
			GUILayoutElements.DrawHeader(data.name, GUIResources.GetMediumHeaderStyle_LG());

			scroll = GUILayout.BeginScrollView(scroll);

			GUILayout.Space(10);
			if (GUILayoutElements.DrawHeader(
				"Basic options",
				GUIResources.GetMediumHeaderStyle_MD(),
				GUIResources.GetLightHeaderStyle_MD(),
				ref this.data.basicOptionsFold
				))
			{
				DrawBasicOptions();
			}
			GUILayout.Space(5);
			if (GUILayoutElements.DrawHeader(
				"Sections",
				GUIResources.GetMediumHeaderStyle_MD(),
				GUIResources.GetLightHeaderStyle_MD(),
				ref data.sectionFold
				))
			{
				DrawSections();
			}

			GUILayout.Space(5);
			if (GUILayoutElements.DrawHeader(
				"Data type options",
				GUIResources.GetMediumHeaderStyle_MD(),
				GUIResources.GetLightHeaderStyle_MD(),
				ref data.additionalOptionsFold
				))
			{
				DrawTypeInfo();
			}

			GUILayout.Space(5);

			if (GUILayoutElements.DrawHeader(
				"Contact Points",
				GUIResources.GetMediumHeaderStyle_MD(),
				GUIResources.GetLightHeaderStyle_MD(),
				ref data.contactPointsFold
				))
			{
				DrawContactPoints();
			}

			GUILayout.EndScrollView();
			GUILayout.Space(20);

			drawRawOptions = EditorGUILayout.Toggle(new GUIContent("Draw raw options"), drawRawOptions);
			if (drawRawOptions)
			{
				base.OnInspectorGUI();
			}

			if (data != null)
			{
				EditorUtility.SetDirty(data);
			}

			GUILayout.Space(50);
		}

		AnimationClip clip;

		private void OnDisable()
		{

		}

		private void DrawBasicOptions()
		{
			EditorGUILayout.EnumPopup(
				new GUIContent("Data type"),
				this.data.dataType
				);
			for (int i = 0; i < data.clips.Count; i++)
			{
				EditorGUILayout.ObjectField(data.clips[i], typeof(AnimationClip), true);
			}
			EditorGUILayout.LabelField(new GUIContent("Length: \t\t\t\t" + data.animationLength));
			EditorGUILayout.LabelField(new GUIContent("Motion matching data frame time: \t" + data.frameTime));
			EditorGUILayout.LabelField(new GUIContent("Motion matching data frame count: \t" + data.numberOfFrames));

			GUILayout.Space(5);

			//EditorGUILayout.ObjectField(new GUIContent("Animation clip"), data.clips, typeof(AnimationClip), false);
			data.blendToYourself = EditorGUILayout.Toggle(
					new GUIContent("Blend to yourself"),
					data.blendToYourself
				);
			data.findInYourself = EditorGUILayout.Toggle(
					new GUIContent("Find in yourself"),
					data.findInYourself
				);
		}

		#region Drawing Sections:
		private void DrawSections()
		{
			GUILayout.Space(5);
			DrawNeverCheckingSecction(data.neverChecking, ref data.neverChecking.fold);
			GUILayout.Space(5);
			DrawNonEditableSection(data.notLookingForNewPose, ref data.notLookingForNewPose.fold);
			GUILayout.Space(5);
			/*
			GUILayout.BeginHorizontal();
			GUILayoutElements.DrawHeader("Editable sections:", GUIResources.GetMediumHeaderStyle_SM());
			if (GUILayout.Button("Add Section", GUILayout.Height(20)))
			{
				if (data.sections.Count < MotionMatchingData.maxSectionsCounts)
				{
					data.sections.Add(new DataSection("New Section"));
					//OnAddSection(data.sections.Count - 1);
				}
				else
				{
					Debug.LogWarning("AnimationData can contains max " + MotionMatchingData.maxSectionsCounts + " sections!");
				}
			}
			if (GUILayout.Button("Clear", GUILayout.Height(20)))
			{
				int removingIndex = 1;
				for (int i = 1; i < data.sections.Count; i++)
				{
					data.sections.RemoveAt(i);
					i--;
					OnRemoveSection(removingIndex);
					removingIndex++;
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			for (int i = 0; i < data.sections.Count; i++)
			{
				DrawEditableSection(data.sections[i], ref i);
				GUILayout.Space(5);
			}*/
		}

		private void DrawEditableSection(DataSection section, ref int index)
		{
			GUILayout.BeginHorizontal();
			GUILayoutElements.DrawHeader(
				"Section " + index + ":  " + section.sectionName,
				GUIResources.GetLightHeaderStyle_SM(),
				GUIResources.GetMediumHeaderStyle_SM(),
				ref section.fold
				);
			if (index != 0)
			{
				if (GUILayout.Button("X", GUILayout.Width(20)))
				{
					data.sections.RemoveAt(index);
					OnRemoveSection(index);
					index--;
					return;
				}
			}
			GUILayout.EndHorizontal();

			if (section.fold)
			{
				return;
			}

			GUILayout.BeginHorizontal();

			if (index != 0)
			{
				section.sectionName = EditorGUILayout.TextField(new GUIContent("Section name"), section.sectionName);
				//if (GUILayout.Button("Remove", GUILayout.Width(60)))
				//{
				//    data.timeSection.RemoveAt(index);
				//    OnRemoveSection(index);
				//    return;
				//}
			}

			GUILayout.EndHorizontal();

			DrawIntervalsTable(section, index);

		}

		private void DrawNonEditableSection(DataSection section, ref bool fold)
		{
			if (!GUILayoutElements.DrawHeader(
					"Section :  " + section.sectionName,
					GUIResources.GetMediumHeaderStyle_SM(),
					GUIResources.GetLightHeaderStyle_SM(),
					ref fold
				))
			{
				return;
			}
			for (int intervalIndex = 0; intervalIndex < section.timeIntervals.Count; intervalIndex++)
			{
				float min = section.timeIntervals[intervalIndex].x;
				float max = section.timeIntervals[intervalIndex].y;

				GUILayout.BeginHorizontal();
				min = EditorGUILayout.FloatField(Mathf.Clamp(min, 0f, max), GUILayout.Width(60));
				EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, data.animationLength);
				max = EditorGUILayout.FloatField(Mathf.Clamp(max, min, data.animationLength), GUILayout.Width(60));

				min = (float)Math.Round(min, 4);
				max = (float)Math.Round(max, 4);

				section.SetTimeIntervalWithCheck(intervalIndex, new float2(min, max));

				if (GUILayout.Button("X", GUILayout.Width(20)))
				{
					section.timeIntervals.RemoveAt(intervalIndex);
					intervalIndex = Mathf.Clamp(intervalIndex - 1, 0, int.MaxValue);
				}
				GUILayout.EndHorizontal();

			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add interval"))
			{
				section.timeIntervals.Add(new float2(0f, data.animationLength));
			}
			if (GUILayout.Button("Clear"))
			{
				section.timeIntervals.Clear();
			}
			GUILayout.EndHorizontal();

		}

		private void DrawIntervalsTable(DataSection section, int sectionIndex)
		{
			if (section.timeIntervals.Count == 0)
			{
				for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
				{
					FrameData buffor = data[frameIndex];

					buffor.sections.SetSection(sectionIndex, false);

					data.frames[frameIndex] = buffor;
				}
			}
			for (int intervalIndex = 0; intervalIndex < section.timeIntervals.Count; intervalIndex++)
			{
				float min = section.timeIntervals[intervalIndex].x;
				float max = section.timeIntervals[intervalIndex].y;

				GUILayout.BeginHorizontal();
				min = EditorGUILayout.FloatField(Mathf.Clamp(min, 0f, max), GUILayout.Width(60));
				EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, data.animationLength);
				max = EditorGUILayout.FloatField(Mathf.Clamp(max, min, data.animationLength), GUILayout.Width(60));

				min = (float)Math.Round(min, 4);
				max = (float)Math.Round(max, 4);

				if (section.SetTimeIntervalWithCheck(intervalIndex, new float2(min, max)))
				{
					for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
					{
						ChangeSectionInFrame(frameIndex, sectionIndex);
					}
				}

				if (GUILayout.Button("X", GUILayout.Width(20)))
				{
					section.timeIntervals.RemoveAt(intervalIndex);
					intervalIndex = Mathf.Clamp(intervalIndex - 1, 0, int.MaxValue);

					for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
					{
						ChangeSectionInFrame(frameIndex, sectionIndex);
					}
				}
				GUILayout.EndHorizontal();

			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add interval"))
			{
				if (section.timeIntervals.Count == 0)
				{
					section.timeIntervals.Add(new float2(0f, data.animationLength));
					for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
					{
						ChangeSectionInFrame(frameIndex, sectionIndex);
					}
				}
				else
				{
					section.timeIntervals.Add(section.timeIntervals[section.timeIntervals.Count - 1]);
				}
			}
			if (GUILayout.Button("Clear"))
			{
				section.timeIntervals.Clear();
			}
			GUILayout.EndHorizontal();
		}

		private void ChangeSectionInFrame(int frameIndex, int sectionIndex)
		{
			FrameData buffor = data[frameIndex];

			bool result = false;
			for (int i = 0; i < data.sections[sectionIndex].timeIntervals.Count; i++)
			{
				if (buffor.localTime >= data.sections[sectionIndex].timeIntervals[i].x &&
					buffor.localTime <= data.sections[sectionIndex].timeIntervals[i].y)
				{
					result = true;
					break;
				}
			}
			buffor.sections.SetSection(sectionIndex, result);

			data.frames[frameIndex] = buffor;
		}

		private void OnAddSection(int sectionIndex)
		{
			for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
			{
				FrameData buffor = data[frameIndex];

				buffor.sections.SetSection(sectionIndex, true);

				data.frames[frameIndex] = buffor;
			}
		}

		private void OnRemoveSection(int sectionIndex)
		{
			for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
			{
				FrameData buffor = data[frameIndex];

				buffor.sections.SetSection(sectionIndex, false);

				data.frames[frameIndex] = buffor;
			}
		}

		private void DrawNeverCheckingSecction(DataSection section, ref bool fold)
		{
			if (!GUILayoutElements.DrawHeader(
					"Section :  " + section.sectionName,
					GUIResources.GetMediumHeaderStyle_SM(),
					GUIResources.GetLightHeaderStyle_SM(),
					ref fold
				))
			{
				return;
			}
			for (int intervalIndex = 0; intervalIndex < section.timeIntervals.Count; intervalIndex++)
			{
				float min = section.timeIntervals[intervalIndex].x;
				float max = section.timeIntervals[intervalIndex].y;

				GUILayout.BeginHorizontal();
				min = EditorGUILayout.FloatField(Mathf.Clamp(min, 0f, max), GUILayout.Width(60));
				EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, data.animationLength);
				max = EditorGUILayout.FloatField(Mathf.Clamp(max, min, data.animationLength), GUILayout.Width(60));

				min = (float)Math.Round(min, 4);
				max = (float)Math.Round(max, 4);

				if (section.SetTimeIntervalWithCheck(intervalIndex, new float2(min, max)))
				{
					data.usedFrameCount = 0;
					for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
					{
						if (!section.Contain(data[frameIndex].localTime))
						{
							data.usedFrameCount++;
						}
					}
				}

				if (GUILayout.Button("X", GUILayout.Width(20)))
				{
					section.timeIntervals.RemoveAt(intervalIndex);
					intervalIndex = Mathf.Clamp(intervalIndex - 1, 0, int.MaxValue);

					data.usedFrameCount = 0;
					for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
					{
						if (!section.Contain(data[frameIndex].localTime))
						{
							data.usedFrameCount++;
						}
					}

				}
				GUILayout.EndHorizontal();

			}

			if (section.timeIntervals.Count == 0)
			{
				data.usedFrameCount = data.numberOfFrames;
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add interval"))
			{
				section.timeIntervals.Add(new float2(0f, data.animationLength));

				data.usedFrameCount = 0;
				for (int frameIndex = 0; frameIndex < data.numberOfFrames; frameIndex++)
				{
					if (!section.Contain(data[frameIndex].localTime))
					{
						data.usedFrameCount++;
					}
				}

			}
			if (GUILayout.Button("Clear"))
			{
				section.timeIntervals.Clear();
			}
			GUILayout.EndHorizontal();

		}

		private void DrawTypeInfo()
		{
			switch (this.data.dataType)
			{
				case AnimationDataType.SingleAnimation:
					GUILayout.Label("SINGLE ANMATION type have not Extra Options!");
					break;
				case AnimationDataType.BlendTree:
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical();
					GUILayout.Label("Animations");
					for (int i = 0; i < data.clips.Count; i++)
					{
						EditorGUILayout.ObjectField(data.clips[i], typeof(AnimationClip), true);
					}
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					GUILayout.Label("Weights");
					for (int i = 0; i < data.clips.Count; i++)
					{
						GUILayout.Label(data.blendTreeWeights[i].ToString());
					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					break;
			}
		}

		#endregion

		private void DrawContactPoints()
		{



			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();
			GUILayout.Label("Nr.");

			for (int i = 0; i < data.contactPoints.Count; i++)
			{
				GUILayout.Label(string.Format("{0}. ", i + 1));
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("Contact start time");

			for (int i = 0; i < data.contactPoints.Count; i++)
			{
				GUILayout.Label(string.Format("{0}", data.contactPoints[i].startTime));
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("Contact end time");

			for (int i = 0; i < data.contactPoints.Count; i++)
			{
				GUILayout.Label(string.Format("{0}", data.contactPoints[i].endTime));
			}
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
		}
	}
}
