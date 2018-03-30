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
        /// <summary>
        /// The depth of each step.
        /// </summary>
        [SerializeField]
        float stepDepth = 0.2f;

        /// <summary>
        /// Gets or sets the depth of each step.
        /// </summary>
        /// <value>The depth of each step.</value>
        public float StepDepth { get { return stepDepth; } set { stepDepth = value; } }

        /// <summary>
        /// The height of each step.
        /// </summary>
        [SerializeField]
	    float stepHeight = 0.1f;

        /// <summary>
        /// Gets or sets the height of each step.
        /// </summary>
        /// <value>The height of each step.</value>
        public float StepHeight { get { return stepHeight; } set { stepHeight = value; } }

        /// <summary>
        /// The step depth spacing.
        /// </summary>
        [SerializeField]
        float stepDepthSpacing = 0f;

        /// <summary>
        /// Gets or sets the step depth spacing.
        /// </summary>
        /// <value>The step depth spacing.</value>
        public float StepDepthSpacing { get { return stepDepthSpacing; } set { stepDepthSpacing = value; } }

        /// <summary>
        /// The step height spacing.
        /// </summary>
        [SerializeField]
        float stepHeightSpacing = 0f;

        /// <summary>
        /// Gets or sets the step height spacing.
        /// </summary>
        /// <value>The step height spacing.</value>
        public float StepHeightSpacing { get { return stepHeightSpacing; } set { stepHeightSpacing = value; } }

        /// <summary>
        /// Whether to automatically determine the best step depth.
        /// </summary>
        [SerializeField]
		bool autoDepth = false;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically determine the best step depth.
        /// </summary>
        /// <value><c>true</c> to automatically determine the best step depth; otherwise, <c>false</c>.</value>
        public bool AutomaticDepth { get { return autoDepth; } set { autoDepth = value; } }

        /// <summary>
        /// Whether to automatically determine the best step height.
        /// </summary>
        [SerializeField]
		bool autoHeight = false;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically determine the best step height.
        /// </summary>
        /// <value><c>true</c> to automatically determine the best step height; otherwise, <c>false</c>.</value>
        public bool AutomaticHeight { get { return autoDepth; } set { autoDepth = value; } }

        /// <summary>
        /// Whether to lead from the top.
        /// </summary>
        [SerializeField]
		bool leadFromTop = false;

        /// <summary>
        /// Gets or sets a value indicating whether to lead from the top.
        /// </summary>
        /// <value><c>true</c> to lead from the top; otherwise, <c>false</c>.</value>
        public bool LeadFromTop { get { return leadFromTop; } set { leadFromTop = value; } }

        /// <summary>
        /// Whether to fill steps to the bottom to make a solid staircase.
        /// </summary>
        [SerializeField]
        bool fillToBottom = false;

        /// <summary>
        /// Gets or sets a value indicating whether to fill steps to the bottom to make a solid staircase.
        /// </summary>
        /// <value><c>true</c> to fill steps to the bottom to make a solid staircase; otherwise, <c>false</c>.</value>
        public bool FillToBottom { get { return fillToBottom; } set { fillToBottom = value; } }

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public override string BeautifulBrushName
        {
            get
            {
                return "Linear Stairs Brush";
            }
        }

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