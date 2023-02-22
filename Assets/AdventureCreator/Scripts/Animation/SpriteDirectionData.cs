/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SpriteDirectionData.cs"
 * 
 *	A data class for the various directions that a 2D sprite-based character can face.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/** A data class for the various directions that a 2D sprite-based character can face. */
	[System.Serializable]
	public class SpriteDirectionData
	{

		#region Variables

		[SerializeField] private enum Directions { None, Four, Eight, Custom };
		[SerializeField] private Directions directions = Directions.Four;
		[SerializeField] private bool down = true, left = true, right = true, up = true, downLeft = false, upLeft = false, upRight = false, downRight = false;
		[SerializeField] private SpriteDirection[] spriteDirections = new SpriteDirection[0];
		[SerializeField] private bool isUpgraded = false;

		#endregion


		#region Constructors

		/** The default Constructor.  This takes the now-deprecated values of the Char script's doDirections and doDiagonals variables to generate initial values. */
		public SpriteDirectionData (bool doDirections, bool doDiagonals)
		{
			isUpgraded = true;
			if (!doDirections)
			{
				directions = Directions.None;
			}
			else
			{
				directions = (doDiagonals) ? Directions.Eight : Directions.Four;
			}

			spriteDirections = new SpriteDirection[0];
			CalcDirections ();
		}


		/** A Constructor that copies its values from another class instance */
		public SpriteDirectionData (SpriteDirectionData other)
		{
			isUpgraded = true;
			directions = other.directions;
			down = other.down;
			left = other.left;
			right = other.right;
			up = other.up;
			downLeft = other.downLeft;
			upLeft = other.upLeft;
			upRight = other.upRight;
			downRight = other.downRight;

			spriteDirections = new SpriteDirection[0];
			CalcDirections ();
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			directions = (Directions) UnityEditor.EditorGUILayout.EnumPopup ("Facing directions:", directions);

			if (directions == Directions.Custom)
			{
				down = UnityEditor.EditorGUILayout.Toggle ("Down?", down);
				downLeft = UnityEditor.EditorGUILayout.Toggle ("Down-left?", downLeft);
				left = UnityEditor.EditorGUILayout.Toggle ("Left?", left);
				upLeft = UnityEditor.EditorGUILayout.Toggle ("Up-left?", upLeft);
				up = UnityEditor.EditorGUILayout.Toggle ("Up?", up);
				upRight = UnityEditor.EditorGUILayout.Toggle ("Up-right?", upRight);
				right = UnityEditor.EditorGUILayout.Toggle ("Right?", right);
				downRight = UnityEditor.EditorGUILayout.Toggle ("Down-right?", downRight);
			}

			if (GUI.changed)
			{
				CalcDirections ();
			}
		}


		public string GetExpectedList (AC_2DFrameFlipping frameFlipping, string animName, string indexString = "")
		{
			if (!HasDirections ())
			{
				return "\n- " + animName + indexString;
			}

			string result = string.Empty;

			if (up) result += "\n- " + animName + "_U" + indexString;
			if (down) result += "\n- " + animName + "_D" + indexString;

			if (frameFlipping == AC_2DFrameFlipping.LeftMirrorsRight || frameFlipping == AC_2DFrameFlipping.None)
			{
				if (right) result += "\n- " + animName + "_R" + indexString;
			}
			if (frameFlipping == AC_2DFrameFlipping.RightMirrorsLeft || frameFlipping == AC_2DFrameFlipping.None)
			{
				if (left) result += "\n- " + animName + "_L" + indexString;
			}

			if (frameFlipping == AC_2DFrameFlipping.LeftMirrorsRight || frameFlipping == AC_2DFrameFlipping.None)
			{
				if (upRight) result += "\n- " + animName + "_UR" + indexString;
				if (downRight) result += "\n- " + animName + "_DR" + indexString;
			}
			if (frameFlipping == AC_2DFrameFlipping.RightMirrorsLeft || frameFlipping == AC_2DFrameFlipping.None)
			{
				if (upLeft) result += "\n- " + animName + "_UL" + indexString;
				if (downLeft) result += "\n- " + animName + "_DL" + indexString;
			}

			result += "\n";

			return result;
		}

		#endif


		#region PublicFunctions

		/**
		 * <summary>Converts the class's serializable data into a single string</summary>
		 * <returns>The class's data<returns>
		 */
		public string SaveData ()
		{
			string data = string.Empty;
			data += (down) ? "1" : "0";
			data += (left) ? "1" : "0";
			data += (right) ? "1" : "0";
			data += (up) ? "1" : "0";
			data += (downLeft) ? "1" : "0";
			data += (upLeft) ? "1" : "0";
			data += (upRight) ? "1" : "0";
			data += (downRight) ? "1" : "0";
			return data;
		}


		/**
		 * <summary>Restores the class's serializable data from a data string</summary>
		 * <param name = "data">The data string to load from</param>
		 */
		public void LoadData (string data)
		{
			if (string.IsNullOrEmpty (data) || data.Length != 8) return;

			bool _down = (data[0] == "1"[0]);
			bool _left = (data[1] == "1"[0]);
			bool _right = (data[2] == "1"[0]);
			bool _up = (data[3] == "1"[0]);
			bool _downLeft = (data[4] == "1"[0]);
			bool _upLeft = (data[5] == "1"[0]);
			bool _upRight = (data[6] == "1"[0]);
			bool _downRight = (data[7] == "1"[0]);
			isUpgraded = true;

			SetDirections (_down, _left, _right, _up, _downLeft, _upLeft, _upRight, _downRight);
		}


		/** Updates which directions are enabled */
		public void SetDirections (bool _down, bool _left, bool _right, bool _up, bool _downLeft, bool _upLeft, bool _upRight, bool _downRight)
		{
			directions = Directions.Custom;
			down = _down;
			left = _left;
			right = _right;
			up = _up;
			downLeft = _downLeft;
			upLeft = _upLeft;
			upRight = _upRight;
			downRight = _downRight;

			if (!down && !left && !right && !up && !downLeft && !upLeft && !upRight && !downRight)
			{
				directions = Directions.None;
			}
			else if (down && left && right && up && !downLeft && !upLeft && !upRight && !downRight)
			{
				directions = Directions.Four;
			}
			else if (down && left && right && up && downLeft && upLeft && upRight && downRight)
			{
				directions = Directions.Eight;
			}

			CalcDirections ();
		}


		/**
		 * <summary>Checks if the character sprite makes use of directions</summary>
		 * <returns>True if the character sprite makes use of directions</returns>
		 */
		public bool HasDirections ()
		{
			return (spriteDirections.Length > 0);
		}



		/**
		 * <summary>Gets the directional suffix (i.e. "D" for Down, "UR" for Up-right) for the sprite direction that best matches the given angle</summary>
		 * <param name = "angle">The angle, in degrees.  This starts at 0 when downward, and rotates clockwise to 360 for a complete revolution.</summary>
		 * <returns>The directional suffix (i.e. "D" for Down, "UR" for Up-right) for the sprite direction that best matches the given angle</summary>
		 */
		public string GetDirectionalSuffix (float angle)
		{
			if (HasDirections ())
			{
				if (angle > 360f)
				{
					angle -= 360f;
				}
				if (angle < 0f)
				{
					angle += 360f;
				}

				if (spriteDirections.Length <= 1)
				{
					return spriteDirections[0].suffix;
				}

				for (int i=0; i<spriteDirections.Length; i++)
				{
					if (i < (spriteDirections.Length-1))
					{
						if (spriteDirections[i+1].maxAngle < spriteDirections[i].maxAngle && (spriteDirections[i+1].maxAngle) > angle)
						{
							// Special case
							return spriteDirections[i+1].suffix;
						}

						if (spriteDirections[i].maxAngle < angle)
						{
							continue;
						}
					}
					else
					{
						if (spriteDirections[i].maxAngle < angle && (spriteDirections[0].maxAngle + 180f) < angle)
						{
							// Special case
							return spriteDirections[0].suffix;
						}
					}

					return spriteDirections[i].suffix;
				}
			}

			return string.Empty;
		}

		#endregion


		#region PrivateFunctions

		private void CalcDirections ()
		{
			if (directions == Directions.None)
			{
				spriteDirections = new SpriteDirection[0];
				down = left = right = up = downLeft = upLeft = upRight = downRight = false;
				return;
			}
			else if (directions == Directions.Four)
			{
				down = left = right = up = true;
				downLeft = upLeft = upRight = downRight = false;
			}
			else if (directions == Directions.Eight)
			{
				down = left = right = up = true;
				downLeft = upLeft = upRight = downRight = true;
			}

			List<SpriteDirection> newDirections = new List<SpriteDirection>();
			newDirections.Clear ();

			if (down)
			{
				newDirections.Add (new SpriteDirection ("D", 0f));
			}
			if (downLeft)
			{
				newDirections.Add (new SpriteDirection ("DL", 45f));
			}
			if (left)
			{
				newDirections.Add (new SpriteDirection ("L", 90f));
			}
			if (upLeft)
			{
				newDirections.Add (new SpriteDirection ("UL", 135f));
			}
			if (up)
			{
				newDirections.Add (new SpriteDirection ("U", 180f));
			}
			if (upRight)
			{
				newDirections.Add (new SpriteDirection ("UR", 225f));
			}
			if (right)
			{
				newDirections.Add (new SpriteDirection ("R", 270f));
			}
			if (downRight)
			{
				newDirections.Add (new SpriteDirection ("DR", 315f));
			}

			if (newDirections.Count > 1)
			{
				for (int i=0; i<newDirections.Count; i++)
				{
					int j = (i == newDirections.Count-1) ? 0 : i+1;

					float thisAngle = newDirections[i].angle;
					float nextAngle = newDirections[j].angle;

					if (nextAngle < thisAngle)
					{
						nextAngle += 360f;
					}

					float maxAngle = (thisAngle + nextAngle) / 2f;

					if (maxAngle > 360f)
					{
						maxAngle -= 360f;
					}

					newDirections[i] = new SpriteDirection (newDirections[i].suffix, newDirections[i].angle, maxAngle);
				}
			}

			spriteDirections = newDirections.ToArray ();
		}

		#endregion


		#region GetSet

		/**
		 * Checks if this data has been upgraded from AC v1.66.0 or earlier. The upgrade process should be automatic.
		 */
		public bool IsUpgraded
		{
			get
			{
				return isUpgraded;
			}
		}


		/**
		 * An array containing data about the various directions the sprite can face.
		 */
		public SpriteDirection[] SpriteDirections
		{
			get
			{
				return spriteDirections;
			}
		}

		#endregion


		#region PrivateStructs

		/**
		 * A data container for a direction that a sprite can face
		 */
		[System.Serializable]
		public struct SpriteDirection
		{

			/** The suffix (i.e. "D") for the direction */
			public string suffix;
			/** The angle that the direction points in */
			public float angle;
			/** The maximum value that a given angle can be and still be valid for this direction */
			public float maxAngle;


			public SpriteDirection (string suffix, float angle)
			{
				this.suffix = suffix;
				this.angle = angle;
				maxAngle = 0f;
			}


			public SpriteDirection (string suffix, float angle, float maxAngle)
			{
				this.suffix = suffix;
				this.angle = angle;
				this.maxAngle = maxAngle;
			}

		}

		#endregion

	}

}