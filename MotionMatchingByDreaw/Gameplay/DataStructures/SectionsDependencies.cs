using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	[CreateAssetMenu(fileName = "SectionsDependencies", menuName = "Motion Matching/Data/Sections dependencies")]
	public class SectionsDependencies : ScriptableObject
	{
		[SerializeField]
		public List<SectionSettings> SectionSettings;

		private Dictionary<string, int> sectionIndexes;
		public Dictionary<string, int> SectionIndexes
		{
			get
			{
				if (sectionIndexes == null)
				{
					sectionIndexes = new Dictionary<string, int>();
					for (int i = 0; i < SectionSettings.Count; i++)
					{
						SectionIndexes.Add(SectionSettings[i].name, i);
					}
				}

				return sectionIndexes;
			}
		}

		public int SectionsCount
		{
			get
			{
				return SectionSettings.Count;
			}
			private set { }
		}

		public SectionsDependencies()
		{
			SectionSettings = new List<SectionSettings>();
			SectionSettings.Add(new SectionSettings("Always"));
		}

		public string GetSectionName(int index)
		{
			return SectionSettings[index].name;
		}

		public int GetSectionIndex(string sectionName)
		{
			for (int i = 0; i < SectionSettings.Count; i++)
			{
				if (SectionSettings[i].name.Equals(sectionName))
				{
					return i;
				}
			}

			Debug.LogError($"In SectionDependencies \"{this.name}\"  can not be find section with name \"{sectionName}\"!");

			return -1;
		}

#if UNITY_EDITOR

		[SerializeField]
		public bool DeleteScriptOnDeleteAsset = true;

		public void SetSectionName(string newName, int index)
		{
			int occurrenceCounter = 0;

			string currentName = newName;
			for (int i = 0; i < SectionSettings.Count; i++)
			{
				if (currentName == SectionSettings[i].name && i != index)
				{
					occurrenceCounter++;
					i = 0;
					currentName = newName + "_" + occurrenceCounter.ToString();
				}
			}

			SectionSettings[index].name = currentName;
		}

		public bool AddSection()
		{
			if (SectionSettings.Count + 1 > MotionMatchingData.maxSectionsCounts)
			{
				Debug.LogWarning(string.Format("Max number of section is {0}", MotionMatchingData.maxSectionsCounts));
				return false;
			}

			SectionSettings.Add(new SectionSettings("Section"));

			SetSectionName("Section", SectionSettings.Count - 1);

			return true;
		}

		public void RemoveSection(int index)
		{
			SectionSettings.RemoveAt(index);
		}

		public void UpdateSectionDependecesInMMData(MotionMatchingData data)
		{
			int curretSectionsCount = data.sections.Count;

			// removing non existing sections:
			for (int dataSectionIndex = 0; dataSectionIndex < data.sections.Count; dataSectionIndex++)
			{
				bool removeSection = true;
				for (int i = 0; i < this.SectionSettings.Count; i++)
				{
					if (data.sections[dataSectionIndex].sectionName.Equals(SectionSettings[i].name))
					{
						removeSection = false;
						break;
					}
				}
				if (removeSection)
				{
					data.sections.RemoveAt(dataSectionIndex);
					dataSectionIndex--;
				}
			}

			for (int i = 0; i < this.SectionSettings.Count; i++)
			{
				bool addNewSection = true;
				for (int dataSectionIndex = 0; dataSectionIndex < data.sections.Count; dataSectionIndex++)
				{
					if (SectionSettings[i].name.Equals(data.sections[dataSectionIndex].sectionName))
					{
						addNewSection = false;
						break;
					}
				}

				if (addNewSection)
				{
					data.sections.Add(new DataSection(SectionSettings[i].name));
				}
			}


			//for (int i = 1; i < this.SectionSettings.Count; i++)
			//{
			//	if (curretSectionsCount == MotionMatchingData.maxSectionsCounts && data.sections[i].sectionName != this.SectionSettings[i].name)
			//	{
			//		data.sections[i].sectionName = this.SectionSettings[i].name;
			//	}
			//	else if (i >= data.sections.Count)
			//	{
			//		data.sections.Add(new DataSection(SectionSettings[i].name));
			//	}
			//	else if (data.sections[i].sectionName != this.SectionSettings[i].name)
			//	{
			//		data.sections[i].sectionName = this.SectionSettings[i].name;
			//	}
			//}

			//if (SectionSettings.Count < data.sections.Count)
			//{
			//	while (SectionSettings.Count != data.sections.Count)
			//	{
			//		data.sections.RemoveAt(data.sections.Count - 1);
			//	}
			//}


			for (int i = 0; i < data.frames.Count; i++)
			{
				FrameData f = data.frames[i];

				f.sections.sections = 0;

				for (int sectionIndex = 0; sectionIndex < data.sections.Count; sectionIndex++)
				{
					if (data.sections[sectionIndex].Contain(f.localTime))
					{
						f.sections.SetSection(sectionIndex, true);
					}
				}

				data.frames[i] = f;
			}

		}

#endif
	}



	[System.Serializable]
	public class SectionSettings
	{
		[SerializeField]
		public string name;
		[SerializeField]
		public List<SectionInfo> SectionInfos;

#if UNITY_EDITOR
		[SerializeField]
		public bool fold;
#endif


		public SectionSettings(string name)
		{
			this.name = name;
			SectionInfos = new List<SectionInfo>();
		}

		public bool AddNewSectionInfo(int maxInfos)
		{
			if ((SectionInfos.Count + 1) >= maxInfos)
			{
				return false;
			}

			int sectionIndex = 0;

			//for (int i = 0; i < sectionInfos.Count; i++)
			//{
			//    if (sectionIndex == sectionInfos[i].GetIndex())
			//    {
			//        sectionIndex++;
			//    }
			//}

			SectionInfos.Add(new SectionInfo(sectionIndex, 1.0f));

			return true;
		}

		public bool SetSectionIndex(int infoIndex, int newIndex)
		{
			for (int i = 0; i < SectionInfos.Count; i++)
			{
				if (infoIndex != i && newIndex == SectionInfos[i].GetIndex())
				{
					SectionInfo changedInfo = SectionInfos[i];
					changedInfo.SetIndex(-1);

					SectionInfos[i] = changedInfo;
				}
			}

			SectionInfo buffor = SectionInfos[infoIndex];
			buffor.SetIndex(newIndex);

			SectionInfos[infoIndex] = buffor;

			return true;
		}

		public void SetSectionWeight(int infoIndex, float weight)
		{
			SectionInfo buffor = SectionInfos[infoIndex];
			buffor.SetWeight(weight);

			SectionInfos[infoIndex] = buffor;
		}

	}

	[System.Serializable]
	public struct SectionInfo
	{
		[SerializeField]
		public int sectionIndex;
		[SerializeField]
		public float sectionWeight;

		public SectionInfo(int sectionIndex, float sectionWeight)
		{
			this.sectionIndex = sectionIndex;
			this.sectionWeight = sectionWeight;
		}

		public void Set(int sectionIndex, float sectionWeight)
		{
			this.sectionIndex = sectionIndex;
			this.sectionWeight = sectionWeight;
		}

		public void SetIndex(int sectionIndex)
		{
			this.sectionIndex = sectionIndex;
		}

		public void SetWeight(float sectionWeight)
		{
			this.sectionWeight = sectionWeight;
		}

		public int GetIndex()
		{
			return sectionIndex;
		}

		public float GetWeight()
		{
			return sectionWeight;
		}

	}


}