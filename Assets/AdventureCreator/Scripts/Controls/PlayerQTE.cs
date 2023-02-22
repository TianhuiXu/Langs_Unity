/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerQTE.cs"
 * 
 *	This script handles the processing of quick-time events
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script handles the state, input and progress of Quick Time Events (QTEs).
	 * It should be attached to the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_q_t_e.html")]
	public class PlayerQTE : MonoBehaviour
	{

		#region Variables

		protected QTEState qteState = QTEState.None;
		protected QTEType qteType = QTEType.SingleKeypress;
		
		protected string inputName;
		protected Animator animator;
		protected bool wrongKeyFails;

		protected float holdDuration;
		protected float cooldownTime;
		protected int targetPresses;
		protected bool doCooldown;

		protected float progress;
		protected int numPresses;
		protected float startTime;
		protected float endTime;
		protected float lastPressTime;
		protected bool canMash;
		protected float axisThreshold;

		protected const string touchScreenTap = "TOUCHSCREENTAP";

		protected string verticalInputName;
		protected bool rotationIsClockwise;
		protected float targetRotations;
		protected float currentRotations;
		protected float maxRotation;
		protected Vector2 lastFrameRotationInput;

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets the current QTE state (None, Win, Lose, Running)</summary>
		 * <returns>The current QTE state (None, Win, Lose, Running)</returns>
		 */
		public QTEState GetState ()
		{
			return qteState;
		}


		/** Automatically wins the current QTE. */
		public void SkipQTE ()
		{
			endTime = 0f;
			qteState = QTEState.Win;
		}


		/** Automatically end the current QTE. */
		public void KillQTE ()
		{
			endTime = 0f;
			qteState = QTEState.None;
		}


		/**
		 * <summary>Begins a QTE that involves a single key being pressed to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be pressed to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartSinglePressQTE (string _inputName, float _duration, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (string.IsNullOrEmpty (_inputName) && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				_inputName = touchScreenTap;
			}

			if (string.IsNullOrEmpty (_inputName) || _duration <= 0f)
			{
				return;
			}

			Setup (QTEType.SingleKeypress, _inputName, _duration, _animator, _wrongKeyFails, 0f);
		}


		/**
		 * <summary>Begins a QTE that involves a single axis being pressed to win.</summary>
		 * <param name = "_inputName">The name of the input axis that must be pressed to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_axisThreshold">If positive, the value that the input must be greater than for it to register as succesful.  If positive, the input must be lower that this value.</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any axis other than _inputName will instantly fail the QTE</param>
		 */
		public void StartSingleAxisQTE (string _inputName, float _duration, float _axisThreshold, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (string.IsNullOrEmpty (_inputName) || _duration <= 0f)
			{
				return;
			}

			Setup (QTEType.SingleAxis, _inputName, _duration, _animator, _wrongKeyFails, _axisThreshold);
		}


		/**
		 * <summary>Begins a QTE that involves a single key being held down to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be held down to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_holdDuration">The duration, in seconds, that the key must be held down for</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states, and a "Held" trigger</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartHoldKeyQTE (string _inputName, float _duration, float _holdDuration, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (string.IsNullOrEmpty (_inputName) && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				_inputName = touchScreenTap;
			}

			if (string.IsNullOrEmpty (_inputName) || _duration <= 0f)
			{
				return;
			}

			if (_holdDuration > _duration)
			{
				_holdDuration = _duration;
			}

			holdDuration = _holdDuration;
			Setup (QTEType.HoldKey, _inputName, _duration, _animator, _wrongKeyFails, 0f);
		}


		/**
		 * <summary>Begins a QTE that involves a single key being pressed repeatedly to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be pressed repeatedly to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_targetPresses">The number of times that the key must be pressed to win</param>
		 * <param name = "_doCooldown">If True, then the number of registered key-presses will decrease over time</param>
		 * <param name = "_cooldownTime">The cooldown time, if _doCooldown = True</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Hit", "Win" and "Lose" states</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartButtonMashQTE (string _inputName, float _duration, int _targetPresses, bool _doCooldown, float _cooldownTime, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (string.IsNullOrEmpty (_inputName) && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				_inputName = touchScreenTap;
			}

			if (string.IsNullOrEmpty (_inputName) || _duration <= 0f)
			{
				return;
			}

			targetPresses = _targetPresses;
			doCooldown = _doCooldown;
			cooldownTime = _cooldownTime;

			Setup (QTEType.ButtonMash, _inputName, _duration, _animator, _wrongKeyFails, 0f);
		}


		/**
		 * <summary>Begins a QTE that involves rotating two input axes to win.</summary>
		 * <param name = "_horizontalInputName">The name of the horizontal input axis</param>
		 * <param name = "_verticalInputName">The name of the vertical input axis</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_targetRotations">How many revolutions the input must be rotated by</param>
		 * <param name = "_rotationIsClockwise">If true, input is required to be clockwise. Otherwise, it must be anti-clockwise</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states</param>
		 * <param name = "_wrongDirectionFails">If True, then rotating in the opposite direction will instantly fail the QTE</param>
		 */
		public void StartThumbstickRotationQTE (string _horizontalInputName, string _verticalInputName, float _duration, float _targetRotations, bool _rotationIsClockwise, Animator _animator = null, bool _wrongDirectionFails = false)
		{
			if (string.IsNullOrEmpty (_horizontalInputName) || string.IsNullOrEmpty (_verticalInputName) || _duration <= 0f || _targetRotations <= 0f)
			{
				return;
			}

			verticalInputName = _verticalInputName;
			rotationIsClockwise = _rotationIsClockwise;
			targetRotations = (rotationIsClockwise) ? _targetRotations : -_targetRotations;
			maxRotation = 0f;
			currentRotations = 0f;
			lastFrameRotationInput = Vector2.zero;

			Setup (QTEType.ThumbstickRotation, _horizontalInputName, _duration, _animator, _wrongDirectionFails, 0.2f);
		}


		/**
		 * <summary>Gets the time factor remaining in the current QTE, as a decimal.</summary>
		 * <returns>The time factor remaining in the current QTE, as a decimal</returns>
		 */
		public float GetRemainingTimeFactor ()
		{
			if (endTime <= 0f || Time.time <= startTime)
			{
				return 1f;
			}

			if (Time.time >= endTime)
			{
				return 0f;
			}
			return (1f - (Time.time - startTime) / (endTime - startTime));
		}


		/**
		 * <summary>Gets the progress made towards completing the current QTE, as a decimal.</summary>
		 * <returns>The progress made towards competing the current QTE, as a decimal</returns>
		 */
		public float GetProgress ()
		{
			switch (qteState)
			{
				case QTEState.Win:
					progress = 1f;
					break;

				case QTEState.Lose:
					progress = 1f;
					break;

				case QTEState.Running:
					{
						if (endTime > 0f)
						{
							switch (qteType)
							{
								case QTEType.HoldKey:
									progress = (lastPressTime > 0f) ? ((Time.time - lastPressTime) / holdDuration) : 0f;
									break;

								case QTEType.ButtonMash:
									progress = (float) numPresses / (float) targetPresses;
									break;

								case QTEType.ThumbstickRotation:
									progress = Mathf.Clamp01 (currentRotations / targetRotations);
									break;

								default:
									break;
							}
						}
					}
					break;

				default:
					progress = 0f;
					break;
			}

			return progress;
		}


		/**
		 * <summary>Checks if a QTE sequence is currently active.</summary>
		 * <returns>True if a QTE sequence is currently active.</returns>
		 */
		public bool QTEIsActive ()
		{
			if (endTime > 0f)
			{
				return true;
			}
			return false;
		}


		/**  Updates the current QTE. This is called every frame by StateHandler. */
		public void UpdateQTE ()
		{
			if (endTime <= 0f)
			{
				return;
			}

			if (Time.time > endTime)
			{
				Lose ();
				return;
			}

			switch (qteType)
			{
				case QTEType.SingleKeypress:
				{
					if (inputName == touchScreenTap)
					{
						if (Input.touchCount > 0f)
						{
							Win ();
							return;
						}
					}
					else
					{
						if (KickStarter.playerInput.InputGetButtonDown (inputName))
						{
							Win ();
							return;
						}
						else if (wrongKeyFails && KickStarter.playerInput.InputAnyKey () && KickStarter.playerInput.GetMouseState () == MouseState.Normal)
						{
							Lose ();
							return;
						}
					}
				}
				break;

				case QTEType.SingleAxis:
				{
					float axisValue = KickStarter.playerInput.InputGetAxis (inputName);

					if (axisThreshold > 0f && axisValue > axisThreshold)
					{
						Win ();
						return;
					}
					else if (axisThreshold < 0f && axisValue < axisThreshold)
					{
						Win ();
						return;
					}
					else if (wrongKeyFails)
					{
						if (axisThreshold > 0f && axisValue < -axisThreshold)
						{
							Lose ();
							return;
						}
						else if (axisThreshold < 0f && axisValue > -axisThreshold)
						{
							Lose ();
							return;
						}
					}
				}
				break;

				case QTEType.HoldKey:
				{
					if (inputName == touchScreenTap)
					{
						if (Input.touchCount > 0f)
						{
							if (lastPressTime <= 0f)
							{
								lastPressTime = Time.time;
							}
							else if (Time.time > lastPressTime + holdDuration)
							{
								Win ();
								return;
							}
						}
						else
						{
							lastPressTime = 0f;
						}
					}
					else
					{
						if (KickStarter.playerInput.InputGetButton (inputName))
						{
							if (lastPressTime <= 0f)
							{
								lastPressTime = Time.time;
							}
							else if (Time.time > lastPressTime + holdDuration)
							{
								Win ();
								return;
							}
						}
						else if (wrongKeyFails && Input.anyKey)
						{
							Lose ();
							return;
						}
						else
						{
							lastPressTime = 0f;
						}
					}

					if (animator)
					{
						if (lastPressTime <= 0f)
						{
							animator.SetBool ("Held", false);
						}
						else
						{
							animator.SetBool ("Held", true);
						}
					}
				}
				break;

				case QTEType.ButtonMash:
				{
					if (inputName == touchScreenTap)
					{
						if (Input.touchCount > 1)
						{
							if (canMash)
							{
								numPresses ++;
								lastPressTime = Time.time;
								if (animator)
								{
									animator.Play ("Hit", 0, 0f);
								}
								canMash = false;
							}
						}
						else
						{
							canMash = true;

							if (doCooldown)
							{
								if (lastPressTime > 0f && Time.time > lastPressTime + cooldownTime)
								{
									numPresses --;
									lastPressTime = Time.time;
								}
							}
						}
					}
					else
					{
						if (KickStarter.playerInput.InputGetButtonDown (inputName))
						{
							if (canMash)
							{
								numPresses ++;
								lastPressTime = Time.time;
								if (animator)
								{
									animator.Play ("Hit", 0, 0f);
								}
								canMash = false;
							}
						}
						else
						{
							canMash = true;

							if (doCooldown)
							{
								if (lastPressTime > 0f && Time.time > lastPressTime + cooldownTime)
								{
									numPresses --;
									lastPressTime = Time.time;
								}
							}
						}

						if (KickStarter.playerInput.InputGetButtonDown (inputName)) {}
						else if (wrongKeyFails && Input.anyKeyDown)
						{
							Lose ();
							return;
						}
					}
					
					if (numPresses < 0)
					{
						numPresses = 0;
					}
					
					if (numPresses >= targetPresses)
					{
						Win ();
						return;
					}
				}
				break;

				case QTEType.ThumbstickRotation:
				{
					Vector2 currentInput = new Vector2 (KickStarter.playerInput.InputGetAxis (inputName), KickStarter.playerInput.InputGetAxis (verticalInputName));
					if (currentInput.sqrMagnitude > axisThreshold)
					{	
						if (lastFrameRotationInput != Vector2.zero)
						{
							float frameAngleDiff = AdvGame.SignedAngle (currentInput, lastFrameRotationInput);
							if (frameAngleDiff > 180f)
							{
								frameAngleDiff -= 360f;
							}
							else if (frameAngleDiff < -180f)
							{
								frameAngleDiff += 360f;
							}

							currentRotations += frameAngleDiff / 360f;

							if (rotationIsClockwise)
							{
								maxRotation = Mathf.Max (maxRotation, currentRotations);

								if (currentRotations > targetRotations)
								{
									Win ();
									return;
								}
							}
							else
							{
								maxRotation = Mathf.Min (maxRotation, currentRotations);

								if (currentRotations < targetRotations)
								{
									Win ();
									return;
								}
							}

							if (wrongKeyFails)
							{
								if (rotationIsClockwise)
								{
									if (maxRotation - currentRotations > 0.15f)
									{
										Lose ();
										return;
									}
								}
								else
								{
									if (maxRotation - currentRotations < -0.15f)
									{
										Lose ();
										return;
									}
								}
							}


						}
						lastFrameRotationInput = currentInput;
					}
					else
					{
						currentRotations = 0f;
						lastFrameRotationInput = Vector2.zero;
					}
				}
					break;
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void Setup (QTEType _qteType, string _inputName, float _duration, Animator _animator, bool _wrongKeyFails, float _axisThreshold)
		{
			qteType = _qteType;
			qteState = QTEState.Running;

			progress = 0f;
			inputName = _inputName;
			animator = _animator;
			wrongKeyFails = _wrongKeyFails;
			numPresses = 0;
			startTime = Time.time;
			lastPressTime = 0f;
			endTime = Time.time + _duration;
			axisThreshold = _axisThreshold;

			KickStarter.eventManager.Call_OnQTEBegin (qteType, inputName, _duration);
		}


		protected virtual void Win ()
		{
			if (animator)
			{
				animator.Play ("Win");
			}
			qteState = QTEState.Win;
			endTime = 0f;

			KickStarter.eventManager.Call_OnQTEEnd (qteType, true);
		}


		protected virtual void Lose ()
		{
			qteState = QTEState.Lose;
			endTime = 0f;
			if (animator)
			{
				animator.Play ("Lose");
			}

			KickStarter.eventManager.Call_OnQTEEnd (qteType, false);
		}

		#endregion

	}

}