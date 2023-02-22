/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"IKCommand.cs"
 * 
 *	This stores information about an active IK limb command.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** This stores information about an active IK limb command. */
	public class IKCommand
	{

		#region Variables

		private Transform targetTransform;
		private float transitionDuration;
		private AnimationCurve transitionCurve;

		private float transitionTime;
		private float actualWeight;
		private bool isActive;

		#endregion


		#region Constructors

		/** The default Constructor */
		public IKCommand (Transform _targetTransform, AnimationCurve _transitionCurve, bool onInstantly)
		{
			targetTransform = _targetTransform;
			transitionCurve = _transitionCurve;
			transitionDuration = GetCurveDuration (transitionCurve);
			
			transitionTime = (onInstantly) ? transitionDuration : 0f;
			actualWeight = (onInstantly) ? 1f : 0f;
			isActive = true;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Updates the command's transition</summary>
		 * <param name="deltaTime">The Time.deltaTime value this frame</param>
		 */
		public void Update (float deltaTime)
		{
			if (isActive)
			{
				if (transitionTime < transitionDuration)
				{
					transitionTime += deltaTime;
					transitionTime = Mathf.Clamp (transitionTime, 0f, transitionDuration);
				}
			}
			else
			{
				if (transitionTime > 0f)
				{
					transitionTime -= deltaTime;
					transitionTime = Mathf.Clamp (transitionTime, 0f, transitionDuration);
				}
			}
			float progress = transitionTime / transitionDuration;
			actualWeight = Mathf.Clamp01 (transitionCurve.Evaluate (progress));
		}


		/**
		 * <summary>Applies the command's effect on an Animator</summary>
		 * <param name = "animator">The Animator to control</param>
		 * <param name = "avatarIKGoal">The AvatarIKGoal to control</param>
		 * <param name = "otherCommand">If set, the combined effects will be weight-blended together</param>
		 */
		public void OnAnimatorIK (Animator animator, AvatarIKGoal avatarIKGoal, IKCommand otherCommand = null)
		{
			if (otherCommand != null)
			{
				float totalWeight = otherCommand.actualWeight + actualWeight;
				Vector3 averagePosition = Vector3.Lerp (otherCommand.Position, Position, ProportionAlong);
				Quaternion averageRotation = Quaternion.Lerp (otherCommand.Rotation, Rotation, ProportionAlong);

				animator.SetIKPositionWeight (avatarIKGoal, totalWeight);
				animator.SetIKRotationWeight (avatarIKGoal, totalWeight);
				animator.SetIKPosition (avatarIKGoal, averagePosition);
				animator.SetIKRotation (avatarIKGoal, averageRotation);				
			}
			else
			{
				animator.SetIKPositionWeight (avatarIKGoal, actualWeight);
				animator.SetIKRotationWeight (avatarIKGoal, actualWeight);
				animator.SetIKPosition (avatarIKGoal, targetTransform.position);
				animator.SetIKRotation (avatarIKGoal, targetTransform.rotation);
			}
		}


		/**
		 * <summary>Deactivates the command's effect</summary>
		 * <param name = "deactivationCurve">If set, the effect's transition data will be replaced by this</param>
		 */
		public void Deactivate (AnimationCurve deactivationCurve = null)
		{
			if (deactivationCurve != null)
			{
				float newDuration = GetCurveDuration (deactivationCurve);
				float durationChangeAmount = newDuration / transitionDuration;
				transitionDuration = newDuration;
				transitionTime *= durationChangeAmount;
				Update (0f);
			}
			isActive = false;
		}


		/**
		 * <summary>Checks if the command refers to a given target transform</summary>
		 * <param name="transform">The transform to check for</param>
		 * <returns>True if the command refers to the transform</returns>
		 */
		public bool HasTarget (Transform transform)
		{
			return (targetTransform == transform);
		}


		/** Serializes data about the command into a single string */
		public string GetSaveData ()
		{
			int targetID = 0;
			ConstantID targetIDComponent = (targetTransform) ? targetTransform.GetComponent<ConstantID> () : null;
			if (targetIDComponent != null)
			{
				targetID = targetIDComponent.constantID;
			}

			if (targetID == 0 && targetTransform)
			{
				ACDebug.LogWarning ("Cannot save IK target " + targetTransform + " because it has no Constant ID component.", targetTransform);
			}

			string data = targetID.ToString () + ":";
			data += CurveToString (transitionCurve);
			return data;
		}


		/** Creates a new instance of the class from string data */
		public static IKCommand LoadData (string data)
		{
			string[] dataArray = data.Split (":"[0]);
			if (dataArray.Length != 2) return null;

			string targetData = dataArray[0];
			int targetID = 0;
			if (int.TryParse (targetData, out targetID))
			{
				if (targetID != 0)
				{
					ConstantID targetConstantID = ConstantID.GetComponent (targetID);
					if (targetConstantID)
					{
						string curveData = dataArray[1];
						AnimationCurve transitionCurve = StringToCurve (curveData);
						if (transitionCurve != null)
						{
							return new IKCommand (targetConstantID.transform, transitionCurve, true);
						}
					}
					else
					{
						ACDebug.LogWarning ("Could not find IK target with Constant ID = " + targetID);
					}
				}
			}
			return null;
		}

		#endregion


		#region PrivateFunctions

		private float GetCurveDuration (AnimationCurve curve)
		{
			return (curve != null) ? Mathf.Max (0f, curve.keys[curve.keys.Length - 1].time) : 0f;
		}


		private static string CurveToString (AnimationCurve animationCurve)
		{
			string data = string.Empty;
			for (int i=0; i<animationCurve.keys.Length; i++)
			{
				Keyframe keyframe = animationCurve.keys[i];
				data += keyframe.time.ToString () + "," + keyframe.value.ToString ();
				if (i < animationCurve.keys.Length - 1)
				{
					data += "#";
				}
			}
			return data;
			// (0,1) (2,3) (4,5) => 0,1#2,3#4,5
		}


		private static AnimationCurve StringToCurve (string data)
		{
			string[] allKeyData = data.Split ("#"[0]);
			List<Keyframe> keyframes = new List<Keyframe>();

			for (int i=0; i<allKeyData.Length; i++)
			{
				string[] singleKeyData = allKeyData[i].Split (","[0]);
				if (singleKeyData.Length == 2)
				{
					float time = 0f;
					if (float.TryParse (singleKeyData[0], out time))
					{
						float value = 0f;
						if (float.TryParse (singleKeyData[1], out value))
						{
							keyframes.Add (new Keyframe (time, value));
						}
					}
				}
			}
			
			if (keyframes.Count > 0)
			{
				return new AnimationCurve (keyframes.ToArray ());
			}
			return null;
		}

		#endregion


		#region GetSet

		/** True if the command is no longer having an effect */
		public bool IsReadyToDestroy
		{
			get
			{
				return (!isActive && actualWeight <= 0f);
			}
		}


		/** True if the command is active */
		public bool IsActive
		{
			get
			{
				return isActive;
			}
		}


		/** True if the command is currently in transition */
		public bool IsInTransition
		{
			get
			{
				return (actualWeight < 1f);
			}
		}


		private float ProportionAlong { get { return transitionTime / transitionDuration; } }
		private Vector3 Position { get { return targetTransform.position;  } }
		private Quaternion Rotation { get { return targetTransform.rotation; } }

		#endregion

	}

}
 