/*
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"IMenuReferencer.cs"
 * 
 *	An interface used to aid the location of references to Menus and Menu Elements
 * 
 */

namespace AC
{

	/** An interface used to aid the location of references to Menus and Menu Elements */
	public interface IMenuReferencer
	{

		#if UNITY_EDITOR

		int GetNumMenuReferences (string menuName, string elementName = "");

		#endif

	}

}