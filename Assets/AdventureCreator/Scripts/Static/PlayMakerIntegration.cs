/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayMakerIntegration.cs"
 * 
 *	This script contains static functions for use
 *	in calling PlayMaker FSMs.
 *
 *	To allow for PlayMaker integration, the 'PlayMakerIsPresent'
 *	preprocessor must be defined.  This can be done from
 *	Edit -> Project Settings -> Player, and entering
 *	'PlayMakerIsPresent' into the Scripting Define Symbols text box
 *	for your game's build platform.
 * 
 */

using UnityEngine;

#if PlayMakerIsPresent
using HutongGames.PlayMaker;
#endif

namespace AC
{

	/**
	 * A class the contains a number of static functions to assist with PlayMaker integration.
	 * To use PlayMaker with Adventure Creator, the 'PlayMakerIsPresent' preprocessor must be defined.
	 */
	public class PlayMakerIntegration
	{

		/**
		 * <summary>Checks if the 'PlayMakerIsPresent' preprocessor has been defined.</summary>
		 * <returns>True if the 'PlayMakerIsPresent' preprocessor has been defined</returns>
		 */
		public static bool IsDefinePresent ()
		{
			#if PlayMakerIsPresent
			return true;
			#else
			return false;
			#endif
		}


		/**
		 * <summary>Checks if a GameObject has a PlayMakerFSM component.</summary>
		 * <param name = "gameObject">The GameObject to check</param>
		 * <returns>True if the GameObject has a PlayMakerFSM component</returns>
		 */
		public static bool HasFSM (GameObject gameObject)
		{
			#if PlayMakerIsPresent
			if (gameObject != null)
			{
				return (gameObject.GetComponent <PlayMakerFSM>() != null);
			}
			#endif
			return false;
		}


		/**
		 * <summary>Calls a PlayMaker event on a specific FSM.</summary>
		 * <param name = "linkedObject">The GameObject with the PlayMakerFSM component</param>
		 * <param name = "eventName">The name of the event to call</param>
		 * <param name = "fsmNme">The name of the FSM to call</param>
		 */
		public static void CallEvent (GameObject linkedObject, string eventName, string fsmName)
		{
			#if PlayMakerIsPresent
			PlayMakerFSM[] playMakerFsms = linkedObject.GetComponents<PlayMakerFSM>();
			foreach (PlayMakerFSM playMakerFSM in playMakerFsms)
			{
				if (playMakerFSM.FsmName == fsmName)
				{
					playMakerFSM.Fsm.Event (eventName);
				}
			}
			#endif
		}
		

		/**
		 * <summary>Calls a PlayMaker FSM event.</summary>
		 * <param name = "linkedObject">The GameObject with the PlayMakerFSM component</param>
		 * <param name = "eventName">The name of the event to call</param>
		 */
		public static void CallEvent (GameObject linkedObject, string eventName)
		{
			#if PlayMakerIsPresent
			if (linkedObject.GetComponent <PlayMakerFSM>())
			{
				PlayMakerFSM playMakerFSM = linkedObject.GetComponent <PlayMakerFSM>();
				playMakerFSM.Fsm.Event (eventName);
			}
			#endif
		}
		

		/**
		 * <summary>Gets the value of a PlayMaker integer.</summary>
		 * <param name = "_name">The name of the PlayMaker integer to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker integer</returns>
		 */
		public static int GetInt (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmInt fsmInt = playMakerFSM.Fsm.GetFsmInt (_name);
					if (fsmInt != null)
					{
						return fsmInt.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Integer with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmInt fsmInt = FsmVariables.GlobalVariables.FindFsmInt (_name);
				if (fsmInt != null)
				{
					return fsmInt.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Integer with the name '" + _name + "'");
			}
			#endif
			return 0;
		}
		

		/**
		 * <summary>Gets the value of a PlayMaker boolean.</summary>
		 * <param name = "_name">The name of the PlayMaker boolean to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker boolean</returns>
		 */
		public static bool GetBool (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmBool fsmBool = playMakerFSM.Fsm.GetFsmBool (_name);
					if (fsmBool != null)
					{
						return fsmBool.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Bool with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmBool fsmBool = FsmVariables.GlobalVariables.FindFsmBool (_name);
				if (fsmBool != null)
				{
					return fsmBool.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Boolean with the name '" + _name + "'");
			}
			#endif
			return false;
		}
		

		/**
		 * <summary>Gets the value of a PlayMaker string.</summary>
		 * <param name = "_name">The name of the PlayMaker string to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker string</returns>
		 */
		public static string GetString (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmString fsmString = playMakerFSM.Fsm.GetFsmString (_name);
					if (fsmString != null)
					{
						return fsmString.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker String with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmString fsmString = FsmVariables.GlobalVariables.FindFsmString (_name);
				if (fsmString != null)
				{
					return fsmString.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global String with the name '" + _name + "'");
			}
			#endif
			return string.Empty;
		}
		

		/**
		 * <summary>Gets the value of a PlayMaker float.</summary>
		 * <param name = "_name">The name of the PlayMaker float to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker float</returns>
		 */
		public static float GetFloat (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmFloat fsmFloat = playMakerFSM.Fsm.GetFsmFloat (_name);
					if (fsmFloat != null)
					{
						return fsmFloat.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Float with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmFloat fsmFloat = FsmVariables.GlobalVariables.FindFsmFloat (_name);
				if (fsmFloat != null)
				{
					return fsmFloat.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Float with the name '" + _name + "'");
			}
			#endif
			return 0f;
		}


		/**
		 * <summary>Gets the value of a PlayMaker Vector3.</summary>
		 * <param name = "_name">The name of the PlayMaker Vector3 to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker Vector3</returns>
		 */
		public static Vector3 GetVector3 (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmVector3 fsmVector3 = playMakerFSM.Fsm.GetFsmVector3 (_name);
					if (fsmVector3 != null)
					{
						return fsmVector3.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Vector3 with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmVector3 fsmVector3 = FsmVariables.GlobalVariables.FindFsmVector3 (_name);
				if (fsmVector3 != null)
				{
					return fsmVector3.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Vector3 with the name '" + _name + "'");
			}
			#endif
			return Vector3.zero;
		}


		/**
		 * <summary>Gets the value of a PlayMaker GameObject.</summary>
		 * <param name = "_name">The name of the PlayMaker GameObject to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker GameObject</returns>
		 */
		public static GameObject GetGameObject (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmGameObject fsmGameObject = playMakerFSM.Fsm.GetFsmGameObject (_name);
					if (fsmGameObject != null)
					{
						return fsmGameObject.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker GameObject with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmGameObject fsmGameObject = FsmVariables.GlobalVariables.FindFsmGameObject (_name);
				if (fsmGameObject != null)
				{
					return fsmGameObject.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global GameObject with the name '" + _name + "'");
			}
			#endif
			return null;
		}


		/**
		 * <summary>Gets the value of a PlayMaker Object.</summary>
		 * <param name = "_name">The name of the PlayMaker Object to search for</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 * <returns>The value of the PlayMaker Object</returns>
		 */
		public static Object GetObject (string _name, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmObject fsmObject = playMakerFSM.Fsm.GetFsmObject (_name);
					if (fsmObject != null)
					{
						return fsmObject.Value;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Object with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmObject fsmObject = FsmVariables.GlobalVariables.FindFsmObject (_name);
				if (fsmObject != null)
				{
					return fsmObject.Value;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Object with the name '" + _name + "'");
			}
			#endif
			return null;
		}
		

		/**
		 * <summary>Sets the value of a PlayMaker integer.</summary>
		 * <param name = "_name">The name of the PlayMaker integer to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker integer</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetInt (string _name, int _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmInt fsmInt = playMakerFSM.Fsm.GetFsmInt (_name);
					if (fsmInt != null)
					{
						fsmInt.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Integer with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmInt fsmInt = FsmVariables.GlobalVariables.FindFsmInt (_name);
				if (fsmInt != null)
				{
					fsmInt.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Integer with the name '" + _name + "'");
			}
			#endif
		}
		

		/**
		 * <summary>Sets the value of a PlayMaker booleam.</summary>
		 * <param name = "_name">The name of the PlayMaker booleam to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker boolean</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetBool (string _name, bool _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmBool fsmBool = playMakerFSM.Fsm.GetFsmBool (_name);
					if (fsmBool != null)
					{
						fsmBool.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Bool with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmBool fsmBool = FsmVariables.GlobalVariables.FindFsmBool (_name);
				if (fsmBool != null)
				{
					fsmBool.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Boolean with the name '" + _name + "'");
			}
			#endif
		}
		

		/**
		 * <summary>Sets the value of a PlayMaker string.</summary>
		 * <param name = "_name">The name of the PlayMaker string to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker string</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetString (string _name, string _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmString fsmString = playMakerFSM.Fsm.GetFsmString (_name);
					if (fsmString != null)
					{
						fsmString.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker String with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmString fsmString = FsmVariables.GlobalVariables.FindFsmString (_name);
				if (fsmString != null)
				{
					fsmString.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global String with the name '" + _name + "'");
			}
			#endif
		}
		

		/**
		 * <summary>Sets the value of a PlayMaker float.</summary>
		 * <param name = "_name">The name of the PlayMaker float to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker float</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetFloat (string _name, float _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmFloat fsmFloat = playMakerFSM.Fsm.GetFsmFloat (_name);
					if (fsmFloat != null)
					{
						fsmFloat.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Float with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmFloat fsmFloat = FsmVariables.GlobalVariables.FindFsmFloat (_name);
				if (fsmFloat != null)
				{
					fsmFloat.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Float with the name '" + _name + "'");
			}
			#endif
		}


		/**
		 * <summary>Sets the value of a PlayMaker Vector3.</summary>
		 * <param name = "_name">The name of the PlayMaker Vector3 to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker Vector3</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetVector3 (string _name, Vector3 _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmVector3 fsmVector3 = playMakerFSM.Fsm.GetFsmVector3 (_name);
					if (fsmVector3 != null)
					{
						fsmVector3.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker Vector3 with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmVector3 fsmVector3 = FsmVariables.GlobalVariables.FindFsmVector3 (_name);
				if (fsmVector3 != null)
				{
					fsmVector3.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global Vector3 with the name '" + _name + "'");
			}
			#endif
		}


		/**
		 * <summary>Sets the value of a PlayMaker GameObject.</summary>
		 * <param name = "_name">The name of the PlayMaker GameObject to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker GameObject</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetGameObject (string _name, GameObject _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmGameObject fsmGameObject = playMakerFSM.Fsm.GetFsmGameObject (_name);
					if (fsmGameObject != null)
					{
						fsmGameObject.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker GameObject with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmGameObject fsmGameObject = FsmVariables.GlobalVariables.FindFsmGameObject (_name);
				if (fsmGameObject != null)
				{
					fsmGameObject.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global GameObject with the name '" + _name + "'");
			}
			#endif
		}


		/**
		 * <summary>Sets the value of a PlayMaker Object.</summary>
		 * <param name = "_name">The name of the PlayMaker Object to update</param>
		 * <param name = "_val">The new value to assign the PlayMaker Object</param>
		 * <param name = "_variables">The Variables component attached to the FSM, if local. If null, the variable is assumed to be global</param>
		 */
		public static void SetObject (string _name, Object _val, Variables _variables)
		{
			#if PlayMakerIsPresent
			if (_variables != null)
			{
				PlayMakerFSM playMakerFSM = _variables.GetComponent <PlayMakerFSM>();
				if (playMakerFSM != null && playMakerFSM.Fsm != null)
				{
					FsmObject fsmObject = playMakerFSM.Fsm.GetFsmObject (_name);
					if (fsmObject != null)
					{
						fsmObject.Value = _val;
						return;
					}
				}
				ACDebug.LogWarning ("Cannot find Playmaker GameObject with the name '" + _name + "' on " + _variables.gameObject + ".", _variables);
			}
			else
			{
				FsmObject fsmObject = FsmVariables.GlobalVariables.FindFsmObject (_name);
				if (fsmObject != null)
				{
					fsmObject.Value = _val;
					return;
				}
				ACDebug.LogWarning ("Cannot find Playmaker global GameObject with the name '" + _name + "'");
			}
			#endif
		}
		
	}
	
}