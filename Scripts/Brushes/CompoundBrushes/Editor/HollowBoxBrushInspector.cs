namespace Sabresaurus.SabreCSG
{
	using UnityEditor;
	using UnityEngine;

	[CanEditMultipleObjects]
	[CustomEditor( typeof( HollowBoxBrush ), true )]
	public class HollowBoxBrushInspector : CompoundBrushInspector
	{
		private SerializedProperty wallThicknessProp;

		public override void DoInspectorGUI()
		{
			using( new NamedVerticalScope( "Hollow Box" ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( wallThicknessProp );

				if( EditorGUI.EndChangeCheck() )
				{
					ApplyAndInvalidate();
				}

				EditorGUILayout.Space();
			}

			base.DoInspectorGUI();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			// Setup the SerializedProperties.
			wallThicknessProp = serializedObject.FindProperty( "wallThickness" );
		}

		private void ApplyAndInvalidate()
		{
			serializedObject.ApplyModifiedProperties();
			System.Array.ForEach( BrushTargets, item => item.Invalidate( true ) );
		}
	}
}
