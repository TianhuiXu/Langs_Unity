/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AnimEngine_Mecanim.cs"
 * 
 *	This script uses the Mecanim
 *	system for 3D animation.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class AnimEngine_Mecanim : AnimEngine
	{

		private bool enteredCorrectState;


		public override void Declare (AC.Char _character)
		{
			character = _character;
			turningStyle = TurningStyle.RootMotion;
			updateHeadAlways = (character && character.ikHeadTurning);
			enteredCorrectState = false;
		}


		public override void CharSettingsGUI ()
		{
			#if UNITY_EDITOR
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Mecanim parameters", EditorStyles.boldLabel);

			character.moveSpeedParameter = CustomGUILayout.TextField ("Move speed float:", character.moveSpeedParameter, "", "The name of the Animator float parameter set to the movement speed");
			character.turnParameter = CustomGUILayout.TextField ("Turn float:", character.turnParameter, "", "The name of the Animator float parameter set to the turning direction");
			character.talkParameter = CustomGUILayout.TextField ("Talk bool:", character.talkParameter, "", "The name of the Animator bool parameter set to True while talking");

			if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager &&
			    AdvGame.GetReferences ().speechManager.lipSyncMode != LipSyncMode.Off && AdvGame.GetReferences ().speechManager.lipSyncMode != LipSyncMode.FaceFX)
			{
				if (AdvGame.GetReferences ().speechManager.lipSyncOutput == LipSyncOutput.PortraitAndGameObject)
				{
					character.phonemeParameter = CustomGUILayout.TextField ("Phoneme integer:", character.phonemeParameter, "", "The name of the Animator integer parameter set to the active lip-syncing phoneme index");
					character.phonemeNormalisedParameter = CustomGUILayout.TextField ("Normalised phoneme float:", character.phonemeNormalisedParameter, "", "The name of the Animator float parameter set to the active lip-syncing phoneme index, relative to the number of phonemes");
					if (character.GetShapeable ())
					{
						character.lipSyncGroupID = ActionBlendShape.ShapeableGroupGUI ("Phoneme shape group:", character.GetShapeable ().shapeGroups, character.lipSyncGroupID);
						character.lipSyncBlendShapeSpeedFactor = CustomGUILayout.Slider ("Shapeable speed factor:", character.lipSyncBlendShapeSpeedFactor, 0f, 1f, "", "The rate at which Blendshapes will be animated when using a Shapeable component, with 1 = normal speed and lower = faster speed");
					}
				}
				else if (AdvGame.GetReferences ().speechManager.lipSyncOutput == LipSyncOutput.GameObjectTexture)
				{
					if (character.GetComponent <LipSyncTexture>() == null)
					{
						EditorGUILayout.HelpBox ("Attach a LipSyncTexture script to allow texture lip-syncing.", MessageType.Info);
					}
				}
			}

			if (!character.ikHeadTurning)
			{
				character.headYawParameter = CustomGUILayout.TextField ("Head yaw float:", character.headYawParameter, "", "The name of the Animator float parameter set to the head yaw");
				character.headPitchParameter = CustomGUILayout.TextField ("Head pitch float:", character.headPitchParameter, "", "The name of the Animator float parameter set to the head pitch");
			}

			character.verticalMovementParameter = CustomGUILayout.TextField ("Vertical movement float:", character.verticalMovementParameter, "", "The name of the Animator float parameter set to the vertical movement speed");
			character.isGroundedParameter = CustomGUILayout.TextField ("'Is grounded' bool:", character.isGroundedParameter, "", "The name of the Animator boolean parameter set to the 'Is Grounded' check");
			Player player = character as Player;
			if (player)
			{
				player.jumpParameter = CustomGUILayout.TextField ("Jump bool:", player.jumpParameter, "", "The name of the Animator boolean parameter to set to 'True' when jumping");
			}
			character.talkingAnimation = TalkingAnimation.Standard;

			if (character.useExpressions)
			{
				character.expressionParameter = CustomGUILayout.TextField ("Expression ID integer:", character.expressionParameter, "", "The name of the Animator integer parameter set to the active Expression ID number");
			}

			CustomGUILayout.EndVertical ();
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Mecanim settings", EditorStyles.boldLabel);

			if (SceneSettings.IsTopDown ())
			{
				character.spriteChild = (Transform) CustomGUILayout.ObjectField <Transform> ("Animator child:", character.spriteChild, true, "", "The Animator, which should be on a child GameObject");
			}
			else
			{
				character.spriteChild = null;
				character.customAnimator = (Animator) CustomGUILayout.ObjectField <Animator> ("Animator (optional):", character.customAnimator, true, "", "The Animator, if not on the root GameObject");
			}

			character.headLayer = CustomGUILayout.IntField ("Head layer #:", character.headLayer, "", "The Animator layer used to play head animations while talking");
			character.mouthLayer = CustomGUILayout.IntField ("Mouth layer #:", character.mouthLayer, "", "The Animator layer used to play mouth animations while talking");

			character.ikHeadTurning = CustomGUILayout.Toggle ("Use IK for head-turning?", character.ikHeadTurning, "", "If True, then inverse-kinematics will be used to turn the character's head dynamically, rather than playing pre-made animations");
			if (character.ikHeadTurning)
			{
				if (character.neckBone == null && character.GetComponent <CapsuleCollider>() == null && character.GetComponent <CharacterController>() == null)
				{
					EditorGUILayout.HelpBox ("For IK head-turning, a 'Neck bone' must be defined, or a Capsule Collider / Character Controller must be placed on this GameObject.", MessageType.Warning);
				}
				character.headIKTurnFactor = CustomGUILayout.Slider ("Head-turn factor:", character.headIKTurnFactor, 0f, 1f, "", "How much the head is influenced by IK head-turning.");
				character.bodyIKTurnFactor = CustomGUILayout.Slider ("Body-turn factor:", character.bodyIKTurnFactor, 0f, 1f, "", "How much the body is influenced by IK head-turning.");
				character.eyesIKTurnFactor = CustomGUILayout.Slider ("Eyes-turn factor:", character.eyesIKTurnFactor, 0f, 1f, "", "How much the eyes is influenced by IK head-turning.");
				EditorGUILayout.HelpBox ("'IK Pass' must be enabled for this character's Base layer.", MessageType.Info);
			}

			if (!Application.isPlaying)
			{
				character.ResetAnimator ();
			}
			Animator charAnimator = character.GetAnimator ();
			if (charAnimator && charAnimator.applyRootMotion)
			{
				character.rootTurningFactor = CustomGUILayout.Slider ("Root Motion turning:", character.rootTurningFactor, 0f, 1f, "", "The factor by which the job of turning is left to Mecanim root motion");
			}
			character.doWallReduction = CustomGUILayout.Toggle ("Slow movement near walls?", character.doWallReduction, "", "If True, then characters will slow down when walking into walls");
			if (character.doWallReduction)
			{
				character.wallLayer = CustomGUILayout.TextField ("Wall collider layer:", character.wallLayer, "", "The layer that walls are expected to be placed on");
				character.wallDistance = CustomGUILayout.Slider ("Collider distance:", character.wallDistance, 0f, 2f, "", "The distance to keep away from walls");
				character.wallReductionOnlyParameter = CustomGUILayout.Toggle ("Only affects Mecanim parameter?", character.wallReductionOnlyParameter, "", "If True, then the wall reduction factor will only affect the Animator move speed float parameter, and not character's actual speed");
			}

			CustomGUILayout.EndVertical ();
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Bone transforms", EditorStyles.boldLabel);

			character.neckBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Neck bone:", character.neckBone, true, "", "The 'Neck bone' Transform");
			character.leftHandBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Left hand:", character.leftHandBone, true, "", "The 'Left hand bone' transform");
			character.rightHandBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Right hand:", character.rightHandBone, true, "", "The 'Right hand bone' transform");
			CustomGUILayout.EndVertical ();

			if (GUI.changed && character)
			{
				EditorUtility.SetDirty (character);
			}

			#endif
		}


		public override bool IKEnabled
		{
			get
			{
				return true;
			}
		}


		public override void CharExpressionsGUI ()
		{
			#if UNITY_EDITOR
			if (character.useExpressions)
			{
				character.mapExpressionsToShapeable = CustomGUILayout.Toggle ("Map to Shapeable?", character.mapExpressionsToShapeable, string.Empty, "If True, a Shapeable component can be mapped to expressions to allow for expression tokens to control blendshapes");
				if (character.mapExpressionsToShapeable)
				{
					if (character.GetShapeable ())
					{
						character.expressionGroupID = ActionBlendShape.ShapeableGroupGUI ("Expression shape group:", character.GetShapeable ().shapeGroups, character.expressionGroupID);

						bool anyMissing = false;
						ShapeGroup shapeGroup = character.GetShapeable ().GetGroup (character.expressionGroupID);
						if (shapeGroup != null)
						{
							foreach (Expression expression in character.expressions)
							{
								bool keyFound = false;
								foreach (ShapeKey shapeKey in shapeGroup.shapeKeys)
								{
									if (shapeKey.label == expression.label)
									{
										keyFound = true;
									}
								}

								if (!keyFound)
								{
									anyMissing = true;
								}
							}
						}

						if (shapeGroup == null || anyMissing)
						{
							EditorGUILayout.HelpBox ("The names of the expressions below must match the shape key labels.", MessageType.Warning);
						}
						character.expressionTransitionTime = CustomGUILayout.FloatField ("Transition time (s)", character.expressionTransitionTime, string.Empty, "The time to transition between expressions via shapekey");
					}
					else
					{
						EditorGUILayout.HelpBox ("A Shapeable component must be present on the model's Skinned Mesh Renderer.", MessageType.Warning);
					}
				}
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


		public override void ActionSpeechGUI (ActionSpeech action, Char speaker)
		{
			#if UNITY_EDITOR
			
			action.headClip2D = EditorGUILayout.TextField ("Head animation:", action.headClip2D);
			action.mouthClip2D = EditorGUILayout.TextField ("Mouth animation:", action.mouthClip2D);

			#endif
		}


		public override void ActionSpeechRun (ActionSpeech action)
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (action.headClip2D))
			{
				character.GetAnimator ().CrossFade (action.headClip2D, 0.1f, character.headLayer);
			}
			if (!string.IsNullOrEmpty (action.mouthClip2D))
			{
				character.GetAnimator ().CrossFade (action.mouthClip2D, 0.1f, character.mouthLayer);
			}
		}


		public override void ActionSpeechSkip (ActionSpeech action)
		{}


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
				    action.changeSpeed = EditorGUILayout.Toggle ("Change speed scale?", action.changeSpeed);
				    if (action.changeSpeed)
				    {
						action.newSpeed = EditorGUILayout.FloatField ("Walk speed scale:", action.newSpeed);
						action.parameterValue = EditorGUILayout.FloatField ("Run speed scale:", action.parameterValue);
					}

					action.changeSound = EditorGUILayout.Toggle ("Change sound?", action.changeSound);
					if (action.changeSound)
					{
						action.standard = (AnimStandard) EditorGUILayout.EnumPopup ("Change:", action.standard);
						if (action.standard == AnimStandard.Walk || action.standard == AnimStandard.Run)
						{
							action.newSound = (AudioClip) EditorGUILayout.ObjectField ("New sound:", action.newSound, typeof (AudioClip), false);
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
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip name:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip name:", action.clip2D);
				}
				//action.includeDirection = EditorGUILayout.Toggle ("Add directional suffix?", action.includeDirection);
				
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


		public override void ActionCharAnimSkip (ActionCharAnim action)
		{
			ActionCharAnimProcess (action, true);
		}


		protected float ActionCharAnimProcess (ActionCharAnim action, bool isSkipping)
		{
			switch (action.methodMecanim)
			{
				case AnimMethodCharMecanim.SetStandard:
					if (action.mecanimCharParameter == MecanimCharParameter.MoveSpeedFloat)
					{
						if (!string.IsNullOrEmpty (action.parameterName))
						{
							character.moveSpeedParameter = action.parameterName;
						}

						if (action.changeSpeed)
						{
							character.walkSpeedScale = action.newSpeed;
							character.runSpeedScale = action.parameterValue;
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
					else if (action.mecanimCharParameter == MecanimCharParameter.TalkBool)
					{
						character.talkParameter = action.parameterName;
					}
					else if (action.mecanimCharParameter == MecanimCharParameter.TurnFloat)
					{
						character.turnParameter = action.parameterName;
					}

					return 0f;

				case AnimMethodCharMecanim.ChangeParameterValue:
					if (character.GetAnimator () == null)
					{
						return 0f;
					}

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
							bool paramValue = (action.parameterValue > 0f) ? true : false;
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
					break;

				case AnimMethodCharMecanim.PlayCustom:
					if (character.GetAnimator () == null)
					{
						return 0f;
					}

					if (!action.isRunning)
					{
						if (!string.IsNullOrEmpty (action.clip2D))
						{
							character.GetAnimator ().CrossFade (action.clip2D, action.fadeTime, action.layerInt);

							if (action.willWait)
							{
								enteredCorrectState = false;
								action.isRunning = true;
								return action.defaultPauseTime;
							}
						}
					}
					else
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
						return (action.defaultPauseTime / 6f);
					}
					break;

				default:
					return 0f;
			}
			
			return 0f;
		}


		public override bool ActionCharHoldPossible ()
		{
			return true;
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
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip name:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip name:", action.clip2D);
				}
				action.layerInt = EditorGUILayout.IntField ("Mecanim layer:", action.layerInt);
				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 2f);
				action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
			}
			else if (action.methodMecanim == AnimMethodMecanim.BlendShape)
			{
				action.isPlayer = EditorGUILayout.Toggle ("Is player?", action.isPlayer);
				if (!action.isPlayer)
				{
					action.parameterID = AC.Action.ChooseParameterGUI ("Object:", parameters, action.parameterID, ParameterType.GameObject);
					if (action.parameterID >= 0)
					{
						action.constantID = 0;
						action.shapeObject = null;
					}
					else
					{
						action.shapeObject = (Shapeable) EditorGUILayout.ObjectField ("Object:", action.shapeObject, typeof (Shapeable), true);
						
						action.constantID = action.FieldToID <Shapeable> (action.shapeObject, action.constantID);
						action.shapeObject = action.IDToField <Shapeable> (action.shapeObject, action.constantID, false);
					}
				}

				action.shapeKey = EditorGUILayout.IntField ("Shape key:", action.shapeKey);
				action.shapeValue = EditorGUILayout.Slider ("Shape value:", action.shapeValue, 0f, 100f);
				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 2f);
				action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
			}
			
			#endif
		}


		public override string ActionAnimLabel (ActionAnim action)
		{
			string label = string.Empty;
			
			if (action.animator)
			{
				label = action.animator.name;
				
				if (action.methodMecanim == AnimMethodMecanim.ChangeParameterValue && action.parameterName != "")
				{
					label += " - " + action.parameterName;
				}
				else if (action.methodMecanim == AnimMethodMecanim.BlendShape)
				{
					label += " - Shapekey";
				}
			}
			
			return label;
		}


		public override void ActionAnimAssignValues (ActionAnim action, List<ActionParameter> parameters)
		{
			action.runtimeAnimator = action.AssignFile <Animator> (parameters, action.parameterID, action.constantID, action.animator);
			action.runtimeShapeObject = action.AssignFile <Shapeable> (parameters, action.parameterID, action.constantID, action.shapeObject);

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

		
		public override void ActionAnimSkip (ActionAnim action)
		{
			if (action.methodMecanim == AnimMethodMecanim.BlendShape)
			{
				if (action.runtimeShapeObject)
				{
					action.runtimeShapeObject.Change (action.shapeKey, action.shapeValue, action.fadeTime);
				}
			}
			else
			{
				ActionAnimProcess (action, true);
			}
		}


		protected float ActionAnimProcess (ActionAnim action, bool isSkipping)
		{
			if (!action.isRunning)
			{
				switch (action.methodMecanim)
				{
					case AnimMethodMecanim.ChangeParameterValue:
						if (action.runtimeAnimator && !string.IsNullOrEmpty (action.parameterName))
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
								bool paramValue = (action.parameterValue > 0f) ? true : false;
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
						break;

					case AnimMethodMecanim.PlayCustom:
						if (action.runtimeAnimator && !string.IsNullOrEmpty (action.clip2D))
						{
							int hash = Animator.StringToHash (action.clip2D);
							if (action.runtimeAnimator.HasState (action.layerInt, hash))
							{
								action.runtimeAnimator.CrossFade (hash, action.fadeTime, action.layerInt);
							}
							else
							{
								action.ReportWarning ("Cannot play clip " + action.clip2D + " on " + action.runtimeAnimator.name, action.runtimeAnimator);
							}

							if (action.willWait)
							{
								enteredCorrectState = false;
								action.isRunning = true;
								return action.defaultPauseTime;
							}
						}
						break;

					case AnimMethodMecanim.BlendShape:
						if (action.methodMecanim == AnimMethodMecanim.BlendShape && action.shapeKey > -1)
						{
							if (action.runtimeShapeObject)
							{
								action.runtimeShapeObject.Change (action.shapeKey, action.shapeValue, action.fadeTime);

								if (action.willWait)
								{
									action.isRunning = true;
									return action.fadeTime;
								}
							}
						}
						break;

					default:
						break;
				}
			}
			else
			{
				switch (action.methodMecanim)
				{
					case AnimMethodMecanim.BlendShape:
						action.isRunning = false;
						return 0f;

					case AnimMethodMecanim.PlayCustom:
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

					default:
						return 0f;
				}
			}
			
			return 0f;
		}


		public override void ActionCharRenderGUI (ActionCharRender action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR
			
			EditorGUILayout.Space ();
			action.renderLock_scale = (RenderLock) EditorGUILayout.EnumPopup ("Character scale:", action.renderLock_scale);
			if (action.renderLock_scale == RenderLock.Set)
			{
				action.scaleParameterID = Action.ChooseParameterGUI ("New scale (%):", parameters, action.scaleParameterID, ParameterType.Integer);
				if (action.scaleParameterID < 0)
				{
					action.scale = EditorGUILayout.IntField ("New scale (%):", action.scale);
				}
			}
			
			#endif
		}
		
		
		public override float ActionCharRenderRun (ActionCharRender action)
		{
			if (action.renderLock_scale == RenderLock.Set)
			{
				character.lockScale = true;
				float _scale = (float) action.scale / 100f;
				
				if (character.spriteChild)
				{
					character.spriteScale = _scale;
				}
				else
				{
					character.transform.localScale = new Vector3 (_scale, _scale, _scale);
				}
			}
			else if (action.renderLock_scale == RenderLock.Release)
			{
				character.lockScale = false;
			}
			
			return 0f;
		}


		public override void PlayIdle ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			MoveCharacter ();
			AnimTalk (character.GetAnimator ());

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, character.GetTurnFloat ());
			}

			if (character.IsPlayer)
			{
				if (!string.IsNullOrEmpty (character.jumpParameter))
				{
					character.GetAnimator ().SetBool (character.jumpParameter, character.IsJumping);
				}
			}
		}


		public override void PlayWalk ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			MoveCharacter ();
			AnimTalk (character.GetAnimator ());

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, character.GetTurnFloat ());
			}

			if (character.IsPlayer)
			{
				if (!string.IsNullOrEmpty (character.jumpParameter))
				{
					character.GetAnimator ().SetBool (character.jumpParameter, character.IsJumping);
				}
			}
		}


		protected void MoveCharacter ()
		{
			if (!string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				character.GetAnimator ().SetFloat (character.moveSpeedParameter, character.GetMoveSpeed (true));
			}
		}


		public override void PlayRun ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			MoveCharacter ();
			AnimTalk (character.GetAnimator ());

			if (!string.IsNullOrEmpty (character.turnParameter))
			{
				character.GetAnimator ().SetFloat (character.turnParameter, character.GetTurnFloat ());
			}

			if (character.IsPlayer)
			{
				if (!string.IsNullOrEmpty (character.jumpParameter))
				{
					character.GetAnimator ().SetBool (character.jumpParameter, character.IsJumping);
				}
			}
		}


		public override void PlayTalk ()
		{
			PlayIdle ();
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

			if (!string.IsNullOrEmpty (character.isGroundedParameter))
			{
				character.GetAnimator ().SetBool (character.isGroundedParameter, character.IsGrounded (true));
			}
		}


		public override void PlayJump ()
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (character.IsPlayer)
			{
				Player player = character as Player;
				
				if (!string.IsNullOrEmpty (player.jumpParameter))
				{
					character.GetAnimator ().SetBool (player.jumpParameter, true);
				}

				AnimTalk (character.GetAnimator ());
			}
		}


		public override void TurnHead (Vector2 angles)
		{
			if (character.GetAnimator () == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty (character.headYawParameter))
			{
				character.GetAnimator ().SetFloat (character.headYawParameter, angles.x);
			}

			if (!string.IsNullOrEmpty (character.headPitchParameter))
			{
				character.GetAnimator ().SetFloat (character.headPitchParameter, angles.y);
			}
		}


		public override void OnSetExpression ()
		{
			if (character.mapExpressionsToShapeable && character.GetShapeable ())
			{
				if (character.CurrentExpression != null)
				{
					character.GetShapeable ().SetActiveKey (character.expressionGroupID, character.CurrentExpression.label, 100f, character.expressionTransitionTime, MoveMethod.Smooth, null);
				}
				else
				{
					character.GetShapeable ().DisableAllKeys (character.expressionGroupID, character.expressionTransitionTime, MoveMethod.Smooth, null);
				}
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