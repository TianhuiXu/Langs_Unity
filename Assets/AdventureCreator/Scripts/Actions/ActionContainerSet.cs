/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionContainerSet.cs"
 * 
 *	This action is used to add or remove items from a container,
 *	with items being defined in the Inventory Manager.
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
	public class ActionContainerSet : Action
	{
		
		public enum ContainerAction { Add, Remove, RemoveAll };
		public ContainerAction containerAction;

		public ContainerTransfer containerTransfer = ContainerTransfer.DoNotTransfer;

		public Container transferContainer;
		public int transferContainerConstantID = 0;
		public int transferContainerParameterID = -1;
		protected Container runtimeTransferContainer;

		public int invParameterID = -1;
		public int invID;
		protected int invNumber;

		public bool useActive = false;
		public int constantID = 0;
		public int parameterID = -1;
		public Container container;
		protected Container runtimeContainer;

		public bool setAmount = false;
		public int amountParameterID = -1;
		public int amount = 1;
		public bool transferToPlayer = false;
		public bool removeAllInstances = false;

		public override ActionCategory Category { get { return ActionCategory.Container; }}
		public override string Title { get { return "Add or remove"; }}
		public override string Description { get { return "Adds or removes Inventory items from a Container."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, invParameterID, invID);
			amount = AssignInteger (parameters, amountParameterID, amount);

			if (useActive)
			{
				runtimeContainer = KickStarter.playerInput.activeContainer;
			}
			else
			{
				runtimeContainer = AssignFile <Container> (parameters, parameterID, constantID, container);
			}

			runtimeTransferContainer = null;
			switch (containerAction)
			{
				case ContainerAction.Remove:
				case ContainerAction.RemoveAll:
					if (transferToPlayer)
					{
						containerTransfer = ContainerTransfer.TransferToPlayer;
						transferToPlayer = false;
					}
					if (containerTransfer == ContainerTransfer.TransferToOtherContainer)
					{
						runtimeTransferContainer = AssignFile<Container> (parameters, transferContainerParameterID, transferContainerConstantID, transferContainer);
					}
					break;

				default:
					break;
			}
		}

		
		public override float Run ()
		{
			if (runtimeContainer == null)
			{
				return 0f;
			}

			if (!setAmount)
			{
				amount = 1;
			}

			switch (containerAction)
			{
				case ContainerAction.Add:
					runtimeContainer.Add (invID, amount);
					break;

				case ContainerAction.Remove:
					Remove ();
					break;

				case ContainerAction.RemoveAll:
					RemoveAll ();
					break;

				default:
					break;
			}

			return 0f;
		}


		protected void Remove ()
		{
			switch (containerTransfer)
			{
				case ContainerTransfer.DoNotTransfer:
					if (removeAllInstances)
					{
						runtimeContainer.InvCollection.DeleteAllOfType (invID);
					}
					else
					{
						runtimeContainer.InvCollection.Delete (invID, amount);
					}
					break;

				case ContainerTransfer.TransferToPlayer:
					if (removeAllInstances)
					{
						KickStarter.runtimeInventory.PlayerInvCollection.Transfer (invID, runtimeContainer.InvCollection);
					}
					else
					{
						KickStarter.runtimeInventory.PlayerInvCollection.Transfer (invID, runtimeContainer.InvCollection, amount);
					}
					break;

				case ContainerTransfer.TransferToOtherContainer:
					if (runtimeTransferContainer)
					{
						if (removeAllInstances)
						{
							runtimeTransferContainer.InvCollection.Transfer (invID, runtimeContainer.InvCollection);
						}
						else
						{
							runtimeTransferContainer.InvCollection.Transfer (invID, runtimeContainer.InvCollection, amount);
						}
					}
					break;

				default:
					break;
			}
		}


		protected void RemoveAll ()
		{
			switch (containerTransfer)
			{
				case ContainerTransfer.DoNotTransfer:
					runtimeContainer.InvCollection.DeleteAll ();
					break;

				case ContainerTransfer.TransferToPlayer:
					KickStarter.runtimeInventory.PlayerInvCollection.TransferAll (runtimeContainer.InvCollection);
					break;

				case ContainerTransfer.TransferToOtherContainer:
					if (runtimeTransferContainer)
					{
						runtimeTransferContainer.InvCollection.TransferAll (runtimeContainer.InvCollection);
					}
					break;

				default:
					break;
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (KickStarter.inventoryManager == null)
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be defined to use this Action", MessageType.Warning);
				return;
			}

			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
				
			int i = 0;
			if (invParameterID == -1)
			{
				invNumber = -1;
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

			containerAction = (ContainerAction) EditorGUILayout.EnumPopup ("Method:", containerAction);

			if (transferToPlayer)
			{
				transferToPlayer = false;
				containerTransfer = ContainerTransfer.TransferToPlayer;
			}

			if (containerAction != ContainerAction.RemoveAll)
			{
				if (KickStarter.inventoryManager.items.Count == 0)
				{
					EditorGUILayout.LabelField ("No inventory items exist!");
					return;
				}

				foreach (InvItem _item in KickStarter.inventoryManager.items)
				{
					labelList.Add (_item.label);

					// If a item has been removed, make sure selected variable is still valid
					if (_item.id == invID)
					{
						invNumber = i;
					}

					i++;
				}

				if (invNumber == -1)
				{
					if (invID > 0) LogWarning ("Previously chosen item no longer exists!");
					invNumber = 0;
					invID = 0;
				}

				invParameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, invParameterID, ParameterType.InventoryItem);
				if (invParameterID >= 0)
				{
					invNumber = Mathf.Min (invNumber, KickStarter.inventoryManager.items.Count - 1);
					invID = -1;
				}
				else
				{
					invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray ());
					invID = KickStarter.inventoryManager.items[invNumber].id;
				}
			}

			if (containerAction != ContainerAction.Add)
			{
				containerTransfer = (ContainerTransfer) EditorGUILayout.EnumPopup ("Transfer:", containerTransfer);

				if (containerTransfer == ContainerTransfer.TransferToOtherContainer)
				{
					transferContainerParameterID = Action.ChooseParameterGUI ("To Container:", parameters, transferContainerParameterID, ParameterType.GameObject);
					if (transferContainerParameterID >= 0)
					{
						transferContainerConstantID = 0;
						transferContainer = null;
					}
					else
					{
						transferContainer = (Container) EditorGUILayout.ObjectField ("To Container:", transferContainer, typeof (Container), true);

						transferContainerConstantID = FieldToID<Container> (transferContainer, transferContainerConstantID);
						transferContainer = IDToField<Container> (transferContainer, transferContainerConstantID, false);
					}
				}
			}

			if (containerAction == ContainerAction.Remove)
			{
				removeAllInstances = EditorGUILayout.Toggle ("Remove all instances?", removeAllInstances);
			}

			if (containerAction != ContainerAction.RemoveAll && KickStarter.inventoryManager.items[invNumber].canCarryMultiple)
			{	
				if (containerAction == ContainerAction.Remove && removeAllInstances)
				{}
				else
				{
					setAmount = EditorGUILayout.Toggle ("Set amount?", setAmount);
					if (setAmount)
					{
						string _label = (containerAction == ContainerAction.Add) ? "Increase count by:" : "Reduce count by:";

						amountParameterID = Action.ChooseParameterGUI (_label, parameters, amountParameterID, ParameterType.Integer);
						if (amountParameterID < 0)
						{
							amount = EditorGUILayout.IntField (_label, amount);
						}
					}
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberContainer> (container);
			}
			AssignConstantID <Container> (container, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			string labelItem = string.Empty;

			if (KickStarter.inventoryManager)
			{
				if (KickStarter.inventoryManager.items.Count > 0)
				{
					if (invNumber > -1)
					{
						labelItem = " " + KickStarter.inventoryManager.items[invNumber].label;
					}
				}
			}
			
			switch (containerAction)
			{
				case ContainerAction.Add:
					return "Add" + labelItem;
					
				case ContainerAction.Remove:
					return "Remove" + labelItem;
					
				case ContainerAction.RemoveAll:
					return "Remove all";

				default:
					return string.Empty;
			}
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
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to add an inventory item to a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "itemIDToAdd">The ID of the inventory item to add to the Container</param>
		* <param name = "instancesToAdd">If multiple instances of the item can be held, the number to be added</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_Add (Container containerToModify, int itemIDToAdd, int instancesToAdd = 1)
		{
			ActionContainerSet newAction = CreateNew<ActionContainerSet> ();
			newAction.useActive = (containerToModify == null);
			newAction.containerAction = ContainerAction.Add;
			newAction.container = containerToModify;
			newAction.TryAssignConstantID (newAction.container, ref newAction.constantID);
			newAction.invID = itemIDToAdd;
			newAction.setAmount = true;
			newAction.amount = instancesToAdd;

			return newAction;
		}


		/**
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to remove an inventory item from a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "itemIDToRemove">The ID of the inventory item to remove from the Container</param>
		* <param name = "instancesToAdd">If multiple instances of the item can be held, the number to be from</param>
		* <param name = "transferToPlayer">If True, the current Player will receive the item</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_Remove (Container containerToModify, int itemIDToRemove, int instancesToRemove = 1, bool transferToPlayer = false)
		{
			ActionContainerSet newAction = CreateNew<ActionContainerSet> ();
			newAction.useActive = (containerToModify == null);
			newAction.containerAction = ContainerAction.Remove;
			newAction.container = containerToModify;
			newAction.TryAssignConstantID (newAction.container, ref newAction.constantID);
			newAction.invID = itemIDToRemove;
			newAction.setAmount = true;
			newAction.amount = instancesToRemove;
			newAction.containerTransfer = (transferToPlayer) ? ContainerTransfer.TransferToPlayer : ContainerTransfer.DoNotTransfer;

			return newAction;
		}


		/**
		* <summary>Creates a new instance of the 'Containter: Add or remove' Action, set to remove all inventory items in a Constainer</summary>
		* <param name = "containerToModify">The Container to modify</param>
		* <param name = "transferToPlayer">If True, the current Player will receive the items</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionContainerSet CreateNew_RemoveAll (Container containerToModify, bool transferToPlayer = false)
		{
			ActionContainerSet newAction = CreateNew<ActionContainerSet> ();
			newAction.useActive = (containerToModify == null);
			newAction.containerAction = ContainerAction.RemoveAll;
			newAction.container = containerToModify;
			newAction.TryAssignConstantID (newAction.container, ref newAction.constantID);
			newAction.containerTransfer = (transferToPlayer) ? ContainerTransfer.TransferToPlayer : ContainerTransfer.DoNotTransfer;

			return newAction;
		}

	}

}