/*
Date: 5/10/2026
Author: Tony
Use by: 1. Hierarchy: select PlayerSphere > Add Component > BlueprintInteractor.
        2. Optional Inspector setting: Reach (how far from the ball a blueprint can be opened).
        3. Nothing else to connect. Needs in the scene: the Crosshair script (HUD), a BlueprintPanel on an ACTIVE canvas
           (see BlueprintPanel), and an InteractionMachine (or GameRule "World Interaction" ticked).
Purpose: Aim at a placed blueprint and press Interact (right click, set in KeyBindings) to open its panel.
         The blueprint must be inside an InteractionMachine's area (or the GameRule WorldInteraction on),
         otherwise a warning shows on screen.
         Right click on anything else does nothing here (so it can still levitate things).
*/

using UnityEngine;

public class BlueprintInteractor : MonoBehaviour
{
    // ===== Fields =====
    [Range(1f, 30f)]
    [Tooltip("How far from the ball a blueprint can be opened (measured to its surface).")]
    [SerializeField] float reach = 8f;

    // ===== Unity messages (Unity calls these) =====

    void Update()
    {
        // That click belongs to a menu, to placing, or just closed the panel.
        if (MenuState.AnyOpen || BlueprintPlacer.Busy || BlueprintPanel.ClosedThisFrame) return;
        if (!InteractPressed() || Crosshair.Target == null) return;

        Blueprint bp = Crosshair.Target.GetComponentInParent<Blueprint>();
        if (bp == null || !bp.IsPlaced) return;   // not a blueprint: leave the click to others (e.g. levitate)

        if (!InReach(Crosshair.Target))
        {
            ScreenMessage.Show("Too far away");
            return;
        }

        if (!InteractionMachine.CanInteractWith(BoundsUtil.Of(bp.gameObject)))
        {
            ScreenMessage.Show("No Interaction Machine in range");
            return;
        }

        BlueprintPanel.Open(bp);
    }

    // ===== My methods =====

    bool InReach(GameObject target)
    {
        // Measure to the nearest point on the piece, so big blueprints aren't harder to reach.
        Collider col = target.GetComponent<Collider>();
        Vector3 nearest = col != null ? col.ClosestPoint(transform.position) : target.transform.position;
        return Vector3.Distance(transform.position, nearest) <= reach;
    }

    bool InteractPressed()
    {
        return Input.GetKeyDown(KeyBindings.Interact);   // the key is chosen in KeyBindings
    }
}
