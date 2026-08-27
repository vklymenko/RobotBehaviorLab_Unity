using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Click an actor -> menu of commands next to them. No colliders in this demo,
// so picking is by screen distance. Drawing is IMGUI; clicks come from the
// Input System (IMGUI button events are unreliable with the new input backend).
public class ClickMenu : MonoBehaviour
{
    struct Item
    {
        public string label;
        public Action act;
    }

    Transform selected;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2 mp = mouse.position.ReadValue();                    // origin: bottom-left
        Vector2 gui = new Vector2(mp.x, Screen.height - mp.y);      // origin: top-left

        if (selected != null)
        {
            var items = ItemsFor(selected.name);
            Rect panel = PanelRect(selected, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                if (ItemRect(panel, i).Contains(gui))
                {
                    items[i].act();
                    selected = null;
                    return;
                }
            }
            if (panel.Contains(gui)) return;
            selected = null;                                        // clicked elsewhere - close
        }

        // pick the nearest actor within 70 px of the click
        var wm = WorldManager.I;
        Transform best = null;
        float bestDist = 70f;
        foreach (var t in new[] { wm.resident, wm.kid, wm.guest })
        {
            if (t == null) continue;
            Vector3 sp = Camera.main.WorldToScreenPoint(t.position);
            if (sp.z < 0) continue;
            float d = Vector2.Distance(mp, new Vector2(sp.x, sp.y));
            if (d < bestDist) { bestDist = d; best = t; }
        }
        selected = best;
    }

    Item[] ItemsFor(string actor)
    {
        var wm = WorldManager.I;
        switch (actor)
        {
            case "Resident": return new[]
            {
                new Item { label = "Bring me the cup", act = () => wm.roboQueue.Enqueue(RoboCmd.BringCup) },
                new Item { label = "Robo, come here", act = () => wm.roboQueue.Enqueue(RoboCmd.ComeHere) },
            };
            case "Kid": return new[]
            {
                new Item { label = "Go check what Robo is doing", act = () => wm.kidCmd = KidCmd.CheckRobo },
                new Item { label = "Go to bed", act = () => wm.kidCmd = KidCmd.GoToBed },
            };
            case "Guest": return new[]
            {
                new Item { label = "Go ring the doorbell", act = () => wm.guestCmd = GuestCmd.RingBell },
                new Item { label = "Wander off", act = () => wm.guestCmd = GuestCmd.WanderOff },
            };
        }
        return Array.Empty<Item>();
    }

    GUIStyle headerStyle;
    GUIStyle itemStyle;

    Rect PanelRect(Transform actor, int count)
    {
        Vector3 sp = Camera.main.WorldToScreenPoint(actor.position);
        return new Rect(sp.x + 26f, Screen.height - sp.y - 30f, 430f, 46f + count * 58f);
    }

    Rect ItemRect(Rect panel, int i)
    {
        return new Rect(panel.x + 8f, panel.y + 42f + i * 58f, panel.width - 16f, 52f);
    }

    void OnGUI()
    {
        if (selected == null) return;

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.box) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            itemStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        }

        var items = ItemsFor(selected.name);
        Rect panel = PanelRect(selected, items.Length);
        GUI.Box(panel, selected.name, headerStyle);
        GUI.Box(panel, GUIContent.none);

        Vector2 gui = Vector2.zero;
        if (Mouse.current != null)
        {
            Vector2 mp = Mouse.current.position.ReadValue();
            gui = new Vector2(mp.x, Screen.height - mp.y);
        }

        for (int i = 0; i < items.Length; i++)
        {
            Rect r = ItemRect(panel, i);
            GUI.color = r.Contains(gui) ? new Color(1f, 1f, 0.6f) : Color.white;
            GUI.Box(r, items[i].label, itemStyle);
        }
        GUI.color = Color.white;
    }
}
