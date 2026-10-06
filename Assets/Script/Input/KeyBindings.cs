/*
First Date: 2/10/2026
Date: 4/10/2026
Author: Tony
Use by: Create an empty GameObject called "KeyBindings" in the scene and add this script.
        The keys in the Inspector are the DEFAULT keys. The player can change them in game
        with KeyBindMenu, and their choices are saved (PlayerPrefs) for next time.
        (If you forget to add this script, one is created by itself with the default keys.)
Purpose: One place to change every key in the game.
         Other scripts ask for a key like this: Input.GetKeyDown(KeyBindings.Grab)
         Note: KeyCode only works with the old Input Manager
         (Project Settings → Player → Active Input Handling = "Input Manager (Old)" or "Both").
*/

using UnityEngine;

// Every thing the player can do with a key. KeyBindMenu shows one row per entry, in this order.
public enum KeyAction
{
    MoveForward, MoveForwardAlt,
    MoveBack, MoveBackAlt,
    MoveLeft, MoveLeftAlt,
    MoveRight, MoveRightAlt,
    Grab,
    Inventory,
    ToggleView,
    UnlockCursor,
    LockCursor,
    KeyMenu,
    Levitate,
    Swing,
    Build, Interact, PlaceBuilding, CancelBuilding, AutoPlace,
    Hotbar1, Hotbar2, Hotbar3, Hotbar4, Hotbar5, Hotbar6, Hotbar7, Hotbar8,   // keep them in order: HotbarKey() counts from Hotbar1
}

[DisallowMultipleComponent]   // only one KeyBindings per object
public class KeyBindings : MonoBehaviour
{
    // ===== Fields =====
    [Header("Movement (default keys)")]
    [Tooltip("Roll the ball forward (the way the camera faces).")]
    [SerializeField] KeyCode moveForward = KeyCode.W;
    [Tooltip("Second key for forward.")]
    [SerializeField] KeyCode moveForwardAlt = KeyCode.UpArrow;

    [Tooltip("Roll the ball backward.")]
    [SerializeField] KeyCode moveBack = KeyCode.S;
    [Tooltip("Second key for backward.")]
    [SerializeField] KeyCode moveBackAlt = KeyCode.DownArrow;

    [Tooltip("Roll the ball left.")]
    [SerializeField] KeyCode moveLeft = KeyCode.A;
    [Tooltip("Second key for left.")]
    [SerializeField] KeyCode moveLeftAlt = KeyCode.LeftArrow;

    [Tooltip("Roll the ball right.")]
    [SerializeField] KeyCode moveRight = KeyCode.D;
    [Tooltip("Second key for right.")]
    [SerializeField] KeyCode moveRightAlt = KeyCode.RightArrow;

    [Header("Actions (default keys)")]
    [Tooltip("Pick up the item under the crosshair.")]
    [SerializeField] KeyCode grab = KeyCode.F;

    [Tooltip("Open / close the inventory.")]
    [SerializeField] KeyCode inventory = KeyCode.E;

    [Tooltip("Open / close the key binding menu.")]
    [SerializeField] KeyCode keyMenu = KeyCode.K;

    [Tooltip("Hold to levitate the object under the crosshair (only with no hotbar slot selected). Mouse1 = right mouse button.")]
    [SerializeField] KeyCode levitate = KeyCode.Mouse1;

    [Tooltip("Swing the tool in the selected hotbar slot (the swing animation). Mouse0 = left mouse button.")]
    [SerializeField] KeyCode swing = KeyCode.Mouse0;

    [Header("Building (default keys)")]
    [Tooltip("Open / close the build panel.")]
    [SerializeField] KeyCode build = KeyCode.B;

    [Tooltip("Use what the crosshair points at (e.g. open a blueprint). Mouse1 = right mouse button.")]
    [SerializeField] KeyCode interact = KeyCode.Mouse1;

    [Tooltip("Put the blueprint down while placing it. Mouse0 = left mouse button.")]
    [SerializeField] KeyCode placeBuilding = KeyCode.Mouse0;

    [Tooltip("Stop placing the blueprint. Mouse1 = right mouse button.")]
    [SerializeField] KeyCode cancelBuilding = KeyCode.Mouse1;

    [Tooltip("Turn auto placement (sit on the ground) on / off while placing. Mouse2 = middle mouse button.")]
    [SerializeField] KeyCode autoPlace = KeyCode.Mouse2;

    [Header("Camera (default keys)")]
    [Tooltip("Switch between first and third person.")]
    [SerializeField] KeyCode toggleView = KeyCode.F5;

    [Tooltip("Show the cursor (stop aiming).")]
    [SerializeField] KeyCode unlockCursor = KeyCode.Escape;

    [Tooltip("Hide the cursor again (start aiming). Mouse0 = left mouse button.")]
    [SerializeField] KeyCode lockCursor = KeyCode.Mouse0;

    [Header("Hotbar (default keys)")]
    [Tooltip("Select hotbar slot 1, 2, 3 ... (one key per slot, in order).")]
    [SerializeField] KeyCode[] hotbar =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
    };

    static KeyBindings instance;   // the one KeyBindings in the scene

    // The keys being used right now (default or chosen by the player), one per KeyAction.
    KeyCode[] keys;

    // Other scripts subscribe to this to be told when any key changes (e.g. to redraw a menu).
    public static event System.Action Changed;

    // Other scripts read the keys through these, e.g. KeyBindings.Grab
    public static KeyCode MoveForward    => Get(KeyAction.MoveForward);
    public static KeyCode MoveForwardAlt => Get(KeyAction.MoveForwardAlt);
    public static KeyCode MoveBack       => Get(KeyAction.MoveBack);
    public static KeyCode MoveBackAlt    => Get(KeyAction.MoveBackAlt);
    public static KeyCode MoveLeft       => Get(KeyAction.MoveLeft);
    public static KeyCode MoveLeftAlt    => Get(KeyAction.MoveLeftAlt);
    public static KeyCode MoveRight      => Get(KeyAction.MoveRight);
    public static KeyCode MoveRightAlt   => Get(KeyAction.MoveRightAlt);
    public static KeyCode Grab           => Get(KeyAction.Grab);
    public static KeyCode Inventory      => Get(KeyAction.Inventory);
    public static KeyCode KeyMenu        => Get(KeyAction.KeyMenu);
    public static KeyCode Levitate       => Get(KeyAction.Levitate);
    public static KeyCode Swing          => Get(KeyAction.Swing);
    public static KeyCode Build          => Get(KeyAction.Build);
    public static KeyCode Interact       => Get(KeyAction.Interact);
    public static KeyCode PlaceBuilding  => Get(KeyAction.PlaceBuilding);
    public static KeyCode CancelBuilding => Get(KeyAction.CancelBuilding);
    public static KeyCode AutoPlace      => Get(KeyAction.AutoPlace);
    public static KeyCode ToggleView     => Get(KeyAction.ToggleView);
    public static KeyCode UnlockCursor   => Get(KeyAction.UnlockCursor);
    public static KeyCode LockCursor     => Get(KeyAction.LockCursor);

    // The key for hotbar slot i (0 = first slot). Wrong numbers give KeyCode.None.
    public static KeyCode HotbarKey(int i)
    {
        if (i < 0 || i >= HotbarKeyCount) return KeyCode.None;
        return Get(KeyAction.Hotbar1 + i);
    }

    public const int HotbarKeyCount = KeyAction.Hotbar8 - KeyAction.Hotbar1 + 1;   // 8

    // Finds the KeyBindings in the scene. If there isn't one, makes one with the default keys.
    static KeyBindings Current
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("KeyBindings").AddComponent<KeyBindings>();
            }
            return instance;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // A second KeyBindings would be confusing (which keys win?), so remove it.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("There is more than one KeyBindings in the scene. Removing the extra one.");
            Destroy(this);
            return;
        }
        instance = this;

        LoadKeys();
    }

    void OnDestroy()
    {
        if (instance != this) return;

        instance = null;   // static values outlive the scene, so clear them
        Changed = null;
    }

    // ===== My methods =====

    // The key used right now for this action.
    public static KeyCode Get(KeyAction action)
    {
        return Current.keys[(int)action];
    }

    // Gives an action a new key and saves it.
    // If another action already uses that key, the two swap keys, so no key does two things.
    public static void Set(KeyAction action, KeyCode newKey)
    {
        KeyCode[] keys = Current.keys;
        KeyCode oldKey = keys[(int)action];

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] == newKey) keys[i] = oldKey;   // the other action gets our old key
        }
        keys[(int)action] = newKey;

        SaveKeys();
        Changed?.Invoke();   // tell every listening script (if there are any)
    }

    // Puts every key back to the default (the keys set in the Inspector) and forgets the saved ones.
    public static void ResetToDefaults()
    {
        KeyBindings kb = Current;

        for (int i = 0; i < kb.keys.Length; i++)
        {
            kb.keys[i] = kb.DefaultKey((KeyAction)i);
            PlayerPrefs.DeleteKey(SaveName((KeyAction)i));
        }
        PlayerPrefs.Save();

        Changed?.Invoke();
    }

    // Uses the saved key for each action, or the default key if nothing was saved yet.
    void LoadKeys()
    {
        int count = System.Enum.GetValues(typeof(KeyAction)).Length;
        keys = new KeyCode[count];

        for (int i = 0; i < count; i++)
        {
            KeyAction action = (KeyAction)i;
            keys[i] = (KeyCode)PlayerPrefs.GetInt(SaveName(action), (int)DefaultKey(action));
        }
    }

    // Writes every key to PlayerPrefs (stored on the computer, kept after the game closes).
    static void SaveKeys()
    {
        KeyCode[] keys = Current.keys;

        for (int i = 0; i < keys.Length; i++)
        {
            PlayerPrefs.SetInt(SaveName((KeyAction)i), (int)keys[i]);
        }
        PlayerPrefs.Save();
    }

    // The name each key is saved under, e.g. "Key_Grab".
    static string SaveName(KeyAction action)
    {
        return "Key_" + action;
    }

    // The default key for an action = the one set in the Inspector.
    KeyCode DefaultKey(KeyAction action)
    {
        switch (action)
        {
            case KeyAction.MoveForward:    return moveForward;
            case KeyAction.MoveForwardAlt: return moveForwardAlt;
            case KeyAction.MoveBack:       return moveBack;
            case KeyAction.MoveBackAlt:    return moveBackAlt;
            case KeyAction.MoveLeft:       return moveLeft;
            case KeyAction.MoveLeftAlt:    return moveLeftAlt;
            case KeyAction.MoveRight:      return moveRight;
            case KeyAction.MoveRightAlt:   return moveRightAlt;
            case KeyAction.Grab:           return grab;
            case KeyAction.Inventory:      return inventory;
            case KeyAction.KeyMenu:        return keyMenu;
            case KeyAction.Levitate:       return levitate;
            case KeyAction.Swing:          return swing;
            case KeyAction.Build:          return build;
            case KeyAction.Interact:       return interact;
            case KeyAction.PlaceBuilding:  return placeBuilding;
            case KeyAction.CancelBuilding: return cancelBuilding;
            case KeyAction.AutoPlace:      return autoPlace;
            case KeyAction.ToggleView:     return toggleView;
            case KeyAction.UnlockCursor:   return unlockCursor;
            case KeyAction.LockCursor:     return lockCursor;
        }

        // Hotbar1 - Hotbar8 read from the hotbar list (Hotbar1 = hotbar[0]).
        int slot = action - KeyAction.Hotbar1;
        if (slot >= 0 && slot < HotbarKeyCount && hotbar != null && slot < hotbar.Length)
        {
            return hotbar[slot];
        }

        return KeyCode.None;   // a new KeyAction with no default yet
    }
}
