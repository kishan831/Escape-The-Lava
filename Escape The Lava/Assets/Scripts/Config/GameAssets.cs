using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Single lookup for every sprite, material, audio clip and font the game needs.
    /// The one-click builder generates the assets procedurally and fills this in, which is
    /// why the repository ships without any binary art or audio files.
    /// </summary>
    [CreateAssetMenu(menuName = "Escape The Lava/Game Assets", fileName = "GameAssets")]
    public class GameAssets : ScriptableObject
    {
        [Header("Sprites")]
        [Tooltip("Rounded square used for every tile body.")]
        public Sprite tile;

        [Tooltip("Rounded square with no border, used for the inner tile face.")]
        public Sprite tileFace;

        public Sprite diamond;
        public Sprite heart;

        [Tooltip("Soft radial gradient. Doubles as glow, bubble and ember.")]
        public Sprite glow;

        [Tooltip("Inverse radial gradient: clear in the middle, opaque at the edges. Used for the damage vignette.")]
        public Sprite vignette;

        [Tooltip("Hollow ring used for the lava shockwave.")]
        public Sprite ring;

        [Tooltip("Four-point sparkle used for diamond shine and confetti.")]
        public Sprite sparkle;

        [Tooltip("Flat white 8x8. Used for full-screen flashes and solid UI fills.")]
        public Sprite solid;

        [Tooltip("9-sliced rounded panel for UI.")]
        public Sprite panel;

        [Tooltip("Wavy top edge for the rising lava loss overlay.")]
        public Sprite lavaSurface;

        [Header("Materials")]
        [Tooltip("Unlit sprite material. Explicitly assigned so tiles never depend on 2D lights.")]
        public Material spriteUnlit;

        [Tooltip("Additive blend material used for every glow so bloom has something to catch.")]
        public Material spriteAdditive;

        [Header("Audio")]
        public AudioClip sfxCollect;
        public AudioClip sfxDamage;
        public AudioClip sfxTick;
        public AudioClip sfxWin;
        public AudioClip sfxLose;
        public AudioClip sfxUi;
        public AudioClip sfxWhoosh;

        [Header("Font")]
        public Font font;

        /// <summary>True when the builder has run and every required reference is present.</summary>
        public bool IsComplete =>
            tile && tileFace && diamond && heart && glow && vignette && ring && sparkle && solid &&
            panel && lavaSurface && spriteUnlit && spriteAdditive && font;
    }
}
