using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(CompoundBrush), true)]
	public class CompoundBrushInspector : BrushBaseInspector
	{
//		protected override void OnEnable ()
//		{
//			base.OnEnable ();
//		}
//
		public override void DoInspectorGUI()
		{
			using (new NamedVerticalScope("Compound"))
			{
				if(GUILayout.Button("Detach Brushes"))
				{
					for (int i = 0; i < BrushTargets.Length; i++) 
					{
						Undo.DestroyObjectImmediate(BrushTargets[i]);
					}
				}
			}
			
			base.DoInspectorGUI();
		}
	}
}