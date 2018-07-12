#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [ExecuteInEditMode]
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
                // If the brush is too small for walls, just default to a single brush.
                return (IsBrushXYZTooSmall) ? 2 : 1;
            }
        }

        /// <summary>
        /// Is the size of the bounds X, Y, and Z above the minimum size?
        /// </summary>
        /// <returns></returns>
        public bool IsBrushXYZTooSmall
        {
            get
            {
                return localBounds.size.x > wallThickness * 2.0f && localBounds.size.z > wallThickness * 2.0f && localBounds.size.y > wallThickness * 2;
            }
        }

        /// <summary>
        /// The thickness of the walls.
        /// </summary>
        [SerializeField]
        private float wallThickness = 0.25f;

        public override void UpdateVisibility()
        {
        }

        public override void Invalidate(bool polygonsChanged)
        {
            base.Invalidate(polygonsChanged);

            for (int i = 0; i < BrushCount; i++)
            {
                generatedBrushes[i].Mode = this.Mode;
                generatedBrushes[i].IsNoCSG = this.IsNoCSG;
                generatedBrushes[i].IsVisible = this.IsVisible;
                generatedBrushes[i].HasCollision = this.HasCollision;
            }

            if (IsBrushXYZTooSmall)
            {
                generatedBrushes[0].Mode = CSGMode.Add;
                BrushUtility.Resize(generatedBrushes[0], localBounds.size);

                generatedBrushes[1].Mode = CSGMode.Subtract;
                BrushUtility.Resize(generatedBrushes[1], localBounds.size - new Vector3(wallThickness * 2, wallThickness * 2, wallThickness * 2));

                for (int i = 0; i < BrushCount; i++)
                {
                    generatedBrushes[i].SetPolygons(GeneratePolys(i));
                    generatedBrushes[i].Invalidate(true);
                }
            }
            else
            {
                BrushUtility.Resize(generatedBrushes[0], localBounds.size);
            }
        }

        private Polygon[] GeneratePolys(int index)
        {
            Polygon[] output = generatedBrushes[index].GetPolygons();

            for (int i = 0; i < 6; i++)
            {
                output[i].ResetVertexNormals();
                output[i].GenerateUvCoordinates();
            }

            return output;
        }
    }
}

#endif