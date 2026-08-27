using System.Collections.Generic;
using UnityEngine;

// Console in the top-left corner: appends Robo's tree state whenever it changes
// and always shows the tail (auto-scroll). Colors: yellow = Running,
// green = Success, gray = Failure. Not-visited branches simply don't appear -
// preemption is literally "stopped being asked".
public class TreeConsole : MonoBehaviour
{
    List<string> log = new List<string>();
    string lastSig = "";
    GUIStyle style;

    void LateUpdate()
    {
        if (BTTrace.LastTick.Count == 0) return;

        var lines = new List<string>();
        var sig = new List<string>();
        foreach (var e in BTTrace.LastTick)
        {
            string indent = new string(' ', e.depth * 3);
            sig.Add(indent + e.name + ":" + e.status);
            lines.Add($"{indent}{e.name} <color={ColorOf(e.status)}>{e.status}</color>");
        }

        string signature = string.Join("\n", sig);
        if (signature == lastSig) return;
        lastSig = signature;

        log.Add($"<color=#6ab0f9>[{Time.time:F1}s]</color>");
        log.AddRange(lines);
        if (log.Count > 400)
            log.RemoveRange(0, log.Count - 400);
    }

    static string ColorOf(Status s)
    {
        if (s == Status.Running) return "#e2c04c";
        if (s == Status.Success) return "#63d063";
        return "#8a8a8a";
    }

    void OnGUI()
    {
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 24 };

        float height = Screen.height * 0.5f;
        var box = new Rect(8, 8, 620, height);
        GUI.color = new Color(1f, 1f, 1f, 0.35f);
        GUI.Box(box, GUIContent.none);
        GUI.color = Color.white;

        int max = (int)((height - 16) / 28);
        int start = Mathf.Max(0, log.Count - max);
        float y = box.y + 8;
        for (int i = start; i < log.Count; i++)
        {
            GUI.Label(new Rect(box.x + 10, y, box.width - 20, 28), log[i], style);
            y += 28;
        }
    }
}
