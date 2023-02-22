namespace AC
{

	public class IngredientCollection
	{

		#region Variables

		public readonly string MenuName;
		public readonly string ElementName;
		public readonly InvCollection InvCollection;

		#endregion


		#region Constructors

		public IngredientCollection (string menuName, string elementName)
		{
			MenuName = menuName;
			ElementName = elementName;
			InvCollection = new InvCollection ();
		}

		#endregion


		#region PublicFunctions

		public bool Matches (string menuName, string elementName)
		{
			return MenuName == menuName && ElementName == elementName;
		}

		#endregion

	}

}