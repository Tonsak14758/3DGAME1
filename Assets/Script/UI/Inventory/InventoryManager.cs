/*
First Date: 30/9/2026
Date: 5/10/2026
Author: Tony
Use by: Add this script to InventoryCanvas.
        Then drag InventoryMenu (the child of InventoryCanvas) into the "Inventory Menu" slot.
Purpose: Press the Inventory key (E, set in KeyBindings) to open or close the inventory.
         While open: the cursor is shown so items can be clicked, the camera stops turning,
         and the crosshair hides by itself.
*/

using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The inventory panel to show and hide (a child of InventoryCanvas).")]
    [SerializeField] GameObject inventoryMenu;

    [Tooltip("Optional: extra objects to hide while the inventory is open (e.g. a mini-map).")]
    [SerializeField] GameObject[] hideWhenOpen;

    public static bool IsOpen { get; private set; }   // any script can check InventoryManager.IsOpen

    // Other scripts subscribe to this to be told when the inventory opens (true) or closes (false).
    public static event System.Action<bool> Toggled;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        SetMenu(false);   // start with the inventory closed
    }

    void Update()
    {
        // Another menu owns the cursor (and keys pressed in the key menu are for rebinding).
        if (KeyBindMenu.IsOpen || BuildMenu.IsOpen || BlueprintPanel.IsOpen) return;

        if (Input.GetKeyDown(KeyBindings.Inventory)) SetMenu(!IsOpen);   // the key is chosen in KeyBindings
    }

    void OnDestroy()
    {
        IsOpen = false;   // static values outlive the scene, so clear it
    }

    // ===== My methods =====

    // Also usable from a Close button's On Click () list.
    public void SetMenu(bool open)
    {
        IsOpen = open;
        inventoryMenu.SetActive(open);

        foreach (GameObject obj in hideWhenOpen)
        {
            if (obj != null) obj.SetActive(!open);   // skip empty slots in the list
        }

        // Open: free cursor to click items. Closed: hidden cursor for the camera.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        Toggled?.Invoke(open);   // tell every listening script (if there are any)
    }
}
