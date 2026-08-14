using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Named particle recipes, kept in one place so the look of the game can be re-tuned without
    /// hunting through gameplay code.
    /// </summary>
    public static class FxPresets
    {
        /// <summary>Bright shards thrown out when a diamond is collected.</summary>
        public static SpriteParticles.Burst DiamondShards(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.sparkle, art.spriteAdditive);
            b.count = 14;
            b.speedMin = 2.2f; b.speedMax = 5.4f;
            b.lifeMin = 0.3f; b.lifeMax = 0.55f;
            b.sizeMin = 0.10f; b.sizeMax = 0.22f;
            b.endSizeScale = 0f;
            b.colorA = cfg.diamondCore;
            b.colorB = Color.white;
            b.endColor = new Color(cfg.diamondGlow.r, cfg.diamondGlow.g, cfg.diamondGlow.b, 0f);
            b.drag = 4.5f;
            b.gravity = 1.5f;
            b.spin = 320f;
            b.spawnRadius = 0.06f;
            b.sortingOrder = 60;
            return b;
        }

        /// <summary>Soft halo puff layered under the shards so the collect reads at a glance.</summary>
        public static SpriteParticles.Burst DiamondHalo(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.glow, art.spriteAdditive);
            b.count = 5;
            b.speedMin = 0.3f; b.speedMax = 1.1f;
            b.lifeMin = 0.35f; b.lifeMax = 0.6f;
            b.sizeMin = 0.45f; b.sizeMax = 0.8f;
            b.endSizeScale = 1.9f;
            b.colorA = new Color(cfg.diamondGlow.r, cfg.diamondGlow.g, cfg.diamondGlow.b, 0.75f);
            b.colorB = new Color(1f, 1f, 1f, 0.6f);
            b.endColor = new Color(cfg.diamondGlow.r, cfg.diamondGlow.g, cfg.diamondGlow.b, 0f);
            b.drag = 3f;
            b.spin = 40f;
            b.spawnRadius = 0.08f;
            b.sortingOrder = 55;
            return b;
        }

        /// <summary>Molten blobs flung upwards when the player taps lava.</summary>
        public static SpriteParticles.Burst LavaSplash(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.glow, art.spriteAdditive);
            b.count = 20;
            b.angleMin = 25f; b.angleMax = 155f;   // upward fan, like a splash
            b.speedMin = 2.4f; b.speedMax = 6.5f;
            b.lifeMin = 0.4f; b.lifeMax = 0.85f;
            b.sizeMin = 0.10f; b.sizeMax = 0.30f;
            b.endSizeScale = 0.05f;
            b.colorA = cfg.lavaHot;
            b.colorB = new Color(1f, 0.85f, 0.35f, 1f);
            b.endColor = new Color(0.5f, 0.05f, 0f, 0f);
            b.gravity = 9f;
            b.drag = 0.6f;
            b.spin = 180f;
            b.spawnRadius = 0.16f;
            b.sortingOrder = 60;
            return b;
        }

        /// <summary>Dark smoke that lingers after the splash and sells the burn.</summary>
        public static SpriteParticles.Burst LavaSmoke(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.glow, art.spriteUnlit);
            b.count = 7;
            b.angleMin = 55f; b.angleMax = 125f;
            b.speedMin = 0.6f; b.speedMax = 1.6f;
            b.lifeMin = 0.7f; b.lifeMax = 1.2f;
            b.sizeMin = 0.4f; b.sizeMax = 0.75f;
            b.endSizeScale = 2.2f;
            b.colorA = new Color(0.14f, 0.10f, 0.10f, 0.7f);
            b.colorB = new Color(0.28f, 0.16f, 0.12f, 0.55f);
            b.endColor = new Color(0.1f, 0.08f, 0.08f, 0f);
            b.gravity = -0.8f;                      // negative gravity = rises
            b.drag = 1.4f;
            b.spin = 60f;
            b.spawnRadius = 0.2f;
            b.sortingOrder = 58;
            return b;
        }

        /// <summary>Single slow bubble used for the lava idle animation.</summary>
        public static SpriteParticles.Burst LavaBubble(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.glow, art.spriteAdditive);
            b.count = 1;
            b.angleMin = 70f; b.angleMax = 110f;
            b.speedMin = 0.25f; b.speedMax = 0.6f;
            b.lifeMin = 0.55f; b.lifeMax = 1.0f;
            b.sizeMin = 0.10f; b.sizeMax = 0.22f;
            b.endSizeScale = 1.5f;
            b.colorA = new Color(cfg.lavaHot.r, cfg.lavaHot.g, cfg.lavaHot.b, 0.75f);
            b.colorB = new Color(1f, 0.78f, 0.3f, 0.7f);
            b.endColor = new Color(cfg.lavaGlow.r, cfg.lavaGlow.g, cfg.lavaGlow.b, 0f);
            b.gravity = -0.4f;
            b.drag = 1.2f;
            b.spin = 20f;
            b.spawnRadius = 0.3f;
            b.sortingOrder = 25;
            return b;
        }

        /// <summary>Tiny drifting motes over the whole board, so the scene is never static.</summary>
        public static SpriteParticles.Burst AmbientMote(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.glow, art.spriteAdditive);
            b.count = 1;
            b.angleMin = 60f; b.angleMax = 120f;
            b.speedMin = 0.3f; b.speedMax = 0.9f;
            b.lifeMin = 1.6f; b.lifeMax = 3.2f;
            b.sizeMin = 0.05f; b.sizeMax = 0.13f;
            b.endSizeScale = 0.2f;
            b.colorA = new Color(1f, 0.55f, 0.2f, 0.5f);
            b.colorB = new Color(1f, 0.8f, 0.4f, 0.35f);
            b.endColor = new Color(1f, 0.5f, 0.2f, 0f);
            b.gravity = -0.15f;
            b.drag = 0.3f;
            b.spin = 30f;
            b.spawnRadius = 0.1f;
            b.sortingOrder = 5;
            return b;
        }

        /// <summary>Win celebration confetti, emitted along the top of the board.</summary>
        public static SpriteParticles.Burst WinConfetti(GameAssets art, GameConfig cfg)
        {
            var b = SpriteParticles.Burst.Default(art.sparkle, art.spriteAdditive);
            b.count = 26;
            b.angleMin = 200f; b.angleMax = 340f;   // downward fan
            b.speedMin = 1.5f; b.speedMax = 5f;
            b.lifeMin = 1.0f; b.lifeMax = 1.9f;
            b.sizeMin = 0.12f; b.sizeMax = 0.3f;
            b.endSizeScale = 0.3f;
            b.colorA = cfg.diamondCore;
            b.colorB = cfg.uiAccent;
            b.endColor = new Color(1f, 1f, 1f, 0f);
            b.gravity = 3.5f;
            b.drag = 0.5f;
            b.spin = 420f;
            b.spawnRadius = 0.4f;
            b.sortingOrder = 70;
            return b;
        }
    }
}
