using UnityEngine;

namespace FlappyBird
{
    // Decorative only: no colliders, damage, or changes to bird physics.
    public sealed class FollowingStorm : MonoBehaviour
    {
        private const float SafeDistance = 1.8f;
        private FlappyBirdGame game;
        private Transform[] rain;
        private GameObject lightning;
        private Texture2D cloudTexture;
        private Sprite cloudSprite;
        private float animationTime;
        private bool leaving;
        private float surge;

        public void Initialize(FlappyBirdGame owner, Sprite square)
        {
            game = owner;
            Sprite cloudArt = PixelArt.Prop("storm_cloud", new Vector2(0.5f, 0.5f));
            if (cloudArt != null) Piece("Cloud", cloudArt, new Vector2(0, 0.35f), Vector2.one, Color.white, 0);
            else
            {
                cloudTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                cloudTexture.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                        cloudTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - distance)));
                    }
                cloudTexture.Apply();
                cloudSprite = Sprite.Create(cloudTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
                Color dark = new Color(0.22f, 0.25f, 0.34f);
                Piece("Cloud Base", cloudSprite, new Vector2(0, 0.15f), new Vector2(1.65f, 0.62f), dark, 0);
                Piece("Cloud Left", cloudSprite, new Vector2(-0.5f, 0.33f), new Vector2(0.68f, 0.67f), dark, 0);
                Piece("Cloud Crown", cloudSprite, new Vector2(-0.1f, 0.53f), new Vector2(0.88f, 0.82f), dark, 0);
                Piece("Cloud Right", cloudSprite, new Vector2(0.43f, 0.37f), new Vector2(0.76f, 0.72f), dark, 0);
            }

            rain = new Transform[12];
            for (int i = 0; i < rain.Length; i++)
            {
                rain[i] = Piece("Rain", square, Vector2.zero, new Vector2(0.025f, 0.16f), new Color(0.67f, 0.86f, 1, 0.75f), 1).transform;
                rain[i].localRotation = Quaternion.Euler(0, 0, -15);
            }
            Sprite boltArt = PixelArt.Prop("bolt", new Vector2(0.5f, 1));
            if (boltArt != null)
            {
                lightning = Piece("Lightning", boltArt, new Vector2(0.05f, 0.02f), Vector2.one, Color.white, 2);
                ResetStorm();
                return;
            }
            lightning = new GameObject("Lightning");
            lightning.transform.SetParent(transform, false);
            Vector2[] points = { new Vector2(0.12f, 0.12f), new Vector2(-0.07f, -0.24f), new Vector2(0.18f, -0.2f), new Vector2(-0.13f, -0.62f) };
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 direction = points[i + 1] - points[i];
                GameObject segment = Piece("Bolt", square, (points[i] + points[i + 1]) / 2,
                    new Vector2(0.055f, direction.magnitude), new Color(1, 0.93f, 0.5f), 2);
                segment.transform.SetParent(lightning.transform, false);
                segment.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
            }
            ResetStorm();
        }

        private GameObject Piece(string label, Sprite sprite, Vector2 position, Vector2 size, Color color, int order) =>
            PixelArt.Piece(label, transform, sprite, position, size, color, order).gameObject;

        // Start off-screen left; the normal follow eases it in.
        public void EnterFromLeft()
        {
            leaving = false;
            transform.position = new Vector3(-game.HalfWidth - 2.5f, game.Bird.transform.position.y, 0);
        }

        // 0..1: the Gale's storm surge. The cloud swells, hangs back a little further
        // (it is bigger, not closer) and flashes lightning more often.
        public void Surge(float amount)
        {
            surge = amount;
            transform.localScale = Vector3.one * (1 + 1.2f * amount);
        }

        // Drift away to the left (the game hides it once it is gone).
        public void Leave() { leaving = true; lightning.SetActive(false); }

        public void ResetStorm()
        {
            leaving = false;
            animationTime = 0;
            transform.position = game.Bird.transform.position + Vector3.left * SafeDistance;
            lightning.SetActive(false);
            AnimateRain();
        }

        private void LateUpdate()
        {
            if (game == null || !game.WorldMoving) return;
            animationTime += Time.deltaTime;
            if (leaving)
            {
                transform.position += Vector3.left * 3 * Time.deltaTime;
                AnimateRain();
                return;
            }
            Vector3 bird = game.Bird.transform.position;
            float distance = SafeDistance + 0.9f * surge;
            Vector3 target = bird + Vector3.left * distance;
            Vector3 position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-3 * Time.deltaTime));
            // Clamp every frame: even rapid movement cannot close the safety gap.
            position.x = Mathf.Min(position.x, bird.x - distance);
            transform.position = position;
            AnimateRain();
            float period = Mathf.Lerp(3.6f, 1.4f, surge);
            float flash = Mathf.Repeat(animationTime, period) - (period - 0.5f);
            lightning.SetActive(flash > 0 && flash < 0.14f || flash > 0.22f && flash < 0.3f);
        }

        private void AnimateRain()
        {
            for (int i = 0; i < rain.Length; i++)
            {
                float fall = Mathf.Repeat(animationTime * 1.7f + i * 0.137f, 1);
                rain[i].localPosition = new Vector3(-0.65f + (i % 6) * 0.24f - fall * 0.08f, -0.05f - fall * 1.1f, 0);
            }
        }

        private void OnDestroy()
        {
            if (cloudSprite != null) Destroy(cloudSprite);
            if (cloudTexture != null) Destroy(cloudTexture);
        }
    }
}
