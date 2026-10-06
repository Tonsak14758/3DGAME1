/*
Date: 4/10/2026
Author: Tony
Use by: TEST script. Drag it onto any object with a collider (e.g. a cube) and hit it with a tool (ToolWielder).
        Watch the Console: every hit prints the damage, cut or blunt, the speed, and the HP left.
Purpose: A simple target to feel and balance tool hits before real rocks, trees and creatures exist.
         Later this becomes Breakable (rocks drop pieces, trees fall, creatures die).
*/

using UnityEngine;

[DisallowMultipleComponent]
public class HitTarget : MonoBehaviour
{
    // ===== Fields =====
    [Min(1f)]
    [Tooltip("Health of the target. At 0 it breaks.")]
    [SerializeField] float maxHp = 100f;

    [Tooltip("Destroy the object when HP reaches 0. Off = HP just fills up again (for endless testing).")]
    [SerializeField] bool destroyAtZero = false;

    float hp;

    public float Hp => hp;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        hp = maxHp;
    }

    // ===== My methods =====

    // Called by ToolWielder when a tool hits this object.
    public void TakeHit(string toolName, float damage, bool isCut, float speed)
    {
        hp = Mathf.Max(0f, hp - damage);

        Debug.Log(string.Format("HitTarget.cs -> {0} hit {1}: {2:F1} {3} damage (speed {4:F1} m/s). HP {5:F1} / {6:F0}",
                                toolName, name, damage, isCut ? "CUT" : "blunt", speed, hp, maxHp));

        if (hp > 0f) return;

        Debug.Log(name + " broke!");
        if (destroyAtZero) Destroy(gameObject);
        else hp = maxHp;   // fill up again for more testing
    }
}
