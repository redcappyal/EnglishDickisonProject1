using System.Collections.Generic;
using UnityEngine;

namespace FlappyBird
{
    // Immediate-mode pixel UI: a bitmap font matching the art, 9-slice frames and icons.
    // Frames and icons come from Resources/DickinsonSprites/ui; the font is built in code.
    // Call only from OnGUI. Sizes are in GUI units; Px is one art pixel.
    public static class PixelUi
    {
        public const float Px = 3f;

        // OnGUI runs several times a frame (layout, input, repaint); only repaint needs pixels.
        private static bool Painting => Event.current == null || Event.current.type == EventType.Repaint;
        public static readonly Color Outline = new Color(0.08f, 0.06f, 0.1f);

        private const string Chars = " !\"'(),-./0123456789:;?ABCDEFGHIJKLMNOPQRSTUVWXYZ|";
        private static readonly string[] Glyphs = {
            ".. .. .. .. ..", "# # # . #", "#.# #.# ... ... ...", "# # . . .", ".# #. #. #. .#", "#. .# .# .# #.",
            ". . . # #", "... ... ### ... ...", ". . . . #", "..# ..# .#. #.. #..", "### #.# #.# #.# ###", ".#. ##. .#. .#. ###",
            "##. ..# .#. #.. ###", "##. ..# .#. ..# ##.", "#.# #.# ### ..# ..#", "### #.. ##. ..# ##.", ".## #.. ### #.# ###", "### ..# .#. .#. .#.",
            "### #.# ### #.# ###", "### #.# ### ..# ##.", ". # . # .", ". # . # #", "##. ..# .#. ... .#.", ".#. #.# ### #.# #.#",
            "##. #.# ##. #.# ##.", ".## #.. #.. #.. .##", "##. #.# #.# #.# ##.", "### #.. ##. #.. ###", "### #.. ##. #.. #..", ".## #.. #.# #.# .##",
            "#.# #.# ### #.# #.#", "### .#. .#. .#. ###", "..# ..# ..# #.# .#.", "#.# #.# ##. #.# #.#", "#.. #.. #.. #.. ###", "#...# ##.## #.#.# #...# #...#",
            "#..# ##.# #.## #..# #..#", ".#. #.# #.# #.# .#.", "##. #.# ##. #.. #..", ".#. #.# #.# ##. .##", "##. #.# ##. #.# #.#", ".## #.. .#. ..# ##.",
            "### .#. .#. .#. .#.", "#.# #.# #.# #.# ###", "#.# #.# #.# #.# .#.", "#...# #...# #.#.# ##.## #...#", "#.# #.# .#. #.# #.#", "#.# #.# .#. .#. .#.",
            "### ..# .#. #.. ###", "# # # # #"
        };
        private static Texture2D atlas, white;
        private static readonly Dictionary<char, int> Index = new Dictionary<char, int>();
        private static readonly Dictionary<string, Texture2D> UiTextures = new Dictionary<string, Texture2D>();

        public static float TextWidth(string text, float size)
        {
            float width = 0;
            foreach (char raw in text) width += (GlyphWidth(Glyph(raw)) + 1) * size;
            return Mathf.Max(0, width - size);
        }

        // align: 0 = left, 0.5 = centre, 1 = right. y is the top of the text.
        public static void Text(string text, float x, float y, float size, Color color, float align = 0, bool outline = true)
        {
            if (!Painting || string.IsNullOrEmpty(text)) return;
            BuildAtlas();
            x = Mathf.Round(x - TextWidth(text, size) * align);
            y = Mathf.Round(y);
            Color previous = GUI.color;
            if (outline)
            {
                // One UI pixel thick at any text size, plus a drop shadow.
                float o = Mathf.Min(size, Px);
                GUI.color = new Color(Outline.r, Outline.g, Outline.b, color.a * previous.a);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0) DrawGlyphs(text, x + dx * o, y + dy * o, size);
                DrawGlyphs(text, x, y + 2 * o, size);
            }
            GUI.color = color * new Color(1, 1, 1, previous.a);
            DrawGlyphs(text, x, y, size);
            GUI.color = previous;
        }

        // Small print: no outline, just a one-pixel shadow.
        public static void SmallPrint(string text, float x, float y, Color color, float align = 0)
        {
            Text(text, x + 2, y + 2, 2, new Color(Outline.r, Outline.g, Outline.b, color.a * 0.8f), align, false);
            Text(text, x, y, 2, color, align, false);
        }

        public static void Fill(Rect rect, Color color)
        {
            if (!Painting) return;
            if (white == null)
            {
                white = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            Color previous = GUI.color;
            GUI.color = color * new Color(1, 1, 1, previous.a);
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        // Draws a ui/<name> texture as a 9-slice with the given border (in art pixels).
        public static void Frame(Rect rect, string name, int border = 3)
        {
            if (!Painting) return;
            Texture2D texture = Ui(name);
            if (texture == null) { Fill(rect, new Color(0.14f, 0.13f, 0.23f, 0.92f)); return; }
            float b = border * Px, u = border / (float)texture.width, v = border / (float)texture.height;
            float[] xs = { rect.xMin, rect.xMin + b, rect.xMax - b, rect.xMax };
            float[] ys = { rect.yMin, rect.yMin + b, rect.yMax - b, rect.yMax };
            float[] us = { 0, u, 1 - u, 1 };
            float[] vs = { 1, 1 - v, v, 0 }; // GUI y grows down, texture v grows up
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[i], ys[j], xs[i + 1], ys[j + 1]), texture,
                        Rect.MinMaxRect(us[i], vs[j + 1], us[i + 1], vs[j]));
        }

        // A ui texture repeated across a rect, shifted by offset (GUI units) - for walls that scroll.
        public static void Pattern(Rect rect, string name, Vector2 offset, float scale = Px)
        {
            if (!Painting || rect.width <= 0 || rect.height <= 0) return;
            Texture2D texture = Ui(name);
            if (texture == null) { Fill(rect, Outline); return; }
            texture.wrapMode = TextureWrapMode.Repeat;
            float w = texture.width * scale, h = texture.height * scale;
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect((rect.x + offset.x) / w, -(rect.yMax + offset.y) / h, rect.width / w, rect.height / h));
        }

        public static void Icon(string name, float x, float y, float scale = Px, bool flip = false)
        {
            if (!Painting) return;
            Texture2D texture = Ui(name);
            if (texture == null) return;
            Rect rect = new Rect(Mathf.Round(x), Mathf.Round(y), texture.width * scale, texture.height * scale);
            if (flip) GUI.DrawTextureWithTexCoords(rect, texture, new Rect(1, 0, -1, 1));
            else GUI.DrawTexture(rect, texture);
        }

        public static Vector2 IconSize(string name, float scale = Px)
        {
            Texture2D texture = Ui(name);
            return texture == null ? Vector2.zero : new Vector2(texture.width, texture.height) * scale;
        }

        private static Texture2D Ui(string name)
        {
            if (UiTextures.TryGetValue(name, out Texture2D cached) && cached != null) return cached;
            Texture2D texture = Resources.Load<Texture2D>("DickinsonSprites/ui/" + name);
            if (texture != null) texture.filterMode = FilterMode.Point;
            UiTextures[name] = texture;
            return texture;
        }

        private static int Glyph(char raw)
        {
            BuildAtlas();
            char c = char.ToUpperInvariant(raw);
            if (c == '\u2014' || c == '\u2013') c = '-';
            else if (c == '\u2019' || c == '\u2018') c = '\'';
            else if (c == '\u201C' || c == '\u201D') c = '"';
            return Index.TryGetValue(c, out int index) ? index : 0;
        }

        private static int GlyphWidth(int index)
        {
            int space = Glyphs[index].IndexOf(' ');
            return space > 0 ? space : Glyphs[index].Length;
        }

        private static void DrawGlyphs(string text, float x, float y, float size)
        {
            float cellWidth = 6f * Chars.Length;
            foreach (char raw in text)
            {
                int index = Glyph(raw);
                int width = GlyphWidth(index);
                GUI.DrawTextureWithTexCoords(new Rect(x, y, width * size, 5 * size), atlas,
                    new Rect(index * 6 / cellWidth, 0, width / cellWidth, 1));
                x += (width + 1) * size;
            }
        }

        private static void BuildAtlas()
        {
            if (atlas != null) return;
            atlas = new Texture2D(6 * Chars.Length, 5, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new Color(1, 1, 1, 0);
            for (int x = 0; x < atlas.width; x++)
                for (int y = 0; y < atlas.height; y++)
                    atlas.SetPixel(x, y, clear);
            Index.Clear();
            for (int i = 0; i < Chars.Length; i++)
            {
                Index[Chars[i]] = i;
                string[] rows = Glyphs[i].Split(' ');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                        if (rows[row][col] == '#') atlas.SetPixel(i * 6 + col, 4 - row, Color.white);
            }
            atlas.Apply();
        }
    }
}
