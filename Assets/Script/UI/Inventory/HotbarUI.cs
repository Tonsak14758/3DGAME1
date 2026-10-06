/*
First Date: 3/10/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto Hotbar (a child of InventoryCanvas, NOT inside InventoryMenu, so it stays on screen).
        Drag the object whose children are Slot1 - Slot8 into "Slots Parent",
        and the SelectionHighlight image into "Highlight".
        In the Hierarchy, put Hotbar ABOVE InventoryMenu, so the stack held on the mouse draws on top of the hotbar.
        Number of slots here = PlayerInventory's "Hotbar Size".
Purpose: Shows the hotbar slots of PlayerInventory (icon + count) and the selected (active) slot.
         Keys 1 - 8 (set in KeyBindings) or the mouse wheel choose the active slot.
         Pressing the key of the selected slot again unselects it (no slot selected = highlight hidden).
         With the inventory open (E), items move in and out with the same clicks as the inventory.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The object whose children are the hotbar slot images (Slot1 - Slot8).")]
    [SerializeField] Transform slotsParent;

    [Tooltip("The image that marks the active slot. It is moved onto the active slot and stretched to its size.")]
    [SerializeField] RectTransform highlight;

    [Range(0.5f, 2f)]
    [Tooltip("Size of the highlight compared to the slot (1 = same size as the slot, 1.2 = 20% bigger). Can be changed while playing.")]
    [SerializeField] float highlightSize = 1.1f;

    [Range(0.1f, 1.5f)]
    [Tooltip("Size of the item icon compared to the slot (1 = fills the slot, 0.5 = half). Can be changed while playing.")]
    [SerializeField] float iconSize = 0.6f;

    [Range(8f, 48f)]
    [Tooltip("Size of the item count number in each slot.")]
    [SerializeField] float countFontSize = 18f;

    [Tooltip("Scroll the mouse wheel to change the active slot.")]
    [SerializeField] bool useMouseWheel = true;

    Image[] icons;
    TMP_Text[] counts;

    UISpriteAnimation highlightAnim;           // the highlight's animation (can be missing)
    int shownSlot = PlayerInventory.NoHotbar;  // the slot the highlight is on now, to notice when it changes

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (slotsParent == null)
        {
            Debug.LogWarning("HotbarUI: drag the parent of Slot1 - Slot8 into Slots Parent.");
            return;
        }

        if (highlight != null)
        {
            // The highlight must not catch clicks, or the slot under it can't be clicked.
            Graphic g = highlight.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = false;

            highlightAnim = highlight.GetComponent<UISpriteAnimation>();   // cache it once
        }
    }

    // Start, not Awake: PlayerInventory.Awake must have made the slots first (HotbarStart is needed).
    void Start()
    {
        if (slotsParent == null) return;

        BuildSlots();
        Redraw();
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Redraw;
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Redraw;
    }

    // Unity calls this when a value is changed in the Inspector, so the sizes update while playing.
    void OnValidate()
    {
        ApplyIconSize();
        if (Application.isPlaying) ApplyHighlightSize();   // in Edit mode, leave the highlight in the scene as you made it
    }

    void Update()
    {
        if (KeyBindMenu.IsOpen) return;   // keys pressed there are for rebinding, not for us

        ReadSelectInput();
    }

    // ===== My methods =====

    void ReadSelectInput()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) return;

        // Number keys: select that slot, or unselect it if it is already selected.
        int keys = Mathf.Min(inv.HotbarSize, KeyBindings.HotbarKeyCount);
        for (int i = 0; i < keys; i++)
        {
            if (Input.GetKeyDown(KeyBindings.HotbarKey(i)))
            {
                inv.ToggleHotbar(i);
                return;
            }
        }

        // Mouse wheel: down = next slot, up = previous slot (like Minecraft).
        if (useMouseWheel && !BlueprintPlacer.IsPlacing)   // while placing, the wheel rotates the blueprint
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll < 0f) inv.ScrollHotbar(1);
            else if (scroll > 0f) inv.ScrollHotbar(-1);
        }
    }

    void BuildSlots()
    {
        PlayerInventory inv = PlayerInventory.Instance;
        int n = slotsParent.childCount;

        if (inv != null && n != inv.HotbarSize)
        {
            Debug.LogWarning("HotbarUI: " + n + " slots in the UI but PlayerInventory has Hotbar Size " + inv.HotbarSize + ".");
        }

        icons = new Image[n];
        counts = new TMP_Text[n];
        int start = inv != null ? inv.HotbarStart : 0;

        for (int i = 0; i < n; i++)
        {
            Transform slot = slotsParent.GetChild(i);

            icons[i] = InventoryUI.CreateIcon(slot);
            icons[i].transform.SetAsFirstSibling();   // drawn first = behind the slot number label
            counts[i] = InventoryUI.CreateCount(slot, countFontSize);

            // Same clicking as the inventory. The index is the slot's place in PlayerInventory.Slots.
            InventorySlotClick click = slot.GetComponent<InventorySlotClick>();
            if (click == null) click = slot.gameObject.AddComponent<InventorySlotClick>();
            click.Setup(start + i);
        }

        ApplyIconSize();
    }

    // Resizes every icon to iconSize, with the count following it (see InventoryUI.FitIcon).
    void ApplyIconSize()
    {
        if (icons == null) return;   // slots not built yet (e.g. not playing)

        for (int i = 0; i < icons.Length; i++)
        {
            InventoryUI.FitIcon(icons[i], counts[i], iconSize);
        }
    }

    void Redraw()
    {
        if (icons == null) return;   // slots were never built

        PlayerInventory inv = PlayerInventory.Instance;

        for (int i = 0; i < icons.Length; i++)
        {
            int index = inv != null ? inv.HotbarStart + i : -1;
            bool hasItem = inv != null && index < inv.Slots.Count && !inv.Slots[index].IsEmpty;
            InventoryUI.ShowStack(icons[i], counts[i], hasItem ? inv.Slots[index] : null);
        }

        MoveHighlight(inv != null ? inv.ActiveHotbar : PlayerInventory.NoHotbar);
    }

    // Puts the highlight inside the active slot, stretched to its size, drawn behind the slot's contents.
    // Being a child of the slot means it follows the slot without any work every frame.
    // No slot selected (i = -1): the highlight is hidden, which also stops its animation.
    // The animation starts from frame 1 whenever the highlight lands on a new slot.
    void MoveHighlight(int i)
    {
        if (highlight == null) return;

        bool show = i >= 0 && i < slotsParent.childCount;
        if (!show)
        {
            highlight.gameObject.SetActive(false);
            shownSlot = PlayerInventory.NoHotbar;
            return;
        }

        if (i == shownSlot) return;   // same slot (e.g. only an item changed): don't restart the animation

        bool wasHidden = !highlight.gameObject.activeSelf;
        highlight.SetParent(slotsParent.GetChild(i), false);
        highlight.SetAsFirstSibling();   // drawn first = over the slot image, but behind the icon, number and count
        InventoryUI.Stretch(highlight, 0f);   // same size as the slot; highlightSize scales it from there
        ApplyHighlightSize();
        highlight.gameObject.SetActive(true);

        // Showing it again already restarts it (UISpriteAnimation.OnEnable); moving from slot to slot doesn't, so do it here.
        if (!wasHidden && highlightAnim != null) highlightAnim.Restart();

        shownSlot = i;
    }

    // Scales the highlight around the slot's center (scale doesn't change the layout, so it's cheap).
    void ApplyHighlightSize()
    {
        if (highlight != null) highlight.localScale = Vector3.one * highlightSize;
    }
}
