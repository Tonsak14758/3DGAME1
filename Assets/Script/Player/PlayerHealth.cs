/*
Date: 3/10/2026
Author: Tony
Use by: Drag this script onto the Player sphere. Drag HealthBar (with StatBar on it) into "Bar".
        Testing: in Play mode, change "Health" in the Inspector, or right-click the component's
        title and choose "Test: Take Damage" / "Test: Heal" (uses "Test Amount").
Purpose: Holds the player's health. TakeDamage / Heal change it and update the health bar.
         At 0 health the player dies (only a log for now).
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    // ===== Fields =====
    [Header("Health")]
    [Tooltip("Most health the player can have.")]
    [Min(1f)]
    [SerializeField] float maxHealth = 100f;

    [Tooltip("Health right now. Change it in Play mode to test the bar.")]
    [SerializeField] float health = 100f;

    [Header("UI")]
    [Tooltip("The bar that shows health (HealthBar with a StatBar on it).")]
    [SerializeField] StatBar bar;

    [Header("Testing")]
    [Tooltip("How much the right-click test menu damages / heals.")]
    [Min(0f)]
    [SerializeField] float testAmount = 10f;

    public float Health => health;         // other scripts can read but not set it
    public float MaxHealth => maxHealth;
    public bool IsDead => health <= 0f;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        UpdateBar();   // show the starting health
    }

    // Unity calls this when a value is changed in the Inspector, so the bar follows while testing.
    void OnValidate()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);   // keep it between 0 and max
        UpdateBar();
    }

    // ===== My methods =====

    // Lose health. Negative amounts are ignored (use Heal for that).
    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        health = Mathf.Max(health - amount, 0f);   // never below 0
        UpdateBar();

        if (IsDead)
        {
            Die();
        }
    }

    // Get health back, never above max.
    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        health = Mathf.Min(health + amount, maxHealth);
        UpdateBar();
    }

    void UpdateBar()
    {
        if (bar != null) bar.SetValue(health, maxHealth);
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died.");   // TODO: real death / respawn
    }

    // Right-click the component title in the Inspector to run these (Play mode).
    [ContextMenu("Test: Take Damage")]
    void TestTakeDamage()
    {
        TakeDamage(testAmount);
    }

    [ContextMenu("Test: Heal")]
    void TestHeal()
    {
        Heal(testAmount);
    }
}
