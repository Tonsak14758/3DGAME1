/*
Date: 2/10/2026
Author: Tony
Use by: Nothing to set up. ItemGrabber and AutoCollector call ItemTemplates.Store(...) when they collect an item,
        and ItemDropper calls ItemTemplates.Spawn(...) to put it back into the world.
Purpose: Collected items are destroyed, so this keeps one hidden copy (a "template") of each kind of item.
         Dropping an item makes a new copy of the template, with the same mesh, colors, scripts and properties.
         The templates live under a hidden, switched-off object, so they never show, move or run scripts.
*/

using System.Collections.Generic;
using UnityEngine;

public static class ItemTemplates
{
    // ===== Fields =====
    static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
    static Transform holder;   // switched off: everything under it is frozen and invisible

    // ===== My methods =====

    // The key for one kind of item: Item Id + its properties (a red and a blue stone are different kinds).
    public static string KeyFor(string itemId, ItemProperties props)
    {
        return props != null ? itemId + "|" + props.Key : itemId;
    }

    // Saves a hidden copy of an item. Call it BEFORE the item is hidden or destroyed.
    public static void Store(string key, GameObject original)
    {
        if (templates.TryGetValue(key, out GameObject existing) && existing != null) return;   // already have one

        if (holder == null)
        {
            GameObject h = new GameObject("ItemTemplates (hidden)");
            h.SetActive(false);              // switched off = its children don't run Awake, Update or physics
            Object.DontDestroyOnLoad(h);     // keep the templates when a new scene loads
            holder = h.transform;
        }

        // Copy it straight under the switched-off holder, so the copy never runs.
        GameObject copy = Object.Instantiate(original, holder, true);   // true = keep its world size
        copy.name = original.name;
        copy.SetActive(true);   // "on" by itself, so the copies made from it are on too
        templates[key] = copy;
    }

    public static bool Has(string key)
    {
        return templates.TryGetValue(key, out GameObject t) && t != null;
    }

    // Makes a new copy of the item in the world. Returns null if there is no template for this key.
    public static GameObject Spawn(string key, Vector3 position)
    {
        if (!templates.TryGetValue(key, out GameObject template) || template == null) return null;

        GameObject go = Object.Instantiate(template, position, template.transform.rotation);   // no parent = in the scene
        go.name = template.name;   // without "(Clone)"
        return go;
    }
}
