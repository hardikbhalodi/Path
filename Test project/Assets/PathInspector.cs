using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class PathInspector : Editor
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
					AutoConnect (1 << s_AutoConnectBlockingLayer, s_AutoConnectMaxWidth);
					UpdateLists (target);
				}
				if (GUILayout.Button ("Auto scale", EditorStyles.miniButton))
				{
					AutoScale (1 << s_AutoConnectBlockingLayer, s_AutoConnectMaxWidth);
					EditorUtility.SetDirty (Navigation.Instance);
				}
			GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();
		
		EditorGUILayout.Space ();
		
		if (GUILayout.Button ("Disconnect all", EditorStyles.miniButton))
		{
			foreach (Waypoint waypoint in s_Waypoints)
			{
				waypoint.Disconnect ();
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
					SelectWaypoint (s_Waypoints[s_WaypointDropDownIndex - 2]);
				}
				GUI.enabled = s_WaypointDropDownIndex + 1 < s_WaypointSelectionNames.Length;
				if (GUILayout.Button (">", EditorStyles.miniButtonMid, GUILayout.Width (kPlusMinusWidth)))
				{
					s_WaypointDropDownIndex++;
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

				GUI.enabled = s_ConnectionDropDownIndex - 1 > 1;
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
		
		GameObject go = new GameObject (name + index);
		go.hideFlags = HideFlags.HideInHierarchy;
		
		return go.AddComponent<Waypoint> ();
	}
	
	
	public static void SelectWaypoint (Waypoint waypoint)
	{
		Selection.activeObject = waypoint.gameObject;
		s_ConnectionDropDownIndex = 0;
		UpdateLists (waypoint);
	}
	
	
	public static void AutoConnect (LayerMask layerMask, float maxWidth)
	{
		foreach (Waypoint one in s_Waypoints)
		{
			foreach (Waypoint other in s_Waypoints)
			{
				if (one == other)
				{
					continue;
				}
				
				float radius = maxWidth;

				while (radius > s_MinConnectionWidth)
				{
					RaycastHit hit;
					if (!Physics.CheckSphere (one.Position, radius, layerMask) && 
						!Physics.SphereCast (
							one.Position,
							radius,
							other.Position - one.Position,
							out hit,
							(other.Position - one.Position).magnitude,
							layerMask
						)
					)
					{
						new Connection (one, other).Width = radius * 2.0f;
						EditorUtility.SetDirty (one);
						break;
					}
					
					radius -= s_AutoConnectSearchStep;
				}
			}
		}
	}
	
	
	public static void AutoScale (LayerMask layerMask, float maxWidth)
	{
		foreach (Waypoint waypoint in s_Waypoints)
		{
			float radius = maxWidth;

			while (radius > s_MinConnectionWidth)
			{
				if (!Physics.CheckSphere (waypoint.Position, radius, layerMask))
				{
					waypoint.Radius = radius;
					EditorUtility.SetDirty (waypoint);
					break;
				}
				
				radius -= s_AutoConnectSearchStep;
			}
		}
	}
}
