#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Simple water volume example for Kerfuffles.
	/// </summary>
	/// <seealso cref="Sabresaurus.SabreCSG.Volume"/>
	[Serializable]
	public class TriggerVolume : Volume
	{
		//[SerializeField]
		//public int thickness = 0;

		[SerializeField]
		public VolumeEventType volumeEventType = VolumeEventType.SendMessage;

		[SerializeField]
		public TriggerMode triggerMode = TriggerMode.Enter;

		[SerializeField]
		public string filterTag = "Untagged";

		[SerializeField]
		public LayerMask layerMask = 0;

		[SerializeField]
		public bool triggerOnce = false;

		[SerializeField]
		public TriggerVolumeEvent onEnterEvent;

		[SerializeField]
		public TriggerVolumeEvent onStayEvent;

		[SerializeField]
		public TriggerVolumeEvent onExitEvent;

		public override bool OnInspectorGUI()
		{
#if UNITY_EDITOR

			GUILayout.BeginVertical( "Box" );
			{
				UnityEditor.EditorGUILayout.LabelField( "Trigger Options", UnityEditor.EditorStyles.boldLabel );
				GUILayout.Space( 4 );

				UnityEditor.EditorGUI.indentLevel = 1;

				GUILayout.BeginVertical();
				{
					volumeEventType = (VolumeEventType)UnityEditor.EditorGUILayout.EnumPopup( new GUIContent( "Trigger Event Type" ), volumeEventType );
					triggerMode = (TriggerMode)UnityEditor.EditorGUILayout.EnumPopup( new GUIContent( "Trigger Mode", "What kind of trigger events do we want to use?" ), triggerMode );
					layerMask = UnityEditor.EditorGUILayout.LayerField( new GUIContent( "Layer", "The layer that is detected by this trigger." ), layerMask );
					filterTag = UnityEditor.EditorGUILayout.TagField( new GUIContent( "Tag", "The tag that is detected by this trigger." ), filterTag );
					triggerOnce = UnityEditor.EditorGUILayout.Toggle( new GUIContent( "Trigger Once", "Is this a one use only trigger?" ), triggerOnce );
				}
				GUILayout.EndVertical();

				UnityEditor.EditorGUI.indentLevel = 0;
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical( "Box" );
			{
				UnityEditor.EditorGUILayout.LabelField( "Trigger Events", UnityEditor.EditorStyles.boldLabel );
				GUILayout.Space( 4 );

				UnityEditor.EditorGUI.indentLevel = 1;

				GUILayout.BeginVertical();
				{
					UnityEditor.SerializedObject tv = new UnityEditor.SerializedObject( this );
					UnityEditor.SerializedProperty prop1 = tv.FindProperty( "onEnterEvent" );
					UnityEditor.SerializedProperty prop2 = tv.FindProperty( "onStayEvent" );
					UnityEditor.SerializedProperty prop3 = tv.FindProperty( "onExitEvent" );

					UnityEditor.EditorGUI.BeginChangeCheck();

					UnityEditor.EditorGUILayout.PropertyField( prop1 );
					UnityEditor.EditorGUILayout.PropertyField( prop2 );
					UnityEditor.EditorGUILayout.PropertyField( prop3 );

					if( UnityEditor.EditorGUI.EndChangeCheck() )
					{
						tv.ApplyModifiedProperties();
						return true;
					}
				}
				GUILayout.EndVertical();

				UnityEditor.EditorGUI.indentLevel = 0;
			}
			GUILayout.EndVertical();

			base.OnInspectorGUI();

			if( ChangeCheck() == true )
				return true;

#endif
			return false;
		}

		public override void OnCreateVolume( GameObject volume )
		{
			//WaterVolumeComponent component = volume.AddComponent<WaterVolumeComponent>();
			//component.thickness = thickness;

			base.OnCreateVolume( volume );

			TriggerVolumeComponent tvc = volume.AddComponent<TriggerVolumeComponent>();
			tvc.volumeEventType = volumeEventType;
			tvc.triggerMode = triggerMode;
			tvc.filterTag = filterTag;
			tvc.layerMask = layerMask;
			tvc.triggerOnce = triggerOnce;
			tvc.onEnterEvent = onEnterEvent;
			tvc.onStayEvent = onStayEvent;
			tvc.onExitEvent = onExitEvent;
		}

		protected override bool ChangeCheck()
		{
			if( base.ChangeCheck() == true )
				return true;

			VolumeEventType oldTT = volumeEventType;
			TriggerMode oldTM = triggerMode;
			string oldTag = filterTag;
			LayerMask oldLM = layerMask;
			bool oldTO = triggerOnce;
			TriggerVolumeEvent oldOEnterE = onEnterEvent;
			TriggerVolumeEvent oldOStayE = onStayEvent;
			TriggerVolumeEvent oldOExitE = onExitEvent;

			if( volumeEventType != oldTT )
				return true;

			if( triggerMode != oldTM )
				return true;

			if( filterTag != oldTag )
				return true;

			if( layerMask != oldLM )
				return true;

			if( triggerOnce != oldTO )
				return true;

			if( oldOEnterE != onEnterEvent )
				return true;

			if( oldOStayE != onStayEvent )
				return true;

			if( oldOExitE != onExitEvent )
				return true;

			return false;
		}
	}

	[Serializable]
	public enum VolumeEventType : byte
	{
		SendMessage = 0
	};

	[Serializable]
	public enum TriggerMode : byte
	{
		Enter = 0,
		Exit = 2,
		Stay = 4,
		EnterExit = 8,
		EnterStay = 16,
		All = 32
	};
}

#endif
