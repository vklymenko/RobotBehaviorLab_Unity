using UnityEngine;

// Robo's decision layer: ticks the tree at 10 Hz. Movement is CharacterMover's job.
// Priorities: answer the doorbell > run user commands > idle.
public class RobotBrain : MonoBehaviour
{
    Node root;
    CharacterMover mover;
    float nextTick;
    bool kidHold;

    // hysteresis: freeze when the kid is closer than 2 m,
    // resume only after they retreat past 2.5 m - no flapping at the border
    bool KidNear()
    {
        var wm = WorldManager.I;
        Vector3 d = wm.kid.position - transform.position;
        d.y = 0;
        kidHold = d.magnitude < (kidHold ? 2.5f : 2.0f);
        return kidHold;
    }

    void Start()
    {
        mover = GetComponent<CharacterMover>();
        var wm = WorldManager.I;
        Vector3 doorSpot = new Vector3(0f, 0f, -4.6f);

        root = new Selector
        (
            // top priority: a kid nearby preempts everything - stop, watch, wait
            new Sequence
            (
                new Condition(KidNear) { Label = "KidNear?" },
                new Do(() => { mover.Stop(); return Status.Success; }) { Label = "Stop" },
                new Face(transform, () => wm.kid.position) { Label = "Watch(kid)" }
            ) { Label = "KidSafety" },

            new AutoReset(new Sequence
            (
                new Condition(() => wm.doorbellPending) { Label = "Doorbell?" },
                new GoTo(mover, () => doorSpot) { Label = "GoTo(door)" },
                new Do(() => { wm.SetDoorOpen(true); return Status.Success; }) { Label = "OpenDoor" },
                new Wait(2f) { Label = "Greet" },
                new Do(() =>
                {
                    wm.SetDoorOpen(false);
                    wm.doorbellPending = false;
                    return Status.Success;
                }) { Label = "CloseDoor" }
            )) { Label = "AnswerDoor" },

            new RunCommands(this) { Label = "Commands" },

            new Idle()
        ) { Label = "Robo" };
    }

    void Update()
    {
        if (root == null) return;

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + 0.1f;
            BTTrace.Begin();
            root.Tick();
            BTTrace.End();
        }
    }

    // Runs the user command queue. Each command gets a freshly built subtree,
    // so finished commands can never leave stale state behind.
    class RunCommands : Node
    {
        RobotBrain brain;
        Node current;

        public RunCommands(RobotBrain brain) => this.brain = brain;

        protected override Status OnTick()
        {
            var wm = WorldManager.I;

            if (current == null)
            {
                if (wm.roboQueue.Count == 0)
                    return Status.Failure;
                current = brain.BuildCommand(wm.roboQueue.Peek());
            }

            Status status = current.Tick();
            if (status != Status.Running)
            {
                wm.roboQueue.Dequeue();
                current = null;
            }
            return Status.Running;
        }

        public override void Reset()
        {
            current = null;
        }
    }

    Node BuildCommand(RoboCmd cmd)
    {
        var wm = WorldManager.I;

        switch (cmd)
        {
            case RoboCmd.BringCup:
                return new Sequence
                (
                    new GoTo(mover, () => wm.cup.position, 1.0f) { Label = "GoTo(cup)" },
                    new Do(() =>
                    {
                        wm.cup.SetParent(transform);
                        wm.cup.localPosition = new Vector3(0f, 0.25f, 0.55f);
                        return Status.Success;
                    }) { Label = "PickUp" },
                    new GoTo(mover, () => wm.resident.position, 1.1f) { Label = "GoTo(Resident)" },
                    new Do(() =>
                    {
                        wm.cup.SetParent(null);
                        Vector3 p = transform.position + transform.forward * 0.6f;
                        p.y = 0.12f;
                        wm.cup.position = p;
                        return Status.Success;
                    }) { Label = "GiveCup" }
                ) { Label = "BringCup" };

            case RoboCmd.ComeHere:
                return new GoTo(mover, () => wm.resident.position, 1.1f) { Label = "ComeHere" };
        }

        return new Do(() => Status.Success) { Label = "Unknown" };
    }
}
