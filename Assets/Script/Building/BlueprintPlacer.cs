/*
Date: 5/10/2026
Author: Tony
Use by: 1. Hierarchy: select PlayerSphere > Add Component > BlueprintPlacer (only one, on the player).
        2. Optional Inspector settings: Place Range (how far away you can place), Rotate Step (degrees per wheel step),
           Auto Place (start with auto placement on).
        3. Nothing else to connect: BuildMenu starts it when a building is clicked (BlueprintPlacer.Begin).
        Needs: an InteractionMachine in the scene (or GameRule "World Interaction" ticked), otherwise every blueprint is red.
        Optional: a ScreenMessage in the scene to show why a blueprint is red (else the reason goes to the Console).
        The blueprint follows the crosshair by the MIDDLE of the building's shapes, wherever its root object (pivot) is.
        Recommended: in the toolbar (top left of the Scene view), click "Pivot" to switch it to "Center".
        Unity's Move / Rotate tools then appear in the middle of the selected shapes, the same point this script uses.
        This is only a view setting and changes nothing in the game.
Purpose: Moves a new blueprint with the crosshair until the player puts it down.
         Left click = place, right click = cancel, mouse wheel = rotate,
         middle click = auto placement on / off (keys set in KeyBindings).
         Auto placement on: the blueprint sits on the surface the crosshair points at.
         Auto placement off: it floats at a fixed distance in front of you (e.g. to place it against a wall).
         It turns red, and can't be placed, when it is outside an InteractionMachine's area,
         inside something, or in the air (unless a piece is made of a floating material).
*/

using UnityEngine;

[DisallowMultipleComponent]
public class BlueprintPlacer : MonoBehaviour
{
    // ===== Fields =====
    [Range(2f, 50f)]
    [Tooltip("How far from the camera a blueprint can be placed.")]
    [SerializeField] float placeRange = 12f;

    [Range(1f, 90f)]
    [Tooltip("Degrees turned per mouse wheel step.")]
    [SerializeField] float rotateStep = 15f;

    [Tooltip("Start with auto placement on (sit on the surface under the crosshair).")]
    [SerializeField] bool autoPlace = true;

    static BlueprintPlacer instance;   // the one on the player
    static int stopFrame = -1;         // the frame placing ended (that click must not also do something else)

    Camera cam;
    Blueprint ghost;                   // the blueprint being placed (null = not placing)
    int startFrame;                    // the frame placing started (that click must not place it straight away)
    float yaw;                         // the blueprint's turn, in degrees
    float freeDistance;                // auto placement off: how far in front of the camera it floats
    Vector3 pivotToBottom;             // from the blueprint's position (pivot) to the middle of its bottom, unturned
    Vector3 pivotToCenter;             // from the pivot to the middle of the whole building, unturned
    bool valid;                        // can it be placed where it is now?
    string problem;                    // why not (shown on screen when the player tries)

    public static bool IsPlacing { get; private set; }

    // Placing now, or it ended this frame. Other scripts using the same buttons check this,
    // so e.g. the right click that cancels doesn't also start levitating.
    public static bool Busy => IsPlacing || Time.frameCount == stopFrame;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        cam = Camera.main;
    }

    void OnDisable()
    {
        Cancel();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear them
        IsPlacing = false;
    }

    void Update()
    {
        if (!IsPlacing) return;
        if (ghost == null) { Stop(); return; }   // destroyed by something else

        // A menu is open (the cursor is free): wait.
        if (MenuState.AnyOpen || Cursor.lockState != CursorLockMode.Locked) return;
        if (Time.frameCount == startFrame) return;   // the click that chose the building

        if (CancelPressed()) { Cancel(); return; }
        if (AutoPressed()) ToggleAuto();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) yaw += rotateStep;
        else if (scroll < 0f) yaw -= rotateStep;

        if (PlacePressed()) TryPlace();
    }

    // LateUpdate: the camera has moved this frame, so the blueprint follows the crosshair exactly.
    void LateUpdate()
    {
        if (!IsPlacing || ghost == null || cam == null) return;

        Move();
        valid = Check(out problem);
        ghost.SetValid(valid);
    }

    // ===== My methods =====

    // Starts placing a new blueprint of this building (called by BuildMenu).
    public static void Begin(BuildingData data)
    {
        if (instance == null)
        {
            Debug.LogWarning("BlueprintPlacer: add it to the PlayerSphere.");
            return;
        }
        instance.StartPlacing(data);
    }

    // Stops placing and throws the blueprint away (e.g. B pressed while placing).
    public static void CancelPlacing()
    {
        if (instance != null) instance.Cancel();
    }

    void StartPlacing(BuildingData data)
    {
        if (data == null || data.Prefab == null) return;
        Cancel();   // only one at a time

        ghost = Blueprint.Create(data);

        // Measure where the building really is compared to its pivot (the prefab's root can be off to one side
        // of the shapes). Measured once at position 0 with no turn; Move() turns these with the blueprint.
        ghost.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Bounds b = BoundsUtil.Of(ghost.gameObject);
        pivotToCenter = b.center;
        pivotToBottom = new Vector3(b.center.x, b.min.y, b.center.z);

        yaw = cam != null ? cam.transform.eulerAngles.y : 0f;   // start facing the way you look
        freeDistance = placeRange * 0.5f;
        startFrame = Time.frameCount;
        IsPlacing = true;
    }

    void TryPlace()
    {
        if (!valid)
        {
            ScreenMessage.Show(problem);
            return;
        }

        ghost.Place();
        ghost = null;   // it stays in the world as a placed blueprint
        Stop();
    }

    void Cancel()
    {
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        if (IsPlacing) Stop();
    }

    void Stop()
    {
        IsPlacing = false;
        stopFrame = Time.frameCount;
    }

    void ToggleAuto()
    {
        autoPlace = !autoPlace;

        // Turning it off: keep floating at the distance it is now, so it doesn't jump.
        if (!autoPlace)
        {
            float d = Vector3.Distance(cam.transform.position, ghost.transform.position);
            freeDistance = Mathf.Clamp(d, 2f, placeRange);
        }

        ScreenMessage.Show(autoPlace ? "Auto placement: ON" : "Auto placement: OFF");
    }

    // Puts the blueprint where the crosshair points.
    void Move()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));   // from the screen center
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 pos;

        // Triggers are ignored: other blueprints' blue pieces don't count as ground.
        if (autoPlace && Physics.Raycast(ray, out RaycastHit hit, placeRange,
                                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // The middle of the bottom sits on the point the crosshair hits.
            pos = hit.point - rot * pivotToBottom;
        }
        else
        {
            // The middle of the building floats on the crosshair line.
            Vector3 point = ray.origin + ray.direction * (autoPlace ? placeRange : freeDistance);
            pos = point - rot * pivotToCenter;
        }

        ghost.transform.SetPositionAndRotation(pos, rot);
    }

    // Can it be placed here? If not, says why.
    bool Check(out string why)
    {
        why = null;

        if (!InteractionMachine.CanInteractWith(BoundsUtil.Of(ghost.gameObject)))
            why = "No Interaction Machine in range";
        else if (ghost.Overlaps())
            why = "Something is in the way";
        else if (!ghost.IsSupported())
            why = "Can't build in the air";

        return why == null;
    }

    // The keys below are chosen in KeyBindings.
    bool PlacePressed()
    {
        return Input.GetKeyDown(KeyBindings.PlaceBuilding);
    }

    bool CancelPressed()
    {
        return Input.GetKeyDown(KeyBindings.CancelBuilding);
    }

    bool AutoPressed()
    {
        return Input.GetKeyDown(KeyBindings.AutoPlace);
    }
}
