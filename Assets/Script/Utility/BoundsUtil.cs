/*
Date: 4/10/2026
Author: Tony
Use by: Nothing to set up. Other scripts call BoundsUtil.Of(someObject).
Purpose: The box around everything you can see of an object (all its renderers, children too).
         Its center is the real middle of the meshes, which for objects made of several shapes
         is often NOT the object's position (pivot).
*/

using UnityEngine;

public static class BoundsUtil
{
    // ===== My methods =====

    // A box around all the object's renderers (or a 1 x 1 x 1 box at its position if it has none).
    public static Bounds Of(GameObject go)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            bounds.Encapsulate(rends[i].bounds);
        }
        return bounds;
    }
}
