#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor (typeof (AC.Char))]
	public class CharEditor : Editor
	{

		private Expression expressionToAffect;


		public override void OnInspectorGUI ()
		{
			EditorGUILayout.HelpBox ("This component should not be used directly - use Player or NPC instead.", MessageType.Warning);
		}


		protected void SharedGUIOne (AC.Char _target)
		{
			_target.GetAnimEngine ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Animation settings", EditorStyles.boldLabel);
			AnimationEngine animationEngine = _target.animationEngine;
			_target.animationEngine = (AnimationEngine) CustomGUILayout.EnumPopup ("Animation engine:", _target.animationEngine, "", "The animation engine that the character relies on for animation playback");
			if (animationEngine != _target.animationEngine)
			{
				_target.ResetAnimationEngine ();
			}
			if (_target.animationEngine == AnimationEngine.Custom)
			{
				_target.customAnimationClass = CustomGUILayout.TextField ("Script name:", _target.customAnimationClass, "", "The class name of the AnimEngine ScriptableObject subclass that animates the character");
			}
			_target.motionControl = (MotionControl) CustomGUILayout.EnumPopup ("Motion control:", _target.motionControl, "", "How motion is controlled");
			CustomGUILayout.EndVertical ();

			_target.GetAnimEngine ().CharSettingsGUI ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Movement settings", EditorStyles.boldLabel);

			_target.walkSpeedScale = CustomGUILayout.FloatField ("Walk speed scale:", _target.walkSpeedScale, "", "The movement speed when walking");
			_target.runSpeedScale = CustomGUILayout.FloatField ("Run speed scale:", _target.runSpeedScale, "", "The movement speed when running");
			_target.acceleration = CustomGUILayout.FloatField ("Acceleration:", _target.acceleration, "", "The acceleration factor");
			_target.deceleration = CustomGUILayout.FloatField ("Deceleration:", _target.deceleration, "", "The deceleration factor");
			_target.runDistanceThreshold = CustomGUILayout.FloatField ("Minimum run distance:", _target.runDistanceThreshold, "", "The minimum distance between the character and its destination for running to be possible");
			_target.turnSpeed = CustomGUILayout.FloatField ("Turn speed:", _target.turnSpeed, "", "The turn speed");

			if (_target.GetMotionControl () != MotionControl.Manual)
			{
				if (_target.GetAnimEngine ().isSpriteBased)
				{
					_target.turn2DCharactersIn3DSpace = CustomGUILayout.Toggle ("Turn root object in 3D?", _target.turn2DCharactersIn3DSpace, "", "If True, then the root object of a 2D, sprite-based character will rotate around the Z-axis. Otherwise, turning will be simulated and the actual rotation will be unaffected");
				}
			}
			_target.turnBeforeWalking = CustomGUILayout.Toggle ("Turn before pathfinding?", _target.turnBeforeWalking, "", "If True, the character will turn on the spot to face their destination before moving");
			_target.retroPathfinding = CustomGUILayout.Toggle ("Retro-style movement?", _target.retroPathfinding, "", "Enables 'retro-style' movement when pathfinding, where characters ignore Acceleration and Deceleration values, and turn instantly when moving");

			_target.headTurnSpeed = CustomGUILayout.Slider ("Head turn speed:", _target.headTurnSpeed, 0.1f, 20f, "", "The speed of head-turning");
			if (_target.IsPlayer && AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.PlayerCanReverse ())
			{
				_target.reverseSpeedFactor = CustomGUILayout.Slider ("Reverse speed factor:", _target.reverseSpeedFactor, 0f, 1f, "", "The factor by which speed is reduced when reversing");
				_target.canRunInReverse = CustomGUILayout.Toggle ("Can run in reverse?", _target.canRunInReverse, "", "If True, the Player can run backwards");
			}

			CustomGUILayout.EndVertical ();
		}


		protected void SharedGUITwo (AC.Char _target)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Physics settings", EditorStyles.boldLabel);
			_target.ignoreGravity = CustomGUILayout.Toggle ("Ignore gravity?", _target.ignoreGravity, "", "If True, the character will ignore the effects of gravity");
			if (_target.GetComponent <Rigidbody>() != null || _target.GetComponent <Rigidbody2D>())
			{
				if (_target.motionControl == MotionControl.Automatic)
				{
					_target.freezeRigidbodyWhenIdle = CustomGUILayout.Toggle ("Freeze Rigidbody when Idle?", _target.freezeRigidbodyWhenIdle, "", "If True, the character's Rigidbody will be frozen in place when idle. This is to help slipping when on sloped surfaces");
				}

				if (_target.motionControl != MotionControl.Manual)
				{
					if (_target.GetComponent <Rigidbody>() != null)
					{
						_target.useRigidbodyForMovement = CustomGUILayout.Toggle ("Move with Rigidbody?", _target.useRigidbodyForMovement, "", "If True, then it will be moved by adding forces in FixedUpdate, as opposed to the transform being manipulated in Update");

						if (_target.useRigidbodyForMovement)
						{
							if (_target.GetAnimator () != null && _target.GetAnimator ().applyRootMotion)
							{
								EditorGUILayout.HelpBox ("Rigidbody movement will be disabled as 'Root motion' is enabled in the Animator.", MessageType.Warning);
							}
							else if (_target.GetComponent <Rigidbody>().interpolation == RigidbodyInterpolation.None)
							{
								EditorGUILayout.HelpBox ("For smooth movement, the Rigidbody's 'Interpolation' should be set to either 'Interpolate' or 'Extrapolate'.", MessageType.Warning);
							}
						}
					}
					else if (_target.GetComponent <Rigidbody2D>() != null)
					{
						_target.useRigidbody2DForMovement = CustomGUILayout.Toggle ("Move with Rigidbody 2D?", _target.useRigidbody2DForMovement, "", "If True, then it will be moved by adding forces in FixedUpdate, as opposed to the transform being manipulated in Update");

						if (_target.useRigidbody2DForMovement)
						{
							if (_target.GetAnimator () != null && _target.GetAnimator ().applyRootMotion)
							{
								EditorGUILayout.HelpBox ("Rigidbody movement will be disabled as 'Root motion' is enabled in the Animator.", MessageType.Warning);
							}
							else if (_target.GetComponent <Rigidbody2D>().interpolation == RigidbodyInterpolation2D.None)
							{
								EditorGUILayout.HelpBox ("For smooth movement, the Rigidbody's 'Interpolation' should be set to either 'Interpolate' or 'Extrapolate'.", MessageType.Warning);
							}

							if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
							{
								EditorGUILayout.HelpBox ("Rigidbody2D-based motion only allows for X and Y movement, not Z, which may not be appropriate for 3D.", MessageType.Warning);
							}

							if (_target.GetAnimEngine ().isSpriteBased && _target.turn2DCharactersIn3DSpace)
							{
								EditorGUILayout.HelpBox ("For best results, 'Turn root object in 3D space?' above should be disabled.", MessageType.Warning);
							}
						}
					}
				}
			}

			if (!_target.ignoreGravity && _target.GetComponent<CharacterController>())
			{
				_target.simulatedMass = EditorGUILayout.FloatField ("Simulated mass:", _target.simulatedMass);
			}

			_target.groundCheckLayerMask = LayerMaskField ("Ground-check layer(s):", _target.groundCheckLayerMask);
			CustomGUILayout.EndVertical ();
			
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Audio clips", EditorStyles.boldLabel);
		
			_target.walkSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Walk sound:", _target.walkSound, false, "", "The sound to play when walking");
			_target.runSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Run sound:", _target.runSound, false, "", "The sound to play when running");
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().speechManager != null && AdvGame.GetReferences ().speechManager.scrollSubtitles)
			{
				_target.textScrollClip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Text scroll override:", _target.textScrollClip, false, "", "The sound to play when the character's speech text is scrolling");
			}
			_target.soundChild = (Sound) CustomGUILayout.ObjectField <Sound> ("SFX Sound child:", _target.soundChild, true, "", "");
			_target.speechAudioSource = (AudioSource) CustomGUILayout.ObjectField <AudioSource> ("Speech AudioSource:", _target.speechAudioSource, true, "", "The AudioSource from which to play speech audio");
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Dialogue settings", EditorStyles.boldLabel);

			_target.speechColor = CustomGUILayout.ColorField ("Speech text colour:", _target.speechColor, "", "");
			_target.speechLabel = CustomGUILayout.TextField ("Speaker label:", _target.speechLabel, "", "");
			if (_target.lineID > 0) EditorGUILayout.LabelField ("Speech Manager ID:", _target.lineID.ToString ());
			_target.speechMenuPlacement = (Transform) CustomGUILayout.ObjectField <Transform> ("Speech menu placement child:", _target.speechMenuPlacement, true, "", "The Transform at which to place Menus set to appear 'Above Speaking Character'. If this is not set, the placement will be set automatically");

			if (_target.useExpressions)
			{
				EditorGUILayout.LabelField ("Default portrait graphic:");
			}
			else
			{
				EditorGUILayout.LabelField ("Portrait graphic:");
			}
			_target.portraitIcon.ShowGUI (false);

			_target.useExpressions = CustomGUILayout.Toggle ("Use expressions?", _target.useExpressions, "", "If True, speech text can use expression tokens to change the character's expression");
			if (_target.useExpressions)
			{
				EditorGUILayout.Space ();
				CustomGUILayout.BeginVertical ();
				for (int i=0; i<_target.expressions.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Expression #" + _target.expressions[i].ID.ToString (), EditorStyles.boldLabel);

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						ExpressionSideMenu (_target, i);
					}
					EditorGUILayout.EndHorizontal ();
					_target.expressions[i].ShowGUI ();
				}

				if (GUILayout.Button ("Add new expression"))
				{
					_target.expressions.Add (new Expression (GetExpressionIDArray (_target.expressions)));
				}
				CustomGUILayout.EndVertical ();

				if (Application.isPlaying && _target.CurrentExpression != null)
				{
					GUILayout.Label ("Current expression:" + _target.CurrentExpression.label, EditorStyles.miniLabel);
				}

				EditorGUILayout.Space ();
				_target.GetAnimEngine ().CharExpressionsGUI ();
			}

			EditorGUILayout.HelpBox ("The following tokens are available to place in this character's speech text:" + GetTokensList (_target), MessageType.Info);

			CustomGUILayout.EndVertical ();
		}


		private string GetTokensList (AC.Char _target)
		{
			string result = "\n  [wait]\n  [wait:X]\n  [hold]\n  [continue]\n  [speaker]\n  [line:ID]\n  [token:ID]";
			if (_target.useExpressions)
			{
				result += "\n  [expression:none]";
				for (int i=0; i < _target.expressions.Count; i++)
				{
					if (!string.IsNullOrEmpty (_target.expressions[i].label))
					{
						result += "\n  [expression:" + _target.expressions[i].label + "]";
					}
				}
			}
			return result;
		}


		private int[] GetExpressionIDArray (List<Expression> expressions)
		{
			List<int> idArray = new List<int>();
			if (expressions != null)
			{
				foreach (Expression expression in expressions)
				{
					idArray.Add (expression.ID);
				}
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private void ExpressionSideMenu (AC.Char _target, int i)
		{
			expressionToAffect = _target.expressions[i];
			GenericMenu menu = new GenericMenu ();
			
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");

			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (i < _target.expressions.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
			}
			
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			AC.Char t = (AC.Char) target;
			ModifyAction (t, expressionToAffect, obj.ToString ());
			EditorUtility.SetDirty (t);
		}
		
		
		private void ModifyAction (AC.Char _target, Expression _expression, string callback)
		{
			int i = -1;
			if (_expression != null && _target.expressions.IndexOf (_expression) > -1)
			{
				i = _target.expressions.IndexOf (_expression);
			}
			
			switch (callback)
			{
			case "Insert after":
				Undo.RecordObject (_target, "Create expression");
				_target.expressions.Insert (i+1, new Expression (GetExpressionIDArray (_target.expressions)));
				break;
				
			case "Delete":
				Undo.RecordObject (_target, "Delete expression");
				_target.expressions.Remove (_expression);
				break;
				
			case "Move up":
				Undo.RecordObject (_target, "Move expression up");
				_target.expressions.Remove (_expression);
				_target.expressions.Insert (i-1, _expression);
				break;
				
			case "Move down":
				Undo.RecordObject (_target, "Move expression down");
				_target.expressions.Remove (_expression);
				_target.expressions.Insert (i+1, _expression);
				break;
			}
		}


 		private List<int> layerNumbers = new List<int>();
		private LayerMask LayerMaskField (string label, LayerMask layerMask)
		{
			var layers = InternalEditorUtility.layers;

			layerNumbers.Clear ();

			for (int i = 0; i < layers.Length; i++)
			layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

			int maskWithoutEmpty = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if (((1 << layerNumbers[i]) & layerMask.value) > 0)
				{
					maskWithoutEmpty |= (1 << i);
				}
			}

			maskWithoutEmpty = EditorGUILayout.MaskField (label, maskWithoutEmpty, layers);

			int mask = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if ((maskWithoutEmpty & (1 << i)) != 0)
				{
					mask |= (1 << layerNumbers[i]);
				}
			}
			layerMask.value = mask;

			return layerMask;
		}

	}

}

#endif