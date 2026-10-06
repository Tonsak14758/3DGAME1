/*
Date: 3/10/2026
Author: Tony
Use by: Put this on a bar under HUDCanvas (e.g. HealthBar, HungerBar).
        Drag the bar's Fill child (an Image with Image Type = Filled, Fill Method = Horizontal) into "Fill".
        Set "Pixel Width" to the width of the fill sprite in pixels (0 = smooth, no pixel steps).
Purpose: Shows a value as a bar. Any script (PlayerHealth, later hunger) calls SetValue(current, max).
         The same script works for every bar, so it doesn't know what the value means.
*/

using UnityEngine;
using UnityEngine.UI;   // Image

public class StatBar : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The Image that grows/shrinks. Image Type must be Filled.")]
    [SerializeField] Image fill;

    [Tooltip("Width of the fill sprite in pixels. The bar moves one pixel at a time so pixel art stays sharp. 0 = smooth.")]
    [Min(0)]
    [SerializeField] int pixelWidth = 96;

    [Tooltip("Bar color. Use a white fill sprite so this color shows as it is.")]
    [SerializeField] Color color = Color.red;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (fill != null) fill.color = color;   // tint the white sprite
    }

    // ===== My methods =====

    // Show current / max on the bar (0 = empty, max = full).
    public void SetValue(float current, float max)
    {
        if (fill == null) return;   // nothing to draw on

        // 0 - 1 how full the bar is. Guard against max = 0 (dividing by 0).
        float amount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Snap to whole pixels: e.g. 96 px wide = 96 steps.
        if (pixelWidth > 0)
        {
            amount = Mathf.Round(amount * pixelWidth) / pixelWidth;
        }

        fill.fillAmount = amount;
    }
}
