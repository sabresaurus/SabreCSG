#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace Sabresaurus.SabreCSG
{
	public class MaterialPaletteWindow : PaletteWindow
    {
		[MenuItem("Window/Material Palette")]
		static void CreateAndShow()
		{
			EditorWindow window = EditorWindow.GetWindow<MaterialPaletteWindow>("Material Palette");//false, "Palette", true);

			window.Show();
		}

        protected override string PlayerPrefKeyPrefix 
		{
			get 
			{
				return "MaterialPaletteSelection";
			}
		}

		protected override System.Type TypeFilter 
		{
			get 
			{
				return typeof(Material);
			}
		}

		protected override void OnItemClick (Object selectedObject)
		{
			if(selectedObject is Material)
			{
				CSGModel activeModel = CSGModel.GetActiveCSGModel();
				if(activeModel != null)
				{
					SurfaceEditor surfaceEditor = (SurfaceEditor)activeModel.GetTool(MainMode.Face);
					surfaceEditor.SetSelectionMaterial((Material)selectedObject);
				}
			}
		}
    }
}
#endif