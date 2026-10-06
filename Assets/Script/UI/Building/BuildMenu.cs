/*
Date: 5/10/2026
Author: Tony
Use by: Hierarchy when done:
          BuildMenu (Canvas)          <- THIS SCRIPT (on the Canvas, NOT on the panel)
              MenuPanel (Panel)       -> "Menu Panel"
                  Buttons (Empty)     -> "Buttons Parent"   (leave it empty: buttons are made here when the game starts)

        1. Hierarchy: right click > UI > Canvas, rename it "BuildMenu".
           (If Unity also made a second EventSystem, delete it: keep only one in the scene.)
           Drag this script (BuildMenu.cs) from the Project window onto the BuildMenu Canvas.
        2. Right click BuildMenu > UI > Panel, rename it "MenuPanel".
        3. Right click MenuPanel > Create Empty, rename it "Buttons".
           Rect Transform: click the anchor square, hold Alt and click the bottom-right preset (stretch both ways).
           Add Component > Grid Layout Group, Cell Size 120 x 140, Spacing 10 x 10.
        4. The BUTTON PREFAB (one button, the script copies it for every building):
           a. Right click Buttons > UI > Button - TextMeshPro, rename it "BuildButton".
              Rect Transform: Pos X 0, Pos Y 0, Width 120, Height 140.
           b. Right click BuildButton > UI > Image, rename it EXACTLY "Icon" (the script finds it by this name).
              Anchor preset top-center, Pos Y -10, Width 100, Height 100,
              Image: tick Preserve Aspect, untick Raycast Target (so it doesn't catch the click).
           c. Its "Text (TMP)" child: anchor preset bottom-stretch, Height 30, Font Size 16, Alignment center.
              Keep only this ONE TMP text in the button (the script writes the building's name into it).
           d. Drag BuildButton from the Hierarchy into the Project window (e.g. Assets/Prefab): it becomes a prefab.
              Then DELETE it from the Hierarchy (a button left there shows as an extra empty button).
        5. Select the BuildMenu Canvas and fill in this script's fields:
           Menu Panel     <- MenuPanel
           Buttons Parent <- Buttons
           Button Prefab  <- the BuildButton PREFAB from the Project window
           Buildings      <- click +, then drag a Building Data ASSET (see BuildingData), NOT the building prefab.
        6. Play: the menu is hidden. Press B: one button per building. Needs BlueprintPlacer on the PlayerSphere.
Purpose: Press B (set in KeyBindings) to open the build panel. Click a building: the panel closes
         and its blueprint follows the crosshair (see BlueprintPlacer).
         Pressing B while placing cancels the blueprint.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The panel to show and hide.")]
    [SerializeField] GameObject menuPanel;

    [Tooltip("The object the buttons are created under (give it a Layout Group).")]
    [SerializeField] Transform buttonsParent;

    [Tooltip("A Button - TextMeshPro prefab. One is made for every building. A child Image named \"Icon\" shows its picture.")]
    [SerializeField] Button buttonPrefab;

    [Tooltip("Everything the player can build, in the order shown.")]
    [SerializeField] BuildingData[] buildings;

    public static bool IsOpen { get; private set; }   // any script can check BuildMenu.IsOpen

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        BuildButtons();   // Start, not Awake: the automatic icons use IconCapture, which needs the scene ready
        SetMenu(false);   // start with the menu closed
    }

    void Update()
    {
        // Other menus own the keyboard and cursor while they're open.
        if (KeyBindMenu.IsOpen || InventoryManager.IsOpen || BlueprintPanel.IsOpen) return;
        if (!BuildPressed()) return;

        if (BlueprintPlacer.IsPlacing)
        {
            BlueprintPlacer.CancelPlacing();   // B while placing = stop placing
            return;
        }
        SetMenu(!IsOpen);
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
        if (menuPanel != null) menuPanel.SetActive(open);

        // Open: free cursor to click buttons. Closed: hidden cursor for the camera.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void BuildButtons()
    {
        if (buttonsParent == null || buttonPrefab == null)
        {
            Debug.LogWarning("BuildMenu: drag in Buttons Parent and Button Prefab.");
            return;
        }

        foreach (BuildingData data in buildings)
        {
            if (data == null) continue;   // an empty slot in the list

            Button button = Instantiate(buttonPrefab, buttonsParent);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = data.DisplayName;

            Transform iconChild = button.transform.Find("Icon");
            Image icon = iconChild != null ? iconChild.GetComponent<Image>() : null;
            if (icon != null)
            {
                icon.sprite = data.GetIcon();
                icon.enabled = icon.sprite != null;   // no picture: hide the empty white box
            }

            BuildingData chosen = data;   // copy it, so each button remembers its own building
            button.onClick.AddListener(() => Choose(chosen));
        }
    }

    void Choose(BuildingData data)
    {
        SetMenu(false);              // hides the cursor again, so the crosshair aims
        BlueprintPlacer.Begin(data);
    }

    bool BuildPressed()
    {
        return Input.GetKeyDown(KeyBindings.Build);   // the key is chosen in KeyBindings
    }
}
