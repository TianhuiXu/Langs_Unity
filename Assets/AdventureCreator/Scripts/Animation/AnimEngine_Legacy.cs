/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AnimEngine_Legacy.cs"
 * 
 *	This script uses the Legacy system for 3D animation.
 *	Additional code provided by ilmari.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	public class AnimEngine_Legacy : AnimEngine
	{

		public static string lastClipName = "";
		public static Dictionary<int, string> animationStateDict = new Dictionary<int, string>();


		public override void CharSettingsGUI ()
		{
			#if UNITY_EDITOR
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Standard 3D animations", EditorStyles.boldLabel);

			if (SceneSettings.IsTopDown ())
			{
				character.spriteChild = (Transform) CustomGUILayout.ObjectField <Transform> ("Animation child:", character.spriteChild, true, "", "The child object that contains the Animation component");
			}
			else
			{
				character.spriteChild = null;
			}

			character.talkingAnimation = (TalkingAnimation) CustomGUILayout.EnumPopup ("Talk animation style:", character.talkingAnimation, "", "How talking animations are handled");
			character.idleAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Idle:", character.idleAnim, false, "", "The 'Idle' animation");
			character.walkAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Walk:", character.walkAnim, false, "", "The 'Walk' animation");
			character.runAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Run:", character.runAnim, false, "", "The 'Run' animation");
			if (character.talkingAnimation == TalkingAnimation.Standard)
			{
				character.talkAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Talk:", character.talkAnim, false, "", "The 'Talk' animation");
			}

			if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager)
			{
				if (AdvGame.GetReferences ().speechManager.lipSyncMode != LipSyncMode.Off && AdvGame.GetReferences ().speechManager.lipSyncMode != LipSyncMode.FaceFX)
				{
					if (AdvGame.GetReferences ().speechManager.lipSyncOutput == LipSyncOutput.PortraitAndGameObject)
					{
						if (character.GetShapeable ())
						{
							character.lipSyncGroupID = ActionBlendShape.ShapeableGroupGUI ("Phoneme shape group:", character.GetShapeable ().shapeGroups, character.lipSyncGroupID);
							character.lipSyncBlendShapeSpeedFactor = CustomGUILayout.Slider ("Shapeable speed factor:", character.lipSyncBlendShapeSpeedFactor, 0f, 1f, "", "The rate at which Blendshapes will be animated when using a Shapeable component, with 1 = normal speed and lower = faster speed");
						}
						else
						{
							EditorGUILayout.HelpBox ("Attach a Shapeable script to show phoneme options", MessageType.Info);
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
			}

			character.turnLeftAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Turn left:", character.turnLeftAnim, false, "", "The 'Turn left' animation");
			character.turnRightAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Turn right:", character.turnRightAnim, false, "", "The 'Turn right' animation");
			character.headLookLeftAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Head look left:", character.headLookLeftAnim, false, "", "The 'Look left' animation");
			character.headLookRightAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Head look right:", character.headLookRightAnim,  false, "", "The 'Look right' animation");
			character.headLookUpAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Head look up:", character.headLookUpAnim, false, "", "The 'Look up' animation");
			character.headLookDownAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Head look down:", character.headLookDownAnim, false, "", "The 'Look down' animation");

			Player player = character as Player;
			if (player != null)
			{
				player.jumpAnim = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Jump:", player.jumpAnim, false, "", "The 'Jump' animation");
			}
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Bone transforms", EditorStyles.boldLabel);
			
			character.upperBodyBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Upper body:", character.upperBodyBone, true, "", "The 'Upper body bone' Transform, used to isolate animations");
			character.neckBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Neck bone:", character.neckBone, true, "", "The 'Neck bone' Transform, used to isolate animations");
			character.leftArmBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Left arm:", character.leftArmBone, true, "", "The 'Left arm bone' Transform, used to isolate animations");
			character.rightArmBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Right arm:", character.rightArmBone, true, "", "The 'Right arm bone' Transform, used to isolate animations");
			character.leftHandBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Left hand:", character.leftHandBone, true, "", "The 'Left hand bone' Transform, used to isolate animations");
			character.rightHandBone = (Transform) CustomGUILayout.ObjectField <Transform> ("Right hand:", character.rightHandBone, true, "", "The 'Right hand bone' Transform, used to isolate animations");
			CustomGUILayout.EndVertical ();

			if (GUI.changed && character != null)
			{
				EditorUtility.SetDirty (character);
			}

			#endif
		}


		public override void CharExpressionsGUI ()
		{
			#if UNITY_EDITOR
			if (character.useExpressions)
			{
				character.mapExpressionsToShapeable = CustomGUILayout.Toggle ("Map to Shapeable?", character.mapExpressionsToShapeable, "", "If True, a Shapeable component can be mapped to expressions to allow for expression tokens to control blendshape");
				if (character.mapExpressionsToShapeable)
				{
					if (character.GetShapeable ())
					{
						character.expressionGroupID = ActionBlendShape.ShapeableGroupGUI ("Expression shape group:", character.GetShapeable ().shapeGroups, character.expressionGroupID);
						EditorGUILayout.HelpBox ("The names of the expressions below must match the shape key labels.", MessageType.Info);
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
			playerData.playerIdleAnim = AssetLoader.GetAssetInstanceID (player.idleAnim);
			playerData.playerWalkAnim = AssetLoader.GetAssetInstanceID (player.walkAnim);
			playerData.playerRunAnim = AssetLoader.GetAssetInstanceID (player.runAnim);
			playerData.playerTalkAnim = AssetLoader.GetAssetInstanceID (player.talkAnim);

			return playerData;
		}


		public override NPCData SaveNPCData (NPCData npcData, NPC npc)
		{
			npcData.idleAnim = AssetLoader.GetAssetInstanceID (npc.idleAnim);
			npcData.walkAnim = AssetLoader.GetAssetInstanceID (npc.walkAnim);
			npcData.runAnim = AssetLoader.GetAssetInstanceID (npc.runAnim);
			npcData.talkAnim = AssetLoader.GetAssetInstanceID (npc.talkAnim);

			return npcData;
		}


		public override void LoadPlayerData (PlayerData playerData, Player player)
		{
			#if AddressableIsPresent
			if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
			{
				Addressables.LoadAssetAsync<AnimationClip> (playerData.playerIdleAnim).Completed += OnCompleteLoadIdleAnim;
				Addressables.LoadAssetAsync<AnimationClip> (playerData.playerWalkAnim).Completed += OnCompleteLoadWalkAnim;
				Addressables.LoadAssetAsync<AnimationClip> (playerData.playerRunAnim).Completed += OnCompleteLoadRunAnim;
				Addressables.LoadAssetAsync<AnimationClip> (playerData.playerTalkAnim).Completed += OnCompleteLoadTalkAnim;
				return;
			}
			#endif

			player.idleAnim = AssetLoader.RetrieveAsset (player.idleAnim, playerData.playerIdleAnim);
			player.walkAnim = AssetLoader.RetrieveAsset (player.walkAnim, playerData.playerWalkAnim);
			player.talkAnim = AssetLoader.RetrieveAsset (player.talkAnim, playerData.playerTalkAnim);
			player.runAnim = AssetLoader.RetrieveAsset (player.runAnim, playerData.playerRunAnim);
		}



		public override void LoadNPCData (NPCData npcData, NPC npc)
		{
			#if AddressableIsPresent
			if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
			{
				Addressables.LoadAssetAsync<AnimationClip> (npcData.idleAnim).Completed += OnCompleteLoadIdleAnim;
				Addressables.LoadAssetAsync<AnimationClip> (npcData.walkAnim).Completed += OnCompleteLoadWalkAnim;
				Addressables.LoadAssetAsync<AnimationClip> (npcData.runAnim).Completed += OnCompleteLoadRunAnim;
				Addressables.LoadAssetAsync<AnimationClip> (npcData.talkAnim).Completed += OnCompleteLoadTalkAnim;
				return;
			}
			#endif

			npc.idleAnim = AssetLoader.RetrieveAsset (npc.idleAnim, npcData.idleAnim);
			npc.walkAnim = AssetLoader.RetrieveAsset (npc.walkAnim, npcData.walkAnim);
			npc.runAnim = AssetLoader.RetrieveAsset (npc.runAnim, npcData.talkAnim);
			npc.talkAnim = AssetLoader.RetrieveAsset (npc.talkAnim, npcData.runAnim);
		}


		#if AddressableIsPresent

		private void OnCompleteLoadIdleAnim (AsyncOperationHandle<AnimationClip> obj)
		{
			if (obj.Result != null) character.idleAnim = obj.Result;
		}


		private void OnCompleteLoadWalkAnim (AsyncOperationHandle<AnimationClip> obj)
		{
			if (obj.Result != null) character.walkAnim = obj.Result;
		}


		private void OnCompleteLoadRunAnim (AsyncOperationHandle<AnimationClip> obj)
		{
			if (obj.Result != null) character.runAnim = obj.Result;
		}


		private void OnCompleteLoadTalkAnim (AsyncOperationHandle<AnimationClip> obj)
		{
			if (obj.Result != null) character.talkAnim = obj.Result;
		}

		#endif


		public override void ActionCharAnimGUI (ActionCharAnim action, List<ActionParameter> parameters = null)
		{
			#if UNITY_EDITOR

			action.method = (ActionCharAnim.AnimMethodChar) EditorGUILayout.EnumPopup ("Method:", action.method);

			if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom || action.method == ActionCharAnim.AnimMethodChar.StopCustom)
			{
				action.clipParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clipParameterID, ParameterType.UnityObject);
				if (action.clipParameterID < 0)
				{
					action.clip = (AnimationClip) EditorGUILayout.ObjectField ("Clip:", action.clip, typeof (AnimationClip), true);
				}

				if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
				{
					action.layer = (AnimLayer) EditorGUILayout.EnumPopup ("Layer:", action.layer);
					
					if (action.layer == AnimLayer.Base)
					{
						EditorGUILayout.LabelField ("Blend mode:", "Blend");
						action.playModeBase = (AnimPlayModeBase) EditorGUILayout.EnumPopup ("Play mode:", action.playModeBase);
					}
					else
					{
						action.blendMode = (AnimationBlendMode) EditorGUILayout.EnumPopup ("Blend mode:", action.blendMode);
						action.playMode = (AnimPlayMode) EditorGUILayout.EnumPopup ("Play mode:", action.playMode);
					}
				}
				
				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 1f);
				action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);
			}
			
			else if (action.method == ActionCharAnim.AnimMethodChar.SetStandard)
			{
				action.clipParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clipParameterID, ParameterType.UnityObject);
				if (action.clipParameterID < 0)
				{
					action.clip = (AnimationClip) EditorGUILayout.ObjectField ("Clip:", action.clip, typeof (AnimationClip), true);
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

		public override void ActionCharAnimAssignValues (ActionCharAnim action, List<ActionParameter> parameters)
		{
			switch (action.method)
			{
				case ActionCharAnim.AnimMethodChar.PlayCustom:
				case ActionCharAnim.AnimMethodChar.StopCustom:
				case ActionCharAnim.AnimMethodChar.SetStandard:
					action.clip = (AnimationClip) action.AssignObject<AnimationClip> (parameters, action.clipParameterID, action.clip);
					break;

				default:
					break;
			}
		}


		public override float ActionCharAnimRun (ActionCharAnim action)
		{
			if (character == null)
			{
				return 0f;
			}
			
			Animation animation = character.GetAnimation ();

			if (!action.isRunning)
			{
				action.isRunning = true;
				
				switch (action.method)
				{
					case ActionCharAnim.AnimMethodChar.PlayCustom:
						if (action.clip)
						{
							AdvGame.CleanUnusedClips (animation);

							WrapMode wrap = WrapMode.Once;
							Transform mixingTransform = null;

							if (action.layer == AnimLayer.Base)
							{
								character.charState = CharState.Custom;
								action.blendMode = AnimationBlendMode.Blend;
								action.playMode = (AnimPlayMode) action.playModeBase;
							}
							else if (action.layer == AnimLayer.UpperBody)
							{
								mixingTransform = character.upperBodyBone;
							}
							else if (action.layer == AnimLayer.LeftArm)
							{
								mixingTransform = character.leftArmBone;
							}
							else if (action.layer == AnimLayer.RightArm)
							{
								mixingTransform = character.rightArmBone;
							}
							else if (action.layer == AnimLayer.Neck || action.layer == AnimLayer.Head || action.layer == AnimLayer.Face || action.layer == AnimLayer.Mouth)
							{
								mixingTransform = character.neckBone;
							}

							if (action.playMode == AnimPlayMode.PlayOnceAndClamp)
							{
								wrap = WrapMode.ClampForever;
							}
							else if (action.playMode == AnimPlayMode.Loop)
							{
								wrap = WrapMode.Loop;
							}

							AdvGame.PlayAnimClip (animation, AdvGame.GetAnimLayerInt (action.layer), action.clip, action.blendMode, wrap, action.fadeTime, mixingTransform, false);
						}
						break;

					case ActionCharAnim.AnimMethodChar.StopCustom:
						if (action.clip)
						{
							if (action.clip != character.idleAnim && action.clip != character.walkAnim)
							{
								animation.Blend (action.clip.name, 0f, action.fadeTime);
							}
						}
						break;

					case ActionCharAnim.AnimMethodChar.ResetToIdle:
						character.ResetBaseClips ();
						character.charState = CharState.Idle;
						AdvGame.CleanUnusedClips (animation);
						break;

					case ActionCharAnim.AnimMethodChar.SetStandard:
						if (action.clip != null)
						{
							if (action.standard == AnimStandard.Idle)
							{
								character.idleAnim = action.clip;
							}
							else if (action.standard == AnimStandard.Walk)
							{
								character.walkAnim = action.clip;
							}
							else if (action.standard == AnimStandard.Run)
							{
								character.runAnim = action.clip;
							}
							else if (action.standard == AnimStandard.Talk)
							{
								character.talkAnim = action.clip;
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
								character.walkSound = action.newSound;
							}
							else if (action.standard == AnimStandard.Run)
							{
								character.runSound = action.newSound;
							}
						}
						break;
				}
				
				if (action.willWait && action.clip)
				{
					if (action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
					{
						return action.defaultPauseTime;
					}
					else if (action.method == ActionCharAnim.AnimMethodChar.StopCustom)
					{
						return action.fadeTime;
					}
				}
			}	
			else
			{
				if (character.GetAnimation ()[action.clip.name] && character.GetAnimation ()[action.clip.name].normalizedTime < 1f && character.GetAnimation ().IsPlaying (action.clip.name))
				{
					return action.defaultPauseTime;
				}
				else
				{
					action.isRunning = false;
					
					if (action.playMode == AnimPlayMode.PlayOnce)
					{
						character.GetAnimation ().Blend (action.clip.name, 0f, action.fadeTime);
						
						if (action.layer == AnimLayer.Base && action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
						{
							character.charState = CharState.Idle;
							character.ResetBaseClips ();
						}
					}
					
					AdvGame.CleanUnusedClips (animation);
					
					return 0f;
				}
			}
			
			return 0f;
		}


		public override void ActionCharAnimSkip (ActionCharAnim action)
		{
			if (character == null)
			{
				return;
			}
			
			Animation animation = character.GetAnimation ();

			switch (action.method)
			{
				case ActionCharAnim.AnimMethodChar.PlayCustom:
					if (action.clip)
					{
						if (action.layer == AnimLayer.Base)
						{
							character.charState = CharState.Custom;
							action.blendMode = AnimationBlendMode.Blend;
							action.playMode = (AnimPlayMode) action.playModeBase;
						}

						if (action.playMode == AnimPlayMode.PlayOnce)
						{
							if (action.layer == AnimLayer.Base && action.method == ActionCharAnim.AnimMethodChar.PlayCustom)
							{
								character.charState = CharState.Idle;
								character.ResetBaseClips ();
							}
						}
						else
						{
							AdvGame.CleanUnusedClips (animation);

							WrapMode wrap = WrapMode.Once;
							Transform mixingTransform = null;

							if (action.layer == AnimLayer.UpperBody)
							{
								mixingTransform = character.upperBodyBone;
							}
							else if (action.layer == AnimLayer.LeftArm)
							{
								mixingTransform = character.leftArmBone;
							}
							else if (action.layer == AnimLayer.RightArm)
							{
								mixingTransform = character.rightArmBone;
							}
							else if (action.layer == AnimLayer.Neck || action.layer == AnimLayer.Head || action.layer == AnimLayer.Face || action.layer == AnimLayer.Mouth)
							{
								mixingTransform = character.neckBone;
							}

							if (action.playMode == AnimPlayMode.PlayOnceAndClamp)
							{
								wrap = WrapMode.ClampForever;
							}
							else if (action.playMode == AnimPlayMode.Loop)
							{
								wrap = WrapMode.Loop;
							}

							AdvGame.PlayAnimClipFrame (animation, AdvGame.GetAnimLayerInt (action.layer), action.clip, action.blendMode, wrap, action.fadeTime, mixingTransform, 1f);
						}

						AdvGame.CleanUnusedClips (animation);
					}
					break;

				case ActionCharAnim.AnimMethodChar.StopCustom:
					if (action.clip)
					{
						if (action.clip != character.idleAnim && action.clip != character.walkAnim)
						{
							animation.Blend (action.clip.name, 0f, 0f);
						}
					}
					break;

				case ActionCharAnim.AnimMethodChar.ResetToIdle:
					character.ResetBaseClips ();
					character.charState = CharState.Idle;
					AdvGame.CleanUnusedClips (animation);
					break;

				case ActionCharAnim.AnimMethodChar.SetStandard:
					if (action.clip)
					{
						if (action.standard == AnimStandard.Idle)
						{
							character.idleAnim = action.clip;
						}
						else if (action.standard == AnimStandard.Walk)
						{
							character.walkAnim = action.clip;
						}
						else if (action.standard == AnimStandard.Run)
						{
							character.runAnim = action.clip;
						}
						else if (action.standard == AnimStandard.Talk)
						{
							character.talkAnim = action.clip;
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
							character.walkSound = action.newSound;
						}
						else if (action.standard == AnimStandard.Run)
						{
							character.runSound = action.newSound;
						}
					}
					break;

				default:
					break;
			}
		}


		public override bool ActionCharHoldPossible ()
		{
			return true;
		}


		public override void ActionSpeechGUI (ActionSpeech action, Char speaker)
		{
			#if UNITY_EDITOR

			if (speaker != null && speaker.talkingAnimation == TalkingAnimation.CustomFace)
			{
				action.headClip = (AnimationClip) EditorGUILayout.ObjectField ("Head animation:", action.headClip, typeof (AnimationClip), true);
				action.mouthClip = (AnimationClip) EditorGUILayout.ObjectField ("Mouth animation:", action.mouthClip, typeof (AnimationClip), true);
			}

			#endif
		}


		public override void ActionSpeechRun (ActionSpeech action)
		{
			if (action.Speaker != null && action.Speaker.talkingAnimation == TalkingAnimation.CustomFace && (action.headClip || action.mouthClip))
			{
				AdvGame.CleanUnusedClips (action.Speaker.GetAnimation ());
				
				if (action.headClip)
				{
					AdvGame.PlayAnimClip (action.Speaker.GetAnimation (), AdvGame.GetAnimLayerInt (AnimLayer.Head), action.headClip, AnimationBlendMode.Additive, WrapMode.Once, 0f, action.Speaker.neckBone, false);
				}
				
				if (action.mouthClip)
				{
					AdvGame.PlayAnimClip (action.Speaker.GetAnimation (), AdvGame.GetAnimLayerInt (AnimLayer.Mouth), action.mouthClip, AnimationBlendMode.Additive, WrapMode.Once, 0f, action.Speaker.neckBone, false);
				}
			}
		}


		public override void ActionSpeechSkip (ActionSpeech action)
		{
			if (action.Speaker && action.Speaker.talkingAnimation == TalkingAnimation.CustomFace && (action.headClip || action.mouthClip))
			{
				AdvGame.CleanUnusedClips (action.Speaker.GetAnimation ());	
				
				if (action.headClip)
				{
					AdvGame.PlayAnimClipFrame (action.Speaker.GetAnimation (), AdvGame.GetAnimLayerInt (AnimLayer.Head), action.headClip, AnimationBlendMode.Additive, WrapMode.Once, 0f, action.Speaker.neckBone, 1f);
				}
				
				if (action.mouthClip)
				{
					AdvGame.PlayAnimClipFrame (action.Speaker.GetAnimation (), AdvGame.GetAnimLayerInt (AnimLayer.Mouth), action.mouthClip, AnimationBlendMode.Additive, WrapMode.Once, 0f, action.Speaker.neckBone, 1f);
				}
			}
		}


		public override void ActionAnimGUI (ActionAnim action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR

			action.method = (AnimMethod) EditorGUILayout.EnumPopup ("Method:", action.method);

			if (action.method == AnimMethod.PlayCustom || action.method == AnimMethod.StopCustom)
			{
				action.parameterID = Action.ChooseParameterGUI ("Object:", parameters, action.parameterID, ParameterType.GameObject);
				if (action.parameterID >= 0)
				{
					action.constantID = 0;
					action._anim = null;
				}
				else
				{
					action._anim = (Animation) EditorGUILayout.ObjectField ("Object:", action._anim, typeof (Animation), true);
					
					action.constantID = action.FieldToID <Animation> (action._anim, action.constantID);
					action._anim = action.IDToField <Animation> (action._anim, action.constantID, false);
				}

				action.clipParameterID = Action.ChooseParameterGUI ("Clip:", parameters, action.clipParameterID, ParameterType.UnityObject);
				if (action.clipParameterID < 0)
				{
					action.clip = (AnimationClip) EditorGUILayout.ObjectField ("Clip:", action.clip, typeof (AnimationClip), true);
				}

				if (action.method == AnimMethod.PlayCustom)
				{
					action.playMode = (AnimPlayMode) EditorGUILayout.EnumPopup ("Play mode:", action.playMode);
					action.blendMode = (AnimationBlendMode) EditorGUILayout.EnumPopup ("Blend mode:",action.blendMode);
				}

				action.fadeTime = EditorGUILayout.Slider ("Transition time:", action.fadeTime, 0f, 1f);
			}
			else if (action.method == AnimMethod.BlendShape)
			{
				action.isPlayer = EditorGUILayout.Toggle ("Is player?", action.isPlayer);
				if (!action.isPlayer)
				{
					action.parameterID = Action.ChooseParameterGUI ("Object:", parameters, action.parameterID, ParameterType.GameObject);
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
			}
			
			action.willWait = EditorGUILayout.Toggle ("Wait until finish?", action.willWait);

			#endif
		}


		public override string ActionAnimLabel (ActionAnim action)
		{
			string label = string.Empty;
			
			if (action._anim)
			{
				label = action._anim.name;
				
				if (action.method == AnimMethod.PlayCustom && action.clip)
				{
					label += " - Play " + action.clip.name;
				}
				else if (action.method == AnimMethod.StopCustom && action.clip)
				{
					label += " - Stop " + action.clip.name;
				}
				else if (action.method == AnimMethod.BlendShape)
				{
					label += " - Shapekey";
				}
			}
			
			return label;
		}


		public override void ActionAnimAssignValues (ActionAnim action, List<ActionParameter> parameters)
		{
			switch (action.method)
			{
				case AnimMethod.PlayCustom:
				case AnimMethod.StopCustom:
					action.runtimeAnim = action.AssignFile <Animation> (parameters, action.parameterID, action.constantID, action._anim);
					action.clip = (AnimationClip) action.AssignObject<AnimationClip> (parameters, action.clipParameterID, action.clip);
					break;

				case AnimMethod.BlendShape:
					action.runtimeShapeObject = action.AssignFile <Shapeable> (parameters, action.parameterID, action.constantID, action.shapeObject);
					break;

				default:
					break;
			}
		}


		public override float ActionAnimRun (ActionAnim action)
		{
			if (!action.isRunning)
			{
				action.isRunning = true;
				
				switch (action.method)
				{
					case AnimMethod.PlayCustom:
						if (action.runtimeAnim != null && action.clip != null)
						{
							AdvGame.CleanUnusedClips (action.runtimeAnim);

							WrapMode wrap = WrapMode.Once;
							if (action.playMode == AnimPlayMode.PlayOnceAndClamp)
							{
								wrap = WrapMode.ClampForever;
							}
							else if (action.playMode == AnimPlayMode.Loop)
							{
								wrap = WrapMode.Loop;
							}

							AdvGame.PlayAnimClip (action.runtimeAnim, 0, action.clip, action.blendMode, wrap, action.fadeTime, null, false);
						}
						break;

					case AnimMethod.StopCustom:
						if (action.runtimeAnim && action.clip)
						{
							AdvGame.CleanUnusedClips (action.runtimeAnim);
							action.runtimeAnim.Blend (action.clip.name, 0f, action.fadeTime);
						}
						break;

					case AnimMethod.BlendShape:
						if (action.shapeKey > -1)
						{
							if (action.runtimeShapeObject != null)
							{
								action.runtimeShapeObject.Change (action.shapeKey, action.shapeValue, action.fadeTime);

								if (action.willWait)
								{
									return (action.fadeTime);
								}
							}
						}
						break;
				}
				
				if (action.willWait)
				{
					return action.defaultPauseTime;
				}
			}
			else
			{
				switch (action.method)
				{
					case AnimMethod.PlayCustom:
						if (action.runtimeAnim && action.clip)
						{
							if (!action.runtimeAnim.IsPlaying (action.clip.name))
							{
								action.isRunning = false;
								return 0f;
							}
							else
							{
								return action.defaultPauseTime;
							}
						}
						break;

					case AnimMethod.BlendShape:
						if (action.runtimeShapeObject)
						{
							action.isRunning = false;
							return 0f;
						}
						break;

					default:
						break;
				}
			}

			return 0f;
		}


		public override void ActionAnimSkip (ActionAnim action)
		{
			switch (action.method)
			{
				case AnimMethod.PlayCustom:
					if (action.runtimeAnim && action.clip)
					{
						AdvGame.CleanUnusedClips (action.runtimeAnim);

						WrapMode wrap = WrapMode.Once;
						if (action.playMode == AnimPlayMode.PlayOnceAndClamp)
						{
							wrap = WrapMode.ClampForever;
						}
						else if (action.playMode == AnimPlayMode.Loop)
						{
							wrap = WrapMode.Loop;
						}

						AdvGame.PlayAnimClipFrame (action.runtimeAnim, 0, action.clip, action.blendMode, wrap, 0f, null, 1f);
					}
					break;

				case AnimMethod.StopCustom:
					if (action.runtimeAnim && action.clip)
					{
						AdvGame.CleanUnusedClips (action.runtimeAnim);
						action.runtimeAnim.Blend (action.clip.name, 0f, 0f);
					}
					break;

				case AnimMethod.BlendShape:
					if (action.shapeKey > -1)
					{
						if (action.runtimeShapeObject != null)
						{
							action.runtimeShapeObject.Change (action.shapeKey, action.shapeValue, 0f);
						}
					}
					break;

				default:
					break;
			}
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
				if (character.spriteChild != null)
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
			PlayStandardAnim (character.idleAnim, true, false);
		}
		
		
		public override void PlayWalk ()
		{
			PlayStandardAnim (character.walkAnim, true, character.IsReversing ());
		}
		
		
		public override void PlayRun ()
		{
			PlayStandardAnim (character.runAnim, true, character.IsReversing ());
		}


		public override void PlayTalk ()
		{
			PlayStandardAnim (character.talkAnim, true, false);
		}


		public override void PlayJump ()
		{
			if (character.IsPlayer)
			{
				Player player = character as Player;

				if (player.jumpAnim)
				{
					PlayStandardAnim (player.jumpAnim, false, false);
				}
				else
				{
					PlayIdle ();
				}
			}
			else
			{
				PlayIdle ();
			}
		}

		
		public override void PlayTurnLeft ()
		{
			if (character.turnLeftAnim)
			{
				PlayStandardAnim (character.turnLeftAnim, false, false);
			}
			else
			{
				PlayIdle ();
			}
		}
		
		
		public override void PlayTurnRight ()
		{
			if (character.turnRightAnim)
			{
				PlayStandardAnim (character.turnRightAnim, false, false);
			}
			else
			{
				PlayIdle ();
			}
		}


		public override void TurnHead (Vector2 angles)
		{
			if (character == null)
			{
				return;
			}

			Animation animation = character.GetAnimation ();

			if (animation == null)
			{
				return;
			}

			// Horizontal
			if (character.headLookLeftAnim && character.headLookRightAnim)
			{
				if (angles.x < 0f)
				{
					animation.Stop (character.headLookRightAnim.name);
					AdvGame.PlayAnimClipFrame (animation, AdvGame.GetAnimLayerInt (AnimLayer.Neck), character.headLookLeftAnim, AnimationBlendMode.Additive, WrapMode.ClampForever, 0f, character.neckBone, 1f);
					animation [character.headLookLeftAnim.name].weight = -angles.x;
					animation [character.headLookLeftAnim.name].speed = 0f;
				}
				else if (angles.x > 0f)
				{
					animation.Stop (character.headLookLeftAnim.name);
					AdvGame.PlayAnimClipFrame (animation, AdvGame.GetAnimLayerInt (AnimLayer.Neck), character.headLookRightAnim, AnimationBlendMode.Additive, WrapMode.ClampForever, 0f, character.neckBone, 1f);
					animation [character.headLookRightAnim.name].weight = angles.x;
					animation [character.headLookRightAnim.name].speed = 0f;
				}
				else
				{
					animation.Stop (character.headLookLeftAnim.name);
					animation.Stop (character.headLookRightAnim.name);
				}
			}

			// Vertical
			if (character.headLookUpAnim && character.headLookDownAnim)
			{
				if (angles.y < 0f)
				{
					animation.Stop (character.headLookUpAnim.name);
					AdvGame.PlayAnimClipFrame (animation, AdvGame.GetAnimLayerInt (AnimLayer.Neck) +1, character.headLookDownAnim, AnimationBlendMode.Additive, WrapMode.ClampForever, 0f, character.neckBone, 1f);
					animation [character.headLookDownAnim.name].weight = -angles.y;
					animation [character.headLookDownAnim.name].speed = 0f;
				}
				else if (angles.y > 0f)
				{
					animation.Stop (character.headLookDownAnim.name);
					AdvGame.PlayAnimClipFrame (animation, AdvGame.GetAnimLayerInt (AnimLayer.Neck) +1, character.headLookUpAnim, AnimationBlendMode.Additive, WrapMode.ClampForever, 0f, character.neckBone, 1f);
					animation [character.headLookUpAnim.name].weight = angles.y;
					animation [character.headLookUpAnim.name].speed = 0f;
				}
				else
				{
					animation.Stop (character.headLookDownAnim.name);
					animation.Stop (character.headLookUpAnim.name);
				}
			}
		}


		protected void PlayStandardAnim (AnimationClip clip, bool doLoop, bool reverse)
		{
			if (character == null)
			{
				return;
			}

			Animation animation = character.GetAnimation ();

			if (animation != null)
			{
				AnimationState animationState = animation[NonAllocAnimationClipName(clip)];
				if (clip != null && animationState != null)
				{
					if (!animationState.enabled)
					{
						if (doLoop)
						{
							AdvGame.PlayAnimClip (animation, AdvGame.GetAnimLayerInt (AnimLayer.Base), clip, AnimationBlendMode.Blend, WrapMode.Loop, character.animCrossfadeSpeed, null, reverse);
						}
						else
						{
							AdvGame.PlayAnimClip (animation, AdvGame.GetAnimLayerInt (AnimLayer.Base), clip, AnimationBlendMode.Blend, WrapMode.Once, character.animCrossfadeSpeed, null, reverse);
						}
					}
				}
				else
				{
					if (doLoop)
					{
						AdvGame.PlayAnimClip (animation, AdvGame.GetAnimLayerInt (AnimLayer.Base), clip, AnimationBlendMode.Blend, WrapMode.Loop, character.animCrossfadeSpeed, null, reverse);
					}
					else
					{
						AdvGame.PlayAnimClip (animation, AdvGame.GetAnimLayerInt (AnimLayer.Base), clip, AnimationBlendMode.Blend, WrapMode.Once, character.animCrossfadeSpeed, null, reverse);
					}
				}
			}
		}


		public static string NonAllocAnimationClipName (AnimationClip clip)
		{
			if (clip == null)
			{
				return string.Empty;
			}

			int clipHash = clip.GetInstanceID ();
			if (clipHash >= 0)
			{
				if (animationStateDict.TryGetValue (clipHash, out lastClipName))
				{
					if (!string.IsNullOrEmpty (lastClipName))
					{
						return lastClipName;
					}
					else
					{
						lastClipName = clip.name;
						animationStateDict[clipHash] = lastClipName;
						return lastClipName;
					}
				}
				else
				{
					lastClipName = clip.name;
					animationStateDict.Add (clipHash, lastClipName);
					return lastClipName;
				}
			}

			return string.Empty;
		}


		public override void OnSetExpression ()
		{
			if (character.mapExpressionsToShapeable && character.GetShapeable () != null)
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

	}

}