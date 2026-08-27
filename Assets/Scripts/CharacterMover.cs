using System.Collections.Generic;
using UnityEngine;

// Actuation layer: walks a character along checkpoint paths, every frame.
// Brains (behavior trees) only call MoveTo/Stop - they never move the transform.
public class CharacterMover : MonoBehaviour
{
    public float speed = 2.5f;
    public float arriveDistance = 0.35f;

    List<Vector3> path = new List<Vector3>();
    int index;
    Vector3 destination;
    bool hasDestination;

    public bool Arrived => !hasDestination || index >= path.Count;

    public void MoveTo(Vector3 target)
    {
        // re-asserting the same destination is free (trees call this every tick);
        // a moving target only triggers a re-path after drifting half a meter
        if (hasDestination && (destination - target).sqrMagnitude < 0.25f)
            return;

        destination = target;
        hasDestination = true;
        path = WorldManager.I.FindPath(transform.position, target);
        index = 0;
    }

    public void Stop()
    {
        hasDestination = false;
        path.Clear();
        index = 0;
    }

    void Update()
    {
        if (Arrived) return;

        Vector3 wp = path[index];
        wp.y = transform.position.y;
        Vector3 to = wp - transform.position;

        if (to.magnitude < arriveDistance)
        {
            index++;
            return;
        }

        transform.position += to.normalized * (speed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(to),
            10f * Time.deltaTime);
    }
}
