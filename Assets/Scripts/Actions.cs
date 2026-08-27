using System;
using UnityEngine;

// Leaf nodes that act in the world. Decision only: they assert intent
// on the CharacterMover; the mover does the actual walking every frame.

public class GoTo : Node
{
    CharacterMover mover;
    Func<Vector3> target;   // lambda, so moving targets (a person) work too
    float stopDistance;

    bool moving;

    public GoTo(CharacterMover mover, Func<Vector3> target, float stopDistance = 0.7f)
    {
        this.mover = mover;
        this.target = target;
        this.stopDistance = stopDistance;
    }

    protected override Status OnTick()
    {
        Vector3 t = target();
        Vector3 flat = t - mover.transform.position;
        flat.y = 0;

        if (flat.magnitude <= stopDistance)
        {
            // stop only on the Running -> Success transition; a re-checked,
            // already-satisfied GoTo must have no side effects, or it kills
            // the movement of whatever node runs after it in the sequence
            if (moving) { mover.Stop(); moving = false; }
            return Status.Success;
        }

        mover.MoveTo(t);
        moving = true;
        return Status.Running;
    }

    public override void Reset()
    {
        moving = false;
    }
}

// Stand still and keep facing a target. Always Running - the guard
// condition above it in the sequence decides when this ends.
public class Face : Node
{
    Transform self;
    Func<Vector3> target;

    public Face(Transform self, Func<Vector3> target)
    {
        this.self = self;
        this.target = target;
    }

    protected override Status OnTick()
    {
        Vector3 dir = target() - self.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            self.rotation = Quaternion.Slerp(self.rotation, Quaternion.LookRotation(dir), 0.3f);
        return Status.Running;
    }
}

public class Idle : Node
{
    protected override Status OnTick()
    {
        return Status.Running;
    }
}
