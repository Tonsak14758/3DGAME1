/*
First Date: 30/9/2026
Date: 2/10/2026
Author: Tony
Use by: Drag this script onto any object the player can pick up (e.g. the StoneBlock prefab).
        Set Item Id: objects with the same Item Id (and the same properties) stack together and share one icon.
        Optional: drag a Material Data asset into "Material" (see MaterialData),
        and set "Edge Shape" if Auto guesses wrong (e.g. a custom wedge mesh).
Purpose: Marks an object as an item that can be grabbed with the crosshair and F.
         Its properties (see ItemProperties) are read when it is collected.
         Very thin Box Colliders are made thicker so physics doesn't miss them (the stats keep the real thickness).
*/

using System.Text.RegularExpressions;   // Regex: find and replace text by a pattern
using UnityEngine;

[DisallowMultipleComponent]
public class ItemPickup : MonoBehaviour
{
    // ===== Fields =====
    [Header("Item")]
    [Tooltip("The kind of item. Same Id = same inventory slot and same icon (e.g. \"Stone\").")]
    [SerializeField] string itemId = "";

    [Tooltip("Name shown to the player. Leave empty to use the Item Id.")]
    [SerializeField] string displayName = "";

    [Range(1, 99)]
    [Tooltip("How many items the player gets from grabbing this object.")]
    [SerializeField] int amount = 1;

    [Header("Properties")]
    [Tooltip("What this object is made of (a Material Data asset: Stone, Wood...). Sets hardness, toughness and weight.")]
    [SerializeField] MaterialData material;

    [Tooltip("What kind of edge it has. Auto = guess from the mesh (Cube = Box, Sphere = Round, \"Wedge\" in the name = Wedge).")]
    [SerializeField] EdgeShape edgeShape = EdgeShape.Auto;

    // itemId == null || itemId == "" is same as string.IsNullOrEmpty(itemId)
    public string ItemId => string.IsNullOrEmpty(itemId) ? CleanName(gameObject.name) : itemId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? ItemId : displayName;
    public int Amount => amount;
    public MaterialData Material => material;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        GrowThinColliders();
    }

    // Editor only: runs when the script is first added, so Item Id starts with the object's name.
    void Reset()
    {
        itemId = CleanName(gameObject.name);
    }

    // ===== My methods =====

    // Changes how many items this object gives (ItemDropper sets dropped objects to 1).
    public void SetAmount(int newAmount)
    {
        amount = Mathf.Max(1, newAmount);
    }

    // Reads this object's properties (color, shape, size, weight...) right now.
    public ItemProperties ReadProperties()
    {
        return ItemProperties.Read(gameObject, material, ItemId, edgeShape);
    }

    // Physics misses colliders that are very thin (a fast object can pass right through).
    // Makes every Box Collider at least SharpnessMath.MinColliderThickness thick.
    // Only the collider grows: the stats are read from the mesh, so they keep the real thickness.
    void GrowThinColliders()
    {
        foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>())
        {
            Vector3 scale = box.transform.lossyScale;
            Vector3 size = box.size;

            for (int axis = 0; axis < 3; axis++)   // 0 = x, 1 = y, 2 = z
            {
                float s = Mathf.Abs(scale[axis]);
                if (s <= 0f) continue;   // scale 0 can't be fixed by size

                // World thickness = size x scale. Too thin? Make the size big enough.
                if (size[axis] * s < SharpnessMath.MinColliderThickness)
                {
                    size[axis] = SharpnessMath.MinColliderThickness / s;
                }
            }

            box.size = size;
        }
    }

    // Copies of an object get names like "Coin (1)" or "Coin(Clone)".
    // This removes those endings so every copy gets the same Item Id ("Coin") and stacks.
    public static string CleanName(string objectName)
    {
        return Regex.Replace(objectName, @"(\s*\((\d+|Clone)\))+$", "");
    }
}
