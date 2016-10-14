#if UNITY_EDITOR || RUNTIME_CSG
using System;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public interface IPostBuildListener
	{
		void OnBuildFinished(Transform meshGroupTransform);
	}
}
#endif