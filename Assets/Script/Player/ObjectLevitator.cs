/*
First Date: 4/10/2026
Date: 6/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (PlayerInventory is added automatically).
        Needs the Crosshair script in the scene. The object must be inside an InteractionMachine's area
        (or the GameRule WorldInteraction on); it is dropped when it leaves the area.
        Only objects with a Rigidbody (not kinematic) can be levitated.
Purpose: With an empty hand (no hotbar slot selected, or the selected slot has no item),
         aim at an object and HOLD the Levitate key (right mouse, set in KeyBindings).
         The object floats in front of the camera and follows your aim, held by the spot you aimed at
         (aim at a hammer's grip and it hangs from the grip).
         Let go of the key (or select a hotbar slot with an item) to drop it.
         "Keep Out Of Surfaces" (on by default): aiming at the ground or a wall keeps the object in front of it,
         and while held it uses Continuous collision detection, so a hard flick can't push it through the ground.
         Untick it for the old behavior.
         This script only moves the object. Visuals (e.g. LevitationEffect) listen to
         ObjectLevitator.Started / Stopped, so the look can be changed without touching this script.
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class ObjectLevitator : MonoBehaviour
{
    // ===== Fields =====
    [Header("Reach")]
    [Range(1f, 30f)]
    [Tooltip("How far from the ball an object can be picked up (measured to the object's surface).")]
    [SerializeField] float pickRange = 8f;

    [Range(1f, 10f)]
    [Tooltip("Closest an object can float to the camera.")]
    [SerializeField] float minHoldDistance = 2f;

    [Range(2f, 30f)]
    [Tooltip("Farthest an object can float from the camera.")]
    [SerializeField] float maxHoldDistance = 8f;

    [Range(2f, 50f)]
    [Tooltip("If the object gets stuck this far from where it should float (e.g. behind a wall), it is dropped.")]
    [SerializeField] float breakDistance = 12f;

    [Header("Movement")]
    [Range(1f, 30f)]
    [Tooltip("How fast the object moves to the float point (bigger = snappier).")]
    [SerializeField] float followStrength = 10f;

    [Range(1f, 50f)]
    [Tooltip("Top speed of the levitated object, so it can't fly through things.")]
    [SerializeField] float maxSpeed = 15f;

    [Range(0f, 1f)]
    [Tooltip("How much of its spin the object loses each physics step (1 = stops spinning at once).")]
    [SerializeField] float spinDamping = 0.2f;

    [Header("Ground / wall protection")]
    [Tooltip("Ticked (NEW system, default): the object floats just in front of the ground or a wall you aim at, " +
             "and uses Continuous collision while held, so a hard flick can't push it through the ground.\n" +
             "Unticked (OLD system): the object always floats at its hold distance, even if that point is inside " +
             "the ground, with its own collision setting (fast flicks can pass through the ground). Uses a bit less CPU.")]
    [SerializeField] bool keepOutOfSurfaces = true;

    PlayerInventory inv;
    Camera cam;

    Rigidbody held;          // the object floating now (null = nothing)
    Collider heldCollider;   // the part we aimed at, used to check the object is still inside a machine's area
    bool heldUsedGravity;    // its gravity setting before we picked it, to put back on drop
    CollisionDetectionMode heldDetection;   // its collision setting before we picked it, to put back on drop
    float heldRadius;        // about how far the object reaches from the grabbed spot, to keep it out of walls / ground
    float holdDistance;      // distance from the camera, chosen when it is picked
    bool levitating;         // true from pick to drop, even if the object gets destroyed in between
    Vector3 grabOffset;      // the aimed-at point, measured from the object's position and turned with it

    static readonly RaycastHit[] rayHits = new RaycastHit[16];   // reused, so the ray check makes no garbage

    public static event System.Action<Rigidbody> Started;   // an object starts floating (the object is passed along)
    public static event System.Action Stopped;              // the floating object is dropped

    public Rigidbody Held => held;   // the object floating now (null = nothing)

    // The point the object is held by (the spot you aimed at), in the world.
    // It turns with the object, so it stays on the same spot (e.g. the hammer's grip).
    public Vector3 GrabPoint => held != null ? held.position + held.rotation * grabOffset : Vector3.zero;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        cam = Camera.main;   // the camera the crosshair aims with
    }

    void Update()
    {
        if (levitating)
        {
            // Drop when the key is let go, a hotbar slot is selected, a menu opens,
            // the object was destroyed (e.g. collected while floating), or it left the machine's area.
            if (held == null || !LevitateHeld() || !CanLevitate()
                || !InteractionMachine.CanInteractWith(heldCollider))
            {
                Drop();
            }
            return;
        }

        if (LevitatePressed() && CanLevitate()) TryPick();
    }

    // Physics work goes in FixedUpdate (Unity's physics step), not Update.
    void FixedUpdate()
    {
        if (held == null) return;

        // Move the grabbed spot (not the object's pivot) to the crosshair.
        // For objects made of several shapes, the pivot can be off in the air beside the parts.
        Vector3 toTarget = HoldPoint() - GrabPoint;

        if (toTarget.magnitude > breakDistance)
        {
            Drop();   // stuck behind something
            return;
        }

        // Speed toward the float point: far = fast, close = slow, so it settles without shaking.
        held.velocity = Vector3.ClampMagnitude(toTarget * followStrength, maxSpeed);
        held.angularVelocity *= 1f - spinDamping;
    }

    void OnDisable()
    {
        Drop();   // never leave an object floating with gravity off
    }

    void OnDestroy()
    {
        Started = null;   // static values outlive the scene, so clear them
        Stopped = null;
    }

    // ===== My methods =====

    // True if the hand is empty (no hotbar slot selected, or an empty one) and no menu is using the mouse.
    bool CanLevitate()
    {
        if (MenuState.AnyOpen) return false;
        if (BlueprintPlacer.Busy || BlueprintPanel.ClosedThisFrame) return false;   // that right click was for building
        if (cam == null || Cursor.lockState != CursorLockMode.Locked) return false;   // not aiming

        // Empty hand only: no slot selected (ActiveItem = null), or the selected slot has no item.
        PlayerInventory.Slot active = inv.ActiveItem;
        return active == null || active.IsEmpty;
    }

    void TryPick()
    {
        GameObject target = Crosshair.Target;
        if (target == null) return;

        // The collider hit may be a child; attachedRigidbody finds the Rigidbody it belongs to.
        Collider col = target.GetComponent<Collider>();
        Rigidbody rb = col != null ? col.attachedRigidbody : target.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;          // nothing physics can move
        if (rb.transform.IsChildOf(transform)) return;     // never lift ourselves
        if (!InteractionMachine.CanInteractWith(col)) return;   // not inside any machine's area

        // Measure to the nearest point, so big objects aren't harder to reach.
        Vector3 nearest = col != null ? col.ClosestPoint(transform.position) : rb.position;
        if (Vector3.Distance(transform.position, nearest) > pickRange) return;

        held = rb;
        heldCollider = col;
        levitating = true;
        heldUsedGravity = rb.useGravity;
        rb.useGravity = false;

        // Continuous = physics checks the whole path between two steps, so a fast flick can't skip
        // through the ground or a wall (Discrete, the default, only checks where it is at each step).
        heldDetection = rb.collisionDetectionMode;
        if (keepOutOfSurfaces) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Half the object's size: the float point stays at least this far in front of surfaces.
        heldRadius = BoundsUtil.Of(rb.gameObject).extents.magnitude;

        // Remember where on the object we aimed, in the object's own directions
        // (Inverse(rotation) "un-turns" it, so GrabPoint can turn it back as the object turns).
        Vector3 hit = Crosshair.HitPoint;
        grabOffset = Quaternion.Inverse(rb.rotation) * (hit - rb.position);

        // Keep it at the distance it was picked at (inside the min / max limits).
        holdDistance = Mathf.Clamp(Vector3.Distance(cam.transform.position, hit), minHoldDistance, maxHoldDistance);

        Started?.Invoke(rb);   // tell the visuals (if any are listening)
    }

    void Drop()
    {
        if (!levitating) return;

        if (held != null)   // (if it still exists) put its own settings back: it falls normally again
        {
            held.useGravity = heldUsedGravity;
            held.collisionDetectionMode = heldDetection;
        }
        held = null;
        heldCollider = null;
        levitating = false;

        Stopped?.Invoke();
    }

    // Where the object should float: straight out from the camera (on the crosshair), at holdDistance.
    // If the ground or a wall is closer than that, it floats just in front of that surface instead,
    // so the script never pulls the object into the ground (which pushed it through to underground).
    Vector3 HoldPoint()
    {
        Transform c = cam.transform;
        float dist = holdDistance;

        if (!keepOutOfSurfaces) return c.position + c.forward * dist;   // old system: no surface check

        int count = Physics.RaycastNonAlloc(c.position, c.forward, rayHits, holdDistance,
                                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (rayHits[i].collider.attachedRigidbody == held) continue;   // the held object itself is not a wall

            // Stop the object's size before the surface (but never closer to the camera than 0.3).
            dist = Mathf.Min(dist, Mathf.Max(0.3f, rayHits[i].distance - heldRadius));
        }

        return c.position + c.forward * dist;
    }

    bool LevitatePressed()
    {
        return Input.GetKeyDown(KeyBindings.Levitate);   // the key is chosen in KeyBindings
    }

    bool LevitateHeld()
    {
        return Input.GetKey(KeyBindings.Levitate);
    }
}
