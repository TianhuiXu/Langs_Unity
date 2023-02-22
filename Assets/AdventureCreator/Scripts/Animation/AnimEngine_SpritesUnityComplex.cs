/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AnimEngine_SpritesUnityComplex.cs"
 * 
 *	This script uses Unity's built-in 2D
 *	sprite engine for animation, only allows
 *  for much finer control over the FSM.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class AnimEngine_SpritesUnityComplex : AnimEngine
	{

		private bool enteredCorrectState;


		public override void Declare (AC.Char _character)
		{
			character = _character;
			turningStyle = TurningStyle.Linear;
			isSpriteBased = true;
			_character.frameFlipping = AC_2DFrameFlipping.None;
			updateHeadAlways = true;
			enteredCorrectState = false;
		}


		public override void CharSettingsGUI ()
		{
			#if UNITY_EDITOR
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Mecanim parameters:", EditorStyles.boldLabel);
			
			character.spriteChild = (Transform) CustomGUILayout.ObjectField <Transform> ("Sprite child:", character.spriteChild, true, "", "The sprite Transform, which should be a child GameObject");

			if (character.spriteChild && character.spriteChild.GetComponent <Animator>() == null)
			{
				character.customAnimator = (Animator) CustomGUILayout.ObjectField <Animator> ("Animator (if not on s.c.):", character.customAnimator, true, "", "The Animator component, which will be assigned automatically if not set manually.");
			}

			character.moveSpeedParameter = CustomGUILayout.TextField ("Move speed float:", character.moveSpeedParameter, "", "The name of the Animator float parameter set to the movement speed");
			character.turnParameter = CustomGUILayout.TextField ("Turn float:", character.turnParameter, "", "The name of the Animator float parameter set to the turning direction");

			if (character.spriteDirectionData.HasDirections ())
			{
				character.directionParameter = CustomGUILayout.TextField ("Direction integer:", character.directionParameter, "", "The name of the Animator integer parameter set to the sprite direction. This is set to 0 for down, 1 for left, 2 for right, 3 for up, 4 for down-left, 5 for down-right, 6 for up-left, and 7 for up-right");
			}
			character.angleParameter = CustomGUILayout.TextField ("Body angle float:", character.angleParameter, "", "The name of the Animator float parameter set to the facing angle");
			character.headYawParameter = CustomGUILayout.TextField ("Head angle float:", character.headYawParameter, "", "The name of the Animator float parameter set to the head yaw");

			if (!string.IsNullOrEmpty (character.angleParameter) || !string.IsNullOrEmpty (character.headYawParameter))
			{
				character.angleSnapping = (AngleSnapping) CustomGUILayout.EnumPopup ("Angle snapping:", character.angleSnapping, "", "The snapping method for the 'Body angle float' and 'Head angle float' parameters");
			}

			character.talkParameter = CustomGUILayout.TextField ("Talk bool:", character.talkParameter, "", "The name of the Animator bool parameter set to True while talking");

			if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager && AdvGame.GetReferences ().speechManager.lipSyncMode != LipSyncMode.Off)
			{
				if (AdvGame.GetReferences ().speechManager.lipSyncOutput == LipSyncOutput.PortraitAndGameObject)
				{
					character.phonemeParameter = CustomGUILayout.TextField ("Phoneme integer:", character.phonemeParameter, "", "The name of the Animator integer parameter set to the active lip-syncing phoneme index");
					character.phonemeNormalisedParameter = CustomGUILayout.TextField ("Normalised phoneme float:", character.phonemeNormalisedParameter, "", "The name of the Animator float parameter set to the active lip-syncing phoneme index, relative to the number of phonemes");
				}
				else if (AdvGame.GetReferences ().speechManager.lipSyncOutput == LipSyncOutput.GameObjectTexture)
				{
					if (character.GetComponent <LipSyncTexture>() == null)
					{
						EditorGUILayout.HelpBox ("Attach a LipSyncTexture script to allow texture lip-syncing.", MessageType.Info);
					}
				} 
			}

			if (character.useExpressions)
			{
				character.expressionParameter = CustomGUILayout.TextField ("Expression ID integer:", character.expressionParameter, "", "The name of the Animator integer parameter set to the active Expression ID number");
			}

			character.verticalMovementParameter = CustomGUILayout.TextField ("Vertical movement float:", character.verticalMovementParameter, "", "The name of the Animator float parameter set to the vertical movement speed");
			character.talkingAnimation = TalkingAnimation.Standard;

			character.spriteDirectionData.ShowGUI ();
			if (character.spriteDirectionData.HasDirections ())
			{
				EditorGUILayout.HelpBox ("The above field affects the 'Direction integer' parameter only.", MessageType.Info);
			}

			Animator charAnimator = character.GetAnimator ();
			if (charAnimator == null || !charAnimator.applyRootMotion)
			{
				character.antiGlideMode = EditorGUILayout.ToggleLeft ("Only move when sprite changes?", character.antiGlideMode);

				if (character.antiGlideMode)
				{
					if (character.GetComponent <Rigidbody2D>())
					{
						EditorGUILayout.HelpBox ("This feature will disable use of the Rigidbody2D component.", MessageType.Warning);
					}
					if (character.IsPlayer && AdvGame.GetReferences () != null && AdvGame.GetReferences ().settingsManager)
					{
						if (AdvGame.GetReferences ().settingsManager.movementMethod != MovementMethod.PointAndClick && AdvGame.GetReferences ().settingsManager.movementMethod != MovementMethod.None)
						{
							EditorGUILayout.HelpBox ("This feature will not work with collision - it is not recommended for " + AdvGame.GetReferences ().settingsManager.movementMethod.ToString () + " movement.", MessageType.Warning);
						}
					}
				}
			}

			character.doWallReduction = EditorGUILayout.BeginToggleGroup ("Slow movement near walls?", character.doWallReduction);
			character.wallLayer = EditorGUILayout.TextField ("Wall collider layer:", character.wallLayer);
			character.wallDistance = EditorGUILayout.Slider ("Collider distance:", character.wallDistance, 0f, 2f);
			character.wallReductionOnlyParameter = EditorGUILayout.Toggle ("Only affects Mecanim parameter?", character.wallReductionOnlyParameter);
			EditorGUILayout.EndToggleGroup ();

			if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
			{
				character.rotateSprite3D = (RotateSprite3D) EditorGUILayout.EnumPopup ("Rotate sprite to:", character.rotateSprite3D);
			}

			CustomGUILayout.EndVertical ();

			if (GUI.changed && character)
			{
				EditorUtility.SetDirty (character);
			}

			#endif
		}


		public override PlayerData SavePlayerData (PlayerData playerData, Player player)
		{
			playerData.playerWalkAnim = player.moveSpeedParameter;
			playerData.playerTalkAnim = player.talkParameter;
			playerData.playerRunAnim = player.turnParameter;

			return playerData;
		}


		public override void LoadPlayerData (PlayerData playerData, Player player)
		{
			player.moveSpeedParameter = playerData.playerWalkAnim;
			player.talkParameter = playerData.playerTalkAnim;
			player.turnParameter = playerData.playerRunAnim;
		}


		public override NPCData SaveNPCData (NPCData npcData, NPC npc)
		{
			npcData.walkAnim = npc.moveSpeedParameter;
			npcData.talkAnim = npc.talkParameter;
			npcData.runAnim = npc.turnParameter;

			return npcData;
		}


		public override void LoadNPCData (NPCData npcData, NPC npc)
		{
			npc.moveSpeedParameter = npcData.walkAnim;
			npc.talkParameter = npcData.talkAnim;
			npc.turnParameter = npcData.runAnim;;
		}

		
		public override void ActionCharAnimGUI (ActionCharAnim action, List<ActionParameter> parameters = null)
		{
			#if UNITY_EDITOR
			
			action.methodMecanim = (AnimMethodCharMecanim) EditorGUILayout.EnumPopup ("Method:", action.methodMecanim);
			
			if (action.methodMecanim == AnimMethodCharMecanim.ChangeParameterValue)
			{
				action.parameterNameID = Action.ChooseParameterGUI ("Parameter to affect:", parameters, action.parameterNameID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.parameterNameID < 0)
				{
					action.parameterName = EditorGUILayout.TextField ("Parameter to affect:", action.parameterName);
				}

				action.mecanimParameterType = (MecanimParameterType) EditorGUILayout.EnumPopup ("Parameter type:", action.mecanimParameterType);

				if (action.mecanimParameterType == MecanimParameterType.Bool)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Boolean);
					if (action.parameterValueParameterID < 0)
					{
						bool value = (action.parameterValue <= 0f) ? false : true;
						value = EditorGUILayout.Toggle ("Set as value:", value);
						action.parameterValue = (value) ? 1f : 0f;
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Int)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Integer);
					if (action.parameterValueParameterID < 0)
					{
						int value = (int) action.parameterValue;
						value = EditorGUILayout.IntField ("Set as value:", value);
						action.parameterValue = (float) value;
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Float)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Float);
					if (action.parameterValueParameterID < 0)
					{
						action.parameterValue = EditorGUILayout.FloatField ("Set as value:", action.parameterValue);
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Trigger)
				{
					bool value = (action.parameterValue <= 0f) ? false : true;
					value = EditorGUILayout.Toggle ("Ignore when skipping?", value);
					action.parameterValue = (value) ? 1f : 0f;
				}
			}
			
			else if (action.methodMecanim == AnimMethodCharMecanim.SetStandard)
			{
				action.mecanimCharParameter = (MecanimCharParameter) EditorGUILayout.EnumPopup ("Parameter to change:", action.mecanimCharParameter);
				action.parameterName = EditorGUILayout.TextField ("New parameter name:", action.parameterName);

				if (action.mecanimCharParameter == MecanimCharParameter.MoveSpeedFloat)
				{
					action.changeSound = EditorGUILayout.Toggle ("Change sound?", action.changeSound);
					if (action.changeSound)
					{
						action.standard = (AnimStandard) EditorGUILayout.EnumPopup ("Change:", action.standard);

						if (action.standard == AnimStandard.Walk || action.standard == AnimStandard.Run)
						{
							action.newSound = (AudioClip) EditorGUILayout.ObjectField ("New " + action.standard.ToString () + " sound:", action.newSound, typeof (AudioClip), false);
						}
						else
						{
							EditorGUILayout.HelpBox ("Only Walk and Run have a standard sounds.", MessageType.Info);
						}
					}
					action.changeSpeed = EditorGUILayout.Toggle ("Change speed?", action.changeSpeed);
					if (action.changeSpeed)
					{
						if (!action.changeSound)
						{
							action.standard = (AnimStandard) EditorGUILayout.EnumPopup ("Change:", action.standard);
						}

						if (action.standard == AnimStandard.Walk || action.standard == AnimStandard.Run)
						{
							action.newSpeed = EditorGUILayout.FloatField ("New " + action.standard.ToString () + " speed:", action.newSpeed);
						}
						else
						{
							EditorGUILayout.HelpBox ("Only Walk and Run have a standard sounds.", MessageType.Info);
						}
					}
				}
			}

			else if (action.methodMecanim == AnimMethodCharMecanim.PlayCustom)
			{
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip:", action.clip2D);
				}

				action.includeDirection = EditorGUILayout.Toggle ("Add directional suffix?", action.includeDirection);
				action.layerInt = EditorGUILayout.IntField ("Mecanim layer:", action.layerInt);
				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 1f);
				action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
			}
			
			#endif
		}


		public override void ActionCharAnimAssignValues (ActionCharAnim action, List<ActionParameter> parameters)
		{
			if (action.methodMecanim == AnimMethodCharMecanim.ChangeParameterValue)
			{
				switch (action.mecanimParameterType)
				{
					case MecanimParameterType.Bool:
						BoolValue boolValue = (action.parameterValue <= 0f) ? BoolValue.False : BoolValue.True;
						boolValue = action.AssignBoolean (parameters, action.parameterValueParameterID, boolValue);
						action.parameterValue = (boolValue == BoolValue.True) ? 1f : 0f;
						break;

					case MecanimParameterType.Int:
						action.parameterValue = (float) action.AssignInteger (parameters, action.parameterValueParameterID, (int) action.parameterValue);
						break;

					case MecanimParameterType.Float:
						action.parameterValue = action.AssignFloat (parameters, action.parameterValueParameterID, action.parameterValue);
						break;

					default:
						break;
				}
			}
		}
		
		
		public override float ActionCharAnimRun (ActionCharAnim action)
		{
			return ActionCharAnimProcess (action, false);
		}


		protected float ActionCharAnimProcess (ActionCharAnim action, bool isSkipping)
		{
			if (action.methodMecanim == AnimMethodCharMecanim.SetStandard)
			{
				if (!string.IsNullOrEmpty (action.parameterName))
				{
					if (action.mecanimCharParameter == MecanimCharParameter.MoveSpeedFloat)
					{
						character.moveSpeedParameter = action.parameterName;
					}
					else if (action.mecanimCharParameter == MecanimCharParameter.TalkBool)
					{
						character.talkParameter = action.parameterName;
					}
					else if (action.mecanimCharParameter == MecanimCharParameter.TurnFloat)
					{
						character.turnParameter = action.parameterName;
					}
				}

				if (action.mecanimCharParameter == MecanimCharParameter.MoveSpeedFloat)
				{
					if (action.changeSpeed)
					{
						if (action.standard == AnimStandard.Walk)
						{
							character.walkSpeedScale = action.newSpeed;
						}
						else if (action.standard == AnimStandard.Run)
						{
							character.runSpeedScale = action.newSpeed;
						}
					}

					if (action.changeSound)
					{
						if (action.standard == AnimStandard.Walk)
						{
							character.walkSound = action.newSound;
						}
						else if (action.standard == AnimStandard.Run)
						{
							character.runSound = action.newSound;
						}
					}
				}

				return 0f;
			}
			
			if (character.GetAnimator () == null)
			{
				return 0f;
			}
			
			if (!action.isRunning)
			{
				action.isRunning = true;
				if (action.methodMecanim == AnimMethodCharMecanim.ChangeParameterValue)
				{
					if (!string.IsNullOrEmpty (action.parameterName))
					{
						if (action.mecanimParameterType == MecanimParameterType.Float)
						{
							character.GetAnimator ().SetFloat (action.parameterName, action.parameterValue);
						}
						else if (action.mecanimParameterType == MecanimParameterType.Int)
						{
							character.GetAnimator ().SetInteger (action.parameterName, (int) action.parameterValue);
						}
						else if (action.mecanimParameterType == MecanimParameterType.Bool)
						{
							bool paramValue = false;
							if (action.parameterValue > 0f)
							{
								paramValue = true;
							}
							character.GetAnimator ().SetBool (action.parameterName, paramValue);
						}
						else if (action.mecanimParameterType == MecanimParameterType.Trigger)
						{
							if (!isSkipping || action.parameterValue < 1f)
							{
								character.GetAnimator ().SetTrigger (action.parameterName);
							}
						}
					}
				}
				else if (action.methodMecanim == AnimMethodCharMecanim.PlayCustom)
				{
					if (!string.IsNullOrEmpty (action.clip2D))
					{
						enteredCorrectState = false;
						character.GetAnimator ().CrossFade (action.clip2D, action.fadeTime, action.layerInt);
						
						if (action.willWait)
						{
							return action.defaultPauseTime;
						}
					}
				}
			}
			else
			{
				if (action.methodMecanim == AnimMethodCharMecanim.PlayCustom)
				{
					if (!enteredCorrectState)
					{
						if (character.GetAnimator ().GetCurrentAnimatorStateInfo (action.layerInt).shortNameHash == Animator.StringToHash (action.clip2D))
						{
							enteredCorrectState = true;
						}
						else
						{
							return action.defaultPauseTime;
						}
					}

					if (character.GetAnimator ().GetCurrentAnimatorStateInfo (action.layerInt).normalizedTime >= 1f ||
						character.GetAnimator ().GetCurrentAnimatorStateInfo (action.layerInt).shortNameHash != Animator.StringToHash (action.clip2D))
					{ 
						action.isRunning = false;
						return 0f;
					}

					return action.defaultPauseTime;
				}
			}
			
			return 0f;
		}
		
		
		public override void ActionCharAnimSkip (ActionCharAnim action)
		{
			ActionCharAnimProcess (action, true);
		}


		public override void ActionAnimGUI (ActionAnim action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR

			action.methodMecanim = (AnimMethodMecanim) EditorGUILayout.EnumPopup ("Method:", action.methodMecanim);
			
			if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue || action.methodMecanim == AnimMethodMecanim.PlayCustom)
			{
				action.parameterID = AC.Action.ChooseParameterGUI ("Animator:", parameters, action.parameterID, ParameterType.GameObject);
				if (action.parameterID >= 0)
				{
					action.constantID = 0;
					action.animator = null;
				}
				else
				{
					action.animator = (Animator) EditorGUILayout.ObjectField ("Animator:", action.animator, typeof (Animator), true);
					
					action.constantID = action.FieldToID <Animator> (action.animator, action.constantID);
					action.animator = action.IDToField <Animator> (action.animator, action.constantID, false);
				}
			}
			
			if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue)
			{
				action.parameterNameID = Action.ChooseParameterGUI ("Parameter to affect:", parameters, action.parameterNameID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.parameterNameID < 0)
				{
					action.parameterName = EditorGUILayout.TextField ("Parameter to affect:", action.parameterName);
				}

				action.mecanimParameterType = (MecanimParameterType) EditorGUILayout.EnumPopup ("Parameter type:", action.mecanimParameterType);

				if (action.mecanimParameterType == MecanimParameterType.Bool)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Boolean);
					if (action.parameterValueParameterID < 0)
					{
						bool value = (action.parameterValue <= 0f) ? false : true;
						value = EditorGUILayout.Toggle ("Set as value:", value);
						action.parameterValue = (value) ? 1f : 0f;
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Int)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Integer);
					if (action.parameterValueParameterID < 0)
					{
						int value = (int) action.parameterValue;
						value = EditorGUILayout.IntField ("Set as value:", value);
						action.parameterValue = (float) value;
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Float)
				{
					action.parameterValueParameterID = Action.ChooseParameterGUI ("Set as value:", parameters, action.parameterValueParameterID, ParameterType.Float);
					if (action.parameterValueParameterID < 0)
					{
						action.parameterValue = EditorGUILayout.FloatField ("Set as value:", action.parameterValue);
					}
				}
				else if (action.mecanimParameterType == MecanimParameterType.Trigger)
				{
					bool value = (action.parameterValue <= 0f) ? false : true;
					value = EditorGUILayout.Toggle ("Ignore when skipping?", value);
					action.parameterValue = (value) ? 1f : 0f;
				}
			}
			else if (action.methodMecanim == AnimMethodMecanim.PlayCustom)
			{
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip:", action.clip2D);
				}

				action.layerInt = EditorGUILayout.IntField ("Mecanim layer:", action.layerInt);
				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 2f);
				action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
			}
			else if (action.methodMecanim == AnimMethodMecanim.BlendShape)
			{
				EditorGUILayout.HelpBox ("This method is not compatible with Sprites Unity Complex.", MessageType.Info);
			}
			
			#endif
		}
		
		
		public override string ActionAnimLabel (ActionAnim action)
		{
			string label = "";
			
			if (action.animator)
			{
				label = action.animator.name;
				
				if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue && action.parameterName != "")
				{
					label += " - " + action.parameterName;
				}
			}
			
			return label;
		}
		

		public override void ActionAnimAssignValues (ActionAnim action, List<ActionParameter> parameters)
		{
			action.runtimeAnimator = action.AssignFile <Animator> (parameters, action.parameterID, action.constantID, action.animator);

			if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue)
			{
				switch (action.mecanimParameterType)
				{
					case MecanimParameterType.Bool:
						BoolValue boolValue = (action.parameterValue <= 0f) ? BoolValue.False : BoolValue.True;
						boolValue = action.AssignBoolean (parameters, action.parameterValueParameterID, boolValue);
						action.parameterValue = (boolValue == BoolValue.True) ? 1f : 0f;
						break;

					case MecanimParameterType.Int:
						action.parameterValue = (float) action.AssignInteger (parameters, action.parameterValueParameterID, (int) action.parameterValue);
						break;

					case MecanimParameterType.Float:
						action.parameterValue = action.AssignFloat (parameters, action.parameterValueParameterID, action.parameterValue);
						break;

					default:
						break;
				}
			}
		}

		
		public override float ActionAnimRun (ActionAnim action)
		{
			return ActionAnimProcess (action, false);
		}


		protected float ActionAnimProcess (ActionAnim action, bool isSkipping)
		{
			if (!action.isRunning)
			{
				if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue && action.runtimeAnimator && !string.IsNullOrEmpty (action.parameterName))
				{
					if (action.mecanimParameterType == MecanimParameterType.Float)
					{
						action.runtimeAnimator.SetFloat (action.parameterName, action.parameterValue);
					}
					else if (action.mecanimParameterType == MecanimParameterType.Int)
					{
						action.runtimeAnimator.SetInteger (action.parameterName, (int) action.parameterValue);
					}
					else if (action.mecanimParameterType == MecanimParameterType.Bool)
					{
						bool paramValue = false;
						if (action.parameterValue > 0f)
						{
							paramValue = true;
						}
						action.runtimeAnimator.SetBool (action.parameterName, paramValue);
					}
					else if (action.mecanimParameterType == MecanimParameterType.Trigger)
					{
						if (!isSkipping || action.parameterValue < 1f)
						{
							action.runtimeAnimator.SetTrigger (action.parameterName);
						}
					}
					
					return 0f;
				}
				
				else if (action.methodMecanim == AnimMethodMecanim.PlayCustom && action.runtimeAnimator)
				{
					if (!string.IsNullOrEmpty (action.clip2D))
					{
						if (isSkipping)
						{
							action.runtimeAnimator.CrossFade (action.clip2D, action.fadeTime, action.layerInt);
							enteredCorrectState = false;

							if (action.willWait)
							{
								action.isRunning = true;
								return action.defaultPauseTime;
							}
						}
						else
						{
							action.runtimeAnimator.CrossFade (action.clip2D, 0f, action.layerInt);
						}
					}
				}
			}
			else
			{
				if (!enteredCorrectState)
				{
					if (action.runtimeAnimator.GetCurrentAnimatorStateInfo (action.layerInt).shortNameHash == Animator.StringToHash (action.clip2D))
					{
						enteredCorrectState = true;
					}
					else
					{
						return action.defaultPauseTime;
					}
				}

				if (action.runtimeAnimator.GetCurrentAnimatorStateInfo (action.layerInt).normalizedTime >= 1f ||
					action.runtimeAnimator.GetCurrentAnimatorStateInfo (action.layerInt).shortNameHash != Animator.StringToHash (action.clip2D))
				{
					action.isRunning = false;
					return 0f;
				}

				return action.defaultPauseTime;
			}

			return 0f;
		}
		
		
		public override void ActionAnimSkip (ActionAnim action)
		{
			ActionAnimProcess (action, true);
		}


		public override void ActionCharRenderGUI (ActionCharRender action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR
			
			EditorGUILayout.Space ();
			action.renderLock_scale = (RenderLock) EditorGUILayout.EnumPopup ("Sprite scale:", action.renderLock_scale);
			if (action.renderLock_scale == RenderLock.Set)
			{
				action.scaleParameterID = Action.ChooseParameterGUI ("New scale (%):", parameters, action.scaleParameterID, ParameterType.Integer);
				if (action.scaleParameterID < 0)
				{
					action.scale = EditorGUILayout.IntField ("New scale (%):", action.scale);
				}
			}
			
			EditorGUILayout.Space ();
			action.renderLock_direction = (RenderLock) EditorGUILayout.EnumPopup ("Sprite direction:", action.renderLock_direction);
			if (action.renderLock_direction == RenderLock.Set)
			{
				action.directionParameterID = Action.ChooseParameterGUI ("New direction:", parameters, action.directionParameterID, ParameterType.Integer);
				if (action.directionParameterID < 0)
				{
					action.direction = (CharDirection) EditorGUILayout.EnumPopup ("New direction:", action.direction);
				}
			}

			EditorGUILayout.Space ();
			action.renderLock_sortingMap = (RenderLock) EditorGUILayout.EnumPopup ("Sorting Map:", action.renderLock_sortingMap);
			if (action.renderLock_sortingMap == RenderLock.Set)
			{
				action.sortingMapParameterID = Action.ChooseParameterGUI ("New Sorting Map:", parameters, action.sortingMapParameterID, ParameterType.GameObject);
				if (action.sortingMapParameterID >= 0)
				{
					action.sortingMapConstantID = 0;
					action.sortingMap = null;
				}
				else
				{
					action.sortingMap = (SortingMap) EditorGUILayout.ObjectField ("New Sorting Map:", action.sortingMap, typeof (SortingMap), true);
					
					action.sortingMapConstantID = action.FieldToID <SortingMap> (action.sortingMap, action.sortingMapConstantID);
					action.sortingMap = action.IDToField <SortingMap> (action.sortingMap, action.sortingMapConstantID, false);
				}
			}

			EditorGUILayout.Space ();
			action.setNewDirections = EditorGUILayout.Toggle ("Rebuid directions?", action.setNewDirections);
			if (action.setNewDirections)
			{
				action.spriteDirectionData.ShowGUI ();
			}
			
			#endif
		}
		
		
		public override float ActionCharRenderRun (ActionCharRender action)
		{
			if (action.renderLock_scale == RenderLock.Set)
			{
				character.lockScale = true;
				character.spriteScale = (float) action.scale / 100f;
			}
			else if (action.renderLock_scale == RenderLock.Release)
			{
				character.lockScale = false;
			}
			
			if (action.renderLock_direction == RenderLock.Set)
			{
				character.SetSpriteDirection (action.direction);
			}
			else if (action.renderLock_direction == RenderLock.Release)
			{
				character.lockDirection = false;
			}

			if (action.renderLock_sortingMap != RenderLock.NoChange && character.GetComponentInChildren <FollowSortingMap>())
			{
				FollowSortingMap[] followSortingMaps = character.GetComponentsInChildren <FollowSortingMap>();
				SortingMap sortingMap = (action.renderLock_sortingMap == RenderLock.Set) ? action.RuntimeSortingMap : KickStarter.sceneSettings.sortingMap;
				
				foreach (FollowSortingMap followSortingMap in followSortingMaps)
				{
					followSortingMap.SetSortingMap (sortingMap);
				}
			}

			if (action.setNewDirections)
			{
				character._spriteDirectionData = new SpriteDirectionData (action.spriteDirectionData);
			}

			return 0f;
		}


		public override void PlayIdle ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, character.GetMoveSpeed ());
			}

			AnimTalk (character.GetAnimator ());
			
			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, 0f);
			}

			SetDirection (character.GetAnimator ());
		}


		public override void PlayWalk ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, character.GetMoveSpeed ());
			}

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, 0f);
			}

			AnimTalk (character.GetAnimator ());
			SetDirection (character.GetAnimator ());
		}
		
		
		public override void PlayRun ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, character.GetMoveSpeed ());
			}

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, 0f);
			}

			AnimTalk (character.GetAnimator ());
			SetDirection (character.GetAnimator ());
		}
		
		
		public override void PlayTalk ()
		{
			PlayIdle ();
		}
		
		
		public override void PlayTurnLeft ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, -1f);
			}
			
			AnimTalk (character.GetAnimator ());
			
			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, 0f);
			}

			SetDirection (character.GetAnimator ());
		}
		
		
		public override void PlayTurnRight ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, 1f);
			}
			
			AnimTalk (character.GetAnimator ());
			
			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, 0f);
			}

			SetDirection (character.GetAnimator ());
		}


		public override void PlayVertical ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}
			
			if (!string.IsNullOrEmpty (character.verticalMovementParameter))
			{
				character.GetAnimator ().SetFloat (character.verticalMovementParameter, character.GetHeightChange ());
			}
		}


		public override void TurnHead (Vector2 angles)
		{
			if (!string.IsNullOrEmpty (character.headYawParameter))
			{
				float spinAngleOffset = angles.x * Mathf.Rad2Deg;
				float headAngle = character.GetSpriteAngle () + spinAngleOffset;

				if (headAngle > 360f)
				{
					headAngle -= 360f;
				}
				if (headAngle < 0f)
				{
					headAngle += 360f;
				}

				if (character.angleSnapping != AngleSnapping.None)
				{
					headAngle = character.FlattenSpriteAngle (headAngle, character.angleSnapping);
				}

				character.GetAnimator ().SetFloat (character.headYawParameter, headAngle);
			}
		}


		protected void AnimTalk (Animator animator)
		{
			if (!string.IsNullOrEmpty (character.talkParameter))
			{
				animator.SetBool (character.talkParameter, character.isTalking);
			}
			
			if (character.LipSyncGameObject ())
			{
				if (!string.IsNullOrEmpty (character.phonemeParameter))
				{
					animator.SetInteger (character.phonemeParameter, character.GetLipSyncFrame ());
				}
				if (!string.IsNullOrEmpty (character.phonemeNormalisedParameter))
				{
					animator.SetFloat (character.phonemeNormalisedParameter, character.GetLipSyncNormalised ());
				}
			}

			if (!string.IsNullOrEmpty (character.expressionParameter) && character.useExpressions)
			{
				animator.SetInteger (character.expressionParameter, character.GetExpressionID ());
			}
		}


		protected void SetDirection (Animator animator)
		{
			if (!string.IsNullOrEmpty (character.angleParameter))
			{
				animator.SetFloat (character.angleParameter, character.GetSpriteAngle ());
			}
			if (!string.IsNullOrEmpty (character.directionParameter) && character.spriteDirectionData.HasDirections ())
			{
				animator.SetInteger (character.directionParameter, character.GetSpriteDirectionInt ());
			}
		}


		#if UNITY_EDITOR

		public override bool RequiresRememberAnimator (ActionCharAnim action)
		{
			if (action.methodMecanim == AnimMethodCharMecanim.ChangeParameterValue ||
				action.methodMecanim == AnimMethodCharMecanim.PlayCustom)
			{
				return true;
			}
			return false;
		}


		public override bool RequiresRememberAnimator (ActionAnim action)
		{
			if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue ||
				action.methodMecanim == AnimMethodMecanim.PlayCustom)
			{
				return true;
			}
			return false;
		}


		public override void AddSaveScript (Action _action, GameObject _gameObject)
		{
			if (_gameObject && _gameObject.GetComponentInChildren <Animator>())
			{
				_action.AddSaveScript <RememberAnimator> (_gameObject.GetComponentInChildren <Animator>());
			}
		}
		
		#endif

	}

}