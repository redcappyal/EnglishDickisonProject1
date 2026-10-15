using System.Collections.Generic;
using UnityEngine;

namespace FlappyBird
{
    // Loads the pixel-art sprites in Resources/DickinsonSprites (made by Art/make_sprites.py).
    // Every lookup returns null when a file is missing, so the game keeps its placeholder blocks.
    public static class PixelArt
    {
        public const float PixelsPerUnit = 28f;
        // Pipe sprites keep this many rows of plain body, which repeats to any length.
        private const int PipeBodyRows = 18;
        // Rows of drips/icicles that hang below the top pipe's gap edge.
        public const int TopPipeOverhangRows = 5;
        // Background art: rows of sky above the row where its ground starts.
        private const int BackgroundSkyRows = 200;
        private static readonly string[] LevelFolders = { "level1_hope", "level2_gale", "level3_extremity" };
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Bird(string frame) => Load("bird/bird_" + frame, new Vector2(0.5f, 0.5f));

        // Decorations shared by all levels: nest, storm cloud, lightning, notes.
        public static Sprite Prop(string name, Vector2 pivot) => Load("props/" + name, pivot);

        // The nest's tree (Art/make_sprites.py: TREE_*). Both layers share one canvas: the trunk
        // pivots on the base of the trunk, the crown on the point where it meets the trunk (so the
        // storm can rock it there).
        private const float TreeWidth = 128, TreeHeight = 200, TreeBaseX = 68;
        public const float TreeCrownRows = 135;
        public static Sprite Tree(int level) => Prop("tree_" + level, new Vector2(TreeBaseX / TreeWidth, 0));
        public static Sprite TreeCrown(int level) => Prop("crown_" + level, new Vector2(TreeBaseX / TreeWidth, TreeCrownRows / TreeHeight));

        // Pivot on the top edge of the ground's grass line.
        public static Sprite Ground(int level) => Load(Folder(level) + "/ground", new Vector2(0.5f, 1));

        // Scaled so the sky exactly spans skyHeight world units; pivot on the art's horizon row.
        public static Sprite Background(int level, float skyHeight)
        {
            Texture2D texture = Texture(Folder(level) + "/background");
            if (texture == null) return null;
            float horizon = (texture.height - BackgroundSkyRows) / (float)texture.height;
            return Load(Folder(level) + "/background", new Vector2(0.5f, horizon), BackgroundSkyRows / skyHeight);
        }

        // Tiled sprites: the cap end is a fixed border and the body rows repeat.
        public static Sprite TopPipe(int level)
        {
            Texture2D texture = Texture(Folder(level) + "/pipe_top");
            return texture == null ? null : Load(Folder(level) + "/pipe_top", new Vector2(0.5f, 1), PixelsPerUnit,
                new Vector4(0, texture.height - PipeBodyRows, 0, 0));
        }

        public static Sprite BottomPipe(int level)
        {
            Texture2D texture = Texture(Folder(level) + "/pipe_bottom");
            return texture == null ? null : Load(Folder(level) + "/pipe_bottom", new Vector2(0.5f, 0), PixelsPerUnit,
                new Vector4(0, 0, 0, texture.height - PipeBodyRows));
        }

        // A sprite on its own child object - the building block for everything drawn in the world.
        public static SpriteRenderer Piece(string label, Transform parent, Sprite sprite, Vector2 position, Vector2 scale, Color color, int order)
        {
            SpriteRenderer piece = new GameObject(label, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = new Vector3(scale.x, scale.y, 1);
            piece.sprite = sprite;
            piece.color = color;
            piece.sortingOrder = order;
            return piece;
        }

        // A sprite that repeats to fill whatever size it's given (ground, backdrops, pipes).
        public static SpriteRenderer Tiled(string label, Transform parent, int order)
        {
            SpriteRenderer piece = Piece(label, parent, null, Vector2.zero, Vector2.one, Color.white, order);
            piece.drawMode = SpriteDrawMode.Tiled;
            return piece;
        }

        private static string Folder(int level) => LevelFolders[Mathf.Clamp(level, 0, LevelFolders.Length - 1)];

        private static Texture2D Texture(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>("DickinsonSprites/" + path);
            if (texture != null) texture.filterMode = FilterMode.Point;
            return texture;
        }

        private static Sprite Load(string path, Vector2 pivot, float pixelsPerUnit = PixelsPerUnit, Vector4 border = default)
        {
            string key = $"{path}|{pivot}|{pixelsPerUnit}|{border}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Texture2D texture = Texture(path);
            if (texture == null) return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot,
                pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = path;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
