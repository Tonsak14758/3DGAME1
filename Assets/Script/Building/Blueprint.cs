/*
Date: 5/10/2026
Author: Tony
Use by: Nothing to set up. BlueprintPlacer makes blueprints with Blueprint.Create(...).
Purpose: A building that is planned but not built yet: a see-through blue copy of the building's prefab.
         - While placing: no collisions, blue = can be placed, red = can't.
         - Placed: each piece (BuildPart) is blue until the player adds its item, then it becomes the real, solid piece.
           Blue pieces are triggers: the crosshair can aim at them, but nothing bumps into them.
         - Every piece added = the real building replaces the blueprint.
         - Removing gives every added item back (only if they all fit in the inventory).
         The copy's own scripts are removed (except BuildPart), so e.g. a blueprint of an InteractionMachine
         doesn't already work as a machine.
*/

using System.Collections.Generic;
using UnityEngine;

public class Blueprint : MonoBehaviour
{
    // ===== Fields =====
    // Tuning: the hologram colors (A = see-through amount).
    static readonly Color ValidColor = new Color(0.3f, 0.65f, 1f, 0.35f);
    static readonly Color InvalidColor = new Color(1f, 0.25f, 0.25f, 0.35f);
    const float OverlapMargin = 0.02f;   // pieces may touch other things by this much (e.g. sitting on the ground)

    // One piece of the recipe and everything needed to switch it between "blue" and "real".
    class Part
    {
        public BuildPart piece;
        public Renderer[] renderers;
        public Material[][] realMaterials;   // the piece's own materials, to put back when it is added
        public Collider[] colliders;
        public bool[] realIsTrigger;
        public PlayerInventory.Slot added;   // the item that was put in (null = still blue)

        public bool IsAdded => added != null;
    }

    // One row of the recipe: an item and how many of it.
    public struct Line
    {
        public string itemId;
        public int need;    // pieces of this item in the building
        public int added;   // pieces already added
    }

    static Transform holder;    // switched-off parent: copies made under it don't run Awake
    static Material validMat;   // shared by every blueprint (made once)
    static Material invalidMat;
    static readonly Collider[] overlapBuffer = new Collider[32];   // reused, so checking makes no garbage

    BuildingData data;
    readonly List<Part> parts = new List<Part>();
    Renderer[] allRenderers;    // every shape, found once (the placing checks run every frame)
    Renderer[] otherRenderers;  // shapes without BuildPart: blue until the whole building is finished
    Collider[] otherColliders;
    bool placed;

    public BuildingData Data => data;
    public bool IsPlaced => placed;

    // Fired when a piece is added (e.g. the panel redraws).
    public event System.Action Changed;

    // ===== My methods =====

    // Makes a new blueprint of this building (in "placing" mode). Move it, then call Place() or Destroy it.
    public static Blueprint Create(BuildingData data)
    {
        if (holder == null)
        {
            GameObject h = new GameObject("Blueprint Holder (inactive)");
            h.SetActive(false);   // children of a switched-off object don't run Awake
            holder = h.transform;
        }

        GameObject go = Instantiate(data.Prefab, holder);
        go.name = data.DisplayName + " (Blueprint)";
        RemoveScripts(go);

        Blueprint bp = go.AddComponent<Blueprint>();
        bp.Setup(data);

        go.transform.SetParent(null);   // out of the holder: now it shows in the world
        return bp;
    }

    // Takes out every script, Rigidbody and joint, so the copy only looks like the building.
    // DestroyImmediate: Destroy waits until the end of the frame, and the copy is switched on before that.
    static void RemoveScripts(GameObject go)
    {
        MonoBehaviour[] scripts = go.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = scripts.Length - 1; i >= 0; i--)   // backwards: scripts that need others are usually added later
        {
            if (!(scripts[i] is BuildPart)) DestroyImmediate(scripts[i]);
        }

        foreach (Joint j in go.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
        foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
    }

    void Setup(BuildingData buildingData)
    {
        data = buildingData;

        foreach (BuildPart piece in GetComponentsInChildren<BuildPart>(true))
        {
            Part p = new Part { piece = piece };
            p.renderers = piece.GetComponentsInChildren<Renderer>(true);
            p.realMaterials = new Material[p.renderers.Length][];
            for (int i = 0; i < p.renderers.Length; i++) p.realMaterials[i] = p.renderers[i].sharedMaterials;

            p.colliders = piece.GetComponentsInChildren<Collider>(true);
            p.realIsTrigger = new bool[p.colliders.Length];
            for (int i = 0; i < p.colliders.Length; i++) p.realIsTrigger[i] = p.colliders[i].isTrigger;

            parts.Add(p);
        }

        if (parts.Count == 0)
        {
            Debug.LogWarning(data.DisplayName + ": the prefab has no BuildPart, so the blueprint needs nothing. " +
                             "Add BuildPart to every shape.");
        }

        allRenderers = GetComponentsInChildren<Renderer>(true);
        otherRenderers = FindOutsideParts(allRenderers);
        otherColliders = FindOutsideParts(GetComponentsInChildren<Collider>(true));

        SetCollidersOn(false);   // while placing: nothing to bump into or aim at
        SetValid(true);
    }

    // Only the components that are not inside any BuildPart.
    static T[] FindOutsideParts<T>(T[] all) where T : Component
    {
        List<T> list = new List<T>();
        foreach (T c in all)
        {
            if (c.GetComponentInParent<BuildPart>(true) == null) list.Add(c);
        }
        return list.ToArray();
    }

    // ----- Placing -----

    // Blue = can be placed here, red = can't.
    public void SetValid(bool valid)
    {
        Material mat = GetHologram(valid);

        foreach (Part p in parts)
        {
            if (!p.IsAdded) Paint(p.renderers, mat);
        }
        Paint(otherRenderers, mat);
    }

    // Puts the blueprint down where it is: the crosshair can now aim at it.
    public void Place()
    {
        placed = true;
        SetCollidersOn(true);
        SetValid(true);
    }

    // True if any piece is inside something else (a wall, a tree, the player, another blueprint...).
    public bool Overlaps()
    {
        foreach (Renderer r in allRenderers)
        {
            GetBox(r, out Vector3 center, out Vector3 halfSize, out Quaternion turn);

            int count = Physics.OverlapBoxNonAlloc(center, halfSize, overlapBuffer, turn,
                                                   Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (Blocks(overlapBuffer[i])) return true;
            }
        }
        return false;
    }

    // True if something under the building holds it up, or a piece is made of a floating material.
    public bool IsSupported()
    {
        foreach (Part p in parts)
        {
            if (p.piece.Material != null && p.piece.Material.Floats) return true;
        }

        // Look a little way down from the bottom of every piece. One touching the ground is enough.
        foreach (Renderer r in allRenderers)
        {
            Bounds b = r.bounds;
            Vector3 from = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);

            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 0.2f,
                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    // Should this collider stop the blueprint from being placed?
    bool Blocks(Collider other)
    {
        if (other.transform.IsChildOf(transform)) return false;                       // our own piece
        if (other.GetComponentInParent<ToolHitReporter>() != null) return false;     // the player's held tool
        if (other.isTrigger) return other.GetComponentInParent<Blueprint>() != null; // only other blueprints' triggers
        return true;
    }

    // A box around one shape that turns with it (tighter than Unity's bounds, which never turn).
    static void GetBox(Renderer r, out Vector3 center, out Vector3 halfSize, out Quaternion turn)
    {
        MeshFilter mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds mb = mf.sharedMesh.bounds;   // the mesh's own box, before scaling and turning
            center = r.transform.TransformPoint(mb.center);
            halfSize = Vector3.Scale(mb.extents, r.transform.lossyScale);
            turn = r.transform.rotation;
        }
        else
        {
            center = r.bounds.center;
            halfSize = r.bounds.extents;
            turn = Quaternion.identity;
        }

        // A little smaller, so touching (e.g. standing on the ground) doesn't count as inside.
        halfSize = new Vector3(Mathf.Abs(halfSize.x), Mathf.Abs(halfSize.y), Mathf.Abs(halfSize.z));
        halfSize = Vector3.Max(halfSize - Vector3.one * OverlapMargin, Vector3.one * 0.001f);
    }

    // ----- Building it -----

    // The recipe: one line per item, with how many are needed and added. In the order the pieces appear.
    public List<Line> GetLines()
    {
        List<Line> lines = new List<Line>();
        foreach (Part p in parts)
        {
            string id = p.piece.ItemId;
            int index = lines.FindIndex(l => l.itemId == id);
            if (index < 0)
            {
                lines.Add(new Line { itemId = id });
                index = lines.Count - 1;
            }

            Line line = lines[index];   // a struct is a copy: change it, then put it back
            line.need++;
            if (p.IsAdded) line.added++;
            lines[index] = line;
        }
        return lines;
    }

    // Puts one of this item from the inventory into the next blue piece that needs it.
    public bool AddOne(string itemId)
    {
        Part p = parts.Find(x => !x.IsAdded && x.piece.ItemId == itemId);
        if (p == null) return false;   // no piece needs it any more

        if (!TryFill(p)) return false;
        AfterAdding();
        return true;
    }

    // Adds every piece the player has the items for. Returns how many were added.
    public int AddAll()
    {
        int count = 0;
        foreach (Part p in parts)
        {
            if (!p.IsAdded && TryFill(p)) count++;
        }

        if (count > 0) AfterAdding();
        return count;
    }

    // Removes the blueprint and gives back every added item.
    // Returns false (and keeps the blueprint) if they wouldn't all fit in the inventory.
    public bool TryRemove(out string problem)
    {
        if (!TryGiveBackAll(out problem)) return false;

        Destroy(gameObject);
        return true;
    }

    // Takes every added item back out, but keeps the blueprint: all pieces turn blue again.
    // Returns false (and changes nothing) if they wouldn't all fit in the inventory.
    public bool TryRemoveAllMaterials(out string problem)
    {
        if (!TryGiveBackAll(out problem)) return false;

        foreach (Part p in parts)
        {
            if (p.IsAdded) Empty(p);
        }

        Changed?.Invoke();
        return true;
    }

    // Takes ONE added piece of this item back out (the last one added), and turns it blue again.
    // Returns false (and changes nothing) if none was added, or it doesn't fit in the inventory.
    public bool TryRemoveOne(string itemId, out string problem)
    {
        Part p = parts.FindLast(x => x.IsAdded && x.piece.ItemId == itemId);
        if (p == null)
        {
            problem = "None of this has been added.";
            return false;
        }

        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null || !inv.Add(p.added.id, p.added.name, p.added.icon, p.added.count, p.added.props))
        {
            problem = "Inventory full: make room to get the material back.";
            return false;
        }

        Empty(p);
        problem = null;
        Changed?.Invoke();
        return true;
    }

    // Turns an added piece back into a blue, see-through one (its item must already be given back).
    void Empty(Part p)
    {
        p.added = null;
        Paint(p.renderers, GetHologram(true));
        for (int i = 0; i < p.colliders.Length; i++) SetTrigger(p.colliders[i], true);
    }

    // True if any piece has been added (e.g. to tell the player there is nothing to take out).
    public bool AnyAdded => parts.Exists(p => p.IsAdded);

    // Puts every added item back into the inventory, if they all fit. Doesn't change the pieces.
    bool TryGiveBackAll(out string problem)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        List<PlayerInventory.Slot> refunds = new List<PlayerInventory.Slot>();
        foreach (Part p in parts)
        {
            if (p.IsAdded) refunds.Add(p.added);
        }

        if (inv != null && !inv.CanAddAll(refunds))
        {
            problem = "Inventory full: make room to get the materials back.";
            return false;
        }

        foreach (PlayerInventory.Slot s in refunds)
        {
            inv.Add(s.id, s.name, s.icon, s.count, s.props);
        }

        problem = null;
        return true;
    }

    // Takes the item out of the inventory and turns the piece real.
    bool TryFill(Part p)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) return false;

        PlayerInventory.Slot taken = inv.TakeOne(p.piece.ItemId);
        if (taken == null) return false;   // the player has none

        p.added = taken;
        for (int i = 0; i < p.renderers.Length; i++) p.renderers[i].sharedMaterials = p.realMaterials[i];
        for (int i = 0; i < p.colliders.Length; i++) SetTrigger(p.colliders[i], p.realIsTrigger[i]);   // solid now
        return true;
    }

    void AfterAdding()
    {
        Changed?.Invoke();
        if (parts.TrueForAll(x => x.IsAdded)) Finish();
    }

    // Every piece is in: swap the blueprint for the real building (with its scripts working).
    void Finish()
    {
        Instantiate(data.Prefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    // ----- Look -----

    // Placing: all off. Placed: on, and blue pieces are triggers (aimable, but nothing bumps into them).
    void SetCollidersOn(bool on)
    {
        foreach (Part p in parts)
        {
            for (int i = 0; i < p.colliders.Length; i++)
            {
                p.colliders[i].enabled = on;
                SetTrigger(p.colliders[i], p.IsAdded ? p.realIsTrigger[i] : true);
            }
        }

        foreach (Collider c in otherColliders)
        {
            c.enabled = on;
            SetTrigger(c, true);
        }
    }

    // Unity doesn't allow a non-convex Mesh Collider to be a trigger, so those stay solid.
    static void SetTrigger(Collider c, bool trigger)
    {
        if (trigger && c is MeshCollider mc && !mc.convex) return;
        c.isTrigger = trigger;
    }

    static void Paint(Renderer[] renderers, Material mat)
    {
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static Material GetHologram(bool valid)
    {
        // "Sprites/Default" is a simple see-through shader that is always in the game build.
        if (validMat == null) validMat = new Material(Shader.Find("Sprites/Default")) { color = ValidColor };
        if (invalidMat == null) invalidMat = new Material(Shader.Find("Sprites/Default")) { color = InvalidColor };
        return valid ? validMat : invalidMat;
    }
}
