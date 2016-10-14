#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public static class LinearFPSCam
	{
		const float CONSTANT_SPEED_NORMAL = 0.06f;
		// When shift is held
		const float CONSTANT_SPEED_FASTER = 0.12f;

		static bool overrideActive = false;
		static Vector3 activeCameraPosition;

	    static float forwardSpeed = 0;
	    static float rightSpeed = 0;
	    static float upSpeed = 0;

		static bool isShiftHeld = false;

	    public static void OnUpdate()
	    {
	        if(overrideActive)
	        {
	            if(forwardSpeed != 0
	                || rightSpeed != 0
	                || upSpeed != 0)
	            {
	                // Derived from Jonathan Brodsky's code - https://gist.github.com/jonbro/b591700056fed8abe60f
					Transform cameraTransform = SceneView.lastActiveSceneView.camera.transform;

					Vector3 deltaPosition = Vector3.zero;

					// Forward/back and right/left uses the camera's local orientation
					deltaPosition += forwardSpeed * cameraTransform.forward;
	                deltaPosition += rightSpeed * cameraTransform.right;
					// Up/down uses the absolute (global) orientation
	                deltaPosition += upSpeed * Vector3.up;

	                if(isShiftHeld) // Use a faster speed when shift is held
	                {
	                    activeCameraPosition += CONSTANT_SPEED_FASTER * deltaPosition;
	                }
	                else
	                {
	                    activeCameraPosition += CONSTANT_SPEED_NORMAL * deltaPosition;
	                }

					// Previous vector from the camera position to the camera pivot
					Vector3 pivotOffset = SceneView.lastActiveSceneView.pivot - cameraTransform.position;

					// Update the camera position to the new position
	                cameraTransform.position = activeCameraPosition;

					// The camera pivot is the point at which the camera focusses on. When using the Orbit view tool, this
					// is the point that the camera rotates around. In FPS cam we rotate around the camera position, but
					// it's still necessary to recalculate the pivot
					SceneView.lastActiveSceneView.pivot = cameraTransform.position + pivotOffset;
	            }
	        }
	    }

	    public static void OnSceneGUI(SceneView sceneView) 
	    {
	        if(Tools.viewTool == ViewTool.FPS)
	        {
	            if(!overrideActive) // Just activated, grab the position
	            {
	                overrideActive = true;
	                activeCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
	            }

	            Event e = Event.current;

	            // KeyDown events don't change when you press shift after the initial key down, so need to cache this separately
	            isShiftHeld = e.shift;

	//            if(e.type != EventType.Layout && e.type != EventType.Repaint)
	//            {
	//                Debug.Log(e);
	//            }

	            if(e.type == EventType.KeyDown)
	            {
					
					// A key has been pressed, so track the relevant direction
					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewForwardMapping(), true, false))
	                {
	                    forwardSpeed = 1;
	                }
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewBackMapping(), true, false))
	                {
	                    forwardSpeed = -1;
	                }

					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeRightMapping(), true, false))
	                {
	                    rightSpeed = 1;
	                }
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeLeftMapping(), true, false))
	                {
	                    rightSpeed = -1;
	                }
	              
					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeUpMapping(), true, false))
	                {
	                    upSpeed = 1;
	                }
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeDownMapping(), true, false))
	                {
	                    upSpeed = -1;
	                }
					e.Use();
//					Debug.Log(e);
	            }
	            else if(e.type == EventType.KeyUp)
	            {
					// A key has been released so reset the related direction
					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewForwardMapping(), true, false))
					{
						forwardSpeed = 0;
					}
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewBackMapping(), true, false))
					{
						forwardSpeed = 0;
					}

					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeRightMapping(), true, false))
					{
						rightSpeed = 0;
					}
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeLeftMapping(), true, false))
					{
						rightSpeed = 0;
					}

					if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeUpMapping(), true, false))
					{
						upSpeed = 0;
					}
					else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetViewStrafeDownMapping(), true, false))
					{
						upSpeed = 0;
					}
					e.Use();
	            }
	        }
	        else
	        {
				// FPS cam is no longer active, so stop overriding it and reset speeds
	            overrideActive = false;
	            forwardSpeed = 0;
	            rightSpeed = 0;
	            upSpeed = 0;
	        }
	    }
	}
}
#endif