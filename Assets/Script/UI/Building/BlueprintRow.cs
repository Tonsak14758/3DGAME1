/*
Date: 5/10/2026
Author: Tony
Use by: This goes on the ROW PREFAB that BlueprintPanel copies once per material. Prefab when done:
          BlueprintRow                 <- THIS SCRIPT + Horizontal Layout Group
              Icon (Image)             -> "Icon"
              Label (Text - TMP)       -> "Label"
              AddButton (Button - TMP)     -> "Add Button"
              RemoveButton (Button - TMP)  -> "Remove Button"

        1. Build it inside a Canvas first (e.g. inside BlueprintPanel > Rows, so you can see it):
           right click Rows > Create Empty, rename it "BlueprintRow". Width 560, Height 60.
           Drag this script (BlueprintRow.cs) onto it.
           Add Component > Horizontal Layout Group: Child Alignment = Middle Left, Spacing 10,
           untick Control Child Size (Width, Height) and Child Force Expand (Width, Height).
           (Without the layout group all children sit on top of each other and hide the icon.)
        2. Right click BlueprintRow > UI > Image, rename it "Icon". Width 50, Height 50,
           tick Preserve Aspect, untick Raycast Target. Leave Source Image empty: the script puts the picture in.
        3. Right click BlueprintRow > UI > Text - TextMeshPro, rename it "Label". Width 250, Height 50,
           Font Size 20, Alignment left + middle, a color you can read on the panel.
        4. Right click BlueprintRow > UI > Button - TextMeshPro, rename it "AddButton", Width 80, Height 40,
           and change its Text (TMP) child to "Add".
           Again: UI > Button - TextMeshPro, rename it "RemoveButton", Width 100, Height 40, text "Subtract".
        5. Select BlueprintRow and drag into this script's fields:
           Icon <- Icon,  Label <- Label,  Add Button <- AddButton,  Remove Button <- RemoveButton.
           The FIELD decides what a button does, not its name or text.
           Leave both buttons' On Click () lists EMPTY: BlueprintPanel connects them when it makes each row.
        6. Drag BlueprintRow from the Hierarchy into the Project window (it becomes a prefab),
           then DELETE it from the Hierarchy, and drag the prefab into BlueprintPanel's "Row Prefab".
           Later changes: double-click the prefab in the Project window, edit, then Save (Ctrl+S).
Purpose: One line of a blueprint's recipe: "Stone Cube   added 1 / 4   (have 12)" with Add and Remove buttons.
*/

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BlueprintRow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Shows the material item's picture (e.g. the stone).")]
    [SerializeField] Image icon;

    [Tooltip("Ticked: show a picture: the item's inventory icon, or, if the player has none, " +
             "a photo of that piece (IconCapture). Unticked: no picture.")]
    [SerializeField] bool autoIcon = true;

    [Tooltip("Shows the item's name and the counts.")]
    [SerializeField] TMP_Text label;

    [Tooltip("Adds one of this item to the blueprint.")]
    [SerializeField] Button addButton;

    [Tooltip("Takes one added piece of this item back out (it goes back to the inventory).")]
    [SerializeField] Button removeButton;

    [Tooltip("Color of the text while the player has enough of this item.")]
    [SerializeField] Color enoughColor = Color.white;

    [Tooltip("Color of the text while the player doesn't have enough.")]
    [SerializeField] Color notEnoughColor = new Color(1f, 0.45f, 0.45f);

    public bool AutoIcon => autoIcon;   // BlueprintPanel only looks for a picture when this is ticked

    // ===== My methods =====

    // Called once when the row is made: what the Add and Remove buttons do.
    public void Setup(UnityAction onAdd, UnityAction onRemove)
    {
        if (addButton != null) addButton.onClick.AddListener(onAdd);
        if (removeButton != null) removeButton.onClick.AddListener(onRemove);
    }

    // Updates the row. have = in the inventory, need / added = pieces of the blueprint.
    public void Show(string itemName, Sprite sprite, int have, int need, int added)
    {
        int stillNeeded = need - added;
        bool done = stillNeeded <= 0;

        if (label != null)
        {
            label.text = done
                ? itemName + "   " + added + " / " + need + "   (done)"
                : itemName + "   " + added + " / " + need + "   (have " + have + ")";
            label.color = done || have >= stillNeeded ? enoughColor : notEnoughColor;
        }

        if (icon != null)
        {
            icon.sprite = autoIcon ? sprite : null;
            icon.enabled = icon.sprite != null;   // no picture: hide the empty white box
        }

        if (addButton != null) addButton.interactable = !done && have > 0;   // something to add, and room for it
        if (removeButton != null) removeButton.interactable = added > 0;    // something to take out
    }
}
