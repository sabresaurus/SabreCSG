using UnityEngine;
using UnityEditor;

public class MaterialLabelPostProcessor : AssetPostprocessor
{
	private void OnPostProcessMaterial( Material m )
	{
		string[] tags = AssetDatabase.GetLabels( m );

		AssetDatabase.SetLabels( m, new string[] { "Untagged" } );
		AssetDatabase.SaveAssets();

		Debug.Log( m.name );
	}
}
