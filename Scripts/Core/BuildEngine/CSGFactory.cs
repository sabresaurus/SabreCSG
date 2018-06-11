#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public enum BuildStatus { Unnecessary, Started, Complete };

    public static class CSGFactory
    {
        const bool ENABLE_OPTIMIZE_GEOMETRY = false;

		static List<Brush> brushes;
		static CSGBuildSettings buildSettings;
		static CSGBuildContext.BuildContext buildContext;
		static Transform rootTransform;
		static MaterialMeshDictionary materialMeshDictionary;
		static List<Mesh> collisionMeshDictionary;
		static bool polygonsRemoved;
		static bool forceRebuild;
		static Action<float> onProgressChange;
		static Action<GameObject, Mesh> onFinalizeVisualMesh;
		static Action<GameObject, Mesh> onFinalizeCollisionMesh;

		static DateTime buildStartTime = DateTime.Now;
		static DateTime time1 = DateTime.Now;
		static int brushesBuilt = 0;

		static bool buildInProgress = false;
		static bool readyForFinalize = false;

		static Transform meshGroup;
		static Transform meshGroupHolder;

        static bool Prepare(Transform rootTransform)
		{
			meshGroupHolder = rootTransform.Find("MeshGroup");

			if (meshGroupHolder != null)
			{
				meshGroupHolder.SetAsLastSibling();
				return false;
			}
			else
			{
				meshGroupHolder = rootTransform.AddChild("MeshGroup");
				return true;
			}
		}



		internal static BuildStatus Build(List<Brush> brushes, 
			CSGBuildSettings buildSettings,
			CSGBuildContext.BuildContext buildContext, 
			Transform rootTransform,
			MaterialMeshDictionary materialMeshDictionary,
			List<Mesh> collisionMeshDictionary,
			bool polygonsRemoved,
			bool forceRebuild,
			Action<float> onProgressChange,
			Action<GameObject, Mesh> onFinalizeVisualMesh,
			Action<GameObject, Mesh> onFinalizeCollisionMesh,
			bool multithreaded)
		{
			CSGFactory.brushes = brushes;
			CSGFactory.buildSettings = buildSettings;
			CSGFactory.buildContext = buildContext;
			CSGFactory.rootTransform = rootTransform; 
			CSGFactory.materialMeshDictionary = materialMeshDictionary;
			CSGFactory.collisionMeshDictionary = collisionMeshDictionary;
			CSGFactory.polygonsRemoved = polygonsRemoved;
			CSGFactory.forceRebuild = forceRebuild;
			CSGFactory.onProgressChange = onProgressChange;
			CSGFactory.onFinalizeVisualMesh = onFinalizeVisualMesh;
			CSGFactory.onFinalizeCollisionMesh = onFinalizeCollisionMesh;

//			DebugExclude.hackyHolder = DebugExclude.GetMetaDataHolder();
//			BrushChunk.ResetNextID();

			if(buildInProgress)
			{
				Debug.LogWarning("Existing build has not completed");
			}

			if(multithreaded)
			{
				readyForFinalize = false;
				buildInProgress = true;
				ThreadPool.QueueUserWorkItem(CoreBuild, true);
				// FinalizeBuild is called by Tick when CoreBuild is complete
				return BuildStatus.Started;
			}
			else
			{
				readyForFinalize = false;
				CoreBuild(null);
				bool buildOccurred = FinalizeBuild();
				buildInProgress = false;

				return buildOccurred ? BuildStatus.Complete : BuildStatus.Unnecessary;
			}
		}

		internal static void CoreBuild(object state)
		{
			MeshGroupManager.OnFinalizeVisualMesh = onFinalizeVisualMesh;
			MeshGroupManager.OnFinalizeCollisionMesh = onFinalizeCollisionMesh;

			if(forceRebuild)
			{				
				buildContext.ClearAll();
			}

			buildStartTime = DateTime.Now;

			int brushesToBuild = 0;
			brushesBuilt = 0;

			BrushCache[] allBrushCaches = new BrushCache[brushes.Count];

			int totalPolygons = 0;
			for (int i = 0; i < allBrushCaches.Length; i++) 
			{
				allBrushCaches[i] = brushes[i].BrushCache;
				totalPolygons += allBrushCaches[i].Polygons.Length;
			}

			PolygonEntry[] oldVisualPolygonIndex = buildContext.VisualPolygonIndex;
			PolygonEntry[] oldCollisionPolygonIndex = buildContext.CollisionPolygonIndex;

			PolygonEntry[] newVisualPolygonIndex = new PolygonEntry[totalPolygons];
			PolygonEntry[] newCollisionPolygonIndex = new PolygonEntry[totalPolygons];

			int polygonUniqueID = 0;

			// Walk through each brush, assigning unique IDs to each polygon (since Unity serialisation will break
			// references on a recompile, reload etc)
			for (int i = 0; i < allBrushCaches.Length; i++) 
			{
				brushes[i].AssignUniqueIDs(polygonUniqueID);

				// TODO: Find a way to remove this when Nova is removed
				polygonUniqueID += allBrushCaches[i].AssignUniqueIDs(polygonUniqueID, 
					brushes[i].IsVisible,
					oldVisualPolygonIndex, 
					newVisualPolygonIndex, 
					brushes[i].HasCollision,
					oldCollisionPolygonIndex, 
					newCollisionPolygonIndex);
			}

			buildContext.VisualPolygonIndex = newVisualPolygonIndex;
			buildContext.CollisionPolygonIndex = newCollisionPolygonIndex;

			// Create a list of builders that need to be built
			bool[] shouldBuildVisible = new bool[allBrushCaches.Length];
			bool[] shouldBuildCollision = new bool[allBrushCaches.Length];

			for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
			{
				if(!allBrushCaches[brushIndex].Built && brushes[brushIndex].IsVisible)
				{
					shouldBuildVisible[brushIndex] = true;
					brushesToBuild++;

					// Mark all the intersecting brushes as needing to build
					foreach (BrushCache brushCache in allBrushCaches[brushIndex].IntersectingVisualBrushCaches) 
					{
						// TODO: This is slower than it needs to be
						int otherIndex = Array.IndexOf(allBrushCaches, brushCache);
						if(otherIndex != -1)
						{
							if(!shouldBuildVisible[otherIndex] && brushes[otherIndex].IsVisible)
							{
								shouldBuildVisible[otherIndex] = true;
								brushesToBuild++;

								if(brushes[otherIndex].Mode == CSGMode.Subtract)
								{
									foreach (BrushCache brushCache2 in allBrushCaches[otherIndex].IntersectingVisualBrushCaches) 
									{
										// TODO: This is slower than it needs to be
										int otherIndex2 = Array.IndexOf(allBrushCaches, brushCache2);
										if(otherIndex2 != -1)
										{
											if(!shouldBuildVisible[otherIndex2] && brushes[otherIndex2].IsVisible)
											{
												shouldBuildVisible[otherIndex2] = true;
												brushesToBuild++;
											}
										}
									}
								}
							}
						}
					}
				}
			}

			for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
			{
				if(!allBrushCaches[brushIndex].Built && brushes[brushIndex].HasCollision)
				{
					shouldBuildCollision[brushIndex] = true;
					// TODO: This is slower than it needs to be
					foreach (BrushCache brushCache in allBrushCaches[brushIndex].IntersectingCollisionBrushCaches) 
					{
						int otherIndex = Array.IndexOf(allBrushCaches, brushCache);
						if(otherIndex != -1)
						{
							if(!shouldBuildCollision[otherIndex] && brushes[otherIndex].HasCollision)
							{
								shouldBuildCollision[otherIndex] = true;
								brushesToBuild++;

								if(brushes[otherIndex].Mode == CSGMode.Subtract)
								{
									foreach (BrushCache brushCache2 in allBrushCaches[otherIndex].IntersectingCollisionBrushCaches) 
									{
										// TODO: This is slower than it needs to be
										int otherIndex2 = Array.IndexOf(allBrushCaches, brushCache2);
										if(otherIndex2 != -1)
										{
											if(!shouldBuildCollision[otherIndex2] && brushes[otherIndex2].HasCollision)
											{
												shouldBuildCollision[otherIndex2] = true;
												brushesToBuild++;
											}
										}
									}
								}
							}
						}
					}
				}
			}

			for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
			{
				if(shouldBuildVisible[brushIndex] || shouldBuildCollision[brushIndex])
				{
					allBrushCaches[brushIndex].ResetForBuild(newVisualPolygonIndex, newCollisionPolygonIndex);
				}
			}
			bool showProgressBar = false;

			if(DateTime.Now - buildStartTime > new TimeSpan(0,0,1))
			{
				showProgressBar = true;
				if(state == null && onProgressChange != null) // Can only fire on main thread
				{
					onProgressChange(0);
				}
			}

			// TODO: All this should be possible to parallelise.
			for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
			{
				if(shouldBuildVisible[brushIndex])
				{
					// Intersecting builders can probably be calculated at edit time
					BrushBuilder.Build(allBrushCaches[brushIndex], brushIndex, allBrushCaches, false);

                    // Build volume brushes.
                    BuildVolumes(brushIndex);

                    brushesBuilt++;

					// If we are not required to build collision (either for this brush, or at all) then we've built it!
					if(!shouldBuildCollision[brushIndex] || !buildSettings.GenerateCollisionMeshes)
					{
						allBrushCaches[brushIndex].SetBuilt();
					}

					if(showProgressBar)
					{
						if(state == null && onProgressChange != null) // Can only fire on main thread
						{
							onProgressChange(brushesBuilt / (float)brushesToBuild);
						}
					}
					else
					{
						if(DateTime.Now - buildStartTime > new TimeSpan(0,0,1))
						{
							showProgressBar = true;
							if(state == null && onProgressChange != null) // Can only fire on main thread
							{
								onProgressChange(brushesBuilt / (float)brushesToBuild);
							}
						}
					}
				}
			}

			for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
			{
				if(shouldBuildVisible[brushIndex])
				{
					// Intersecting builders can probably be calculated at edit time
					BrushCache.ReclaimStolenVisualPolygons(allBrushCaches[brushIndex]);
				}
			}

			if(buildSettings.GenerateCollisionMeshes)
			{
				for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
				{
					if(shouldBuildCollision[brushIndex])
					{
						if(allBrushCaches[brushIndex].CollisionVisualEqual)
						{

						}
						else
						{
							// Intersecting builders can probably be calculated at edit time
							BrushBuilder.Build(allBrushCaches[brushIndex], brushIndex, allBrushCaches, true);

							brushesBuilt++;

							if(showProgressBar)
							{
								if(state == null && onProgressChange != null) // Can only fire on main thread
								{
									onProgressChange(brushesBuilt / (float)brushesToBuild);
								}
							}
							else
							{
								if(DateTime.Now - buildStartTime > new TimeSpan(0,0,1))
								{
									showProgressBar = true;
									if(state == null && onProgressChange != null) // Can only fire on main thread
									{
										onProgressChange(brushesBuilt / (float)brushesToBuild);
									}
								}
							}
						}

						allBrushCaches[brushIndex].SetBuilt();
					}
				}

				for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
				{
					if(shouldBuildCollision[brushIndex])
					{
						if(!allBrushCaches[brushIndex].CollisionVisualEqual)
						{
							// Intersecting builders can probably be calculated at edit time
							BrushCache.ReclaimStolenCollisionPolygons(allBrushCaches[brushIndex]);
						}
					}
				}
			}

			time1 = DateTime.Now;

			// TODO: Can parallelise the vertex/index buffer generation, built putting them into mesh needs to be main thread
			// Triangulate the new polygons
			if(brushesBuilt > 0)
			{
                //VisualDebug.ClearAll();

                Dictionary<int, List<Polygon>> allGroupedPolygons = new Dictionary<int, List<Polygon>>();

				for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
				{
					if(shouldBuildVisible[brushIndex])
					{
						// Grab the polygons grouped by ID, optimizing them if requested
						Dictionary<int, List<Polygon>> groupedPolygons =  null;
                        if (buildSettings.OptimizeGeometry)
						{
							// Return the optimal list of polygons (this call also updates BuiltVisualPolygons)
							groupedPolygons = BrushCache.OptimizeVisual(allBrushCaches[brushIndex]);
						}
						else
                        {
							groupedPolygons = allBrushCaches[brushIndex].GetGroupedBuiltVisualPolygons();
						}

						// Set the visual mapping from the built polygons (used by SurfaceEditor)
						List<Polygon> polygons = allBrushCaches[brushIndex].BuiltVisualPolygons;
						buildContext.SetVisualMapping(brushes[brushIndex], polygons);

						// Add each set of polygons to the dictionary to be triangulated
                        foreach (KeyValuePair<int, List<Polygon>> row in groupedPolygons)
                        {
                            allGroupedPolygons.Add(row.Key, row.Value);
                        }
                    }
				}

				bool useIndividualVertices = buildSettings.GenerateLightmapUVs; // Generate individual vertices for unwrapped geometry

				MeshGroupManager.TriangulateNewPolygons(useIndividualVertices, allGroupedPolygons, buildContext.VisualPolygonIndex);

				if(buildSettings.GenerateCollisionMeshes)
				{
					allGroupedPolygons.Clear();

					for (int brushIndex = 0; brushIndex < allBrushCaches.Length; brushIndex++) 
					{
						if(shouldBuildCollision[brushIndex])
						{
							if(allBrushCaches[brushIndex].CollisionVisualEqual)
							{
								// Intersection sets equal, so just copy visual triangulation
								List<Polygon> builtVisualPolygons = allBrushCaches[brushIndex].BuiltVisualPolygons;
								for (int i = 0; i < builtVisualPolygons.Count; i++) 
								{
									PolygonEntry visualPolygon = buildContext.VisualPolygonIndex[builtVisualPolygons[i].UniqueIndex];
									if(visualPolygon != null)
									{
										buildContext.CollisionPolygonIndex[builtVisualPolygons[i].UniqueIndex] = visualPolygon.DeepCopy();
									}
								}
							}
							else
							{
								// Intersection sets are different, need to triangulate separately
								Dictionary<int, List<Polygon>> groupedPolygons =  null;
                                if(buildSettings.OptimizeGeometry)
								{
									// Return the optimal list of polygons (this call also updates BuiltCollisionPolygons)
									groupedPolygons = BrushCache.OptimizeCollision(allBrushCaches[brushIndex]);
								}
								else
								{
									groupedPolygons = allBrushCaches[brushIndex].GetGroupedBuiltCollisionPolygons();
								}

								// Add each set of polygons to the dictionary to be triangulated
								foreach (KeyValuePair<int, List<Polygon>> row in groupedPolygons)
								{
									allGroupedPolygons.Add(row.Key, row.Value);
								}
							}
						}
					}

					useIndividualVertices = false; // Never use individual vertices for collision geometry
					MeshGroupManager.TriangulateNewPolygons(useIndividualVertices, allGroupedPolygons, buildContext.CollisionPolygonIndex);
				}
			}

			// If multithreaded
			if(state != null)
			{
				// All done, tell the main thread to finish the build up
				readyForFinalize = true;
			}
		}

		internal static bool FinalizeBuild()
		{
			if(brushesBuilt > 0)
			{
				buildContext.WriteVisualMappings();
			}

			// Ensure a mesh group exists
			bool newGroupCreated = Prepare(rootTransform);
			if(brushesBuilt > 0 || polygonsRemoved || newGroupCreated)
			{
				MeshGroupManager.Cleanup(meshGroupHolder);

				MeshGroupManager.BuildVisual(meshGroupHolder, buildContext.VisualPolygonIndex, buildSettings, buildContext, materialMeshDictionary);

				if(buildSettings.GenerateCollisionMeshes)
				{
					MeshGroupManager.BuildCollision(meshGroupHolder, buildContext.CollisionPolygonIndex, buildSettings, collisionMeshDictionary);
				}

                // All done
                DateTime time2 = DateTime.Now;

				buildContext.buildMetrics.BuildMetaData = (time1-buildStartTime).TotalSeconds + " " + (time2-time1).TotalSeconds + " " + brushesBuilt;
				buildContext.buildMetrics.BuildTime = (float)(DateTime.Now - buildStartTime).TotalSeconds;
				buildInProgress = false;
				return true;
			}
			else
			{
				buildInProgress = false;
				return false;
			}
		}

		internal static bool Tick()
		{
			if(readyForFinalize)
			{
				readyForFinalize = false;
				return FinalizeBuild();
			}
			else
			{
				return false;
			}
		}

        internal static void BuildVolumes(int brushIndex)
        {
            // remove volumes from brushes that are no longer volumes:
            if (brushes[brushIndex].Mode != CSGMode.Volume && brushes[brushIndex].Volume != null)
            {
                // set volume handle to null.
                brushes[brushIndex].Volume = null;
                // delete any built volume.
                Transform volume1 = brushes[brushIndex].transform.Find(Constants.GameObjectVolumeComponentIdentifier);
                if (volume1 != null)
                    GameObject.DestroyImmediate(volume1.gameObject);
            }

            // generate all of the volume brushes:
            if (brushes[brushIndex].Mode == CSGMode.Volume && brushes[brushIndex].Volume != null)
            {
                Volume volume = brushes[brushIndex].Volume;
                if (volume != null)
                {
                    // remove any existing built volume:
                    Transform volume2 = brushes[brushIndex].transform.Find(Constants.GameObjectVolumeComponentIdentifier);
                    if (volume2 != null)
                        GameObject.DestroyImmediate(volume2.gameObject);

                    // create the game object with convex mesh collider:
                    Mesh mesh = new Mesh();
                    BrushFactory.GenerateMeshFromPolygonsFast(brushes[brushIndex].GetPolygons(), ref mesh, 0.0f);
                    GameObject gameObject = CreateVolumeMesh(brushes[brushIndex].transform, mesh);
                    gameObject.transform.position = brushes[brushIndex].transform.position;
                    gameObject.transform.rotation = brushes[brushIndex].transform.rotation;

                    // execute custom volume generation code:
                    volume.OnCreateVolume(gameObject);
                }
            }
        }

       
            

            public static GameObject CreateMaterialMesh(Transform rootTransform, Material material, Mesh mesh)
            {
                meshGroup = rootTransform.Find("MeshGroup");
                // Create a grouping object which will act as a parent for all the per material meshes
                if (meshGroup == null)
                {
                    meshGroup = new GameObject("MeshGroup").transform;
                    meshGroup.parent = rootTransform;
                }

                GameObject materialMesh = new GameObject("MaterialMesh", typeof(MeshFilter), typeof(MeshRenderer));
                materialMesh.transform.SetParent(meshGroup, false);

                // Set the mesh to be rendered
                materialMesh.GetComponent<MeshFilter>().sharedMesh = mesh;

                materialMesh.GetComponent<Renderer>().material = material;

                return materialMesh;
            }

            public static GameObject CreateCollisionMesh(Transform rootTransform, Mesh mesh)
            {
                meshGroup = rootTransform.Find("MeshGroup");
                // Create a grouping object which will act as a parent for all the per material meshes
                if (meshGroup == null)
                {
                    meshGroup = new GameObject("MeshGroup").transform;
                    meshGroup.parent = rootTransform;
                }

                GameObject colliderMesh = new GameObject("CollisionMesh", typeof(MeshCollider));
                colliderMesh.transform.SetParent(meshGroup, false);

                // Set the mesh to be rendered
                colliderMesh.GetComponent<MeshCollider>().sharedMesh = mesh;

                return colliderMesh;
            }

            public static GameObject CreateVolumeMesh(Transform parent, Mesh mesh)
            {
                GameObject volumeMesh = new GameObject(Constants.GameObjectVolumeComponentIdentifier, typeof(MeshCollider));
                volumeMesh.transform.SetParent(parent, false);
#if UNITY_EDITOR
                if (!CurrentSettings.ShowHiddenGameObjectsInInspector)
                        volumeMesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
#endif

                // Set the mesh to be used for triggers.
                MeshCollider meshCollider = volumeMesh.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = true;
                meshCollider.isTrigger = true;

                return volumeMesh;
            }
    }
    }
#endif