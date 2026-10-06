/*
First Date: 4/10/2026
Date: 6/10/2026
Author: Tony
Use by: Drag this script onto the machine object (for now a placeholder cube).
        Set "Area Range", and tick "Show Area" (sphere) / "Show Circle" (line) to see the area in the game
        (all can be changed while playing).
        The sphere turns "Inside Color" (green) while the player is inside the area.
        Place as many machines as you like; being inside any one of them is enough.
        The area is centered on the MIDDLE of the machine's shapes, wherever its root object (pivot) is.
        Recommended: in the toolbar (top left of the Scene view), click "Pivot" to switch it to "Center".
        Unity's Move / Rotate tools then appear in the middle of the selected shapes, the same point the area uses.
        This is only a view setting and changes nothing in the game.
Purpose: The ball has no hands, so the machine does the work: the ball can only grab (F) and levitate (right click)
         objects that are inside the area of an InteractionMachine. A levitated object is dropped when it leaves the area.
         The GameRule "WorldInteraction" turns this off (then any object can be grabbed / levitated).
         Other scripts ask: InteractionMachine.CanInteractWith(the object's collider).
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionMachine : MonoBehaviour
{
    // ===== Fields =====
    [Header("Area")]
    [Range(0.5f, 50f)]
    [Tooltip("How far the machine reaches from its center, in world units.")]
    [SerializeField] float areaRange = 5f;

    [Header("Visual")]
    [Tooltip("Show the see-through sphere of the area in the game.")]
    [SerializeField] bool showArea = true;

    [Tooltip("Show the circle line around the area in the game.")]
    [SerializeField] bool showCircle = true;

    [Tooltip("Color of the see-through sphere. Keep Alpha (A) low so you can see through it.")]
    [SerializeField] Color areaColor = new Color(0.2f, 0.8f, 1f, 0.12f);

    [Tooltip("Color of the see-through sphere while the player is inside the area.")]
    [SerializeField] Color insideColor = new Color(0.2f, 1f, 0.3f, 0.2f);

    [Tooltip("The player, for Inside Color. Leave empty to find it automatically (the object with ItemGrabber).")]
    [SerializeField] Transform player;

    [Tooltip("Color of the circle line around the area.")]
    [SerializeField] Color lineColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Range(0.01f, 0.5f)]
    [Tooltip("How thick the circle line is.")]
    [SerializeField] float lineWidth = 0.05f;

    const int CIRCLE_SEGMENTS = 64;   // straight pieces in the circle (more = rounder)
    const int IGNORE_RAYCAST_LAYER = 2;

    // Every machine switched on in the scene. Each machine adds / removes itself,
    // so no script has to search the scene (FindObjectsOfType is slow).
    static readonly List<InteractionMachine> machines = new List<InteractionMachine>();

    Transform sphere;      // the see-through sphere
    LineRenderer circle;   // the circle line
    Material sphereMat;    // made by this script, so this script must destroy them too
    Material lineMat;
    bool playerInside;     // last value, so the color is only changed when it really changes
    Vector3 centerOffset;  // from the machine's position (pivot) to the middle of its shapes, "un-turned"

    public float AreaRange => areaRange;

    // The middle of the area = the middle of the machine's own shapes, NOT its pivot.
    // (A machine built from shapes can have its root object off to one side of the shapes.)
    // The offset turns with the machine, so it stays right if the machine is rotated.
    public Vector3 Center => transform.position + transform.rotation * centerOffset;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        MeasureCenter();   // first: before the area's own sphere and circle exist (they would change the measurement)
        BuildVisual();
        UpdateVisual();
    }

    void Start()
    {
        // Nothing dragged in? Look for the player (the object with ItemGrabber).
        if (player == null)
        {
            ItemGrabber grabber = FindFirstObjectByType<ItemGrabber>();
            if (grabber != null) player = grabber.transform;
        }
    }

    void Update()
    {
        // The machine moved or turned (hasChanged is set by Unity): move the circle with it.
        // The sphere is a child, so it follows by itself; the circle's points are world positions.
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            UpdateVisual();
        }

        if (!showArea || player == null) return;

        // sqrMagnitude = distance squared: faster than Vector3.Distance and gives the same answer here.
        bool inside = (player.position - Center).sqrMagnitude <= areaRange * areaRange;
        if (inside == playerInside) return;   // no change: nothing to do

        playerInside = inside;
        ApplySphereColor();
    }

    void OnEnable()
    {
        machines.Add(this);
    }

    void OnDisable()
    {
        machines.Remove(this);   // switched off or destroyed: its area stops working
    }

    void OnDestroy()
    {
        // Materials made with "new Material" are not cleaned up by Unity on their own.
        if (sphereMat != null) Destroy(sphereMat);
        if (lineMat != null) Destroy(lineMat);
    }

    // Unity calls this when a value is changed in the Inspector, so the visual updates while playing.
    void OnValidate()
    {
        if (sphere != null) UpdateVisual();   // only once the visual is built (in Play mode)
    }

    // Editor only: draws the area in the Scene view, even when not playing.
    void OnDrawGizmos()
    {
        // Not playing: Awake hasn't measured the center yet, so measure it here (no area visuals exist then).
        Vector3 center = Application.isPlaying ? Center : BoundsUtil.Of(gameObject).center;

        Gizmos.color = lineColor;
        Gizmos.DrawWireSphere(center, areaRange);
    }

    // ===== My methods =====

    // True if the ball may grab / levitate this object: the rule is on, or the object is inside a machine's area.
    public static bool CanInteractWith(Collider col)
    {
        if (col == null) return GameRules.WorldInteraction;
        return CanInteractWith(col.bounds);
    }

    // Same, for a box (e.g. a whole blueprint made of many parts).
    public static bool CanInteractWith(Bounds box)
    {
        if (GameRules.WorldInteraction) return true;   // rule on: no machine needed

        for (int i = 0; i < machines.Count; i++)
        {
            if (machines[i].Contains(box)) return true;
        }
        return false;
    }

    // Same as CanInteractWith, but for one point (e.g. where a held tool floats).
    public static bool CanInteractAt(Vector3 point)
    {
        if (GameRules.WorldInteraction) return true;

        for (int i = 0; i < machines.Count; i++)
        {
            Vector3 center = machines[i].Center;
            float r = machines[i].areaRange;
            if ((point - center).sqrMagnitude <= r * r) return true;
        }
        return false;
    }

    // True if any part of the object's box is inside this machine's area
    // (so big objects only need to reach in, not fit in completely).
    public bool Contains(Bounds box)
    {
        Vector3 center = Center;
        Vector3 nearest = box.ClosestPoint(center);   // the object's point closest to the machine

        // sqrMagnitude = distance squared: faster than Vector3.Distance and gives the same answer here.
        return (nearest - center).sqrMagnitude <= areaRange * areaRange;
    }

    // Finds the middle of all the machine's shapes (renderers) and remembers it compared to the pivot.
    // Runs once in Awake: also for a machine just finished from a blueprint (Instantiate runs Awake).
    void MeasureCenter()
    {
        Bounds shapes = BoundsUtil.Of(gameObject);
        centerOffset = Quaternion.Inverse(transform.rotation) * (shapes.center - transform.position);
    }

    void BuildVisual()
    {
        // The sphere: a Unity sphere without its collider, so it never blocks the player, items or the crosshair.
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "AreaSphere";
        s.layer = IGNORE_RAYCAST_LAYER;
        Destroy(s.GetComponent<Collider>());
        s.transform.SetParent(transform, false);

        MeshRenderer rend = s.GetComponent<MeshRenderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        sphereMat = new Material(Shader.Find("Sprites/Default"));   // simple shader that supports see-through colors
        rend.sharedMaterial = sphereMat;
        sphere = s.transform;

        // The circle: a child object, because one GameObject can't have two renderers.
        GameObject c = new GameObject("AreaCircle");
        c.transform.SetParent(transform, false);

        circle = c.AddComponent<LineRenderer>();
        circle.loop = true;              // join the last point back to the first
        circle.useWorldSpace = true;     // points are in world units, so the machine's scale doesn't matter
        circle.positionCount = CIRCLE_SEGMENTS;
        circle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        circle.receiveShadows = false;
        lineMat = new Material(Shader.Find("Sprites/Default"));
        circle.material = lineMat;
    }

    // Shows / hides the visual and fits it to Area Range. Called on start, when the Inspector changes,
    // and when the machine moves (not every frame).
    void UpdateVisual()
    {
        UpdateSphere();
        UpdateCircle();
    }

    void UpdateSphere()
    {
        sphere.gameObject.SetActive(showArea);
        if (!showArea) return;

        sphere.position = Center;   // on the middle of the shapes, not on the pivot

        // The sphere mesh is 1 wide, so scale it to 2 x range.
        // Divide by the machine's own scale, so a scaled cube doesn't make the sphere the wrong size.
        Vector3 s = transform.lossyScale;
        sphere.localScale = new Vector3(
            SafeDivide(areaRange * 2f, s.x),
            SafeDivide(areaRange * 2f, s.y),
            SafeDivide(areaRange * 2f, s.z));
        ApplySphereColor();
    }

    void UpdateCircle()
    {
        circle.enabled = showCircle;
        if (!showCircle) return;

        circle.startColor = lineColor;
        circle.endColor = lineColor;
        circle.startWidth = lineWidth;
        circle.endWidth = lineWidth;

        Vector3 center = Center;
        for (int i = 0; i < CIRCLE_SEGMENTS; i++)
        {
            float angle = (float)i / CIRCLE_SEGMENTS * Mathf.PI * 2f;   // 0 to a full turn, in radians
            circle.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * areaRange);
        }
    }

    // Green while the player is inside, the normal color otherwise.
    void ApplySphereColor()
    {
        sphereMat.color = playerInside ? insideColor : areaColor;
    }

    static float SafeDivide(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? a : a / b;   // never divide by 0
    }
}
