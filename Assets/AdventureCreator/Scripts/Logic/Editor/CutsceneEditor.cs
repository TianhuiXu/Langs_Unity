#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(Cutscene))]
	[System.Serializable]
	public class CutsceneEditor : ActionListEditor
	{

		public override void OnInspectorGUI ()
		{
			Cutscene _target = (Cutscene) target;
			PropertiesGUI (_target);
			base.DrawSharedElements (_target);
			
			UnityVersionHandler.CustomSetDirty (_target);
		}


		public static void PropertiesGUI (Cutscene _target)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Cutscene properties", EditorStyles.boldLabel);
			_target.source = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.source, "", "Where the Actions are stored");
			if (_target.source == ActionListSource.AssetFile)
			{
				_target.assetFile = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList asset:", _target.assetFile, false, "", "The ActionList asset that stores the Actions");
				_target.syncParamValues = CustomGUILayout.Toggle ("Sync parameter values?", _target.syncParamValues, "", "If True, the ActionList asset's parameter values will be shared amongst all linked ActionLists");
			}
			_target.actionListType = (ActionListType) CustomGUILayout.EnumPopup ("When running:", _target.actionListType, "", "The effect that running the Actions has on the rest of the game");
			if (_target.actionListType == ActionListType.PauseGameplay)
			{
				_target.isSkippable = CustomGUILayout.Toggle ("Is skippable?", _target.isSkippable, "", "If True, the Actions will be skipped when the user presses the 'EndCutscene' Input button");
			}
			_target.triggerTime = CustomGUILayout.Slider ("Start delay (s):", _target.triggerTime, 0f, 10f, "", "The delay, in seconds, before the Actions are run when the ActionList is triggered");
			_target.autosaveAfter = CustomGUILayout.Toggle ("Autosave after?", _target.autosaveAfter, "", "If True, the game will auto-save when the Actions have finished running");
			_target.tagID = ShowTagUI (_target.actions.ToArray (), _target.tagID);
			if (_target.source == ActionListSource.InScene)
			{
				_target.useParameters = CustomGUILayout.Toggle ("Use parameters?", _target.useParameters, "", "If True, ActionParameters can be used to override values within the Action objects");
			}
			else if (_target.source == ActionListSource.AssetFile && _target.assetFile != null && !_target.syncParamValues && _target.assetFile.useParameters)
			{
				_target.useParameters = CustomGUILayout.Toggle ("Set local parameter values?", _target.useParameters, "", "If True, parameter values set here will be assigned locally, and not on the ActionList asset");
			}
			CustomGUILayout.EndVertical ();

			if (_target.useParameters)
			{
				if (_target.source == ActionListSource.InScene)
				{
					EditorGUILayout.Space ();
					CustomGUILayout.BeginVertical ();

					EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
					ActionListEditor.ShowParametersGUI (_target, null, _target.parameters);

					CustomGUILayout.EndVertical ();
				}
				else if (!_target.syncParamValues && _target.source == ActionListSource.AssetFile && _target.assetFile != null && _target.assetFile.useParameters)
				{
					bool isAsset = UnityVersionHandler.IsPrefabFile (_target.gameObject);

					EditorGUILayout.Space ();
					CustomGUILayout.BeginVertical ();

					EditorGUILayout.LabelField ("Local parameter values", EditorStyles.boldLabel);
					ActionListEditor.ShowLocalParametersGUI (_target.parameters, _target.assetFile.GetParameters (), isAsset);

					CustomGUILayout.EndVertical ();
				}
			}
	    }

	}

}

#endif