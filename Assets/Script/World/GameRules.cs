/*
Date: 4/10/2026
Author: Tony
Use by: Create an empty GameObject called "GameRules" in the scene and add this script.
        Tick / untick the rules in the Inspector (also works while playing).
        (If you forget to add this script, one is created by itself with the default rules.)
Purpose: On / off rules for the world, like Minecraft's /gamerule.
         Other scripts ask for a rule like this: if (GameRules.DoDaylightCycle) ...
         - DoDaylightCycle (default on): time of day moves (DaylightCycle). Off = time stops.
         - WorldInteraction (default off): the ball can grab / levitate anywhere,
           without being inside an InteractionMachine's radius.
         Adding a rule = new GameRule entry + bool field + a case in Get and Set + static property.
*/

using UnityEngine;

// Every rule. A future rules menu can show one row per entry, in this order.
public enum GameRule
{
    DoDaylightCycle,
    WorldInteraction,
}

[DisallowMultipleComponent]   // only one GameRules per scene
public class GameRules : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("On: the time of day moves (sun goes around). Off: time stops where it is.")]
    [SerializeField] bool doDaylightCycle = true;

    [Tooltip("On: a body with no hands (the ball, simple shapes) can grab and levitate anywhere. " +
             "Off: only inside an InteractionMachine's radius.")]
    [SerializeField] bool worldInteraction = false;

    static GameRules instance;   // the one GameRules in the scene

    // Other scripts subscribe to this to be told when any rule changes.
    public static event System.Action Changed;

    // Other scripts read the rules through these, e.g. GameRules.WorldInteraction
    public static bool DoDaylightCycle  => Get(GameRule.DoDaylightCycle);
    public static bool WorldInteraction => Get(GameRule.WorldInteraction);

    // Finds the GameRules in the scene. If there isn't one, makes one with the default rules.
    static GameRules Current
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GameRules").AddComponent<GameRules>();
            }
            return instance;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // A second GameRules would be confusing (which rules win?), so remove it.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("There is more than one GameRules in the scene. Removing the extra one.");
            Destroy(this);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance != this) return;

        instance = null;   // static values outlive the scene, so clear them
        Changed = null;
    }

    // Unity calls this when a box is ticked in the Inspector, so listening scripts update while playing.
    void OnValidate()
    {
        if (Application.isPlaying) Changed?.Invoke();
    }

    // ===== My methods =====

    // The value of a rule right now.
    public static bool Get(GameRule rule)
    {
        GameRules r = Current;
        switch (rule)
        {
            case GameRule.DoDaylightCycle:  return r.doDaylightCycle;
            case GameRule.WorldInteraction: return r.worldInteraction;
        }
        return false;   // a new GameRule with no field yet
    }

    // Changes a rule (e.g. from a future rules menu or cheat command).
    public static void Set(GameRule rule, bool value)
    {
        GameRules r = Current;
        switch (rule)
        {
            case GameRule.DoDaylightCycle:  r.doDaylightCycle = value;  break;
            case GameRule.WorldInteraction: r.worldInteraction = value; break;
        }

        Changed?.Invoke();   // tell every listening script (if there are any)
    }
}
