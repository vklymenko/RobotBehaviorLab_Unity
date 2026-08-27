using UnityEngine;

// Floating name above a character, always facing the camera.
public class NameLabel : MonoBehaviour
{
    public string text = "";

    Transform label;

    void Start()
    {
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);

        // characters are uniformly scaled capsules - compensate so all labels match
        float s = transform.localScale.x;
        go.transform.localScale = Vector3.one / s;
        go.transform.localPosition = new Vector3(0f, (transform.localScale.y + 0.45f) / s, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 64;
        tm.characterSize = 0.06f;
        tm.color = new Color(0.1f, 0.1f, 0.12f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;

        label = go.transform;
    }

    void LateUpdate()
    {
        if (label != null && Camera.main != null)
            label.rotation = Camera.main.transform.rotation;
    }
}
