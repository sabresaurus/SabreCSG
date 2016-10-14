#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Helper for accessing Unity's built in key mappings
	/// </summary>
	public static class EditorKeyMappings
	{
		const string TOOL_VIEW_KEY = "Tools/View";
		const string TOOL_MOVE_KEY = "Tools/Move";
		const string TOOL_ROTATE_KEY = "Tools/Rotate";

		const string TOOL_VIEW_DEFAULT = "Q";
		const string TOOL_MOVE_DEFAULT = "W";
		const string TOOL_ROTATE_DEFAULT = "E";

		const string VIEW_FORWARD_KEY = "View/FPS Forward";
		const string VIEW_BACK_KEY = "View/FPS Back";
		const string VIEW_STRAFE_LEFT_KEY = "View/FPS Strafe Left";
		const string VIEW_STRAFE_RIGHT_KEY = "View/FPS Strafe Right";
		const string VIEW_STRAFE_UP_KEY = "View/FPS Strafe Up";
		const string VIEW_STRAFE_DOWN_KEY = "View/FPS Strafe Down";

		const string VIEW_FORWARD_DEFAULT = "W";
		const string VIEW_BACK_DEFAULT = "S";
		const string VIEW_STRAFE_LEFT_DEFAULT = "A";
		const string VIEW_STRAFE_RIGHT_DEFAULT = "D";
		const string VIEW_STRAFE_UP_DEFAULT = "E";
		const string VIEW_STRAFE_DOWN_DEFAULT = "Q";

		public static Event GetToolViewMapping()
		{
			return GetMapping(TOOL_VIEW_KEY) ?? Event.KeyboardEvent(TOOL_VIEW_DEFAULT);
		}

		public static Event GetToolMoveMapping()
		{
			return GetMapping(TOOL_MOVE_KEY) ?? Event.KeyboardEvent(TOOL_MOVE_DEFAULT);
		}

		public static Event GetToolRotateMapping()
		{
			return GetMapping(TOOL_ROTATE_KEY) ?? Event.KeyboardEvent(TOOL_ROTATE_DEFAULT);
		}

		public static Event GetViewForwardMapping()
		{
			return GetMapping(VIEW_FORWARD_KEY) ?? Event.KeyboardEvent(VIEW_FORWARD_DEFAULT);
		}

		public static Event GetViewBackMapping()
		{
			return GetMapping(VIEW_BACK_KEY) ?? Event.KeyboardEvent(VIEW_BACK_DEFAULT);
		}

		public static Event GetViewStrafeLeftMapping()
		{
			return GetMapping(VIEW_STRAFE_LEFT_KEY) ?? Event.KeyboardEvent(VIEW_STRAFE_LEFT_DEFAULT);
		}

		public static Event GetViewStrafeRightMapping()
		{
			return GetMapping(VIEW_STRAFE_RIGHT_KEY) ?? Event.KeyboardEvent(VIEW_STRAFE_RIGHT_DEFAULT);
		}

		public static Event GetViewStrafeUpMapping()
		{
			return GetMapping(VIEW_STRAFE_UP_KEY) ?? Event.KeyboardEvent(VIEW_STRAFE_UP_DEFAULT);
		}

		public static Event GetViewStrafeDownMapping()
		{
			return GetMapping(VIEW_STRAFE_DOWN_KEY) ?? Event.KeyboardEvent(VIEW_STRAFE_DOWN_DEFAULT);
		}

		private static Event GetMapping(string key)
		{
			string value = EditorPrefs.GetString(key);

			if(!string.IsNullOrEmpty(value))
			{
				string[] split = value.Split(';');
				if(split.Length == 2)
				{
					return Event.KeyboardEvent(split[1]);
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}
	}
}
#endif
