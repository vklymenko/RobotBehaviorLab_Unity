using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoboCmd { BringCup, ComeHere }
public enum KidCmd { None, CheckRobo, GoToBed }
public enum GuestCmd { None, RingBell, WanderOff }

// Shared world state (blackboard) + checkpoint pathfinding. One per scene.
public class WorldManager : MonoBehaviour
{
    public static WorldManager I { get; private set; }

    public Transform robot;
    public Transform kid;
    public Transform guest;
    public Transform resident;
    public Transform fridge;
    public Transform entranceDoor;
    public Transform doorbell;
    public Transform cup;

    // world events: set by whoever causes them, cleared by whoever reacts
    [NonSerialized] public bool doorbellPending;

    // commands issued by the user via the click menu
    [NonSerialized] public Queue<RoboCmd> roboQueue = new Queue<RoboCmd>();
    [NonSerialized] public KidCmd kidCmd;
    [NonSerialized] public GuestCmd guestCmd;

    Checkpoint[] checkpoints;
    Vector3 doorClosedPos;

    void Awake()
    {
        I = this;
        checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        if (entranceDoor != null)
            doorClosedPos = entranceDoor.position;
    }

    void Start()
    {
        AddLabel(robot, "Robo");
        AddLabel(kid, "Kid");
        AddLabel(guest, "Guest");
        AddLabel(resident, "Resident");

        if (kid != null) kid.gameObject.AddComponent<KidBrain>();
        if (guest != null) guest.gameObject.AddComponent<GuestBrain>();

        gameObject.AddComponent<TreeConsole>();
        gameObject.AddComponent<ClickMenu>();
    }

    static void AddLabel(Transform t, string text)
    {
        if (t != null)
            t.gameObject.AddComponent<NameLabel>().text = text;
    }

    public void RingDoorbell()
    {
        doorbellPending = true;
        Debug.Log("DING DONG!");
    }

    // slides the door aside so "answering the door" is visible
    public void SetDoorOpen(bool open)
    {
        if (entranceDoor != null)
            entranceDoor.position = open ? doorClosedPos + new Vector3(1.25f, 0f, 0f) : doorClosedPos;
    }

    public Checkpoint Nearest(Vector3 pos)
    {
        Checkpoint best = null;
        float bestDist = float.MaxValue;
        foreach (var cp in checkpoints)
        {
            float d = (cp.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = cp; }
        }
        return best;
    }

    // BFS over the checkpoint graph, then the exact target as the last waypoint.
    public List<Vector3> FindPath(Vector3 from, Vector3 to)
    {
        var path = new List<Vector3>();
        Checkpoint start = Nearest(from);
        Checkpoint goal = Nearest(to);

        if (start != null && goal != null && start != goal)
        {
            var parent = new Dictionary<Checkpoint, Checkpoint>();
            var queue = new Queue<Checkpoint>();
            parent[start] = null;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cp = queue.Dequeue();
                if (cp == goal) break;
                foreach (var n in cp.neighbors)
                {
                    if (n == null || parent.ContainsKey(n)) continue;
                    parent[n] = cp;
                    queue.Enqueue(n);
                }
            }

            if (parent.ContainsKey(goal))
            {
                for (var cp = goal; cp != null; cp = parent[cp])
                    path.Insert(0, cp.transform.position);
            }
        }
        // start == goal: same zone, walk straight to the target

        path.Add(to);

        // don't walk BACK to our zone's node when we're already closer
        // to the next waypoint than that node is (kills path-flapping
        // when the path is rebuilt while chasing a moving target)
        if (path.Count >= 2 && (path[1] - from).sqrMagnitude <= (path[1] - path[0]).sqrMagnitude)
            path.RemoveAt(0);

        return path;
    }
}
