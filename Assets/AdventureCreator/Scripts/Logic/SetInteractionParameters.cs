/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SetInteractionParameters.cs"
 * 
 *	A component used to set all of an Interaction's parameters when run as the result of interacting with a Hotspot.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A component used to set all of an Interaction's parameters when run as the result of interacting with a Hotspot. */
	[AddComponentMenu("Adventure Creator/Hotspots/Set Interaction parameters")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_set_interaction_parameters.html")]
	public class SetInteractionParameters : SetParametersBase
	{

		#region Variables

		[SerializeField] protected Hotspot hotspot = null;
		[SerializeField] protected InteractionType interactionType = InteractionType.Use;
		protected enum InteractionType { Use, Examine, Inventory, UnhandledInventory };

		[SerializeField] protected int buttonIndex = 0;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnHotspotInteract += OnHotspotInteract;
		}


		protected void OnDisable ()
		{
			EventManager.OnHotspotInteract -= OnHotspotInteract;
		}

		#endregion


		#region CustomEvents

		protected void OnHotspotInteract (Hotspot hotspot, Button button)
		{
			if (this.hotspot == hotspot && button != null)
			{
				switch (interactionType)
				{
					case InteractionType.Use:
						if (hotspot.provideUseInteraction && hotspot.useButtons.Contains (button) && hotspot.useButtons.IndexOf (button) == buttonIndex)
						{
							ProcessButton (button);
						}
						break;

					case InteractionType.Examine:
						if (hotspot.provideLookInteraction && hotspot.lookButton == button)
						{
							ProcessButton (button);
						}
						break;

					case InteractionType.Inventory:
						if (hotspot.provideInvInteraction && hotspot.invButtons.Contains (button) && hotspot.invButtons.IndexOf (button) == buttonIndex)
						{
							ProcessButton (button);
						}
						break;

					case InteractionType.UnhandledInventory:
						if (hotspot.provideUnhandledInvInteraction && hotspot.unhandledInvButton == button)
						{
							ProcessButton (button);
						}
						break;
				}
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			if (hotspot == null)
			{
				hotspot = GetComponent <Hotspot>();
			}

			hotspot = (Hotspot) EditorGUILayout.ObjectField ("Hotspot:", hotspot, typeof (Hotspot), true);
			if (hotspot)
			{
				interactionType = (InteractionType) EditorGUILayout.EnumPopup ("Interaction type:", interactionType);

				if (interactionType == InteractionType.Use)
				{
					if (hotspot.provideUseInteraction && hotspot.useButtons != null && hotspot.useButtons.Count > 0)
					{
						string[] labelArray = new string[hotspot.useButtons.Count];
						for (int i=0; i<hotspot.useButtons.Count; i++)
						{
							string label = (KickStarter.cursorManager)
											? i.ToString () + ": " + KickStarter.cursorManager.GetLabelFromID (hotspot.useButtons[i].iconID, 0)
											: i.ToString ();
							labelArray[i] = label;
						}

						buttonIndex = EditorGUILayout.Popup ("Interaction:", buttonIndex, labelArray);
						if (buttonIndex < hotspot.useButtons.Count)
						{
							ShowParametersGUI (hotspot.useButtons[buttonIndex], labelArray[buttonIndex]);
						}
						return;
					}
				}
				else if (interactionType == InteractionType.Inventory)
				{
					if (hotspot.provideInvInteraction && hotspot.invButtons != null && hotspot.invButtons.Count > 0)
					{
						string[] labelArray = new string[hotspot.invButtons.Count];
						for (int i=0; i<hotspot.invButtons.Count; i++)
						{
							string label = (KickStarter.inventoryManager)
											? i.ToString () + ": " + KickStarter.inventoryManager.GetLabel (hotspot.invButtons[i].invID)
											: i.ToString ();
							labelArray[i] = label;
						}

						buttonIndex = EditorGUILayout.Popup ("Interaction:", buttonIndex, labelArray);
						ShowParametersGUI (hotspot.invButtons[buttonIndex], labelArray[buttonIndex]);
						return;
					}
				}
				else if (interactionType == InteractionType.Examine && hotspot.provideLookInteraction && hotspot.lookButton != null)
				{
					ShowParametersGUI (hotspot.lookButton, "Examine");
					return;
				}
				else if (interactionType == InteractionType.UnhandledInventory && hotspot.provideUnhandledInvInteraction && hotspot.unhandledInvButton != null)
				{
					ShowParametersGUI (hotspot.unhandledInvButton, "Unhandled inventory");
					return;
				}

				EditorGUILayout.HelpBox ("No interactions of type '" + interactionType.ToString () + " available!", MessageType.Warning);
			}
		}


		protected void ShowParametersGUI (Button button, string label)
		{
			if (hotspot.interactionSource == InteractionSource.InScene && button.interaction)
			{
				ShowParametersGUI (button.interaction);
			}
			else if (hotspot.interactionSource == InteractionSource.AssetFile && button.assetFile)
			{
				ShowParametersGUI (button.assetFile);
			}
			else
			{
				EditorGUILayout.HelpBox ("Cannot set parameters for Interaction '" + label + "', since it has no associated ActionList.", MessageType.Warning);
			}
		}


		protected void ShowParametersGUI (Interaction interaction)
		{
			if (interaction == null) return;

			if (interaction.source == ActionListSource.AssetFile && interaction.assetFile && interaction.assetFile.NumParameters > 0)
			{
				ShowActionListReference (interaction.assetFile);
				ShowParametersGUI (interaction.assetFile.DefaultParameters, interaction.syncParamValues);
			}
			else if (interaction.source == ActionListSource.InScene && interaction.NumParameters > 0)
			{
				ShowActionListReference (interaction);
				ShowParametersGUI (interaction.parameters, false);
			}
			else
			{
				EditorGUILayout.HelpBox ("No parameters defined for Interaction '" + interaction.gameObject.name + "'.", MessageType.Warning);
			}
		}


		protected void ShowParametersGUI (ActionListAsset actionListAsset)
		{
			if (actionListAsset == null) return;

			if (actionListAsset.NumParameters > 0)
			{
				ShowActionListReference (actionListAsset);
				ShowParametersGUI (actionListAsset.DefaultParameters, true);
			}
			else
			{
				EditorGUILayout.HelpBox ("No parameters defined for ActionList Assset '" + actionListAsset.name + "'.", MessageType.Warning);
			}
		}


		protected void ShowActionListReference (Interaction interaction)
		{
			if (interaction)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Interaction: " + interaction);
				if (GUILayout.Button ("Ping", GUILayout.Width (50f)))
				{
					EditorGUIUtility.PingObject (interaction);
				}
				EditorGUILayout.EndHorizontal ();
			}
		}


		protected void ShowActionListReference (ActionListAsset actionListAsset)
		{
			if (actionListAsset)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Asset file: " + actionListAsset);
				if (GUILayout.Button ("Ping", GUILayout.Width (50f)))
				{
					EditorGUIUtility.PingObject (actionListAsset);
				}
				EditorGUILayout.EndHorizontal ();
			}
		}

		#endif


		#region ProtectedFunctions

		protected void ProcessButton (AC.Button button)
		{
			if (hotspot.interactionSource == InteractionSource.AssetFile)
			{
				AssignParameterValues (button.assetFile);
			}
			else if (hotspot.interactionSource == InteractionSource.InScene)
			{
				AssignParameterValues (button.interaction);
			}
		}

		#endregion

	}

}