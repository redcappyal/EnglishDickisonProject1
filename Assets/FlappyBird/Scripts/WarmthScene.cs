using System.Collections.Generic;
using UnityEngine;

namespace FlappyBird
{
    // Deliberately rastered on a small integer grid, like the other Dickinson sprites.
    // No smoothing, generated images, or full-resolution painted textures are used.
    public sealed class WarmthScene : MonoBehaviour
    {
        private const int Width = 512;
        private const int Height = 256;
        private const int SkyRows = 200;
        public static Color SkyColor => Ink(216, 182, 190);

        private readonly List<Sprite> ownedSprites = new List<Sprite>();
        private readonly Dictionary<float, Sprite> backgrounds = new Dictionary<float, Sprite>();
        private Texture2D landscapeTexture;
        private Texture2D sunTexture;
        private SpriteRenderer sun;
        private float backgroundPixelSize = 9.3f / SkyRows;

        public void Initialize(Sprite square)
        {
            if (sun != null) return;
            sunTexture = MakeSun();
            Sprite sprite = Sprite.Create(sunTexture, new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f), PixelArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "Warmth - pixel sun";
            ownedSprites.Add(sprite);
            GameObject child = new GameObject("Rising Warmth Sun", typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            child.transform.localScale = Vector3.one * (backgroundPixelSize * PixelArt.PixelsPerUnit);
            sun = child.GetComponent<SpriteRenderer>();
            sun.sprite = sprite;
            sun.sortingOrder = -8;
            sun.color = new Color(1, 1, 1, 0);
        }

        public Sprite Background(float skyHeight)
        {
            skyHeight = Mathf.Max(0.01f, skyHeight);
            backgroundPixelSize = skyHeight / SkyRows;
            if (backgrounds.TryGetValue(skyHeight, out Sprite cached)) return cached;
            if (landscapeTexture == null) landscapeTexture = MakeLandscape();
            Sprite sprite = Sprite.Create(landscapeTexture, new Rect(0, 0, Width, Height),
                new Vector2(0.5f, (Height - SkyRows) / (float)Height), SkyRows / skyHeight,
                0, SpriteMeshType.FullRect);
            sprite.name = "Warmth - village panorama";
            ownedSprites.Add(sprite);
            backgrounds.Add(skyHeight, sprite);
            return sprite;
        }

        public void Animate(Camera camera, float visibility, float elapsed)
        {
            if (sun == null || camera == null) return;
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 3.6f));
            // The sun uses the same pixel size as the original background artwork.
            float pixel = backgroundPixelSize;
            sun.transform.localScale = Vector3.one * (pixel * PixelArt.PixelsPerUnit);
            float x = camera.transform.position.x + camera.orthographicSize * camera.aspect * 0.5f;
            float y = camera.transform.position.y + Mathf.Lerp(-1, 1.7f, rise);
            sun.transform.position = new Vector3(Mathf.Round(x / pixel) * pixel,
                Mathf.Round(y / pixel) * pixel, transform.position.z);
            sun.color = new Color(1, 1, 1, Mathf.Clamp01(visibility));
            sun.enabled = visibility > 0;
        }

        private static Texture2D MakeSun()
        {
            Color32[] pixels = new Color32[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int dx = x - 16;
                    int dy = y - 16;
                    // Same radius and integer circle rule as Hope's baked-in sun.
                    if (dx * dx + dy * dy <= 14 * 14 + 14)
                        pixels[y * 32 + x] = y < 7 ? Ink(255, 221, 119) : Ink(255, 230, 128);
                }
            }
            Grain(pixels, 17);
            return Texture("Warmth sun pixels", 32, 32, pixels);
        }

        private static Texture2D MakeLandscape()
        {
            Color32[] pixels = new Color32[Width * Height];
            // Like all three original backdrops, one flat sky carries the scene.
            Fill(pixels, 0, 0, Width, Height, (Color32)SkyColor);
            Cloud(pixels, 14, 70);
            Cloud(pixels, 70, 30);
            Cloud(pixels, 130, 110);
            Cloud(pixels, 204, 40);
            Cloud(pixels, 254, 90);
            Cloud(pixels, 360, 56);
            Cloud(pixels, 412, 104);
            Cloud(pixels, 470, 36);

            // Reuse Hope's round silhouettes and two green inks without extra terraces.
            Circle(pixels, 40, 226, 50, Ink(108, 191, 90));
            Circle(pixels, 128, 222, 46, Ink(90, 168, 74));
            Circle(pixels, 214, 215, 50, Ink(108, 191, 90));
            Circle(pixels, 304, 225, 55, Ink(90, 168, 74));
            Circle(pixels, 384, 220, 48, Ink(108, 191, 90));
            Circle(pixels, 470, 226, 52, Ink(90, 168, 74));

            // Three tiny hearths suggest the many kept warm while preserving empty sky.
            House(pixels, 112, 170);
            House(pixels, 224, 166);
            House(pixels, 410, 181);
            Chunky(pixels);
            Grain(pixels, 24);
            return Texture("Warmth village pixels", Width, Height, pixels);
        }

        private static void House(Color32[] pixels, int x, int y)
        {
            Color32 roof = Ink(192, 57, 43);
            Fill(pixels, x, y, x + 16, y + 12, Ink(160, 96, 58));
            for (int row = 0; row < 7; row++)
                Fill(pixels, x - 1 + row, y - 1 - row, x + 17 - row, y - row, roof);
            Fill(pixels, x + 12, y - 9, x + 15, y - 3, roof);
            Fill(pixels, x + 3, y + 3, x + 6, y + 6, Ink(255, 230, 128));
            Fill(pixels, x + 10, y + 3, x + 13, y + 6, Ink(255, 230, 128));
            Fill(pixels, x + 7, y + 6, x + 10, y + 12, Ink(74, 42, 26));
            for (int i = 0; i < 4; i++)
                Circle(pixels, x + 14 + i, y - 16 - i * 7, 2, Ink(238, 238, 238));
        }

        private static void Cloud(Color32[] pixels, int x, int y)
        {
            Color32 pale = Ink(255, 250, 235);
            Fill(pixels, x + 4, y, x + 14, y + 4, pale);
            Fill(pixels, x, y + 3, x + 22, y + 9, pale);
            Fill(pixels, x + 12, y - 2, x + 19, y + 4, pale);
        }

        private static void Circle(Color32[] pixels, int cx, int cy, int radius, Color32 color)
        {
            for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(Height - 1, cy + radius); y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius + radius)
                        pixels[(Height - 1 - y) * Width + (x % Width + Width) % Width] = color;
        }

        private static void Grain(Color32[] pixels, int seed)
        {
            // Match the other backgrounds (Art/make_sprites.py): 1.5% of pixels scuffed by +/-14,
            // then v1.7's rougher pass - another 1.2% shifted by +/-26 and every colour snapped
            // to a coarse palette - without changing silhouettes.
            System.Random random = new System.Random(seed);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                Color32 color = pixels[i];
                int shift = random.NextDouble() < 0.015 ? (random.Next(2) == 0 ? -14 : 14) : 0;
                if (random.NextDouble() < 0.012) shift += random.Next(2) == 0 ? -26 : 26;
                pixels[i] = Ink(Coarse(color.r + shift), Coarse(color.g + shift), Coarse(color.b + shift), color.a);
            }
        }

        // Half resolution, like the other backdrops (Art/make_sprites.py: CHUNKY).
        private static void Chunky(Color32[] pixels)
        {
            for (int y = 0; y + 1 < Height; y += 2)
                for (int x = 0; x + 1 < Width; x += 2)
                {
                    Color32 color = pixels[y * Width + x];
                    pixels[y * Width + x + 1] = pixels[(y + 1) * Width + x] = pixels[(y + 1) * Width + x + 1] = color;
                }
        }

        private static byte Coarse(int value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value / 24f) * 24, 0, 255);


        // Art coordinates start at the upper left, matching Art/make_sprites.py.
        private static void Fill(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(Height, y1); y++)
                for (int x = x0; x < x1; x++)
                    pixels[(Height - 1 - y) * Width + (x % Width + Width) % Width] = color;
        }

        private static Color32 Ink(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

        private static Texture2D Texture(string label, int width, int height, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = label;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in ownedSprites)
                if (sprite != null) Destroy(sprite);
            if (landscapeTexture != null) Destroy(landscapeTexture);
            if (sunTexture != null) Destroy(sunTexture);
        }
    }
}
