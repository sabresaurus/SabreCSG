#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.SabreCSG
{
	[Serializable]
	public class BuildMetrics 
	{
		public int TotalMeshes = 0;
		public int TotalVertices = 0;
		public int TotalTriangles = 0;
		public float BuildTime = 0; 
		public string BuildMetaData = "";
	}
}
#endif