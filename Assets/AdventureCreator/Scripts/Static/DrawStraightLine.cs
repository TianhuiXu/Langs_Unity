/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerCursor.cs"
 * 
 *	This script is adapted from the code found here:
 *	http://forum.unity3d.com/threads/71979-Drawing-lines-in-the-editor
 * 
 */


using UnityEngine;

namespace AC
{

	/**
	 * A class that provides line-drawing functions.
	 */
	public class DrawStraightLine
	{

		private static Texture2D _aaLineTex = null;
		private static Texture2D _lineTex = null;


		private static Texture2D adLineTex
		{
			get
			{
				if (!_aaLineTex)
				{
					_aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, true);
					_aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
					_aaLineTex.SetPixel(0, 1, Color.white);
					_aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
					_aaLineTex.Apply();
				}
				return _aaLineTex;
			}
		}


		private static Texture2D lineTex
		{
			get
			{
				if (!_lineTex)
				{
					_lineTex = new Texture2D(1, 1, TextureFormat.ARGB32, true);
					_lineTex.SetPixel(0, 1, Color.white);
					_lineTex.Apply();
				}
				return _lineTex;
			}
		}


		private static void DrawLineMac (Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
		{
			Color savedColor = GUI.color;
			Matrix4x4 savedMatrix = GUI.matrix;

			float oldWidth = width;

			if (antiAlias)
			{
				width *= 3;
			}

			float angle = Vector3.Angle (pointB - pointA, Vector2.right) * (pointA.y <= pointB.y?1:-1);
			float m = (pointB - pointA).magnitude;
	 
			if (m > 0.01f)
			{
				Vector3 dz = new Vector3(pointA.x, pointA.y, 0);
				Vector3 offset = new Vector3((pointB.x - pointA.x) * 0.5f, (pointB.y - pointA.y) * 0.5f, 0f);
	 
				Vector3 tmp = Vector3.zero;

				if (antiAlias)
				{
					tmp = new Vector3 (-oldWidth * 1.5f * Mathf.Sin(angle * Mathf.Deg2Rad), oldWidth * 1.5f * Mathf.Cos (angle * Mathf.Deg2Rad));
				}
				else
				{
					tmp = new Vector3 (-oldWidth * 0.5f * Mathf.Sin(angle * Mathf.Deg2Rad), oldWidth * 0.5f * Mathf.Cos (angle * Mathf.Deg2Rad));
				}

				GUI.color = color;
				GUI.matrix = translationMatrix (dz) * GUI.matrix;
				GUIUtility.ScaleAroundPivot (new Vector2 (m, width), new Vector2(-0.5f, 0));
				GUI.matrix = translationMatrix (-dz) * GUI.matrix;
				GUIUtility.RotateAroundPivot (angle, Vector2.zero);
				GUI.matrix = translationMatrix (dz  - tmp - offset) * GUI.matrix;
	 
				GUI.DrawTexture(new Rect(0, 0, 1, 1), antiAlias ? adLineTex :  lineTex);
			}

			GUI.matrix = savedMatrix;

			GUI.color = savedColor;
		}


		private static void DrawLineWindows (Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
		{
			float m = (pointB - pointA).magnitude;
			if (Mathf.Approximately (m, 0f))
			{
				return;
			}

			Color savedColor = GUI.color;
	 		Matrix4x4 savedMatrix = GUI.matrix;

	 		if (antiAlias)
			{
				width *= 3;
			}

			float angle = Vector3.Angle (pointB - pointA, Vector2.right) * (pointA.y <= pointB.y ? 1 : -1);

			Vector3 dz = new Vector3(pointA.x, pointA.y, 0);
			GUI.color = color;
			GUI.matrix = translationMatrix(dz) * GUI.matrix;

			GUIUtility.ScaleAroundPivot(new Vector2(m, width), new Vector2(-0.5f, 0));
			GUI.matrix = translationMatrix(-dz) * GUI.matrix;
			GUIUtility.RotateAroundPivot(angle, new Vector2(0, 0));
			GUI.matrix = translationMatrix(dz + new Vector3(width / 2, -m / 2) * Mathf.Sin(angle * Mathf.Deg2Rad)) * GUI.matrix;
	 
			GUI.DrawTexture(new Rect(0, 0, 1, 1), !antiAlias ? lineTex : adLineTex);
			GUI.matrix = savedMatrix;
			GUI.color = savedColor;
		}


		/**
		 * <summary>Draws a line between two points.</summary>
		 * <param name = "pointA">The location of the first point</param>
		 * <param name = "pointB">The location of the second point</param>
		 * <param name = "color">The colour of the line</param>
		 * <param name = "width">The width of the line</param>
		 * <param name = "antiAlias">True if the line should be anti-aliased</param>
		 */
		public static void Draw (Vector2 pointA, Vector2 pointB, Color color, float width, bool antiAlias)
		{
			/*if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
			{
				#if UNITY_EDITOR
				if (Application.platform == RuntimePlatform.WindowsEditor && AdvGame.GetReferences () != null && AdvGame.GetReferences ().menuManager != null && AdvGame.GetReferences ().menuManager.doWindowsPreviewFix)
				{
					DrawLineMac (pointA, pointB, color, width, antiAlias);
					return;
				}
				#endif

				DrawLineWindows (pointA, pointB, color, width, antiAlias);
			}
			else
			{
				DrawLineMac (pointA, pointB, color, width, antiAlias);
			}*/

			DrawLineMac (pointA, pointB, color, width, antiAlias);
		}
		
		
		/**
		 * Draws a box around a rectangle.</summary>
		 * <param name = "rect">The Rect to draw around</param>
		 * <param name = "color">The colour of the line</param>
		 * <param name = "width">The width of the line</param>
		 * <param name = "antiAlias">True if the line should be anti-aliased</param>
		 * <param name = "offset">The distance between the line and the rectangle</param>
		 */
		public static void DrawBox (Rect rect, Color color, float width, bool antiAlias, int offset)
		{
			Draw (new Vector2 (rect.x, rect.y - offset), new Vector2 (rect.x + rect.width, rect.y - offset), color, width, false);
			Draw (new Vector2 (rect.x - offset, rect.y - 2*offset), new Vector2 (rect.x - offset, rect.y + rect.height + 2*offset), color, width, false);
			Draw (new Vector2 (rect.x + rect.width + offset, rect.y - 2*offset), new Vector2 (rect.x + rect.width + offset, rect.y + rect.height + 2*offset), color, width, false);
			Draw (new Vector2 (rect.x, rect.y + rect.height + offset), new Vector2 (rect.x + rect.width, rect.y + rect.height + offset), color, width, false);
		}


		private static Matrix4x4 translationMatrix(Vector3 v)
		{
			return Matrix4x4.TRS(v, Quaternion.identity, Vector3.one);
		}

	}

}