/*
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"IVariableReferencer.cs"
 * 
 *	An interface used to aid the location of references to IVariableReferencer
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/** An interface used to aid the location of references to Variables */
	public interface IVariableReferencer
	{

		#if UNITY_EDITOR

		int GetNumVariableReferences (VariableLocation location, int variableID, Variables variables = null, int variablesConstantID = 0);
		int UpdateVariableReferences (VariableLocation location, int oldVariableID, int newVariableID, Variables variables = null, int variablesConstantID = 0);
		
		#endif

	}


	public interface IVariableReferencerAction
	{

		#if UNITY_EDITOR

		int GetNumVariableReferences (VariableLocation location, int variableID, List<ActionParameter> actionParameters, Variables variables = null, int variablesConstantID = 0);
		int UpdateVariableReferences (VariableLocation location, int oldVariableID, int newVariableID, List<ActionParameter> actionParameters, Variables variables = null, int variablesConstantID = 0);

		#endif

	}

}