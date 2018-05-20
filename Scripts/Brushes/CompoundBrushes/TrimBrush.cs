#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[ExecuteInEditMode]
	public class TrimBrush : CompoundBrush
	{
        /// <summary>
        /// The size of the trim.
        /// </summary>
        [SerializeField]
		float trimSize = 0.25f;

        /// <summary>
        /// Gets or sets the size of the trim.
        /// </summary>
        /// <value>The size of the trim.</value>
        public float TrimSize { get { return trimSize; } set { trimSize = value; } }

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public override string BeautifulBrushName
        {
            get
            {
                return "Trim Brush";
            }
        }

        public override int BrushCount
		{
			get 
			{
				// If the brush is too small for trims, just default to a single brush to not break things ok!
                return (localBounds.size.x > trimSize * 2.0f && localBounds.size.z > trimSize * 2.0f)?5:1;
			}
		}

		public override void UpdateVisibility ()
		{
		}

		Polygon[] GeneratePolys(int index) {
			Polygon[] output = generatedBrushes[index].GetPolygons();

			float xNorm = Mathf.Cos(index * Mathf.PI * 0.5f);
			float zNorm = -Mathf.Sin(index * Mathf.PI * 0.5f);

			Vector3 frontOuterTop = new Vector3(
				localBounds.center.x + (localBounds.size.x * 0.5f * xNorm) + (localBounds.size.x * 0.5f * zNorm), 
				localBounds.max.y, 
				localBounds.center.z + (localBounds.size.z * 0.5f * xNorm) + (localBounds.size.z * 0.5f * zNorm)
			);

			Vector3 frontOuterBottom = frontOuterTop; frontOuterBottom.y = localBounds.min.y;
			
			Vector3 backOuterTop = new Vector3(
				localBounds.center.x + (localBounds.size.x * 0.5f * xNorm) + (localBounds.size.x * 0.5f * -zNorm), 
				localBounds.max.y, 
				localBounds.center.z + (localBounds.size.z * 0.5f * -xNorm) + (localBounds.size.z * 0.5f * zNorm)
			);

			Vector3 backOuterBottom = backOuterTop; backOuterBottom.y = localBounds.min.y;

			Vector3 frontInnerTop = new Vector3(
				localBounds.center.x + ((localBounds.size.x - trimSize * 2.0f) * 0.5f * xNorm) + ((localBounds.size.x - trimSize * 2.0f) * 0.5f * zNorm), 
				localBounds.max.y, 
				localBounds.center.z + ((localBounds.size.z - trimSize * 2.0f) * 0.5f * xNorm) + ((localBounds.size.z - trimSize * 2.0f) * 0.5f * zNorm)
			);

			Vector3 frontInnerBottom = frontInnerTop; frontInnerBottom.y = localBounds.min.y;
			
			Vector3 backInnerTop = new Vector3(
				localBounds.center.x + ((localBounds.size.x - trimSize * 2.0f) * 0.5f * xNorm) + ((localBounds.size.x - trimSize * 2.0f) * 0.5f * -zNorm), 
				localBounds.max.y, 
				localBounds.center.z + ((localBounds.size.z - trimSize * 2.0f) * 0.5f * -xNorm) + ((localBounds.size.z - trimSize * 2.0f) * 0.5f * zNorm)
			);

			Vector3 backInnerBottom = backInnerTop; backInnerBottom.y = localBounds.min.y;

			// Outer Plane
			output[0].Vertices[0].Position = frontOuterTop;
			output[0].Vertices[1].Position = frontOuterBottom;
			output[0].Vertices[2].Position = backOuterBottom;
			output[0].Vertices[3].Position = backOuterTop;

			// Inner Plane
			output[1].Vertices[0].Position = frontInnerTop;			
			output[1].Vertices[1].Position = backInnerTop;			
			output[1].Vertices[2].Position = backInnerBottom;			
			output[1].Vertices[3].Position = frontInnerBottom;

			// Top Plane
			output[2].Vertices[0].Position = frontInnerTop;			
			output[2].Vertices[1].Position = frontOuterTop;			
			output[2].Vertices[2].Position = backOuterTop;			
			output[2].Vertices[3].Position = backInnerTop;

			// Bottom Plane			
			output[3].Vertices[0].Position = frontOuterBottom;
			output[3].Vertices[1].Position = frontInnerBottom;			
			output[3].Vertices[2].Position = backInnerBottom;			
			output[3].Vertices[3].Position = backOuterBottom;	

			// Front Plane
			output[4].Vertices[0].Position = frontInnerTop;
			output[4].Vertices[1].Position = frontInnerBottom;			
			output[4].Vertices[2].Position = frontOuterBottom;			
			output[4].Vertices[3].Position = frontOuterTop;	

			// Back Plane
			output[5].Vertices[0].Position = backInnerBottom;
			output[5].Vertices[1].Position = backInnerTop;			
			output[5].Vertices[2].Position = backOuterTop;			
			output[5].Vertices[3].Position = backOuterBottom;

			

			for (int i = 0; i < 6; i++) {
				if (index % 2 == 1) Array.Reverse(output[i].Vertices);
				GenerateNormals(output[i]);
				GenerateUvCoordinates(output[i]);
			}

			return output;
		}

		/// <summary>
        /// Generates the UV coordinates for a <see cref="Polygon"/> automatically.
        /// </summary>
        /// <param name="polygon">The polygon to be updated.</param>
        private void GenerateUvCoordinates(Polygon polygon)
        {
            // stolen code from the surface editor "AutoUV".
            Vector3 planeNormal = polygon.Plane.normal;
            Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-planeNormal));
            // Sets the UV at each point to the position on the plane
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position = polygon.Vertices[i].Position;
                Vector2 uv = (cancellingRotation * position) * 0.5f;
                polygon.Vertices[i].UV = uv;
            }
        }

        private void GenerateNormals(Polygon polygon)
        {
            Plane plane = new Plane(polygon.Vertices[1].Position, polygon.Vertices[2].Position, polygon.Vertices[3].Position);
            foreach (Vertex vertex in polygon.Vertices)
                vertex.Normal = plane.normal;
        }

		public override void Invalidate (bool polygonsChanged)
		{
			base.Invalidate(polygonsChanged);

			for (int i = 0; i < BrushCount; i++) {
				generatedBrushes[i].Mode = this.Mode;
				generatedBrushes[i].IsNoCSG = this.IsNoCSG;
				generatedBrushes[i].IsVisible = this.IsVisible;
				generatedBrushes[i].HasCollision = this.HasCollision;
			}

			Vector3 baseSize;
			if (localBounds.size.x > trimSize * 2.0f && localBounds.size.z > trimSize * 2.0f) {
				float sizeX = localBounds.size.x - trimSize * 2.0f;
				float sizeZ = localBounds.size.z - trimSize * 2.0f;
				baseSize = new Vector3(sizeX, localBounds.size.y, sizeZ);

				// Build the trims here
				for (int i = 1; i < BrushCount; i++) {
					// Vector3 brushPos = localBounds.center;
					// brushPos.x += Mathf.Sin(i * Mathf.PI * 0.5f) * ((localBounds.size.x - trimSize) * 0.5f);
					// brushPos.z += Mathf.Cos(i * Mathf.PI * 0.5f) * ((localBounds.size.z - trimSize) * 0.5f);
					// generatedBrushes[i].transform.localPosition = brushPos;

					Vector3 brushSize = new Vector3();
					if (i % 2 == 0) {
						brushSize.x = sizeX;
						brushSize.z = trimSize;
					} else {
						brushSize.x = trimSize;
						brushSize.z = sizeZ;
					}
					brushSize.y = localBounds.size.y;
					BrushUtility.Resize(generatedBrushes[i], brushSize);

					generatedBrushes[i].SetPolygons(GeneratePolys(i));
					generatedBrushes[i].Invalidate(true);
				}
			} else {
				baseSize = localBounds.size;
			}
			
			generatedBrushes[0].transform.localPosition = localBounds.center;
			BrushUtility.Resize(generatedBrushes[0], baseSize);
		}

	}
}
#endif