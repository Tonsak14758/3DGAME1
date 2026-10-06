/*
First Date: 2/10/2026
Date: 5/10/2026
Author: Tony
Use by: In the Project window: right click > Create > Crafting > Material Data.
        Make one asset per resource (Stone, Wood, Iron...) and fill in its numbers.
        Then drag the asset into the "Material" field of ItemPickup on the objects made of it.
Purpose: Describes what a resource is made of. ItemProperties reads it when the player grabs an item,
         and crafting will use these numbers to calculate the stats of what the player builds.
*/

using UnityEngine;

// CreateAssetMenu adds this to the right click > Create menu in the Project window.
[CreateAssetMenu(fileName = "NewMaterial", menuName = "Crafting/Material Data")]
public class MaterialData : ScriptableObject
{
    // ===== Fields =====
    [Tooltip("Name shown to the player. Leave empty to use the asset's file name.")]
    [SerializeField] string displayName = "";

    [Min(0f)]
    [Tooltip("Weight of a 1 x 1 x 1 block of this material. Water = 1, Wood ~0.7, Stone ~2.5, Iron ~7.9.")]
    [SerializeField] float weightPerUnit = 1f;

    [Range(0f, 10f)]
    [Tooltip("How hard it is. 0 = soft like cloth, 10 = hard like diamond.")]
    [SerializeField] float hardness = 5f;

    [Range(0f, 10f)]
    [Tooltip("How well it survives hits without chipping or snapping. 0 = brittle like glass, 10 = tough like spring steel.")]
    [SerializeField] float toughness = 5f;

    [Tooltip("Ticked: this material floats, so a building with a piece of it can be built in the air.")]
    [SerializeField] bool floats = false;

    // Sharpness is NOT set here: it comes from the shape (how thin) and the hardness. See SharpnessMath.

    // "name" is the asset's file name (every ScriptableObject has it).
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public float WeightPerUnit => weightPerUnit;
    public float Hardness => hardness;
    public float Toughness => toughness;
    public bool Floats => floats;
}
