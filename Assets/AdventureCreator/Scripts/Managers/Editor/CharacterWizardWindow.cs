#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class CharacterWizardWindow : EditorWindow
	{

		private GameObject baseObject;
		private AnimationEngine animationEngine = AnimationEngine.SpritesUnity;
		private string charName = "";
		private CharType charType;
		private string customAnimationClass;
		
		private int pageNumber = 0;
		private int numPages = 4;

		private bool enforce3D = false;

		private enum CharType { Player, NPC };
		private Rect pageRect = new Rect (350, 335, 150, 25);


		[MenuItem ("Adventure Creator/Editors/Character wizard", false, 4)]
		public static void Init ()
		{
			CharacterWizardWindow window = EditorWindow.GetWindowWithRect <CharacterWizardWindow> (new Rect (0, 0, 420, 360), true, "Character Wizard", true);
			window.titleContent.text = "Character wizard";
			window.position = new Rect (300, 200, 420, 360);
		}
		

		public void OnInspectorUpdate ()
		{
			Repaint ();
		}


		private bool IsFirstPerson ()
		{
			if (charType == CharType.Player && AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.movementMethod == MovementMethod.FirstPerson)
			{
				return true;
			}
			return false;
		}

		
		private void OnGUI ()
		{
			GUILayout.BeginVertical (CustomStyles.thinBox, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));

			GUILayout.Label (GetTitle (), CustomStyles.managerHeader);
			if (GetTitle () != "")
			{
				EditorGUILayout.Separator ();
				GUILayout.Space (10f);
			}
			
			ShowPage ();
			
			GUILayout.Space (15f);
			GUILayout.BeginHorizontal ();
			if (pageNumber < 1)
			{
				if (pageNumber < 0)
				{
					pageNumber = 0;
				}
				GUI.enabled = false;
			}
			if (pageNumber < numPages)
			{
				if (GUILayout.Button ("Previous", EditorStyles.miniButtonLeft))
				{
					pageNumber --;
				}
			}
			else
			{
				if (GUILayout.Button ("Restart", EditorStyles.miniButtonLeft))
				{
					pageNumber = 0;
				}
			}

			GUI.enabled = true;
			if (pageNumber < numPages - 1)
			{
				if (pageNumber == 1)
				{
					if (!IsFirstPerson ())
					{
						if (baseObject == null || UnityVersionHandler.IsPrefabFile (baseObject) || baseObject.GetComponent <AC.Char>() || !baseObject.activeInHierarchy)
						{
							GUI.enabled = false;
						}
					}
				}

				if (GUILayout.Button ("Next", EditorStyles.miniButtonRight))
				{
					pageNumber ++;
					if (pageNumber == 2 && IsFirstPerson ())
					{
						animationEngine = AnimationEngine.Mecanim;
						pageNumber = 3;
					}
					if (pageNumber == 2)
					{
						if (baseObject != null && baseObject.GetComponentInChildren <Animation>())
						{
							animationEngine = AnimationEngine.Legacy;
						}
						else if (baseObject != null && (baseObject.GetComponentInChildren <SkinnedMeshRenderer>() || baseObject.GetComponentInChildren <MeshRenderer>()))
						{
							animationEngine = AnimationEngine.Mecanim;
						}
						else if (baseObject != null && baseObject.GetComponentInChildren <SpriteRenderer>())
						{
							animationEngine = AnimationEngine.SpritesUnity;
						}
						else if (baseObject != null && tk2DIntegration.Is2DtkSprite (baseObject))
						{
							animationEngine = AnimationEngine.Sprites2DToolkit;
						}
						else if (SceneSettings.CameraPerspective == CameraPerspective.TwoD)
						{
							animationEngine = AnimationEngine.SpritesUnity;
						}
						else
						{
							animationEngine = AnimationEngine.Mecanim;
						}
					}
				}

				GUI.enabled = true;
			}
			else
			{
				if (pageNumber == numPages)
				{
					GUI.enabled = false;
				}
				if (GUILayout.Button ("Finish", EditorStyles.miniButtonRight))
				{
					pageNumber ++;
					Finish ();
				}
				GUI.enabled = true;
			}
			GUILayout.EndHorizontal ();
			
			GUI.Label (pageRect, "Page " + (pageNumber + 1) + " of " + (numPages + 1));

			GUILayout.FlexibleSpace ();
			CustomGUILayout.EndVertical ();
		}
		
		
		private string GetTitle ()
		{
			if (pageNumber == 1)
			{
				return "Base graphic";
			}
			else if (pageNumber == 2)
			{
				return "Animation engine";
			}
			else if (pageNumber == 3)
			{
				return "Additional settings";
			}
			else if (pageNumber == 4)
			{
				return "Complete";
			}
			
			return "";
		}
		
		
		private void Finish ()
		{
			bool is2D = false;

			GameObject newCharacterOb = baseObject;
			GameObject newBaseObject = baseObject;

			if (IsFirstPerson ())
			{
				newBaseObject = new GameObject ("Player");
				newBaseObject.AddComponent <Paths>();
				newBaseObject.AddComponent <Rigidbody>();
				Player playerScript = newBaseObject.AddComponent <Player>();
				playerScript.animationEngine = animationEngine;
				CapsuleCollider capsuleCollider = newBaseObject.AddComponent <CapsuleCollider>();
				capsuleCollider.center = new Vector3 (0f, 1f, 0f);
				capsuleCollider.height = 2f;

				GameObject cameraObject = new GameObject ("First person camera");
				cameraObject.transform.parent = newBaseObject.transform;
				cameraObject.transform.position = new Vector3 (0f, 1.5f, 0f);
				Camera cam = cameraObject.AddComponent <Camera>();
				cam.enabled = false;
				cameraObject.AddComponent <FirstPersonCamera>();

				newBaseObject.layer = LayerMask.NameToLayer ("Ignore Raycast");
				return;
			}

			if (animationEngine == AnimationEngine.Sprites2DToolkit || animationEngine == AnimationEngine.SpritesUnity || animationEngine == AnimationEngine.SpritesUnityComplex)
			{
				string _name = charName;
				if (charName == null || charName.Length == 0) _name = ("My new " + charType.ToString ());

				if (!enforce3D)
				{
					is2D = true;

					FollowSortingMap followSortingMap = newCharacterOb.AddComponent <FollowSortingMap>();
					followSortingMap.followSortingMap = true;
				}

				newBaseObject = new GameObject (_name);
				newCharacterOb.transform.parent = newBaseObject.transform;
				newCharacterOb.transform.position = Vector3.zero;
				newCharacterOb.transform.eulerAngles = Vector3.zero;

				newBaseObject.layer = LayerMask.NameToLayer ("Ignore Raycast");
			}

			if (animationEngine == AnimationEngine.Mecanim || animationEngine == AnimationEngine.SpritesUnity || animationEngine == AnimationEngine.SpritesUnityComplex)
			{
				if (newCharacterOb.GetComponent <Animator>() == null)
				{
					newCharacterOb.AddComponent <Animator>();
				}
			}
			else if (animationEngine == AnimationEngine.Legacy)
			{
				if (newCharacterOb.GetComponent <Animation>() == null)
				{
					newCharacterOb.AddComponent <Animation>();
				}
			}

			if (newBaseObject.GetComponent <AudioSource>() == null)
			{
				AudioSource baseAudioSource = newBaseObject.AddComponent <AudioSource>();
				baseAudioSource.playOnAwake = false;
			}

			if (newBaseObject.GetComponent <Paths>() == null)
			{
				newBaseObject.AddComponent <Paths>();
			}

			AC.Char charScript = null;
			if (charType == CharType.Player)
			{
				charScript = newBaseObject.AddComponent <Player>();
			}
			else if (charType == CharType.NPC)
			{
				charScript = newBaseObject.AddComponent <NPC>();

				if (is2D)
				{
					BoxCollider2D boxCollider = newCharacterOb.AddComponent <BoxCollider2D>();
					boxCollider.offset = new Vector2 (0f, 1f);
					boxCollider.size = new Vector2 (1f, 2f);
					boxCollider.isTrigger = true;
				}
				else
				{
					CapsuleCollider capsuleCollider = newCharacterOb.AddComponent <CapsuleCollider>();
					capsuleCollider.center = new Vector3 (0f, 1f, 0f);
					capsuleCollider.height = 2f;
				}

				Hotspot hotspot = newCharacterOb.AddComponent <Hotspot>();
				if (is2D)
				{
					hotspot.drawGizmos = false;
				}
				hotspot.hotspotName = charName;
			}

			if (is2D)
			{
				newBaseObject.AddComponent <CircleCollider2D>();

				if (charType == CharType.Player)
				{
					if (newBaseObject.GetComponent <Rigidbody2D>() == null)
					{
						newBaseObject.AddComponent <Rigidbody2D>();
					}
					charScript.ignoreGravity = true;
				}
			}
			else
			{
				if (newBaseObject.GetComponent <Rigidbody>() == null)
				{
					newBaseObject.AddComponent <Rigidbody>();
				}
				if (charType == CharType.Player)
				{
					CapsuleCollider capsuleCollider = newBaseObject.AddComponent <CapsuleCollider>();
					capsuleCollider.center = new Vector3 (0f, 1f, 0f);
					capsuleCollider.height = 2f;
					newBaseObject.layer = LayerMask.NameToLayer ("Ignore Raycast");
				}
			}

			if (animationEngine == AnimationEngine.Sprites2DToolkit || animationEngine == AnimationEngine.SpritesUnity || animationEngine == AnimationEngine.SpritesUnityComplex)
			{
				charScript.spriteChild = newCharacterOb.transform;
			}

			if (charType == CharType.Player && AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity)
			{
				GameObject detectorOb = new GameObject ("HotspotDetector");
				detectorOb.transform.parent = newBaseObject.transform;
				detectorOb.transform.position = Vector3.zero;
				detectorOb.AddComponent <DetectHotspots>();

				if (is2D)
				{
					CircleCollider2D circleCollider = detectorOb.AddComponent <CircleCollider2D>();
					circleCollider.isTrigger = true;
				}
				else
				{
					SphereCollider sphereCollider = detectorOb.AddComponent <SphereCollider>();
					sphereCollider.isTrigger = true;
				}
			}

			charScript.animationEngine = animationEngine;
			if (animationEngine == AC.AnimationEngine.Custom)
			{
				charScript.customAnimationClass = customAnimationClass;
			}
			charScript.speechLabel = charName;

			GameObject soundChild = new GameObject ("Sound child");
			soundChild.transform.parent = newBaseObject.transform;
			soundChild.transform.localPosition = Vector3.zero;
			AudioSource childAudioSource = soundChild.AddComponent <AudioSource>();
			childAudioSource.playOnAwake = false;
			Sound sound = soundChild.AddComponent <Sound>();
			charScript.soundChild = sound;

			baseObject = null;
			charName = string.Empty;
			EditorGUIUtility.PingObject (newBaseObject);
		}


		private void ShowPage ()
		{
			GUI.skin.label.wordWrap = true;
			
			if (pageNumber == 0)
			{
				if (Resource.ACLogo != null)
				{
					GUI.DrawTexture (new Rect (82, 25, 256, 128), Resource.ACLogo);
				}
				GUILayout.Space (140f);
				GUILayout.Label ("This window can assist with the creation of a Player or NPC.");
				GUILayout.Label ("To begin, click 'Next'.");
			}
			
			else if (pageNumber == 1)
			{
				GUILayout.Label ("Is this a Player or an NPC?");
				charType = (CharType) EditorGUILayout.EnumPopup (charType);

				if (charType == CharType.Player && AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					EditorGUILayout.HelpBox ("First-person Player prefabs require no base graphic, though one can be added after creation if desired.", MessageType.Info);
					return;
				}

				EditorGUILayout.Space ();
				charName = EditorGUILayout.TextField ("The " + charType.ToString () + "'s name:", charName);

				EditorGUILayout.Space ();
				GUILayout.Label ("Assign your character's base GameObject (such as a Skinned Mesh Renderer or 'idle' sprite):");
				baseObject = (GameObject) EditorGUILayout.ObjectField (baseObject, typeof (GameObject), true);

				if (baseObject != null && !IsFirstPerson ())
				{
					if (baseObject.GetComponent <AC.Char>())
					{
						EditorGUILayout.HelpBox ("The wizard cannot modify an existing character!", MessageType.Warning);
					}
					else if (UnityVersionHandler.IsPrefabFile (baseObject) || !baseObject.activeInHierarchy)
					{
						EditorGUILayout.HelpBox ("The object must be in the scene and enabled for the wizard to work.", MessageType.Warning);
					}
				}
			}
			
			else if (pageNumber == 2)
			{
				GUILayout.Label ("How should '" + charType.ToString () + "' should be animated?");
				animationEngine = (AnimationEngine) EditorGUILayout.EnumPopup (animationEngine);

				if (animationEngine == AnimationEngine.Custom)
				{
					EditorGUILayout.HelpBox ("This option is intended for characters that make use of a custom/third-party animation system that will require additional coding.", MessageType.Info);
				}
				else if (animationEngine == AnimationEngine.Legacy)
				{
					EditorGUILayout.HelpBox ("Legacy animation is for 3D characters that do not require complex animation trees or multiple layers. Its easier to use than Mecanim, but not as powerful.", MessageType.Info);
				}
				else if (animationEngine == AnimationEngine.Mecanim)
				{
					EditorGUILayout.HelpBox ("Mecanim animation is the standard option for 3D characters. You will need to define Mecanim parameters and transitions, but will have full control over how the character is animated.", MessageType.Info);
				}
				else if (animationEngine == AnimationEngine.Sprites2DToolkit)
				{
					EditorGUILayout.HelpBox ("This option allows you to animate characters using the 3rd-party 2D Toolkit asset.", MessageType.Info);
					if (!tk2DIntegration.IsDefinePresent ())
					{
						EditorGUILayout.HelpBox ("The 'tk2DIsPresent' preprocessor define must be declared in your game's Scripting Define Symbols, found in File -> Build -> Player settings.", MessageType.Warning);
					}
				}
				else if (animationEngine == AnimationEngine.SpritesUnity)
				{
					EditorGUILayout.HelpBox ("This option is the standard option for 2D characters. Animation clips are played automatically, without the need to define Mecanim parameters, but you will not be able to make e.g. smooth transitions between the different movement animations.", MessageType.Info);
				}
				else if (animationEngine == AnimationEngine.SpritesUnityComplex)
				{
					EditorGUILayout.HelpBox ("This option is harder to use than 'Sprites Unity', but gives you more control over how your character animates - allowing you to control animations using Mecanim parameters and transitions.", MessageType.Info);
				}
			}
			
			else if (pageNumber == 3)
			{
				EditorGUILayout.LabelField ("Chosen animation engine: " + animationEngine.ToString (), EditorStyles.boldLabel);

				if (!IsFirstPerson ())
				{
					if (animationEngine == AnimationEngine.Custom)
					{
						EditorGUILayout.HelpBox ("A subclass of 'AnimEngine' will be used to bridge AC with an external animation engine. The subclass script defined above must exist for the character to animate. Once created, enter its name in the box below:", MessageType.Info);
						customAnimationClass = EditorGUILayout.TextField ("Subclass script name:", customAnimationClass);
					}
					else if (animationEngine == AnimationEngine.Mecanim || animationEngine == AnimationEngine.SpritesUnityComplex || animationEngine == AnimationEngine.SpritesUnity)
					{
						if (baseObject.GetComponent <Animator>() == null)
						{
							EditorGUILayout.HelpBox ("This chosen method will make use of an Animator Controller asset.\nOnce the wizard has finished, you will need to create such an asset and assign it in your character's 'Animator' component.", MessageType.Info);
						}
						else
						{
							EditorGUILayout.HelpBox ("This chosen method will make use of an Animator component, and one has already been detected on the base object.\nThis will be assumed to be the Animator to animate the character with.", MessageType.Info);
						}
					}

					if (animationEngine == AnimationEngine.Sprites2DToolkit || animationEngine == AnimationEngine.SpritesUnityComplex || animationEngine == AnimationEngine.SpritesUnity)
					{
						if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
						{
							EditorGUILayout.LabelField ("It has been detected that you are attempting\nto create a 2D character in a 3D game.\nIs this correct?", GUILayout.Height (40f));
							enforce3D = EditorGUILayout.Toggle ("Yes!", enforce3D);
						}
					}
				}
				EditorGUILayout.HelpBox ("Click 'Finish' below to create the character and complete the wizard.", MessageType.Info);
			}
			
			else if (pageNumber == 4)
			{
				GUILayout.Label ("Congratulations, your " + charType.ToString () + " has been created! Check the '" + charType.ToString () + "' Inspector to set up animation and other properties, as well as modify any generated Colliders / Rigidbody components.");
				if (charType == CharType.Player)
				{
					GUILayout.Space (5f);
					GUILayout.Label ("To register this is as the main player character, turn it into a prefab and assign it in your Settings Manager, underneath 'Player settings'.");
				}
			}
		}

	}

}

#endif