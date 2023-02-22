/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCheck.cs"
 * 
 *	This is an intermediate class for "checking" Actions,
 *	that have TRUE and FALSE endings.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** An Action subclass that allows for two different outcomes based on a boolean check. */
	[System.Serializable]
	public class ActionCheck : Action
	{

		/** Deprecated */
		[SerializeField] private ResultAction resultActionTrue = ResultAction.Continue;
		/** Deprecated */
		[SerializeField] private int skipActionTrue = -1;
		/** Deprecated */
		[SerializeField] private AC.Action skipActionTrueActual = null;
		/** Deprecated */
		[SerializeField] private Cutscene linkedCutsceneTrue = null;
		/** Deprecated */
		[SerializeField] private ActionListAsset linkedAssetTrue = null;

		/** Deprecated */
		[SerializeField] private ResultAction resultActionFail = ResultAction.Stop;
		/** Deprecated */
		[SerializeField] private int skipActionFail = -1;
		/** Deprecated */
		[SerializeField] private AC.Action skipActionFailActual = null;
		/** Deprecated */
		[SerializeField] private Cutscene linkedCutsceneFail = null;
		/** Deprecated */
		[SerializeField] private ActionListAsset linkedAssetFail = null;


		public override int NumSockets { get { return 2; }}


		public override int GetNextOutputIndex ()
		{
			bool result = CheckCondition ();
			return (result) ? 0 : 1;
		}


		/**
		 * <summary>Works out which of the two outputs should be run when this Action is complete.</summary>
		 * <returns>If True, then resultActionTrue will be used - otherwise resultActionFalse will be used</returns>
		 */
		public virtual bool CheckCondition ()
		{
			return false;
		}


		public override void Upgrade ()
		{
			if (skipActionTrue != -99 && endings.Count == 0)
			{
				ActionEnd actionEndTrue = new ActionEnd ();
				actionEndTrue.resultAction = resultActionTrue;
				actionEndTrue.skipAction = skipActionTrue;
				actionEndTrue.skipActionActual = skipActionTrueActual;
				actionEndTrue.linkedCutscene = linkedCutsceneTrue;
				actionEndTrue.linkedAsset = linkedAssetTrue;
				endings.Add (actionEndTrue);

				ActionEnd actionEndFail = new ActionEnd ();
				actionEndFail.resultAction = resultActionFail;
				actionEndFail.skipAction = skipActionFail;
				actionEndFail.skipActionActual = skipActionFailActual;
				actionEndFail.linkedCutscene = linkedCutsceneFail;
				actionEndFail.linkedAsset = linkedAssetFail;
				endings.Add (actionEndFail);

				skipActionTrue = -99;
			}
		}


		#if UNITY_EDITOR

		protected override string GetSocketLabel (int index)
		{
			if (index == 0)
			{
				return "If condition is met:";
			}
			return "If condition is not met:";
		}

		#endif


		/**
		 * <summary>Update the Action's output sockets</summary>
		 * <param name = "actionEndOnPass">A data container for the 'Condition is met' output socket</param>
		 * <param name = "actionEndOnFail">A data container for the 'Condition is not met' output socket</param>
		 */
		public void SetOutputs (ActionEnd actionEndOnPass, ActionEnd actionEndOnFail)
		{
			endings = new List<ActionEnd> ();
			endings.Add (new ActionEnd (actionEndOnPass));
			endings.Add (new ActionEnd (actionEndOnFail));
		}

	}

}