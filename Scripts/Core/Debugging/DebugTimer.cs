#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.SabreCSG
{
	internal static class DebugTimer
	{
#if DEBUG_CSG
		static DateTime startTime;
		static DateTime lastEvent = DateTime.MinValue;
#endif

		[System.Diagnostics.Conditional("DEBUG_CSG")]
	    public static void StartTimer()
		{
#if DEBUG_CSG
			startTime = DateTime.UtcNow;
			lastEvent =  DateTime.UtcNow;
	        Debug.Log("Started timer");
#endif
	    }

		[System.Diagnostics.Conditional("DEBUG_CSG")]
	    public static void LogEvent(string message)
	    {
#if DEBUG_CSG			
	        Debug.Log("Event " + (DateTime.UtcNow - startTime) + " " + (DateTime.UtcNow - lastEvent) + " - " + message);
	        lastEvent = DateTime.UtcNow;
#endif
	    }
	}
}
#endif