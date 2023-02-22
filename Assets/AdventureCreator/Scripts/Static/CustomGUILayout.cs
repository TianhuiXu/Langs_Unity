#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class CustomGUILayout
	{

		private static int LabelWidth
		{
			get
			{
				return ACEditorPrefs.EditorLabelWidth;
			}
		}


		public static void DrawUILine ()
		{
			DrawUILine (Color.grey);
		}


		public static void DrawUILine (Color colour)
		{
			int padding = 10;
			int thickness = 1;

			Rect r = EditorGUILayout.GetControlRect (GUILayout.Height (padding + thickness));
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 2;
			r.width += 6;

			r.x += padding;
			r.width -= padding * 2;

			EditorGUI.DrawRect (r, colour);
		}


		public static void BeginVertical ()
		{
			EditorGUILayout.BeginVertical (EditorGUIUtility.isProSkin ? CustomStyles.Toolbar : CustomStyles.thinBox);
		}


		public static void EndVertical ()
		{
			EditorGUILayout.EndVertical ();
			GUILayout.Space (4f);
		}


		public static void MultiLineLabelGUI (string title, string text)
		{
			if (string.IsNullOrEmpty (text)) return;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (title, GUILayout.MaxWidth (146f));
			GUIStyle style = new GUIStyle ();
			if (EditorGUIUtility.isProSkin)
			{
				style.normal.textColor = new Color (0.8f, 0.8f, 0.8f);
			}
			style.wordWrap = true;
			style.alignment = TextAnchor.MiddleLeft;
			EditorGUILayout.LabelField (text, style, GUILayout.MaxWidth (570f));
			EditorGUILayout.EndHorizontal ();
		}


		public static System.Enum EnumPopup (string label, System.Enum value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.EnumPopup (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static System.Enum EnumFlagsField (string label, System.Enum value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.EnumFlagsField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static void LabelField (string label, string api = "")
		{
			EditorGUILayout.LabelField (label);
			CreateMenu (api);
		}


		public static void LabelField (string label, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUILayout.LabelField (label, layoutOption);
			CreateMenu (api);
		}


		public static bool Toggle (string label, bool value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Toggle (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static bool Toggle (bool value, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Toggle (value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static bool Toggle (bool value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Toggle (value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static bool ToggleLeft (string label, bool value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.ToggleLeft (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}

		
		public static int IntField (string label, int value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.IntField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static int IntField (int value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.IntField (value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}
		
		
		public static int IntSlider (string label, int value, int min, int max, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.IntSlider (new GUIContent (label, tooltip), value, min, max);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}
		
		
		public static float FloatField (string label, float value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.FloatField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static float FloatField (float value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.FloatField (value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}
		
		
		public static float Slider (string label, float value, float min, float max, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Slider (new GUIContent (label, tooltip), value, min, max);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}
		

		public static string TextField (string value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.TextField (value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}

		
		public static string TextField (string label, string value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.TextField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static string DelayedTextField (string label, string value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.DelayedTextField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static string DelayedTextField (string value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.DelayedTextField (value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static string TextArea (string value, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.TextArea (value, EditorStyles.textArea, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static string TextArea (string label, string value, string api = "", string tooltip = "")
		{
			int labelWidth = LabelWidth;
			if (labelWidth <= 0) labelWidth = 150;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent (label, tooltip), GUILayout.Width (labelWidth));
			value = TextArea (value, GUILayout.MaxWidth (570f), api);
			EditorGUILayout.EndHorizontal();
			return value;
		}


		public static int Popup (string label, int value, string[] list, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			#if UNITY_2019_3_OR_NEWER
			value = EditorGUILayout.Popup (new GUIContent (label, tooltip), value, list);
			#else
			value = EditorGUILayout.Popup (label, value, list);
			#endif
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static int Popup (int value, string[] list, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Popup (value, list);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static Color ColorField (string label, Color value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.ColorField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (string label, Object value, bool allowSceneObjects, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.ObjectField (new GUIContent (label, tooltip), value, typeof (T), allowSceneObjects);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (Object value, bool allowSceneObjects, GUILayoutOption layoutOption, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.ObjectField (value, typeof (T), allowSceneObjects, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (Object value, bool allowSceneObjects, GUILayoutOption option1, GUILayoutOption option2, string api = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.ObjectField (value, typeof (T), allowSceneObjects, option1, option2);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return value;
		}


		public static Vector2 Vector2Field (string label, Vector2 value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Vector2Field (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return (value);
		}


		public static Vector2 Vector2Field (string label, Vector2 value, GUILayoutOption layoutOption, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Vector2Field (new GUIContent (label, tooltip), value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return (value);
		}


		public static Vector3 Vector3Field (string label, Vector3 value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Vector3Field (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return (value);
		}


		public static Vector3 Vector2Field (string label, Vector3 value, GUILayoutOption layoutOption, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.Vector3Field (new GUIContent (label, tooltip), value, layoutOption);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return (value);
		}
		

		public static AnimationCurve CurveField (string label, AnimationCurve value, string api = "", string tooltip = "")
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			value = EditorGUILayout.CurveField (new GUIContent (label, tooltip), value);
			EditorGUIUtility.labelWidth = 0;
			CreateMenu (api);
			return (value);
		}


		public static bool ToggleHeader (bool toggle, string label, bool spaceAbove = true)
		{
			if (spaceAbove)
			{
				EditorGUILayout.Space ();
			}
			if (GUILayout.Button (toggle ? label : "(+) " + label, CustomStyles.toggleHeader))
			{
				toggle = !toggle;
			}
			return toggle;
		}


		public static void TokenLabel (string token)
		{
			EditorGUIUtility.labelWidth = LabelWidth;
			EditorGUILayout.LabelField (new GUIContent ("Replacement token:", "Text that you can enter into Menu elements, speech text etc and have it be replaced by this variable's value."), new GUIContent (token));
			EditorGUIUtility.labelWidth = 0;
			CreateTokenMenu (token);
		}


		public static void HelpBox (string message, MessageType messageType)
		{
			EditorGUILayout.HelpBox (message, messageType);
		}


		public static void BeginHorizontal ()
		{
			EditorGUILayout.BeginHorizontal ();
		}


		public static void EndHorizontal ()
		{
			EditorGUILayout.EndHorizontal ();
		}


		private static void CreateMenu (string api)
		{
			if (!string.IsNullOrEmpty (api) && Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition))
			{
				GenericMenu menu = new GenericMenu ();
				menu.AddDisabledItem (new GUIContent (api));
				menu.AddItem (new GUIContent ("Copy script variable"), false, CustomCallback, api);
				menu.ShowAsContext ();
			}
		}


		private static void CreateTokenMenu (string token)
		{
			if (!string.IsNullOrEmpty (token) && Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition))
			{
				GenericMenu menu = new GenericMenu ();
				menu.AddItem (new GUIContent ("Copy token text"), false, CustomCallback, token);
				menu.ShowAsContext ();
			}
		}


		private static void CustomCallback (object obj)
		{
			if (obj != null)
			{
				TextEditor te = new TextEditor ();
				te.text = obj.ToString ();
				te.SelectAll ();
				te.Copy ();
			}
		}

	}


	public class CustomStyles
	{

		public static GUIStyle subHeader;
		public static GUIStyle toggleHeader;
		public static GUIStyle managerHeader;
		public static GUIStyle smallCentre;
		public static GUIStyle linkCentre;
		public static GUIStyle thinBox;
		public static GUIStyle disabledActionType;

		private static bool isInitialised;

		static CustomStyles ()
		{
			Init ();
		}


		private static void Init ()
		{
			if (isInitialised)
			{
				return;
			}

			subHeader = new GUIStyle (GUI.skin.label);
			subHeader.fontSize = 13;
			subHeader.margin.top = 10;
			subHeader.fixedHeight = 21;
			if (EditorGUIUtility.isProSkin) subHeader.normal.textColor = Color.white;

			toggleHeader = new GUIStyle (GUI.skin.label);
			toggleHeader.fontSize = 13;
			toggleHeader.margin.top = 0;
			toggleHeader.fixedHeight = 21;
			if (EditorGUIUtility.isProSkin) toggleHeader.normal.textColor = Color.white;

			managerHeader = new GUIStyle (GUI.skin.label);
			managerHeader.fontSize = 17;
			managerHeader.alignment = TextAnchor.UpperCenter;
			managerHeader.fixedHeight = 30;
			if (EditorGUIUtility.isProSkin) managerHeader.normal.textColor = Color.white;

			smallCentre = new GUIStyle (GUI.skin.label);
			smallCentre.richText = true;
			smallCentre.alignment = TextAnchor.MiddleCenter;

			linkCentre = new GUIStyle (GUI.skin.label);
			linkCentre.richText = true;
			linkCentre.alignment = TextAnchor.MiddleCenter;
			linkCentre.normal.textColor = (EditorGUIUtility.isProSkin) ? new Color (0.35f, 0.45f, 0.9f) : new Color (0.1f, 0.2f, 0.7f);

			thinBox = new GUIStyle (GUI.skin.box);
			thinBox.padding = new RectOffset(0, 0, 0, 0);

			disabledActionType = new GUIStyle (GUI.skin.label);
			disabledActionType.richText = true;
			disabledActionType.alignment = TextAnchor.MiddleCenter;
			disabledActionType.normal.textColor = (EditorGUIUtility.isProSkin) ? new Color (1f, 0.4f, 0.4f) : new Color (0.7f, 0f, 0f);

			isInitialised = true;
		}


		public static GUIStyle IconNodes
		{
			get
			{
				return GetCustomGUIStyle (13);
			}
		}


		public static GUIStyle IconSave
		{
			get
			{
				return GetCustomGUIStyle (14);
			}
		}


		public static GUIStyle IconCogNode
		{
			get
			{
				return GetCustomGUIStyle (0);
			}
		}


		public static GUIStyle IconCog
		{
			get
			{
				return GetCustomGUIStyle (25, 24);
			}
		}


		public static GUIStyle IconLock
		{
			get
			{
				return GetCustomGUIStyle (11);
			}
		}


		public static GUIStyle IconUnlock
		{
			get
			{
				return GetCustomGUIStyle (12);
			}
		}


		public static GUIStyle IconSocket
		{
			get
			{
				return GetCustomGUIStyle (10);
			}
		}


		public static GUIStyle IconMarquee
		{
			get
			{
				return GetCustomGUIStyle (9);
			}
		}


		public static GUIStyle LabelToolbar
		{
			get
			{
				return GetCustomGUIStyle (8);
			}
		}


		public static GUIStyle IconInsert
		{
			get
			{
				return GetCustomGUIStyle (7);
			}
		}


		public static GUIStyle IconDelete
		{
			get
			{
				return GetCustomGUIStyle (5);
			}
		}


		public static GUIStyle IconAutoArrange
		{
			get
			{
				return GetCustomGUIStyle (6);
			}
		}


		public static GUIStyle IconPlay
		{
			get
			{
				return GetCustomGUIStyle (4);
			}
		}


		public static GUIStyle IconStop
		{
			get
			{
				return GetCustomGUIStyle (29);
			}
		}


		public static GUIStyle IconCut
		{
			get
			{
				return GetCustomGUIStyle (3);
			}
		}


		public static GUIStyle IconCopy
		{
			get
			{
				return GetCustomGUIStyle (1);
			}
		}


		public static GUIStyle IconPaste
		{
			get
			{
				return GetCustomGUIStyle (2);
			}
		}


		public static GUIStyle NodeNormal
		{
			get
			{
				return GetCustomGUIStyle (19, "Window");
			}
		}


		public static GUIStyle NodeRunning
		{
			get
			{
				return GetCustomGUIStyle (21, 16);
			}
		}


		public static GUIStyle NodeSelected
		{
			get
			{
				return GetCustomGUIStyle (20, 15); 
			}
		}


		public static GUIStyle NodeBreakpoint
		{
			get
			{
				return GetCustomGUIStyle (22, 17); 
			}
		}


		public static GUIStyle NodeDisabled
		{
			get
			{
				return GetCustomGUIStyle (23, 18);
			}
		}


		public static GUIStyle Toolbar
		{
			get
			{
				return GetCustomGUIStyle (27, 26);
			}
		}


		public static GUIStyle ToolbarInverted
		{
			get
			{
				return GetCustomGUIStyle (26, 27);
			}
		}


		public static GUIStyle FolderIcon
		{
			get
			{

				return GetCustomGUIStyle (28);
			}
		}


		private static GUIStyle GetCustomGUIStyle (int index)
		{
			return GetCustomGUIStyle (index, index);
		}


		private readonly static GUIStyle notFoundStyle = new GUIStyle ();
		private static GUIStyle GetCustomGUIStyle (int normalIndex, int proIndex)
		{
			int index = EditorGUIUtility.isProSkin ? proIndex : normalIndex;
			if (Resource.NodeSkin && index >= 0 && index < Resource.NodeSkin.customStyles.Length)
			{
				return Resource.NodeSkin.customStyles[index];
			}
			return notFoundStyle;
		}


		private static GUIStyle GetCustomGUIStyle (int normalIndex, string proName)
		{
			if (EditorGUIUtility.isProSkin)
			{
				if (Resource.NodeSkin)
				{
					return Resource.NodeSkin.GetStyle (proName);
				}
				return notFoundStyle;
			}
			return GetCustomGUIStyle (normalIndex);
		}

	}

}

#endif