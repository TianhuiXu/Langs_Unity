/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionFootstepSounds.cs"
 * 
 *	This Action changes the sounds listed in a FootstepSounds component.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionFootstepSounds : Action
	{

		public int constantID = 0;
		public int parameterID = -1;
		public FootstepSounds footstepSounds;
		protected FootstepSounds runtimeFootstepSounds;

		public bool isPlayer;
		public int playerID = -1;

		public enum FootstepSoundType { Walk, Run };
		public FootstepSoundType footstepSoundType = FootstepSoundType.Walk;

		public AudioClip[] newSounds;


		public override ActionCategory Category { get { return ActionCategory.Sound; }}
		public override string Title { get { return "Change footsteps"; }}
		public override string Description { get { return "Changes the sounds used by a FootstepSounds component."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, parameterID);
				if (player != null)
				{
					runtimeFootstepSounds = player.GetComponentInChildren <FootstepSounds>();
				}
			}
			else
			{
				runtimeFootstepSounds = AssignFile <FootstepSounds> (parameters, parameterID, constantID, footstepSounds);
			}
		}


		public override float Run ()
		{
			if (runtimeFootstepSounds == null)
			{
				LogWarning ("No FootstepSounds component set.");
			}
			else
			{
				if (footstepSoundType == FootstepSoundType.Walk)
				{
					runtimeFootstepSounds.footstepSounds = newSounds;
				}
				else if (footstepSoundType == FootstepSoundType.Run)
				{
					runtimeFootstepSounds.runSounds = newSounds;
				}
			}

			return 0f;
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Change Player's?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					parameterID = ChooseParameterGUI ("Player ID:", parameters, parameterID, ParameterType.Integer);
					if (parameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("FootstepSounds:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					footstepSounds = null;
				}
				else
				{
					footstepSounds = (FootstepSounds) EditorGUILayout.ObjectField ("FootstepSounds:", footstepSounds, typeof (FootstepSounds), true);
					
					constantID = FieldToID <FootstepSounds> (footstepSounds, constantID);
					footstepSounds = IDToField <FootstepSounds> (footstepSounds, constantID, false);
				}
			}

			footstepSoundType = (FootstepSoundType) EditorGUILayout.EnumPopup ("Clips to change:", footstepSoundType);
			newSounds = ShowClipsGUI (newSounds, (footstepSoundType == FootstepSoundType.Walk) ? "New walk sounds:" : "New run sounds:");
		}


		private AudioClip[] ShowClipsGUI (AudioClip[] clips, string headerLabel)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField (headerLabel, EditorStyles.boldLabel);
			List<AudioClip> clipsList = new List<AudioClip>();

			if (clips != null)
			{
				foreach (AudioClip clip in clips)
				{
					clipsList.Add (clip);
				}
			}

			int numParameters = clipsList.Count;
			numParameters = EditorGUILayout.IntField ("# of footstep sounds:", numParameters);

			if (numParameters < clipsList.Count)
			{
				clipsList.RemoveRange (numParameters, clipsList.Count - numParameters);
			}
			else if (numParameters > clipsList.Count)
			{
				if (numParameters > clipsList.Capacity)
				{
					clipsList.Capacity = numParameters;
				}
				for (int i=clipsList.Count; i<numParameters; i++)
				{
					clipsList.Add (null);
				}
			}

			for (int i=0; i<clipsList.Count; i++)
			{
				clipsList[i] = (AudioClip) EditorGUILayout.ObjectField ("Sound #" + (i+1).ToString (), clipsList[i], typeof (AudioClip), false);
			}
			if (clipsList.Count > 1)
			{
				EditorGUILayout.HelpBox ("Sounds will be chosen at random.", MessageType.Info);
			}
			CustomGUILayout.EndVertical ();

			return clipsList.ToArray ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			FootstepSounds obToUpdate = footstepSounds;
			if (isPlayer && (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
			{
				if (!fromAssetFile && GameObject.FindObjectOfType <Player>() != null)
				{
					obToUpdate = GameObject.FindObjectOfType <Player>().GetComponentInChildren <FootstepSounds>();
				}

				if (obToUpdate == null && AdvGame.GetReferences ().settingsManager != null)
				{
					Player player = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					obToUpdate = player.GetComponentInChildren <FootstepSounds>();
				}
			}

			if (saveScriptsToo)
			{
				AddSaveScript <RememberFootstepSounds> (obToUpdate);
			}
			AssignConstantID <FootstepSounds> (obToUpdate, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (parameterID == -1)
			{
				if (isPlayer)
				{
					return "Player";
				}
				else if (footstepSounds)
				{
					return footstepSounds.gameObject.name;
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (footstepSounds && footstepSounds.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>()) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && parameterID < 0) return true;
			return (parameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Change footsteps' Action</summary>
		 * <param name = "footstepSoundsToModify">The FootstepSounds component to affect</param>
		 * <param name = "footstepSoundType">The type of footsteps (Walk / Run) to change</param>
		 * <param name = "newSounds">An array of sounds to set as the new sounds</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionFootstepSounds CreateNew (FootstepSounds footstepSoundsToModify, FootstepSoundType footstepSoundType, AudioClip[] newSounds)
		{
			ActionFootstepSounds newAction = CreateNew<ActionFootstepSounds> ();
			newAction.footstepSounds = footstepSoundsToModify;
			newAction.TryAssignConstantID (newAction.footstepSounds, ref newAction.constantID);
			newAction.footstepSoundType = footstepSoundType;
			newAction.newSounds = newSounds;
			return newAction;
		}
		
	}
	
}