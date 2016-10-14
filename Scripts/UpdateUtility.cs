#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class UpdateUtility
	{
		static readonly Dictionary<string, string> FILES_TO_MOVE = new Dictionary<string, string>
		{
			{ "Assets/SabreCSG/Materials/default_map.png", "Assets/SabreCSG/Resources/Materials/default_map.png" },
			{ "Assets/SabreCSG/Materials/default_map_2.png", "Assets/SabreCSG/Resources/Materials/default_map_2.png" },
			{ "Assets/SabreCSG/Materials/Default_map.mat", "Assets/SabreCSG/Resources/Materials/Default_map.mat" }
		};

		static readonly List<string> FILES_TO_DELETE = new List<string>
		{
//			"Assets/Foo.mat"
		};

		[InitializeOnLoadMethod]
		public static void CleanupPackageUpgrade()
		{
			foreach (KeyValuePair<string, string> pair in FILES_TO_MOVE) 
			{
				if(File.Exists(pair.Key))
				{
					// Ensure target directory structure exists
					string[] directories = Path.GetDirectoryName(pair.Value).Split('/');

					string totalPath = directories[0];
					for (int i = 1; i < directories.Length; i++) 
					{
						totalPath = Path.Combine(totalPath, directories[i]);
						if(!Directory.Exists(totalPath))
						{
							AssetDatabase.CreateFolder(Path.GetDirectoryName(totalPath), Path.GetFileName(totalPath));
						}
					}

					// Attempt to move the file
					string message = AssetDatabase.MoveAsset(pair.Key, pair.Value);
					if(!string.IsNullOrEmpty(message))
					{
						Debug.LogError("Unable to move " + pair.Key + "\n" + message);
					}
				}
			}

			foreach (string filename in FILES_TO_DELETE) 
			{
				if(File.Exists(filename))
				{
					// Attempt to delete the file
					if(!AssetDatabase.MoveAssetToTrash(filename))
					{
						Debug.LogError("Unable to delete " + filename);
					}
				}
			}
		}

		public static void RunCleanup()
		{
			// As of 1.1 CurrentSettings is no longer a MonoBehaviour, so remove any existing objects
			CleanupOldSettings();
		}

		private static void CleanupOldSettings()
		{
			// As of 1.1 CurrentSettings is no longer a MonoBehaviour
			GameObject existingSettingsObject = GameObject.Find("CurrentSettings");
			if(existingSettingsObject != null) // Found an old style object
			{
				Component[] components = existingSettingsObject.GetComponents<Component>();

				// Should be a transform and a null
				if(components.Length == 2)
				{
					bool matched = true;
					for (int i = 0; i < components.Length; i++) 
					{
						if(components[i] != null && components[i].GetType() != typeof(Transform))
						{
							matched = false;
							break;
						}
					}

					if(matched)
					{
						GameObject.DestroyImmediate(existingSettingsObject);
					}
				}
			}
		}
	}
}
#endif