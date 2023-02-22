/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Variables.cs"
 * 
 *	This component allows Component variables to be linked to an Animator parameter.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** This component allows Component variables to be linked to an Animator parameter. */
	[AddComponentMenu ("Adventure Creator/Logic/Link Variable to Animator")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_link_variable_to_animator.html")]
	public class LinkVariableToAnimator : MonoBehaviour
	{

		#region Variables

		/** The name shared by the Component Variable and the Animator */
		public string sharedVariableName;
		/** The Variables component with the variable to link */
		public Variables variables;
		/** The Animator component with the parameter to link */
		public Animator _animator;
		
		[SerializeField] private LinkableVariableLocation variableLocation = LinkableVariableLocation.Component;
		private enum LinkableVariableLocation { Global=0, Component=2 };

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			if (variableLocation == LinkableVariableLocation.Component && variables == null)
			{
				variables = GetComponent <Variables>();
			}
			if (_animator == null)
			{
				_animator = GetComponent <Animator>();
			}

			if (variables == null)
			{
				ACDebug.LogWarning ("No Variables component found for Link Variable To Animator on " + gameObject, this);
				return;
			}

			if (_animator == null)
			{
				ACDebug.LogWarning ("No Animator component found for Link Variable To Animator on " + gameObject, this);
				return;
			}

			if (string.IsNullOrEmpty (sharedVariableName))
			{
				ACDebug.LogWarning ("No shared variable name set for Link Variable To Animator on " + gameObject, this);
				return;
			}

			GVar linkedVariable = null;
			switch (variableLocation)
			{
				case LinkableVariableLocation.Global:
					linkedVariable = GlobalVariables.GetVariable (sharedVariableName);
					break;

				case LinkableVariableLocation.Component:
					linkedVariable = variables.GetVariable (sharedVariableName);
					break;
			}
			
			if (linkedVariable != null)
			{
				if (linkedVariable.link != VarLink.CustomScript)
				{
					ACDebug.LogWarning ("The " + variableLocation + " variable '" + sharedVariableName + "' must have its 'Link to' field set to 'Custom Script' in order to link it to an Animator");
				}
			}
			else
			{
				ACDebug.LogWarning ("Variable '" + sharedVariableName + "' was not found for Link Variable To Animator on " + gameObject, this);
				return;
			}

			EventManager.OnDownloadVariable += OnDownload;
			EventManager.OnUploadVariable += OnUpload;
		}


		private void OnDisable ()
		{
			EventManager.OnDownloadVariable -= OnDownload;
			EventManager.OnUploadVariable -= OnUpload;
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			sharedVariableName = EditorGUILayout.DelayedTextField ("Shared Variable name:", sharedVariableName);

			variableLocation = (LinkableVariableLocation) EditorGUILayout.EnumPopup ("Variable location:", variableLocation);
			if (variableLocation == LinkableVariableLocation.Component)
			{
				variables = (Variables) EditorGUILayout.ObjectField ("Variables:", variables, typeof (Variables), true);
			}
			_animator = (Animator) EditorGUILayout.ObjectField ("Animator:", _animator, typeof (Animator), true);

			if (!string.IsNullOrEmpty (sharedVariableName))
			{
				if (variableLocation == LinkableVariableLocation.Global)
				{
					GVar linkedVariable = KickStarter.variablesManager.GetVariable (sharedVariableName);
					if (linkedVariable != null)
					{
						if (linkedVariable.link != VarLink.CustomScript)
						{
							EditorGUILayout.HelpBox ("The global variable '" + sharedVariableName + "' must have its 'Link to' field set to 'Custom Script' in order to link it to an Animator", MessageType.Warning);
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("Variable '" + sharedVariableName + "' was not found for Link Variable To Animator on " + gameObject, MessageType.Warning);
					}
				}
				else
				{
					Variables _variables = variables ? variables : GetComponent<Variables> ();
					if (_variables)
					{
						GVar linkedVariable = _variables.GetVariable (sharedVariableName);
						if (linkedVariable != null)
						{
							if (linkedVariable.link != VarLink.CustomScript)
							{
								EditorGUILayout.HelpBox ("The component variable '" + sharedVariableName + "' must have its 'Link to' field set to 'Custom Script' in order to link it to an Animator", MessageType.Warning);
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("Variable '" + sharedVariableName + "' was not found for Link Variable To Animator on " + gameObject, MessageType.Warning);
						}
					}
				}
			}
		}

		#endif


		#region PrivateFunctions

		private void OnDownload (GVar variable, Variables variables)
		{
			if (this.variables == variables && variable.label == sharedVariableName)
			{
				switch (variable.type)
				{
					case VariableType.Boolean:
						variable.BooleanValue = _animator.GetBool (sharedVariableName);
						break;

					case VariableType.Integer:
					case VariableType.PopUp:
						variable.IntegerValue = _animator.GetInteger (sharedVariableName);
						break;

					case VariableType.Float:
						variable.FloatValue = _animator.GetFloat (sharedVariableName);
						break;

					default:
						break;
				}
			}
		}


		private void OnUpload (GVar variable, Variables variables)
		{
			if (this.variables == variables && variable.label == sharedVariableName)
			{
				switch (variable.type)
				{
					case VariableType.Boolean:
						_animator.SetBool (sharedVariableName, variable.BooleanValue);
						break;

					case VariableType.Integer:
					case VariableType.PopUp:
						_animator.SetInteger (sharedVariableName, variable.IntegerValue);
						break;

					case VariableType.Float:
						_animator.SetFloat (sharedVariableName, variable.FloatValue);
						break;

					default:
						break;
				}
			}
		}

		#endregion

	}

}