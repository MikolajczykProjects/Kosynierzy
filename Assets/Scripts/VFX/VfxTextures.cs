using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Procedural textures for the code-built effects (no texture assets to import or wire).
    /// White RGB unless noted, shape in alpha, so particle colour / _BaseColor tints them.
    /// Generated once at start-up (a few ms), mip-mapped, then made non-readable.
    /// </summary>
    public static class VfxTextures
    {
        static Texture2D New(int w, int h, bool mips = true)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, mips)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1,
            };
        }

        static void Finish(Texture2D tex, Color32[] px, string name)
        {
            tex.name = name;
            tex.SetPixels32(px);
            tex.Apply(true, true);
        }

        static byte B(float v) => (byte)(Mathf.Clamp01(v) * 255f + 0.5f);

        /// <summary>Soft round blob (smoothstep falloff).</summary>
        public static Texture2D SoftCircle(int size)
        {
            var tex = New(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                px[y * size + x] = new Color32(255, 255, 255, B(a));
            }
            Finish(tex, px, "vfx_soft");
            return tex;
        }

        /// <summary>Hot dot: tight core + soft halo. Used for sparks (stretched), embers and tracer heads.</summary>
        public static Texture2D Spark(int size)
        {
            var tex = New(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float r2 = nx * nx + ny * ny;
                float a = Mathf.Exp(-r2 * 28f) + Mathf.Exp(-r2 * 6f) * 0.35f;
                a *= Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 4f);
                px[y * size + x] = new Color32(255, 255, 255, B(a));
            }
            Finish(tex, px, "vfx_spark");
            return tex;
        }

        /// <summary>
        /// Muzzle / impact flash: four long tapering rays, four short diagonal ones, hot core and a soft bloom halo.
        /// RGB shifts from white in the core to warm at the ray tips (the particle colour tints on top).
        /// </summary>
        public static Texture2D Star(int size)
        {
            var tex = New(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float r2 = nx * nx + ny * ny;
                float r = Mathf.Sqrt(r2);
                float core = Mathf.Exp(-r2 * 34f);
                float halo = Mathf.Exp(-r2 * 7f) * 0.32f;
                float rays = Ray(nx, ny, 1f, 0.075f) + Ray(ny, nx, 1f, 0.075f);
                const float s = 0.70710678f;
                float u = (nx + ny) * s, v = (nx - ny) * s;
                rays += (Ray(u, v, 0.55f, 0.05f) + Ray(v, u, 0.55f, 0.05f)) * 0.5f;
                float a = Mathf.Clamp01(core + halo + rays) * Mathf.Clamp01((1f - r) * 6f);
                // Hue shift: white core → warm tips.
                float warm = Mathf.Clamp01(r * 1.3f);
                px[y * size + x] = new Color32(255, B(1f - warm * 0.18f), B(1f - warm * 0.45f), B(a));
            }
            Finish(tex, px, "vfx_star");
            return tex;
        }

        // A ray along the "along" axis: tapers to a point at |along| = length.
        static float Ray(float along, float across, float length, float width)
        {
            float t = Mathf.Abs(along) / length;
            if (t >= 1f) return 0f;
            float w = width * (1f - t) + 0.004f;
            float k = across / w;
            return Mathf.Pow(1f - t, 1.6f) * Mathf.Exp(-k * k);
        }

        /// <summary>
        /// 2×2 atlas of four different smoke puffs: lumpy outline, inner density noise and baked top light,
        /// so random frames + small rotation never repeat the same blob.
        /// </summary>
        public static Texture2D SmokeAtlas(int tile)
        {
            int size = tile * 2;
            var tex = New(size, size);
            var px = new Color32[size * size];
            for (int ty = 0; ty < 2; ty++)
            for (int tx = 0; tx < 2; tx++)
            {
                float seed = 13.7f + (ty * 2 + tx) * 41.3f;
                float c = (tile - 1) * 0.5f;
                for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    float nx = (x - c) / c, ny = (y - c) / c;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);
                    // Lumpy silhouette: radius wobbles with angle.
                    float lump = Mathf.PerlinNoise(Mathf.Cos(ang) * 1.6f + seed, Mathf.Sin(ang) * 1.6f + seed * 0.7f);
                    float edge = 0.58f + (lump - 0.5f) * 0.42f;
                    float body = Mathf.Clamp01((edge - r) / (edge * 0.75f));
                    body = body * body * (3f - 2f * body);
                    // Billows inside.
                    float n = Fbm(nx * 2.2f + seed, ny * 2.2f - seed, 4);
                    float a = body * (0.45f + 0.75f * n);
                    // Baked light from above: brighter top, shadowed underside.
                    float lit = 0.72f + 0.28f * Mathf.Clamp01(ny * 0.6f + 0.5f) + (n - 0.5f) * 0.18f;
                    byte l = B(lit);
                    px[(ty * tile + y) * size + tx * tile + x] = new Color32(l, l, l, B(a));
                }
            }
            Finish(tex, px, "vfx_smoke");
            return tex;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x, y) * amp;
                norm += amp;
                x = x * 2.03f + 5.1f;
                y = y * 2.03f + 1.7f;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Thin soft ring with a faint inner fill — shockwaves and ground dust rings.</summary>
        public static Texture2D Ring(int size)
        {
            var tex = New(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float k = (r - 0.82f) / 0.07f;
                float ring = Mathf.Exp(-k * k);
                float fill = r < 0.82f ? Mathf.Pow(r / 0.82f, 3f) * 0.22f : 0f;
                float a = Mathf.Clamp01(ring + fill) * Mathf.Clamp01((1f - r) * 12f);
                px[y * size + x] = new Color32(255, 255, 255, B(a));
            }
            Finish(tex, px, "vfx_ring");
            return tex;
        }

        /// <summary>
        /// Bullet hole (alpha blended, coloured): black hole, scorched ring, chipped light rim, soot streaks.
        /// </summary>
        public static Texture2D BulletHole(int size)
        {
            var tex = New(size, size);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float ang = Mathf.Atan2(ny, nx);
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float jag = Mathf.PerlinNoise(ca * 2.5f + 3.1f, sa * 2.5f + 8.4f);

                // Layer 1: soot streaks.
                float spokes = Mathf.Pow(Mathf.PerlinNoise(ca * 4f + 11f, sa * 4f + 2f), 3f) * 2.2f;
                float soot = Mathf.Clamp01(spokes * Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f)) * 0.55f;
                Color col = new Color(0.09f, 0.08f, 0.07f, soot);

                // Layer 2: chipped light rim (plaster / stone dust).
                float rimR = 0.36f + (jag - 0.5f) * 0.12f;
                float rk = (r - rimR) / 0.07f;
                float rim = Mathf.Exp(-rk * rk) * (0.35f + 0.65f * Mathf.PerlinNoise(nx * 9f + 4f, ny * 9f + 1f));
                col = Over(col, new Color(0.7f, 0.66f, 0.58f, rim * 0.75f));

                // Layer 3: scorched ring.
                float scorchR = 0.3f + (jag - 0.5f) * 0.08f;
                float scorch = Mathf.Clamp01((scorchR - r) / 0.1f);
                col = Over(col, new Color(0.13f, 0.11f, 0.09f, scorch * 0.9f));

                // Layer 4: the hole.
                float holeR = 0.14f + (jag - 0.5f) * 0.05f;
                float hole = Mathf.Clamp01((holeR - r) / 0.035f);
                col = Over(col, new Color(0.02f, 0.018f, 0.015f, hole));

                px[y * size + x] = new Color32(B(col.r), B(col.g), B(col.b), B(col.a));
            }
            Finish(tex, px, "vfx_bullethole");
            return tex;
        }

        static Color Over(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 1e-4f) return new Color(0f, 0f, 0f, 0f);
            Color rgb = (src * src.a + dst * dst.a * (1f - src.a)) / a;
            rgb.a = a;
            return rgb;
        }

        /// <summary>
        /// Tracer glow for a LineRenderer in Stretch mode: U along the line (0 = head), V across.
        /// White-hot core, warm halo, bright head that tapers toward the tail.
        /// </summary>
        public static Texture2D TracerGlow(int along, int across)
        {
            var tex = New(along, across);
            var px = new Color32[along * across];
            for (int y = 0; y < across; y++)
            for (int x = 0; x < along; x++)
            {
                float u = x / (float)(along - 1);
                float v = (y / (float)(across - 1)) * 2f - 1f;
                float core = Mathf.Exp(-v * v * 70f);
                float halo = Mathf.Exp(-v * v * 5f) * 0.4f;
                float tail = Mathf.Lerp(1f, 0.3f, u);
                float head = Mathf.Exp(-u * u * 260f) * Mathf.Exp(-v * v * 9f) * 0.8f;
                float a = Mathf.Clamp01((core + halo) * tail + head);
                float hot = Mathf.Clamp01(core * 1.4f + head);
                px[y * along + x] = new Color32(255, B(0.62f + 0.38f * hot), B(0.32f + 0.68f * hot), B(a));
            }
            Finish(tex, px, "vfx_tracer");
            return tex;
        }

        /// <summary>
        /// Three grey levels in a 4×1 strip for faux-lit debris cubes (top / sides / bottom) with an unlit,
        /// vertex-coloured particle material, so chips tint to the surface colour and still read as 3D.
        /// </summary>
        public static Texture2D ShadeStrip()
        {
            var tex = New(4, 1, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color32[]
            {
                new Color32(255, 255, 255, 255), // top
                new Color32(200, 200, 200, 255), // light side
                new Color32(150, 150, 150, 255), // dark side
                new Color32(95, 95, 95, 255),    // bottom
            };
            Finish(tex, px, "vfx_shade");
            return tex;
        }
    }
}
