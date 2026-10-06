/*
Date: 4/10/2026
Author: Tony
Use by: Nothing to set up. ToolWielder adds it to the floating tool by itself.
Purpose: Unity only sends collision messages to scripts on the object that got hit (the tool),
         so this small script passes them on to the player's ToolWielder.
*/

using UnityEngine;

public class ToolHitReporter : MonoBehaviour
{
    // ===== Fields =====
    ToolWielder owner;   // who to tell

    // ===== Unity messages (Unity calls these) =====

    void OnCollisionEnter(Collision collision)
    {
        if (owner != null) owner.OnToolCollision(collision);
    }

    // ===== My methods =====

    public void Setup(ToolWielder wielder)
    {
        owner = wielder;
    }
}
