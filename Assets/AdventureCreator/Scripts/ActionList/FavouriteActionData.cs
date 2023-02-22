#if UNITY_EDITOR

using UnityEngine;

namespace AC
{

	[System.Serializable]
	public class FavouriteActionData
	{

		#region Variables

		[SerializeField] private string actionAsJson;
		[SerializeField] private string label;
		[SerializeField] private string className;
		[SerializeField] private int id;

		#endregion


		#region Constructors

		public FavouriteActionData (Action action, int _id)
		{
			Update (action);
			id = _id;
		}

		#endregion


		#region PublicFunctions

		public void Update (Action action)
		{
			actionAsJson = JsonUtility.ToJson (action);
			className = action.GetType ().ToString ();
			label = action.Category.ToString () + ": " + action.Title;

			if (!string.IsNullOrEmpty (className) && className.StartsWith ("AC."))
			{
				className = className.Substring (3);
			}
		}


		public Action Generate ()
		{
			if (string.IsNullOrEmpty (className) || string.IsNullOrEmpty (actionAsJson))
			{
				ACDebug.LogWarning ("Cannot create favourite Action.");
				return null;
			}

			Action newAction = Action.CreateNew (className);
			JsonUtility.FromJsonOverwrite (actionAsJson, newAction);
			newAction.ClearIDs ();
			return newAction;
		}

		#endregion


		#region GetSet

		public int ID
		{
			get
			{
				return id;
			}
		}


		public string Label
		{
			get
			{
				return label;
			}
		}

		#endregion

	}

}

#endif