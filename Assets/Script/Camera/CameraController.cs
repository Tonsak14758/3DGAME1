/*
First Date: 29/9/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the Main Camera, then drag the Player sphere into the "Player" slot.
Purpose: Move the mouse to turn the camera.
         Press F5 to switch between third person (orbit around the ball) and first person (view from the ball).
         Press Esc to show the cursor again, click in the Game view to hide it.
         (These keys can be changed in KeyBindings.)
*/
 
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
 
public class CameraController : MonoBehaviour
{
    // ===== Fields =====
    [Header("Target")]
    [Tooltip("The object the camera follows (drag the Player sphere here).")]
    [SerializeField] Transform player;
 
    [Header("Mouse")]
    [Tooltip("How fast the camera turns when you move the mouse.")]
    [Range(0.1f, 10f)]
    [SerializeField] float sensitivity = 3f;
 
    [Tooltip("Ticked: moving the mouse up makes the camera look down.")]
    [SerializeField] bool invertY = false;
 
    [Header("Third person")]
    [Tooltip("Lowest angle: 0 = level with the ball.")]
    [Range(-89f, 89f)]
    [SerializeField] float minPitch = 10f;
 
    [Tooltip("Highest angle: 89 = almost straight down on the ball.")]
    [Range(-89f, 89f)]
    [SerializeField] float maxPitch = 80f;
 
    [Header("First person")]
    [Tooltip("Start the game in first person.")]
    [SerializeField] bool firstPerson = false;
 
    [Tooltip("How high above the ball's center the eyes are. 0.5 = top of a normal sphere.")]
    [SerializeField] float eyeHeight = 0.5f;
 
    [Tooltip("How far you can look down (negative = up, positive = down).")]
    [Range(-89f, 89f)]
    [SerializeField] float fpsMinPitch = -80f;
 
    [Tooltip("How far you can look down.")]
    [Range(-89f, 89f)]
    [SerializeField] float fpsMaxPitch = 80f;
 
    float yaw;        // left/right angle
    float pitch;      // up/down angle
    float distance;   // third person: how far the camera stays from the ball
 
    // ===== Unity messages (Unity calls these) =====
 
    void Start()
    {
        // Keep the same distance and angle the camera had when the game started.
        distance = (transform.position - player.position).magnitude;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;   // e.g. 350° really means -10°
 
        LockCursor(true);
    }
 
    void Update()
    {
        // A menu controls the cursor while it's open.
        if (MenuState.AnyOpen) return;
 
        if (F5Pressed()) ToggleView();
 
        // Esc frees the cursor (e.g. to click UI). Clicking hides it again.
        if (EscapePressed()) LockCursor(false);
        else if (ClickPressed()) LockCursor(true);
    }
 
    // LateUpdate runs after the ball has moved this frame.
    void LateUpdate()
    {
        // Only turn the camera while the cursor is hidden and no menu is open.
        if (Cursor.lockState == CursorLockMode.Locked && !MenuState.AnyOpen)
        {
            Vector2 mouse = ReadMouseDelta();
            yaw += mouse.x * sensitivity;
            pitch += (invertY ? mouse.y : -mouse.y) * sensitivity;
        }
 
        // Each view has its own up/down limits.
        float min = firstPerson ? fpsMinPitch : minPitch;
        float max = firstPerson ? fpsMaxPitch : maxPitch;
        pitch = Mathf.Clamp(pitch, min, max);
 
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
 
        if (firstPerson)
        {
            // Sit on top of the ball. Only the position follows, so the view doesn't spin as it rolls.
            transform.position = player.position + Vector3.up * eyeHeight;
        }
        else
        {
            // Stand at the ball, then step back along the direction the camera is facing.
            transform.position = player.position - rotation * Vector3.forward * distance;
        }
    }
 
    // ===== My methods =====
 
    // Switches between first and third person. Also usable from a button's On Click () list.
    public void ToggleView()
    {
        firstPerson = !firstPerson;
    }
 
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
 
    Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.delta.ReadValue() * 0.1f;   // scaled to match the old system
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }
 
    // The keys below are chosen in KeyBindings.
    bool F5Pressed()
    {
        return Input.GetKeyDown(KeyBindings.ToggleView);
    }
 
    bool EscapePressed()
    {
        return Input.GetKeyDown(KeyBindings.UnlockCursor);
    }
 
    bool ClickPressed()
    {
        return Input.GetKeyDown(KeyBindings.LockCursor);   // KeyCode.Mouse0 = left mouse button
    }
}