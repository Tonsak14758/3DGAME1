/*
First Date: 2/10/2026
Date: 3/10/2026
Author: Tony
Use by: Nothing to set up: InventoryUI and HotbarUI add this to every slot automatically.
        The scene needs an EventSystem (GameObject > UI > Event System) and the slot images
        must have "Raycast Target" ticked so they can be clicked.
Purpose: Minecraft-style clicking on an inventory slot.
         Left click = pick up / put down / swap the whole stack.
         Right click = pick up half / put down one.
         Hold right click while holding a stack and move over slots = put down one in each slot.
         Double left click a slot = gather every stack of that item into this slot.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // IPointerDownHandler, IPointerEnterHandler

[DisallowMultipleComponent]
public class InventorySlotClick : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    // ===== Fields =====
    int index;   // which inventory slot this is (0, 1, 2 ...)

    // Shared by all slots: are we spreading items with right click, and which slots already got one.
    static bool spreading;
    static readonly HashSet<int> visited = new HashSet<int>();   // HashSet = a list with no duplicates, fast "Contains"

    // Double click: two left clicks on the same slot within this many seconds.
    const float DoubleClickTime = 0.3f;
    static int lastClickIndex = -1;   // -1 = no slot
    static float lastClickTime;

    // ===== Unity messages (Unity calls these) =====

    // Called the moment a mouse button is pressed on this slot (Minecraft reacts on press, not release).
    public void OnPointerDown(PointerEventData eventData)
    {
        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) return;
        if (!InventoryManager.IsOpen) return;   // hotbar slots are always on screen, but only clickable with the inventory open

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Second click on the same slot, quickly enough = double click.
            bool doubleClick = index == lastClickIndex && Time.unscaledTime - lastClickTime < DoubleClickTime;

            if (doubleClick)
            {
                inv.GatherSame(index);
                lastClickIndex = -1;   // a third quick click starts fresh, it is not another double click
            }
            else
            {
                inv.LeftClick(index);
                lastClickIndex = index;
                lastClickTime = Time.unscaledTime;   // unscaled = still works if the game is paused (timeScale 0)
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Holding a stack and this slot can take it? Start spreading: this slot gets the first one.
            visited.Clear();
            spreading = inv.PlaceOne(index);
            if (spreading) visited.Add(index);
            else inv.RightClick(index);   // normal right click: pick up half, or swap
        }
        // The inventory fires Changed, and InventoryUI redraws.
    }

    // Called when the mouse moves onto this slot.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!spreading) return;

        if (!RightHeld())
        {
            spreading = false;   // the button was let go: the spread is over
            return;
        }

        if (visited.Contains(index)) return;   // each slot gets only one per spread

        // PlaceOne fails if the slot has a different item or the stack ran out; that's fine, skip it.
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.PlaceOne(index))
        {
            visited.Add(index);
        }
    }

    void OnDestroy()
    {
        spreading = false;   // static values outlive the scene, so clear them
        visited.Clear();
        lastClickIndex = -1;
    }

    // ===== My methods =====

    // InventoryUI calls this once when it builds the slots.
    public void Setup(int slotIndex)
    {
        index = slotIndex;
    }

    bool RightHeld()
    {
        return Input.GetMouseButton(1);   // 1 = right mouse button
    }
}
