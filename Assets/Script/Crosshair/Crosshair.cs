/*
First Date: 30/9/2026
Date: 4/10/2026
Author: Tony
Use by: Drag this script onto the Crosshair Image in the HUD Canvas.
        Set the Player sphere's Layer to "Ignore Raycast" so the crosshair looks past the ball.
Purpose: Shows the crosshair in the center of the screen while the mouse turns the camera.
         Changes color when it points at an item (ItemPickup on it or a parent) or an object with the Target Tag (e.g. "PickUp"),
         but only within the Grab Range of the player's ItemGrabber.
         Turns the Auto Color (green) for "AutoPickUp" objects within Auto Range.
         Other scripts can read Crosshair.Target to know what the player is aiming at,
         and Crosshair.HitPoint for the exact point on it.
*/

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class Crosshair : MonoBehaviour
{
    // ===== Fields =====
    [Header("Colors")]
    [Tooltip("Color when the crosshair is not pointing at a target.")]
    [SerializeField] Color normalColor = Color.white;

    [Tooltip("Color when the crosshair points at an object with the Target Tag.")]
    [SerializeField] Color targetColor = Color.yellow;

    [Tooltip("Color when the crosshair points at an object with the Auto Tag (collected by touch).")]
    [SerializeField] Color autoColor = Color.green;

    [Header("Aiming")]
    [Tooltip("Objects with this tag make the crosshair change color. The tag must exist in the project.")]
    [SerializeField] string targetTag = "PickUp";

    [Tooltip("Objects with this tag make the crosshair turn the Auto Color. The tag must exist in the project.")]
    [SerializeField] string autoTag = "AutoPickUp";

    [Range(1f, 20f)]
    [Tooltip("How far from the player an Auto Tag object turns the crosshair the Auto Color (measured to its surface).")]
    [SerializeField] float autoRange = 3f;

    [Range(1f, 100f)]
    [Tooltip("How far from the camera the crosshair can detect objects.")]
    [SerializeField] float maxDist = 30f;

    // What the crosshair is pointing at right now (null = nothing). Any script can read it.
    public static GameObject Target { get; private set; }

    // The exact point on Target the ray hit (only meaningful while Target isn't null).
    public static Vector3 HitPoint { get; private set; }

    [Tooltip("The player's ItemGrabber. The crosshair only changes color within its Grab Range. Leave empty to find it automatically.")]
    [SerializeField] ItemGrabber grabber;

    Image img;
    Camera cam;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false;   // the crosshair must never block clicks on other UI
    }

    void Start()
    {
        cam = Camera.main;

        // Nothing dragged in? Look for the ItemGrabber in the scene (on the player).
        if (grabber == null) grabber = FindFirstObjectByType<ItemGrabber>();
    }

    void Update()
    {
        // Show the crosshair only while the mouse turns the camera (cursor hidden).
        bool aiming = Cursor.lockState == CursorLockMode.Locked;
        img.enabled = aiming;   // hides the picture, but this script keeps running

        Target = aiming ? FindTarget() : null;
        img.color = PickColor(Target);
    }

    void OnDestroy()
    {
        Target = null;   // static values outlive the scene, so clear it
    }

    // ===== My methods =====

    // Shoots an invisible ray from the center of the screen and returns the first object it hits.
    GameObject FindTarget()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // (0.5, 0.5) = screen center

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDist,
                             Physics.DefaultRaycastLayers,          // skips the "Ignore Raycast" layer
                             QueryTriggerInteraction.Collide))      // also hit triggers, like pick-ups
        {
            return null;
        }

        HitPoint = hit.point;   // e.g. ObjectLevitator holds the object by this point
        return hit.collider.gameObject;
    }

    // Chooses the crosshair color for what it is pointing at.
    Color PickColor(GameObject go)
    {
        if (IsAutoPickUp(go)) return autoColor;      // checked first, so auto items are always green
        if (IsHighlighted(go)) return targetColor;
        return normalColor;
    }

    // True if the object (or a parent) has the Auto Tag and is within Auto Range of the player.
    bool IsAutoPickUp(GameObject go)
    {
        if (go == null) return false;

        bool tagged = false;
        for (Transform t = go.transform; t != null; t = t.parent)   // the ray may hit a child part
        {
            if (t.CompareTag(autoTag)) { tagged = true; break; }
        }
        if (!tagged) return false;

        // Measure from the player (the object with ItemGrabber), or from the camera if there is none.
        Vector3 from = grabber != null ? grabber.transform.position : cam.transform.position;

        // Measure to the nearest point on the object, so big objects aren't harder to reach.
        Collider col = go.GetComponent<Collider>();
        Vector3 nearest = col != null ? col.ClosestPoint(from) : go.transform.position;
        return Vector3.Distance(from, nearest) <= autoRange;
    }

    // True if the crosshair should change color for this object.
    bool IsHighlighted(GameObject go)
    {
        if (go == null) return false;
        if (grabber != null && !grabber.CanGrab(go)) return false;   // F wouldn't work (too far / no machine), so don't highlight

        // The ray may hit a child part (e.g. the sword's blade), so also look up through the parents.
        // This is the same check ItemGrabber uses, so if the crosshair turns color, F can grab it.
        if (go.GetComponentInParent<ItemPickup>() != null) return true;

        return go.CompareTag(targetTag);   // still works for simple tagged objects
    }
}
