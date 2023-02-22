/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionContainerCheck.cs"
 * 
 *	This action checks to see if a particular inventory item
 *	is inside a container, and performs something accordingly.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionContainerCheck : ActionCheck
	{

		public int invParameterID = -1;
		public int invID;
		protected int invNumber;

		public bool useActive = false;
		public int parameterID = -1;
		public int constantID = 0;
		public Container container;
		protected Container runtimeContainer;

		public bool doCount;
		public int intValue = 1;
		public enum IntCondition { EqualTo, NotEqualTo, LessThan, MoreThan };
		public IntCondition intCondition;

		#if UNITY_EDITOR
		protected InventoryManager inventoryManager;
		#endif

		
		public override ActionCategory Category { get { return ActionCategory.Container; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries the contents of a Container for a stored Item, and reacts accordingly."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeContainer = AssignFile <Container> (parameters, parameterID, constantID, container);
			invID = AssignInvItemID (parameters, invParameterID, invID);

			if (useActive)
			{
				runtimeContainer = KickStarter.playerInput.activeContainer;
			}
		}

		
		public override bool CheckCondition ()
		{
			if (runtimeContainer == null)
			{
				return false;
			}

			int count = runtimeContainer.GetCount (invID);
			
			if (doCount)
			{
				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return (count == intValue);

					case IntCondition.NotEqualTo:
						return (count != intValue);

					case IntCondition.LessThan:
						return (count < intValue);

					case IntCondition.MoreThan:
						return (count > intValue);

					default:
						return false;
				}
			}
			
			return (count > 0);
		}
		

		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (AdvGame.GetReferences ().inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			if (inventoryManager)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				if (invParameterID == -1)
				{
					invNumber = -1;
				}
				
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem _item in inventoryManager.items)
					{
						labelList.Add (_item.label);
						// If an item has been removed, make sure selected variable is still valid
						if (_item.id == invID)
						{
							invNumber = i;
						}
						
						i++;
					}
					
					if (invNumber == -1)
					{
						// Wasn't found (item was possibly deleted), so revert to zero
						if (invID > 0) LogWarning ("Previously chosen item no longer exists!");
						
						invNumber = 0;
						invID = 0;
					}

					useActive = EditorGUILayout.Toggle ("Affect active container?", useActive);
					if (!useActive)
					{
						parameterID = Action.ChooseParameterGUI ("Container:", parameters, parameterID, ParameterType.GameObject);
						if (parameterID >= 0)
						{
							constantID = 0;
							container = null;
						}
						else
						{
							container = (Container) EditorGUILayout.ObjectField ("Container:", container, typeof (Container), true);

							constantID = FieldToID <Container> (container, constantID);
							container = IDToField <Container> (container, constantID, false);
						}

					}

					//
					invParameterID = Action.ChooseParameterGUI ("Item to check:", parameters, invParameterID, ParameterType.InventoryItem);
					if (invParameterID >= 0)
					{
						invNumber = Mathf.Min (invNumber, inventoryManager.items.Count-1);
						invID = -1;
					}
					else
					{
						invNumber = EditorGUILayout.Popup ("Item to check:", invNumber, labelList.ToArray());
						invID = inventoryManager.items[invNumber].id;
					}
					//

					if (inventoryManager.items[invNumber].canCarryMultiple)
					{
						doCount = EditorGUILayout.Toggle ("Query count?", doCount);
					
						if (doCount)
						{
							EditorGUILayout.BeginHorizontal ("");
								EditorGUILayout.LabelField ("Count is:", GUILayout.MaxWidth (70));
								intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);
								intValue = EditorGUILayout.IntField (intValue);
							
								if (intValue < 1)
								{
									intValue = 1;
								}
							EditorGUILayout.EndHorizontal ();
						}
					}
					else
					{
						doCount = false;
					}
				}

				else
				{
					EditorGUILayout.LabelField ("No inventory items exist!");
					invID = -1;
					invNumber = -1;
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <Container> (container, constantID, parameterID);
		}

		
		public override string SetLabel ()
		{
			if (inventoryManager == null)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			if (inventoryManager != null)
			{
				if (inventoryManager.items.Count > 0 && inventoryManager.items.Count > invNumber && invNumber > -1)
				{
					return inventoryManager.items[invNumber].label;
				}
			}
			
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!useActive && parameterID < 0)
			{
				if (container && container.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}

		#endif


		/**
		* <summary>Creates a new instance of the 'Containter: Check' Action</summary>
		* <param name = "container">The Container to search</param>
		* <param name = "itemID">The ID of the inventory item to check the presence of</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerCheck CreateNew (Container container, int itemID)
		{
			ActionContainerCheck newAction = CreateNew<ActionContainerCheck> ();
			newAction.container = container;
			newAction.TryAssignConstantID (newAction.container, ref newAction.constantID);
			newAction.invID = itemID;
			return newAction;
		}
		
	}

}