/*
First Date: 30/9/2026
Date: 3/10/2026
Author: Tony
Use by: Drag this script onto InventoryMenu.
        Then drag the object that holds your slot images (the grid) into "Slots Parent".
        Every child of Slots Parent is one slot. An icon, a count and InventorySlotClick are added to each slot automatically.
Purpose: Shows the items in PlayerInventory, with an icon and a count in each slot.
         Items are moved Minecraft-style by clicking (see InventorySlotClick);
         the stack you are holding is drawn under the mouse.
         Closing the inventory puts the held stack back into a slot.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The object whose children are the slot images (e.g. the grid of slots).")]
    [SerializeField] Transform slotsParent;

    [Range(0.1f, 1.5f)]
    [Tooltip("Size of the item icon compared to the slot (1 = fills the slot, 0.5 = half). Can be changed while playing.")]
    [SerializeField] float iconSize = 0.8f;

    [Range(8f, 48f)]
    [Tooltip("Size of the item count number in each slot.")]
    [SerializeField] float countFontSize = 18f;

    const float IconMargin = 4f;   // gap between the slot edge and the icon / count, in pixels

    Image[] icons;
    TMP_Text[] counts;

    RectTransform heldRoot;   // follows the mouse and shows the held stack
    Image heldIcon;
    TMP_Text heldCount;
    Canvas canvas;            // the top canvas, used to turn the mouse position into a UI position

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (slotsParent == null)
        {
            Debug.LogWarning("InventoryUI: drag the grid of slots into Slots Parent.");
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) canvas = parentCanvas.rootCanvas;

        BuildSlots();
        BuildHeld();
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Redraw;
        Redraw();   // items may have been added while the menu was closed
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Redraw;

        // The inventory closed: don't leave an item stuck on the cursor.
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.ReturnHeld();
    }

    // Unity calls this when a value is changed in the Inspector, so the icon size updates while playing.
    void OnValidate()
    {
        ApplyIconSize();
    }

    void Update()
    {
        FollowMouse();
    }

    // ===== My methods =====

    void FollowMouse()
    {
        if (heldRoot == null || !heldRoot.gameObject.activeSelf || canvas == null) return;

        // Move the held stack to the mouse (works for every canvas mode).
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)canvas.transform, Input.mousePosition, uiCam, out Vector3 worldPos))
        {
            heldRoot.position = worldPos;
        }
    }

    void BuildSlots()
    {
        int n = slotsParent.childCount;
        icons = new Image[n];
        counts = new TMP_Text[n];

        for (int i = 0; i < n; i++)
        {
            Transform slot = slotsParent.GetChild(i);
            icons[i] = CreateIcon(slot);
            counts[i] = CreateCount(slot, countFontSize);

            // Lets the player click this slot to pick up / put down items.
            InventorySlotClick click = slot.GetComponent<InventorySlotClick>();
            if (click == null) click = slot.gameObject.AddComponent<InventorySlotClick>();
            click.Setup(i);
        }
    }

    // Makes the icon + count that follow the mouse while holding a stack.
    void BuildHeld()
    {
        GameObject go = new GameObject("Held", typeof(RectTransform), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;   // a Layout Group on the menu must not move it
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling();   // drawn last = on top of the slots
        heldRoot = (RectTransform)go.transform;

        heldIcon = CreateIcon(heldRoot);
        heldCount = CreateCount(heldRoot, countFontSize);
        go.SetActive(false);

        ApplyIconSize();   // slots and held stack are both built now
    }

    // Resizes every icon (and the held one) to iconSize, with the count following it.
    void ApplyIconSize()
    {
        if (icons == null) return;   // slots not built yet (e.g. not playing)

        for (int i = 0; i < icons.Length; i++)
        {
            FitIcon(icons[i], counts[i], iconSize);
        }
        FitIcon(heldIcon, heldCount, iconSize);
    }

    void Redraw()
    {
        if (icons == null) return;   // slots were never built

        PlayerInventory inv = PlayerInventory.Instance;

        for (int i = 0; i < icons.Length; i++)
        {
            bool hasItem = inv != null && i < inv.Slots.Count && !inv.Slots[i].IsEmpty;
            ShowStack(icons[i], counts[i], hasItem ? inv.Slots[i] : null);
        }

        // The stack on the cursor.
        bool holding = inv != null && !inv.Held.IsEmpty && icons.Length > 0;
        heldRoot.gameObject.SetActive(holding);
        if (holding)
        {
            heldRoot.sizeDelta = ((RectTransform)slotsParent.GetChild(0)).rect.size;   // same size as a slot
            ShowStack(heldIcon, heldCount, inv.Held);
            FollowMouse();   // move it now, so it is never drawn for a frame at its old spot
        }
    }

    // The helpers below are static so HotbarUI can build its slots the same way.

    // Shows one stack in an icon + count. null = show nothing.
    public static void ShowStack(Image icon, TMP_Text count, PlayerInventory.Slot slot)
    {
        icon.enabled = slot != null;
        icon.sprite = slot != null ? slot.icon : null;
        count.text = slot != null && slot.count > 1 ? slot.count.ToString() : "";
    }

    // Scales the icon around the slot's center (scale doesn't change the layout, so it's cheap).
    // The count box is resized to the same area, so the number sits on the icon's corner.
    // The count itself is not scaled, so the number keeps its font size and stays readable.
    public static void FitIcon(Image icon, TMP_Text count, float size)
    {
        if (icon != null) icon.rectTransform.localScale = Vector3.one * size;
        if (count == null) return;

        RectTransform rt = count.rectTransform;
        float half = size * 0.5f;
        rt.anchorMin = new Vector2(0.5f - half, 0.5f - half);   // e.g. 0.8 → from 10% to 90% of the slot
        rt.anchorMax = new Vector2(0.5f + half, 0.5f + half);

        float margin = IconMargin * size;   // the icon's margin shrinks with it too
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    public static Image CreateIcon(Transform slot)
    {
        GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slot, false);
        Stretch((RectTransform)go.transform, IconMargin);

        Image img = go.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;   // clicks go to the slot, not the icon
        img.enabled = false;
        return img;
    }

    public static TMP_Text CreateCount(Transform slot, float fontSize)
    {
        GameObject go = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(slot, false);
        Stretch((RectTransform)go.transform, IconMargin);

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.fontSize = fontSize;
        txt.raycastTarget = false;
        txt.text = "";
        return txt;
    }

    // Makes a UI element fill its parent, with a small margin on every side.
    public static void Stretch(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }
}
