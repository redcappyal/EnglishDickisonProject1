using System;
using UnityEngine;

namespace FlappyBird
{
    public enum Sfx { Point, Crash, Heart, Thunder, Gust, Click, Word, Voice, Hurt, Charge, Beam, Cut, Win }

    // Short sound effects, synthesized in code like the music so there are no audio files to import.
    // A few sources take turns, so sounds overlap and each can have its own pitch.
    public sealed class SoundFx : MonoBehaviour
    {
        private const int Rate = 22050;
        private readonly AudioSource[] sources = new AudioSource[6];
        private AudioClip[] clips;
        private int next;
        private readonly System.Random noise = new System.Random(7);

        public void Initialize()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0;
                sources[i].ignoreListenerPause = true;
            }
            clips = new AudioClip[Enum.GetValues(typeof(Sfx)).Length];
            clips[(int)Sfx.Point] = Make("Point", 0.16f, t => Tone(t < 0.06f ? 988 : 1319, t) * Env(t < 0.06f ? t : t - 0.06f, 0.003f, 0.08f) * 0.3f);
            clips[(int)Sfx.Crash] = Make("Crash", 0.4f, t => Noise() * Env(t, 0.002f, 0.12f) * 0.6f + Chirp(t, 160, 50, 0.4f) * Env(t, 0.002f, 0.3f) * 0.7f);
            clips[(int)Sfx.Heart] = Make("Heart", 0.42f, t =>
            {
                float note = t < 0.08f ? 523 : t < 0.16f ? 659 : 784;
                float since = t < 0.08f ? t : t < 0.16f ? t - 0.08f : t - 0.16f;
                return (Tone(note, t) + 0.3f * Tone(note * 2, t)) * Env(since, 0.004f, t < 0.16f ? 0.1f : 0.26f) * 0.3f;
            });
            float rumble = 0;
            clips[(int)Sfx.Thunder] = Make("Thunder", 1.4f, t =>
            {
                rumble += (Noise() - rumble) * 0.06f; // a low, rolling rumble under a sharp crack
                return (rumble * 2.2f * Env(t, 0.01f, 0.9f) + Noise() * Env(t, 0.001f, 0.05f) * 0.5f) * 0.7f;
            });
            float wind = 0;
            clips[(int)Sfx.Gust] = Make("Gust", 0.7f, t =>
            {
                wind += (Noise() - wind) * 0.15f;
                return wind * Mathf.Sin(Mathf.PI * t / 0.7f) * 0.9f;
            });
            clips[(int)Sfx.Click] = Make("Click", 0.035f, t => Square(1100, t) * Env(t, 0.001f, 0.02f) * 0.18f);
            clips[(int)Sfx.Word] = Make("Word", 0.2f, t => (Tone(660, t) + 0.25f * Tone(1320, t)) * Env(t, 0.002f, 0.09f) * 0.22f);
            clips[(int)Sfx.Voice] = Make("Voice", 0.05f, t => Square(290, t) * Env(t, 0.003f, 0.03f) * 0.16f);
            clips[(int)Sfx.Hurt] = Make("Hurt", 0.26f, t => Square(Mathf.Lerp(420, 140, t / 0.26f), t) * Env(t, 0.002f, 0.18f) * 0.22f);
            clips[(int)Sfx.Charge] = Make("Charge", 0.6f, t => Chirp(t, 260, 1250, 0.6f) * (0.6f + 0.4f * Mathf.Sin(t * 90)) * Mathf.Min(1, t / 0.05f) * 0.2f);
            clips[(int)Sfx.Beam] = Make("Beam", 0.55f, t => (Noise() * 0.5f + Saw(110, t) * 0.6f + Saw(165, t) * 0.3f) * Env(t, 0.004f, 0.4f) * 0.4f);
            clips[(int)Sfx.Cut] = Make("Cut", 0.06f, t => Noise() * Env(t, 0.001f, 0.025f) * 0.3f);
            clips[(int)Sfx.Win] = Make("Win", 1.4f, t =>
            {
                float[] notes = { 523, 659, 784, 1047 };
                float sum = 0;
                for (int i = 0; i < notes.Length; i++)
                {
                    float since = t - i * 0.12f;
                    if (since > 0) sum += (Tone(notes[i], t) + 0.3f * Tone(notes[i] * 2, t)) * Env(since, 0.005f, 0.8f);
                }
                return sum * 0.16f;
            });
        }

        public void Play(Sfx sound, float volume = 1, float pitch = 1)
        {
            if (clips == null) return;
            AudioSource source = sources[next];
            next = (next + 1) % sources.Length;
            source.pitch = pitch;
            source.PlayOneShot(clips[(int)sound], volume);
        }

        private float Noise() => (float)noise.NextDouble() * 2 - 1;
        private static float Tone(float hz, float t) => Mathf.Sin(2 * Mathf.PI * hz * t);
        private static float Square(float hz, float t) => Mathf.Repeat(hz * t, 1) < 0.5f ? 1 : -1;
        private static float Saw(float hz, float t) => Mathf.Repeat(hz * t, 1) * 2 - 1;
        // A tone sliding from one pitch to another over the clip (the phase is integrated, so no clicks).
        private static float Chirp(float t, float from, float to, float length) =>
            Mathf.Sin(2 * Mathf.PI * (from * t + (to - from) * t * t / (2 * length)));
        // Quick attack, exponential decay.
        private static float Env(float t, float attack, float decay) => t < 0 ? 0 : Mathf.Min(1, t / attack) * Mathf.Exp(-t / decay);

        private static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                // A short fade at the end so nothing clicks off.
                float fadeOut = Mathf.Clamp01((count - i) / (Rate * 0.01f));
                samples[i] = Mathf.Clamp(wave(i / (float)Rate) * fadeOut, -1, 1);
            }
            AudioClip clip = AudioClip.Create("Sfx - " + name, count, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (clips == null) return;
            foreach (AudioClip clip in clips) if (clip != null) Destroy(clip);
        }
    }
}
