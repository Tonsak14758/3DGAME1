/*
Date: 30/9/2026
Author: Tony
Use by: Drag script to the Main Camera, drag target object to the object slot 
        in inspector.
Purpose: Main Camera will follow target

*/
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // ===== Field =====
    [Tooltip("The object that camera will follow.")]
    [SerializeField] Transform target;
    [Tooltip("Set how far of camera and target.")]
    [SerializeField] Vector3 offset = new Vector3(0f, 5f, -8f);
    [Range(0,100)]
    [Tooltip("Smooth speed of camera.")]
    [SerializeField] float smoothSpeed = 5f;
    [Tooltip("On true set camera to look at target.")]
    [SerializeField] bool lookAtTarget = true;


    // LateUpdate is Like Update, but runs after all Updates. 
    // The cube moves first, then the camera catches up, which prevents jittering.
    void LateUpdate()
    {
        // Safety check: do nothing if no target is assigned
        if (target == null) return;
 
        // 1. Where the camera WANTS to be
        Vector3 desiredPosition = target.position + offset;
 
        // 2. Glide smoothly from where it is now towards that spot
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // 3. Turn to face the target
        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
 
        // 4. Press C to print the camera's position
        if (Input.GetKeyDown(KeyCode.C))
        {
            Vector3 pos = transform.position;
            Debug.Log("Camera x: " + pos.x + "  y: " + pos.y + "  z: " + pos.z);
        }
    }
    
}
