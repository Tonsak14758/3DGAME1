/*
First Date: 30/9/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (PlayerInventory is added automatically).
        Needs the Crosshair script in the scene. The item must be inside an InteractionMachine's area
        (or the GameRule WorldInteraction on).
Purpose: Aim the crosshair at an item and press the Grab key (F, set in KeyBindings)
         to put it in the inventory. Fires ItemGrabber.Grabbed so effects (PickupEffect) can play.
         An ItemBundle box gives back the whole stack it holds (not picked up while the inventory is full).
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class ItemGrabber : MonoBehaviour
{
    // ===== Fields =====
    [Range(1f, 20f)]
    [Tooltip("How far from the ball an item can be grabbed (measured to the item's surface).")]
    [SerializeField] float grabRange = 3f;

    PlayerInventory inv;

    // Fired when an item is grabbed, with the box around its meshes (the item itself is destroyed right after).
    // Visuals like PickupEffect listen to it.
    public static event System.Action<Bounds> Grabbed;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void OnDestroy()
    {
        Grabbed = null;   // static values outlive the scene, so clear it
    }

    void Update()
    {
        if (KeyBindMenu.IsOpen) return;   // keys pressed there are for rebinding, not for us
        if (!GrabPressed() || Crosshair.Target == null) return;

        ItemPickup item = Crosshair.Target.GetComponentInParent<ItemPickup>();
        if (item == null) return;   // not something you can pick up
        if (!CanGrab(Crosshair.Target)) return;   // too far, or no InteractionMachine around

        Grab(item);
    }

    // ===== My methods =====

    // True if F would really grab this object: it is inside an InteractionMachine's area
    // (or the WorldInteraction rule is on) and close enough. Crosshair also uses this for its color.
    public bool CanGrab(GameObject target)
    {
        return InteractionMachine.CanInteractWith(target.GetComponent<Collider>()) && InRange(target);
    }

    // True if the object is close enough to grab.
    public bool InRange(GameObject target)
    {
        // Measure to the nearest point on the object, so big items aren't harder to reach.
        Collider col = target.GetComponent<Collider>();
        Vector3 nearest = col != null ? col.ClosestPoint(transform.position) : target.transform.position;
        return Vector3.Distance(transform.position, nearest) <= grabRange;
    }

    void Grab(ItemPickup item)
    {
        // A bundle box gives back the stack it holds (with its own saved properties), not the box itself.
        ItemBundle bundle = item.GetComponent<ItemBundle>();
        if (bundle != null)
        {
            Bounds box = BoundsUtil.Of(item.gameObject);   // read before it is gone
            if (bundle.TryGiveTo(inv)) Grabbed?.Invoke(box);
            return;
        }

        ItemProperties props = item.ReadProperties();   // color, weight, material... read before it is destroyed

        // Different properties (e.g. a red and a blue stone) get their own icon.
        string key = ItemTemplates.KeyFor(item.ItemId, props);
        Sprite icon = IconCapture.GetIcon(key, item.gameObject);

        if (inv.Add(item.ItemId, item.DisplayName, icon, item.Amount, props))
        {
            ItemTemplates.Store(key, item.gameObject);   // keep a hidden copy so it can be dropped later
            Grabbed?.Invoke(BoundsUtil.Of(item.gameObject));   // tell the visuals before it is gone
            Destroy(item.gameObject);
        }
        else
        {
            Debug.Log("Inventory is full.");
        }
    }

    bool GrabPressed()
    {
        return Input.GetKeyDown(KeyBindings.Grab);   // the key is chosen in KeyBindings
    }
}
