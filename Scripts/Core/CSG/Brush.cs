#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sabresaurus.SabreCSG
{
	[Serializable]
	public class BrushOrder : IComparable
	{
		public int[] Position;

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < Position.Length; i++) 
			{
				builder.Append(Position[i]);
				if(i < Position.Length - 1)
				{
					builder.Append(", ");
				}
			}
			return builder.ToString();
		}

		public int CompareTo(object other)
		{
			// If LHS of compare to is bigger than RHS then return 1
			// If RHS of compare to is bigger than LHS then return -1
			// If LHS of compare to is the same as RHS then return 0

			BrushOrder otherBrushOrder = (BrushOrder)other;

			int biggestLength = Position.Length;
			if(otherBrushOrder.Position.Length > biggestLength)
			{
				biggestLength = otherBrushOrder.Position.Length;
			}

			for (int i = 0; i < biggestLength; i++) 
			{
				if(i > Position.Length-1)
				{
					return -1;
				}
				else if(i > otherBrushOrder.Position.Length-1)
				{
					return 1;
				}
				else if(Position[i] > otherBrushOrder.Position[i])
				{
					return 1;
				}
				else if(Position[i] < otherBrushOrder.Position[i])
				{
					return -1;
				}
			}
			return 0;
		}
	}
	public abstract class Brush : BrushBase
	{
		protected bool isBrushConvex = true;

		[SerializeField]
		protected BrushCache brushCache = null;

		public bool IsBrushConvex {
			get {
				return isBrushConvex;
			}
		}

		public BrushCache BrushCache {
			get {
				return brushCache;
			}
		}

		public abstract Polygon[] GenerateTransformedPolygons();

		public abstract void RecalculateBrushCache();
		public abstract void RecachePolygons(bool markUnbuilt);
		public abstract void RecalculateIntersections();
		public abstract void RecalculateIntersections(List<Brush> brushes, bool isRootChange);

		public abstract int AssignUniqueIDs(int startingIndex);

		public abstract int[] GetPolygonIDs ();

		public abstract Polygon[] GetPolygons ();

		public abstract void PrepareToBuild(List<Brush> brushes, bool forceRebuild);

		protected static List<Brush> CalculateIntersectingBrushes(Brush sourceBrush, List<Brush> brushes, bool isCollisionPass)
		{	
			// If the brush is not CSG it can't intersect any brushes!
			if(sourceBrush.IsNoCSG || sourceBrush.Mode == CSGMode.Volume)
			{
				return new List<Brush>();
			}
            // Return empty lists if the pass is not relevant
            if (isCollisionPass)
			{
				if(!sourceBrush.hasCollision)
				{
					return new List<Brush>();
				}
			}
			else
			{
				if(!sourceBrush.isVisible)
				{
					return new List<Brush>();
				}
			}

			BrushCache sourceCache = sourceBrush.BrushCache;

			List<Brush> intersectingBrushes = new List<Brush>();

			Bounds targetBounds = sourceCache.Bounds;
			Polygon[] targetPolygons = sourceCache.Polygons;

			// Find the index of this brush
			int thisIndex = -1;

			for (int i = 0; i < brushes.Count; i++) 
			{
				if(brushes[i] != null)
				{
					BrushCache brushCache = brushes[i].BrushCache;
					if(brushCache == sourceCache) 
					{
						thisIndex = i;
						break;
					}
				}
			}

			// Go through the brushes before this one
			for (int i = thisIndex-1; i >= 0; i--) 
			{
				if(!Brush.IsInvalidForBuild(brushes[i]))
				{
					// Skip any brushes not suitable for the pass
					if(brushes[i].isNoCSG || brushes[i].mode == CSGMode.Volume)
					{
						// NoCSG and volume brushes skip the CSG calcs
						continue;
					}
					else if(isCollisionPass && !brushes[i].HasCollision)
					{
						continue;
					}
					else if(!isCollisionPass && !brushes[i].IsVisible)
					{
						continue;
					}

					BrushCache brushCache = brushes[i].BrushCache;
					if(brushCache.Bounds.IntersectsApproximate(targetBounds))
					{
						if(GeometryHelper.PolyhedraIntersect(brushCache.Polygons, targetPolygons))
						{
							intersectingBrushes.Add(brushes[i]);

							// If the brush is contained entirely by a previous subtraction then it's impossible
							// to intersect with any brushes before that subtraction
							if(brushCache.Mode == CSGMode.Subtract)
							{
								if(GeometryHelper.PolyhedronContainsPolyhedron(brushCache.Polygons, targetPolygons))
								{
									break;
								}
							}
						}
					}
				}
			}

			intersectingBrushes.Reverse();

			List<BrushCache> activeSubtractions = new List<BrushCache>();

			// Go through the brushes after this one
			for (int i = thisIndex+1; i < brushes.Count; i++) 
			{
				if(!Brush.IsInvalidForBuild(brushes[i]))
				{
					// Skip any brushes not suitable for the pass
					if(brushes[i].isNoCSG || brushes[i].mode == CSGMode.Volume)
					{
						// NoCSG and volume brushes skip the CSG calcs
						continue;
					}
					else if(isCollisionPass && !brushes[i].HasCollision)
					{
						// Collision pass and this brush has no collision so skip
						continue;
					}
					else if(!isCollisionPass && !brushes[i].IsVisible)
					{
						// Visual pass and this brush isn't visible so skip
						continue;
					}

					BrushCache brushCache = brushes[i].BrushCache;
					if(brushCache.Bounds.IntersectsApproximate(targetBounds))
					{
						if(GeometryHelper.PolyhedraIntersect(brushCache.Polygons, targetPolygons))
						{
							bool containedByPreviousSubtraction = false;

							for (int j = 0; j < activeSubtractions.Count; j++) 
							{
								BrushCache subtractionCache = activeSubtractions[j];

								if(subtractionCache.Bounds.IntersectsApproximate(brushCache.Bounds))
								{
									if(GeometryHelper.PolyhedronContainsPolyhedron(subtractionCache.Polygons, brushCache.Polygons))
									{
										containedByPreviousSubtraction = true;
										break;
									}
								}
							}

							if(!containedByPreviousSubtraction)
							{
								intersectingBrushes.Add(brushes[i]);

								if(brushCache.Mode == CSGMode.Subtract)
								{
									activeSubtractions.Add(brushCache);
								}
							}
						}
					}
				}
			}

			return intersectingBrushes;
		}

		public static bool IsInvalidForBuild(Brush brush)
		{
			if(brush == null || brush.Destroyed || !brush.gameObject.activeInHierarchy)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public abstract BrushOrder GetBrushOrder();
		public abstract void UpdateCachedBrushOrder();

		public class BrushOrderComparer : IComparer<Brush>
		{
			public int Compare(Brush x, Brush y)
			{
				return x.GetBrushOrder().CompareTo(y.GetBrushOrder());
			}
		}
	}
}

#endif