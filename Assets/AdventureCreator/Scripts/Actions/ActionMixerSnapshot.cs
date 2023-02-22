/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMixerSnapshot.cs"
 * 
 *	Transitions to a single or multiple Audio Mixer snapshots. (Unity 5 only)
 * 
 */

using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionMixerSnapshot : Action
	{

		public int numSnapshots = 1;
		public AudioMixer audioMixer = null;
		public AudioMixerSnapshot snapshot = null;
		public List<SnapshotMix> snapshotMixes = new List<SnapshotMix>();
		public float changeTime = 0.1f;


		public override ActionCategory Category { get { return ActionCategory.Sound; }}
		public override string Title { get { return "Set Mixer snapshot"; }}
		public override string Description { get { return "Transitions to a single or multiple Audio Mixer snapshots."; }}


		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;

				if (numSnapshots == 1)
				{
					if (snapshot != null)
					{
						snapshot.TransitionTo (changeTime);
						if (changeTime > 0f && willWait)
						{
							return changeTime;
						}
					}
					else
					{
						LogWarning ("No Audio Mixer Snapshot assigned.");
					}
				}
				else if (audioMixer)
				{
					List<AudioMixerSnapshot> snapshots = new List<AudioMixerSnapshot>();
					List<float> weights = new List<float>();

					foreach (SnapshotMix snapshotMix in snapshotMixes)
					{
						snapshots.Add (snapshotMix.snapshot);
						weights.Add (snapshotMix.weight);
					}

					audioMixer.TransitionToSnapshots (snapshots.ToArray (), weights.ToArray (), changeTime);
					if (changeTime > 0f && willWait)
					{
						return changeTime;
					}
				}
				return 0f;
			}
			else
			{
				isRunning = false;
				return 0f;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			numSnapshots = EditorGUILayout.IntSlider ("Number of snapshots:", numSnapshots, 1, 10);
			if (numSnapshots == 1)
			{
				snapshot = (AudioMixerSnapshot) EditorGUILayout.ObjectField ("Snapshot:", snapshot, typeof (AudioMixerSnapshot), false);
			}
			else
			{
				audioMixer = (AudioMixer) EditorGUILayout.ObjectField ("Audio mixer:", audioMixer, typeof (AudioMixer), false);

				if (numSnapshots < snapshotMixes.Count)
				{
					snapshotMixes.RemoveRange (numSnapshots, snapshotMixes.Count - numSnapshots);
				}
				else if (numSnapshots > snapshotMixes.Count)
				{
					if (numSnapshots > snapshotMixes.Capacity)
					{
						snapshotMixes.Capacity = numSnapshots;
					}
					for (int i=snapshotMixes.Count; i<numSnapshots; i++)
					{
						snapshotMixes.Add (new SnapshotMix ());
					}
				}

				for (int i=0; i<snapshotMixes.Count; i++)
				{
					snapshotMixes[i].snapshot = (AudioMixerSnapshot) EditorGUILayout.ObjectField ("Snapshot " + (i+1).ToString () + ":", snapshotMixes[i].snapshot, typeof (AudioMixerSnapshot), false);
					snapshotMixes[i].weight = EditorGUILayout.FloatField ("Weight " + (i+1).ToString () + ":", snapshotMixes[i].weight);
				}
			}

			changeTime = EditorGUILayout.Slider ("Transition time (s):", changeTime, 0f, 10f);
			if (changeTime > 0f)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (numSnapshots == 1 && snapshot != null)
			{
				return snapshot.name;
			}
			if (numSnapshots > 1 && audioMixer != null)
			{
				return audioMixer.name;
			}
			return string.Empty;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Set Mixer snapshot' Action, set to play a single snapshot</summary>
		 * <param name = "snapshot">The snapshot to play</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMixerSnapshot CreateNew_Single (AudioMixerSnapshot snapshot, float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionMixerSnapshot newAction = CreateNew<ActionMixerSnapshot> ();
			newAction.numSnapshots = 1;
			newAction.snapshot = snapshot;
			newAction.changeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Sound: Set Mixer snapshot' Action, set to mix multiple snapshots together</summary>
		 * <param name = "snapshotMixData">A list of data related to the snapshots to mix</param>
		 * <param name = "audioMixer">The AudioMixer to play the snapshots on</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMixerSnapshot CreateNew_Mix (List<SnapshotMix> snapshotMixData, AudioMixer audioMixer, float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionMixerSnapshot newAction = CreateNew<ActionMixerSnapshot> ();
			newAction.numSnapshots = 0;
			newAction.audioMixer = audioMixer;
			newAction.snapshotMixes = snapshotMixData;
			newAction.changeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}


	[System.Serializable]
	public class SnapshotMix
	{
		public AudioMixerSnapshot snapshot;
		public float weight;
	}

}