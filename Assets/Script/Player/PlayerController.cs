/*
First Date: 29/9/2026
Date: 3/10/2026
Author: Tony
Use by: Drag this script onto the sphere. A Rigidbody is added automatically.
Purpose: Move the sphere with W A S D (or the arrow keys, both set in KeyBindings), relative to the camera:
         W always rolls the ball the way the camera is facing.
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]   // adds a Rigidbody for you
public class PlayerController : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("How strongly the ball is pushed. Higher = faster.")]
    [Range(0f, 50f)]
    [SerializeField] float speed = 10f;

    [Tooltip("W moves the way this camera faces. Leave empty to use the Main Camera.")]
    [SerializeField] Transform cameraTransform;

    Rigidbody rb;          // the physics component that moves the ball
    Vector2 moveInput;     // x = left/right (A/D), y = forward/back (W/S)

    // True while the player is pressing a move key (PlayerHunger uses it: walking burns more food).
    public bool IsMoving => moveInput.sqrMagnitude > 0f;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();   // cache it once

        // No camera dragged in? Use the one tagged "MainCamera".
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Update runs every frame: the best place to READ input.
    void Update()
    {
        // Keys pressed in the key binding menu shouldn't roll the ball.
        moveInput = KeyBindMenu.IsOpen ? Vector2.zero : ReadMoveInput();
    }

    // FixedUpdate runs at the physics rate: the best place to PUSH physics objects.
    void FixedUpdate()
    {
        // Default: W = world forward. With a camera: W = the camera's forward.
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            forward.y = 0f;          // ignore that the camera looks down at the ball
            forward.Normalize();     // make its length 1 again

            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }

        Vector3 movement = forward * moveInput.y + right * moveInput.x;
        rb.AddForce(movement * speed);
    }

    // ===== My methods =====

    Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        // The keys are chosen in KeyBindings. Each direction has a main key and a second key.
        if (Held(KeyBindings.MoveForward, KeyBindings.MoveForwardAlt)) input.y += 1f;
        if (Held(KeyBindings.MoveBack,    KeyBindings.MoveBackAlt))    input.y -= 1f;
        if (Held(KeyBindings.MoveRight,   KeyBindings.MoveRightAlt))   input.x += 1f;
        if (Held(KeyBindings.MoveLeft,    KeyBindings.MoveLeftAlt))    input.x -= 1f;

        // Stop diagonal movement (e.g. W + D) from being faster than straight movement.
        return Vector2.ClampMagnitude(input, 1f);
    }

    // True while either of the two keys is held down.
    bool Held(KeyCode key, KeyCode altKey)
    {
        return Input.GetKey(key) || Input.GetKey(altKey);
    }
}