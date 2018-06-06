namespace Sabresaurus.SabreCSG
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor( typeof( HollowBoxBrush ), true )]
	public class HollowBoxBrushInspector : CompoundBrushInspector
	{
		private SerializedProperty wallThicknessProp;
		private SerializedProperty brushSizeProp;

		private float bSize;

		public override void DoInspectorGUI()
		{
			using( new NamedVerticalScope( "Hollow Box" ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( wallThicknessProp );

				EditorGUILayout.BeginHorizontal();

				bSize = EditorGUILayout.FloatField("Brush Size", bSize );

				if( SabreGUILayout.Button( "Set" ) )
				{
					brushSizeProp.floatValue = bSize;
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
			bSize = brushSizeProp.floatValue;
		}

		private void ApplyAndInvalidate()
		{
			serializedObject.ApplyModifiedProperties();
			System.Array.ForEach( BrushTargets, item => item.Invalidate( true ) );
		}
	}
}
