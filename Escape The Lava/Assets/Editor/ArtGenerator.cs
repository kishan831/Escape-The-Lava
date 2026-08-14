using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeTheLava.EditorTools
{
    /// <summary>
    /// Generates every sprite the game uses as a PNG, straight from code.
    ///
    /// Nothing here is decorative cleverness: the assignment has to clone and run without a single
    /// missing reference, and shipping generated art means there are no binary assets to lose, no
    /// licensing questions and no import settings to get wrong by hand. Shapes are signed-distance
    /// fields, supersampled for clean edges.
    /// </summary>
    public static class ArtGenerator
    {
        public const string Folder = "Assets/Generated/Art";

        /// <summary>Writes every PNG, applies import settings, and assigns the sprites onto <paramref name="art"/>.</summary>
        public static void Generate(GameConfig config, GameAssets art)
        {
            BuildPaths.EnsureFolder(Folder);

            // --- board -------------------------------------------------------------------------
            WriteSquare("tile", 256, TileShader);
            WriteSquare("tile_face", 256, TileFaceShader);
            WriteSquare("diamond", 256, DiamondShader, supersample: 4);

            // --- effects -----------------------------------------------------------------------
            WriteSquare("glow", 128, GlowShader, supersample: 2);
            WriteSquare("ring", 128, RingShader, supersample: 3);
            WriteSquare("sparkle", 128, SparkleShader, supersample: 3);
            WriteSquare("vignette", 256, VignetteShader, supersample: 2);

            // --- ui ----------------------------------------------------------------------------
            WriteSquare("heart", 128, HeartShader, supersample: 6);
            WriteSquare("solid", 8, _ => Color.white, supersample: 1);
            WriteSquare("panel", 64, PanelShader, supersample: 4);
            Write("lava_surface", 256, 64, LavaSurfaceShader, supersample: 3);

            AssetDatabase.Refresh();

            // Import settings, then hand the sprites to the asset table.
            art.tile = ImportSprite("tile", 256);
            art.tileFace = ImportSprite("tile_face", 256);
            art.diamond = ImportSprite("diamond", 256);
            art.glow = ImportSprite("glow", 128);
            art.ring = ImportSprite("ring", 128);
            art.sparkle = ImportSprite("sparkle", 128);
            art.vignette = ImportSprite("vignette", 256);
            art.heart = ImportSprite("heart", 128);
            art.solid = ImportSprite("solid", 8);
            art.panel = ImportSprite("panel", 64, new Vector4(22f, 22f, 22f, 22f));
            art.lavaSurface = ImportSprite("lava_surface", 256);

            art.spriteUnlit = BuildMaterial("SpriteUnlit",
                "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Sprites/Default");
            art.spriteAdditive = BuildMaterial("SpriteAdditive",
                "EscapeTheLava/Additive Sprite", "Sprites/Default");

            art.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            EditorUtility.SetDirty(art);
        }

        // ---------------------------------------------------------------------- shape functions
        // Every shader takes uv in 0..1 and returns a straight (non-premultiplied) colour.
        // `P(uv)` maps that to -1..1 so the signed-distance helpers stay readable.

        static Vector2 P(Vector2 uv) => new Vector2(uv.x * 2f - 1f, uv.y * 2f - 1f);

        /// <summary>Tile body: rounded square with a darker rim and a light-from-above gradient.</summary>
        static Color TileShader(Vector2 uv)
        {
            Vector2 p = P(uv);
            float d = RoundedBox(p, new Vector2(0.98f, 0.98f), 0.44f);
            float alpha = Mathf.Clamp01(-d / 0.012f);

            float inner = Mathf.Clamp01(-d / 0.22f);
            float luminance = Mathf.Lerp(0.62f, 1f, inner);
            luminance *= Mathf.Lerp(0.86f, 1f, uv.y);

            return new Color(luminance, luminance, luminance, alpha);
        }

        /// <summary>Inner tile face: slightly smaller rounded square, lit from the top.</summary>
        static Color TileFaceShader(Vector2 uv)
        {
            Vector2 p = P(uv);
            float d = RoundedBox(p, new Vector2(0.96f, 0.96f), 0.4f);
            float alpha = Mathf.Clamp01(-d / 0.012f);
            float luminance = Mathf.Lerp(0.78f, 1f, uv.y);
            return new Color(luminance, luminance, luminance, alpha);
        }

        static readonly Vector2[] GemOutline =
        {
            new Vector2(-0.34f, 0.72f),
            new Vector2(0.34f, 0.72f),
            new Vector2(0.68f, 0.28f),
            new Vector2(0f, -0.9f),
            new Vector2(-0.68f, 0.28f)
        };

        /// <summary>
        /// Brilliant-cut gem. The blue is baked in (there is only ever one diamond colour), the
        /// facets come from banding the angle around the crown, and a bevel keeps the silhouette crisp.
        /// </summary>
        static Color DiamondShader(Vector2 uv)
        {
            Vector2 p = P(uv);
            float d = PolygonSdf(p, GemOutline);
            float alpha = Mathf.Clamp01(-d / 0.014f);
            if (alpha <= 0f) return new Color(0f, 0f, 0f, 0f);

            var deep = new Color(0.05f, 0.32f, 0.7f);
            var mid = new Color(0.24f, 0.62f, 0.95f);
            var bright = new Color(0.68f, 0.94f, 1f);

            // Vertical base gradient: dark at the point, bright at the table.
            float vertical = Mathf.InverseLerp(-0.9f, 0.72f, p.y);
            Color color = Color.Lerp(deep, mid, vertical);

            // Facet banding radiating from just below the table.
            Vector2 fromCrown = p - new Vector2(0f, 0.28f);
            float angle = Mathf.Atan2(fromCrown.y, fromCrown.x);
            float band = Mathf.Repeat(angle / (Mathf.PI / 3f), 1f);
            float facet = 0.82f + Mathf.Abs(band - 0.5f) * 0.72f;
            color *= facet;

            // Table facet on top reads as flat and bright.
            if (p.y > 0.28f) color = Color.Lerp(color, bright, 0.55f);

            // Specular blob, upper left.
            float highlight = Mathf.Clamp01(1f - (p - new Vector2(-0.24f, 0.44f)).magnitude / 0.26f);
            color += Color.white * highlight * highlight * 0.55f;

            // Bevel: brighten the last few pixels inside the silhouette.
            float rim = 1f - Mathf.Clamp01(-d / 0.07f);
            color = Color.Lerp(color, Color.white, rim * 0.45f);

            return new Color(
                Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), alpha);
        }

        /// <summary>Soft radial falloff. Used for glows, bubbles, embers and smoke.</summary>
        static Color GlowShader(Vector2 uv)
        {
            float r = P(uv).magnitude;
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Hollow ring for the lava shockwave.</summary>
        static Color RingShader(Vector2 uv)
        {
            float r = P(uv).magnitude;
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(r - 0.72f) / 0.16f), 1.7f);
            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Eight-point sparkle: two thin lenses on the axes, two smaller on the diagonals.</summary>
        static Color SparkleShader(Vector2 uv)
        {
            Vector2 p = P(uv);
            Vector2 rotated = new Vector2(p.x + p.y, p.y - p.x) * 0.70710678f;

            float horizontal = Lens(p.x, p.y, 0.98f, 0.12f);
            float vertical = Lens(p.y, p.x, 0.98f, 0.12f);
            float diagonalA = Lens(rotated.x, rotated.y, 0.55f, 0.07f);
            float diagonalB = Lens(rotated.y, rotated.x, 0.55f, 0.07f);
            float core = Mathf.Clamp01(1f - p.magnitude / 0.2f);

            float alpha = Mathf.Max(
                Mathf.Max(Mathf.Pow(horizontal, 0.7f), Mathf.Pow(vertical, 0.7f)),
                Mathf.Max(Mathf.Max(diagonalA, diagonalB), core * core));

            return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        static float Lens(float along, float across, float length, float width)
        {
            float a = along / length;
            float b = across / width;
            return Mathf.Clamp01(1f - a * a - b * b);
        }

        /// <summary>Inverse radial gradient: clear in the middle, opaque at the edges.</summary>
        static Color VignetteShader(Vector2 uv)
        {
            float r = P(uv).magnitude;
            float alpha = Mathf.SmoothStep(0.42f, 1.05f, r);
            return new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Classic implicit heart curve, shaded with a small highlight.</summary>
        static Color HeartShader(Vector2 uv)
        {
            Vector2 p = P(uv);

            // The curve (x^2+y^2-1)^3 = x^2*y^3 spans roughly x in [-1.2, 1.2] and y in [-1.0, 1.26],
            // so the vertical map is offset upwards to keep the lobes inside the texture.
            float x = p.x * 1.25f;
            float y = p.y * 1.175f + 0.125f;

            float t = x * x + y * y - 1f;
            float f = t * t * t - x * x * y * y * y;
            if (f > 0f) return new Color(0f, 0f, 0f, 0f);

            float luminance = Mathf.Lerp(0.72f, 1f, uv.y);
            float highlight = Mathf.Clamp01(1f - (p - new Vector2(-0.3f, 0.34f)).magnitude / 0.3f);
            luminance = Mathf.Clamp01(luminance + highlight * highlight * 0.35f);

            return new Color(luminance, luminance, luminance, 1f);
        }

        /// <summary>9-sliced rounded panel. The inset is darker so a flat tint still shows an outline.</summary>
        static Color PanelShader(Vector2 uv)
        {
            Vector2 p = P(uv);
            float d = RoundedBox(p, new Vector2(0.97f, 0.97f), 0.55f);
            float alpha = Mathf.Clamp01(-d / 0.05f);
            float inset = Mathf.Clamp01(-(d + 0.09f) / 0.05f);
            float luminance = Mathf.Lerp(1f, 0.84f, inset);
            return new Color(luminance, luminance, luminance, alpha);
        }

        /// <summary>Wavy top edge for the rising lava overlay.</summary>
        static Color LavaSurfaceShader(Vector2 uv)
        {
            float wave = 0.58f
                       + Mathf.Sin(uv.x * Mathf.PI * 2f * 3f) * 0.18f
                       + Mathf.Sin(uv.x * Mathf.PI * 2f * 7f + 1.3f) * 0.08f;

            float alpha = Mathf.Clamp01((wave - uv.y) / 0.03f);
            float crest = Mathf.Clamp01((wave - uv.y) * 3.5f);
            float luminance = Mathf.Lerp(1f, 0.62f, crest);
            return new Color(luminance, luminance, luminance, alpha);
        }

        // ---------------------------------------------------------------------- sdf helpers

        /// <summary>Signed distance to a rounded box. Negative inside.</summary>
        static float RoundedBox(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + Vector2.one * radius;
            return Mathf.Min(Mathf.Max(q.x, q.y), 0f) + new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude - radius;
        }

        /// <summary>Signed distance to a closed polygon. Negative inside.</summary>
        static float PolygonSdf(Vector2 p, Vector2[] vertices)
        {
            int count = vertices.Length;
            float squared = (p - vertices[0]).sqrMagnitude;
            float sign = 1f;

            for (int i = 0, j = count - 1; i < count; j = i, i++)
            {
                Vector2 edge = vertices[j] - vertices[i];
                Vector2 toPoint = p - vertices[i];

                float h = Mathf.Clamp01(Vector2.Dot(toPoint, edge) / Vector2.Dot(edge, edge));
                squared = Mathf.Min(squared, (toPoint - edge * h).sqrMagnitude);

                // Winding test: three conditions all true (or all false) means we crossed an edge.
                bool aboveStart = p.y >= vertices[i].y;
                bool belowEnd = p.y < vertices[j].y;
                bool rightOfEdge = edge.x * toPoint.y > edge.y * toPoint.x;
                if ((aboveStart && belowEnd && rightOfEdge) || (!aboveStart && !belowEnd && !rightOfEdge)) sign = -sign;
            }

            return sign * Mathf.Sqrt(squared);
        }

        // ---------------------------------------------------------------------- io

        static void WriteSquare(string name, int size, Func<Vector2, Color> shader, int supersample = 3)
            => Write(name, size, size, shader, supersample);

        /// <summary>Rasterises a shader function into a PNG, averaging <paramref name="supersample"/>^2 samples per pixel.</summary>
        static void Write(string name, int width, int height, Func<Vector2, Color> shader, int supersample = 3)
        {
            var pixels = new Color[width * height];
            int samples = Mathf.Max(1, supersample);
            float sampleWeight = 1f / (samples * samples);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;

                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            var uv = new Vector2(
                                (x + (sx + 0.5f) / samples) / width,
                                (y + (sy + 0.5f) / samples) / height);

                            Color c = shader(uv);
                            // Weight colour by alpha so transparent samples cannot darken the edge.
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    }

                    a *= sampleWeight;
                    if (a > 0.0001f)
                    {
                        float inverse = sampleWeight / a;
                        pixels[y * width + x] = new Color(r * inverse, g * inverse, b * inverse, a);
                    }
                    else
                    {
                        pixels[y * width + x] = new Color(1f, 1f, 1f, 0f);
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(Path.Combine(BuildPaths.ProjectRoot, $"{Folder}/{name}.png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        static Sprite ImportSprite(string name, int pixelsPerUnit, Vector4 border = default)
        {
            string path = $"{Folder}/{name}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"[Escape The Lava] Could not import generated texture at {path}");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Material BuildMaterial(string name, string shaderName, string fallbackShaderName)
        {
            BuildPaths.EnsureFolder(BuildPaths.MaterialFolder);
            string path = $"{BuildPaths.MaterialFolder}/{name}.mat";

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Escape The Lava] Shader '{shaderName}' not found, falling back to '{fallbackShaderName}'.");
                shader = Shader.Find(fallbackShaderName);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
