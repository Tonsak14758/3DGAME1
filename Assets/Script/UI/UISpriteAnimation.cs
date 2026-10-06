/*
First Date: 3/10/2026
Date: 3/10/2026
Author: Tony
Use by: Add this script to a UI Image (e.g. SelectionHighlight).
        Easiest: drag your .aseprite file into "Aseprite File". The frames and each frame's time
        are filled in from it (Aseprite Importer, Import Mode = Animated Sprite).
        Or: leave it empty and drag the frame sprites into "Frames" yourself, in order (then "Fps" is used).
Purpose: Plays a list of sprites on a UI Image, one after another, looping.
         The animation clip the Aseprite Importer makes only works on a SpriteRenderer (not a UI Image),
         so this script plays the same frames on the Image instead.
         Uses unscaled time, so it keeps playing even if the game is paused (Time.timeScale = 0).
*/

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;   // AssetDatabase, AnimationUtility: only exist in the Editor, so they're wrapped in #if
#endif

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class UISpriteAnimation : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Drag a .aseprite file here to fill Frames and Frame Times automatically (Editor only).")]
    [SerializeField] Object asepriteFile;

    [Tooltip("The animation frames, in order.")]
    [SerializeField] Sprite[] frames;

    [Tooltip("How long each frame shows, in seconds (filled from the .aseprite file). Empty = use Fps for every frame.")]
    [SerializeField] float[] frameTimes;

    [Tooltip("Frames per second, used when Frame Times is empty. 100 ms in Aseprite = 10 fps.")]
    [Range(1f, 60f)]
    [SerializeField] float fps = 10f;

    [Tooltip("How see-through the animation is: 0 = invisible, 1 = fully solid. Can be changed while playing.")]
    [Range(0f, 1f)]
    [SerializeField] float alpha = 1f;

    Image image;
    int current = -1;    // the frame shown now (-1 = none yet)
    float startTime;     // when the animation (re)started; frame 1 shows at this time

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        image = GetComponent<Image>();   // cache it once
        ApplyAlpha();
    }

    // Every time the object is shown again (SetActive(true)), start from the first frame.
    void OnEnable()
    {
        Restart();
    }

    void Update()
    {
        ShowFrame();
    }

#if UNITY_EDITOR
    // Unity calls this when a value is changed in the Inspector: read the .aseprite file when one is dropped in.
    void OnValidate()
    {
        if (asepriteFile != null) ReadAseprite();
        ApplyAlpha();
    }
#endif

    // ===== My methods =====

    // Sets the Image's transparency, keeping its color (the Image Color's own alpha is replaced by this one).
    void ApplyAlpha()
    {
        if (image == null) image = GetComponent<Image>();   // OnValidate can run before Awake
        if (image == null) return;

        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    // Plays the animation from the first frame again (e.g. HotbarUI calls it when the selected slot changes).
    public void Restart()
    {
        startTime = Time.unscaledTime;
        current = -1;   // forces the first frame to be drawn now
        ShowFrame();
    }

    void ShowFrame()
    {
        if (frames == null || frames.Length == 0 || image == null) return;

        int frame = FrameAt(Time.unscaledTime - startTime);   // time since the start, not since the game started

        // Only touch the Image when the frame changes: changing a UI image makes the canvas redraw.
        if (frame != current)
        {
            current = frame;
            image.sprite = frames[frame];
        }
    }

    // Which frame shows at this time, looping.
    int FrameAt(float time)
    {
        bool hasTimes = frameTimes != null && frameTimes.Length == frames.Length;
        if (!hasTimes)
        {
            return (int)(time * fps) % frames.Length;   // every frame the same length
        }

        // Each frame has its own length: find where we are inside one loop.
        float loop = 0f;
        foreach (float t in frameTimes) loop += t;
        if (loop <= 0f) return 0;

        float timeInLoop = time % loop;
        for (int i = 0; i < frameTimes.Length; i++)
        {
            timeInLoop -= frameTimes[i];
            if (timeInLoop < 0f) return i;
        }
        return frames.Length - 1;   // only reached by rounding at the very end of the loop
    }

#if UNITY_EDITOR
    // Fills frames + frameTimes from the animation clip the Aseprite Importer made inside the .aseprite file.
    void ReadAseprite()
    {
        string path = AssetDatabase.GetAssetPath(asepriteFile);
        if (!path.EndsWith(".aseprite") && !path.EndsWith(".ase"))
        {
            Debug.LogWarning("UISpriteAnimation: \"" + asepriteFile.name + "\" is not a .aseprite file.");
            return;
        }

        // A .aseprite file holds several things (sprites, clips, a prefab). Find its first animation clip.
        AnimationClip clip = null;
        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (part is AnimationClip c) { clip = c; break; }
        }

        if (clip == null)
        {
            Debug.LogWarning("UISpriteAnimation: no animation in \"" + asepriteFile.name +
                             "\". Set its Import Mode to Animated Sprite and press Apply.");
            return;
        }

        // The clip changes the sprite at set times. Each key = one frame (its sprite + when it starts).
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.propertyName != "m_Sprite") continue;   // only the sprite curve

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var newFrames = new System.Collections.Generic.List<Sprite>();
            var newTimes = new System.Collections.Generic.List<float>();

            for (int i = 0; i < keys.Length; i++)
            {
                // A frame lasts until the next key starts (the last one until the clip ends).
                float end = i + 1 < keys.Length ? keys[i + 1].time : clip.length;
                float length = end - keys[i].time;
                if (length <= 0f) continue;   // the importer may add a last key at the very end; it lasts 0 s

                newFrames.Add(keys[i].value as Sprite);
                newTimes.Add(length);
            }

            frames = newFrames.ToArray();
            frameTimes = newTimes.ToArray();
            return;
        }

        Debug.LogWarning("UISpriteAnimation: the animation in \"" + asepriteFile.name + "\" has no sprite frames.");
    }
#endif
}
