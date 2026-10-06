/*
Date: 2/10/2026
Author: Tony
Use by: Nothing to set up. ItemDropper adds this to items the player drops.
Purpose: Remembers when an item was dropped, so AutoCollector waits a moment before collecting it again
         (otherwise the ball would collect it back the instant it lands).
*/

using UnityEngine;

[DisallowMultipleComponent]
public class DroppedItem : MonoBehaviour
{
    // ===== Fields =====
    float readyTime;   // the game time when it can be auto-collected again

    public bool CanAutoCollect => Time.time >= readyTime;

    // ===== My methods =====

    public void Setup(float delay)
    {
        readyTime = Time.time + delay;
    }
}
