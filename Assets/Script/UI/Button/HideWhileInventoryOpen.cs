/*
Date: 30/9/2026
Author: Tony
Use by: Drag this script onto any UI object (panel, image, text) that should hide while the inventory is open.
        A Canvas Group is added automatically. Nothing needs to be dragged into InventoryManager.
Purpose: Listens to InventoryManager's Toggled event and hides this UI while the inventory is open.
*/

using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]   // lets us hide the object without turning it off
public class HideWhileInventoryOpen : MonoBehaviour
{
    // ===== Fields =====
    CanvasGroup group;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        InventoryManager.Toggled += OnInventoryToggled;   // start listening
        OnInventoryToggled(InventoryManager.IsOpen);      // match the current state right away
    }

    void OnDisable()
    {
        InventoryManager.Toggled -= OnInventoryToggled;   // stop listening
    }

    // ===== My methods =====

    // InventoryManager calls this every time the inventory opens or closes.
    void OnInventoryToggled(bool open)
    {
        group.alpha = open ? 0f : 1f;    // 0 = invisible, 1 = fully visible
        group.blocksRaycasts = !open;    // hidden UI can't be clicked
        group.interactable = !open;
    }
}
