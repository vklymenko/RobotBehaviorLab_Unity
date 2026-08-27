using UnityEngine;

// The guest: waits outside; on command walks to the doorbell and rings it.
public class GuestBrain : MonoBehaviour
{
    Node root;
    float nextTick;

    void Start()
    {
        var mover = GetComponent<CharacterMover>();
        var wm = WorldManager.I;
        Vector3 bellSpot = new Vector3(0.9f, 0f, -6.9f);
        Vector3 wanderSpot = new Vector3(2.4f, 0f, -8.1f);

        root = new Selector
        (
            new AutoReset(new Sequence
            (
                new Condition(() => wm.guestCmd == GuestCmd.RingBell) { Label = "RingBell?" },
                new GoTo(mover, () => bellSpot, 0.4f) { Label = "GoTo(bell)" },
                new Wait(0.5f) { Label = "Press" },
                new Do(() =>
                {
                    wm.RingDoorbell();
                    wm.guestCmd = GuestCmd.None;
                    return Status.Success;
                }) { Label = "Ring" }
            )) { Label = "RingBell" },

            new AutoReset(new Sequence
            (
                new Condition(() => wm.guestCmd == GuestCmd.WanderOff) { Label = "Wander?" },
                new GoTo(mover, () => wanderSpot, 0.4f) { Label = "GoTo(away)" },
                new Do(() => { wm.guestCmd = GuestCmd.None; return Status.Success; }) { Label = "Done" }
            )) { Label = "WanderOff" },

            new Idle()
        ) { Label = "Guest" };
    }

    void Update()
    {
        if (root == null) return;

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + 0.1f;
            root.Tick();
        }
    }
}
