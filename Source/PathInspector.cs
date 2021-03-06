/*
Path - distribution of the 'Path' pathfinding system
version 2.0.1b1, April, 2011

Copyright (C) 2011 by AngryAnt, Emil Johansen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

#if EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PathRuntime;
using Resources = PathRuntime.Resources;


namespace PathEditor
{
	public class PathInspector : UnityEditor.Editor
	{
		private static List<Waypoint> s_Waypoints = new List<Waypoint> ();
		private static string[] s_WaypointNames = new string[0], s_WaypointSelectionNames = new string[0];
		private static int s_WaypointDropDownIndex = 0;

		private static bool s_ShowWaypointFoldout = true;
		private static bool s_ShowConnectionFoldout = true;
		private static bool s_ShowGizmos = true;
		private static bool s_ShowConnectionWidth = true;
		private static Color s_WaypointColour = Color.yellow;
		private static Color s_ConnectionColour = Color.green;

		private static float s_AutoConnectMaxWidth = 10;
		private static float s_MinConnectionWidth = 1.0f;
		private static float s_AutoConnectSearchStep = 0.1f;
		private static int s_AutoConnectBlockingLayer;

		private static GUIStyle s_BoldFoldoutStyle;

		// Waypoint specific
		private static string[] s_ConnectionNames = new string[0];
		private static int s_ConnectionDropDownIndex = 0, s_ConnectionFormingWaypointIndex = 0;

		const float kPlusMinusWidth = 25.0f, kDropDownRightButtonOverlap = -6;


		public void OnEnable ()
		{
			Navigation.DrawGizmosHandler = OnRenderNavigationGizmos;
			UpdateLists (target);
		}


		static void UpdateLists (Object target)
		{
			s_Waypoints = new List<Waypoint> (Navigation.Waypoints);
			string[] waypointNames = s_Waypoints.Select (waypoint => waypoint.ToString ()).ToArray ();
			s_WaypointSelectionNames = new string[waypointNames.Length + 2];
			s_WaypointSelectionNames[0] = "Select waypoint";
			s_WaypointSelectionNames[1] = "";
			System.Array.Copy (waypointNames, 0, s_WaypointSelectionNames, 2, waypointNames.Length);

			Waypoint targetWaypoint = target as Waypoint;
			if (targetWaypoint != null)
			{
				s_ConnectionNames = targetWaypoint.Connections.Select (connection => connection.ToString ()).ToArray ();
				s_ConnectionFormingWaypointIndex = 0;

				if (waypointNames.Length == 1)
				{
					s_WaypointNames = new string[0];
				}
				else
				{
					s_WaypointNames = new string[waypointNames.Length];
					System.Array.Copy (waypointNames, 0, s_WaypointNames, 0, waypointNames.Length);
				}

				s_WaypointDropDownIndex = s_Waypoints.IndexOf (targetWaypoint) + 2;
			}
			else
			{
				s_WaypointDropDownIndex = 0;
				s_ConnectionDropDownIndex = 0;
			}
		}


		public static Waypoint SelectedWaypoint
		{
			get
			{
				return s_Waypoints.Count > 0 && s_WaypointDropDownIndex > 1 ? s_Waypoints[s_WaypointDropDownIndex - 2] : null;
			}
		}


		public static Connection SelectedConnection
		{
			get
			{
				return SelectedWaypoint != null && SelectedWaypoint.Connections.Count > 0 ? SelectedWaypoint.Connections[s_ConnectionDropDownIndex] : null;
			}
		}


		public static bool ShowGizmos
		{
			get
			{
				return s_ShowGizmos;
			}
		}


		public static bool ShowConnectionWidth
		{
			get
			{
				return s_ShowConnectionWidth;
			}
		}


		public static Color WaypointColour
		{
			get
			{
				return s_WaypointColour;
			}
		}


		public static Color ConnectionColour
		{
			get
			{
				return s_ConnectionColour;
			}
		}


		private static GUIStyle BoldFoldoutStyle
		{
			get
			{
				if (s_BoldFoldoutStyle == null)
				{
					s_BoldFoldoutStyle = new GUIStyle (EditorStyles.foldout);
					s_BoldFoldoutStyle.name = "BoldFoldout";
					s_BoldFoldoutStyle.font = EditorStyles.boldFont;
				}

				return s_BoldFoldoutStyle;
			}
		}


		public override void OnInspectorGUI ()
		{
			Navigation.DrawGizmosHandler = OnRenderNavigationGizmos;

			VersionBar ();

			OnNavigationGUI (target);

			Waypoint waypoint = target as Waypoint;

			EditorGUILayout.Space ();

			GUILayout.Space (2);
			if (waypoint != null && s_ShowWaypointFoldout)
			{
				OnWaypointGUI (waypoint);
			}
		}


		public static void OnNavigationGUI (Object target)
		{
			s_ShowGizmos = EditorGUILayout.Toggle ("Gizmos", s_ShowGizmos);
			s_ShowConnectionWidth = EditorGUILayout.Toggle ("Connection width", s_ShowConnectionWidth);
			s_WaypointColour = EditorGUILayout.ColorField ("Waypoint colour", s_WaypointColour);
			s_ConnectionColour = EditorGUILayout.ColorField ("Connection colour", s_ConnectionColour);

			Navigation.SeekerIterationCap = EditorGUILayout.IntField ("Seeker iterations", Navigation.SeekerIterationCap);

			EditorGUILayout.Space ();

			GUILayout.Label ("Autoconnect", EditorStyles.boldLabel);

			s_AutoConnectMaxWidth = EditorGUILayout.FloatField ("Max test width", s_AutoConnectMaxWidth);
			s_MinConnectionWidth = EditorGUILayout.FloatField ("Min test width", s_MinConnectionWidth);
			s_AutoConnectSearchStep = EditorGUILayout.FloatField ("Test step", s_AutoConnectSearchStep);
			s_AutoConnectBlockingLayer = EditorGUILayout.LayerField ("Blocking layers", s_AutoConnectBlockingLayer);

			GUILayout.BeginHorizontal ();
				GUILayout.Space (103);
				GUILayout.BeginVertical ();
					if (GUILayout.Button ("Auto connect", EditorStyles.miniButton))
					{
						Navigation.AutoConnect (1 << s_AutoConnectBlockingLayer, s_MinConnectionWidth, s_AutoConnectMaxWidth, s_AutoConnectSearchStep);
						foreach (Waypoint waypoint in Navigation.Waypoints)
						{
							EditorUtility.SetDirty (waypoint);
						}
						EditorUtility.SetDirty (Navigation.Instance);
						UpdateLists (target);
					}
					if (GUILayout.Button ("Auto scale", EditorStyles.miniButton))
					{
						Navigation.AutoScale (1 << s_AutoConnectBlockingLayer, s_MinConnectionWidth, s_AutoConnectMaxWidth, s_AutoConnectSearchStep);
						foreach (Waypoint waypoint in Navigation.Waypoints)
						{
							EditorUtility.SetDirty (waypoint);
						}
						EditorUtility.SetDirty (Navigation.Instance);
					}
				GUILayout.EndVertical ();
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			if (GUILayout.Button ("Disconnect all", EditorStyles.miniButton))
			{
				Navigation.Disconnect ();
				foreach (Waypoint waypoint in Navigation.Waypoints)
				{
					EditorUtility.SetDirty (waypoint);
				}
				UpdateLists (target);
			}

			EditorGUILayout.Space ();

			GUILayout.Box ("", GUILayout.Height (1), GUILayout.ExpandWidth (true));

			s_ShowWaypointFoldout = EditorGUILayout.Foldout (s_ShowWaypointFoldout, "Waypoints", BoldFoldoutStyle);

			if (s_ShowWaypointFoldout)
			{
				GUILayout.BeginHorizontal ();		
					int newWaypointIndex = EditorGUILayout.Popup (s_WaypointDropDownIndex, s_WaypointSelectionNames);
					if (s_WaypointDropDownIndex != newWaypointIndex && newWaypointIndex > 1)
					{
						s_WaypointDropDownIndex = newWaypointIndex;
						SelectWaypoint (s_Waypoints[s_WaypointDropDownIndex - 2]);
					}

					GUILayout.Space (kDropDownRightButtonOverlap);

					GUI.enabled = s_WaypointDropDownIndex - 1 > 1;
					if (GUILayout.Button ("<", EditorStyles.miniButtonMid, GUILayout.Width (kPlusMinusWidth)))
					{
						s_WaypointDropDownIndex--;
						if (s_WaypointDropDownIndex == 1)
						{
							s_WaypointDropDownIndex = 0;
						}
						SelectWaypoint (s_Waypoints[s_WaypointDropDownIndex - 2]);
					}
					GUI.enabled = s_WaypointDropDownIndex + 1 < s_WaypointSelectionNames.Length;
					if (GUILayout.Button (">", EditorStyles.miniButtonMid, GUILayout.Width (kPlusMinusWidth)))
					{
						s_WaypointDropDownIndex++;
						if (s_WaypointDropDownIndex == 1)
						{
							s_WaypointDropDownIndex = s_Waypoints.Count > 0 ? 2 : 0;
						}
						SelectWaypoint (s_Waypoints[s_WaypointDropDownIndex - 2]);
					}
					GUI.enabled = true;

					if (GUILayout.Button ("+", EditorStyles.miniButtonMid, GUILayout.Width (kPlusMinusWidth)))
					{
						Waypoint newWaypoint = Navigation.RegisterWaypoint (CreateWaypoint ());
						EditorUtility.SetDirty (Navigation.Instance);
						UpdateLists (target);
						s_WaypointDropDownIndex = s_Waypoints.IndexOf (newWaypoint) + 2;
						SelectWaypoint (newWaypoint);
					}

					GUI.enabled = s_WaypointDropDownIndex > 1;
					if (GUILayout.Button ("-", EditorStyles.miniButtonRight, GUILayout.Width (kPlusMinusWidth)))
					{
						Selection.activeObject = Navigation.Instance.gameObject;
						Navigation.UnregisterWaypoint (s_Waypoints[s_WaypointDropDownIndex - 2]);
						DestroyImmediate (s_Waypoints[s_WaypointDropDownIndex - 2].gameObject);
						s_WaypointDropDownIndex = 0;
						EditorUtility.SetDirty (Navigation.Instance);
					}
					GUI.enabled = true;
				GUILayout.EndHorizontal ();
			}
		}


		public static void OnWaypointGUI (Waypoint waypoint)
		{
			GUI.changed = false;

			waypoint.Enabled = EditorGUILayout.Toggle ("Enabled", waypoint.Enabled);
			waypoint.Radius = EditorGUILayout.FloatField ("Radius", waypoint.Radius);
			waypoint.Tag = EditorGUILayout.TagField ("Tag", waypoint.Tag);

			EditorGUILayout.Space ();

			GUILayout.Box ("", GUILayout.Height (1), GUILayout.ExpandWidth (true));

			s_ShowConnectionFoldout = EditorGUILayout.Foldout (s_ShowConnectionFoldout, "Connections", BoldFoldoutStyle);

			if (!s_ShowConnectionFoldout)
			{
				if (GUI.changed)
				{
					EditorUtility.SetDirty (waypoint);
				}
				return;
			}

			if (s_ConnectionNames.Length != 0)
			{
				GUILayout.BeginHorizontal ();
					s_ConnectionDropDownIndex = EditorGUILayout.Popup (s_ConnectionDropDownIndex, s_ConnectionNames);

					GUILayout.Space (kDropDownRightButtonOverlap);

					GUI.enabled = s_ConnectionDropDownIndex - 1 >= 0;
					if (GUILayout.Button ("<", EditorStyles.miniButtonMid, GUILayout.Width (kPlusMinusWidth)))
					{
						s_ConnectionDropDownIndex--;
					}
					GUI.enabled = s_ConnectionDropDownIndex + 1 < s_ConnectionNames.Length;
					if (GUILayout.Button (">", EditorStyles.miniButtonRight, GUILayout.Width (kPlusMinusWidth)))
					{
						s_ConnectionDropDownIndex++;
					}
					GUI.enabled = true;
				GUILayout.EndHorizontal ();

				waypoint.Connections[s_ConnectionDropDownIndex].Enabled = EditorGUILayout.Toggle ("Enabled", waypoint.Connections[s_ConnectionDropDownIndex].Enabled);
				waypoint.Connections[s_ConnectionDropDownIndex].Width = EditorGUILayout.FloatField ("Width", waypoint.Connections[s_ConnectionDropDownIndex].Width);
				waypoint.Connections[s_ConnectionDropDownIndex].Weight = EditorGUILayout.FloatField ("Weight", waypoint.Connections[s_ConnectionDropDownIndex].Weight);
				waypoint.Connections[s_ConnectionDropDownIndex].Tag = EditorGUILayout.TagField ("Tag", waypoint.Connections[s_ConnectionDropDownIndex].Tag);
			}
			else
			{
				GUILayout.Label ("No outgoing connections", EditorStyles.miniLabel);
			}

			EditorGUILayout.Space ();

			if (s_WaypointNames.Length != 0)
			{
				GUILayout.Label ("New connection", EditorStyles.boldLabel);
				GUILayout.BeginHorizontal ();
					int newConnectionFormingIndex = EditorGUILayout.Popup (s_ConnectionFormingWaypointIndex, s_WaypointNames);

					if (newConnectionFormingIndex != s_Waypoints.IndexOf (waypoint))
					{
						s_ConnectionFormingWaypointIndex = newConnectionFormingIndex;
					}

					GUILayout.Space (kDropDownRightButtonOverlap);

					if (GUILayout.Button ("Connect", EditorStyles.miniButtonRight))
					{
						new Connection (waypoint, s_Waypoints[s_ConnectionFormingWaypointIndex]);
						EditorUtility.SetDirty (waypoint);
					}
				GUILayout.EndHorizontal ();
			}

			EditorGUILayout.Space ();

			if (GUILayout.Button ("Disconnect", EditorStyles.miniButton))
			{
				waypoint.Disconnect ();
				EditorUtility.SetDirty (waypoint);
				UpdateLists (waypoint);
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (waypoint);
			}
		}


		public static Waypoint CreateWaypoint ()
		{
			string name = "Waypoint ";
			int index = 0;

			while (GameObject.Find (name + ++index) != null);
			
			return Waypoint.Create (Vector3.zero, HierarchyVisibility.Hidden, name + index);
		}


		public static void SelectWaypoint (Waypoint waypoint)
		{
			Selection.activeObject = waypoint.gameObject;
			s_ConnectionDropDownIndex = 0;
			UpdateLists (waypoint);
		}


		public static void OnRenderNavigationGizmos ()
		{		
			if (!ShowGizmos)
			{
				return;
			}

			foreach (Waypoint waypoint in Navigation.Waypoints)
			{
				OnRenderWaypointGizmos (waypoint);
			}

			if (SelectedWaypoint != null)
			{
				OnRenderWaypointGizmos (SelectedWaypoint);
			}
		}


		public static void OnRenderWaypointGizmos (Waypoint waypoint)
		{
			Gizmos.color = SelectedWaypoint == waypoint ? Color.white : WaypointColour;

			Gizmos.DrawWireSphere (waypoint.Position, waypoint.Radius);
			foreach (Connection connection in waypoint.Connections)
			{
				Gizmos.color = SelectedConnection == connection ? Color.white : ConnectionColour;

				if (ShowConnectionWidth || SelectedConnection == connection)
				{
					Vector3 vector, vectorCrossNormal, fromOffsetA, fromOffsetB, toOffsetA, toOffsetB;

					vector = connection.To.Position - waypoint.Position;
					vectorCrossNormal = Vector3.Cross (vector, Vector3.up).normalized;

					fromOffsetA = 
						waypoint.Position +
						vector.normalized * waypoint.Radius +
						vectorCrossNormal * -(connection.Width / 2.0f);

					fromOffsetB =
						waypoint.Position +
						vector.normalized * waypoint.Radius +
						vectorCrossNormal * (connection.Width / 2.0f);

					toOffsetA =
						connection.To.Position - 
						vector.normalized * connection.To.Radius +
						vectorCrossNormal * -(connection.Width / 2.0f);

					toOffsetB =
						connection.To.Position -
						vector.normalized * connection.To.Radius +
						vectorCrossNormal * (connection.Width / 2.0f);

					Gizmos.DrawLine (fromOffsetA, toOffsetA);
					Gizmos.DrawLine (fromOffsetB, toOffsetB);
					Gizmos.DrawLine (fromOffsetA, fromOffsetB);
					Gizmos.DrawLine (toOffsetA, connection.To.Position);
					Gizmos.DrawLine (toOffsetB, connection.To.Position);
				}
				else
				{
					Gizmos.DrawLine (connection.From.Position, connection.To.Position);
				}
			}
		}


		public static void VersionBar ()
		{
			const float kPadding = 1.0f;
			const float kSize = 20.0f;
			GUILayout.BeginHorizontal (GUI.skin.GetStyle ("Box"), GUILayout.Height (kSize), GUILayout.ExpandHeight (false));
				Rect logoRect = GUILayoutUtility.GetRect (kSize, kSize);
	        	GUI.DrawTexture (new Rect (
					logoRect.x + kPadding,
					logoRect.y + kPadding,
					logoRect.width - 2 * kPadding,
					logoRect.height - 2 * kPadding),
					Resources.Logo);
				GUILayout.Space (6.0f);
				GUILayout.Label ("Path version " + Resources.Version);
				GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}
	}
}
#endif
