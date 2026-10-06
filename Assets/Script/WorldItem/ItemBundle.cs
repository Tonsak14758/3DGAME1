/*
Date: 5/10/2026
Author: Tony
Use by: Make the BUNDLE PREFAB:
          ItemBundle (Cube, scale ~0.4)   <- BoxCollider + Rigidbody + THIS SCRIPT + ItemPickup + a tag
              Label (Empty, ~0.4 above)                  -> drag into "Label"
                  Icon  (Sprite Renderer, Sprite empty)   -> drag into "Icon Renderer"
                  Count (3D Object > Text - TextMeshPro)  -> drag into "Count Text"
        1. Hierarchy: right click > 3D Object > Cube, rename it "ItemBundle", Scale 0.4 (it already has a Box Collider).
           Drag this script (ItemBundle.cs) from the Project window onto it.
           Add Component > Rigidbody, and Add Component > ItemPickup.
           Tag (top of the Inspector): "PickUp" = grabbed with F, "AutoPickUp" = collected by touch.
           (ItemPickup makes the crosshair see it; its Item Id doesn't matter, the box uses the stack it holds.)
        2. Right click ItemBundle > Create Empty, rename it "Label", Position Y = 1 (about 0.4 above the box).
        3. Right click Label > 2D Object > Sprites > Square, rename it "Icon".
           This makes a Sprite Renderer (draws a picture in the 3D world, no Canvas needed).
           Its Sprite field can stay as it is: the script puts the item's icon there.
        4. Right click Label > 3D Object > Text - TextMeshPro (the 3D one, NOT UI > Text), rename it "Count".
           Font Size about 3, Alignment center, Position Y about -0.6 (under the icon).
        5. Select ItemBundle and drag into this script's fields:
           Label -> "Label",  Icon -> "Icon Renderer",  Count -> "Count Text".
        6. Drag ItemBundle from the Hierarchy into the Project window (it becomes a prefab), delete it from the scene,
           then drag the prefab into ItemDropper's "Bundle Prefab" (on the PlayerSphere).
Purpose: One box that holds a whole dropped stack (e.g. 100 Stone), so dropping a lot doesn't spawn 100 objects (lag).
         Shows the item's icon and "100x". Picking it up gives the whole stack back with its ORIGINAL properties
         (color, weight...), not the box's. It can't be picked up while the inventory is full.
         Two bundles of the same item that touch (or are dropped close together) merge into one.
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class ItemBundle : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The object holding the icon and the count.")]
    [SerializeField] Transform label;

    [Header("Label")]
    [Tooltip("Shows the item's icon (a Sprite Renderer in the Label).")]
    [SerializeField] SpriteRenderer iconRenderer;

    [Tooltip("Shows the amount, e.g. \"5x\" (a 3D TextMeshPro in the Label).")]
    [SerializeField] TMP_Text countText;

    
    
    [Range(0.05f, 2f)]
    [Tooltip("Width of the icon in world units.")]
    [SerializeField] float iconSize = 0.3f;

    [Tooltip("Ticked: the label floats above the box and always faces the camera (easy to read). " +
             "Unticked: the label stays where you put it on the box (e.g. printed on a side), costs nothing per frame.")]
    [SerializeField] bool faceCamera = true;

    [Header("Merging")]
    [Range(0f, 3f)]
    [Tooltip("A new bundle merges into a bundle of the same item this close to it. 0 = only when they touch.")]
    [SerializeField] float mergeRadius = 1f;

    [Tooltip("What the bundle holds. Shown here for checking while playing.")]
    [SerializeField] PlayerInventory.Slot stack = new PlayerInventory.Slot();

    Rigidbody rb;
    Camera cam;
    float labelHeight;   // how far above the box's center the label floats
    bool merged;         // already merged into another bundle (about to be destroyed)

    public PlayerInventory.Slot Stack => stack;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (label != null) labelHeight = Vector3.Distance(label.position, transform.position);
    }

    void Start()
    {
        cam = Camera.main;
    }

    // LateUpdate: after the box has moved and the camera has turned this frame.
    void LateUpdate()
    {
        if (!faceCamera || label == null || cam == null) return;
        if (iconRenderer != null && !iconRenderer.isVisible) return;   // not on screen: no work

        // Straight above the box (not turned with it, so it stays on top while the box tumbles),
        // facing the same way as the camera, so the text is never mirrored.
        label.position = transform.position + Vector3.up * labelHeight;
        label.rotation = cam.transform.rotation;
    }

    // Two bundles bumped into each other: the same item merges into one.
    void OnCollisionEnter(Collision collision)
    {
        ItemBundle other = collision.collider.GetComponentInParent<ItemBundle>();
        if (other == null) return;

        // Both bundles get this message; only one of them may do the merging (the older one, lower id).
        if (CanMergeWith(other) && GetInstanceID() < other.GetInstanceID()) Absorb(other);
    }

    // ===== My methods =====

    // Fills the bundle (called by ItemDropper right after spawning it). Merges into a nearby same bundle if there is one.
    public void Setup(PlayerInventory.Slot from, int count)
    {
        stack.CopyItemFrom(from);   // id, name, icon, properties (the count is set below)
        stack.count = count;
        Refresh();

        MergeIntoNearby();
    }

    // Gives the whole stack to the inventory and removes the bundle.
    // Returns false (and keeps the bundle) if the inventory has no room.
    public bool TryGiveTo(PlayerInventory inv)
    {
        if (merged || stack.IsEmpty) return false;

        if (!inv.Add(stack.id, stack.name, stack.icon, stack.count, stack.props))
        {
            ScreenMessage.Show("Inventory is full");
            return false;
        }

        merged = true;              // can't be given twice (e.g. two parts touched in the same frame)
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    bool CanMergeWith(ItemBundle other)
    {
        if (other == this || merged || other.merged) return false;
        return stack.id == other.stack.id && ItemProperties.Same(stack.props, other.stack.props);
    }

    // Takes the other bundle's items, and the other bundle disappears.
    void Absorb(ItemBundle other)
    {
        stack.count += other.stack.count;
        other.merged = true;
        Destroy(other.gameObject);
        Refresh();
    }

    // A freshly dropped bundle joins an older one lying close by, instead of making a pile of boxes.
    void MergeIntoNearby()
    {
        if (mergeRadius <= 0f) return;

        foreach (Collider c in Physics.OverlapSphere(transform.position, mergeRadius))
        {
            ItemBundle other = c.GetComponentInParent<ItemBundle>();
            if (other != null && other.CanMergeWith(this))
            {
                other.Absorb(this);   // the old one keeps its place; this new one disappears
                return;
            }
        }
    }

    // Updates the label and the weight to match what the bundle holds.
    void Refresh()
    {
        if (countText != null) countText.text = stack.count + "x";

        if (iconRenderer != null)
        {
            iconRenderer.sprite = stack.icon;

            // Scale the picture to iconSize wide, whatever its size in pixels
            // (and whatever the box's own scale, which the label inherits).
            if (stack.icon != null && stack.icon.bounds.size.x > 0f)
            {
                Transform parent = iconRenderer.transform.parent;
                float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
                if (parentScale <= 0f) parentScale = 1f;

                float scale = iconSize / (stack.icon.bounds.size.x * parentScale);
                iconRenderer.transform.localScale = Vector3.one * scale;
            }
        }

        // As heavy as everything inside it.
        float each = stack.props != null ? stack.props.weight : 1f;
        rb.mass = Mathf.Max(0.05f, each * stack.count);
    }
}
