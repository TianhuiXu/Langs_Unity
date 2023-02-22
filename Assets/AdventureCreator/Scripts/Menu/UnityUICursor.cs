/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"UnityUICursor.cs"
 * 
 *	This script allows the cursor to be rendered using a Unity UI canvas, allowing for advanced effects such as custom animation.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** This script allows the cursor to be rendered using a Unity UI canvas, allowing for advanced effects such as custom animation. */
	[AddComponentMenu ("Adventure Creator/UI/Unity UI cursor")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_unity_u_i_cursor.html")]
	[RequireComponent (typeof (Canvas))]
	[RequireComponent (typeof (CanvasScaler))]
	public class UnityUICursor : MonoBehaviour
	{

		#region Variables

		[Header ("UI components")]
		[SerializeField] private RawImage rawImageToControl = null;
		[SerializeField] private bool updateImageNativeSize = true;
		[SerializeField] private RectTransform rectTransformToPosition = null;
		private CanvasScaler rootCanvasScaler;
		#if TextMeshProIsPresent
		public TMPro.TextMeshProUGUI itemCountText;
		#else
		public Text itemCountText;
		#endif
		
		[Header ("Animation (Optional)")]
		[SerializeField] private Animator _animator = null;
		[SerializeField] private string cursorIDIntParameter = "CursorID";
		[SerializeField] private string inventoryIDIntParameter = "InventoryID";
		[SerializeField] private string cursorVisibleBoolParameter = "CursorIsVisible";
		[SerializeField] private string clickTriggerParameter = "Click";

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			EventManager.OnSetHardwareCursor += OnSetHardwareCursor;
			EventManager.OnInventoryDeselect += OnInventoryDeselect;
			GetComponent<Canvas> ().sortingOrder = 100;
			rootCanvasScaler = GetComponent<CanvasScaler> ();
		}


		private void OnDisable ()
		{
			EventManager.OnInventoryDeselect -= OnInventoryDeselect;
			EventManager.OnSetHardwareCursor -= OnSetHardwareCursor;
		}


		private void Update ()
		{
			if (_animator)
			{
				if (!string.IsNullOrEmpty (cursorIDIntParameter))
				{
					int cursorID = KickStarter.playerMenus.GetElementOverCursorID ();
					if (cursorID < 0) cursorID = KickStarter.playerCursor.GetSelectedCursorID ();
					_animator.SetInteger (cursorIDIntParameter, cursorID);
				}
				if (!string.IsNullOrEmpty (inventoryIDIntParameter)) _animator.SetInteger (inventoryIDIntParameter, (KickStarter.runtimeInventory.SelectedItem != null) ? KickStarter.runtimeInventory.SelectedItem.id : -1);

				if (Input.GetMouseButtonDown (0))
				{
					if (!string.IsNullOrEmpty (clickTriggerParameter)) _animator.SetTrigger (clickTriggerParameter);
				}
			}

			if (rectTransformToPosition)
			{
				Vector2 _position = KickStarter.playerInput.GetMousePosition ();

				float scalerOffset = 1f;
				if (rootCanvasScaler && rootCanvasScaler.enabled && rootCanvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
				{
					switch (rootCanvasScaler.screenMatchMode)
					{
						case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
							float match = rootCanvasScaler.matchWidthOrHeight;
							scalerOffset = (Screen.width / rootCanvasScaler.referenceResolution.x) * (1 - match) + (Screen.height / rootCanvasScaler.referenceResolution.y) * match;
							break;

						case CanvasScaler.ScreenMatchMode.Expand:
							scalerOffset = Mathf.Min (Screen.width / rootCanvasScaler.referenceResolution.x, Screen.height / rootCanvasScaler.referenceResolution.y);
							break;

						case CanvasScaler.ScreenMatchMode.Shrink:
							scalerOffset = Mathf.Max (Screen.width / rootCanvasScaler.referenceResolution.x, Screen.height / rootCanvasScaler.referenceResolution.y);
							break;
					}
				}

				rectTransformToPosition.localPosition = new Vector3 ((_position.x - (Screen.width / 2f)) / scalerOffset, (_position.y - (Screen.height / 2f)) / scalerOffset, rectTransformToPosition.localPosition.z);
			}

			if (itemCountText && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				itemCountText.text = KickStarter.runtimeInventory.SelectedInstance.GetInventoryDisplayCount ().ToString ();
			}
		}

		#endregion


		#region CustomEvents

		private void OnInventoryDeselect (InvItem invItem)
		{
			if (KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursor || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel)
			{
				if (KickStarter.cursorManager.cursorDisplay == CursorDisplay.Never)
				{
					OnSetHardwareCursor (null, Vector2.zero);
				}
			}
		}


		private void OnSetHardwareCursor (Texture2D texture, Vector2 clickOffset)
		{
			if (_animator && !string.IsNullOrEmpty (cursorVisibleBoolParameter))
			{
				_animator.SetBool (cursorVisibleBoolParameter, (texture != null));

				if (rawImageToControl && texture)
				{
					rawImageToControl.texture = texture;
					if (updateImageNativeSize)
					{
						rawImageToControl.SetNativeSize ();
					}
				}
			}
			else
			{
				if (rawImageToControl)
				{
					rawImageToControl.texture = texture;
					if (updateImageNativeSize)
					{
						rawImageToControl.SetNativeSize ();
					}
				}
			}

			if (rawImageToControl)
			{
				rawImageToControl.enabled = texture != null;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.BeginVertical ();
			CustomGUILayout.LabelField ("UI components");

			rawImageToControl = (RawImage) CustomGUILayout.ObjectField <RawImage> ("RawImage to control:", rawImageToControl, true, string.Empty, "The RawImage to update with the correct cursor graphic");
			if (rawImageToControl)
			{
				updateImageNativeSize = CustomGUILayout.Toggle ("Update Image Native Size?", updateImageNativeSize, string.Empty, "If True, the Raw Image's size will be adjusted to make it pixel-perfect.");
			}

			rectTransformToPosition = (RectTransform) CustomGUILayout.ObjectField <RectTransform> ("RectTransform to position:", rectTransformToPosition, true, string.Empty, "The RectTransform component to control as the cursor's intended position");

			_animator = (Animator) CustomGUILayout.ObjectField<Animator> ("Animator:", _animator, true, string.Empty, "An Animator that can optionally be updated");

			#if TextMeshProIsPresent
			itemCountText = (TMPro.TextMeshProUGUI) CustomGUILayout.ObjectField<TMPro.TextMeshProUGUI> ("Item count Text:", itemCountText, false, string.Empty, "A Text component to display the selected inventory item's Count text");
			#else
			itemCountText = (Text) CustomGUILayout.ObjectField<Text> ("Item count Text:", itemCountText, false, string.Empty, "A Text component to display the selected inventory item's Count text");
			#endif

			CustomGUILayout.EndVertical ();

			if (_animator)
			{
				EditorGUILayout.Space ();

				CustomGUILayout.BeginVertical ();
				CustomGUILayout.LabelField ("Animator parameters");

				cursorIDIntParameter = CustomGUILayout.TextField ("Cursor ID int:", cursorIDIntParameter, string.Empty, "An integer parameter that represents the current cursor ID (= -1 if the main cursor is active)");
				inventoryIDIntParameter = CustomGUILayout.TextField ("Inventory ID int:", inventoryIDIntParameter, string.Empty, "An integer parameter that represents the currently-selected inventory item's ID (= -1 if no item is selected)");
				cursorVisibleBoolParameter = CustomGUILayout.TextField ("Cursor visible bool:", cursorVisibleBoolParameter, string.Empty, "A bool parameter that represents the cursor's visibility state");
				clickTriggerParameter = CustomGUILayout.TextField ("Click trigger:", clickTriggerParameter, string.Empty, "A trigger parameter invokes whenever the cursor is clicked.");

				CustomGUILayout.EndVertical ();
			}

		}

		#endif

	}

}