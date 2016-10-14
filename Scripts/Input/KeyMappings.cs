#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Globalization;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Central store for SabreCSG key mappings, change these on the ScriptableObject in Unity to change shortcuts
	/// </summary>
	public class KeyMappings : ScriptableObject
	{
		// See http://unity3d.com/support/documentation/ScriptReference/MenuItem.html for shortcut format

		private static KeyMappings instance = null;

		public static KeyMappings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = (KeyMappings)AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "KeyMappings.asset");
				}

				return instance;
			}
		}


		//[Header("See http://unity3d.com/support/documentation/ScriptReference/MenuItem.html for shortcut format")]
		[Header("Top Toolbar")]
		public string ToggleMode = "Space";
		public string ToggleModeBack = "#Space";

		public string ActivateDrawMode = "d";

		public string ActivateClipMode = "c";

		[Header("Main Toolbar")]
		public string TogglePosSnapping = "/";
		public string DecreasePosSnapping = ",";
		public string IncreasePosSnapping = ".";

		public string ToggleAngSnapping = "#/";
		public string DecreaseAngSnapping = "#,";
		public string IncreaseAngSnapping = "#.";

		public string ToggleBrushesHidden = "h";

		[Header("General")]
		public string ChangeBrushToAdditive = "a";
		public string ChangeBrushToAdditive2 = "KeypadPlus";

		public string ChangeBrushToSubtractive = "s";
		public string ChangeBrushToSubtractive2 = "KeypadMinus";

		public string Group = "g";
		public string Ungroup = "#g";

		public string EnableRadialMenu = "j";

		[Header("Clip Tool")]
		public string ApplyClip = "Return";
		public string ApplySplit = "#Return";
		public string InsertEdgeLoop = "l";
		public string FlipPlane = "r";

		[Header("Face Tool")]
		public string CopyMaterial = "c";

		[Header("Shared between tools")]
		public string CancelCurrentOperation = "Escape";
		public string Back = "Backspace";
		public string Delete = "Delete";

		// Used in UtilityShortcuts.cs with MenuItem attribute
		public const string Rebuild = "%#r";


		public static bool EventsMatch(Event event1, Event event2)
		{
			return EventsMatch(event1, event2, false, false);
		}

		/// <summary>
		/// Helper method to determine if two keyboard events match
		/// </summary>
		public static bool EventsMatch(Event event1, Event event2, bool ignoreShift, bool ignoreFunction)
		{
			EventModifiers modifiers1 = event1.modifiers;
			EventModifiers modifiers2 = event2.modifiers;

			// Ignore capslock from either modifier
			modifiers1 &= (~EventModifiers.CapsLock);
			modifiers2 &= (~EventModifiers.CapsLock);

			if(ignoreShift)
			{
				// Ignore shift from either modifier
				modifiers1 &= (~EventModifiers.Shift);
				modifiers2 &= (~EventModifiers.Shift);
			}

			// If key code and modifier match
			if(event1.keyCode == event2.keyCode
				&& (modifiers1 == modifiers2))
			{
				return true;
			}

			return false;
		}


	}
}
#endif