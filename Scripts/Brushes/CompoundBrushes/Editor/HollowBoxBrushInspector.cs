namespace Sabresaurus.SabreCSG
{
	using UnityEditor;
	using UnityEngine;

	[CanEditMultipleObjects]
	[CustomEditor( typeof( HollowBoxBrush ), true )]
	public class HollowBoxBrushInspector : CompoundBrushInspector
	{
		private SerializedProperty wallThicknessProp;
		private SerializedProperty brushSizeProp;

		private Vector3 bSize;

		public override void DoInspectorGUI()
		{
			using( new NamedVerticalScope( "Hollow Box" ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( wallThicknessProp );

				EditorGUILayout.BeginHorizontal();

				bSize = EditorGUILayout.Vector3Field( "Brush Size", bSize );

				if( SabreGUILayout.Button( "Set" ) )
				{
					brushSizeProp.vector3Value = bSize;
				}

				EditorGUILayout.EndHorizontal();

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
			brushSizeProp = serializedObject.FindProperty( "brushSize" );

			// Get the size of the brush for the inspector field value.
			bSize = brushSizeProp.vector3Value;
		}

		private void ApplyAndInvalidate()
		{
			serializedObject.ApplyModifiedProperties();
			System.Array.ForEach( BrushTargets, item => item.Invalidate( true ) );
		}
	}
}
