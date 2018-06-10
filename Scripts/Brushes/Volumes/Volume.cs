using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Volumes are custom programmable triggers in the shape of primitive brushes.
    /// </summary>
    [System.Serializable]
    public class Volume : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Gets the brush preview material shown in the editor.
        /// </summary>
        /// <returns>The volume material.</returns>
        public virtual Material GetBrushPreviewMaterial()
        {
            return SabreCSGResources.GetVolumeMaterial();
        }

        /// <summary>
        /// Called when the inspector GUI is drawn in the editor.
        /// </summary>
        public virtual void OnInspectorGUI()
        {
            
        }

        /// <summary>
        /// Searches the main C# assembly for volumes that can be instantiated.
        /// </summary>
        /// <returns>All matched volume types.</returns>
        public static List<Type> FindAllInAssembly()
        {
            List<Type> matchedTypes = new List<Type>();

            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in allAssemblies)
            {
                // walk through all the types in the main assembly
                if (assembly.FullName.StartsWith("Assembly-CSharp"))
                {
                    Type[] types = assembly.GetTypes();

                    for (int i = 0; i < types.Length; i++)
                    {
                        // if the type inherits from volume and is not abstract
                        if (!types[i].IsAbstract && types[i].IsSubclassOf(typeof(Volume)))
                        {
                            // valid volume type found!
                            matchedTypes.Add(types[i]);
                        }
                    }
                }
            }

            return matchedTypes;
        }
#endif
    }
}