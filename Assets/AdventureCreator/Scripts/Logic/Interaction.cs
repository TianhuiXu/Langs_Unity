/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Interaction.cs"
 * 
 *	This ActionList is used by Hotspots and NPCs.
 *	Each instance of the script handles a particular interaction
 *	with an object, e.g. one for "use", another for "examine", etc.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * An ActionList that is run when a Hotspot is clicked on.
	 */
	[System.Serializable]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_interaction.html")]
	public class Interaction : ActionList
	{ }
	
}