/*
Date: 5/10/2026
Author: Tony
Use by: 1. Hierarchy: right click your HUD Canvas > UI > Text - TextMeshPro, rename it "ScreenMessage".
           (To show it on top of open panels too, put it on the canvas with the highest Sort Order instead.)
        2. Rect Transform: anchor preset middle-center, Pos Y 80 (a bit above the crosshair), Width 600, Height 50.
        3. TextMeshPro: delete the text "New Text" (leave it empty), Font Size 24, Alignment center + middle.
        4. Add Component > ScreenMessage (this script). Only one in the scene.
        5. Optional Inspector settings: Show Seconds (how long it stays), Fade Seconds (how long it fades).
Purpose: Short warnings on screen that fade away, e.g. "No Interaction Machine in range".
         Any script can call: ScreenMessage.Show("text");
         (If there is no ScreenMessage in the scene, the text goes to the Console instead.)
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class ScreenMessage : MonoBehaviour
{
    // ===== Fields =====
    [Range(0.5f, 10f)]
    [Tooltip("Seconds the message stays fully visible.")]
    [SerializeField] float showSeconds = 2f;

    [Range(0.1f, 3f)]
    [Tooltip("Seconds it takes to fade away after that.")]
    [SerializeField] float fadeSeconds = 0.5f;

    static ScreenMessage instance;   // the one in the scene

    TMP_Text text;
    float hideTime;                  // when the message is fully gone

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        instance = this;
        text = GetComponent<TMP_Text>();
        text.raycastTarget = false;   // never block clicks on other UI
        text.alpha = 0f;              // start hidden
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear it
    }

    void Update()
    {
        // unscaledTime: keeps working even if the game is paused (Time.timeScale = 0).
        float left = hideTime - Time.unscaledTime;
        text.alpha = Mathf.Clamp01(left / fadeSeconds);   // 1 until the last fadeSeconds, then down to 0
    }

    // ===== My methods =====

    public static void Show(string message)
    {
        if (instance == null)
        {
            Debug.Log(message);   // no ScreenMessage in the scene
            return;
        }

        instance.text.text = message;
        instance.hideTime = Time.unscaledTime + instance.showSeconds + instance.fadeSeconds;
    }
}
