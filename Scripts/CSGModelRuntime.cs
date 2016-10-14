using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[ExecuteInEditMode]
	public class CSGModelRuntime : MonoBehaviour
	{
	    void Start()
	    {	
			// CSGModelRuntime is now obsolete, strip the component from old models that used it
			DestroyImmediate(this);
	    }
	}
}