/*
First Date: 4/10/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (PlayerInventory is added automatically).
        Select a hotbar slot with an item: it floats beside the crosshair (inside an InteractionMachine's area,
        or anywhere with the GameRule WorldInteraction on). The item must have been collected this play session
        (its look comes from ItemTemplates).
        To test hits, put HitTarget on a cube and watch the Console.
Purpose: Holding and swinging tools. No key has to be held:
         - Move the mouse: the tool follows the crosshair on a spring, so a fast flick whips it through the air.
           Damage then uses the REAL speed of the hit.
         - Left click (Swing key, set in KeyBindings): a swing animation. Damage uses a fixed speed from the tool's weight.
         Heavy tools follow slower and swing slower, but hit harder (weight is felt, not just read).
         Damage comes from SharpnessMath: an edge-first hit with the sharpest part = CUT, anything else = BLUNT.
         The tool is held by its "handle" (the part farthest from the sharpest part), head up, edge facing forward.
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class ToolWielder : MonoBehaviour
{
    // ===== Fields =====
    [Header("Holding")]
    [Tooltip("Where the tool's handle floats, measured from the camera: x = right, y = up, z = forward.")]
    [SerializeField] Vector3 holdOffset = new Vector3(0.45f, -0.35f, 1.1f);

    [Range(-90f, 90f)]
    [Tooltip("How far the tool leans forward while resting (degrees).")]
    [SerializeField] float restTilt = 20f;

    [Range(1f, 40f)]
    [Tooltip("How fast the tool follows the crosshair. Heavy tools follow slower (this is divided by √weight).")]
    [SerializeField] float followStrength = 15f;

    [Range(1f, 40f)]
    [Tooltip("How fast the tool turns to its holding angle. Heavy tools turn slower.")]
    [SerializeField] float turnStrength = 12f;

    [Range(5f, 60f)]
    [Tooltip("Top speed of the tool, so it can't fly through things.")]
    [SerializeField] float maxSpeed = 30f;

    [Header("Click swing (animation)")]
    [Range(0.1f, 2f)]
    [Tooltip("Seconds one swing takes for a tool of weight 1. Heavier tools take longer (× √weight).")]
    [SerializeField] float swingTime = 0.35f;

    [Range(0f, 120f)]
    [Tooltip("How far the tool pulls back before the strike (degrees).")]
    [SerializeField] float windUpAngle = 50f;

    [Range(0f, 180f)]
    [Tooltip("How far the tool swings forward and down in the strike (degrees).")]
    [SerializeField] float strikeAngle = 110f;

    [Range(0f, 1f)]
    [Tooltip("How far the tool reaches forward in the middle of the strike.")]
    [SerializeField] float strikeReach = 0.4f;

    [Range(1f, 20f)]
    [Tooltip("Speed used for the damage of a click swing, for a tool of weight 1. Heavier tools: ÷ √weight.")]
    [SerializeField] float clickSwingSpeed = 6f;

    [Header("Hits")]
    [Range(0f, 10f)]
    [Tooltip("Hits slower than this (m/s) don't count, so just touching things does no damage.")]
    [SerializeField] float minHitSpeed = 1.5f;

    [Range(0f, 1f)]
    [Tooltip("Seconds between two hits, so one swing that scrapes along doesn't hit 10 times.")]
    [SerializeField] float hitCooldown = 0.15f;

    const int IGNORE_RAYCAST_LAYER = 2;   // the crosshair looks through the held tool
    const float WIND_UP_PART = 0.35f;     // the first 35% of a click swing pulls back, the rest strikes

    PlayerInventory inv;
    Camera cam;
    Collider[] playerColliders;   // the tool must never hit its own player

    Rigidbody tool;               // the floating tool (null = nothing held)
    string toolKey;               // which kind of item it is (ItemTemplates key), to notice when the slot changes
    string toolName;
    ItemProperties props;         // its stats (sharpness, weight...), from the inventory slot
    float weight;

    // Measured once when the tool appears, in the tool's own directions ("un-turned"), so they turn with it.
    Vector3 anchorOffset;         // the handle point, from the tool's position
    Quaternion localFrameInverse; // turns "head up, edge forward" into the tool's own directions
    Vector3 thinDir;              // the sharpest part's thin side (its edge cuts sideways to this)
    Transform edgePart;           // the sharpest part: only it can CUT

    bool swinging;                // a click swing is playing
    float swingStart;
    float lastHitTime = -10f;
    bool wasLocked;               // was the cursor locked last frame (the click that locks it must not swing)

    // Fired on every counted hit: point, damage, is it a cut. For effects later (sparks, sound, shake).
    public static event System.Action<Vector3, float, bool> Hit;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    void Start()
    {
        cam = Camera.main;
        Refresh();   // a slot may already be selected
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Refresh;   // also fired when the selected hotbar slot changes
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Refresh;
        RemoveTool();
    }

    void OnDestroy()
    {
        Hit = null;   // static values outlive the scene, so clear it
    }

    void Update()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;

        if (tool != null)
        {
            UpdateVisible();
            if (locked && wasLocked && CanSwing() && SwingPressed()) StartSwing();
        }

        wasLocked = locked;
    }

    // Physics work goes in FixedUpdate (Unity's physics step), not Update.
    void FixedUpdate()
    {
        if (tool == null || !tool.gameObject.activeSelf) return;

        float angle = SwingAngle(out float reach);
        Quaternion targetRot = TargetRotation(angle);

        // Heavy = slower to follow and turn. During a click swing, follow harder so the animation shows.
        float boost = swinging ? 2f : 1f;
        float heavy = Mathf.Sqrt(Mathf.Max(weight, 0.25f));

        // Move the HANDLE (not the tool's pivot) to the hold point.
        Vector3 handle = tool.position + tool.rotation * anchorOffset;
        Vector3 toTarget = HoldPoint(reach) - handle;
        tool.velocity = Vector3.ClampMagnitude(toTarget * followStrength * boost / heavy, maxSpeed);

        // Turn toward the target angle: find the turn that is left, and spin that way.
        Quaternion left = targetRot * Quaternion.Inverse(tool.rotation);
        left.ToAngleAxis(out float degrees, out Vector3 axis);
        if (degrees > 180f) degrees -= 360f;   // turn the short way round
        if (IsUsable(axis))
        {
            tool.angularVelocity = axis * (degrees * Mathf.Deg2Rad * turnStrength * boost / heavy);
        }
    }

    // ===== My methods =====

    // Makes the held tool match the selected hotbar slot. Called whenever the inventory changes.
    void Refresh()
    {
        PlayerInventory.Slot active = inv.ActiveItem;
        bool hasItem = active != null && !active.IsEmpty;
        string key = hasItem ? ItemTemplates.KeyFor(active.id, active.props) : null;

        if (key == toolKey) return;   // same item as before (e.g. only the count changed)

        RemoveTool();
        if (key == null || cam == null) return;

        GameObject go = ItemTemplates.Spawn(key, HoldPoint(0f));
        if (go == null) return;   // never collected this session, so there's no look to copy

        toolKey = key;
        toolName = active.name;
        props = active.props;
        PrepareTool(go);
    }

    // Turns the spawned copy into a held tool: not an item any more, physics on, gravity off.
    void PrepareTool(GameObject go)
    {
        // Not an item: F, AutoCollector and the crosshair must ignore it.
        foreach (ItemPickup p in go.GetComponentsInChildren<ItemPickup>()) Destroy(p);
        foreach (DroppedItem d in go.GetComponentsInChildren<DroppedItem>()) Destroy(d);
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = IGNORE_RAYCAST_LAYER;
            t.gameObject.tag = "Untagged";
        }

        tool = go.GetComponent<Rigidbody>();
        if (tool == null) tool = go.AddComponent<Rigidbody>();

        weight = Mathf.Max(props != null ? props.weight : tool.mass, 0.05f);
        tool.mass = weight;
        tool.isKinematic = false;
        tool.useGravity = false;
        tool.interpolation = RigidbodyInterpolation.Interpolate;                // smooth on screen
        tool.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // fast swings don't pass through things
        tool.maxAngularVelocity = 50f;                                          // Unity's default (7) is too slow for a swing

        // The tool must never hit its own player.
        foreach (Collider tc in go.GetComponentsInChildren<Collider>())
        {
            foreach (Collider pc in playerColliders) Physics.IgnoreCollision(tc, pc);
        }

        go.AddComponent<ToolHitReporter>().Setup(this);

        MeasureTool(go);
        SnapToHold();
    }

    // Finds the handle, the head direction and the edge, so the tool can be held the right way round.
    void MeasureTool(GameObject go)
    {
        MeshFilter[] parts = go.GetComponentsInChildren<MeshFilter>();
        Quaternion unturn = Quaternion.Inverse(tool.rotation);

        // 1. The sharpest part (smallest edge angle), like ItemProperties does: it is the "head".
        edgePart = null;
        float bestAngle = float.MaxValue;
        Vector3 edgeCenter = tool.position, edgeThin = Vector3.right, edgeLong = Vector3.up;
        float edgeLength = 0f;

        foreach (MeshFilter mf in parts)
        {
            if (mf.sharedMesh == null) continue;

            Vector3 size = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
            SharpnessMath.SortSides(size, out float t, out float w, out float L);
            EdgeShape shape = SharpnessMath.Resolve(props != null ? props.edgeShape : EdgeShape.Auto, mf.sharedMesh.name);
            float angle = SharpnessMath.EdgeAngle(shape, t, w);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                edgePart = mf.transform;
                edgeCenter = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
                edgeThin = AxisOfSide(mf.transform, size, t);
                edgeLong = AxisOfSide(mf.transform, size, L);
                edgeLength = L;
            }
        }

        // 2. The handle: the part farthest from the head (a hammer's grip end).
        //    One-part items (a stick, a stone): hold near one end of the longest side instead.
        Vector3 handle = edgeCenter - edgeLong * (edgeLength * 0.4f);
        float farthest = 0.01f;
        foreach (MeshFilter mf in parts)
        {
            if (mf.sharedMesh == null || mf.transform == edgePart) continue;

            Vector3 c = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
            float d = Vector3.Distance(c, edgeCenter);
            if (d > farthest) { farthest = d; handle = c; }
        }

        // 3. Store everything "un-turned", so it can be turned with the tool later.
        anchorOffset = unturn * (handle - tool.position);
        Vector3 head = unturn * (edgeCenter - handle);
        if (head.sqrMagnitude < 0.000001f) head = unturn * edgeLong;   // handle = head: use the long side
        head.Normalize();

        thinDir = unturn * edgeThin;
        Vector3 side = Vector3.ProjectOnPlane(thinDir, head);   // must be at right angles to the head direction
        if (side.sqrMagnitude < 0.000001f) side = Vector3.Cross(head, Vector3.forward);
        if (side.sqrMagnitude < 0.000001f) side = Vector3.Cross(head, Vector3.right);

        // "Head up, edge forward" in the tool's own directions. Inverse = from there back to the tool.
        localFrameInverse = Quaternion.Inverse(Quaternion.LookRotation(Vector3.Cross(side, head), head));
    }

    // The tool's axis (right, up or forward of a part) whose side length is "length".
    static Vector3 AxisOfSide(Transform part, Vector3 size, float length)
    {
        if (Mathf.Approximately(Mathf.Abs(size.x), length)) return part.right;
        if (Mathf.Approximately(Mathf.Abs(size.y), length)) return part.up;
        return part.forward;
    }

    void RemoveTool()
    {
        if (tool != null) Destroy(tool.gameObject);
        tool = null;
        toolKey = null;
        swinging = false;
    }

    // Shows the tool only where the ball may interact (inside an InteractionMachine's area, or the rule is on).
    void UpdateVisible()
    {
        bool show = InteractionMachine.CanInteractAt(HoldPoint(0f));
        if (tool.gameObject.activeSelf == show) return;

        tool.gameObject.SetActive(show);
        if (show) SnapToHold();      // appear in the hand, not where it was hidden
        else swinging = false;
    }

    // Puts the tool straight into the holding position, with no speed.
    void SnapToHold()
    {
        Quaternion rot = TargetRotation(0f);
        Vector3 pos = HoldPoint(0f) - rot * anchorOffset;

        tool.transform.SetPositionAndRotation(pos, rot);
        tool.position = pos;
        tool.rotation = rot;
        tool.velocity = Vector3.zero;
        tool.angularVelocity = Vector3.zero;
    }

    // Where the handle should float: beside the crosshair, plus "reach" forward during a strike.
    Vector3 HoldPoint(float reach)
    {
        Transform c = cam.transform;
        return c.position + c.right * holdOffset.x + c.up * holdOffset.y + c.forward * (holdOffset.z + reach);
    }

    // The tool's wanted turn: head up (leaning forward by restTilt + swing angle), edge facing forward.
    Quaternion TargetRotation(float swingAngle)
    {
        Transform c = cam.transform;

        // Positive angle turns "up" toward "forward" (around the camera's right side).
        Vector3 head = Quaternion.AngleAxis(restTilt + swingAngle, c.right) * c.up;
        Quaternion world = Quaternion.LookRotation(Vector3.Cross(c.right, head), head);

        return world * localFrameInverse;
    }

    // ----- Click swing -----

    bool CanSwing()
    {
        if (swinging || !tool.gameObject.activeSelf) return false;
        if (BlueprintPlacer.Busy) return false;   // that left click places a blueprint
        return !MenuState.AnyOpen;
    }

    void StartSwing()
    {
        swinging = true;
        swingStart = Time.time;
    }

    // The swing angle right now: pull back first, then strike forward and down. 0 when not swinging.
    float SwingAngle(out float reach)
    {
        reach = 0f;
        if (!swinging) return 0f;

        float duration = swingTime * Mathf.Sqrt(Mathf.Max(weight, 0.25f));   // heavy = slower swing
        float t = (Time.time - swingStart) / duration;                        // 0 to 1 over the swing

        if (t >= 1f)
        {
            swinging = false;   // done: the spring brings the tool back to rest
            return 0f;
        }

        if (t < WIND_UP_PART)
        {
            return -windUpAngle * Mathf.SmoothStep(0f, 1f, t / WIND_UP_PART);
        }

        float s = (t - WIND_UP_PART) / (1f - WIND_UP_PART);   // 0 to 1 over the strike
        reach = Mathf.Sin(s * Mathf.PI) * strikeReach;        // reach out in the middle of the strike
        return Mathf.Lerp(-windUpAngle, strikeAngle, Mathf.SmoothStep(0f, 1f, s));
    }

    // ----- Hits -----

    // Called by ToolHitReporter when the tool hits something.
    public void OnToolCollision(Collision collision)
    {
        if (Time.time - lastHitTime < hitCooldown) return;   // still the same hit

        float realSpeed = collision.relativeVelocity.magnitude;
        if (realSpeed < minHitSpeed) return;                  // just a touch

        // Click swing = fixed speed from weight. Mouse swing = the real speed.
        float speed = swinging ? clickSwingSpeed / Mathf.Sqrt(Mathf.Max(weight, 0.25f)) : realSpeed;

        // CUT only if the sharpest part hit, edge first. Everything else is BLUNT.
        ContactPoint contact = collision.GetContact(0);
        bool edgeTouched = edgePart != null && contact.thisCollider != null
                           && contact.thisCollider.transform.IsChildOf(edgePart);
        float sharpness = props != null ? props.sharpness : 0f;
        bool isCut = edgeTouched && sharpness > 0f
                     && SharpnessMath.IsEdgeHit(collision.relativeVelocity, tool.rotation * thinDir);

        float damage = isCut ? SharpnessMath.CutDamage(sharpness, weight, speed)
                             : SharpnessMath.BluntDamage(weight, speed);

        lastHitTime = Time.time;

        HitTarget target = collision.collider.GetComponentInParent<HitTarget>();
        if (target != null) target.TakeHit(toolName, damage, isCut, speed);

        Hit?.Invoke(contact.point, damage, isCut);   // tell the effects (if any are listening)
    }

    // ToAngleAxis can give a broken axis when there is no turn left.
    static bool IsUsable(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsInfinity(v.x) && v.sqrMagnitude > 0.5f;
    }

    bool SwingPressed()
    {
        return Input.GetKeyDown(KeyBindings.Swing);   // the key is chosen in KeyBindings
    }
}
