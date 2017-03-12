#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public enum MainMode
	{
	    Resize,
	    Vertex,
		Face,

		Clip,
		Draw,
	};

	public enum OverrideMode
	{
		None,
        TransformModel, // Dummy mode that prevents active tools conflicting
		//Clip,
		//Draw,
	};

	public enum GridMode
	{
		Unity,
		SabreCSG,
		None
	}

	[ExecuteInEditMode]
    public class CurrentSettings : ScriptableObject
	{
		bool brushesHidden = false;
		bool meshHidden = false;
        Material foregroundMaterial;

        static CurrentSettings instance = null;

        static CurrentSettings Instance
        {
            get
            {
                // Instance reference lost or not set
                if(instance == null)
                {
                    // First of all see if a CurrentSettings object exists
                    instance = FindObjectOfType<CurrentSettings>();

                    // Couldn't find an existing object, make a new one
                    if(instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<CurrentSettings>();
                    }
                }
                return instance;
            }
        }

		const string KEY_PREFIX = "SabreCSG";

	    public static bool PositionSnappingEnabled
	    {
	        get
	        {
				return PlayerPrefs.GetInt(KEY_PREFIX + "positionSnappingEnabled", 1) != 0;
	        }
	        set
	        {
				PlayerPrefs.SetInt(KEY_PREFIX + "positionSnappingEnabled", value ? 1 : 0);
	        }
	    }

	    public static float PositionSnapDistance
	    {
	        get
	        {
				return PlayerPrefs.GetFloat(KEY_PREFIX + "positionSnapDistance", 1f);
	        }
	        set
	        {
				if(value > 0)
				{
					PlayerPrefs.SetFloat(KEY_PREFIX + "positionSnapDistance", value);
				}
	        }
		}
		
		public static bool AngleSnappingEnabled
		{
			get
			{
				return PlayerPrefs.GetInt(KEY_PREFIX + "angleSnappingEnabled", 1) != 0;
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "angleSnappingEnabled", value ? 1 : 0);
			}
		}

		public static bool HideGridInPerspective
		{
			get
			{
				return PlayerPrefs.GetInt(KEY_PREFIX + "HideGridInPerspective", 0) != 0;
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "HideGridInPerspective", value ? 1 : 0);
			}
		}

		public static bool OverrideFlyCamera
		{
			get
			{
				return PlayerPrefs.GetInt(KEY_PREFIX + "OverrideFlyCamera", 0) != 0;
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "OverrideFlyCamera", value ? 1 : 0);
			}
		}

		public static bool ShowExcludedPolygons
		{
			get
			{
				return PlayerPrefs.GetInt(KEY_PREFIX + "ShowExcludedPolygons", 0) != 0;
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "ShowExcludedPolygons", value ? 1 : 0);
			}
		}

		public static bool ReducedHandleThreshold
		{
			get
			{
				return PlayerPrefs.GetInt(KEY_PREFIX + "ReducedHandleThreshold", 0) != 0;
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "ReducedHandleThreshold", value ? 1 : 0);
			}
		}

		public static float AngleSnapDistance
		{
			get
			{
				return PlayerPrefs.GetFloat(KEY_PREFIX + "angleSnapDistance", 15);
			}
			set
			{
				if(value > 0)
				{
					PlayerPrefs.SetFloat(KEY_PREFIX + "angleSnapDistance", value);
				}
			}
		}

	    public static Material ForegroundMaterial
	    {
	        get
	        {
	            return Instance.foregroundMaterial;
	        }
	        set
	        {
	            Instance.foregroundMaterial = value;
	        }
		}

		public static GridMode GridMode 
		{
			get 
			{
				string storedValue = PlayerPrefs.GetString(KEY_PREFIX + "gridMode");
				if(Enum.IsDefined(typeof(GridMode), storedValue))
				{
					return (GridMode)Enum.Parse(typeof(GridMode), storedValue);
				}
				else
				{
					return GridMode.SabreCSG;
				}
			}
			set 
			{
				PlayerPrefs.SetString(KEY_PREFIX + "gridMode", value.ToString());
			}
		}

	    public static void ChangePosSnapDistance(float multiplier)
	    {
	        PositionSnapDistance *= multiplier;
	    }

		public static void ChangeAngSnapDistance(float multiplier)
		{
			AngleSnapDistance *= multiplier;
		}

		public static bool BrushesHidden
	    {
	        get
	        {
	            return Instance.brushesHidden;
	        }
	        set
	        {
                Instance.brushesHidden = value;
	        }
	    }

		public static bool MeshHidden
		{
			get
			{
				return Instance.meshHidden;
			}
			set
			{
                Instance.meshHidden = value;
			}
		}

	    // TODO: This behaves differently to just !brushesHidden, need to make the two properties less ambiguous
		public static bool BrushesVisible
	    {
	        get
	        {
                return !Instance.brushesHidden;
	        }
	    }

		public static bool AllowMeshSelection
	    {
	        get
	        {
				return false;
	        }
	    }

		public static MainMode CurrentMode
        {
            get
            {
                int storedValue = PlayerPrefs.GetInt(KEY_PREFIX + "-CurrentMode", 0);

                if (storedValue >= Enum.GetNames(typeof(MainMode)).Length || storedValue < 0)
				{
					return default(MainMode);
				}
				else
				{
					return (MainMode)storedValue;
				}
	        }
	        set
	        {
				PlayerPrefs.SetInt(KEY_PREFIX + "-CurrentMode", (int)value);
				// Occassionally have experienced issues where camera locks up, resetting the Tools class seems to fix it
				Tools.viewTool = ViewTool.None;
				Tools.current = UnityEditor.Tool.None;
	        }
	    }

		public static OverrideMode OverrideMode
		{
			get
			{
				int storedValue = PlayerPrefs.GetInt(KEY_PREFIX + "-OverrideMode", 0);

				if(storedValue >= Enum.GetNames(typeof(OverrideMode)).Length || storedValue < 0)
				{
					return default(OverrideMode);
				}
				else
				{
					return (OverrideMode)storedValue;
				}
			}
			set
			{
				PlayerPrefs.SetInt(KEY_PREFIX + "-OverrideMode", (int)value);
				// Occassionally have experienced issues where camera locks up, resetting the Tools class seems to fix it
				Tools.viewTool = ViewTool.None;
				Tools.current = UnityEditor.Tool.None;
			}
		}
	}
}
#endif