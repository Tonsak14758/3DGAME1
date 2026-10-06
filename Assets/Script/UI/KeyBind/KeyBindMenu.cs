/*
First Date: 2/10/2026
Date: 5/10/2026
Author: Tony
Use by: 1. Make a Canvas (or use InventoryCanvas) and add this script to it.
        2. Inside it, make a Panel called "KeyBindMenu" → drag it into "Menu Panel".
        3. Inside the panel, make an empty object "Rows" with a Vertical Layout Group
           → drag it into "Rows Parent". (14 rows: add a Scroll View if they don't fit.)
        4. Make one Button - TextMeshPro, drag it into the Project window to make it a prefab,
           delete it from the scene, then drag the prefab into "Row Button Prefab".
        5. Optional: a "Reset" button → On Click () → KeyBindMenu.ResetKeys
                     a "Close" button → On Click () → KeyBindMenu.SetMenu (unticked = close).
Purpose: Press K (set in KeyBindings) to open the key binding menu.
         Click a row, then press the new key. Esc cancels.
         While open: the cursor is shown, the camera stops turning, and the ball doesn't move.
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindMenu : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The panel to show and hide.")]
    [SerializeField] GameObject menuPanel;

    [Tooltip("The object the rows are created under (give it a Vertical Layout Group).")]
    [SerializeField] Transform rowsParent;

    [Tooltip("A Button - TextMeshPro prefab. One is made for every key.")]
    [SerializeField] Button rowButtonPrefab;

    public static bool IsOpen { get; private set; }   // any script can check KeyBindMenu.IsOpen

    TMP_Text[] rowLabels;     // the text on each row, one per KeyAction
    int listeningIndex = -1;  // which row is waiting for a new key (-1 = none)

    static KeyCode[] allKeys; // every KeyCode, found once (asking every frame would waste memory)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (allKeys == null) allKeys = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

        BuildRows();
    }

    void Start()
    {
        SetMenu(false);   // start with the menu closed
    }

    void OnEnable()
    {
        KeyBindings.Changed += Redraw;
    }

    void OnDisable()
    {
        KeyBindings.Changed -= Redraw;
    }

    void Update()
    {
        // Waiting for a new key? Then that is the only thing this script does this frame.
        if (listeningIndex >= 0)
        {
            ListenForKey();
            return;
        }

        // Don't open on top of another menu (both would fight over the cursor).
        bool otherMenuOpen = InventoryManager.IsOpen || BuildMenu.IsOpen || BlueprintPanel.IsOpen;
        if (Input.GetKeyDown(KeyBindings.KeyMenu) && !otherMenuOpen)
        {
            SetMenu(!IsOpen);
        }
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
        listeningIndex = -1;   // closing the menu also cancels waiting for a key
        if (menuPanel != null) menuPanel.SetActive(open);

        // Open: free cursor to click rows. Closed: hidden cursor for the camera.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        Redraw();
    }

    // For a "Reset" button's On Click () list.
    public void ResetKeys()
    {
        listeningIndex = -1;
        KeyBindings.ResetToDefaults();   // this fires KeyBindings.Changed, which redraws the rows
    }

    // Makes one button per KeyAction.
    void BuildRows()
    {
        if (rowsParent == null || rowButtonPrefab == null)
        {
            Debug.LogWarning("KeyBindMenu: drag in Rows Parent and Row Button Prefab.");
            return;
        }

        int count = System.Enum.GetValues(typeof(KeyAction)).Length;
        rowLabels = new TMP_Text[count];

        for (int i = 0; i < count; i++)
        {
            Button row = Instantiate(rowButtonPrefab, rowsParent);
            rowLabels[i] = row.GetComponentInChildren<TMP_Text>();

            int index = i;   // copy it, so each button remembers its own row (not the last i)
            row.onClick.AddListener(() => StartListening(index));
        }
    }

    void StartListening(int index)
    {
        listeningIndex = index;
        Redraw();   // show "Press a key..." on that row
    }

    // Checks every key to find the one pressed this frame.
    void ListenForKey()
    {
        if (!Input.anyKeyDown) return;   // nothing pressed: skip the loop below (cheap)

        if (Input.GetKeyDown(KeyCode.Escape))   // Esc = cancel, keep the old key
        {
            listeningIndex = -1;
            Redraw();
            return;
        }

        foreach (KeyCode key in allKeys)
        {
            if (Input.GetKeyDown(key))
            {
                KeyAction action = (KeyAction)listeningIndex;
                listeningIndex = -1;
                KeyBindings.Set(action, key);   // fires KeyBindings.Changed → Redraw
                return;
            }
        }
    }

    // Writes "Grab: F" (or "Grab: Press a key...") on every row.
    void Redraw()
    {
        if (rowLabels == null) return;   // rows were never built

        for (int i = 0; i < rowLabels.Length; i++)
        {
            if (rowLabels[i] == null) continue;   // the prefab has no text

            KeyAction action = (KeyAction)i;
            string key = i == listeningIndex ? "Press a key... (Esc = cancel)" : KeyBindings.Get(action).ToString();
            rowLabels[i].text = NiceName(action.ToString()) + ":  " + key;
        }
    }

    // "MoveForwardAlt" → "Move Forward Alt"
    static string NiceName(string name)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}
