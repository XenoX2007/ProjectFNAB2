using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Place this on every "position" in the school hallway / rooms.
/// The player and NPCs move between these nodes — exactly like At Dead of Night.
/// </summary>
public class NavigationNode : MonoBehaviour
{
    [Header("Node Identity")]
    public string nodeName = "Hallway Node";
    public NodeType nodeType = NodeType.Hallway;

    [Header("Connected Nodes (set in Inspector)")]
    public NavigationNode nodeNorth;
    public NavigationNode nodeSouth;
    public NavigationNode nodeEast;
    public NavigationNode nodeWest;

    [Header("Room Settings")]
    [Tooltip("If true, player can hide here (e.g., under desk, behind locker)")]
    public bool canHide = false;

    [Tooltip("Assign the Room this node belongs to (null = hallway)")]
    public RoomData parentRoom;

    [Tooltip("Does this node require a Teacher Card to enter?")]
    public bool requiresCard = false;

    [Header("Camera")]
    [Tooltip("Optional: security camera watching this node")]
    public SecurityCamera watchingCamera;

    [Header("Gizmo")]
    public Color gizmoColor = Color.cyan;

    // ── Runtime ──────────────────────────────────────────────────
    private bool _isLocked = false;
    public bool IsLocked => _isLocked && requiresCard;

    public void Unlock() => _isLocked = false;
    public void Lock()   => _isLocked = true;

    /// <summary>Returns all non-null neighbour nodes.</summary>
    public List<NavigationNode> GetNeighbours()
    {
        var list = new List<NavigationNode>();
        if (nodeNorth != null) list.Add(nodeNorth);
        if (nodeSouth != null) list.Add(nodeSouth);
        if (nodeEast  != null) list.Add(nodeEast);
        if (nodeWest  != null) list.Add(nodeWest);
        return list;
    }

    /// <summary>
    /// BFS shortest-path from this node to target.
    /// Returns ordered list of nodes to traverse (not including start).
    /// Returns null if no path exists.
    /// </summary>
    public static List<NavigationNode> FindPath(NavigationNode start, NavigationNode goal,
                                                 bool ignoreCards = false)
    {
        if (start == goal) return new List<NavigationNode>();

        var prev    = new Dictionary<NavigationNode, NavigationNode>();
        var queue   = new Queue<NavigationNode>();
        var visited = new HashSet<NavigationNode>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbour in current.GetNeighbours())
            {
                if (visited.Contains(neighbour)) continue;
                if (!ignoreCards && neighbour.IsLocked)  continue;

                visited.Add(neighbour);
                prev[neighbour] = current;
                queue.Enqueue(neighbour);

                if (neighbour == goal)
                {
                    // Reconstruct path
                    var path = new List<NavigationNode>();
                    var node = goal;
                    while (node != start)
                    {
                        path.Insert(0, node);
                        node = prev[node];
                    }
                    return path;
                }
            }
        }
        return null; // no path
    }

    /// <summary>BFS distance between two nodes (-1 if unreachable).</summary>
    public static int Distance(NavigationNode a, NavigationNode b)
    {
        var path = FindPath(a, b, ignoreCards: true);
        return path == null ? -1 : path.Count;
    }

    // ── Editor gizmos ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = requiresCard ? Color.yellow : gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.18f);

        Gizmos.color = Color.white;
        if (nodeNorth) Gizmos.DrawLine(transform.position, nodeNorth.transform.position);
        if (nodeEast)  Gizmos.DrawLine(transform.position, nodeEast.transform.position);
        // South/West are already drawn from the other side
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}

public enum NodeType
{
    Hallway,
    MainRoom,
    OfficeRoom,
    ElectricalRoom,
    WC,
    TheaterPod,
    Classroom,
    Balcony,
    Stairs,
    WaitingCorner
}