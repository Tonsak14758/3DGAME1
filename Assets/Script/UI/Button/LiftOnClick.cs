/*
Date: 29/9/2026
Author: Tony
Use by: Add this script to component on inspector, only work with button.
Purpose: Left click and right click move the button up or down.
         Tick "Left Up" for left = up, right = down.
         Untick "Left Up" for left = down, right = up.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LiftOnClick : MonoBehaviour, IPointerClickHandler
{
    // ===== Fields =====
    [Header("Movement")]                        // bold title above a group of fields
    
    [Range(0f, 100f)]                           // shows a slider and limits the value to 0–100
    [Tooltip("How far the button moves each time you left click.")]
    [SerializeField] float leftLiftAmount = 10f;

    [Space(3)]

    [Range(0f, 100f)]
    [Tooltip("How far the button moves each time you right click.")]
    [SerializeField] float rightLiftAmount = 10f;

    [Space(10)]                                 // adds 10 pixels of empty space above
    [Tooltip("Ticked: left click moves up, right click moves down.\nUnticked: left click moves down, right click moves up.")]
    [SerializeField] bool leftUp = true;

    RectTransform rectTransform;     // UI objects use RectTransform instead of Transform
    Button button;

    // ===== Unity messages (Unity calls these) =====
    void Awake()
    {
        // Cache the components once (lesson 3).
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
    }

    // Left click (and keyboard/gamepad Submit) comes through the Button's onClick.
    void OnEnable()  { button.onClick.AddListener(LeftClick); }
    void OnDisable() { button.onClick.RemoveListener(LeftClick); }


    // ===== Interface methods (EventSystem calls these) =====
    // The EventSystem calls this for EVERY mouse button, so we check which one it was.
    // (Button.onClick only reacts to the left button, so right clicks need this.)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!button.interactable) return; // respect the Interactable checkbox

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClick();
        }
    }

    // ===== My methods =====
    void LeftClick()
    {
        float direction = leftUp ? 1f : -1f;   // up if leftUp, otherwise down
        MoveBy(leftLiftAmount * direction);
    }

    void RightClick()
    {
        float direction = leftUp ? -1f : 1f;   // the opposite of the left click
        MoveBy(rightLiftAmount * direction);
    }

    // Optional: flip the directions from another button's On Click () list.
    public void ToggleDirection() => leftUp = !leftUp;

    void MoveBy(float amount)
    {
        // anchoredPosition is the same as Pos X / Pos Y in the Inspector.
        rectTransform.anchoredPosition += new Vector2(0f, amount);
    }
}