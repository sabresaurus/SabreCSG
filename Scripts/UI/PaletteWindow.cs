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
        [System.Serializable]
        protected class Tab
        {
            public string Name = "";
            public List<Object> trackedObjects = new List<Object>();
        }

        enum DropInType { Replace, InsertAfter };
        const int SPACING = 11;


        Vector2 scrollPosition = Vector2.zero;

        int width = 100; // Width of a palette cell

        bool editingTabName = false;
        int? hotControl = null;

        List<Tab> tabs = new List<Tab>();

        int activeTab = 0;

        int mouseDownIndex = -1;

        int dropInTargetIndex = -1; // Current slot a DragDrop is being considered as a destination
        DropInType dropInType = DropInType.Replace;

        bool isDragging = false; // If a DragDrop from a slot is in progress

        int deferredIndexToRemove = -1;

        protected virtual string PlayerPrefKeyPrefix
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

        bool UseCells
        {
            get
            {
                return width >= 50;
            }
        }

        [MenuItem("Window/Palette")]
        static void CreateAndShow()
        {
            EditorWindow window = EditorWindow.GetWindow<PaletteWindow>("Palette");//false, "Palette", true);

            window.minSize = new Vector2( 120, 120 );

            window.Show();
        }

        void OnEnable()
        {
            for (int i = 0; i < 9; i++) 
            {
                Load(i);    
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < 9; i++) 
            {
                Save(i);    
            }
        }

        void OnGUI()
        {
            Event e = Event.current;
            
#if UNITY_5_4_OR_NEWER
            int columnCount = (int)(Screen.width / EditorGUIUtility.pixelsPerPoint) / (width + SPACING);
#else
            int columnCount = Screen.width / (width + SPACING);
#endif
            List<Object> selectedObjects = tabs[activeTab].trackedObjects;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Space(8);
            //GUILayout.Box(new GUIContent(), GUILayout.ExpandWidth(true));
            
            // Make sure there's an empty one at the end to drag into
            if (selectedObjects.Count == 0 || selectedObjects[selectedObjects.Count-1] != null)
            {
                selectedObjects.Add(null);
            }

            if (e.rawType == EventType.MouseUp || e.rawType == EventType.MouseMove || e.rawType == EventType.DragUpdated || e.rawType == EventType.ScrollWheel)
            {
                dropInTargetIndex = -1;
                dropInType = DropInType.Replace;
            }

            List<Object> deferredInsertObjects = null;
            int deferredInsertIndex = -1;

            for (int i = 0; i < selectedObjects.Count; i++)
            {
                int columnIndex = i % columnCount;

                if(UseCells && columnIndex == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8);
                }
                
                
                List<Object> newSelectedObjects = null;
                bool dropAccepted = false;
                DrawElement(e, selectedObjects[i], i, out newSelectedObjects, out dropAccepted);

                if(dropAccepted || newSelectedObjects.Count > 1 || newSelectedObjects[0] != selectedObjects[i])
                {
                    if(dropInType == DropInType.InsertAfter)
                    {
                        // Defer the insert until after we've drawn the UI so we don't mismatch UI mid-draw
                        deferredInsertIndex = i + 1;
                        deferredInsertObjects = newSelectedObjects;
                    }
                    else
                    {
                        selectedObjects[i] = newSelectedObjects[0];
                        if(newSelectedObjects.Count > 1)
                        {
                            deferredInsertIndex = i + 1;
                            newSelectedObjects.RemoveAt(0);
                            deferredInsertObjects = newSelectedObjects;
                        }

                        Save(activeTab);
                    }
                }
                
                if(UseCells)
                {
                    GUILayout.Space(4);
                }
                else
                {
                    GUILayout.Space(8);
                }

                if(UseCells && (columnIndex == columnCount-1 || i == selectedObjects.Count-1)) // If last in row
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
//                DragAndDrop.paths = new string[] { selectedObjects[mouseDownIndex] };
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
            RectOffset padding = boxStyle.padding;
            padding.top += 1;
            boxStyle.padding = padding;
            GUILayout.Box(new GUIContent(), boxStyle, GUILayout.ExpandWidth(true));

            Rect lastRect = GUILayoutUtility.GetLastRect();

            Rect buttonRect = lastRect;
            buttonRect.y += 1;

            buttonRect.width = (buttonRect.width - 90) / 9f;

            GUIStyle activeStyle = EditorStyles.toolbarButton;
            buttonRect.height = activeStyle.CalcHeight(new GUIContent(), 20);
            for (int i = 0; i < 9; i++) 
            {
                string tabName = (i + 1).ToString();
                if(!string.IsNullOrEmpty(tabs[i].Name))
                {
                    tabName = tabs[i].Name;
                }

                bool oldValue = (activeTab == i);

                if(oldValue == true && editingTabName)
                {                    
                    GUI.SetNextControlName("PaletteTabName");
                    tabs[activeTab].Name = GUI.TextField(buttonRect, tabs[activeTab].Name);

                    if(!hotControl.HasValue)
                    {
                        hotControl = GUIUtility.hotControl;
                        GUI.FocusControl("PaletteTabName");
                    }
                    else
                    {
                        if(GUIUtility.hotControl != hotControl.Value // Clicked off it
                            || Event.current.type == EventType.KeyDown && Event.current.character == (char)10) // Return pressed
                        {
                            editingTabName = false;
                            hotControl = null;
                        }
                    }
                }
                else
                {
                    bool newValue = GUI.Toggle(buttonRect, oldValue, tabName, activeStyle);
                    if(newValue != oldValue)
                    {
                        if(newValue == true)
                        {
                            activeTab = i;
                            Repaint();
                            editingTabName = false;
                        }
                        else if(newValue == false)
                        {
                            editingTabName = true;
                            hotControl = null;
                        }
                    }
                }

                buttonRect.x += buttonRect.width;
            }

//            Debug.Log(GUI.GetNameOfFocusedControl());
//            if(GUI.GetNameOfFocusedControl() != "PaletteTabName")
//            {
//                editingTabName = false;
//            }

            Rect sliderRect = lastRect;
            sliderRect.xMax -= 10;
            sliderRect.xMin = sliderRect.xMax - 60;
            
            // User configurable tile size
            width = (int)GUI.HorizontalSlider(sliderRect, width, 49, 100);


            // Delete at the end of the OnGUI so we don't mismatch any UI groups
            if(deferredIndexToRemove != -1)
            {
                selectedObjects.RemoveAt(deferredIndexToRemove);
                deferredIndexToRemove = -1;
                Save(activeTab);
            }

            // Insert at the end of the OnGUI so we don't mismatch any UI groups
            if(deferredInsertObjects != null)
            {
                selectedObjects.InsertRange(deferredInsertIndex, deferredInsertObjects);
                Save(activeTab);
            }

            // Carried out a DragPerform, so reset drop in states
            if (e.rawType == EventType.DragPerform)
            {
                dropInTargetIndex = -1;
                dropInType = DropInType.Replace;
                Repaint();
            }
        }

        public void DrawElement(Event e, Object selectedObject, int index, out List<Object> newSelection, out bool dropAccepted)
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

            Rect previewRect;
            Rect insertAfterRect;

            if(UseCells)
            {
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

                previewRect = GUILayoutUtility.GetLastRect();
                insertAfterRect = new Rect(previewRect.xMax, previewRect.y, 8, previewRect.height);

                selectedObject = EditorGUILayout.ObjectField(selectedObject, TypeFilter, false, GUILayout.Width(width));
            }
            else
            {
                selectedObject = EditorGUILayout.ObjectField(selectedObject, TypeFilter, false);
                previewRect = GUILayoutUtility.GetLastRect();
                insertAfterRect = new Rect(previewRect.xMin, previewRect.yMax, previewRect.width, 8);
            }

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

            bool mouseInRect = previewRect.Contains(e.mousePosition);
            bool mouseInInsertAfterRect = insertAfterRect.Contains(e.mousePosition);

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
                        else
                        {
                            AssetDatabase.OpenAsset(selectedObject);
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

            newSelection = new List<Object>{ selectedObject };
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

        void Save(int tabIndex)
        {
            while(tabs.Count <= tabIndex)
            {
                tabs.Add(new Tab());
            }
            List<Object> selectedObjects = tabs[tabIndex].trackedObjects;
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                if(selectedObjects[i] != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObjects[i]);
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);

                    // Save both the GUID and the path in case the GUID changes
                    output.Append(guid);
                    output.Append(":");
                    output.Append(assetPath);
                    output.Append(",");
                }
            }
            string outputString = output.ToString().TrimEnd(',');

            string key = PlayerPrefKeyPrefix;
            if(tabIndex > 0)
            {
                key += tabIndex;
            }
            
            PlayerPrefs.SetString(key, outputString);

            PlayerPrefs.SetString(key + "-Tab", tabs[tabIndex].Name);
            PlayerPrefs.Save();
        }

        void Load(int tabIndex)
        {
            while(tabs.Count <= tabIndex)
            {
                tabs.Add(new Tab());
            }

            tabs[tabIndex].trackedObjects.Clear();

            string key = PlayerPrefKeyPrefix;
            if(tabIndex > 0)
            {
                key += tabIndex;
            }

            string trackedString = PlayerPrefs.GetString(key);
            string[] trackedObjectStrings = trackedString.Split(',');
            foreach (string trackedObjectString in trackedObjectStrings)
            {
                // Get the guid, for older versions this is the only thing in the string, for newer versions it's the
                // left side of a colon. Either way Split()[0] works  
                string guid = trackedObjectString.Split(':')[0];
                
                // Even when we have a path tracked, construct it fresh from the GUID in case the file has moved and
                // there's a new one with the old path. We should always try to track on the GUID since that's what
                // Unity does
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

                if (mainAsset == null && trackedObjectString.Contains(":"))
                {
                    // Couldn't find an asset for that GUID, try the path we saved
                    path = trackedObjectString.Split(':')[1];
                    mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                }

                if(mainAsset != null)
                {
                    tabs[tabIndex].trackedObjects.Add(mainAsset);
                }
            }
            tabs[tabIndex].Name = PlayerPrefs.GetString(key + "-Tab");
        }
    }
}
#endif