/*
Date: 4/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (next to ObjectLevitator).
        Sparkles: make a Particle System prefab (GameObject > Effects > Particle System, then drag it into Assets)
                  and drag it into "Sparkles Prefab". Suggested prefab settings:
                  Start Speed 0.3, Start Size 0.05, Start Lifetime 1.5, Click Emission = Rate over Time 25, Gravity Modifier -0.1 (floats up),
                  Simulation Space = World (sparkles stay behind as the object moves), Shape = Box,
                  Color over Lifetime = fade to transparent. Leave it empty for no sparkles.
        Beam: made by this script, set its look below.
Purpose: The look of levitation. Listens to ObjectLevitator.Started / Stopped:
         rising sparkles around the floating object, and a wavy beam from the player to it.
         To change the look later, edit or replace this script; ObjectLevitator doesn't need to change.
*/

using UnityEngine;

[RequireComponent(typeof(ObjectLevitator))]
public class LevitationEffect : MonoBehaviour
{
    // ===== Fields =====
    [Header("Sparkles")]
    [Tooltip("Particle System prefab played around the floating object. Empty = no sparkles.")]
    [SerializeField] ParticleSystem sparklesPrefab;

    [Header("Beam")]
    [Tooltip("Show a beam from the player to the floating object.")]
    [SerializeField] bool showBeam = true;

    [Tooltip("Beam color: start (at the player) to end (at the object).")]
    [SerializeField] Gradient beamColor = DefaultBeamColor();

    [Range(0.005f, 0.3f)]
    [Tooltip("How thick the beam is.")]
    [SerializeField] float beamWidth = 0.04f;

    [Range(2, 64)]
    [Tooltip("How many points the beam is made of. More = smoother wave.")]
    [SerializeField] int beamPoints = 24;

    [Range(0f, 0.5f)]
    [Tooltip("How far the beam waves to the side. 0 = straight line.")]
    [SerializeField] float waveHeight = 0.08f;

    [Range(0f, 5f)]
    [Tooltip("How many waves fit along the beam.")]
    [SerializeField] float waveCount = 2f;

    [Range(0f, 20f)]
    [Tooltip("How fast the waves travel along the beam.")]
    [SerializeField] float waveSpeed = 6f;

    ParticleSystem sparkles;   // our copy of the prefab (null = no sparkles)
    LineRenderer beam;         // made in Awake
    Material beamMaterial;     // made by us, so we must destroy it too
    Rigidbody target;          // the floating object (null = not levitating)
    Vector3 centerOffset;      // the middle of its meshes, measured from its position and turned with it
    ObjectLevitator levitator; // asked for the grab point each frame

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        levitator = GetComponent<ObjectLevitator>();

        if (sparklesPrefab != null)
        {
            sparkles = Instantiate(sparklesPrefab);    // no parent: it moves to the object by itself
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (showBeam) beam = CreateBeam();
    }

    void OnEnable()
    {
        ObjectLevitator.Started += OnStarted;
        ObjectLevitator.Stopped += OnStopped;
    }

    void OnDisable()
    {
        ObjectLevitator.Started -= OnStarted;
        ObjectLevitator.Stopped -= OnStopped;
        OnStopped();   // don't leave the effect on
    }

    void OnDestroy()
    {
        if (sparkles != null) Destroy(sparkles.gameObject);
        if (beamMaterial != null) Destroy(beamMaterial);
    }

    // LateUpdate runs after everything has moved this frame, so the effect never lags behind.
    void LateUpdate()
    {
        if (target == null) return;

        if (sparkles != null) sparkles.transform.position = MeshCenter();
        if (beam != null) DrawBeam(transform.position, levitator.GrabPoint);   // ends on the spot being held
    }

    // ===== My methods =====

    void OnStarted(Rigidbody rb)
    {
        target = rb;

        // The real middle of the meshes, not the pivot (for objects made of several shapes,
        // the pivot can be off in the air). Stored "un-turned" so MeshCenter can follow the object's turning.
        Bounds bounds = BoundsUtil.Of(rb.gameObject);
        centerOffset = Quaternion.Inverse(rb.rotation) * (bounds.center - rb.position);

        if (sparkles != null)
        {
            // Make the sparkle box the size of the object, so big and small things both look right.
            ParticleSystem.ShapeModule shape = sparkles.shape;
            shape.scale = bounds.size;

            sparkles.transform.position = MeshCenter();
            sparkles.Play(true);
        }

        if (beam != null) beam.enabled = true;
    }

    void OnStopped()
    {
        target = null;

        // StopEmitting = no new sparkles, but the ones already out fade away nicely.
        if (sparkles != null) sparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (beam != null) beam.enabled = false;
    }

    // Moves every beam point: a straight line from a to b, pushed sideways by a moving wave.
    void DrawBeam(Vector3 a, Vector3 b)
    {
        beam.positionCount = beamPoints;

        Vector3 dir = b - a;
        // A direction at right angles to the beam, for the wave to push along.
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.0001f) side = Vector3.right;   // beam straight up/down: any side works
        side.Normalize();

        for (int i = 0; i < beamPoints; i++)
        {
            float t = i / (float)(beamPoints - 1);   // 0 at the player, 1 at the object

            // The wave: "- Time.time" makes it travel from the player toward the object.
            float wave = Mathf.Sin(t * waveCount * 2f * Mathf.PI - Time.time * waveSpeed);

            // sin(t * PI) is 0 at both ends, so the beam stays attached to the player and the object.
            float offset = wave * waveHeight * Mathf.Sin(t * Mathf.PI);

            beam.SetPosition(i, a + dir * t + side * offset);
        }
    }

    LineRenderer CreateBeam()
    {
        GameObject go = new GameObject("LevitationBeam");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;   // points are world positions, not relative to the player
        lr.widthMultiplier = beamWidth;
        lr.colorGradient = beamColor;
        lr.numCapVertices = 4;     // round ends
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // "Sprites/Default" is a simple shader that uses the line's colors (and always exists in builds).
        beamMaterial = new Material(Shader.Find("Sprites/Default"));
        lr.material = beamMaterial;

        lr.enabled = false;        // hidden until levitating
        return lr;
    }

    // The middle of the floating object's meshes right now.
    Vector3 MeshCenter()
    {
        return target.position + target.rotation * centerOffset;
    }

    // Light blue at the player, fading to see-through purple at the object.
    static Gradient DefaultBeamColor()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.4f, 0.8f, 1f), 0f), new GradientColorKey(new Color(0.7f, 0.4f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.4f, 1f) });
        return g;
    }
}
