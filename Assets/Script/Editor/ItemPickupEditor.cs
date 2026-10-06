/*
Date: 2/10/2026
Author: Tony
Use by: Nothing to set up. Click any object that has ItemPickup script (in the Scene view or the Hierarchy)
        and look at the bottom of its ItemPickup component in the Inspector.
        This file must stay inside a folder named "Editor": Unity only uses it in the Editor, never in the game.
Purpose: Shows every property the item will have when the player collects it (see ItemProperties),
         worked out live from the object: resource, colors, shape, size, weight, edge and sharpness.
         Scale or recolor the object and the numbers update, so you can test shapes without playing.
*/

using UnityEditor;   // Editor-only tools: CustomEditor, EditorGUILayout...
using UnityEngine;

// Tells Unity: draw ItemPickup's Inspector with this script instead of the normal one.
[CustomEditor(typeof(ItemPickup))]
public class ItemPickupEditor : Editor
{
    // ===== Fields =====
    bool showProperties = true;   // is the "Item Properties" section open

    // ===== Unity messages (Unity calls these) =====

    // Unity calls this to draw the Inspector (many times, e.g. when something changes).
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();   // the normal fields first (Item Id, Amount, Material...)

        EditorGUILayout.Space();
        showProperties = EditorGUILayout.Foldout(showProperties, "Item Properties (live preview)", true, EditorStyles.foldoutHeader);
        if (!showProperties) return;

        ItemPickup item = (ItemPickup)target;   // "target" = the component being shown
        ItemProperties p = item.ReadProperties();

        // DisabledScope greys the fields out: they are results to read, not values to type in.
        using (new EditorGUI.DisabledScope(true))
        {
            Section("Resource");
            EditorGUILayout.TextField("Resource", p.resource);
            EditorGUILayout.ObjectField("Material Data", p.material, typeof(MaterialData), false);
            EditorGUILayout.FloatField("Hardness", p.hardness);
            EditorGUILayout.FloatField("Toughness", p.toughness);

            Section("Look");
            EditorGUILayout.ColorField("Color (shown)", p.color);
            EditorGUILayout.ColorField("Material Color", p.materialColor);
            EditorGUILayout.TextField("Render Material", p.renderMaterial);

            Section("Shape");
            EditorGUILayout.TextField("Shape", p.shape);
            EditorGUILayout.Vector3Field("Size", p.size);
            EditorGUILayout.FloatField("Weight", p.weight);

            Section("Edge (sharpest part)");
            EditorGUILayout.EnumPopup("Edge Shape", p.edgeShape);
            EditorGUILayout.FloatField("Thickness (t)", p.thickness);
            EditorGUILayout.FloatField("Width (w)", p.edgeWidth);
            EditorGUILayout.FloatField("Edge Length (L)", p.edgeLength);
            EditorGUILayout.FloatField("Edge Angle (°)", p.edgeAngle);
            EditorGUILayout.FloatField("Sharpness (0-10)", p.sharpness);
            EditorGUILayout.FloatField("Edge Strength", p.edgeStrength);

            // Not stored on the item, but handy to see while balancing.
            float chip = SharpnessMath.ChipChance(p.hardness, p.edgeStrength);
            EditorGUILayout.TextField("Chip Chance per Cut", (chip * 100f).ToString("0.##") + " %");

            Section("Stacking");
            EditorGUILayout.TextField("Stack Key", p.Key);   // items stack only when this matches
        }

        if (p.material == null)
        {
            EditorGUILayout.HelpBox("No Material Data set: hardness and toughness are 0, so sharpness is 0.", MessageType.Info);
        }
    }

    // ===== My methods =====

    // A small bold title between groups of fields.
    static void Section(string title)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}
