/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AnimEngine_Sprites2DToolkit.cs"
 * 
 *	This script uses the 2D Toolkit
 *	sprite engine for animation.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class AnimEngine_Sprites2DToolkit : AnimEngine
	{

		public override void Declare (AC.Char _character)
		{
			character = _character;
			turningStyle = TurningStyle.Linear;
			isSpriteBased = true;
		}


		public override void CharSettingsGUI ()
		{
			#if UNITY_EDITOR

			if (!tk2DIntegration.IsDefinePresent ())
			{
				EditorGUILayout.HelpBox ("'tk2DIsPresent' must be listed in your Unity Player Setting's 'Scripting define symbols' for AC's 2D Toolkit integration to work.", MessageType.Warning);
			}

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Standard 2D animations", EditorStyles.boldLabel);

			character.talkingAnimation = TalkingAnimation.Standard;
			character.spriteChild = (Transform) CustomGUILayout.ObjectField <Transform> ("Sprite child:", character.spriteChild, true, "", "The sprite Transform, which should be a child GameObject");
			character.idleAnimSprite = CustomGUILayout.TextField ("Idle name:", character.idleAnimSprite, "", "The name of the 'Idle' animation(s), without suffix");
			character.walkAnimSprite = CustomGUILayout.TextField ("Walk name:", character.walkAnimSprite, "", "The name of the 'Walk' animation(s), without suffix");
			character.runAnimSprite = CustomGUILayout.TextField ("Run name:", character.runAnimSprite, "", "The name of the 'Run' animation(s), without suffix");
			character.talkAnimSprite = CustomGUILayout.TextField ("Talk name:", character.talkAnimSprite, "", "The name of the 'Talk' animation(s), without suffix");

			character.spriteDirectionData.ShowGUI ();
			character.angleSnapping = AngleSnapping.None;

			if (character.spriteDirectionData.HasDirections ())
			{
				character.frameFlipping = (AC_2DFrameFlipping) CustomGUILayout.EnumPopup ("Frame flipping:", character.frameFlipping, "", "The type of frame-flipping to use");
				if (character.frameFlipping != AC_2DFrameFlipping.None)
				{
					character.flipCustomAnims = CustomGUILayout.Toggle ("Flip custom animations?", character.flipCustomAnims, "", "If True, then custom animations will also be flipped");
				}
			}

			if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
			{
				character.rotateSprite3D = (RotateSprite3D) CustomGUILayout.EnumPopup ("Rotate sprite to:", character.rotateSprite3D, "", "The method by which the character should face the camera");
			}

			character.listExpectedAnimations = EditorGUILayout.Toggle ("List expected animations?", character.listExpectedAnimations);
			if (character.listExpectedAnimations)
			{
				string result = "\n";
				result = ShowExpected (character, character.idleAnimSprite, result);
				result = ShowExpected (character, character.walkAnimSprite, result);
				result = ShowExpected (character, character.runAnimSprite, result);
				if (character.talkingAnimation == TalkingAnimation.Standard)
				{
					result = ShowExpected (character, character.talkAnimSprite, result);
				}
				
				EditorGUILayout.HelpBox ("The following animations are required, based on the settings above:" + result, MessageType.Info);
			}

			CustomGUILayout.EndVertical ();

			if (GUI.changed && character != null)
			{
				EditorUtility.SetDirty (character);
			}

			#endif
		}


		public override PlayerData SavePlayerData (PlayerData playerData, Player player)
		{
			playerData.playerIdleAnim = player.idleAnimSprite;
			playerData.playerWalkAnim = player.walkAnimSprite;
			playerData.playerRunAnim = player.runAnimSprite;
			playerData.playerTalkAnim = player.talkAnimSprite;

			return playerData;
		}


		public override void LoadPlayerData (PlayerData playerData, Player player)
		{
			player.idleAnimSprite = playerData.playerIdleAnim;
			player.walkAnimSprite = playerData.playerWalkAnim;
			player.talkAnimSprite = playerData.playerTalkAnim;
			player.runAnimSprite = playerData.playerRunAnim;
		}


		public override NPCData SaveNPCData (NPCData npcData, NPC npc)
		{
			npcData.idleAnim = npc.idleAnimSprite;
			npcData.walkAnim = npc.walkAnimSprite;
			npcData.talkAnim = npc.talkAnimSprite;
			npcData.runAnim = npc.runAnimSprite;

			return npcData;
		}


		public override void LoadNPCData (NPCData npcData, NPC npc)
		{
			npc.idleAnimSprite = npcData.idleAnim;
			npc.walkAnimSprite = npcData.walkAnim;
			npc.talkAnimSprite = npcData.talkAnim;
			npc.runAnimSprite = npcData.runAnim;
		}


		public override void ActionCharAnimGUI (ActionCharAnim action, List<ActionParameter> parameters = null)
		{
			#if UNITY_EDITOR

			action.method = (ActionCharAnim.AnimMethodChar) EditorGUILayout.EnumPopup ("Method:", action.method);

			if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
			{
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip:", action.clip2D);
				}
				action.includeDirection = EditorGUILayout.Toggle ("Add directional suffix?", action.includeDirection);
				
				action.playMode = (AnimPlayMode) EditorGUILayout.EnumPopup ("Play mode:", action.playMode);
				if (action.playMode == AnimPlayMode.Loop)
				{
					action.willWait = false;
				}
				else
				{
					action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
				}
				
				action.layer = AnimLayer.Base;
			}
			else if (action.method == ActionCharAnim.AnimMethodChar.StopCustom)
			{
				EditorGUILayout.HelpBox ("This Action does not work for Sprite-based characters.", MessageType.Info);
			}
			else if (action.method == ActionCharAnim.AnimMethodChar.SetStandard)
			{
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip:", action.clip2D);
				}
				action.standard = (AnimStandard) EditorGUILayout.EnumPopup ("Change:", action.standard);

				if (action.standard == AnimStandard.Walk || action.standard == AnimStandard.Run)
				{
					action.changeSound = EditorGUILayout.Toggle ("Change sound?", action.changeSound);
					if (action.changeSound)
					{
						action.newSoundParameterID = Action.ChooseParameterGUI ("New sound:", parameters, action.newSoundParameterID, ParameterType.UnityObject);
						if (action.newSoundParameterID < 0)
						{
							action.newSound = (AudioClip) EditorGUILayout.ObjectField ("New sound:", action.newSound, typeof (AudioClip), false);
						}
					}
					action.changeSpeed = EditorGUILayout.Toggle ("Change speed?", action.changeSpeed);
					if (action.changeSpeed)
					{
						action.newSpeedParameterID = Action.ChooseParameterGUI ("New speed:", parameters, action.newSpeedParameterID, ParameterType.Float);
						if (action.newSpeedParameterID < 0)
						{
							action.newSpeed = EditorGUILayout.FloatField ("New speed:", action.newSpeed);
						}
					}
				}
			}

			#endif
		}


		#if UNITY_EDITOR

		private string ShowExpected (AC.Char character, string animName, string result)
		{
			if (character == null || animName == "")
			{
				return result;
			}

			result += character.spriteDirectionData.GetExpectedList (character.frameFlipping, animName);

			return result;
		}

		#endif


		public override float ActionCharAnimRun (ActionCharAnim action)
		{
			string clip2DNew = action.clip2D;
			if (action.includeDirection)
			{
				clip2DNew += character.GetSpriteDirection ();
			}
			
			if (!action.isRunning)
			{
				action.isRunning = true;
				
				if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom && action.clip2D != "")
				{
					character.charState = CharState.Custom;
					
					if (action.playMode == AnimPlayMode.Loop)
					{
						tk2DIntegration.PlayAnimation (character.spriteChild, clip2DNew, true, WrapMode.Loop);
						action.willWait = false;
					}
					else
					{
						tk2DIntegration.PlayAnimation (character.spriteChild, clip2DNew, true, WrapMode.Once);
					}
				}

				else if (action.method == ActionCharAnim.AnimMethodChar.ResetToIdle)
				{
					character.ResetBaseClips ();
				}
				
				else if (action.method == ActionCharAnim.AnimMethodChar.SetStandard)
				{
					if (action.clip2D != "")
					{
						if (action.standard == AnimStandard.Idle)
						{
							character.idleAnimSprite = action.clip2D;
						}
						else if (action.standard == AnimStandard.Walk)
						{
							character.walkAnimSprite = action.clip2D;
						}
						else if (action.standard == AnimStandard.Talk)
						{
							character.talkAnimSprite = action.clip2D;
						}
						else if (action.standard == AnimStandard.Run)
						{
							character.runAnimSprite = action.clip2D;
						}
					}

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
							if (action.newSound)
							{
								character.walkSound = action.newSound;
							}
							else
							{
								character.walkSound = null;
							}
						}
						else if (action.standard == AnimStandard.Run)
						{
							if (action.newSound)
							{
								character.runSound = action.newSound;
							}
							else
							{
								character.runSound = null;
							}
						}
					}
				}
				
				if (action.willWait && !string.IsNullOrEmpty (action.clip2D))
				{
					if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
					{
						return (action.defaultPauseTime);
					}
				}
			}	
			
			else
			{
				if (character.spriteChild && action.clip2D != "")
				{
					if (!tk2DIntegration.IsAnimationPlaying (character.spriteChild, action.clip2D))
					{
						action.isRunning = false;
						return 0f;
					}
					else
					{
						return (action.defaultPauseTime / 6f);
					}
				}
			}

			return 0f;
		}


		public override void ActionCharAnimSkip (ActionCharAnim action)
		{
			string clip2DNew = action.clip2D;
			if (action.includeDirection)
			{
				clip2DNew += character.GetSpriteDirection ();
			}

			if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom && action.clip2D != "")
			{
				if (!action.willWait || action.playMode == AnimPlayMode.Loop)
				{
					character.charState = CharState.Custom;
					
					if (action.playMode == AnimPlayMode.Loop)
					{
						tk2DIntegration.PlayAnimation (character.spriteChild, clip2DNew, true, WrapMode.Loop);
						action.willWait = false;
					}
					else
					{
						tk2DIntegration.PlayAnimation (character.spriteChild, clip2DNew, true, WrapMode.Once);
					}
				}
				else
				{
					if (action.playMode == AnimPlayMode.PlayOnce)
					{
						character.charState = CharState.Idle;
					}
				}
			}
			
			else if (action.method == ActionCharAnim.AnimMethodChar.ResetToIdle)
			{
				character.ResetBaseClips ();
			}

			else if (action.method == ActionCharAnim.AnimMethodChar.SetStandard)
			{
				if (!string.IsNullOrEmpty (action.clip2D))
				{
					if (action.standard == AnimStandard.Idle)
					{
						character.idleAnimSprite = action.clip2D;
					}
					else if (action.standard == AnimStandard.Walk)
					{
						character.walkAnimSprite = action.clip2D;
					}
					else if (action.standard == AnimStandard.Talk)
					{
						character.talkAnimSprite = action.clip2D;
					}
					else if (action.standard == AnimStandard.Run)
					{
						character.runAnimSprite = action.clip2D;
					}
				}
				
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
						if (action.newSound)
						{
							character.walkSound = action.newSound;
						}
						else
						{
							character.walkSound = null;
						}
					}
					else if (action.standard == AnimStandard.Run)
					{
						if (action.newSound)
						{
							character.runSound = action.newSound;
						}
						else
						{
							character.runSound = null;
						}
					}
				}
			}
		}


		public override void ActionAnimGUI (ActionAnim action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR

			action.method = (AnimMethod) EditorGUILayout.EnumPopup ("Method:", action.method);

			action.parameterID = AC.Action.ChooseParameterGUI ("Object:", parameters, action.parameterID, ParameterType.GameObject);
			if (action.parameterID >= 0)
			{
				action.constantID = 0;
				action._anim2D = null;
			}
			else
			{
				action._anim2D = (Transform) EditorGUILayout.ObjectField ("Object:", action._anim2D, typeof (Transform), true);
				
				action.constantID = action.FieldToID (action._anim2D, action.constantID);
				action._anim2D = action.IDToField (action._anim2D, action.constantID, false);
			}
			
			if (action.method == AnimMethod.PlayCustom)
			{
				action.clip2DParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clip2DParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (action.clip2DParameterID < 0)
				{
					action.clip2D = EditorGUILayout.TextField ("Clip:", action.clip2D);
				}
				action.wrapMode2D = (ActionAnim.WrapMode2D) EditorGUILayout.EnumPopup ("Play mode:", action.wrapMode2D);
				
				if (action.wrapMode2D == ActionAnim.WrapMode2D.Once)
				{
					action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
				}
				else
				{
					action.willWait = false;
				}
			}
			else if (action.method == AnimMethod.BlendShape)
			{
				EditorGUILayout.HelpBox ("BlendShapes are not available in 2D animation.", MessageType.Info);
			}

			#endif
		}


		public override string ActionAnimLabel (ActionAnim action)
		{
			string label = "";
			
			if (action._anim2D)
			{
				label = action._anim2D.name;
				
				if (action.method == AnimMethod.PlayCustom && !string.IsNullOrEmpty (action.clip2D))
				{
					label += " - " + action.clip2D;
				}
			}
			
			return label;
		}


		public override void ActionAnimAssignValues (ActionAnim action, List<ActionParameter> parameters)
		{
			action.runtimeAnim2D = action.AssignFile (parameters, action.parameterID, action.constantID, action._anim2D);
		}

		
		public override float ActionAnimRun (ActionAnim action)
		{
			if (!action.isRunning)
			{
				action.isRunning = true;

				if (action.runtimeAnim2D != null && !string.IsNullOrEmpty (action.clip2D))
				{
					if (action.method == AnimMethod.PlayCustom)
					{
						if (action.wrapMode2D == ActionAnim.WrapMode2D.Loop)
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.Loop);
						}
						else if (action.wrapMode2D == ActionAnim.WrapMode2D.PingPong)
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.PingPong);
						}
						else
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.Once);
						}
						
						if (action.willWait)
						{
							return (action.defaultPauseTime);
						}
					}
					
					else if (action.method == AnimMethod.StopCustom)
					{
						tk2DIntegration.StopAnimation (action.runtimeAnim2D);
					}
					
					else if (action.method == AnimMethod.BlendShape)
					{
						action.ReportWarning ("BlendShapes are not available for 2D animation.");
						return 0f;
					}
				}
			}
			else
			{
				if (action.runtimeAnim2D != null && !string.IsNullOrEmpty (action.clip2D))
				{
					if (!tk2DIntegration.IsAnimationPlaying (action.runtimeAnim2D, action.clip2D))
					{
						action.isRunning = false;
					}
					else
					{
						return (Time.deltaTime);
					}
				}
			}

			return 0f;
		}


		public override void ActionAnimSkip (ActionAnim action)
		{
			if (!action.isRunning)
			{
				action.isRunning = true;
				
				if (action.runtimeAnim2D != null && !string.IsNullOrEmpty (action.clip2D))
				{
					if (action.method == AnimMethod.PlayCustom)
					{
						if (action.wrapMode2D == ActionAnim.WrapMode2D.Loop)
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.Loop);
						}
						else if (action.wrapMode2D == ActionAnim.WrapMode2D.PingPong)
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.PingPong);
						}
						else
						{
							tk2DIntegration.PlayAnimation (action.runtimeAnim2D, action.clip2D, true, WrapMode.Once);
						}
					}
					
					else if (action.method == AnimMethod.StopCustom)
					{
						tk2DIntegration.StopAnimation (action.runtimeAnim2D);
					}
				}
			}
		}


		public override void ActionCharRenderGUI (ActionCharRender action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR
			
			EditorGUILayout.Space ();
			action.renderLock_scale = (RenderLock) EditorGUILayout.EnumPopup ("Sprite scale:", action.renderLock_scale);
			if (action.renderLock_scale == RenderLock.Set)
			{
				action.scale = EditorGUILayout.IntField ("New scale (%):", action.scale);
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

			if (action.setNewDirections)
			{
				character._spriteDirectionData = new SpriteDirectionData (action.spriteDirectionData);
			}
		
			return 0f;
		}


		public override void PlayIdle ()
		{
			PlayStandardAnim (character.idleAnimSprite, true);
		}
		
		
		public override void PlayWalk ()
		{
			PlayStandardAnim (character.walkAnimSprite, true);
		}
		
		
		public override void PlayRun ()
		{
			PlayStandardAnim (character.runAnimSprite, true);
		}
		
		
		public override void PlayTalk ()
		{
			if (character.LipSyncGameObject ())
			{
				PlayStandardAnim (character.talkAnimSprite, true, character.GetLipSyncFrame ());
			}
			else
			{
				PlayStandardAnim (character.talkAnimSprite, true);
			}
		}
		
		
		private void PlayStandardAnim (string clip, bool includeDirection)
		{
			PlayStandardAnim (clip, includeDirection, -1);
		}


		private void PlayStandardAnim (string clip, bool includeDirection, int frame)
		{
			if (!string.IsNullOrEmpty (clip) && character)
			{
				string newClip = clip;
				
				if (includeDirection)
				{
					newClip += character.GetSpriteDirection ();
				}
				
				if (tk2DIntegration.PlayAnimation (character.spriteChild, newClip, frame) == false)
				{
					tk2DIntegration.PlayAnimation (character.spriteChild, clip, frame);
				}
			}
		}

	}

}