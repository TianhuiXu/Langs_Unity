/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Expression.cs"
 * 
 *	A data container for a facial expression that a character (see: Char) can make.
 *	Expressions can involve using a different portrait graphic in MenuGraphic elements, or have their ID numbers used to affect a Mecanim parameter.
 *	They are changed in Speech lines by using the [expression:X] token, where "X" is the label defined for that particular Expression.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for a facial expression that a character (see: Char) can make.
	 * Expressions can involve using a different portrait graphic in MenuGraphic elements, or have their ID numbers used to affect a Mecanim parameter.
	 * They are changed in Speech lines by using the [expression:X] token, where "X" is the label defined for that particular Expression.
	 */
	[System.Serializable]
	public class Expression
	{

		#region Variables

		/** A unique identifier */
		public int ID;
		/** The name used in speech tokens */
		public string label;
		/** A portrait graphic to display in MenuGraphic elemets */
		public CursorIconBase portraitIcon = new CursorIconBase ();

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public Expression (int[] idArray)
		{
			ID = 0;
			portraitIcon = new CursorIconBase ();
			label = "New expression";

			// Update id based on array
			if (idArray != null && idArray.Length > 0)
			{
				foreach (int _id in idArray)
				{
					if (ID == _id)
						ID ++;
				}
			}
		}

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			label = EditorGUILayout.TextField ("Name:", label);
			CustomGUILayout.TokenLabel ("[expression:" + label + "]");
			portraitIcon.ShowGUI (false);
			GUILayout.Box (string.Empty, GUILayout.ExpandWidth (true), GUILayout.Height(1));
		}

		#endif

		#endregion

	}

}