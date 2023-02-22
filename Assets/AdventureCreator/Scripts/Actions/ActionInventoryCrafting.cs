/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInventoryCrafting.cs"
 * 
 *	This action is used to perform crafting-related tasks.
 * 
 */

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionInventoryCrafting : Action
	{

		public enum ActionCraftingMethod { ClearRecipe, CreateRecipe };
		public ActionCraftingMethod craftingMethod;

		public bool specificElement;
		public string menuName;
		public int menuNameParameterID = -1;
		public string elementName;
		public int elementNameParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Crafting"; }}
		public override string Description { get { return "Either clears the current arrangement of crafting ingredients, or evaluates them to create an appropriate result (if this is not done automatically by the recipe itself)."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			menuName = AssignString (parameters, menuNameParameterID, menuName);
			elementName = AssignString (parameters, elementNameParameterID, elementName);
		}


		public override float Run ()
		{
			switch (craftingMethod)
			{
				case ActionCraftingMethod.ClearRecipe:
					if (specificElement)
					{
						KickStarter.runtimeInventory.RemoveRecipe (menuName, elementName);
					}
					else
					{
						KickStarter.runtimeInventory.RemoveRecipes ();
					}
					break;

				case ActionCraftingMethod.CreateRecipe:
					if (specificElement)
					{
						MenuElement element = PlayerMenus.GetElementWithName (menuName, elementName);
						if (element is MenuCrafting)
						{
							MenuCrafting crafting = (MenuCrafting) element;
							crafting.SetOutput ();
							break;
						}
					}
					else
					{
						PlayerMenus.CreateRecipe ();
					}
					break;

				default:
					break;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			craftingMethod = (ActionCraftingMethod) EditorGUILayout.EnumPopup ("Method:", craftingMethod);

			specificElement = EditorGUILayout.Toggle ("Specific element?", specificElement);
			if (specificElement)
			{
				menuNameParameterID = ChooseParameterGUI ("Menu name:", parameters, menuNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (menuNameParameterID < 0)
				{
					menuName = EditorGUILayout.TextField ("Menu name:", menuName);
				}

				switch (craftingMethod)
				{
					case ActionCraftingMethod.ClearRecipe:
						elementNameParameterID = ChooseParameterGUI ("'Ingredients' box name:", parameters, elementNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (elementNameParameterID < 0)
						{
							elementName = EditorGUILayout.TextField ("'Ingredients' box name:", elementName);
						}
						break;

					case ActionCraftingMethod.CreateRecipe:
						elementNameParameterID = ChooseParameterGUI ("'Output' box name:", parameters, elementNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (elementNameParameterID < 0)
						{
							elementName = EditorGUILayout.TextField ("'Output' box name:", elementName);
						}
						break;
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			switch (craftingMethod)
			{
				case ActionCraftingMethod.ClearRecipe:
					return "Clear recipe";
					
				case ActionCraftingMethod.CreateRecipe:
				default:
					return "Create recipe";
			}
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Crafting' Action</summary>
		 * <param name = "craftingMethod">The crafting method to perform</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCrafting CreateNew (ActionCraftingMethod craftingMethod)
		{
			ActionInventoryCrafting newAction = CreateNew<ActionInventoryCrafting> ();
			newAction.craftingMethod = craftingMethod;
			return newAction;
		}
	}

}