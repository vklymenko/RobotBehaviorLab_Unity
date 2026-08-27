using UnityEngine;

// Static navigation node. The apartment never changes, so the graph is hand-placed.
public class Checkpoint : MonoBehaviour
{
    public Checkpoint[] neighbors;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.12f);
        if (neighbors == null) return;
        foreach (var n in neighbors)
            if (n != null)
                Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
