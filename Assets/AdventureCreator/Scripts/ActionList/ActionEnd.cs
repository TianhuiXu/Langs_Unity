/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionEnd.cs"
 * 
 *	This is a container for "end" Action data.
 * 
 */

namespace AC
{

	/** A data container for the variables that determine what happens when an Action has completed running. */
	[System.Serializable]
	public class ActionEnd
	{

		/** What happens when the Action ends (Continue, Stop, Skip, RunCutscene) */
		public ResultAction resultAction;
		/** The index number of the Action to skip to, if resultAction = ResultAction.Skip */
		public int skipAction;
		/** The Action to skip to, if resultAction = ResultAction.Skip */
		public Action skipActionActual;
		/** The Cutscene to run, if resultAction = ResultAction.RunCutscene and the Action is in a scene-based ActionList */
		public Cutscene linkedCutscene;
		/** The ActionListAsset to run, if resultAction = ResultAction.RunCutscene and the Action is in an ActionListAsset file */
		public ActionListAsset linkedAsset;


		/* The default Constructor. */
		public ActionEnd (bool stopAfter = false)
		{
			resultAction = (stopAfter) ? ResultAction.Stop : ResultAction.Continue;
			skipAction = -1;
			skipActionActual = null;
			linkedCutscene = null;
			linkedAsset = null;
		}


		/**
		 * <summary>A Constructor that copies the values of another ActionEnd.</summary>
		 * <param name = "_actionEnd">The ActionEnd to copy from</param>
		 */
		public ActionEnd (ActionEnd _actionEnd)
		{
			resultAction = _actionEnd.resultAction;
			skipAction = _actionEnd.skipAction;
			skipActionActual = _actionEnd.skipActionActual;
			linkedCutscene = _actionEnd.linkedCutscene;
			linkedAsset = _actionEnd.linkedAsset;
		}


		/** A Constructor that sets skipAction explicitly. */
		public ActionEnd (int _skipAction)
		{
			resultAction = ResultAction.Continue;
			skipAction = _skipAction;
		}


		public ActionEnd (Action actionAfter)
		{
			resultAction = ResultAction.Skip;
			skipActionActual = actionAfter;
		}


		public ActionEnd (Cutscene cutsceneAfter)
		{
			resultAction = ResultAction.RunCutscene;
			linkedCutscene = cutsceneAfter;
		}


		public ActionEnd (ActionListAsset actionListAssetAfter)
		{
			resultAction = ResultAction.RunCutscene;
			linkedAsset = actionListAssetAfter;
		}

	}

}