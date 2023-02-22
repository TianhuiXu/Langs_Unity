#if UNITY_EDITOR

namespace AC
{

	public struct PopupSelectData
	{

		#region Variables

		public int ID;
		public string label;
		public int rootIndex;

		#endregion


		#region Constructors

		public PopupSelectData (int _ID, string _label, int _rootIndex)
		{
			ID = _ID;
			label = _label;
			rootIndex = _rootIndex;
		}

		#endregion


		#region GetSet

		public string EditorLabel
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