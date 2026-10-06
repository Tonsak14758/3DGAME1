/*
First Date: 2/10/2026
Date: 5/10/2026
Author: Tony
Use by: Drag this script onto the PlayerSphere (PlayerInventory is added automatically).
        Give any object you want collected by touch the tag "AutoPickUp" (add the tag in Tags & Layers).
        Optional: add ItemPickup to the object to set its Item Id, name and amount.
        Without ItemPickup, the object's name is the Item Id and the amount is 1.
Purpose: Collects "AutoPickUp" objects into the inventory as soon as the ball touches them.
         Works with normal colliders (bump into it) and "Is Trigger" colliders (roll through it).
         The "PickUp" tag is for items grabbed with F (see ItemGrabber).
         Items the player dropped (DroppedItem) are left alone for a moment, so they don't come straight back.
         An ItemBundle box gives back the whole stack it holds (not collected while the inventory is full).
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class AutoCollector : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Objects with this tag are collected by touch. The tag must exist in the project.")]
    [SerializeField] string autoTag = "AutoPickUp";

    PlayerInventory inv;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    // The ball rolled into a collider marked "Is Trigger".
    void OnTriggerEnter(Collider other)
    {
        TryCollect(other.gameObject);
    }

    // The ball bumped into a solid collider.
    void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.collider.gameObject);
    }

    // Still touching. Only matters for dropped items: their wait may end while the ball is already touching them.
    void OnTriggerStay(Collider other)
    {
        TryCollectDropped(other.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        TryCollectDropped(collision.collider.gameObject);
    }

    // ===== My methods =====

    void TryCollectDropped(GameObject hit)
    {
        GameObject item = FindTaggedObject(hit);
        if (item != null && item.GetComponent<DroppedItem>() != null) TryCollect(hit);
    }

    void TryCollect(GameObject hit)
    {
        GameObject item = FindTaggedObject(hit);
        if (item == null) return;          // not an auto pick-up
        if (!item.activeSelf) return;      // already collected this frame (e.g. two of its parts touched us)

        // The player just dropped it: wait a moment, or it would come straight back.
        DroppedItem dropped = item.GetComponent<DroppedItem>();
        if (dropped != null && !dropped.CanAutoCollect) return;

        // A bundle box gives back the stack it holds. Full inventory: it stays, try again in 1 second.
        ItemBundle bundle = item.GetComponent<ItemBundle>();
        if (bundle != null)
        {
            if (!bundle.TryGiveTo(inv) && dropped != null) dropped.Setup(1f);
            return;
        }

        // Use ItemPickup's settings if the object has one, otherwise fall back to its name and 1.
        ItemPickup info = item.GetComponent<ItemPickup>();
        string cleanName = ItemPickup.CleanName(item.name);   // "Coin (2)" -> "Coin", so copies stack
        string id = info != null ? info.ItemId : cleanName;
        string displayName = info != null ? info.DisplayName : cleanName;
        int amount = info != null ? info.Amount : 1;

        // Color, weight, material... (no ItemPickup = no Material Data, the rest is still read).
        ItemProperties props = info != null ? info.ReadProperties() : ItemProperties.Read(item, null, id);

        // Take the picture before the object disappears. Different properties get their own icon.
        string key = ItemTemplates.KeyFor(id, props);
        Sprite icon = IconCapture.GetIcon(key, item);

        if (inv.Add(id, displayName, icon, amount, props))
        {
            ItemTemplates.Store(key, item);   // keep a hidden copy so it can be dropped later (before hiding it!)
            item.SetActive(false);            // hide it right now so it can't be collected twice
            Destroy(item);                    // Destroy happens at the end of the frame
        }
        else
        {
            Debug.Log("Inventory is full.");
            if (dropped != null) dropped.Setup(1f);   // try again in 1 second, not every physics step
        }
    }

    // The ball may touch a child part (e.g. a sword's blade), so look up through the parents
    // for the object that has the tag. Returns null if none of them have it.
    GameObject FindTaggedObject(GameObject hit)
    {
        for (Transform t = hit.transform; t != null; t = t.parent)
        {
            if (t.CompareTag(autoTag)) return t.gameObject;
        }
        return null;
    }
}
