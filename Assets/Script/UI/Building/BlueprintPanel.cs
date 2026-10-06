/*
Date: 5/10/2026
Author: Tony
Use by: Hierarchy when done:
          BlueprintCanvas (Canvas, keep it ACTIVE)  <- THIS SCRIPT (on the Canvas, NOT on the panel)
              BlueprintPanel (Panel)                -> "Panel"   (the script hides / shows this)
                  Title (Text - TMP)                -> "Title Text"
                  Description (Text - TMP)          -> "Description Text"
                  Rows (Empty)                      -> "Rows Parent"   (leave it empty: rows are made here)
                  AddAllButton, RemoveAllButton, RemoveButton, CloseButton (Button - TMP)

        1. Hierarchy: right click > UI > Canvas, rename it "BlueprintCanvas".
           (If Unity also made a second EventSystem, delete it: keep only one in the scene.)
           Canvas component: Sort Order 10 (higher than the HUD / Inventory canvases, so the panel draws on top).
           Drag this script (BlueprintPanel.cs) onto BlueprintCanvas.
           The script must be on the CANVAS: the panel gets switched off, and a switched-off object runs no script.
           Keep BlueprintCanvas itself switched ON (ticked), or right click on a blueprint does nothing.
        2. Right click BlueprintCanvas > UI > Panel, rename it "BlueprintPanel".
        3. Right click BlueprintPanel > UI > Text - TextMeshPro, rename it "Title" (font size ~28, near the top).
           Again: UI > Text - TextMeshPro, rename it "Description" (font size ~18, under the title).
        4. Right click BlueprintPanel > Create Empty, rename it "Rows" (the middle of the panel, e.g. 600 x 300).
           Add Component > Vertical Layout Group: Spacing 5, untick Child Force Expand Width and Height.
        5. Right click BlueprintPanel > UI > Button - TextMeshPro, four times. Rename them and set their text:
           "AddAllButton" (Add All), "RemoveAllButton" (Remove All), "RemoveButton" (Remove Blueprint), "CloseButton" (Close).
        6. Connect each of those four buttons: select it > Button component > On Click () > click +,
           drag BlueprintCanvas (from the Hierarchy, NOT the script file) into the "None (Object)" slot,
           then in the "No Function" list pick:
              AddAllButton    -> BlueprintPanel > AddAll           (every material you have goes in)
              RemoveAllButton -> BlueprintPanel > RemoveAll        (materials back, the blueprint stays)
              RemoveButton    -> BlueprintPanel > RemoveBlueprint  (materials back, the blueprint is deleted)
              CloseButton     -> BlueprintPanel > Close
        7. Make the row prefab (see the steps in BlueprintRow).
        8. Select BlueprintCanvas and fill in this script's fields:
           Panel <- BlueprintPanel,  Title Text <- Title,  Description Text <- Description,
           Rows Parent <- Rows,  Row Prefab <- the BlueprintRow PREFAB from the Project window.
        9. Play: place a blueprint, aim at it and right click: the panel opens (needs BlueprintInteractor on the PlayerSphere).
Purpose: The panel for a placed blueprint (opened by BlueprintInteractor): shows every material it needs,
         how many are added and how many the player has. Add one, Add All, or Remove the blueprint
         (gives the added items back; refused with a warning if the inventory is too full).
         Pressing Interact (right click) again, or the Close button, closes it.
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlueprintPanel : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The panel to show and hide.")]
    [SerializeField] GameObject panel;

    [Tooltip("Shows the building's name.")]
    [SerializeField] TMP_Text titleText;

    [Tooltip("Optional: shows the building's description.")]
    [SerializeField] TMP_Text descriptionText;

    [Tooltip("The object the rows are created under (give it a Vertical Layout Group).")]
    [SerializeField] Transform rowsParent;

    [Tooltip("The row prefab (with BlueprintRow on it). One is made per material.")]
    [SerializeField] BlueprintRow rowPrefab;

    static BlueprintPanel instance;   // the one in the scene

    Blueprint target;                 // the blueprint being shown (null = closed)
    readonly List<BlueprintRow> rows = new List<BlueprintRow>();
    readonly List<string> rowIds = new List<string>();   // which item each row is for
    int openFrame;                    // the right click that opened it must not also close it
    static int closeFrame = -1;       // ...and the right click that closed it must not open it again

    public static bool IsOpen { get; private set; }   // any script can check BlueprintPanel.IsOpen
    public static bool ClosedThisFrame => Time.frameCount == closeFrame;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);   // start closed
    }

    void OnEnable()
    {
        PlayerInventory.Changed += Redraw;   // the "have" counts change when items do
    }

    void OnDisable()
    {
        PlayerInventory.Changed -= Redraw;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear them
        IsOpen = false;
    }

    void Update()
    {
        if (!IsOpen) return;

        // The blueprint was finished (all pieces added) or destroyed: nothing left to show.
        if (target == null)
        {
            Close();
            return;
        }

        if (Time.frameCount != openFrame && ClosePressed()) Close();
    }

    // ===== My methods =====

    // Opens the panel for this blueprint (called by BlueprintInteractor).
    public static void Open(Blueprint bp)
    {
        if (instance == null)
        {
            Debug.LogWarning("BlueprintPanel: add it to a Canvas.");
            return;
        }
        instance.Show(bp);
    }

    void Show(Blueprint bp)
    {
        target = bp;
        target.Changed += Redraw;
        openFrame = Time.frameCount;
        IsOpen = true;

        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = bp.Data.DisplayName;
        if (descriptionText != null) descriptionText.text = bp.Data.Description;

        // Free cursor to click the buttons.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BuildRows();
        Redraw();
    }

    // For the "Close" button's On Click () list.
    public void Close()
    {
        if (target != null) target.Changed -= Redraw;
        target = null;
        IsOpen = false;
        closeFrame = Time.frameCount;
        if (panel != null) panel.SetActive(false);

        // Hidden cursor again, for the camera and crosshair.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // For the "Add All" button's On Click () list.
    public void AddAll()
    {
        if (target == null) return;

        if (target.AddAll() == 0) ScreenMessage.Show("You don't have any of the materials needed.");
        Redraw();
    }

    // For the "Remove All" button's On Click () list: takes every added material back out,
    // but the blueprint stays (all blue again).
    public void RemoveAll()
    {
        if (target == null) return;

        if (!target.AnyAdded)
        {
            ScreenMessage.Show("Nothing has been added yet.");
            return;
        }

        if (!target.TryRemoveAllMaterials(out string problem)) ScreenMessage.Show(problem);
        Redraw();
    }

    // For the "Remove Blueprint" button's On Click () list.
    public void RemoveBlueprint()
    {
        if (target == null) return;

        if (target.TryRemove(out string problem)) Close();
        else ScreenMessage.Show(problem);
    }

    // One row per material in the recipe.
    void BuildRows()
    {
        foreach (BlueprintRow row in rows)
        {
            if (row != null) Destroy(row.gameObject);   // rows of the last blueprint
        }
        rows.Clear();
        rowIds.Clear();

        if (rowsParent == null || rowPrefab == null)
        {
            Debug.LogWarning("BlueprintPanel: drag in Rows Parent and Row Prefab.");
            return;
        }

        foreach (Blueprint.Line line in target.GetLines())
        {
            BlueprintRow row = Instantiate(rowPrefab, rowsParent);
            string id = line.itemId;   // copy it, so each button remembers its own item
            row.Setup(() => AddOne(id), () => RemoveOne(id));

            rows.Add(row);
            rowIds.Add(id);
        }
    }

    void AddOne(string itemId)
    {
        if (target == null) return;

        target.AddOne(itemId);
        Redraw();
    }

    void RemoveOne(string itemId)
    {
        if (target == null) return;

        if (!target.TryRemoveOne(itemId, out string problem)) ScreenMessage.Show(problem);
        Redraw();
    }

    // Updates every row's counts (also called when the inventory changes).
    void Redraw()
    {
        if (target == null) return;   // closed, or the blueprint is gone

        PlayerInventory inv = PlayerInventory.Instance;
        List<Blueprint.Line> lines = target.GetLines();

        for (int i = 0; i < rows.Count && i < lines.Count; i++)
        {
            string id = rowIds[i];
            PlayerInventory.Slot owned = inv != null ? inv.FindAny(id) : null;   // for the name and icon

            rows[i].Show(owned != null ? owned.name : id,
                         RowIcon(rows[i], owned, id),
                         inv != null ? inv.Count(id) : 0,
                         lines[i].need, lines[i].added);
        }
    }

    // The picture for a row: the inventory icon if the player has the item,
    // else a photo of that piece from the building's prefab (taken once, then remembered).
    Sprite RowIcon(BlueprintRow row, PlayerInventory.Slot owned, string itemId)
    {
        if (!row.AutoIcon) return null;
        if (owned != null && owned.icon != null) return owned.icon;
        return target.Data.GetPartIcon(itemId);
    }

    // Right click (Interact) closes the panel, like it opened it.
    bool ClosePressed()
    {
        return Input.GetKeyDown(KeyBindings.Interact);
    }
}
