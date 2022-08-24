using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
	[CustomEditor(typeof(SectionsDependencies))]
	public class SectionsDependenciesEditor : Editor
	{
		private SectionsDependencies data;

		List<string> sectionsNames;

		bool drawRawOption = false;
		private void OnEnable()
		{
			data = (SectionsDependencies)this.target;
		}

		public override void OnInspectorGUI()
		{

			if (sectionsNames == null)
			{
				sectionsNames = new List<string>();
			}
			sectionsNames.Clear();

			for (int i = 1; i < data.SectionSettings.Count; i++)
			{
				sectionsNames.Add(data.SectionSettings[i].name);
			}

			GUILayoutElements.DrawHeader(data.name, GUIResources.GetMediumHeaderStyle_LG());

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Section", GUIResources.Button_MD()))
			{
				data.AddSection();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			for (int i = 0; i < data.SectionSettings.Count; i++)
			{
				if (i == 0)
				{
					GUILayoutElements.DrawHeader(
							   string.Format("{0}. {1}", i, data.SectionSettings[i].name),
							   GUIResources.GetMediumHeaderStyle_MD()
							   );
					GUILayout.Space(5);
					continue;
				}
				GUILayout.BeginHorizontal();
				GUILayoutElements.DrawHeader(
						   data.SectionSettings[i].name,
						   GUIResources.GetMediumHeaderStyle_MD(),
						   GUIResources.GetLightHeaderStyle_MD(),
						   ref data.SectionSettings[i].fold
						   );

				if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
				{
					data.SectionSettings.RemoveAt(i);
					i--;
					continue;
				}
				GUILayout.EndHorizontal();

				if (data.SectionSettings[i].fold)
				{
					GUILayout.Space(5);
					DrawSectionSettings(data.SectionSettings[i], i);
				}
				GUILayout.Space(5);
			}

			GUILayout.Space(10);


			data.DeleteScriptOnDeleteAsset = EditorGUILayout.Toggle("Delete enum script on delete asset", data.DeleteScriptOnDeleteAsset);
			GUILayout.Space(5);
			if (GUILayout.Button("Generate enum script", GUIResources.Button_MD()))
			{
				GenerateEnumScript(data);
			}

			GUILayout.Space(10);
			drawRawOption = EditorGUILayout.Toggle("Draw raw options", drawRawOption);

			if (drawRawOption)
			{
				base.OnInspectorGUI();
			}

			if (data != null)
			{
				EditorUtility.SetDirty(data);
			}
		}

		private void DrawSectionSettings(SectionSettings settings, int index)
		{

			data.SetSectionName(EditorGUILayout.TextField("Section name", settings.name), index);

			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Cost to section");
			GUILayout.Space(15);
			GUILayout.Label("Cost weight");
			GUILayout.Space(40);
			GUILayout.EndHorizontal();

			for (int i = 0; i < settings.SectionInfos.Count; i++)
			{
				GUILayout.BeginHorizontal();

				settings.SetSectionIndex(
					i,
					EditorGUILayout.Popup(settings.SectionInfos[i].GetIndex() - 1, sectionsNames.ToArray()) + 1
					);


				GUILayout.Space(15);

				settings.SetSectionWeight(
					i,
					EditorGUILayout.FloatField(settings.SectionInfos[i].GetWeight())
					);

				if (GUILayout.Button("X", GUILayout.Width(25)))
				{
					settings.SectionInfos.RemoveAt(i);
					i--;
				}
				GUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add section info", GUILayout.MaxWidth(200)))
			{
				settings.AddNewSectionInfo(data.SectionSettings.Count);
			}
		}


		[MenuItem("MotionMatching/Helpers/Generate Section Dependencies enum scripts", priority = 1001)]
		private static void GenerateSectionClassesForAllSections()
		{
			string[] groupsGUIDS = AssetDatabase.FindAssets("t:SectionsDependencies");

			int index = 1;


			foreach (string guid in groupsGUIDS)
			{
				SectionsDependencies sd = AssetDatabase.LoadAssetAtPath<SectionsDependencies>(AssetDatabase.GUIDToAssetPath(guid));

				if (sd != null)
				{
					GenerateEnumScriptWithoutCompilation(sd);
				}
			}


			AssetDatabase.Refresh();
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}


		#region generating enum class for section dependencies
		private const string settingsSaveDirctory = "MotionMatchingByDreawAssetSettings";
		private const string sectionEnumsFolder = "SectionEnums";

		public static string GetPathToEnumScript(SectionsDependencies sections)
		{
			return $"{Application.dataPath}/{settingsSaveDirctory}/{sectionEnumsFolder}/{sections.name}.cs";
		}

		public static string GenerateClassCode(SectionsDependencies sections)
		{
			string namespaceToEnumCode = $"namespace MotionMatching.Gameplay.Sections \n" + "{\n";


			string enumMaskCode = "\t\t[System.Flags]\n";
			enumMaskCode = enumMaskCode + "\t\tpublic enum " + sections.name + "_Mask\n\t\t{\n";

			for (int i = 0; i < sections.SectionSettings.Count; i++)
			{
				enumMaskCode += $"\t\t\t{sections.SectionSettings[i].name} = {(1 << i)},\n";
			}

			enumMaskCode = enumMaskCode + "\t\t}";

			string enumIndexCode = "\n\t\tpublic enum " + sections.name + "_Index\n\t\t{\n";

			for (int i = 0; i < sections.SectionSettings.Count; i++)
			{
				enumIndexCode += $"\t\t\t{sections.SectionSettings[i].name} = {i},\n";
			}

			enumIndexCode = enumIndexCode + "\t\t}";

			string code = namespaceToEnumCode + enumMaskCode + "\n" + enumIndexCode + "\n}";

			return code;
		}

		public static void GenerateEnumScriptWithoutCompilation(SectionsDependencies sections)
		{
			string absolutePathToScript = $"{Application.dataPath}/{settingsSaveDirctory}/{sectionEnumsFolder}/{sections.name}.cs";
			string absolutePathToScriptFolder = $"{Application.dataPath}/{settingsSaveDirctory}/{sectionEnumsFolder}";

			string assetSettingsFolder = $"{Application.dataPath}/{settingsSaveDirctory}";
			if (!Directory.Exists(absolutePathToScriptFolder))
			{
				Directory.CreateDirectory(absolutePathToScriptFolder);
			}


			string content = GenerateClassCode(sections);
			File.WriteAllText(absolutePathToScript, content);

		}

		public static void GenerateEnumScript(SectionsDependencies sections)
		{
			GenerateEnumScriptWithoutCompilation(sections);

			AssetDatabase.Refresh();
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}

		#endregion

	}

	[InitializeOnLoad]
	public class SectionDependeciesDatabaseEditor : UnityEditor.AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string AssetPath, RemoveAssetOptions rao)
		{
			SectionsDependencies sd = (SectionsDependencies)AssetDatabase.LoadAssetAtPath(AssetPath, typeof(SectionsDependencies));
			if (sd != null && sd.DeleteScriptOnDeleteAsset)
			{
				Debug.Log(AssetDatabase.GetAssetPath(sd));
				string scriptPath = SectionsDependenciesEditor.GetPathToEnumScript(sd);
				if (File.Exists(scriptPath))
				{
					File.Delete(scriptPath);
					AssetDatabase.Refresh();
					UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
				}
			}

			return AssetDeleteResult.DidNotDelete;
		}
	}
}
