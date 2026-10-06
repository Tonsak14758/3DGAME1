/*
Date: 2/10/2026
Author: Tony
Use by: Nothing to set up. Other scripts call SharpnessMath.XXX(...).
        To balance the game, change the numbers in the "Tuning" section.
Purpose: Every formula about edges, sharpness, chipping and hit damage, kept in one place.
         Sharpness is not set by hand: it comes from how thin the shape is (edge angle) and how hard the material is.
           Edge angle   Box:   θ = 2 · atan(t / w)        Wedge: θ = 2 · atan(t / (2·w))
                        (t = thinnest side, w = middle side = from the edge to the back)
           Sharpness    S = H · (1 − θ / 90°)              0 = blunt, 10 = razor
           Edge strength = K · t / ReferenceThickness      thin + brittle = weak edge
           Chip chance  = ChipScale · H / edge strength    each chip makes θ bigger (blunter)
           Cut damage   = S · weight · speed · CutFactor   (the edge hit)
           Blunt damage = weight · speed · BluntFactor     (the flat side hit, or a blunt item)
*/

using UnityEngine;

// What kind of edge a shape has. Auto = decide from the mesh name.
public enum EdgeShape
{
    Auto,    // Cube, Cylinder = Box. Sphere, Capsule = Round. A mesh with "Wedge" in its name = Wedge.
    Box,     // square sides: thin plates have a sharp edge, cubes don't
    Wedge,   // tapers to an edge: sharper than a box of the same size
    Round    // no edge at all (sphere, capsule)
}

public static class SharpnessMath
{
    // ===== Tuning =====
    public const float MinColliderThickness = 0.01f;   // colliders thinner than this get missed by physics (only the collider grows, not the stats)
    public const float ReferenceThickness = 0.005f;    // a "normal" blade thickness (5 mm): edge strength = toughness at this thickness
    public const float ChipScale = 0.01f;              // bigger = everything chips more often
    public const float ChipAngle = 5f;                 // degrees added to the edge angle by each chip
    public const float CutFactor = 1f;                 // multiplies all cut damage
    public const float BluntFactor = 1f;               // multiplies all blunt damage
    public const float EdgeHitTolerance = 0.5f;        // 0.5 = the blade must move within 30° of edge-first to count as a cut

    // ===== My methods =====

    // Sorts a size into thinnest (t), middle (w) and longest (L).
    // For a blade: t = thickness, w = width (edge to back), L = length of the edge.
    public static void SortSides(Vector3 size, out float t, out float w, out float L)
    {
        float a = Mathf.Abs(size.x), b = Mathf.Abs(size.y), c = Mathf.Abs(size.z);

        t = Mathf.Min(a, Mathf.Min(b, c));
        L = Mathf.Max(a, Mathf.Max(b, c));
        w = a + b + c - t - L;   // the one that is neither smallest nor largest
    }

    // Picks the edge shape from a mesh's name when the setting is Auto.
    public static EdgeShape Resolve(EdgeShape setting, string meshName)
    {
        if (setting != EdgeShape.Auto) return setting;
        if (string.IsNullOrEmpty(meshName)) return EdgeShape.Box;

        string n = meshName.ToLower();
        if (n.Contains("wedge")) return EdgeShape.Wedge;
        if (n.Contains("sphere") || n.Contains("capsule")) return EdgeShape.Round;
        return EdgeShape.Box;
    }

    // Edge angle in degrees: 0 = infinitely thin, 90 = no edge (a cube).
    public static float EdgeAngle(EdgeShape shape, float t, float w)
    {
        if (shape == EdgeShape.Round) return 90f;   // round things have no edge
        if (t <= 0f || w <= 0f) return 90f;         // flat surfaces (Plane, Quad) have no thickness, so no real edge

        float ratio = shape == EdgeShape.Wedge ? t / (2f * w) : t / w;
        return 2f * Mathf.Atan(ratio) * Mathf.Rad2Deg;   // Atan gives radians, Rad2Deg turns them into degrees
    }

    // 0 - 10. Thin angle and hard material = sharp.
    public static float Sharpness(float hardness, float edgeAngle)
    {
        return hardness * Mathf.Clamp01(1f - edgeAngle / 90f);
    }

    // How well the edge survives. A tough material or a thick edge = strong.
    public static float EdgeStrength(float toughness, float thickness)
    {
        return toughness * thickness / ReferenceThickness;
    }

    // 0 - 1: the chance that one cutting hit chips the edge. Hard and weak edges chip most.
    public static float ChipChance(float hardness, float edgeStrength)
    {
        if (edgeStrength <= 0f) return 1f;   // no strength at all = always chips
        return Mathf.Clamp01(ChipScale * hardness / edgeStrength);
    }

    // The new, blunter edge angle after one chip.
    public static float Chip(float edgeAngle)
    {
        return Mathf.Min(90f, edgeAngle + ChipAngle);
    }

    // True if the hit came at the edge (a cut), false if it hit the flat side.
    // hitDirection = the way the blade was moving. thinAxis = the blade's thickness direction (world space).
    public static bool IsEdgeHit(Vector3 hitDirection, Vector3 thinAxis)
    {
        // Dot = 0 when the blade moves sideways to its thin axis (edge first), 1 when it moves flat side first.
        float dot = Mathf.Abs(Vector3.Dot(hitDirection.normalized, thinAxis.normalized));
        return dot < EdgeHitTolerance;
    }

    public static float CutDamage(float sharpness, float weight, float speed)
    {
        return sharpness * weight * speed * CutFactor;
    }

    public static float BluntDamage(float weight, float speed)
    {
        return weight * speed * BluntFactor;
    }
}
