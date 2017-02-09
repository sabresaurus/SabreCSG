#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
    public class PaletteWindow : EditorWindow
    {
		enum DropInType { Replace, InsertAfter };
        const int SPACING = 8;


        Vector2 scrollPosition = Vector2.zero;

        int width = 100; // Width of a palette cell
        List<Object> selectedObjects = new List<Object>();

        int mouseDownIndex = -1;

		int dropInTargetIndex = -1; // Current slot a DragDrop is being considered as a destination
		DropInType dropInType = DropInType.Replace;

		bool isDragging = false; // If a DragDrop from a slot is in progress

		int deferredIndexToRemove = -1;

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

            if (e.rawType == EventType.MouseUp || e.rawType == EventType.MouseMove || e.rawType == EventType.DragUpdated)
            {
                dropInTargetIndex = -1;
				dropInType = DropInType.Replace;
            }

			Object deferredInsertObject = null;
			int deferredInsertIndex = -1;

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                int columnIndex = i % columnCount;

                if(columnIndex == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                }
                
                
				Object newSelectedObject = null;
				bool dropAccepted = false;
				DrawElement(e, selectedObjects[i], i, out newSelectedObject, out dropAccepted);

				if(dropAccepted || newSelectedObject != selectedObjects[i])
				{
					if(dropInType == DropInType.InsertAfter)
					{
						// Defer the insert until after we've drawn the UI so we don't mismatch UI mid-draw
						deferredInsertIndex = i + 1;
						deferredInsertObject = newSelectedObject;
					}
					else if(newSelectedObject != selectedObjects[i])
					{
						selectedObjects[i] = newSelectedObject;
						Save();
					}
				}
                

				GUILayout.Space(4);

                if(columnIndex == columnCount-1 || i == selectedObjects.Count-1) // If last in row
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

			if (e.type == EventType.MouseDrag && !isDragging && mouseDownIndex != -1 && selectedObjects[mouseDownIndex] != null)
            {
                isDragging = true;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { selectedObjects[mouseDownIndex] };
//				DragAndDrop.paths = new string[] { selectedObjects[mouseDownIndex] };
				DragAndDrop.StartDrag(selectedObjects[mouseDownIndex].name);
            }

            if (e.rawType == EventType.MouseUp || e.rawType == EventType.MouseMove || e.rawType == EventType.DragPerform || e.rawType == EventType.DragExited)
            {
                isDragging = false;
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
            
			// User configurable tile size
            width = (int)GUI.HorizontalSlider(lastRect, width, 50, 100);

            // Delete at the end of the OnGUI so we don't mismatch any UI groups
            if(deferredIndexToRemove != -1)
            {
                selectedObjects.RemoveAt(deferredIndexToRemove);
                deferredIndexToRemove = -1;
                Save();
            }

			// Insert at the end of the OnGUI so we don't mismatch any UI groups
			if(deferredInsertObject != null)
			{
				selectedObjects.Insert(deferredInsertIndex, deferredInsertObject);
				Save();
			}

			// Carried out a DragPerform, so reset drop in states
			if (e.rawType == EventType.DragPerform)
			{
				dropInTargetIndex = -1;
				dropInType = DropInType.Replace;
			}
        }

		public void DrawElement(Event e, Object selectedObject, int index, out Object newSelection, out bool dropAccepted)
        {
			dropAccepted = false;
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

			Rect insertAfterRect = new Rect(previewRect.xMax, previewRect.y, 8, previewRect.height);
			bool mouseInInsertAfterRect = insertAfterRect.Contains(e.mousePosition);

			selectedObject = EditorGUILayout.ObjectField(selectedObject, TypeFilter, false, GUILayout.Width(width));

            if(dropInTargetIndex == index)
            {
				if(dropInType == DropInType.InsertAfter)
				{
					DrawOutline(insertAfterRect, 2, Color.blue);
				}
				else
				{
	                DrawOutline(previewRect, 2, Color.blue);
				}
            }


            if(mouseInRect && e.type == EventType.MouseDown)
            {
                mouseDownIndex = index;
            }

            if (mouseInRect && e.type == EventType.MouseUp && !isDragging)
            {
                if (e.button == 0)
                {
					OnItemClick(selectedObject);                    
                }
                else
                {
                    if(selectedObject == null)
                    {
                        deferredIndexToRemove = index;
                    }
                    else
                    {
                        selectedObject = null;
                    }
                    
                    Repaint();
                }
            }

            if (mouseInRect && e.type == EventType.MouseDown && !isDragging)
            {
                if (e.button == 0)
                {
                    if (e.clickCount == 2 && selectedObject != null)
                    {
                        if (selectedObject.GetType() == typeof(SceneAsset))
                        {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(selectedObject), OpenSceneMode.Single);
                            }
                        }
						else if (typeof(RuntimeAnimatorController).IsAssignableFrom(selectedObject.GetType()))
						{
							SetAnimatorWindow(selectedObject);
						}
                        else if(selectedObject.GetType() == typeof(DefaultAsset))
                        {
                            // Something without a strong Unity type, could be a folder
                            if(ProjectWindowUtil.IsFolder(selectedObject.GetInstanceID()))
                            {
                                SetProjectWindowFolder(AssetDatabase.GetAssetPath(selectedObject));
                            }
                        }
                    }
                }
            }



            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length > 0
                    && (DragAndDrop.objectReferences[0].GetType() == TypeFilter || DragAndDrop.objectReferences[0].GetType().IsSubclassOf(TypeFilter)))
                {
					if (mouseInRect || mouseInInsertAfterRect)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            selectedObject = DragAndDrop.objectReferences[0];

							dropAccepted = true;
                        }
                        else
                        {
                            dropInTargetIndex = index;
							dropInType = mouseInInsertAfterRect ? DropInType.InsertAfter : DropInType.Replace;
                        }
                        Repaint();
                    }
                }
            }

            EditorGUILayout.EndVertical();

			newSelection = selectedObject;
        }

        public static void SetProjectWindowFolder(string folderPath)
        {
            // This code uses reflection to set the project window folder
            // As a result the internal structure could change, so the entire thing is wrapped in a try-catch
            try
            {
				EditorApplication.ExecuteMenuItem("Window/Project");

                Assembly unityEditorAssembly = Assembly.GetAssembly(typeof(Editor));
                if (unityEditorAssembly != null)
                {
                    System.Type projectWindowUtilType = unityEditorAssembly.GetType("UnityEditor.ProjectWindowUtil");
                    MethodInfo getBrowserMethodInfo = projectWindowUtilType.GetMethod("GetProjectBrowserIfExists", BindingFlags.Static | BindingFlags.NonPublic);
                    
                    // Get any active project browser (Project window)
                    object projectBrowser = getBrowserMethodInfo.Invoke(null, new object[] { });

                    // If there is an active project browser
                    if (projectBrowser != null)
                    {
                        System.Type projectBrowserType = unityEditorAssembly.GetType("UnityEditor.ProjectBrowser");

                        System.Type searchFilterType = unityEditorAssembly.GetType("UnityEditor.SearchFilter");

                        // Create a SearchFilter instance which we will populate then pass to the ProjectBrowser
                        object searchFilterInstance = System.Activator.CreateInstance(searchFilterType);
                        PropertyInfo foldersProperty = searchFilterType.GetProperty("folders");
                        // Set the active folder on the new SearchFilter
                        foldersProperty.GetSetMethod().Invoke(searchFilterInstance, new object[] { new string[] { folderPath } });

                        // Supply the new SearchFilter to the project browser so it shows the new folder
                        MethodInfo setSearchMethodInfo = projectBrowserType.GetMethod("SetSearch", BindingFlags.Instance | BindingFlags.Public, null, new System.Type[] { searchFilterType }, null);
                        setSearchMethodInfo.Invoke(projectBrowser, new object[] { searchFilterInstance });

                        // Folder selection has changed, make sure the tree and top bar properly update
                        MethodInfo selectionChangedMethodInfo = projectBrowserType.GetMethod("FolderTreeSelectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                        selectionChangedMethodInfo.Invoke(projectBrowser, new object[] { true });
                    }
                }
            }
            catch
            {
                // Failed, suppress any errors and just do nothing
            }
        }

		public static void SetAnimatorWindow(Object selectedObject)
		{
			EditorApplication.ExecuteMenuItem("Window/Animator");
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