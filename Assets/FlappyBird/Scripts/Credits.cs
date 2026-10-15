using UnityEngine;

namespace FlappyBird
{
    // The credits roll: after the boss fight, and from Settings > Credits. Lines scroll up until
    // the last one reaches the middle of the screen and rests there.
    public static class Credits
    {
        // (text, size, colour: 0 white, 1 gold, 2 muted); null text is a gap.
        private static readonly (string Text, float Size, int Tone)[] Lines = {
            ("THE THING WITH FEATHERS", 5, 1), (null, 0, 0),
            ("A GAME BY", 2, 2), ("ALVIN, SHRIDHAR AND ISHAAN", 3, 0), (null, 0, 0),
            ("THE POEM", 2, 2), ("\"HOPE\" IS THE THING WITH FEATHERS", 3, 0), ("BY EMILY DICKINSON (1830 - 1886)", 2, 0), (null, 0, 0),
            ("STARRING", 2, 2), ("THE BIRD - AS HOPE", 3, 0), ("EMILY DICKINSON - AS HERSELF", 3, 0), (null, 0, 0),
            ("THE WORLDS", 2, 2), ("HOPE - THE GALE - WARMTH - EXTREMITY", 3, 0), (null, 0, 0),
            ("ART, MUSIC AND SOUND", 2, 2), ("PIXEL ART AND SYNTHESIZED MUSIC, ALL MADE IN CODE", 2, 0), (null, 0, 0),
            ("INSPIRED BY", 2, 2), ("FLAPPY BIRD BY DONG NGUYEN", 2, 0), ("UNDERTALE BY TOBY FOX", 2, 0),
            ("GEOMETRY DASH BY ROBERT TOPALA", 2, 0), (null, 0, 0),
            ("POEM TEXT", 2, 2), ("PUBLIC DOMAIN, VIA THE ACADEMY OF AMERICAN POETS", 2, 0), (null, 0, 0),
            ("MADE WITH UNITY 6 FOR HONORS AMERICAN LITERATURE", 2, 2), (null, 0, 0), (null, 0, 0),
            ("AND NEVER STOPS - AT ALL", 4, 1)
        };
        private const float Speed = 55, Gap = 30, Rest = 380, Hold = 3, Height = 800;
        private static readonly Color[] Tones = { Color.white, new Color(1f, 0.85f, 0.45f), new Color(0.86f, 0.82f, 0.9f) };

        public static float Duration => (Height + Lines.Length * Gap - Rest) / Speed + Hold;

        // GUI only. alpha fades the whole roll.
        public static void Draw(float width, float elapsed, float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha * previous.a);
            // A soft dark column behind the text, so it reads over anything.
            PixelUi.Fill(new Rect(Mathf.Round(width / 2 - 330), 0, 660, Height), new Color(0.04f, 0.03f, 0.08f, 0.35f));
            float scroll = Mathf.Min(elapsed * Speed, (Lines.Length - 1) * Gap + Height - Rest);
            for (int i = 0; i < Lines.Length; i++)
            {
                var (text, size, tone) = Lines[i];
                float y = Height + i * Gap - scroll;
                if (text == null || y < 90 || y > Height) continue;
                // Lines fade in at the bottom and out at the top.
                Color color = Tones[tone];
                color.a = Mathf.Clamp01((y - 90) / 80) * Mathf.Clamp01((Height - y) / 80);
                PixelUi.Text(text, width / 2, y, size, color, 0.5f);
            }
            GUI.color = previous;
        }
    }
}
