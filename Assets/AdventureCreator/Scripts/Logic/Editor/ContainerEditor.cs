#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(Container))]
	public class ContainerEditor : Editor
	{

		private Container _target;
		private int itemNumber;
		private int sideItem;
		private InventoryManager inventoryManager;


		public void OnEnable ()
		{
			_target = (Container) target;
		}


		public override void OnInspectorGUI ()
		{
			if (_target == null)
			{
				OnEnable ();
				return;
			}

			inventoryManager = AdvGame.GetReferences ().inventoryManager;

			ShowCategoriesUI (_target);
			EditorGUILayout.Space ();

			EditorGUILayout.LabelField ("Stored Inventory items", EditorStyles.boldLabel);
			if (Application.isPlaying)
			{
				if (_target.InvCollection.InvInstances.Count > 0)
				{
					CustomGUILayout.BeginVertical ();
					foreach (InvInstance invInstance in _target.InvCollection.InvInstances)
					{
						if (!InvInstance.IsValid (invInstance)) continue;

						EditorGUILayout.BeginHorizontal ();

						if (invInstance.InvItem.canCarryMultiple)
						{
							EditorGUILayout.LabelField (invInstance.InvItem.label, EditorStyles.boldLabel, GUILayout.Width (235f));
							EditorGUILayout.LabelField ("Count: " + invInstance.Count.ToString ());
						}
						else
						{
							EditorGUILayout.LabelField (invInstance.InvItem.label, EditorStyles.boldLabel);
						}

						EditorGUILayout.EndHorizontal ();

						CustomGUILayout.DrawUILine ();
					}
					CustomGUILayout.EndVertical ();
				}
				else
				{
					EditorGUILayout.HelpBox ("This Container has no items", MessageType.Info);
				}
			}
			else
			{
				if (_target.items.Count > 0)
				{
					CustomGUILayout.BeginVertical ();
					for (int i=0; i<_target.items.Count; i++)
					{
						_target.items[i].ShowGUI (inventoryManager);

						if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							SideMenu (_target.items[i]);
						}

						EditorGUILayout.EndHorizontal ();

						if (_target.limitToCategory && _target.categoryIDs != null && _target.categoryIDs.Count > 0)
						{
							InvItem listedItem = inventoryManager.GetItem (_target.items[i].ItemID);
							if (listedItem != null && !_target.categoryIDs.Contains (listedItem.binID))
					 		{
								EditorGUILayout.HelpBox ("This item is not in the categories checked above and will not be displayed.", MessageType.Warning);
							}
						}

						CustomGUILayout.DrawUILine ();
					}
					CustomGUILayout.EndVertical ();
				}
				else
				{
					EditorGUILayout.HelpBox ("This Container has no items", MessageType.Info);
				}
			}

			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("New item to store:", GUILayout.MaxWidth (130f));
			bool allowEmptySlots = KickStarter.settingsManager && KickStarter.settingsManager.canReorderItems;
			itemNumber = EditorGUILayout.Popup (itemNumber, CreateItemList (allowEmptySlots));
			if (GUILayout.Button ("Add new item"))
			{
				if (allowEmptySlots)
				{
					if (itemNumber == 0)
					{
						_target.items.Add (new ContainerItem (-1, _target.items.ToArray ()));
					}
					else
					{
						_target.items.Add (new ContainerItem (CreateItemID (itemNumber-1), _target.items.ToArray ()));
					}
				}
				else
				{
					_target.items.Add (new ContainerItem (CreateItemID (itemNumber), _target.items.ToArray ()));
				}
			}
			EditorGUILayout.EndHorizontal ();

			if (_target.maxSlots > 0 && _target.items.Count > _target.maxSlots)
			{
				EditorGUILayout.HelpBox ("The Container is full! Excess slots will be discarded.", MessageType.Warning);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void ShowCategoriesUI (Container _target)
		{
			CustomGUILayout.BeginVertical ();

			_target.label = CustomGUILayout.TextField ("Label:", _target.label, string.Empty, "The Container's display name");
			if (_target.HasExistingTranslation (0))
			{
				EditorGUILayout.LabelField ("Speech Manager ID:", _target.labelLineID.ToString ());
			}

			_target.limitToCategory = CustomGUILayout.Toggle ("Limit by category?", _target.limitToCategory, "", "If True, only inventory items of a specific category will be displayed");
			if (_target.limitToCategory)
			{
				List<InvBin> bins = AdvGame.GetReferences ().inventoryManager.bins;

				if (bins == null || bins.Count == 0)
				{
					_target.categoryIDs.Clear ();
					EditorGUILayout.HelpBox ("No categories defined!", MessageType.Warning);
				}
				else
				{
					for (int i=0; i<bins.Count; i++)
					{
						bool include = (_target.categoryIDs.Contains (bins[i].id)) ? true : false;
						include = EditorGUILayout.ToggleLeft (" " + i.ToString () + ": " + bins[i].label, include);

						if (include)
						{
							if (!_target.categoryIDs.Contains (bins[i].id))
							{
								_target.categoryIDs.Add (bins[i].id);
							}
						}
						else
						{
							if (_target.categoryIDs.Contains (bins[i].id))
							{
								_target.categoryIDs.Remove (bins[i].id);
							}
						}
					}

					if (_target.categoryIDs.Count == 0)
					{
						EditorGUILayout.HelpBox ("At least one category must be checked for this to take effect.", MessageType.Info);
					}
				}
				EditorGUILayout.Space ();
			}

			bool limitItems = (_target.maxSlots > 0);
			limitItems = EditorGUILayout.Toggle ("Limit number of slots?", limitItems);
			if (limitItems)
			{
				if (_target.maxSlots == 0)
				{
					_target.maxSlots = 10;
				}

				_target.maxSlots = EditorGUILayout.DelayedIntField ("Max number of slots:", _target.maxSlots);
				if (_target.maxSlots > 0)
				{
					_target.swapIfFull = CustomGUILayout.Toggle ("Swap items when full?", _target.swapIfFull, string.Empty, "If True, then attempting to insert an item when full will result in it being swapped with the one already in the slot.");
				}
			}
			else
			{
				_target.maxSlots = 0;
			}
			CustomGUILayout.EndVertical ();
		}


		private void SideMenu (ContainerItem item)
		{
			GenericMenu menu = new GenericMenu ();
			sideItem = _target.items.IndexOf (item);
			
			if (_target.items.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideItem > 0 || sideItem < _target.items.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Move up"), false, Callback, "Move up");
			}
			if (sideItem < _target.items.Count-1)
			{
				menu.AddItem (new GUIContent ("Move down"), false, Callback, "Move down");
			}
			
			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			if (sideItem >= 0)
			{
				ContainerItem tempItem = _target.items[sideItem];
				
				switch (obj.ToString ())
				{
				case "Delete":
					Undo.RecordObject (_target, "Delete item");
					_target.items.RemoveAt (sideItem);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move item up");
					_target.items.RemoveAt (sideItem);
					_target.items.Insert (sideItem-1, tempItem);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move item down");
					_target.items.RemoveAt (sideItem);
					_target.items.Insert (sideItem+1, tempItem);
					break;
				}
			}
			
			sideItem = -1;
		}
		
		
		private string[] CreateItemList (bool includeEmpty)
		{
			List<string> itemList = new List<string>();
			
			if (includeEmpty)
			{
				itemList.Add ("(Empty slot)");
			}

			foreach (InvItem item in inventoryManager.items)
			{
				itemList.Add (item.label);
			}

			return itemList.ToArray ();
		}


		private int CreateItemID (int i)
		{
			return (inventoryManager.items[i].id);
		}

	}

}

#endif