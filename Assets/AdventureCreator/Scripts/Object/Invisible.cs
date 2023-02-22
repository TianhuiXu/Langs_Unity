/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Invisible.cs"
 * 
 *	This script makes any gameObject it is attached to invisible.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script disables the Renderer component of any GameObject it is attached to, making it invisible.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_invisible.html")]
	public class Invisible : MonoBehaviour
	{

		#region Variables

		public bool affectOwnGameObject = true;
		public enum ChildrenToAffect { None, OnlyActive, All };
		public ChildrenToAffect childrenToAffect;

		#endregion


		#region UnityStandards
		
		protected void Awake ()
		{
			Renderer ownRenderer = GetComponent <Renderer>();
			Renderer[] allRenderers = new Renderer[1];
			allRenderers[0] = ownRenderer;

			if (childrenToAffect != ChildrenToAffect.None)
			{
				bool includeInactive = (childrenToAffect == ChildrenToAffect.All);
				allRenderers = GetComponentsInChildren <Renderer>(includeInactive);
			}

			foreach (Renderer _renderer in allRenderers)
			{
				if (_renderer && _renderer == ownRenderer && !affectOwnGameObject)
				{
					continue;
				}
				_renderer.enabled = false;
			}
		}

		#endregion

	}

}