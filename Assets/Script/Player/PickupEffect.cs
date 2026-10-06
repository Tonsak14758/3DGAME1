/*
Date: 4/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (next to ItemGrabber).
        Drag a Particle System prefab into "Sparkles Prefab" (the levitation sparkles prefab works well).
        The script turns the prefab's own emission off and its Simulation Space to World by itself.
Purpose: When an item is grabbed with F (ItemGrabber.Grabbed), a burst of sparkles pops out where the item was,
         then flies into the player, like the item being sucked in.
         Only for the F grab (AutoCollector pickups show nothing).
         How the flying works: every frame we read each live sparkle, turn its speed a bit toward the player,
         and remove it when it reaches the ball.
*/

using UnityEngine;

public class PickupEffect : MonoBehaviour
{
    // ===== Fields =====
    [Header("Sparkles")]
    [Tooltip("Particle System prefab used for the burst. Empty = no effect.")]
    [SerializeField] ParticleSystem sparklesPrefab;

    [Range(1, 200)]
    [Tooltip("How many sparkles pop out per pickup.")]
    [SerializeField] int burstCount = 30;

    [Header("Fly to player")]
    [Range(0f, 2f)]
    [Tooltip("Seconds the sparkles just pop and float before flying to the player.")]
    [SerializeField] float flyDelay = 0.25f;

    [Range(1f, 30f)]
    [Tooltip("How fast the sparkles fly to the player.")]
    [SerializeField] float flySpeed = 8f;

    [Range(1f, 30f)]
    [Tooltip("How quickly the sparkles turn toward the player (bigger = sharper turn, smaller = wider curve).")]
    [SerializeField] float turnStrength = 6f;

    [Range(0.05f, 2f)]
    [Tooltip("Sparkles closer than this to the player's center disappear (about the ball's radius).")]
    [SerializeField] float catchRadius = 0.5f;

    ParticleSystem sparkles;           // our one copy of the prefab, reused for every pickup
    ParticleSystem.Particle[] buffer;  // reused every frame, so we don't make garbage

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (sparklesPrefab == null) return;

        sparkles = Instantiate(sparklesPrefab);   // no parent, so sparkles don't move with the ball

        // World = sparkles already flying stay where they are when the system moves to the next pickup.
        ParticleSystem.MainModule main = sparkles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // No sparkles by themselves: only the bursts we Emit() ourselves.
        ParticleSystem.EmissionModule emission = sparkles.emission;
        emission.enabled = false;
        sparkles.Play();   // must be playing, so emitted sparkles move and fade

        buffer = new ParticleSystem.Particle[main.maxParticles];
    }

    void OnEnable()
    {
        ItemGrabber.Grabbed += OnGrabbed;
    }

    void OnDisable()
    {
        ItemGrabber.Grabbed -= OnGrabbed;
    }

    void OnDestroy()
    {
        if (sparkles != null) Destroy(sparkles.gameObject);
    }

    // LateUpdate runs after the ball has moved this frame, so the sparkles aim at where it really is.
    void LateUpdate()
    {
        if (sparkles == null || sparkles.particleCount == 0) return;   // nothing flying: no work

        PullToPlayer();
    }

    // ===== My methods =====

    void OnGrabbed(Bounds itemBounds)
    {
        if (sparkles == null) return;

        // Pop the sparkles out of a box the size of the item, at the real middle of its meshes.
        sparkles.transform.position = itemBounds.center;
        ParticleSystem.ShapeModule shape = sparkles.shape;
        shape.scale = itemBounds.size;

        sparkles.Emit(burstCount);
    }

    // Turns every sparkle toward the player, and removes the ones that arrived.
    void PullToPlayer()
    {
        int count = sparkles.GetParticles(buffer);   // copy the live sparkles into our buffer
        Vector3 player = transform.position;
        float dt = Time.deltaTime;

        for (int i = 0; i < count; i++)
        {
            float age = buffer[i].startLifetime - buffer[i].remainingLifetime;
            if (age < flyDelay) continue;   // still popping out

            Vector3 toPlayer = player - buffer[i].position;
            if (toPlayer.magnitude < catchRadius)
            {
                buffer[i].remainingLifetime = 0f;   // arrived: 0 life left = removed
                continue;
            }

            // Turn the speed a bit toward the player each frame (Lerp), so the sparkles curve in smoothly.
            Vector3 wanted = toPlayer.normalized * flySpeed;
            buffer[i].velocity = Vector3.Lerp(buffer[i].velocity, wanted, turnStrength * dt);
        }

        sparkles.SetParticles(buffer, count);   // put the changed sparkles back
    }
}
