using System;
using System.Collections.Generic;
using UnityEngine;

public enum Status { Success, Failure, Running }

public abstract class Node
{
    public string Label = "";

    public string DisplayName => string.IsNullOrEmpty(Label) ? GetType().Name : Label;

    // wrapper: every tick gets recorded so the debug console can show the tree
    public Status Tick()
    {
        BTTrace.Enter(this);
        Status status = OnTick();
        BTTrace.Exit(status);
        return status;
    }

    protected abstract Status OnTick();

    // clear per-run state so a finished or abandoned branch can run again
    public virtual void Reset() { }
}

// "Do A, then B, then C." Stops on the first child that isn't Success.
public class Sequence : Node
{
    private Node[] children;

    public Sequence(params Node[] nodes) => this.children = nodes;

    protected override Status OnTick()
    {
        foreach (var child in children)
        {
            Status status = child.Tick();
            if (status != Status.Success)
                return status;
        }
        return Status.Success;
    }

    public override void Reset()
    {
        foreach (var child in children)
            child.Reset();
    }
}

// "Try A, else B, else C." Priorities live here: child order = importance.
public class Selector : Node
{
    private Node[] children;

    public Selector(params Node[] nodes) => this.children = nodes;

    protected override Status OnTick()
    {
        foreach (var child in children)
        {
            Status status = child.Tick();
            if (status != Status.Failure)
                return status;
        }
        return Status.Failure;
    }

    public override void Reset()
    {
        foreach (var child in children)
            child.Reset();
    }
}

public class Condition : Node
{
    private Func<bool> func;

    public Condition(Func<bool> func) => this.func = func;

    protected override Status OnTick()
    {
        return func() ? Status.Success : Status.Failure;
    }
}

// One-shot action as a lambda.
public class Do : Node
{
    private Func<Status> act;

    public Do(Func<Status> act) => this.act = act;

    protected override Status OnTick()
    {
        return act();
    }
}

// Waits N seconds. Starts counting on the FIRST tick, not on construction.
public class Wait : Node
{
    float duration;
    float startTime = -1;

    public Wait(float duration) => this.duration = duration;

    protected override Status OnTick()
    {
        if (startTime < 0)
            startTime = Time.time;

        if (Time.time >= startTime + duration)
            return Status.Success;

        return Status.Running;
    }

    public override void Reset()
    {
        startTime = -1;
    }
}

// Resets its child whenever the child finishes (Success or Failure),
// so branches like "answer the door" can run again with fresh state.
public class AutoReset : Node
{
    private Node child;

    public AutoReset(Node child) => this.child = child;

    protected override Status OnTick()
    {
        Status status = child.Tick();
        if (status != Status.Running)
            child.Reset();
        return status;
    }

    public override void Reset()
    {
        child.Reset();
    }
}

// Records the visited nodes of the last traced tick (read by TreeConsole).
// Only the brain that calls Begin/End is traced; other trees tick untraced.
public static class BTTrace
{
    public struct Entry { public int depth; public string name; public Status status; }

    public static readonly List<Entry> LastTick = new List<Entry>();

    static readonly List<Entry> current = new List<Entry>();
    static readonly Stack<int> stack = new Stack<int>();
    static int depth;
    static bool active;

    public static void Begin() { active = true; current.Clear(); stack.Clear(); depth = 0; }

    public static void End()
    {
        active = false;
        LastTick.Clear();
        LastTick.AddRange(current);
    }

    public static void Enter(Node n)
    {
        if (!active) return;
        stack.Push(current.Count);
        current.Add(new Entry { depth = depth, name = n.DisplayName });
        depth++;
    }

    public static void Exit(Status s)
    {
        if (!active) return;
        depth--;
        int i = stack.Pop();
        var e = current[i];
        e.status = s;
        current[i] = e;
    }
}
