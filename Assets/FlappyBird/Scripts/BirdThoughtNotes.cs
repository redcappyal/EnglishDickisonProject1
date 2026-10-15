using UnityEngine;

namespace FlappyBird
{
    // Decorative notes follow the soundtrack's beats, independent of bird recovery.
    public sealed class BirdThoughtNotes : MonoBehaviour
    {
        private sealed class Note
        {
            public Transform Root;
            public SpriteRenderer[] Pieces;
            public Vector3 Origin;
            public float Born;
            public float Phase;
        }

        private readonly Note[] notes = new Note[8];
        private Transform bird;
        private DickinsonMusic music;
        private Sprite headSprite;
        private Texture2D headTexture;
        private int lastBeat = -1;
        private int nextNote;
        private const float Lifetime = 2.4f;
        // While resting, no new notes appear (they'd be mistaken for the boss's shots).
        public bool Resting { get; set; }

        public void Initialize(Transform singer, DickinsonMusic soundtrack, Sprite square)
        {
            bird = singer;
            music = soundtrack;
            Sprite noteArt = PixelArt.Prop("note", new Vector2(0.5f, 0.5f));
            if (noteArt != null)
            {
                for (int i = 0; i < notes.Length; i++)
                {
                    Transform root = new GameObject("Bird's Thought Note").transform;
                    root.SetParent(transform, false);
                    notes[i] = new Note { Root = root, Pieces = new[] { Piece(root, noteArt, Vector2.zero, Vector2.one) }, Phase = i * 1.7f };
                    root.gameObject.SetActive(false);
                }
                return;
            }
            headTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            headTexture.filterMode = FilterMode.Bilinear;
            headTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    headTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(14.5f - distance)));
                }
            headTexture.Apply();
            headSprite = Sprite.Create(headTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32);
            for (int i = 0; i < notes.Length; i++)
            {
                Transform root = new GameObject("Bird's Thought Note").transform;
                root.SetParent(transform, false);
                SpriteRenderer head = Piece(root, headSprite, Vector2.zero, new Vector2(0.2f, 0.13f));
                head.transform.localRotation = Quaternion.Euler(0, 0, 22);
                SpriteRenderer stem = Piece(root, square, new Vector2(0.075f, 0.16f), new Vector2(0.035f, 0.33f));
                SpriteRenderer flag = Piece(root, square, new Vector2(0.13f, 0.27f), new Vector2(0.15f, 0.04f));
                flag.transform.localRotation = Quaternion.Euler(0, 0, -35);
                flag.enabled = i % 2 == 0;
                notes[i] = new Note { Root = root, Pieces = new[] { head, stem, flag }, Phase = i * 1.7f };
                root.gameObject.SetActive(false);
            }
        }

        private static SpriteRenderer Piece(Transform parent, Sprite sprite, Vector2 position, Vector2 size) =>
            PixelArt.Piece("Note Piece", parent, sprite, position, size, Color.white, 12);

        private void Update()
        {
            if (bird == null || music == null) return;
            int beat = music.CurrentBeat;
            if (beat >= 0 && beat != lastBeat && !Resting)
            {
                Note note = notes[nextNote];
                nextNote = (nextNote + 1) % notes.Length;
                note.Origin = bird.position + new Vector3(0.35f, 0.3f, 0);
                note.Born = Time.unscaledTime;
                note.Root.position = note.Origin;
                note.Root.gameObject.SetActive(true);
            }
            lastBeat = beat;
            foreach (Note note in notes)
            {
                if (!note.Root.gameObject.activeSelf) continue;
                float age = Time.unscaledTime - note.Born;
                if (age >= Lifetime) { note.Root.gameObject.SetActive(false); continue; }
                note.Root.position = note.Origin + new Vector3(age * 0.35f + Mathf.Sin(age * 3 + note.Phase) * age * 0.07f, age * 0.65f, 0);
                note.Root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 2 + note.Phase) * 12);
                note.Root.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, age / Lifetime);
                Color color = new Color(1f, 0.94f, 0.62f, Mathf.Clamp01(age / 0.12f) * (1 - age / Lifetime));
                foreach (SpriteRenderer piece in note.Pieces) piece.color = color;
            }
        }

        private void OnDestroy()
        {
            if (headSprite != null) Destroy(headSprite);
            if (headTexture != null) Destroy(headTexture);
        }
    }
}
