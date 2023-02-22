/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Timer.cs"
 * 
 *	A value that can be changed over time
 * 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using  UnityEditor;
#endif

namespace AC
{

	[Serializable]
	/** A value that can be changed over time */
	public class Timer
	{

		#region Variables

		[SerializeField] private int id;
		[SerializeField] private string label;
		[SerializeField] private bool linkToVariable;
		[SerializeField] private int variableID;
		[SerializeField] private float updateIncreaseAmount = 1f;
		[SerializeField] private float updateFrequency = 1f;
		[SerializeField] private float minValue = 0f;
		[SerializeField] private float maxValue = 10f;
		[SerializeField] private bool loops = false;
		[SerializeField] private ActionListAsset actionListAssetOnUpdate = null;
		[SerializeField] private ActionListAsset actionListAssetOnComplete = null;
		[SerializeField] private bool onlyRunDuringGameplay;

		private float ticker;
		private float unlinkedValue;
		private bool isOn;
		private GVar variable;

		#endregion


		#region Constructors

		/** The default Constructor. */
		public Timer ()
		{
			label = string.Empty;
			id = 0;
			linkToVariable = false;
			variableID = 0;
			updateIncreaseAmount = 1f;
			updateFrequency = 1f;
			minValue = 0f;
			maxValue = 10f;
			actionListAssetOnUpdate = null;
			actionListAssetOnComplete = null;
			onlyRunDuringGameplay = false;
			loops = false;
		}


		public Timer (int[] idArray)
		{
			label = string.Empty;
			id = 0;
			linkToVariable = false;
			variableID = 0;
			updateIncreaseAmount = 1f;
			updateFrequency = 1f;
			minValue = 0f;
			maxValue = 10f;
			actionListAssetOnUpdate = null;
			actionListAssetOnComplete = null;
			onlyRunDuringGameplay = false;
			loops = false;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
					id++;
			}
		}

		#endregion


		#region PublicFunctions

		/** Sets the enabled state to the default value. */
		public void SetDefaultState ()
		{
			isOn = false;
			variable = null;
		}


		/** Updates the Timer. This is called every frame by StateHandler. */
		public void Update ()
		{
			if (!isOn || updateIncreaseAmount == 0 || updateFrequency <= 0f || KickStarter.sceneChanger.IsLoading ())
			{
				return;
			}

			if (onlyRunDuringGameplay && !KickStarter.stateHandler.IsInGameplay ())
			{
				return;
			}

			if (maxValue <= minValue)
			{
				ACDebug.LogWarning ("Timer " + Label + " must have a 'Max value' greater than its 'Min value'");
				return;
			}

			ticker -= Time.deltaTime;
			if (ticker <= 0f)
			{
				ticker = updateFrequency;

				if (linkToVariable && Variable == null)
				{
					Debug.LogWarning ("Cannot find Global variable with ID = " + variableID + " for Timer " + Label);
					return;
				}

				Value += updateIncreaseAmount;
				if (updateIncreaseAmount > 0f)
				{
					if (Value >= maxValue)
					{
						if (loops)
						{
							if (Value == maxValue)
							{
								Value = maxValue;
							}
							else
							{
								Value = minValue;
							}
						}
						else
						{
							Value = maxValue;
							OnComplete ();
							return;
						}
					}
				}
				else
				{
					if (Value <= minValue)
					{
						if (loops)
						{
							if (Value == minValue)
							{
								Value = minValue;
							}
							else
							{
								Value = maxValue;
							}
						}
						else
						{
							Value = minValue;
							OnComplete ();
							return;
						}
					}
				}

				OnUpdate ();
			}
		}


		/** Starts the Timer */
		public void Start ()
		{
			Value = Mathf.Clamp (Value, minValue, maxValue);

			if (updateIncreaseAmount > 0f)
			{
				if (Value >= maxValue && !loops)
				{
					return;
				}
			}
			else
			{
				if (Value <= minValue && !loops)
				{
					return;
				}
			}

			if (isOn) return;

			isOn = true;
			if (updateIncreaseAmount > 0f)
			{
				Value = minValue;
			}
			else
			{
				Value = maxValue;
			}

			ticker = updateFrequency;

			if (actionListAssetOnUpdate)
			{
				actionListAssetOnUpdate.Interact ();
			}

			KickStarter.eventManager.Call_OnTimerStart (this);
		}


		/**
		 * <summary>Resumes the Timer</summary>
		 * <param name = "resetTicker">If True, the ticker in between value updates is reset</param>
		 */
		public void Resume (bool resetTicker)
		{
			if (isOn) return;

			isOn = true;

			Value = Mathf.Clamp (Value, minValue, maxValue);
			
			if (resetTicker)
			{
				ticker = updateFrequency;
			}

			KickStarter.eventManager.Call_OnTimerStart (this);
		}


		/** Stops the Timer */
		public void Stop ()
		{
			if (!isOn) return;
			isOn = false;
		}


		/**
		 * <summary>Creates a save string containing the enabled state of a list of timers</summary>
		 * <param name = "timers">The timers to save</param>
		 * <returns>The save string</returns>
		 */
		public static string CreateSaveData (List<Timer> timers)
		{
			if (timers != null && timers.Count > 0)
			{
				System.Text.StringBuilder dataString = new System.Text.StringBuilder ();
				
				foreach (Timer timer in timers)
				{
					if (timer != null)
					{
						dataString.Append (timer.ID.ToString ());
						dataString.Append (SaveSystem.colon);
						dataString.Append (timer.IsRunning ? "1" : "0");
						dataString.Append (SaveSystem.colon);
						dataString.Append (timer.Value);
						dataString.Append (SaveSystem.colon);
						dataString.Append (timer.ticker);
						dataString.Append (SaveSystem.pipe);
					}
				}
				dataString.Remove (dataString.Length-1, 1);
				return dataString.ToString ();
			}
			return string.Empty;
		}


		/**
		 * <summary>Restores the enabled states of the game's timers from a saved data string</summary>
		 * <param name = "dataString">The data string to load</param>
		 */
		public static void LoadSaveData (string dataString)
		{
			if (!string.IsNullOrEmpty (dataString) && KickStarter.variablesManager.timers != null && KickStarter.variablesManager.timers.Count > 0)
			{
				string[] dataArray = dataString.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in dataArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);
		
					int _enabled = 0;
					int.TryParse (chunkData[1], out _enabled);
					bool _isRunning = (_enabled == 1);

					float _value = 0f;
					float.TryParse (chunkData[2], out _value);

					float _ticker = 0f;
					float.TryParse (chunkData[3], out _ticker);
					
					foreach (Timer timer in KickStarter.variablesManager.timers)
					{
						if (timer.ID == _id)
						{
							timer.isOn = false;
							timer.ticker = _ticker;
							timer.Value = _value;
							Debug.Log ("Load value:" + _value);

							if (_isRunning)
							{
								timer.Start ();
							}
							else
							{
								timer.Stop ();
							}
						}
					}
				}
			}
		}


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Settings", EditorStyles.boldLabel);

			label = CustomGUILayout.TextField ("Label:", label);
			linkToVariable = CustomGUILayout.Toggle ("Link to Global Variable?", linkToVariable);

			GVar _variable = null;
			if (linkToVariable)
			{
				variableID = AdvGame.GlobalVariableGUI ("Global variable:", variableID, new VariableType[] { VariableType.Float, VariableType.Integer, VariableType.PopUp }, "The global variable whose value is linked to the timer");
				_variable = GlobalVariables.GetVariable (variableID);
			}

			EditorGUILayout.Space ();

			updateFrequency = CustomGUILayout.FloatField ("Tic duration (s):", updateFrequency);
			if (_variable == null || _variable.type == VariableType.Float)
			{
				updateIncreaseAmount = CustomGUILayout.FloatField ("Value increase per tic:", updateIncreaseAmount);
				minValue = CustomGUILayout.FloatField ("Min value:", minValue);
				maxValue = CustomGUILayout.FloatField ("Max value:", maxValue);
			}
			else
			{
				updateIncreaseAmount = CustomGUILayout.IntField ("Increase amount:", (int) updateIncreaseAmount);
				minValue = CustomGUILayout.IntField ("Min value:", (int) minValue);
				maxValue = CustomGUILayout.IntField ("Max value:", (int) maxValue);
			}

			loops = CustomGUILayout.Toggle ("Run on a loop?", loops);
			onlyRunDuringGameplay = CustomGUILayout.Toggle ("Only run during gameplay?", onlyRunDuringGameplay);

			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("ActionLists", EditorStyles.boldLabel);

			actionListAssetOnUpdate = (ActionListAsset) CustomGUILayout.ObjectField<ActionListAsset> ("ActionList on update:", actionListAssetOnUpdate, false);

			if (!loops)
			{
				actionListAssetOnComplete = (ActionListAsset) CustomGUILayout.ObjectField<ActionListAsset> ("ActionList on complete:", actionListAssetOnComplete, false);
			}

			CustomGUILayout.EndVertical ();

			if (Application.isPlaying)
			{
				EditorGUILayout.Space ();
				CustomGUILayout.HelpBox ("Current value: " + Value.ToString (), MessageType.Info);
			}
		}

		#endif

		#endregion


		#region PrivateFunctions

		private void OnUpdate ()
		{
			if (actionListAssetOnUpdate)
			{
				actionListAssetOnUpdate.Interact ();
			}

			KickStarter.eventManager.Call_OnTimerUpdate (this);
		}


		private void OnComplete ()
		{
			Stop ();

			if (actionListAssetOnComplete)
			{
				actionListAssetOnComplete.Interact ();
			}

			KickStarter.eventManager.Call_OnTimerComplete (this);
		}

		#endregion


		#region GetSet

		/** The Timer's unique ID */
		public int ID { get { return id; } }
		/** If True, the Timer is running */
		public bool IsRunning { get { return isOn; } }
		

		/** The Timer's progress, as a decimal */
		public float Progress
		{
			get
			{
				return Mathf.Clamp01 ((Value - minValue) / (maxValue - minValue));
			}
		}


		/** The Timer's current value */
		public float Value
		{
			get
			{
				if (!linkToVariable)
				{
					return unlinkedValue;
				}

				if (Variable != null)
				{
					if (Variable.type == VariableType.Float)
					{
						return variable.FloatValue;
					}
					return variable.IntegerValue;
				}
				return 0f;
			}
			private set
			{
				if (!linkToVariable)
				{
					unlinkedValue = value;
					return;
				}

				if (Variable != null)
				{
					if (Variable.type == VariableType.Float)
					{
						Variable.FloatValue = value;
					}
					else
					{
						Variable.IntegerValue = (int) value;
					}
					Variable.Upload ();
				}
			}
		}


		/** The Timer's linked Global Variable */
		public GVar Variable
		{ 
			get 
			{
				if (linkToVariable)
				{
					if (variable == null)
					{
						variable = GlobalVariables.GetVariable (variableID); 
					}
					return variable;
				}
				return null;
			}
		}


		/** The Timer's Editor label */
		public string Label
		{
			get
			{
				if (string.IsNullOrEmpty (label))
				{
					label = "(Untitled)";
				}
				return label;
			}
		}

		#endregion

	}

}