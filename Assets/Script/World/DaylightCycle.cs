/*
Date: 4/10/2026
Author: Tony
Use by: Drag this script onto the Directional Light (the sun) in the scene.
        For a sky that turns orange at sunset and dark at night, use the default Procedural Skybox
        (Window > Rendering > Lighting > Environment > Skybox Material = Default-Skybox) and set "Sun Source" there to this light.
        Testing: drag "Time Of Day" in the Inspector while playing.
Purpose: Day and night. Turns the sun around the world and changes its brightness and color.
         Time Of Day: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.
         Only moves while the GameRule "DoDaylightCycle" is on (off = time stops, like Minecraft).
*/

using UnityEngine;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class DaylightCycle : MonoBehaviour
{
    // ===== Fields =====
    [Header("Time")]
    [Range(0f, 1f)]
    [Tooltip("Time of day right now: 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.")]
    [SerializeField] float timeOfDay = 0.3f;

    [Range(0.5f, 60f)]
    [Tooltip("How many real minutes one full day (day + night) takes. Minecraft = 20.")]
    [SerializeField] float dayLengthMinutes = 10f;

    [Header("Sun")]
    [Range(0f, 360f)]
    [Tooltip("Which way the sun rises from (turns the sun's path around the world).")]
    [SerializeField] float sunDirection = 170f;

    [Range(0f, 3f)]
    [Tooltip("Sun brightness at noon.")]
    [SerializeField] float maxSunIntensity = 1.2f;

    [Tooltip("Sun color by height: left = on the horizon (sunrise / sunset), right = high in the sky.")]
    [SerializeField] Gradient sunColor = DefaultSunColor();

    [Header("Ambient (light that comes from everywhere)")]
    [Range(0f, 2f)]
    [Tooltip("Ambient light in the day.")]
    [SerializeField] float dayAmbient = 1f;

    [Range(0f, 2f)]
    [Tooltip("Ambient light at night. Not 0, or the night is pitch black.")]
    [SerializeField] float nightAmbient = 0.2f;

    Light sun;

    public float TimeOfDay => timeOfDay;   // other scripts can read it (e.g. a clock, or monsters at night)
    public bool IsNight => SunHeight() < 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        sun = GetComponent<Light>();
        if (RenderSettings.sun == null) RenderSettings.sun = sun;   // the procedural sky follows this light
    }

    void Update()
    {
        if (GameRules.DoDaylightCycle) AdvanceTime();
        ApplyLight();   // always, so dragging Time Of Day works even when time is stopped
    }

    // ===== My methods =====

    void AdvanceTime()
    {
        // One full day = dayLengthMinutes * 60 seconds, so each second adds 1 / that.
        timeOfDay += Time.deltaTime / (dayLengthMinutes * 60f);
        timeOfDay %= 1f;   // after 1 (midnight again) start back at 0
    }

    void ApplyLight()
    {
        // 0.25 (sunrise) = 0 degrees (horizon), 0.5 (noon) = 90 (straight down), 0.75 (sunset) = 180.
        float angle = timeOfDay * 360f - 90f;
        transform.rotation = Quaternion.Euler(angle, sunDirection, 0f);

        float height = SunHeight();   // 1 = noon, 0 = horizon, below 0 = night

        // Bright in the day, fading out quickly as the sun goes below the horizon.
        // "* 4" makes the fade short: full brightness once the sun is a bit above the horizon.
        sun.intensity = maxSunIntensity * Mathf.Clamp01(height * 4f + 0.1f);
        sun.color = sunColor.Evaluate(Mathf.Clamp01(height));
        sun.enabled = height > -0.1f;   // no sun shadows from below the ground at night

        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbient, dayAmbient, Mathf.Clamp01(height * 2f + 0.3f));
    }

    // How high the sun is: 1 = straight up, 0 = horizon, -1 = straight down (midnight).
    float SunHeight()
    {
        // The light shines along its forward, so the sun itself is in the opposite direction.
        return -transform.forward.y;
    }

    // Orange on the horizon, warm white high in the sky.
    static Gradient DefaultSunColor()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.65f), 0.25f),
                new GradientColorKey(new Color(1f, 0.97f, 0.9f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
