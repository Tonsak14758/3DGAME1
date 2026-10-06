/*
Date: 29/9/2026
Author: Tony
Use by: Drag this script onto any shape (Cube, Sphere, Plane...).
Purpose: Pick the shape's color in the Inspector. The color shows right away,
         in the Scene view and in Play mode, and each shape can have its own color.
*/

using UnityEngine;

[RequireComponent(typeof(Renderer))]   // the component that draws the shape
public class ShapeColor : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The color of this shape. You can change it during Play mode too.")]
    [SerializeField] Color color = Color.red;

    Renderer rend;                  // draws the shape
    MaterialPropertyBlock block;    // holds color overrides for THIS shape only

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        ApplyColor();   // set the color when the game starts
    }

    // Editor only: runs whenever you change a value in the Inspector,
    // so you see the new color immediately, even without pressing Play.
    void OnValidate()
    {
        ApplyColor();
    }

    // ===== My methods =====

    // Other scripts, or an On Click () list, can call this to change the color.
    public void SetColor(Color newColor)
    {
        color = newColor;
        ApplyColor();
    }

    // Picks a random bright color. Try it from a button's On Click () list.
    public void SetRandomColor()
    {
        SetColor(Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
    }

    void ApplyColor()
    {
        // OnValidate can run before Awake, so get the components here if we don't have them yet.
        if (rend == null) rend = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block);       // read this shape's current overrides
        block.SetColor("_BaseColor", color); // color name used by URP (Universal Render Pipeline)
        block.SetColor("_Color", color);     // color name used by the Built-in Render Pipeline
        rend.SetPropertyBlock(block);       // give the overrides back to the shape
    }
}