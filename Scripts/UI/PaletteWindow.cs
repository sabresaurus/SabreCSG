#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text;

namespace Sabresaurus.SabreCSG
{
    public class PaletteWindow : EditorWindow
    {
        const int SPACING = 8;


        Vector2 scrollPosition = Vector2.zero;

        int width = 100; // Width of a palette cell
        List<Object> selectedObjects = new List<Object>();

        int mouseDownIndex = -1;

        int dropInTarget = -1; // Current slot a DragDrop is being considered as a destination

        bool dragging = false; // If a DragDrop from a slot is in progress

        int indexToRemove = -1;

		protected virtual string PlayerPrefKey
		{
			get
			{
				return "PaletteSelection";
			}
		}

		protected virtual System.Type TypeFilter
		{
			get
			{
				return typeof(Object);
			}
		}

        [MenuItem("Window/Palette")]
        static void CreateAndShow()
        {
			EditorWindow window = EditorWindow.GetWindow<PaletteWindow>("Palette");//false, "Palette", true);

            window.Show();
        }

        void OnEnable()
        {
            Load();
        }

        void OnDisable()
        {
            Save();
        }

        void OnGUI()
        {
            Event e = Event.current;
            //Debug.Log(e.type);
            Object newSelectedObject = null;
#if UNITY_5_4_OR_NEWER
			int columnCount = (int)(Screen.width / EditorGUIUtility.pixelsPerPoint) / (width + SPACING);
#else
            int columnCount = Screen.width / (width + SPACING);
#endif
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Space(8);
            //GUILayout.Box(new GUIContent(), GUILayout.ExpandWidth(true));
            
            // Make sure there's an empty one at the end to drag into
            if (selectedObjects.Count == 0 || selectedObjects[selectedObjects.Count-1] != null)
            {
                selectedObjects.Add(null);
            }

            if (e.rawType == EventType.MouseUp || e.rawType == EventType.MouseMove || e.rawType == EventType.DragPerform || e.rawType == EventType.DragUpdated)
            {
                dropInTarget = -1;
            }

            
            
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                int columnIndex = i % columnCount;

                if(columnIndex == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                }
                
                
                newSelectedObject = DrawElement(e, selectedObjects[i], i);

                if(newSelectedObject != selectedObjects[i])
                {
                    selectedObjects[i] = newSelectedObject;
                    Save();
                }

                if(columnIndex == columnCount-1 || i == selectedObjects.Count-1) // If last in row
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

			if (e.type == EventType.MouseDrag && !dragging && mouseDownIndex != -1 && selectedObjects[mouseDownIndex] != null)
            {
                dragging = true;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { selectedObjects[mouseDownIndex] };
//				DragAndDrop.paths = new string[] { selectedObjects[mouseDownIndex] };
				DragAndDrop.StartDrag(selectedObjects[mouseDownIndex].name);
            }

            if (e.rawType == EventType.MouseUp || e.rawType == EventType.MouseMove || e.rawType == EventType.DragPerform || e.rawType == EventType.DragExited)
            {
                dragging = false;
                mouseDownIndex = -1;
            }


            EditorGUILayout.EndScrollView();
            
            GUILayout.FlexibleSpace();

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.margin = new RectOffset(0, 0, 0, 0);
            GUILayout.Box(new GUIContent(), boxStyle, GUILayout.ExpandWidth(true));

            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.xMax -= 10;
            lastRect.xMin = lastRect.xMax - 60;
            
            width = (int)GUI.HorizontalSlider(lastRect, width, 50, 100);

            // Delete at the end of the OnGUI so we don't mismatch any groups
            if(indexToRemove != -1)
            {
                selectedObjects.RemoveAt(indexToRemove);
                indexToRemove = -1;
                Save();
            }
        }

        public Object DrawElement(Event e, Object selectedObject, int index)
        {
            EditorGUILayout.BeginVertical();

            Texture2D texture = null;
            if (selectedObject != null)
            {
                texture = AssetPreview.GetAssetPreview(selectedObject);

                if (texture == null)
                {
                    if (AssetPreview.IsLoadingAssetPreview(selectedObject.GetInstanceID()))
                    {
                        // Not loaded yet, so tell it to repaint
                        Repaint();
                    }
                    else
                    {
                        //texture = AssetPreview.GetMiniTypeThumbnail(selectedObject.GetType());
                        texture = AssetPreview.GetMiniThumbnail(selectedObject);
                    }
                }
            }


            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.padding = new RectOffset(0, 0, 0, 0);
            style.alignment = TextAnchor.MiddleCenter;
            //style.margin = new RectOffset(0, 0, 0, 0);
            if(texture != null)
            {
                GUILayout.Box(texture, style, GUILayout.Width(width), GUILayout.Height(width));
            }
            else
            {
                GUILayout.Box("Drag an object here", style, GUILayout.Width(width), GUILayout.Height(width));
            }
            Rect previewRect = GUILayoutUtility.GetLastRect();
            bool mouseInRect = previewRect.Contains(e.mousePosition);

            //GUILayout.Label("Set Vertex Colors", SabreGUILayout.GetTitleStyle());
            //GUIStyle style = EditorStyles.objectFieldThumb;
			selectedObject = EditorGUILayout.ObjectField(selectedObject, TypeFilter, false, GUILayout.Width(width));


            if(dropInTarget == index)
            {
                DrawOutline(previewRect, 2, Color.blue);
            }

            if(mouseInRect && e.type == EventType.MouseDown)
            {
                mouseDownIndex = index;
            }

            if (mouseInRect && e.type == EventType.MouseUp && !dragging)
            {
                if (e.button == 0)
                {
					OnItemClick(selectedObject);                    
                }
                else
                {
                    if(selectedObject == null)
                    {
                        indexToRemove = index;
                    }
                    else
                    {
                        selectedObject = null;
                    }
                    
                    Repaint();
                }
            }

            if (mouseInRect && e.type == EventType.MouseDown && !dragging)
            {
                if (e.button == 0)
                {
                    if (e.clickCount == 2 && selectedObject != null && selectedObject.GetType() == typeof(SceneAsset))
                    {
                        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(selectedObject), OpenSceneMode.Single);
                        }
                    }
                }
            }



            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
				if (DragAndDrop.objectReferences.Length > 0
					&& (DragAndDrop.objectReferences[0].GetType() == TypeFilter ||DragAndDrop.objectReferences[0].GetType().IsSubclassOf(TypeFilter)))
				{
	                if (mouseInRect)
	                {
	                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

	                    if (e.type == EventType.DragPerform)
	                    {
	                        DragAndDrop.AcceptDrag();

                            selectedObject = DragAndDrop.objectReferences[0];
	                    }
	                    else
	                    {
	                        dropInTarget = index;
	                    }
	                    Repaint();
	                }
			}
            }

            EditorGUILayout.EndVertical();
            return selectedObject;
        }

        void DrawOutline(Rect lastRect, int lineThickness, Color color)
        {
            GUI.color = color;

            GUI.DrawTexture(new Rect(lastRect.xMin, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(lastRect.xMax - lineThickness, lastRect.yMin, lineThickness, lastRect.height), EditorGUIUtility.whiteTexture);

            GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMin, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(lastRect.xMin + lineThickness, lastRect.yMax - lineThickness, lastRect.width - lineThickness * 2, lineThickness), EditorGUIUtility.whiteTexture);

            GUI.color = Color.white; // Reset GUI color
        }

		protected virtual void OnItemClick(Object selectedObject)
		{
			Selection.activeObject = selectedObject;
		}

        void Save()
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                if(selectedObjects[i] != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedObjects[i]));

                    output.Append(guid);
                    output.Append(",");
                }
            }
            string outputString = output.ToString().TrimEnd(',');
			PlayerPrefs.SetString(PlayerPrefKey, outputString);
        }

        void Load()
        {
            selectedObjects.Clear();
			string selectionString = PlayerPrefs.GetString(PlayerPrefKey);
            string[] newGUIDs = selectionString.Split(',');
            for (int i = 0; i < newGUIDs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(newGUIDs[i]);
                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

                if(mainAsset != null)
                {
                    selectedObjects.Add(mainAsset);
                }
            }
        }
    }
}
#endif