namespace Sabresaurus.SabreCSG
{
	using System.Collections.Generic;
	using System;
	using UnityEngine;

	public class HollowBoxBrush : CompoundBrush
	{
		/// <summary>
		/// Gets or sets the thickness of the walls.
		/// </summary>
		/// <value>The thickness of the walls.</value>
		public float WallThickness
		{
			get
			{
				return wallThickness;
			}
			set
			{
				wallThickness = value;
			}
		}

		public float BrushSize
		{
			get
			{
				return brushSize;
			}
			set
			{
				brushSize = value;
			}
		}

		/// <summary>
		/// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
		/// </summary>
		/// <value>The beautiful name of the brush.</value>
		public override string BeautifulBrushName
		{
			get
			{
				return "Hollow Box Brush";
			}
		}

		public override int BrushCount
		{
			get
			{
				// If the brush is too small for walls, just default to a single brush to not break things ok!
				return ( localBounds.size.x > wallThickness * 2.0f &&
					localBounds.size.y > wallThickness * 2.0f &&
					localBounds.size.z > wallThickness * 2.0f ) ? 2 : 1;
			}
		}

		/// <summary>
		/// The thickness of the walls.
		/// </summary>
		[SerializeField]
		private float wallThickness = 0.25f;
		
		/// <summary>
		/// The size of the brush bounds, set by inspector [set] button.
		/// </summary>
		[SerializeField]
		private float brushSize = 2.0f;

		public override void UpdateVisibility()
		{
		}

		public override void Invalidate( bool polygonsChanged )
		{
			base.Invalidate( polygonsChanged );

			for( int i = 0; i < BrushCount; i++ )
			{
				generatedBrushes[i].Mode = this.Mode;
				generatedBrushes[i].IsNoCSG = this.IsNoCSG;
				generatedBrushes[i].IsVisible = this.IsVisible;
				generatedBrushes[i].HasCollision = this.HasCollision;
			}

			if( localBounds.size.x > wallThickness * 2.0f &&
				localBounds.size.y > wallThickness * 2.0f &&
				localBounds.size.z > wallThickness * 2.0f )
			{
				localBounds.size = Vector3.one * brushSize;

				Vector3 baseSize = localBounds.size;

				generatedBrushes[0].Mode = CSGMode.Add;
				BrushUtility.Resize( generatedBrushes[0], baseSize );

				generatedBrushes[1].Mode = CSGMode.Subtract;
				BrushUtility.Resize( generatedBrushes[1], baseSize - new Vector3( wallThickness * 2, wallThickness * 2, wallThickness * 2 ) );

				for( int i = 0; i < BrushCount; i++ )
				{
					generatedBrushes[i].SetPolygons( GeneratePolys( i ) );
					generatedBrushes[i].Invalidate( true );
				}
			}
		}

		private Polygon[] GeneratePolys( int index )
		{
			Polygon[] output = generatedBrushes[index].GetPolygons();

			for( int i = 0; i < 6; i++ )
			{
				GenerateNormals( output[i] );
				GenerateUvCoordinates( output[i] );
			}

			return output;
		}

		/// <summary>
		/// Generates the UV coordinates for a <see cref="Polygon"/> automatically.
		/// </summary>
		/// <param name="polygon">The polygon to be updated.</param>
		private void GenerateUvCoordinates( Polygon polygon )
		{
			// stolen code from the surface editor "AutoUV".
			Vector3 planeNormal = polygon.Plane.normal;
			Quaternion cancellingRotation = Quaternion.Inverse( Quaternion.LookRotation( -planeNormal ) );
			// Sets the UV at each point to the position on the plane
			for( int i = 0; i < polygon.Vertices.Length; i++ )
			{
				Vector3 position = polygon.Vertices[i].Position;
				Vector2 uv = ( cancellingRotation * position ) * 0.5f;
				polygon.Vertices[i].UV = uv;
			}
		}

		private void GenerateNormals( Polygon polygon )
		{
			Plane plane = new Plane( polygon.Vertices[1].Position, polygon.Vertices[2].Position, polygon.Vertices[3].Position );

			foreach( Vertex vertex in polygon.Vertices )
			{
				vertex.Normal = plane.normal;
			}
		}
	}
}
