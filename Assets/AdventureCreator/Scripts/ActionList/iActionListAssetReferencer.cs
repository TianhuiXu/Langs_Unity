/*
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"iActionListAssetReferencer.cs"
 * 
 *	An interface used to aid the location of references to ActionListAsset files
 * 
 */

namespace AC
{

	/** An interface used to aid the location of references to ActionListAsset files */
	public interface iActionListAssetReferencer
	{

		#if UNITY_EDITOR

		bool ReferencesAsset (ActionListAsset actionListAsset);

		#endif

	}

}