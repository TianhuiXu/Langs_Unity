/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionWaitMultiple.cs"
 * 
 *	This Action will only trigger its 'After running' command once all Actions that can run it have been run.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionWaitMultiple : Action
	{

		protected int triggersToWait;

		
		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Wait for preceding"; }}
		public override string Description { get { return "This Action will only trigger its 'After running' command once all Actions that can run it have been run."; }}


		public override float Run ()
		{
			triggersToWait --;
			return 0f;
		}


		public override int GetNextOutputIndex ()
		{
			if (triggersToWait > 0)
			{
				return -1;
			}

			triggersToWait = 100;
			return 0;
		}


		public override void Reset (ActionList actionList)
		{
			triggersToWait = 0;

			int ownIndex = actionList.actions.IndexOf (this);

			if (ownIndex == 0)
			{
				LogWarning ("This Action should not be first in an ActionList, as it will prevent others from running!");
				return;
			}

			for (int i=0; i<actionList.actions.Count; i++)
			{
				Action otherAction = actionList.actions[i];

				if (otherAction != this)
				{
					foreach (ActionEnd ending in otherAction.endings)
					{
						if ((ending.resultAction == ResultAction.Skip && ending.skipAction == ownIndex) ||
							(ending.resultAction == ResultAction.Continue && ownIndex == i+1))
						{
							triggersToWait ++;
							break;
						}
					}
				}
			}

			base.Reset (actionList);
		}

	}

}