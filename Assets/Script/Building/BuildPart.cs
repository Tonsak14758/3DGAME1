/*
Date: 5/10/2026
Author: Tony
Use by: Goes on every piece (shape) of a BUILDING PREFAB. Prefab when done, e.g.:
          InteractMachine (Empty)   <- the building's own scripts (e.g. InteractionMachine)
              Stone                 <- Collider + THIS SCRIPT   (one per piece of the recipe)
              Stone                 <- Collider + THIS SCRIPT

        1. Hierarchy: right click > Create Empty, rename it (e.g. "InteractMachine"). Add the building's own scripts to it.
        2. Make each piece as its child: right click InteractMachine > 3D Object > Cube (or Sphere, Cylinder...),
           move / scale it into place. (Or drag in copies of an item, e.g. Stone, then right click it > Prefab > Unpack.)
        3. On EVERY piece:
           - Add Component > BuildPart (this script).
           - Item Id = the EXACT Item Id of the inventory item it needs, same capitals (look at that item's ItemPickup).
             When the script is added, it starts with the piece's name (without " (1)"), so check it.
           - Material (optional) = a Material Data. If its "Floats" is ticked, the building may be placed in the air.
           - REMOVE Rigidbody and ItemPickup if the piece has them (or the finished building falls apart,
             and the player can grab it apart with F). Set Tag = Untagged. KEEP the Collider.
        4. Drag InteractMachine from the Hierarchy into the Project window (it becomes a prefab), then delete it
           from the Hierarchy. Next: make a Building Data for it (see BuildingData).
        The recipe is counted from these pieces: 8 pieces with Item Id "Stone" = the blueprint asks for 8 Stone.
Purpose: Marks one shape of a building as one required material. The blueprint lists these as its recipe,
         and each one turns from blue into a real piece when the player adds that item.
*/

using UnityEngine;

[DisallowMultipleComponent]
public class BuildPart : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The inventory Item Id this piece needs, e.g. \"StoneCube\". Empty = the object's name (without (1), (Clone)).")]
    [SerializeField] string itemId = "";

    [Tooltip("Optional: what this piece is made of. A material with \"Floats\" ticked lets the building stay in the air.")]
    [SerializeField] MaterialData material;

    public string ItemId => string.IsNullOrEmpty(itemId) ? ItemPickup.CleanName(gameObject.name) : itemId;
    public MaterialData Material => material;

    // ===== Unity messages (Unity calls these) =====

    // Editor only: runs when the script is first added, so Item Id starts with the shape's name.
    void Reset()
    {
        itemId = ItemPickup.CleanName(gameObject.name);
    }
}
