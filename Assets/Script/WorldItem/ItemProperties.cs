/*
Date: 2/10/2026
Author: Tony
Use by: Nothing to set up. ItemGrabber and AutoCollector call ItemProperties.Read(...) when the player
        collects an item, and the result is stored with the stack in PlayerInventory.
Purpose: A snapshot of what a grabbed item is: its resource (Stone...), hardness, toughness,
         color, material color, shape, size, weight and edge (thickness, angle, sharpness, edge strength).
         Sharpness is worked out from the shape and hardness (see SharpnessMath), never set by hand.
         Crafting will use these numbers later to calculate the stats of what the player builds.
         Items only stack together when ALL these properties match (a red stone and a blue stone don't stack).
*/

using System.Globalization;   // CultureInfo: write numbers the same way on every computer
using UnityEngine;

[System.Serializable]   // lets Unity show it in the Inspector (e.g. in PlayerInventory's slots)
public class ItemProperties
{
    // ===== Fields =====
    // Read once when the item is grabbed. Don't change them afterwards, or stacks get mixed up.
    public MaterialData material;   // the resource asset (null = none was set)
    public string resource;         // resource name, e.g. "Stone" (the Item Id if no material is set)
    public float hardness;          // from the material, 0 - 10
    public float toughness;         // from the material, 0 - 10
    public Color color;            // the color you see (ShapeColor's color, or else the material's color)
    public Color materialColor;     // the color set in the material asset itself
    public string renderMaterial;   // the material asset's name, e.g. "Default-Material"
    public string shape;            // the mesh, e.g. "Cube", "Sphere" ("Multi (3 parts)" for multi-part objects)
    public Vector3 size;            // width, height, depth in world units
    public float weight;            // see Read() for how it is worked out

    [Header("Edge (of the sharpest part, see SharpnessMath)")]
    public EdgeShape edgeShape;     // Box, Wedge or Round
    public float thickness;         // t: the thinnest side, the REAL size (not the collider)
    public float edgeWidth;         // w: from the edge to the back
    public float edgeLength;        // L: how long the edge is
    public float edgeAngle;         // degrees: 0 = infinitely thin, 90 = no edge
    public float sharpness;         // 0 - 10, from the edge angle and hardness
    public float edgeStrength;      // from toughness and thickness: weak edges chip

    // One line of text that describes everything above. Same Key = same item = can stack.
    public string Key =>
        string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4:F2}|{5:F2}",
                      resource, shape,
                      ColorUtility.ToHtmlStringRGBA(color),           // e.g. "FF0000FF" for red
                      ColorUtility.ToHtmlStringRGBA(materialColor),
                      weight,                                         // F2 = 2 decimals
                      sharpness);

    // ===== My methods =====

    // True if two items have the same properties (so they can stack).
    public static bool Same(ItemProperties a, ItemProperties b)
    {
        if (a == null || b == null) return a == b;   // both null = same, only one null = different
        return a.Key == b.Key;
    }

    // Reads the properties of an object in the world.
    // material: the resource asset (can be null). fallbackName: used as the resource name if material is null.
    // edgeSetting: what kind of edge the object has (Auto = guess from the mesh name).
    public static ItemProperties Read(GameObject go, MaterialData material, string fallbackName,
                                      EdgeShape edgeSetting = EdgeShape.Auto)
    {
        ItemProperties p = new ItemProperties();

        // --- Resource ---
        p.material = material;
        p.resource = material != null ? material.DisplayName : fallbackName;
        p.hardness = material != null ? material.Hardness : 0f;
        p.toughness = material != null ? material.Toughness : 0f;

        // --- Colors (from the first part that is drawn) ---
        Renderer rend = go.GetComponentInChildren<Renderer>();
        Material mat = rend != null ? rend.sharedMaterial : null;

        p.renderMaterial = mat != null ? mat.name : "";
        p.materialColor = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
        p.color = ReadShownColor(rend, p.materialColor);

        // --- Shape, size, volume and edge ---
        float volume = ReadShape(go, p, edgeSetting);

        // --- Sharpness: comes from the edge angle and the hardness, never set by hand ---
        p.sharpness = SharpnessMath.Sharpness(p.hardness, p.edgeAngle);
        p.edgeStrength = SharpnessMath.EdgeStrength(p.toughness, p.thickness);

        // --- Weight ---
        // Material set: weight = how heavy the material is x how big the object is.
        // No material: use the Rigidbody's Mass. Neither: 1 per 1 x 1 x 1 block.
        Rigidbody rb = go.GetComponent<Rigidbody>();
        float weight = material != null ? material.WeightPerUnit * volume
                     : rb != null ? rb.mass
                     : volume;
        p.weight = Mathf.Round(weight * 100f) / 100f;   // keep 2 decimals, so tiny differences still stack

        return p;
    }

    // The color the player actually sees: ShapeColor (and similar scripts) override the material's
    // color with a MaterialPropertyBlock, so check that first.
    static Color ReadShownColor(Renderer rend, Color materialColor)
    {
        if (rend == null || !rend.HasPropertyBlock()) return materialColor;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);

        if (block.HasColor("_Color")) return block.GetColor("_Color");           // Built-in Render Pipeline
        if (block.HasColor("_BaseColor")) return block.GetColor("_BaseColor");   // URP
        return materialColor;
    }

    // Fills in shape, size and edge, and returns the volume (adds up every part).
    // Uses each mesh's own box (not the world box), so turning the object doesn't change the result.
    // It measures the MESH, never the collider, so the real thickness is kept even if the collider was made thicker.
    static float ReadShape(GameObject go, ItemProperties p, EdgeShape edgeSetting)
    {
        MeshFilter[] parts = go.GetComponentsInChildren<MeshFilter>();
        float volume = 0f;
        Bounds total = new Bounds();
        bool first = true;
        int count = 0;

        p.edgeShape = EdgeShape.Round;   // until a part with a sharper edge is found
        p.edgeAngle = 90f;

        foreach (MeshFilter mf in parts)
        {
            if (mf.sharedMesh == null) continue;
            count++;

            Bounds b = mf.sharedMesh.bounds;   // the box around the mesh, before scaling
            Vector3 s = Vector3.Scale(b.size, mf.transform.lossyScale);   // lossyScale = scale including parents
            volume += Mathf.Abs(s.x * s.y * s.z);

            // The edge of this part. Keep the sharpest part (smallest angle), e.g. a knife's blade, not its handle.
            SharpnessMath.SortSides(s, out float t, out float w, out float L);
            EdgeShape partShape = SharpnessMath.Resolve(edgeSetting, mf.sharedMesh.name);
            float angle = SharpnessMath.EdgeAngle(partShape, t, w);

            if (count == 1 || angle < p.edgeAngle)
            {
                p.edgeShape = partShape;
                p.edgeAngle = angle;
                p.thickness = t;
                p.edgeWidth = w;
                p.edgeLength = L;
            }

            // Add the box's 8 corners to one big box, measured in the object's own space.
            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                    (c & 1) == 0 ? -1f : 1f,     // the 3 bits of c pick - or + on each axis
                    (c & 2) == 0 ? -1f : 1f,
                    (c & 4) == 0 ? -1f : 1f));
                Vector3 point = go.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));

                if (first) { total = new Bounds(point, Vector3.zero); first = false; }
                else total.Encapsulate(point);
            }

            if (count == 1) p.shape = mf.sharedMesh.name;
        }

        if (count == 0) p.shape = "None";
        else if (count > 1) p.shape = "Multi (" + count + " parts)";

        p.size = Vector3.Scale(total.size, go.transform.lossyScale);   // back to world units
        return volume;
    }
}
