#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MPTagEditorWindow : EditorWindow
	{
		public Material material = null;
		public MPWindow parent = null;
		public string[] existingMaterialLabels = new string[]{ };
		public List<string> labelsToAdd = new List<string>();

		private List<string> allLabels = new List<string>();
		private List<string> existingLabels = new List<string>();
		private Vector2 labelsScrollPos = Vector2.zero;
		private Vector2 allLabelsScrollPos = Vector2.zero;
		private Vector2 labelsToAddScrollPos = Vector2.zero;
		private Vector2 labelsOnMaterialScrollPos = Vector2.zero;
		private string newTagStr = string.Empty;

		private void OnGUI()
		{
			GUILayout.BeginVertical( "GameViewBackground" );
			{
				// top toolbar
				GUILayout.BeginHorizontal( EditorStyles.toolbarButton );
				{
					GUI.color = Color.green;
					if( GUILayout.Button( "Apply", EditorStyles.toolbarButton ) )
					{
						AddTagsToMaterial();
						parent.Load();

						Close();
					}
					GUI.color = Color.white;
					GUILayout.FlexibleSpace();
					GUILayout.Label( "Apply without adding to clear tags.", EditorStyles.toolbarButton );
				}
				GUILayout.EndHorizontal();

				// editing area
				GUILayout.BeginHorizontal();
				{
					DrawOptionArea();
					DrawTagList();
				}
				GUILayout.EndHorizontal();

				// status bar
				GUILayout.Label( existingLabels.Count + " Existing labels. " + labelsToAdd.Count + " Labels pending to add.", "HelpBox" );
			}
			GUILayout.EndVertical();
		}

		private void DrawOptionArea()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal( "Box", GUILayout.ExpandWidth( true ) );
				{
					GUILayout.Label( "Add New Label", GUILayout.Width( 90 ) );
					newTagStr = GUILayout.TextField( newTagStr, EditorStyles.textField, GUILayout.Width( 170 ) );

					if( GUILayout.Button( "", "OL Plus", GUILayout.Height( 16 ), GUILayout.Width( 16 ) ) )
					{
						if( newTagStr != string.Empty )
							labelsToAdd.Add( newTagStr );
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					// tags to add
					// TODO: possibly modularize this to get rid of clutter/redundancy
					GUILayout.BeginVertical( "Box", GUILayout.ExpandHeight( true ), GUILayout.ExpandWidth( true ) );
					{
						GUILayout.Label( "Tags to Add", EditorStyles.boldLabel );

						// add new field

						labelsToAddScrollPos = GUILayout.BeginScrollView( labelsToAddScrollPos, Styles.MPScrollViewBackground );
						{
							for( int i = 0; i < labelsToAdd.Count; i++ )
							{
								EditorGUI.indentLevel = 0;

								if( i % 2 != 0 ) // show odd background
								{
									GUI.backgroundColor = new Color32( 150, 150, 150, 255 );
								}
								else // show even background
								{
									GUI.backgroundColor = new Color32( 200, 200, 200, 255 );
								}

								GUILayout.BeginHorizontal();
								{
									GUILayout.Label( new GUIContent( labelsToAdd[i] ), Styles.MPAssetTagLabel, GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( true ) );

									Rect rect = GUILayoutUtility.GetLastRect();
									rect.x = rect.width - 18;
									if( GUI.Button( rect, new GUIContent( "", "Remove." ), "OL Minus" ) )
									{
										labelsToAdd.RemoveAt( i );
										labelsToAdd.TrimExcess();
									}
								}
								GUILayout.EndHorizontal();
								GUI.backgroundColor = Color.white;
							}
						}
						GUILayout.EndScrollView();

						GUILayout.Label( "Tags on Material", EditorStyles.miniLabel );

						// tags on material
						GUILayout.BeginVertical( Styles.MPScrollViewBackground, GUILayout.Height( 42 ) );
						{
							labelsOnMaterialScrollPos = GUILayout.BeginScrollView( labelsOnMaterialScrollPos, true, false );
							{
								GUILayout.Space( 4 );
								GUILayout.BeginHorizontal();
								{
									for( int i = 0; i < existingMaterialLabels.Length; i++ )
									{
										GUILayout.Label( existingMaterialLabels[i], "AssetLabel" );
									}
								}
								GUILayout.EndHorizontal();
							}
							GUILayout.EndScrollView();
						}
						GUILayout.EndVertical();
					}
					GUILayout.EndVertical();

					// all tags
					// TODO: possibly modularize this to get rid of clutter/redundancy
					GUILayout.BeginVertical( "Box", GUILayout.Width( 98 ),
						GUILayout.ExpandHeight( true ), GUILayout.ExpandWidth( true ) );
					{
						GUILayout.Label( "All Tags", EditorStyles.miniLabel );

						allLabelsScrollPos = GUILayout.BeginScrollView( allLabelsScrollPos, Styles.MPScrollViewBackground );
						{
							for( int i = 0; i < allLabels.Count; i++ )
							{
								EditorGUI.indentLevel = 0;

								if( i % 2 != 0 ) // show odd background
								{
									GUI.backgroundColor = new Color32( 150, 150, 150, 255 );
								}
								else // show even background
								{
									GUI.backgroundColor = new Color32( 200, 200, 200, 255 );
								}

								if( GUILayout.Button( new GUIContent( allLabels[i], "Click to add." ), Styles.MPAssetTagLabel, GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( true ) ) )
								{
									if( !labelsToAdd.Contains( allLabels[i] ) )
										labelsToAdd.Add( allLabels[i] );
								}
								GUI.backgroundColor = Color.white;
							}
						}
						GUILayout.EndScrollView();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		// TODO: possibly modularize this to get rid of clutter/redundancy
		private void DrawTagList()
		{
			GUILayout.BeginVertical( "Box", GUILayout.Width( 98 ), GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
			{
				GUILayout.Label( new GUIContent( "Used Tags", "Project-defined tags assigned by the user when importing an asset." ), EditorStyles.miniLabel );

				labelsScrollPos = GUILayout.BeginScrollView( labelsScrollPos, Styles.MPScrollViewBackground );
				{
					for( int i = 0; i < existingLabels.Count; i++ )
					{
						EditorGUI.indentLevel = 0;

						if( i % 2 != 0 ) // show odd background
						{
							GUI.backgroundColor = new Color32( 150, 150, 150, 255 );
						}
						else // show even background
						{
							GUI.backgroundColor = new Color32( 200, 200, 200, 255 );
						}

						if( GUILayout.Button( new GUIContent( existingLabels[i], "Click to add." ), Styles.MPAssetTagLabel, GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( true ) ) )
						{
							if( !labelsToAdd.Contains( existingLabels[i] ) )
								labelsToAdd.Add( existingLabels[i] );
						}
						GUI.backgroundColor = Color.white;
					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
		}

		private void OnEnable()
		{
			List<Material> untagged = new List<Material>();
			List<Material> tagged = new List<Material>();
			string[] el = MPHelper.GetAssetLabels( "", out untagged, out tagged );
			string[] al = MPHelper.GetAllAssetLabels();

			for( int i = 0; i < el.Length; i++ )
			{
				existingLabels.Add( el[i] );
			}

			for( int i = 0; i < al.Length; i++ )
			{
				if( !existingLabels.Contains( al[i] ) )
					allLabels.Add( al[i] );
			}

			// clean up garbage
			untagged = null;
			tagged = null;
		}

		private void AddTagsToMaterial()
		{
			string[] guids = AssetDatabase.FindAssets( "t:Material " + material.name );
			string asset = AssetDatabase.GUIDToAssetPath( guids[0] );
			Material m = (Material)AssetDatabase.LoadMainAssetAtPath( asset );

			AssetDatabase.SetLabels( m, labelsToAdd.Distinct().ToArray() );
			EditorUtility.SetDirty( m );
			AssetDatabase.SaveAssets();
		}
	}
}

#endif
