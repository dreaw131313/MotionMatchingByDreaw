using UnityEngine;

namespace MotionMatching.Gameplay
{
	[System.Serializable]
	public struct FrameSections
	{
		[SerializeField]
		public uint sections;

		public FrameSections(bool alwaysCheck)
		{
			sections = 1;
		}

		public FrameSections(uint startSections)
		{
			this.sections = startSections;
		}

		public void SetSection(int index, bool value)
		{
			if (value)
			{
				uint s = 1;
				s = s << index;
				sections = s | sections;
			}
			else
			{
				uint s = 1;
				s = s << index;
				sections = ~s & sections;
			}
		}

		public bool GetSection(int index)
		{
			uint s = sections;
			s = (s >> index) & 1;
			return s != 0;
		}

	}
}
