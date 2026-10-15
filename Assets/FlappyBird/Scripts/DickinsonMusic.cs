using UnityEngine;

namespace FlappyBird
{
    // Original instrumental variations inspired by the poem's singing bird.
    // Owned by the game, independent of bird physics and the death/recovery state.
    [RequireComponent(typeof(AudioSource))]
    public sealed class DickinsonMusic : MonoBehaviour
    {
        private AudioSource source;
        // Themes: 0 Gale, 1 Warmth, 2 Sea, 3 the boss fight.
        public const int BossTheme = 3;
        private readonly AudioClip[] themes = new AudioClip[4];
        private int level = -1, pendingLevel = -1;
        private const int Silent = -2;
        private float fade = 1;
        private const float BaseVolume = 0.35f, FadeTime = 0.8f;
        public int CurrentBeat => source != null && source.isPlaying && !Muted && level >= 0
            ? Mathf.FloorToInt(source.time / BeatDuration(level)) : -1;
        private static float BeatDuration(int theme) => theme == 0 ? 0.48f : theme == 1 ? 0.6f : theme == 2 ? 0.68f : 0.36f;
        public float BeatLength => BeatDuration(level >= 0 ? level : BossTheme);
        // Where the music is, in seconds (real time when nothing is playing), for things that move to the beat.
        public float SongTime => source != null && source.isPlaying ? source.time : Time.unscaledTime;
        // Mute silences everything (music and sound effects) through the listener.
        public bool Muted => AudioListener.volume <= 0;

        public void Initialize()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = BaseVolume;
            source.ignoreListenerPause = true;
            if (FindAnyObjectByType<AudioListener>() == null)
                Camera.main.gameObject.AddComponent<AudioListener>();
        }

        // Fades the music out and stops it.
        public void Silence()
        {
            if (source.isPlaying) pendingLevel = Silent;
        }

        // Fades the current theme out before the new one starts.
        public void SetLevel(int index)
        {
            if (index == level && source.isPlaying) { pendingLevel = -1; return; }
            if (!source.isPlaying || level < 0) Switch(index);
            else pendingLevel = index;
        }

        private void Switch(int index)
        {
            level = index;
            if (themes[index] == null) themes[index] = Compose(index);
            source.clip = themes[index];
            source.Play();
        }

        private void Update()
        {
            float step = Time.unscaledDeltaTime / FadeTime;
            if (pendingLevel != -1)
            {
                fade -= step;
                if (fade <= 0)
                {
                    fade = 0;
                    if (pendingLevel == Silent) { source.Stop(); level = -1; }
                    else Switch(pendingLevel);
                    pendingLevel = -1;
                }
            }
            else fade = Mathf.Min(1, fade + step);
            source.volume = BaseVolume * fade;
        }

        private static AudioClip Compose(int theme)
        {
            if (theme == BossTheme) return ComposeBoss();
            const int sampleRate = 22050;
            float beat = BeatDuration(theme);
            const int beats = 48;
            int count = Mathf.RoundToInt(beats * beat * sampleRate);
            float[] samples = new float[count];
            int[] melody = { 0, 4, 7, 9, 7, 4, 2, 4, 7, 4, 2, 0,
                4, 7, 12, 9, 7, 4, 2, 7, 4, 2, 0, 0 };
            int[] roots = { 0, 0, 5, 5, 7, 7, 0, 0 };
            float tonic = theme == 0 ? 220f : theme == 1 ? 261.6256f : 196f;
            for (int note = 0; note < beats; note++)
            {
                int start = Mathf.RoundToInt(note * beat * sampleRate);
                int duration = Mathf.RoundToInt(beat * 1.8f * sampleRate);
                float pitch = tonic * Mathf.Pow(2, melody[note % melody.Length] / 12f);
                int root = roots[note / 6];
                float bass = tonic * 0.5f * Mathf.Pow(2, root / 12f);
                float harmony = bass * Mathf.Pow(2, (note % 3 == 0 ? 0 : note % 3 == 1 ? 7 : 12) / 12f);
                for (int j = 0; j < duration; j++)
                {
                    float t = j / (float)sampleRate;
                    float envelope = Mathf.Min(1, t / 0.025f) * Mathf.Exp(-t * (theme == 1 ? 3.8f : 2.8f))
                        * Mathf.Clamp01((duration - j) / (sampleRate * 0.08f));
                    float phase = 2 * Mathf.PI * pitch * t;
                    float birdSong = Mathf.Sin(phase + 0.018f * Mathf.Sin(t * 2 * Mathf.PI * 5))
                        + 0.2f * Mathf.Sin(phase * 2) + 0.07f * Mathf.Sin(phase * 3);
                    float accompaniment = Mathf.Sin(2 * Mathf.PI * harmony * t)
                        + (note % 3 == 0 ? 0.5f * Mathf.Sin(2 * Mathf.PI * bass * t) : 0);
                    // Wrap the ringing tail to make the loop seamless.
                    samples[(start + j) % count] += envelope * (birdSong * 0.13f + accompaniment * 0.065f);
                }
            }
            AudioClip clip = AudioClip.Create("Hope - " + new[] { "Gale", "Warmth", "Sea" }[theme], count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // The boss theme: fast and minor, a driving bass under a climbing arpeggio. Original music.
        private static AudioClip ComposeBoss()
        {
            const int sampleRate = 22050;
            float eighth = BeatDuration(BossTheme) / 2;
            int[] melody = {
                0, 3, 7, 10, 12, 10, 7, 3,   5, 8, 12, 8, 7, 3, 2, 3,
                0, 3, 7, 12, 15, 12, 10, 7,  8, 7, 5, 3, 2, 3, 5, 7,
                -4, 0, 3, 8, 12, 8, 3, 0,    -2, 2, 5, 10, 14, 10, 5, 2,
                0, 3, 7, 12, 15, 17, 15, 12, 10, 7, 5, 3, 2, -1, 2, 7
            };
            int[] roots = { 0, 0, 0, 0, -4, -2, 0, -5 }; // one per 8 eighths
            const float tonic = 220f;
            int count = Mathf.RoundToInt(melody.Length * eighth * sampleRate);
            float[] samples = new float[count];
            var hiss = new System.Random(3);
            for (int note = 0; note < melody.Length; note++)
            {
                int start = Mathf.RoundToInt(note * eighth * sampleRate);
                int length = Mathf.RoundToInt(eighth * 1.6f * sampleRate);
                float pitch = tonic * Mathf.Pow(2, melody[note] / 12f);
                float bass = tonic * 0.25f * Mathf.Pow(2, roots[note / 8] / 12f);
                for (int j = 0; j < length; j++)
                {
                    float t = j / (float)sampleRate;
                    float envelope = Mathf.Min(1, t / 0.006f) * Mathf.Exp(-t * 9) * Mathf.Clamp01((length - j) / (sampleRate * 0.02f));
                    float phase = Mathf.Repeat(pitch * t, 1);
                    // A square-ish lead, softened with a sine.
                    float lead = (phase < 0.5f ? 0.6f : -0.6f) + 0.5f * Mathf.Sin(2 * Mathf.PI * pitch * t);
                    float low = Mathf.Repeat(bass * t, 1) * 2 - 1;
                    float pulse = note % 2 == 0 ? 1 : 0.55f; // bass on every eighth, heavier on the beat
                    float hat = note % 2 == 1 && t < 0.03f ? ((float)hiss.NextDouble() * 2 - 1) * (1 - t / 0.03f) : 0;
                    samples[(start + j) % count] += envelope * (lead * 0.08f + low * 0.11f * pulse) + hat * 0.05f;
                }
            }
            AudioClip clip = AudioClip.Create("Hope - Dickinson", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in themes) if (clip != null) Destroy(clip);
        }
    }
}
