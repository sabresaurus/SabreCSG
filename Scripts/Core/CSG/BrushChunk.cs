#if UNITY_EDITOR || RUNTIME_CSG
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	internal class BrushChunk
	{
		Vector3 centerPoint;
		Bounds bounds;
		List<Polygon> polygons;

		List<Polygon> polyhedronPolygons;

		List<Plane> splitPlanes = new List<Plane>();

		static int NEXT_ID = 0;
		internal static void ResetNextID()
		{
			NEXT_ID = 0;
		}

		internal List<Polygon> Polygons {
			get {
				return polygons;
			}
		}

		internal List<Polygon> PolyhedronPolygons {
			get {
				return polyhedronPolygons;
			}
		}

		internal List<Plane> SplitPlanes {
			get {
				return splitPlanes;
			}
		}

		int uniqueID = 0;

		
		internal int UniqueID {
			get {
				return uniqueID;
			}
		}

		internal BrushChunk(List<Polygon> polygons, List<Plane> splitPlanes = null)
		{
			uniqueID = NEXT_ID;
			NEXT_ID++;

			this.polygons = polygons;
			this.polyhedronPolygons = new List<Polygon>(polygons);
			if(splitPlanes != null)
			{
				this.splitPlanes = splitPlanes;
			}
			else
			{
				this.splitPlanes = new List<Plane>();
			}


			int count = 1;
			centerPoint = polygons[0].Vertices[0].Position;

			for (int i = 0; i < polygons.Count; i++) 
			{
				for (int j = 0; j < polygons[i].Vertices.Length; j++) 
				{
					// Already added the first vertex of the firts polygon, so skip it
					if(i == 0 && j == 0)
					{
						continue;
					}

					centerPoint += polygons[i].Vertices[j].Position;
					
					count++;
				}
			}
			centerPoint *= 1f / count;

			CalculateBounds();
		}

		private void CalculateBounds()
		{
			if (polygons.Count > 0)
			{
				bounds = new Bounds(polygons[0].Vertices[0].Position, Vector3.zero);
				
				for (int i = 0; i < polygons.Count; i++)
				{
					for (int j = 0; j < polygons[i].Vertices.Length; j++)
					{
						bounds.Encapsulate(polygons[i].Vertices[j].Position);
					}
				}
			}
			else
			{
				bounds = new Bounds(Vector3.zero, Vector3.zero);
			}
		}

		private void ProvideSplitPlane (Plane splitPlane)
		{
			splitPlanes.Add(splitPlane);
//			this.splitPlane = splitPlane;
		}

		internal Bounds GetBounds()
		{
			return bounds;
		}

		internal List<Polygon> ProvideSubtractChunks (List<BrushChunk> subtractChunks)
		{
			List<Polygon> damagedPolygons = new List<Polygon>();

			List<Polygon> addedPolygons = new List<Polygon>();
			// If this chunk knows about any split planes
//			if(splitPlanes.Count > 0)
			{
				// For each of the subtract chunks
				int subtractChunkCount = subtractChunks.Count;
				for (int chunkIndex = 0; chunkIndex < subtractChunkCount; chunkIndex++) 
				{
					int polygonCount = subtractChunks[chunkIndex].Polygons.Count;
					// For each of the polygons within those subtract chunks
					for (int j = 0; j < polygonCount; j++) 
					{
						Polygon polygon = subtractChunks[chunkIndex].Polygons[j];

						// Disregard any polygons that can't be displayed as final geometry
						if(polygon.ExcludeFromFinal)
						{
							continue;
						}

						Plane polygonPlane = polygon.CachedPlaneTest;

						// Determine if any of the split planes this chunk has been split against match any of those
						// subtraction polygons
						bool anyFound = false;


						int polygonsCount = polygons.Count;
						Plane splitPlane;
						for (int i = 0; i < polygonsCount; i++) 
						{
							if(!polygons[i].ExcludeFromFinal)
							{
								continue;
							}

							splitPlane = polygons[i].CachedPlaneTest;

							if(MathHelper.PlaneEqualsLooserWithFlip(polygonPlane,splitPlane))
							{
								anyFound = true;
								break;
							}
						}

						bool added = false;
						if(anyFound)
						{
							// TODO: Is GetCenterPoint expensive?
							Vector3 target = polygon.GetCenterPoint();
							// If this brush chunk contains the subtraction polygon
							// TODO: This is a heftly call a lot of the time, so see about optimising
							if(GeometryHelper.PolyhedronContainsPointEpsilon3(this.polygons, target))
							{
								added = true;
								// Duplicate the subtraction polygon
								polygon = polygon.DeepCopy();


								// Flip it, so that it can form outer geometry
								polygon.Flip();
//
//								
								for (int i = 0; i < this.polygons.Count; i++) 
								{
									if(!this.polygons[i].ExcludeFromFinal && MathHelper.PlaneEqualsLooser(this.polygons[i].Plane, polygon.Plane))
									{
										if(!addedPolygons.Contains(this.polygons[i]))
										{
											//Debug.LogWarning("Removing duplicate from chunk " + this.uniqueID + " subtraction chunk " + subtractChunks[chunkIndex].UniqueID);
											polygons[i].ExcludeFromFinal = true;
										}
									}
								}

								// Add it to this brush chunk
								polygons.Add(polygon);

								addedPolygons.Add(polygon);
							}
						}

						if(!added)
						{
							damagedPolygons.Add(polygon);
						}
					}
				}
			}

			return damagedPolygons;
		}
		internal Vector3 GetCenterPoint()
		{
			return centerPoint;
		}

//		internal bool ContainsPoint(Vector3 point)
//		{
//			return GeometryHelper.PolyhedronContainsPoint(this.polygons, point);
//		}

		//		internal bool ContainsPointEpsilon1(Vector3 point)
//		{
//			return GeometryHelper.PolyhedronContainsPointEpsilon1(this.polygons, point);
//		}

		//		internal bool SplitByPlaneNew(Plane splitPlane, out BrushChunk chunkFront, out BrushChunk chunkBack)
//		{
//			Polygon createdPolygon1;
//			Polygon createdPolygon2;
//
//			List<Polygon> polygons1 = PolygonFactory.ClipPolygons(this.Polygons.ToArray().DeepCopy(), splitPlane, out createdPolygon1);
//			splitPlane = splitPlane.Flip();
//			List<Polygon> polygons2 = PolygonFactory.ClipPolygons(this.Polygons.ToArray().DeepCopy(), splitPlane, out createdPolygon2);
//
//			if(createdPolygon1 != null)
//			{
//				createdPolygon1.ExcludeFromFinal = true;
//			}
//			if(createdPolygon2 != null)
//			{
//				createdPolygon2.ExcludeFromFinal = true;
//			}
//
//			if(polygons1.Count > 0 && polygons2.Count > 0)
//			{
//				chunkFront = new BrushChunk(polygons1);
//				chunkBack = new BrushChunk(polygons2);
//
//				return true;
//			}
//			else
//			{
//				chunkFront = null;
//				chunkBack = null;
//				return false;
//			}
//		}

		internal bool SplitByPlane(Plane splitPlane, out BrushChunk chunkFront, out BrushChunk chunkBack)
		{
			List<Polygon> polygonsFront;
			List<Polygon> polygonsBack;

			if(PolygonFactory.SplitPolygonsByPlane(polygons, splitPlane, true, out polygonsFront, out polygonsBack))
			{
				if(polygonsFront.Count > 0)
				{
					chunkFront = new BrushChunk(polygonsFront, this.splitPlanes);
				}
				else
				{
					Debug.LogError("SplitPolygonsByPlane said it succeeded but returned an empty list");
					chunkFront = null;
					chunkBack = null;
					return false;
				}
				
				if(polygonsBack.Count > 0)
				{
					chunkBack = new BrushChunk(polygonsBack, this.splitPlanes);
				}
				else
				{
					Debug.LogError("SplitPolygonsByPlane said it succeeded but returned an empty list");
					chunkFront = null;
					chunkBack = null;
					return false;
				}
				return true;
			}
			else
			{
				chunkFront = null;
				chunkBack = null;
				return false;
			}
		}
		
		internal static List<BrushChunk> SplitChunk (Polygon[] brushToSplit, Polygon[][] brushSplitters)
		{
			// Brush to split is split by the planes in brush splitter to produce a set of Brush Chunks
			
			// This method essentially takes a brush chunk and the partioning planes provided to it from 
			// the brush splitters and 
			List<BrushChunk> chunks = new List<BrushChunk>();
			chunks.Add(new BrushChunk(new List<Polygon>(brushToSplit)));
			
			for (int k = 0; k < brushSplitters.Length; k++) 
			{
				List<BrushChunk> chunksIn = new List<BrushChunk>();
				List<BrushChunk> chunksOut = new List<BrushChunk>();
				
				chunksIn.AddRange(chunks);
				
				
//				Brush brushSplitter = brushSplitters[k];
				Polygon[] splittingPolygons = brushSplitters[k];//.GenerateTransformedPolygons();
				for (int i = 0; i < splittingPolygons.Length; i++) 
				{
					Plane plane = splittingPolygons[i].Plane;
					
					for (int j = 0; j < chunksIn.Count; j++) 
					{
						BrushChunk chunkFront;
						BrushChunk chunkBack;
						
						//						if(chunksIn[j].SplitByPlaneNew(plane, out chunkFront, out chunkBack))
						if(chunksIn[j].SplitByPlane(plane, out chunkFront, out chunkBack))
						{
							chunkBack.ProvideSplitPlane(plane);
							
							chunksIn[j] = chunkFront;
							chunksOut.Add(chunkBack);
						}
					}
				}
				
				chunks.Clear();
				chunks.AddRange(chunksIn);
				chunks.AddRange(chunksOut);
			}
			
			return chunks;
		}
		


//		
//		internal static List<BrushChunk> SplitChunkOld (Brush brushToSplit, Brush[] brushSplitters)
//		{
//			// Brush to split is split by the planes in brush splitter to produce a set of Brush Chunks
//			
//			// This method essentially takes a brush chunk and the partioning planes provided to it from 
//			// the brush splitters and 
//			List<BrushChunk> chunks = new List<BrushChunk>();
//			chunks.Add(new BrushChunk(brushToSplit.GenerateTransformedPolygons().ToList()));
//			
//			for (int k = 0; k < brushSplitters.Length; k++) 
//			{
//				List<BrushChunk> chunksIn = new List<BrushChunk>();
//				List<BrushChunk> chunksOut = new List<BrushChunk>();
//				
//				chunksIn.AddRange(chunks);
//				
//				
//				Brush brushSplitter = brushSplitters[k];
//				Polygon[] splittingPolygons = brushSplitter.GenerateTransformedPolygons();
//				for (int i = 0; i < splittingPolygons.Length; i++) 
//				{
//					Plane plane = splittingPolygons[i].Plane;
//					
//					for (int j = 0; j < chunksIn.Count; j++) 
//					{
//						BrushChunk chunkFront;
//						BrushChunk chunkBack;
//						
//						//						if(chunksIn[j].SplitByPlaneNew(plane, out chunkFront, out chunkBack))
//						if(chunksIn[j].SplitByPlane(plane, out chunkFront, out chunkBack))
//						{
//							chunkBack.ProvideSplitPlane(plane);
//							
//							chunksIn[j] = chunkFront;
//							chunksOut.Add(chunkBack);
//						}
//					}
//				}
//				
//				chunks.Clear();
//				chunks.AddRange(chunksIn);
//				chunks.AddRange(chunksOut);
//			}
//			
//			return chunks;
//		}
		

	}
}
#endif