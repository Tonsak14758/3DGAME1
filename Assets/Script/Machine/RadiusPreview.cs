/*
Date: 2/10/2026
Author: Tony
Use by: Drag this script onto any object (e.g. an empty GameObject or a placeholder cube for the machine).
        Set Radius, press Play, and look at it in the Game view. All the settings can be changed while playing.
        Optional: drag the PlayerSphere into "Target" to see the sphere change color while the player is inside.
Purpose: Shows a radius in the game so you can see how big it feels before building the levitation machine:
         a circle line around the object, and a see-through sphere for the area where the machine is active.
         In the Scene view a wire sphere is drawn too, even without pressing Play.
         Other scripts can use Radius and Contains(point) later, e.g. to check if a tool is in range.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class RadiusPreview : MonoBehaviour
{
    // ===== Fields =====
    [Header("Radius")]
    [Range(0.5f, 50f)]
    [Tooltip("How far the area reaches from this object's center, in world units.")]
    [SerializeField] float radius = 5f;

    [Header("Circle")]
    [Tooltip("Show the circle line around the object.")]
    [SerializeField] bool showCircle = true;

    [Tooltip("Color of the circle line.")]
    [SerializeField] Color circleColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Range(0.01f, 0.5f)]
    [Tooltip("How thick the circle line is.")]
    [SerializeField] float lineWidth = 0.05f;

    [Range(16, 256)]
    [Tooltip("How many straight pieces make the circle. More = rounder.")]
    [SerializeField] int segments = 64;

    [Tooltip("Height of the circle compared to the object's center. Use a negative number to put it on the ground.")]
    [SerializeField] float circleHeight = 0f;

    [Header("Sphere (the active area)")]
    [Tooltip("Show the see-through sphere.")]
    [SerializeField] bool showSphere = true;

    [Tooltip("Color of the sphere. Keep Alpha (A) low so you can see through it.")]
    [SerializeField] Color sphereColor = new Color(0.2f, 0.8f, 1f, 0.15f);

    [Tooltip("Color of the sphere while the Target is inside the radius.")]
    [SerializeField] Color insideColor = new Color(0.2f, 1f, 0.3f, 0.25f);

    [Tooltip("Optional: the sphere turns Inside Color while this object is in the radius (e.g. the PlayerSphere).")]
    [SerializeField] Transform target;

    public float Radius => radius;

    LineRenderer line;     // draws the circle
    Transform sphere;      // the see-through sphere
    Material lineMat;      // made by this script, so this script must destroy them too
    Material sphereMat;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        BuildCircle();
        BuildSphere();
    }

    // LateUpdate runs after everything has moved this frame, so the circle never lags behind.
    void LateUpdate()
    {
        UpdateCircle();
        UpdateSphere();
    }

    void OnDestroy()
    {
        // Materials made with "new Material" are not cleaned up by Unity on their own.
        if (lineMat != null) Destroy(lineMat);
        if (sphereMat != null) Destroy(sphereMat);
    }

    // Editor only: draws in the Scene view, even when not playing.
    void OnDrawGizmos()
    {
        Gizmos.color = circleColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // ===== My methods =====

    // True if a point (e.g. a tool or an item) is inside the radius.
    public bool Contains(Vector3 point)
    {
        // sqrMagnitude = distance squared: faster than Vector3.Distance and gives the same answer here.
        return (point - transform.position).sqrMagnitude <= radius * radius;
    }

    void BuildCircle()
    {
        // A child object, because one GameObject can't have two renderers (e.g. a MeshRenderer and a LineRenderer).
        GameObject go = new GameObject("RadiusCircle");
        go.transform.SetParent(transform, false);

        line = go.AddComponent<LineRenderer>();
        line.loop = true;              // join the last point back to the first
        line.useWorldSpace = true;     // points are in world units, so the object's scale doesn't change the radius
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // "Sprites/Default" is a simple shader that is always in the game build and supports see-through colors.
        lineMat = new Material(Shader.Find("Sprites/Default"));
        line.material = lineMat;
    }

    void BuildSphere()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "RadiusSphere";
        go.layer = 2;   // 2 = "Ignore Raycast": the crosshair looks through it
        Destroy(go.GetComponent<Collider>());   // only for looking at: it must not block the player or items
        go.transform.SetParent(transform, false);

        MeshRenderer rend = go.GetComponent<MeshRenderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        sphereMat = new Material(Shader.Find("Sprites/Default"));
        rend.sharedMaterial = sphereMat;

        sphere = go.transform;
    }

    void UpdateCircle()
    {
        line.enabled = showCircle;
        if (!showCircle) return;

        line.startColor = circleColor;
        line.endColor = circleColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = segments;

        Vector3 center = transform.position + Vector3.up * circleHeight;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;   // 0 to a full turn, in radians
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            line.SetPosition(i, center + offset);
        }
    }

    void UpdateSphere()
    {
        sphere.gameObject.SetActive(showSphere);
        if (!showSphere) return;

        // The sphere mesh is 1 wide, so scale it to 2 x radius.
        // Divide by the parent's scale, so a scaled parent doesn't make the sphere the wrong size.
        Vector3 parentScale = transform.lossyScale;
        sphere.localScale = new Vector3(
            SafeDivide(radius * 2f, parentScale.x),
            SafeDivide(radius * 2f, parentScale.y),
            SafeDivide(radius * 2f, parentScale.z));

        bool targetInside = target != null && Contains(target.position);
        sphereMat.color = targetInside ? insideColor : sphereColor;
    }

    static float SafeDivide(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? a : a / b;   // never divide by 0
    }
}
