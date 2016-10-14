#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace Sabresaurus.SabreCSG
{
	public static class SabreGUIHelper
	{
		public static bool AnyControlFocussed
		{
			get
			{
				string focusedControl = GUI.GetNameOfFocusedControl();
				// A control is considered focused if there is a named control reporting as focused.
				// Note we disregard translation handles which report their active axis
				if(string.IsNullOrEmpty(focusedControl) 
					|| focusedControl == "xAxis" 
					|| focusedControl == "yAxis"
					|| focusedControl == "zAxis")
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}
	}
}
#endif