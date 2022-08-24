using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatching.Gameplay
{
	public enum AnimationDataType
	{
		SingleAnimation,
		BlendTree
	}

	// Trajectory enums
	public enum TrajectoryCorrectionType
	{
		Constant, // based on first trajectory point with future time
				  //ReachTarget,
		Progresive, // based on first trajectory point with future time
		MatchOrientationConstant, // based on first trajectory point with future time
		MatchOrientationProgresive, // based on first trajectory point with future time
		StrafeConstant,
		StarfeProgresive,
		None
	}

	public enum PastTrajectoryType
	{
		Recorded,
		CopyFromCurrentData,
		None
	}


	// Contact state enums 
	public enum ContactPointPositionCorrectionType
	{
		MovePosition,
		LerpPosition,
		LerpWithCurve
	}

	public enum ContactStateType
	{
		NormalContacts,
		Impacts
	}

	public enum ContactPointCostType
	{
		Position,
		//Normal_OR_Direction,
		//PositionNormal_OR_Direction,
		None
	}

	public enum ContactPointType
	{
		Start,
		Contact,
		End,
		Adapted,
	}

	[System.Flags]
	public enum ContactStateTimeScalingPositionMask
	{
		X = (1 << 0),
		Y = (1 << 1),
		Z = (1 << 2)
	}


	// Single animation enums
	public enum SingleAnimationUpdateType
	{
		PlaySelected,
		PlayInSequence,
		PlayRandom
	}

	public enum SingleAnimationFindingType
	{
		FindInAll,
		FindInSpecificAnimation,
	}



	// Contact enums
	//public enum ContactStateMovemetType
	//{
	//	StartContact,
	//	Contact,
	//	ContactLand,
	//	StartLand,
	//	StartContactLand,
	//	Land,
	//	None
	//	//OwnMethod
	//}

	public enum ContactRotatationType
	{
		None = 0,
		RotateOnContact = 1,
		RotateToConatct = 2,
		RotateToAndOnContact = 3
	}

}
