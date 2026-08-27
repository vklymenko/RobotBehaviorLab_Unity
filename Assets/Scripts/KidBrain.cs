using UnityEngine;

// The kid: executes silly user commands, otherwise idles in the guest room.
public class KidBrain : MonoBehaviour
{
    Node root;
    float nextTick;

    void Start()
    {
        var mover = GetComponent<CharacterMover>();
        var wm = WorldManager.I;
        Vector3 bedSpot = new Vector3(5.4f, 0f, 4.2f);
        Vector3 homeSpot = transform.position;   // start point in the guest room

        root = new Selector
        (
            new AutoReset(new Sequence
            (
                new Condition(() => wm.kidCmd == KidCmd.CheckRobo) { Label = "CheckRobo?" },
                new GoTo(mover, () => wm.robot.position, 1.4f) { Label = "GoTo(Robo)" },
                new Wait(2f) { Label = "Watch" },
                // going home is part of the command: otherwise the kid parks
                // next to Robo forever and Robo's safety branch never releases
                new GoTo(mover, () => homeSpot) { Label = "GoHome" },
                new Do(() => { wm.kidCmd = KidCmd.None; return Status.Success; }) { Label = "Done" }
            )) { Label = "CheckRobo" },

            new AutoReset(new Sequence
            (
                new Condition(() => wm.kidCmd == KidCmd.GoToBed) { Label = "GoToBed?" },
                new GoTo(mover, () => bedSpot) { Label = "GoTo(bed)" },
                new Do(() => { wm.kidCmd = KidCmd.None; return Status.Success; }) { Label = "Done" }
            )) { Label = "GoToBed" },

            new Idle()
        ) { Label = "Kid" };
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
