/*
Date: 5/10/2026
Author: Tony
Use by: Nothing to set up. Other scripts ask: if (MenuState.AnyOpen) ...
Purpose: One place to check "is any menu open?" (they all free the cursor and pause aiming).
         Adding a new menu = add its IsOpen here, and every script that asks follows by itself.
*/

public static class MenuState
{
    // ===== My methods =====

    public static bool AnyOpen =>
        InventoryManager.IsOpen || KeyBindMenu.IsOpen || BuildMenu.IsOpen || BlueprintPanel.IsOpen;
}
