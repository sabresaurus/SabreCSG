#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG.Volumes
{
	public static class TriggerVolumeUIUtils
	{
		public static List<TriggerVolumeSendMessageEvent> DrawSendMessageEventInspector( GUIContent label, List<TriggerVolumeSendMessageEvent> events )
		{
			List<TriggerVolumeSendMessageEvent> evnts = events;

			GUILayout.BeginVertical( "Box", GUILayout.ExpandWidth( true ) );
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label( label );

					GUILayout.FlexibleSpace();

					if( GUILayout.Button( "+", EditorStyles.miniButton ) )
					{
						evnts.Add( new TriggerVolumeSendMessageEvent( null, string.Empty, true ) );
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginVertical( "Box", GUILayout.ExpandWidth( true ) );
				{
					GUILayout.Space( 2 );

					for( int i = 0; i < evnts.Count; i++ )
					{
						GUILayout.BeginVertical();
						{
							evnts[i].typeCode = (TriggerVolumeParamTypeCode)EditorGUILayout.EnumPopup( new GUIContent( "Event Param Type" ), evnts[i].typeCode );

							GUILayout.BeginHorizontal();
							{
								evnts[i].target = (GameObject)EditorGUILayout.ObjectField( evnts[i].target, typeof( GameObject ), true );
								evnts[i].message = EditorGUILayout.TextField( evnts[i].message );

								switch( evnts[i].typeCode )
								{
									case TriggerVolumeParamTypeCode.None:
										break;// do nothing, there is nothing to show

									case TriggerVolumeParamTypeCode.Bool:
										evnts[i].value = EditorGUILayout.Toggle( Convert.ToBoolean( evnts[i].value ) );
										break;

									case TriggerVolumeParamTypeCode.String:
										evnts[i].value = EditorGUILayout.TextField( Convert.ToString( evnts[i].value ) );
										break;

									case TriggerVolumeParamTypeCode.Int:
										evnts[i].value = EditorGUILayout.IntField( Convert.ToInt32( evnts[i].value ) );
										break;

									case TriggerVolumeParamTypeCode.Float:
										evnts[i].value = EditorGUILayout.FloatField( Convert.ToSingle( evnts[i].value ) );
										break;

									default:
										break;
								}

								if( GUILayout.Button( "-", EditorStyles.miniButton ) )
								{
									events.RemoveAt( i );
								}
							}
							GUILayout.EndHorizontal();
						}
						GUILayout.EndVertical();
					}
				}
				GUILayout.EndVertical();
			}

			GUILayout.EndVertical();

			return evnts;
		}
	}
}

#endif
