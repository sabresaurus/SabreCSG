#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	public class CSGBuildContext : MonoBehaviour
	{
		[Serializable]
		public class BrushPolygonMapping
		{
			[SerializeField]
			public Brush Brush;
			[SerializeField]
			public List<Polygon> Polygons;
		}

		[Serializable]
		public class BuildContext
		{
			// Metrics for informational purposes
			[SerializeField]
			public BuildMetrics buildMetrics = new BuildMetrics();

			// Concatenated version of visualPolygonMappings, used by Face Tool for polygon selection
			[SerializeField]
			private List<Polygon> visualPolygons;
			
			[SerializeField]
			List<BrushPolygonMapping> visualPolygonMappings = new List<BrushPolygonMapping>();

			// Cached triangulations for the polygons
			[SerializeField]
			public PolygonEntry[] VisualPolygonIndex = new PolygonEntry[0];

			[SerializeField]
			public PolygonEntry[] CollisionPolygonIndex = new PolygonEntry[0];

			public void SetVisualMapping(Brush brush, List<Polygon> polygons)
			{
				for (int i = 0; i < visualPolygonMappings.Count; i++) 
				{
					if(visualPolygonMappings[i].Brush == brush)
					{
						visualPolygonMappings[i].Polygons = polygons;
						return;
					}
				}
				// None already existing
				visualPolygonMappings.Add(new BrushPolygonMapping() { Brush = brush, Polygons = polygons, });
			}

			public void WriteVisualMappings()
			{
				if(visualPolygons != null)
				{
					visualPolygons.Clear();
				}
				else
				{
					visualPolygons = new List<Polygon>();
				}

				for (int i = 0; i < visualPolygonMappings.Count; i++) 
				{
					// Remove any visual polygon mappings from brushes that have been deleted
					if(Brush.IsInvalidForBuild(visualPolygonMappings[i].Brush))
					{
						visualPolygonMappings.RemoveAt(i);
						i--;
						continue;
					}
					// Concat in the polygons for this brush
					visualPolygons.AddRange(visualPolygonMappings[i].Polygons);
				}
			}

			public void ClearAll()
			{
				VisualPolygonIndex = new PolygonEntry[0];
				CollisionPolygonIndex = new PolygonEntry[0];

				visualPolygons = null;
				visualPolygonMappings = new List<BrushPolygonMapping>();
			}

			public List<Polygon> VisualPolygons {
				get {
					return visualPolygons;
				}
			}
		}

		[SerializeField]
		BuildContext buildContext = new BuildContext();

		public BuildContext GetBuildContext()
		{
			return buildContext;
		}
	}
}
#endif