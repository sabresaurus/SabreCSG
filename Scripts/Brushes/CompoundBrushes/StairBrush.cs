#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[ExecuteInEditMode]
	public class StairBrush : CompoundBrush
	{
        [SerializeField]
        float stepDepth = 0.2f;

        [SerializeField]
	    float stepHeight = 0.1f;

        [SerializeField]
        float stepDepthSpacing = 0f;

        [SerializeField]
        float stepHeightSpacing = 0f;

		[SerializeField]
		bool autoDepth = false;

        [SerializeField]
		bool autoHeight = false;

		[SerializeField]
		bool leadFromTop = false;

        [SerializeField]
        bool fillToBottom = false;

        public override int BrushCount 
		{
			get 
			{
                // Count the maximum number of steps in each dimension
                int depthCount = 1 + Mathf.FloorToInt((localBounds.size.z - stepDepth + 0.001f) / (stepDepth + stepDepthSpacing));
                int heightCount = 1 + Mathf.FloorToInt((localBounds.size.y - stepHeight + 0.001f) / (stepHeight + stepHeightSpacing));

                // Return the smaller step count
                return Mathf.Min(depthCount, heightCount);
			}
		}

		public override void UpdateVisibility ()
		{
		}

		public override void Invalidate (bool polygonsChanged)
		{
			base.Invalidate(polygonsChanged);

			int brushCount = BrushCount;
			float activeHeight = stepHeight;
			float activeDepth = stepDepth;

			if(autoHeight)
			{
				activeHeight = localBounds.size.y / brushCount;
			}
			if(autoDepth)
			{
				activeDepth = localBounds.size.z / brushCount;
			}

			Vector3 stepSize = new Vector3(localBounds.size.x, activeHeight, activeDepth);


			Vector3 startPosition = localBounds.center;
			if(leadFromTop)
			{
				startPosition.y += stepSize.y/2f + localBounds.size.y/2f - stepSize.y * brushCount;
				startPosition.z += stepSize.z/2f + localBounds.size.z/2f - stepSize.z * brushCount;
			}
			else
			{
				startPosition.y += stepSize.y/2f - localBounds.size.y/2f;
				startPosition.z += stepSize.z/2f - localBounds.size.z/2f;				
			}
				
			for (int i = 0; i < brushCount; i++) 
			{
                Vector3 localPosition = startPosition + Vector3.forward * i * (activeDepth + stepDepthSpacing) + Vector3.up * i * (activeHeight + stepHeightSpacing) * (fillToBottom ? 0.5f : 1f);
                generatedBrushes[i].transform.localPosition = localPosition;

				generatedBrushes[i].Mode = this.Mode;
				generatedBrushes[i].IsNoCSG = this.IsNoCSG;
				generatedBrushes[i].IsVisible = this.IsVisible;
				generatedBrushes[i].HasCollision = this.HasCollision;
				BrushUtility.Resize(generatedBrushes[i], stepSize);

                if (fillToBottom)
                    stepSize.y += (activeHeight) + stepHeightSpacing;
			}
		}
	}
}

#endif