/*
First Date: 2/10/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (PlayerInventory is added automatically).
        Optional: drag the ItemBundle prefab into "Bundle Prefab".
        The scene needs an EventSystem, so the script can tell clicks on the inventory from clicks outside it.
Purpose: Like Minecraft: while the inventory is open and a stack is held on the cursor,
         clicking outside the inventory drops it in front of the player.
         Left click = drop the whole stack. Right click = drop one.
         Dropping "Bundle Threshold" (5) or more at once makes one ItemBundle box instead (less lag).
*/

using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: is the mouse over UI?

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class ItemDropper : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.5f, 5f)]
    [Tooltip("How far in front of the player dropped items appear.")]
    [SerializeField] float dropDistance = 1.5f;

    [Range(0f, 10f)]
    [Tooltip("How fast dropped items are thrown forward.")]
    [SerializeField] float throwSpeed = 3f;

    [Range(0f, 5f)]
    [Tooltip("Seconds before a dropped AutoPickUp item can be collected by touch again.")]
    [SerializeField] float pickupDelay = 1.5f;

    [Header("Bundles")]
    [Tooltip("The box prefab with ItemBundle on it. Empty = always drop real items.")]
    [SerializeField] ItemBundle bundlePrefab;

    [Range(2, 100)]
    [Tooltip("Dropping this many or more at once makes ONE bundle box instead of that many objects (less lag).")]
    [SerializeField] int bundleThreshold = 5;

    PlayerInventory inv;
    Camera cam;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (!InventoryManager.IsOpen || KeyBindMenu.IsOpen) return;   // only while the inventory is open
        if (inv.Held.IsEmpty) return;                                 // holding nothing
        if (PointerOverUI()) return;                                  // clicks on the inventory are for the slots

        if (LeftClicked()) Drop(inv.Held.count);   // the whole stack
        else if (RightClicked()) Drop(1);          // just one
    }

    // ===== My methods =====

    void Drop(int count)
    {
        PlayerInventory.Slot held = inv.Held;
        string key = ItemTemplates.KeyFor(held.id, held.props);

        // In front of the player, the way the camera looks (flat, so it doesn't drop into the ground).
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 dropPoint = transform.position + forward * dropDistance + Vector3.up * 0.5f;

        // Many at once: one box holds them all (no saved copy of the item is needed for that).
        if (bundlePrefab != null && count >= bundleThreshold)
        {
            DropBundle(held, count, dropPoint, forward);
            inv.RemoveFromHeld(count);
            return;
        }

        if (!ItemTemplates.Has(key))
        {
            Debug.Log("Can't drop " + held.name + ": no copy of this item was saved.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // A small random offset, so a dropped stack doesn't spawn all in the exact same spot.
            GameObject go = ItemTemplates.Spawn(key, dropPoint + Random.insideUnitSphere * 0.2f);
            if (go != null) SetUpDropped(go, held.props, forward);
        }

        inv.RemoveFromHeld(count);
    }

    // Spawns one bundle box holding "count" of the held item, thrown like a normal drop.
    void DropBundle(PlayerInventory.Slot held, int count, Vector3 dropPoint, Vector3 forward)
    {
        ItemBundle bundle = Instantiate(bundlePrefab, dropPoint, Quaternion.identity);

        Rigidbody rb = bundle.GetComponent<Rigidbody>();
        rb.velocity = forward * throwSpeed + Vector3.up;   // throw it forward and a little up

        DroppedItem dropped = bundle.GetComponent<DroppedItem>();
        if (dropped == null) dropped = bundle.gameObject.AddComponent<DroppedItem>();
        dropped.Setup(pickupDelay);   // an AutoPickUp bundle waits a moment, like dropped items

        bundle.Setup(held, count);    // last: it may merge into a bundle lying close by and disappear
    }

    void SetUpDropped(GameObject go, ItemProperties props, Vector3 forward)
    {
        // Each dropped object is worth 1, because a stack of 5 drops as 5 objects.
        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup != null) pickup.SetAmount(1);

        // It needs a Rigidbody to fall and be thrown.
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            // Same mass as its weight, so picking it up again gives the same weight (and it stacks again).
            rb.mass = Mathf.Max(0.01f, props != null ? props.weight : 1f);
        }
        rb.velocity = forward * throwSpeed + Vector3.up;   // throw it forward and a little up

        DroppedItem dropped = go.GetComponent<DroppedItem>();
        if (dropped == null) dropped = go.AddComponent<DroppedItem>();
        dropped.Setup(pickupDelay);
    }

    // True if the mouse is over any UI (the inventory panel, a slot, a button...).
    bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    bool LeftClicked()
    {
        return Input.GetMouseButtonDown(0);   // 0 = left mouse button
    }

    bool RightClicked()
    {
        return Input.GetMouseButtonDown(1);   // 1 = right mouse button
    }
}
