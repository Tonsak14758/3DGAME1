/*
Date: 5/10/2026
Author: Tony
Use by: This is an ASSET (a file in the Project window), not a script you put on an object.
        1. First make the building prefab (see the steps in BuildPart).
        2. Project window: open a folder (e.g. Assets/Building), right click > Create > Building > Building Data.
           Rename the new file, e.g. "InteractionMachine".
        3. Click the file and fill in the Inspector:
           Display Name = the name on the build button, e.g. "Interaction Machine".
           Description  = what it does (shown in the blueprint panel).
           Icon         = leave empty for now (later: drag a Sprite here and it is always used).
           Auto Icon    = ticked: with no Icon, a photo of the prefab is taken automatically. Unticked: name only.
           Prefab       = drag the building PREFAB from the Project window.
        4. Hierarchy: select the BuildMenu Canvas > BuildMenu script > Buildings > click +,
           then drag this Building Data FILE into the new slot (the prefab itself does NOT fit there).
Purpose: One thing the player can build. The recipe is NOT written by hand: it is read from the prefab's
         BuildPart shapes, so making a new machine = building it from shapes.
*/

using UnityEngine;

// CreateAssetMenu adds this to the right click > Create menu in the Project window.
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    // ===== Fields =====
    [Tooltip("Name shown to the player. Leave empty to use the asset's file name.")]
    [SerializeField] string displayName = "";

    [TextArea(2, 5)]
    [Tooltip("What it does, shown in the blueprint panel.")]
    [SerializeField] string description = "";

    [Tooltip("Optional picture for the build panel. Set one here and it is always used.")]
    [SerializeField] Sprite icon;

    [Tooltip("Ticked: with no Icon set above, a photo of the prefab is taken automatically (IconCapture). " +
             "Unticked: no picture, only the name.")]
    [SerializeField] bool autoIcon = true;

    [Tooltip("The finished building, made from shapes with BuildPart on each piece.")]
    [SerializeField] GameObject prefab;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public GameObject Prefab => prefab;

    // ===== My methods =====

    // A photo of the first piece (BuildPart) in the prefab that needs this item, e.g. one stone.
    // Taken from the prefab, so it has its real colors (the blueprint's pieces are blue). Null if none needs it.
    public Sprite GetPartIcon(string itemId)
    {
        if (prefab == null) return null;

        foreach (BuildPart part in prefab.GetComponentsInChildren<BuildPart>(true))
        {
            if (part.ItemId == itemId) return IconCapture.GetIcon("Part|" + itemId, part.gameObject);
        }
        return null;
    }

    // The picture to show: the Icon set above, else an automatic photo of the prefab if Auto Icon is ticked
    // (taken once, then remembered), else none.
    public Sprite GetIcon()
    {
        if (icon != null) return icon;
        if (!autoIcon || prefab == null) return null;
        return IconCapture.GetIcon("Building|" + name, prefab);
    }
}
