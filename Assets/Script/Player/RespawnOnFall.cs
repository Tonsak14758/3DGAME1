/*
Date: 29/9/2026
Author: Tony
Use by: Drag this script onto the Player sphere.
Purpose: When the sphere falls to Y = -100 (or lower), it "dies"
         and respawns at the spawn position, standing still.
*/

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RespawnOnFall : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("If the sphere's Y position goes this low or lower, it dies.")]
    [Range(-1f,-250f)]
    [SerializeField] float deathHeight = -10f;

    [Tooltip("Where the sphere comes back after dying.")]
    [SerializeField] Vector3 spawnPosition = new Vector3(0f, 0f, 0f);

    Rigidbody rb;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        rb = GetComponent<Rigidbody>();   // cache it once
    }

    void Update()
    {
        // Check every frame: has the sphere fallen too far?
        if (transform.position.y <= deathHeight)
        {
            Die();
        }
    }

    // ===== My methods =====

    void Die()
    {
        //Debug.Log(gameObject.name + " died. Respawning...");
        Respawn();
    }

    void Respawn()
    {
        // Stop all movement, or the ball keeps its falling speed after respawning.
        rb.velocity = Vector3.zero;          // moving speed
        rb.angularVelocity = Vector3.zero;   // rolling / spinning speed

        // Move the ball back to the spawn point, upright.
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
    }
}