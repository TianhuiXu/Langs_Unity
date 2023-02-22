namespace AC
{

	/**
	 * A data container for custom tokens. MenuLabel elements and speech text will replace occurences of [token:ID] with the relevant
	 * token's replacementText string.  Tokens are created and stored within the RuntimeVariables script.
	 */
	public struct CustomToken
	{

		#region Variables

		/** The token's unique identifier */
		public int ID;
		/** The token's replacement text. */
		public string replacementText;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_ID">The token's unique identifier</param>
		 * <param name = "_replacementText">The token's replacement text.</param>
		 */
		public CustomToken (int _ID, string _replacementText)
		{
			ID = _ID;
			replacementText = _replacementText;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Converts the replacementText into a temporary one that has no colon or pipe characters, so that it is safe for saving.</summary>
		 * <returns>The converted replacementText that is safe for saving.</returns>
		 */
		public string GetSafeReplacementText ()
		{
			return AdvGame.PrepareStringForSaving (replacementText);
		}


		/**
		 * <summary>Assigns the replacementText from a safe-to-store string that was stored in save data.</summary>
		 * <param name = "safeText">The safe-to-store variant of replacementText that was stored in save data</param>
		 */
		public void SetSafeReplacementText (string safeText)
		{
			replacementText = AdvGame.PrepareStringForLoading (safeText);
		}

		#endregion

	}

}