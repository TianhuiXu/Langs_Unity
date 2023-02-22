/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberAnimator.cs"
 * 
 *	This script is attached to Animator components in the scene we wish to save the state of. (Unity 5-only)
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if AddressableIsPresent
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/** This script is attached to Animator components in the scene we wish to save the state of. */
	[RequireComponent (typeof (Animator))]
	[AddComponentMenu("Adventure Creator/Save system/Remember Animator")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_animator.html")]
	public class RememberAnimator : Remember
	{

		#region Variables

		[SerializeField] private bool saveController = false; 
		[SerializeField] private bool setDefaultParameterValues = false;
		[SerializeField] private List<DefaultAnimParameter> defaultAnimParameters = new List<DefaultAnimParameter>();

		private Animator _animator;

		#endregion


		#region UnityStandards

		protected override void Start ()
		{
			base.Start ();

			if (loadedData) return;

			if (GameIsPlaying () && setDefaultParameterValues && isActiveAndEnabled)
			{
				for (int i=0; i<Animator.parameters.Length; i++)
				{
					if (i < defaultAnimParameters.Count)
					{
						string parameterName = Animator.parameters[i].name;

						switch (Animator.parameters[i].type)
						{
							case AnimatorControllerParameterType.Bool:
								Animator.SetBool (parameterName, defaultAnimParameters[i].intValue == 1);
								break;

							case AnimatorControllerParameterType.Float:
								Animator.SetFloat (parameterName, defaultAnimParameters[i].floatValue);
								break;

							case AnimatorControllerParameterType.Int:
								Animator.SetInteger (parameterName, defaultAnimParameters[i].intValue);
								break;
						}
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		public override string SaveData ()
		{
			AnimatorData animatorData = new AnimatorData ();
			animatorData.objectID = constantID;
			animatorData.savePrevented = savePrevented;

			if (saveController && Animator && Animator.runtimeAnimatorController)
			{
				animatorData.controllerID = AssetLoader.GetAssetInstanceID (Animator.runtimeAnimatorController);
			}
			
			animatorData.parameterData = ParameterValuesToString (Animator.parameters).ToString ();
			animatorData.layerWeightData = LayerWeightsToString ().ToString ();
			animatorData.stateData = StatesToString ().ToString ();

			return Serializer.SaveScriptData <AnimatorData> (animatorData);
		}
		
		
		public override void LoadData (string stringData)
		{
			AnimatorData data = Serializer.LoadScriptData <AnimatorData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			loadedData = true;

			if (!string.IsNullOrEmpty (data.controllerID) && Animator && saveController)
			{
				#if AddressableIsPresent

				if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
				{
					StartCoroutine (LoadDataFromAddressable (data));
					return;
				}
				#endif

				RuntimeAnimatorController runtimeAnimatorController = AssetLoader.RetrieveAsset (Animator.runtimeAnimatorController, data.controllerID);
				if (runtimeAnimatorController)
				{
					_animator.runtimeAnimatorController = runtimeAnimatorController;
				}
			}

			StringToParameterValues (Animator.parameters, data.parameterData);
			StringToLayerWeights (data.layerWeightData);
			StringToStates (data.stateData);
		}

		#endregion


		#if AddressableIsPresent

		private IEnumerator LoadDataFromAddressable (AnimatorData data)
		{
			AsyncOperationHandle<RuntimeAnimatorController> handle = Addressables.LoadAssetAsync<RuntimeAnimatorController> (data.controllerID);
			yield return handle;
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				_animator.runtimeAnimatorController = handle.Result;
			}
			Addressables.Release (handle);

			StringToParameterValues (Animator.parameters, data.parameterData);
			StringToLayerWeights (data.layerWeightData);
			StringToStates (data.stateData);
		}

		#endif


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.BeginVertical ();

			saveController = EditorGUILayout.ToggleLeft ("Save change in Controller?", saveController);

			setDefaultParameterValues = EditorGUILayout.ToggleLeft ("Set default parameters?", setDefaultParameterValues);
			if (setDefaultParameterValues)
			{
				if (!UnityVersionHandler.IsPrefabEditing (gameObject) && !UnityVersionHandler.ObjectIsInActiveScene (gameObject))
				{
					EditorGUILayout.HelpBox ("To view/edit parameters, the GameObject must be active in the scene.", MessageType.Warning);
				}
				else if (Animator.parameters != null)
				{
					GUILayout.Box (string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));
					
					int numParameters = Animator.parameters.Length;
					if (numParameters < defaultAnimParameters.Count)
					{
						defaultAnimParameters.RemoveRange (numParameters, defaultAnimParameters.Count - numParameters);
					}
					else if (numParameters > defaultAnimParameters.Count)
					{
						if (numParameters > defaultAnimParameters.Capacity)
						{
							defaultAnimParameters.Capacity = numParameters;
						}
						for (int i=defaultAnimParameters.Count; i<numParameters; i++)
						{
							defaultAnimParameters.Add (new DefaultAnimParameter ());
						}
					}

					for (int i=0; i<Animator.parameters.Length; i++)
					{
						AnimatorControllerParameter parameter = Animator.parameters[i];
						switch (parameter.type)
						{
							case AnimatorControllerParameterType.Bool:
								bool boolValue = (defaultAnimParameters[i].intValue == 1);
								boolValue = EditorGUILayout.Toggle (parameter.name, boolValue);
								defaultAnimParameters[i] = new DefaultAnimParameter ((boolValue) ? 1 : 0);
								break;

							case AnimatorControllerParameterType.Float:
								float floatValue = EditorGUILayout.FloatField (parameter.name, defaultAnimParameters[i].floatValue);
								defaultAnimParameters[i] = new DefaultAnimParameter (floatValue);
								break;

							case AnimatorControllerParameterType.Int:
								int intValue = EditorGUILayout.IntField (parameter.name, defaultAnimParameters[i].intValue);
								defaultAnimParameters[i] = new DefaultAnimParameter (intValue);
								break;
						}
					}
				}
			}

			CustomGUILayout.EndVertical ();
		}

		#endif


		#region PrivateFunctions
		
		private StringBuilder ParameterValuesToString (AnimatorControllerParameter[] parameters)
		{
			StringBuilder stateString = new StringBuilder ();

			foreach (AnimatorControllerParameter parameter in parameters)
			{
				switch (parameter.type)
				{
					case AnimatorControllerParameterType.Bool:
						stateString.Append (Animator.GetBool (parameter.name) ? "1" : "0");
						break;

					case AnimatorControllerParameterType.Float:
						stateString.Append (Animator.GetFloat (parameter.name));
						break;

					case AnimatorControllerParameterType.Int:
						stateString.Append (Animator.GetInteger (parameter.name));
						break;

					default:
						stateString.Append ("0");
						break;
				}
				
				stateString.Append (SaveSystem.pipe);
			}
			
			return stateString;
		}


		private StringBuilder LayerWeightsToString ()
		{
			StringBuilder stateString = new StringBuilder ();

			if (Animator.layerCount > 1)
			{
				for (int i=1; i<Animator.layerCount; i++)
				{
					float weight = Animator.GetLayerWeight (i);
					stateString.Append (weight).Append (SaveSystem.pipe);
				}
			}

			return stateString;
		}


		private StringBuilder StatesToString ()
		{
			StringBuilder stateString = new StringBuilder ();

			for (int i=0; i<Animator.layerCount; i++)
			{
				if (Animator.IsInTransition (i))
				{
					ProcessState (ref stateString, Animator.GetNextAnimatorStateInfo (i));
				}
				else
				{
					ProcessState (ref stateString, Animator.GetCurrentAnimatorStateInfo (i));
				}

				stateString.Append (SaveSystem.pipe);
			}

			return stateString;
		}


		private void ProcessState (ref StringBuilder stateString, AnimatorStateInfo stateInfo)
		{
			int nameHash = stateInfo.shortNameHash;
			float timeAlong = stateInfo.normalizedTime;

			if (timeAlong > 1f)
			{
				if (stateInfo.loop)
				{
					timeAlong = (timeAlong % 1);
				}
				else
				{
					timeAlong = 1f;
				}
			}

			stateString.Append (nameHash).Append (",").Append (timeAlong);
		}
		
		
		private void StringToParameterValues (AnimatorControllerParameter[] parameters, string valuesString)
		{
			if (string.IsNullOrEmpty (valuesString))
			{
				return;
			}
			
			string[] valuesArray = valuesString.Split (SaveSystem.pipe[0]);
			
			for (int i=0; i<parameters.Length; i++)
			{
				if (i < valuesArray.Length && valuesArray[i].Length > 0)
				{
					string parameterName = parameters[i].name;

					switch (parameters[i].type)
					{
						case AnimatorControllerParameterType.Bool:
							Animator.SetBool (parameterName, (valuesArray[i] == "1") ? true : false);
							break;

						case AnimatorControllerParameterType.Float:
							float floatValue = 0f;
							if (float.TryParse (valuesArray[i], out floatValue))
							{
								Animator.SetFloat (parameterName, floatValue);
							}
							break;

						case AnimatorControllerParameterType.Int:
							int intValue = 0;
							if (int.TryParse (valuesArray[i], out intValue))
							{
								Animator.SetInteger (parameterName, intValue);
							}
							break;
					}
				}
			}
		}


		private void StringToLayerWeights (string valuesString)
		{
			if (string.IsNullOrEmpty (valuesString) || Animator.layerCount <= 1)
			{
				return;
			}

			string[] valuesArray = valuesString.Split (SaveSystem.pipe[0]);

			for (int i=1; i<Animator.layerCount; i++)
			{
				if (i < (valuesArray.Length+1) && valuesArray[i-1].Length > 0)
				{
					float weight = 1f;
					if (float.TryParse (valuesArray[i-1], out weight))
					{
						Animator.SetLayerWeight (i, weight);
					}
				}
			}
		}


		private void StringToStates (string valuesString)
		{
			if (string.IsNullOrEmpty (valuesString))
			{
				return;
			}
			
			string[] valuesArray = valuesString.Split (SaveSystem.pipe[0]);
			
			for (int i=0; i<Animator.layerCount; i++)
			{
				if (i < (valuesArray.Length) && valuesArray[i].Length > 0)
				{
					string[] stateInfoArray = valuesArray[i].Split (","[0]);

					if (stateInfoArray.Length >= 2)
					{
						int nameHash = 0;
						float timeAlong = 0f;

						if (int.TryParse (stateInfoArray[0], out nameHash))
						{
							if (float.TryParse (stateInfoArray[1], out timeAlong))
							{
								Animator.Play (nameHash, i, timeAlong);
							}
						}
					}
				}
			}
		}

		#endregion


		#region GetSet

		private Animator Animator
		{
			get
			{
				if (_animator == null || !Application.isPlaying)
				{
					_animator = GetComponent <Animator>();
				}
				return _animator;
			}
		}

		#endregion


		#region PrivateStructs

		[System.Serializable]
		private struct DefaultAnimParameter
		{

			public int intValue;
			public float floatValue;


			public DefaultAnimParameter (int _intValue)
			{
				intValue = _intValue;
				floatValue = 0f;
			}


			public DefaultAnimParameter (float _floatValue)
			{
				intValue = 0;
				floatValue = _floatValue;
			}

		}

		#endregion

	}


	/** A data container used by the RememberAnimator script. */
	[System.Serializable]
	public class AnimatorData : RememberData
	{

		/** The unique identified of the Animator Controller */
		public string controllerID;
		/** The values of the parameters, separated by a pipe (|) character. */
		public string parameterData;
		/** The weights of each layer, separated by a pipe (|) character. */
		public string layerWeightData;
		/** Data for each layer's animation state. */
		public string stateData;

		/** The default Constructor. */
		public AnimatorData () { }

	}

}
	