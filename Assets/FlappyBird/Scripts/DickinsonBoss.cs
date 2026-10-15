using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyBird
{
    // The finale: Emily Dickinson herself, fought the way a certain skeleton fights. She talks
    // between attacks in dashes and Capitals, and can't be hit back - the bird only has to keep
    // flying until she tires. Hits count as crashes and cost hope, but never end the run.
    // Her attacks, in order: Fascicles (stitched gates that open and shut on the beat), Blanks
    // (lines of the poem with a word missing - fly through the right one), the Wave (a slanted
    // tunnel flown by holding and letting go), Quills (laser beams), and her Letter to the World
    // (all of it at once, ending on the line the whole game argues: "and never stops - at all").
    public sealed class DickinsonBoss : MonoBehaviour
    {
        // A column the bird has to get through. Fascicle gates are stitched pages whose gap opens
        // and shuts in time with the music; blank walls have three word windows, and the two wrong
        // ones are inked shut just before the bird arrives.
        private sealed class Column
        {
            public float X, Speed, Phase, Age;
            public float[] GapY, Half;
            public bool Fascicle, Inked, Passed;
            public Blank Blank;
        }

        // A line of the poem with one word missing, and the words on offer.
        private sealed class Blank
        {
            public string Before, After;
            public string[] Words;
            public int Right;
            public float ShownAt = Time.time, FilledAt = -1, PassedAt = -1;
        }

        // A giant quill that slides in at the edge, charges, and fires a beam straight across.
        private sealed class Quill
        {
            public float Y, Born;
            public bool FromLeft, Charged, Fired;
        }

        private const float QuillIn = 0.35f, QuillFire = 0.45f, QuillOut = 0.3f, BeamHalf = 0.42f;
        private const float BirdRadius = 0.19f, Invulnerable = 1.2f, CharsPerSecond = 26;
        private const float ColumnHalf = 0.5f, WindowHalf = 0.72f, WaveIn = 0.9f, WaveGrace = 1.2f, WaveOut = 0.8f;
        private const float TurnSweep = 0.45f;

        // A Wave's shape. Its vertical speed always matches its scroll speed, so it flies at 45 degrees.
        // Each style is built from its own fixed seed, so a Wave is the same tunnel every time you
        // play it - learn it, like a level.
        private struct WaveStyle
        {
            public int Seed;
            public float Seconds, StartHalf, EndHalf, StartSpeed, EndSpeed, MinRun, MaxRun, Flats;
        }

        // The first Wave is a warm-up; the one in her Letter is built like an easy demon level:
        // narrow, fast and speeding up, with short runs and the odd flat stretch you have to tap through.
        private static readonly WaveStyle FirstWave = new WaveStyle
            { Seed = 1830, Seconds = 20, StartHalf = 1.4f, EndHalf = 1.1f, StartSpeed = 4.2f, EndSpeed = 4.2f, MinRun = 0.9f, MaxRun = 2.2f };
        private static readonly WaveStyle DemonWave = new WaveStyle
            { Seed = 1886, Seconds = 24, StartHalf = 0.82f, EndHalf = 0.66f, StartSpeed = 5.4f, EndSpeed = 6.6f, MinRun = 0.9f, MaxRun = 1.8f, Flats = 0.06f };
        private static readonly WaveStyle EasyWave = new WaveStyle
            { Seed = 1861, Seconds = 20, StartHalf = 1.7f, EndHalf = 1.45f, StartSpeed = 3.6f, EndSpeed = 3.6f, MinRun = 1f, MaxRun = 2.4f };
        private static readonly WaveStyle EasyLetterWave = new WaveStyle
            { Seed = 1890, Seconds = 18, StartHalf = 1.4f, EndHalf = 1.2f, StartSpeed = 4.2f, EndSpeed = 4.6f, MinRun = 0.9f, MaxRun = 2f };
        private const float WaveStartY = 0.3f; // the height the bird is set back to between attacks
        private const float Pixel = 2 / PixelArt.PixelsPerUnit; // she's drawn at twice the usual scale
        // The blanks: the line around the missing word, the right word, then two that don't belong.
        private static readonly string[][] BlankLines = {
            new[] { "\"HOPE\" IS THE THING WITH", "-", "FEATHERS", "WINGS", "WORDS" },
            new[] { "THAT PERCHES IN THE", "-", "SOUL", "TREE", "HEART" },
            new[] { "AND SINGS THE", "WITHOUT THE WORDS -", "TUNE", "SONG", "HYMN" },
            new[] { "AND SWEETEST - IN THE", "- IS HEARD -", "GALE", "RAIN", "DARK" },
            new[] { "IT ASKED A", "- OF ME.", "CRUMB", "WORD", "THING" }
        };
        private static readonly string[] LastLine = { "AND NEVER STOPS - AT", "-", "ALL", "ONCE", "LAST" };
        private static readonly Color Ink = new Color(0.23f, 0.14f, 0.09f), Glow = new Color(0.55f, 0.92f, 1f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.45f), Page = new Color(0.95f, 0.9f, 0.78f);
        private static readonly Color Thread = new Color(0.75f, 0.2f, 0.2f), Night = new Color(0.01f, 0.008f, 0.025f); // GUI colours are linear: this reads as deep night

        private FlappyBirdGame game;
        private DickinsonMusic music;
        private SoundFx sfx;
        private SpriteRenderer art;
        private Sprite idle, asleep, smiling;
        private readonly List<Column> columns = new List<Column>();
        private readonly List<Quill> quills = new List<Quill>();
        private Blank currentBlank;
        // The Wave: a tunnel along a zigzag path (track distance, height), and the bird's trail.
        private readonly List<Vector2> path = new List<Vector2>(), trail = new List<Vector2>();
        private bool waving;
        private float waveScroll, waveHalf, waveStart, waveY;
        private float waveSpeed, waveOpen, waveLength;
        // Easy mode (Settings) is read when the fight starts: gentler gates, slower beams, wider tunnels.
        private bool easy;
        private float quillCharge = 0.6f;
        // The page turn between attacks.
        private string turnTitle, turnNote;
        private float turnAt = -10, turnHold;

        // Talking: the bird hovers. Fighting: the bird flies under its own wings.
        private bool hovering, fighting, attacking, sleeping, waitingToPerch, perching, advance;
        private string line;
        private readonly List<string> lineRows = new List<string>();
        private float lineStart, hurtUntil = -10, cutUntil = -10, eyeAt = -10, dawnAt = -10, creditsAt = -10, creditsEnd = -10;
        private int blips;

        public bool Active { get; private set; }
        // The bird sits on her shoulder at the very end.
        public bool Perched { get; private set; }
        public bool Flickering => Active && Time.time < hurtUntil;
        public bool BirdStill => Perched || perching;
        // 0..1: how far the HUD is faded out for the credits.
        public float HudHide => creditsAt < 0 ? 0
            : creditsEnd > 0 ? Mathf.Clamp01(1 - (Time.time - creditsEnd) / 0.5f) : Mathf.Clamp01((Time.time - creditsAt) / 1f);

        private Camera View => Camera.main;
        private float Right => View.orthographicSize * View.aspect;
        private float Top => game.cameraHalfHeight;
        private float Floor => game.Floor;
        private float BirdX => game.Bird.transform.position.x;
        private float BirdY => game.Bird.transform.position.y;
        // She floats over the sea on the right, feet a little above the water.
        private Vector2 Home => new Vector2(Right - 1.9f, Floor + 2.3f);
        private int Shown => line == null ? 0 : Mathf.Min(line.Length, Mathf.FloorToInt((Time.time - lineStart) * CharsPerSecond));

        public void Initialize(FlappyBirdGame owner, DickinsonMusic soundtrack, SoundFx sounds)
        {
            game = owner;
            music = soundtrack;
            sfx = sounds;
            art = GetComponent<SpriteRenderer>();
            idle = PixelArt.Prop("dickinson", new Vector2(0.5f, 0));
            asleep = PixelArt.Prop("dickinson_sleep", new Vector2(0.5f, 0));
            smiling = PixelArt.Prop("dickinson_smile", new Vector2(0.5f, 0));
            art.sortingOrder = 4;
            transform.localScale = new Vector3(2, 2, 1);
            ResetBoss();
        }

        public void Begin()
        {
            ResetBoss();
            easy = game.EasyMode;
            quillCharge = easy ? 0.9f : 0.6f;
            Active = hovering = true;
            game.BeginHover();
            music.Silence();
            StartCoroutine(Fight());
        }

        public void ResetBoss()
        {
            StopAllCoroutines();
            Active = Perched = hovering = fighting = attacking = sleeping = waitingToPerch = perching = advance = false;
            ClearAttacks();
            line = null;
            hurtUntil = cutUntil = eyeAt = dawnAt = creditsAt = creditsEnd = turnAt = -10;
            art.enabled = false;
            art.sprite = idle;
        }

        private void ClearAttacks()
        {
            columns.Clear();
            quills.Clear();
            path.Clear();
            trail.Clear();
            currentBlank = null;
            waving = false;
        }

        // ---- The script ----------------------------------------------------------------------

        private IEnumerator Fight()
        {
            yield return Wait(1.6f); // the world glides to a stop and the music fades away
            yield return Cut(0.5f);
            art.enabled = true; // ...and out of the dark, she's simply there
            yield return Wait(0.8f);
            yield return Say("so -");
            yield return Say("you're the Thing - with Feathers -");
            yield return Say("i've heard you - you know -");
            yield return Say("in the chillest Land - and on the strangest Sea -");
            yield return Say("and not once - did you ask me - for a Crumb -");
            yield return Say("very polite.");
            yield return Say("but i wonder -");
            yield return Say("do you ever - Stop - at all?");
            eyeAt = Time.time;
            yield return Say("let's find out.");

            // 1. Fascicles: she sewed her poems into little books by hand.
            yield return Turn("I - FASCICLES", "WAIT FOR THE PAGES TO OPEN");
            music.SetLevel(DickinsonMusic.BossTheme);
            fighting = true;
            hovering = false;
            game.Bird.Flap();
            game.RestoreHope(100);
            yield return Talk("i sewed these - by hand - you know -");
            yield return Attack(Fascicles(12, easy ? 3.6f : 4.2f, easy ? 1.9f : 1.6f));
            yield return Talk("not bad - for a Bird -");
            // 2. Blanks: the poem, with a word missing.
            yield return Talk("let's see - if you know - the Words -");
            yield return Turn("II - BLANKS", "FLY THROUGH THE RIGHT WORD");
            yield return Attack(Blanks(easy ? 3f : 3.6f));
            yield return Talk("hm - you do - know them -");
            // 3. The Wave: a slanted tunnel.
            yield return Talk("tell all the truth - but tell it - Slant -");
            yield return Turn("III - THE WAVE", "HOLD TO RISE - LET GO TO FALL");
            yield return Attack(Wave(easy ? EasyWave : FirstWave));
            // 4. Quills.
            yield return Talk("this next one - is a real - Page-turner -");
            yield return Turn("IV - QUILLS", "KEEP OUT OF THE LIGHT");
            yield return Attack(Quills(10, easy ? 1.2f : 0.75f));
            // 5. Her Letter to the World: everything at once, pages flipping between, and then the
            // last line - the one the whole game argues.
            yield return Talk("alright. this is my Letter - to the World -");
            yield return Turn("V - MY LETTER TO THE WORLD", "EVERYTHING AT ONCE");
            yield return Attack(Fascicles(easy ? 5 : 6, easy ? 4.2f : 5.2f, easy ? 1.6f : 1.15f), Quills(easy ? 5 : 6, easy ? 2.2f : 1.4f));
            yield return Turn(null, null, 0.15f);
            yield return Attack(Wave(easy ? EasyLetterWave : DemonWave));
            yield return Turn(null, null, 0.15f);
            yield return Attack(Quills(easy ? 3 : 4, easy ? 0.8f : 0.45f));
            yield return Talk("finish it - for me -");
            yield return Turn(null, null, 0.15f);
            yield return Attack(OneBlank(LastLine, easy ? 3f : 3.4f));
            music.Silence();
            eyeAt = -10; // the glow goes out of her eye
            yield return Wait(1.2f);
            yield return Talk("...");
            yield return Talk("you're still - singing -");

            fighting = false;
            hovering = true;
            game.BeginHover();
            yield return Say("i wrote that - didn't i -");
            yield return Say("that you'd never stop - at all -");
            yield return Say("guess i was right.");
            yield return Say("i'll just - rest my Eyes - a moment -");
            sleeping = true;
            art.sprite = asleep;
            yield return Wait(1.2f);

            // The bird flies to her and perches on her shoulder.
            waitingToPerch = true;
            advance = false;
            while (!advance) yield return null;
            waitingToPerch = false;
            yield return Perch();
            yield return Wait(1.2f);
            sleeping = false;
            art.sprite = smiling;
            yield return Say("...oh.");
            yield return Say("you stayed.");
            yield return Say("that's Hope - i suppose.");
            yield return Say("go on then - sing me - one more -");
            music.SetLevel(1); // the warm theme from the first stanza
            sfx.Play(Sfx.Win);
            dawnAt = Time.time;
            yield return Wait(2.5f);
            yield return RollCredits();
            game.FinishBoss();
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) yield return null;
        }

        // A black cut, the way the fight jumps between attacks. Clears the screen.
        private IEnumerator Cut(float seconds)
        {
            cutUntil = Time.time + seconds;
            columns.Clear();
            quills.Clear();
            sfx.Play(Sfx.Cut);
            yield return Wait(seconds);
        }

        // Between attacks the page turns: ink sweeps in from the right with the next attack's name on
        // it, the bird is set gently back in the middle underneath, and the ink sweeps on out to the
        // left as the next attack begins. A turn with no title is a quick flip.
        private IEnumerator Turn(string title, string note, float hold = 0.8f)
        {
            // Let what's left of the last attack fly off first.
            for (float t = 0; t < 1.5f && (columns.Count > 0 || quills.Count > 0); t += Time.deltaTime) yield return null;
            turnTitle = title;
            turnNote = note;
            turnAt = Time.time;
            turnHold = hold;
            sfx.Play(Sfx.Gust, 0.45f, 1.3f);
            yield return Wait(TurnSweep);
            columns.Clear();
            quills.Clear();
            if (fighting && !hovering) { hovering = true; game.BeginHover(); }
            yield return Wait(hold);
            if (fighting) { hovering = false; game.Bird.Flap(); }
        }

        private void ShowLine(string text)
        {
            line = text.ToUpperInvariant();
            lineStart = Time.time;
            blips = 0;
            // Wrap once, up front, so the bubble doesn't change shape as the words type out.
            lineRows.Clear();
            string row = "";
            foreach (string word in line.Split(' '))
            {
                string next = row.Length == 0 ? word : row + " " + word;
                if (next.Length > 24 && row.Length > 0) { lineRows.Add(row); row = word; }
                else row = next;
            }
            lineRows.Add(row);
        }

        // A line while the bird hovers: Space finishes it, then moves on (or it moves on by itself).
        private IEnumerator Say(string text)
        {
            ShowLine(text);
            advance = false;
            float typing = text.Length / CharsPerSecond;
            while (true)
            {
                float t = Time.time - lineStart;
                if (advance)
                {
                    advance = false;
                    if (t < typing) lineStart = Time.time - typing;
                    else break;
                }
                if (t > typing + 2.4f) break;
                yield return null;
            }
            line = null;
            yield return Wait(0.15f);
        }

        // A line mid-fight: the bird keeps flying, and it goes by on its own. She lets it catch
        // its breath, too: some hope comes back while she talks.
        private IEnumerator Talk(string text)
        {
            ShowLine(text);
            if (fighting) game.RestoreHope(25, true);
            yield return Wait(text.Length / CharsPerSecond + 1.4f);
            line = null;
        }

        // Runs attacks side by side and waits for all of them.
        private IEnumerator Attack(params IEnumerator[] parts)
        {
            attacking = true;
            int running = parts.Length;
            foreach (IEnumerator part in parts) StartCoroutine(Track(part, () => running--));
            while (running > 0) yield return null;
            attacking = false;
        }

        private IEnumerator Track(IEnumerator part, System.Action done)
        {
            yield return StartCoroutine(part);
            done();
        }

        // ---- Attacks ---------------------------------------------------------------------------

        // Fascicles: pages stitched into gates whose gaps open and shut, a bar of the music at a
        // time. The bird can't wait for a gate - it only flies up and down - so each gate's rhythm
        // is set as it appears so that it is always open while it passes the bird (somewhere in the
        // middle of its open stretch, never right at the edge). Hard to read, never impossible.
        private const float GateBar = 1.44f; // four beats of the boss theme
        private IEnumerator Fascicles(float seconds, float speed, float every)
        {
            float gapY = BirdY;
            for (float t = 0, next = 0; t < seconds; t += Time.deltaTime)
            {
                if (t >= next)
                {
                    gapY = Mathf.Clamp(gapY + Random.Range(-1.6f, 1.6f), Floor + 1.7f, Top - 1.7f);
                    float travel = (Right + 1 - BirdX) / speed;
                    columns.Add(new Column
                    {
                        X = Right + 1, Speed = speed, Fascicle = true, Phase = Random.Range(0.14f, 0.36f) * GateBar - travel,
                        GapY = new[] { gapY }, Half = new[] { 1.3f }
                    });
                    next += every;
                }
                yield return null;
            }
        }

        // 0 shut .. 1 open, over a bar: open (the first half), stitch shut, hold, unpick.
        private static float Openness(Column gate)
        {
            float f = Mathf.Repeat((gate.Age + gate.Phase) / GateBar, 1);
            return f < 0.5f ? 1 : f < 0.62f ? 1 - (f - 0.5f) / 0.12f : f < 0.86f ? 0 : (f - 0.86f) / 0.14f;
        }

        // Blanks: one line of the poem at a time, with its missing word in one of three windows.
        private IEnumerator Blanks(float speed)
        {
            foreach (string[] blank in BlankLines) yield return OneBlank(blank, speed);
        }

        private IEnumerator OneBlank(string[] data, float speed)
        {
            string[] words = { data[2], data[3], data[4] };
            for (int i = words.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (words[i], words[j]) = (words[j], words[i]);
            }
            currentBlank = new Blank { Before = data[0], After = data[1], Words = words, Right = System.Array.IndexOf(words, data[2]) };
            // Three windows across the middle of the sky, clear of the line shown at the top.
            float middle = (Top + Floor) / 2 - 0.2f;
            var wall = new Column
            {
                X = Right + 1.2f, Speed = speed, Blank = currentBlank,
                GapY = new[] { middle + 2.4f, middle, middle - 2.4f }, Half = new[] { WindowHalf, WindowHalf, WindowHalf }
            };
            columns.Add(wall);
            while (!wall.Passed && columns.Contains(wall)) yield return null;
            yield return Wait(1.1f); // the finished line stays up a moment
            currentBlank = null;
        }

        // The Wave, after Geometry Dash: hold to fly up at a slant, let go to fly down, through a
        // zigzag tunnel that narrows as it goes. The walls close in at the start and open out at the end.
        private IEnumerator Wave(WaveStyle style)
        {
            game.BeginHover();
            waving = true;
            waveStart = Time.time;
            waveScroll = 0;
            waveOpen = 1;
            waveLength = style.Seconds;
            waveY = BirdY;
            trail.Clear();
            BuildTunnel(style.Seconds * Mathf.Max(style.StartSpeed, style.EndSpeed) + 2 * Right + 6, style);
            for (float t = 0; t < style.Seconds; t += Time.deltaTime)
            {
                float f = t / style.Seconds;
                waveHalf = Mathf.Lerp(style.StartHalf, style.EndHalf, f);
                waveSpeed = Mathf.Lerp(style.StartSpeed, style.EndSpeed, f);
                waveOpen = t < WaveIn ? 1 - Mathf.SmoothStep(0, 1, t / WaveIn)
                    : Mathf.SmoothStep(0, 1, (t - (style.Seconds - WaveOut)) / WaveOut);
                yield return null;
            }
            waving = false;
            path.Clear();
            trail.Clear();
            game.Bird.Flap();
        }

        // A straight run-up, then diagonals at the wave's own slope (and, in harder waves, the odd
        // flat stretch), kept clear of the floor and the sky.
        private void BuildTunnel(float length, WaveStyle style)
        {
            var random = new System.Random(style.Seed);
            float Roll(float min, float max) => min + (float)random.NextDouble() * (max - min);
            path.Clear();
            float low = Floor + 1.7f, high = Top - 1.7f, y = WaveStartY, s = WaveGrace * style.StartSpeed + 2;
            path.Add(new Vector2(-30, y));
            path.Add(new Vector2(s, y));
            int dir = 1;
            while (s < length)
            {
                if (Roll(0, 1) < style.Flats)
                {
                    s += Roll(0.5f, 0.9f);
                    path.Add(new Vector2(s, y));
                    continue;
                }
                float room = dir > 0 ? high - y : y - low;
                if (room < 0.6f) { dir = -dir; continue; }
                float run = Mathf.Min(Roll(style.MinRun, style.MaxRun), room);
                s += run;
                y += dir * run;
                path.Add(new Vector2(s, y));
                if (Roll(0, 1) < 0.8f) dir = -dir; // mostly zigzag, sometimes a longer climb or dive
            }
            path.Add(new Vector2(s + 30, y));
        }

        private float TunnelCentre(float s)
        {
            for (int i = 1; i < path.Count; i++)
                if (s <= path[i].x) return Mathf.Lerp(path[i - 1].y, path[i].y, Mathf.InverseLerp(path[i - 1].x, path[i].x, s));
            return path.Count > 0 ? path[path.Count - 1].y : 0;
        }

        // Quills aimed at the bird's height; now and then a pair either side of it, to hold still between.
        private IEnumerator Quills(float seconds, float every)
        {
            for (float t = 0, next = 0; t < seconds; t += Time.deltaTime)
            {
                if (t >= next)
                {
                    if (Random.value < 0.25f)
                    {
                        AddQuill(BirdY + 1.05f);
                        AddQuill(BirdY - 1.05f);
                    }
                    else AddQuill(BirdY + Random.Range(-0.35f, 0.35f));
                    next += every;
                }
                yield return null;
            }
        }

        private void AddQuill(float y) =>
            quills.Add(new Quill { Y = Mathf.Clamp(y, Floor + 0.5f, Top - 0.5f), Born = Time.time, FromLeft = Random.value < 0.35f });

        // ---- The ending ------------------------------------------------------------------------

        private IEnumerator Perch()
        {
            perching = true;
            Transform bird = game.Bird.transform;
            Vector3 from = bird.position;
            // Her shoulder, on the side away from the bird's flight.
            Vector3 to = transform.position + new Vector3(5 * Pixel, 30 * Pixel + 0.2f, 0);
            for (float t = 0; t < 1; t += Time.deltaTime / 1.6f)
            {
                float e = Mathf.SmoothStep(0, 1, t);
                Vector3 at = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 1.2f;
                bird.SetPositionAndRotation(at, Quaternion.Euler(0, 0, Mathf.Lerp(15, 0, e)));
                yield return null;
            }
            bird.SetPositionAndRotation(to, Quaternion.identity);
            perching = false;
            Perched = true;
        }

        // ---- Every frame -----------------------------------------------------------------------

        private void Update()
        {
            if (!Active || Time.deltaTime <= 0) return; // nothing moves under the poem page
            float dt = Time.deltaTime;
            // She floats, bobbing a pixel or two - and keeps still once she's asleep.
            float bob = sleeping || Perched ? 0 : Mathf.Round(Mathf.Sin(Time.time * 1.6f) * 1.5f) * Pixel;
            transform.position = Home + Vector2.up * bob;

            if (FlappyBirdGame.FlapPressed() && !game.PointerOverControls()) OnFlap();
            if (hovering && !BirdStill) game.HoverBird(0.3f);

            // Her voice: a blip every other letter as the line types out.
            if (line != null)
                for (int shown = Shown; blips < shown; blips++)
                    if (char.IsLetter(line[blips]) && blips % 2 == 0) sfx.Play(Sfx.Voice, 0.7f, Random.Range(0.94f, 1.06f));

            UpdateColumns(dt);
            UpdateQuills();
            if (waving) UpdateWave(dt);
            else if (fighting && !hovering)
            {
                // The edges of the sky sting too, and bounce the bird back.
                if (BirdY > Top - 0.25f || BirdY < Floor + 0.27f) Hurt(true);
            }
        }

        // A flap: a wingbeat in the fight, otherwise it moves the talking along.
        private void OnFlap()
        {
            if (!fighting) advance = true;
            else if (!waving && !hovering) game.Bird.Flap(game.Energy > 0 ? 1 : 0.8f);
        }

        private void UpdateColumns(float dt)
        {
            for (int i = columns.Count - 1; i >= 0; i--)
            {
                Column column = columns[i];
                column.X -= column.Speed * dt;
                column.Age += dt;
                if (column.Fascicle) column.Half[0] = Mathf.Lerp(easy ? 0.55f : 0.15f, 1.3f, Openness(column));
                else if (!column.Inked && column.X - BirdX < column.Speed * 0.55f)
                {
                    // She strikes out the wrong words just before the bird gets there.
                    column.Inked = true;
                    sfx.Play(Sfx.Cut, 0.8f);
                    game.Shake(0.08f);
                }
                if (column.X < -Right - 2) { columns.RemoveAt(i); continue; }
                if (!fighting || hovering) continue;
                if (Mathf.Abs(BirdX - column.X) < ColumnHalf + BirdRadius)
                {
                    bool clear = false;
                    for (int g = 0; g < column.GapY.Length; g++)
                        if ((!column.Inked || g == column.Blank.Right) && Mathf.Abs(BirdY - column.GapY[g]) < column.Half[g] - BirdRadius * 0.8f)
                            clear = true;
                    if (!clear) Hurt(false);
                }
                if (!column.Passed && column.X + ColumnHalf < BirdX - BirdRadius)
                {
                    column.Passed = true;
                    if (column.Blank != null) column.Blank.PassedAt = Time.time;
                    if (column.Blank != null && Nearest(column) == column.Blank.Right)
                    {
                        // The right word drops into the line, as a note.
                        column.Blank.FilledAt = Time.time;
                        sfx.Play(Sfx.Word, 0.7f);
                        game.RestoreHope(10, true);
                    }
                }
            }
        }

        private int Nearest(Column column)
        {
            int best = 0;
            for (int g = 1; g < column.GapY.Length; g++)
                if (Mathf.Abs(BirdY - column.GapY[g]) < Mathf.Abs(BirdY - column.GapY[best])) best = g;
            return best;
        }

        private void UpdateWave(float dt)
        {
            waveScroll += waveSpeed * dt;
            bool ready = Time.time - waveStart > WaveGrace;
            bool rising = FlappyBirdGame.FlapHeld();
            if (ready) waveY += (rising ? waveSpeed : -waveSpeed) * dt;
            else waveY = Mathf.MoveTowards(waveY, WaveStartY, 3 * dt); // settle onto the tunnel's start
            // While the walls are still closing in (or opening out) they can't hurt, only guide.
            float centre = TunnelCentre(waveScroll), room = waveHalf + waveOpen * 8 - BirdRadius;
            waveY = Mathf.Clamp(waveY, Floor + 0.4f, Top - 0.4f);
            if (Mathf.Abs(waveY - centre) > room)
            {
                if (ready && waveOpen <= 0) Hurt(false);
                waveY = Mathf.Clamp(waveY, centre - room, centre + room);
            }
            Transform bird = game.Bird.transform;
            bird.SetPositionAndRotation(new Vector3(bird.position.x, waveY, 0), Quaternion.Euler(0, 0, ready ? (rising ? 45 : -45) : 0));
            trail.Add(new Vector2(waveScroll, waveY));
            while (trail.Count > 0 && trail[0].x < waveScroll - 5) trail.RemoveAt(0);
        }

        private void UpdateQuills()
        {
            for (int i = quills.Count - 1; i >= 0; i--)
            {
                Quill quill = quills[i];
                float age = Time.time - quill.Born;
                if (!quill.Charged && age >= QuillIn) { quill.Charged = true; sfx.Play(Sfx.Charge, 0.5f); }
                if (!quill.Fired && age >= QuillIn + quillCharge) { quill.Fired = true; sfx.Play(Sfx.Beam, 0.6f); game.Shake(0.1f); }
                bool firing = age >= QuillIn + quillCharge && age < QuillIn + quillCharge + QuillFire;
                if (firing && fighting && !hovering && Mathf.Abs(BirdY - quill.Y) < BeamHalf + BirdRadius) Hurt(false);
                if (age > QuillIn + quillCharge + QuillFire + QuillOut) quills.RemoveAt(i);
            }
        }

        // A hit costs hope and counts as a crash, then the bird flickers, safe, for a moment.
        public void Hurt(bool surface)
        {
            if (!fighting || hovering) return;
            if (surface) game.Bird.Bounce(BirdY < 0 ? 5.5f : -4.5f);
            if (Time.time < hurtUntil) return;
            hurtUntil = Time.time + Invulnerable;
            game.Wound(easy ? 4 : 8);
            sfx.Play(Sfx.Hurt);
        }

        // ---- Drawing (GUI, in the HUD's 800-unit space) ----------------------------------------

        public void Draw(float width)
        {
            if (!Active || Event.current.type != EventType.Repaint) return;
            float now = Time.time;
            DrawDawn(width, now);
            if (waving) DrawWave(width);
            foreach (Column column in columns) DrawColumn(column);
            DrawQuills(width, now);
            if (currentBlank != null) DrawBlankLine(width, currentBlank);

            Vector2 head = game.ToUi((Vector2)transform.position + new Vector2(0, 38 * Pixel));
            // Her eye glows while she means it.
            if (eyeAt > 0 && art.enabled)
            {
                Vector2 eye = game.ToUi((Vector2)transform.position + new Vector2(-2 * Pixel, 37.5f * Pixel));
                float flicker = Mathf.Repeat(now * 9, 1) < 0.5f ? 1 : 0.6f;
                PixelUi.Fill(new Rect(Mathf.Round(eye.x - 6), Mathf.Round(eye.y - 6), 12, 12), new Color(Glow.r, Glow.g, Glow.b, 0.35f * flicker));
                PixelUi.Fill(new Rect(Mathf.Round(eye.x - 3), Mathf.Round(eye.y - 3), 6, 6), new Color(Glow.r, Glow.g, Glow.b, flicker));
                for (int i = 0; i < 3; i++)
                {
                    float rise = Mathf.Repeat(now * 1.5f + i / 3f, 1);
                    PixelUi.Fill(new Rect(Mathf.Round(eye.x - 9 - rise * 18), Mathf.Round(eye.y - 3 - rise * 24), 3, 3),
                        new Color(Glow.r, Glow.g, Glow.b, 1 - rise));
                }
            }
            if (sleeping)
                for (int i = 0; i < 3; i++)
                {
                    float rise = Mathf.Repeat(now * 0.45f + i / 3f, 1);
                    PixelUi.Text("Z", head.x - 30 - rise * 40, head.y - 60 - rise * 70, 2 + Mathf.Round(rise * 2), new Color(1, 1, 1, 1 - rise), 0.5f);
                }
            if (line != null) DrawBubble(width, head);
            if (creditsAt > 0) Credits.Draw(width, now - creditsAt, HudHide);
            if (waitingToPerch && Mathf.Repeat(now, 1.1f) < 0.8f)
                PixelUi.Text("SPACE OR CLICK - FLY TO HER", width / 2, 520, 3, Color.white, 0.5f);
            DrawTurn(width, now);
            // A cut snaps to black and fades back.
            float black = now < cutUntil ? 1 : 1 - (now - cutUntil) / 0.35f;
            if (black > 0) PixelUi.Fill(new Rect(0, 0, width, 800), new Color(0, 0, 0, black));
        }

        // A column in the HUD's space: stitched manuscript pages, or an ink wall with word windows.
        private void DrawColumn(Column column)
        {
            float top = game.ToUi(new Vector2(column.X, Top + 1)).y, bottom = game.ToUi(new Vector2(column.X, Floor - 1)).y;
            Vector2 centre = game.ToUi(new Vector2(column.X, 0));
            float half = ColumnHalf * 80;
            float left = Mathf.Round(centre.x - half), w = Mathf.Round(half * 2);
            // Solid runs between the openings, from the top of the sky down.
            float y = top;
            int[] order = column.Fascicle ? new[] { 0 } : new[] { 0, 1, 2 };
            foreach (int g in order)
            {
                float open = column.Inked && g != column.Blank.Right ? 0 : column.Half[g];
                float gapTop = game.ToUi(new Vector2(column.X, column.GapY[g] + open)).y;
                float gapBottom = game.ToUi(new Vector2(column.X, column.GapY[g] - open)).y;
                Slab(column, left, w, y, gapTop);
                y = gapBottom;
                if (column.Fascicle && column.Half[0] < 1.25f)
                {
                    // Red thread zigzags across the gap as she stitches it shut.
                    float shut = game.ToUi(new Vector2(column.X, column.GapY[0] - 1.3f)).y, openTop = game.ToUi(new Vector2(column.X, column.GapY[0] + 1.3f)).y;
                    for (float ty = openTop; ty < shut; ty += 12)
                        if (ty < gapTop || ty > gapBottom)
                            PixelUi.Fill(new Rect(left + (Mathf.Repeat(ty, 24) < 12 ? 6 : w - 18), Mathf.Round(ty), 12, 3), Thread);
                }
                if (column.Blank != null)
                {
                    float labelY = game.ToUi(new Vector2(column.X, column.GapY[g])).y;
                    bool struck = column.Inked && g != column.Blank.Right;
                    PixelUi.Text(column.Blank.Words[g], centre.x, labelY - 7.5f, 3, struck ? new Color(0.5f, 0.45f, 0.55f) : Color.white, 0.5f);
                    if (struck)
                        for (int k = -1; k <= 1; k++) // scribbled out
                            PixelUi.Fill(new Rect(Mathf.Round(centre.x - 60), Mathf.Round(labelY + k * 6 - 1), 120, 3), Night);
                }
            }
            Slab(column, left, w, y, bottom);
        }

        private static void Slab(Column column, float left, float w, float from, float to)
        {
            if (to <= from) return;
            PixelUi.Fill(new Rect(left - 3, Mathf.Round(from), w + 6, Mathf.Round(to - from)), PixelUi.Outline);
            if (!column.Fascicle)
            {
                PixelUi.Fill(new Rect(left, Mathf.Round(from), w, Mathf.Round(to - from)), Night);
                return;
            }
            // Manuscript pages: paper with faint ruled lines and a red margin.
            PixelUi.Fill(new Rect(left, Mathf.Round(from), w, Mathf.Round(to - from)), Page);
            for (float y = Mathf.Ceil(from / 15) * 15; y < to; y += 15)
                PixelUi.Fill(new Rect(left, y, w, 3), new Color(0.6f, 0.75f, 0.9f, 0.6f));
            PixelUi.Fill(new Rect(left + 9, Mathf.Round(from), 3, Mathf.Round(to - from)), new Color(0.85f, 0.4f, 0.4f, 0.7f));
        }

        // The line being asked, at the top, with a box where the missing word goes.
        private void DrawBlankLine(float width, Blank blank)
        {
            const float size = 3;
            float boxWidth = 0;
            foreach (string word in blank.Words) boxWidth = Mathf.Max(boxWidth, PixelUi.TextWidth(word, size));
            boxWidth += 18;
            float before = PixelUi.TextWidth(blank.Before, size), after = PixelUi.TextWidth(blank.After, size);
            float total = before + 12 + boxWidth + 12 + after;
            // Fades in as the wall appears, and out once the bird is through.
            float alpha = Mathf.Clamp01((Time.time - blank.ShownAt) / 0.3f);
            if (blank.PassedAt > 0) alpha *= Mathf.Clamp01(1 - (Time.time - blank.PassedAt - 0.8f) / 0.3f);
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha * previous.a);
            Rect panel = new Rect(Mathf.Round((width - total) / 2 - 24), 96, total + 48, 48);
            PixelUi.Frame(panel, "panel");
            float x = panel.x + 24, y = panel.y + 16;
            PixelUi.Text(blank.Before, x, y, size, Color.white);
            x += before + 12;
            if (blank.FilledAt > 0)
                PixelUi.Text(blank.Words[blank.Right], x + boxWidth / 2, y - Mathf.Round(Mathf.Clamp01(1 - (Time.time - blank.FilledAt) / 0.2f) * 2) * 3, size, Gold, 0.5f);
            else if (Mathf.Repeat(Time.time, 0.8f) < 0.55f)
                PixelUi.Fill(new Rect(x, y + 15 + 3, boxWidth, 3), Gold);
            PixelUi.Text(blank.After, x + boxWidth + 12, y, size, Color.white);
            GUI.color = previous;
        }

        // The Wave's tunnel, Geometry Dash style: walls of blocks that scroll with the tunnel,
        // saw-tooth neon edges that glow brighter on every beat, the bird's trail, and a progress
        // bar across the top so you can learn it.
        private void DrawWave(float width)
        {
            const float step = 6;
            float birdX = BirdX, scrollPx = waveScroll * 80;
            float pulse = 1 - Mathf.Repeat(music.SongTime, music.BeatLength) / music.BeatLength;
            Color glow = new Color(Gold.r, Gold.g, Gold.b, 0.15f + 0.25f * pulse);
            for (float ux = 0; ux < width; ux += step)
            {
                float wx = (ux / width * 2 - 1) * Right;
                float s = waveScroll + (wx - birdX);
                float half = waveHalf + waveOpen * 8 + Mathf.Abs(Mathf.Repeat(s * 3, 1) - 0.5f) * 0.24f;
                float centre = TunnelCentre(s);
                float upper = Mathf.Round(game.ToUi(new Vector2(wx, centre + half)).y), lower = Mathf.Round(game.ToUi(new Vector2(wx, centre - half)).y);
                // One unit of overlap, so the columns never leave seams between them.
                PixelUi.Pattern(new Rect(ux, 0, step + 1, upper), "wave_block", new Vector2(scrollPx, 0));
                PixelUi.Pattern(new Rect(ux, lower, step + 1, 800 - lower), "wave_block", new Vector2(scrollPx, 0));
                PixelUi.Fill(new Rect(ux, upper, step + 1, 9), glow);
                PixelUi.Fill(new Rect(ux, lower - 9, step + 1, 9), glow);
                PixelUi.Fill(new Rect(ux, upper - 3, step + 1, 3), Gold);
                PixelUi.Fill(new Rect(ux, lower, step + 1, 3), Gold);
            }
            for (int i = 0; i < trail.Count; i++)
            {
                Vector2 point = game.ToUi(new Vector2(birdX + trail[i].x - waveScroll, trail[i].y));
                float fade = 1 - (waveScroll - trail[i].x) / 5;
                float x = Mathf.Round(point.x / 3) * 3, y = Mathf.Round(point.y / 3) * 3;
                PixelUi.Fill(new Rect(x - 4.5f, y - 4.5f, 9, 9), new Color(Glow.r, Glow.g, Glow.b, 0.35f * fade));
                PixelUi.Fill(new Rect(x - 1.5f, y - 1.5f, 3, 3), new Color(1, 1, 1, 0.9f * fade));
            }
            float progress = Mathf.Clamp01((Time.time - waveStart) / waveLength);
            Rect bar = new Rect(Mathf.Round(width / 2 - 160), 104, 320, 24);
            PixelUi.Frame(bar, "bar");
            PixelUi.Fill(new Rect(bar.x + 9, bar.y + 9, Mathf.Round((bar.width - 18) * progress / 3) * 3, 6), Gold);
            PixelUi.Text(Mathf.FloorToInt(progress * 100).ToString(), bar.xMax + 12, bar.y + 5, 2, Color.white);
        }

        private void DrawQuills(float width, float now)
        {
            const float scale = 6;
            Vector2 size = PixelUi.IconSize("quill", scale);
            foreach (Quill quill in quills)
            {
                float age = now - quill.Born;
                float leave = age - (QuillIn + quillCharge + QuillFire);
                float slide = leave > 0 ? 1 - leave / QuillOut : Mathf.Clamp01(age / QuillIn);
                slide = Mathf.SmoothStep(0, 1, slide);
                float y = game.ToUi(new Vector2(0, quill.Y)).y;
                // The quill's back stays at the screen edge; its nib points across the sky.
                float x = quill.FromLeft ? -size.x + (size.x - 12) * slide : width - (size.x - 12) * slide;
                float nib = quill.FromLeft ? x + size.x : x;
                bool charging = age >= QuillIn && age < QuillIn + quillCharge;
                bool firing = age >= QuillIn + quillCharge && leave < 0;
                if (charging && Mathf.Repeat(now * 14, 1) < 0.6f)
                {
                    Rect warn = quill.FromLeft ? Rect.MinMaxRect(nib, y - 1.5f, width, y + 1.5f) : Rect.MinMaxRect(0, y - 1.5f, nib, y + 1.5f);
                    PixelUi.Fill(warn, new Color(Gold.r, Gold.g, Gold.b, 0.7f));
                }
                if (firing)
                {
                    // The beam narrows as it fades out.
                    float half = BeamHalf * 80 * Mathf.Clamp01((QuillIn + quillCharge + QuillFire - age) / 0.15f);
                    float a = quill.FromLeft ? nib : 0, b = quill.FromLeft ? width : nib;
                    PixelUi.Fill(Rect.MinMaxRect(a, Mathf.Round(y - half), b, Mathf.Round(y + half)), new Color(Gold.r, Gold.g, Gold.b, 0.9f));
                    PixelUi.Fill(Rect.MinMaxRect(a, Mathf.Round(y - half * 0.55f), b, Mathf.Round(y + half * 0.55f)), Color.white);
                }
                PixelUi.Icon(charging || firing ? "quill_open" : "quill", x, y - size.y / 2, scale, quill.FromLeft);
            }
        }

        private void DrawBubble(float width, Vector2 head)
        {
            const float size = 3, rowHeight = 22;
            float textWidth = 0;
            foreach (string row in lineRows) textWidth = Mathf.Max(textWidth, PixelUi.TextWidth(row, size));
            Rect bubble = new Rect(0, 0, textWidth + 42, lineRows.Count * rowHeight + 30);
            bubble.x = Mathf.Max(24, Mathf.Round(head.x - bubble.width - 60));
            bubble.y = Mathf.Clamp(Mathf.Round(head.y - bubble.height / 2 - 40), 130, 800 - bubble.height - 120);
            PixelUi.Frame(bubble, "parchment");
            // A little tail pointing at her.
            Color paper = new Color(0.95f, 0.9f, 0.78f);
            float tailY = Mathf.Round(Mathf.Clamp(head.y - 30, bubble.y + 12, bubble.yMax - 24));
            PixelUi.Fill(new Rect(bubble.xMax - 3, tailY, 12, 12), paper);
            PixelUi.Fill(new Rect(bubble.xMax + 9, tailY + 3, 9, 6), paper);
            int left = Shown;
            for (int i = 0; i < lineRows.Count && left > 0; i++)
            {
                string row = lineRows[i];
                PixelUi.Text(row.Substring(0, Mathf.Min(row.Length, left)), bubble.x + 21, bubble.y + 16 + i * rowHeight, size, Ink, 0, false);
                left -= row.Length + 1;
            }
        }

        // The credits roll over the dawn; Space or a click skips them.
        private IEnumerator RollCredits()
        {
            creditsAt = Time.time;
            creditsEnd = -10;
            advance = false;
            while (Time.time - creditsAt < Credits.Duration && !advance) yield return null;
            creditsEnd = Time.time;
            yield return Wait(0.5f);
            creditsAt = creditsEnd = -10;
        }

        // The page turn: an ink page with a ragged, gold-ruled edge slides in from the right, holds,
        // and slides on out to the left, carrying the next attack's name.
        private void DrawTurn(float width, float now)
        {
            float t = now - turnAt;
            if (turnAt < 0 || t > 2 * TurnSweep + turnHold) return;
            float cover = Mathf.SmoothStep(0, 1, t / TurnSweep), leave = Mathf.SmoothStep(0, 1, (t - TurnSweep - turnHold) / TurnSweep);
            float left = width * (1 - cover) - (width + 60) * leave, right = left + width + 60;
            for (int row = 0; row * 12 < 800; row++)
            {
                float ragged = Mathf.Round(Mathf.Sin(row * 1.7f) * 2 + Mathf.Sin(row * 0.6f) * 3) * 3;
                float a = Mathf.Max(0, left + ragged), b = Mathf.Min(width, right + ragged);
                if (b <= a) continue;
                PixelUi.Fill(new Rect(a, row * 12, b - a, 12), Night);
                if (a > 0) PixelUi.Fill(new Rect(a, row * 12, 3, 12), Gold);
                if (b < width) PixelUi.Fill(new Rect(b - 3, row * 12, 3, 12), Gold);
            }
            if (turnTitle == null) return;
            float centre = (left + right - 60) / 2;
            PixelUi.Text(turnTitle, centre, 350, 5, Gold, 0.5f);
            PixelUi.Text(turnNote, centre, 410, 2, new Color(0.86f, 0.82f, 0.9f), 0.5f);
        }

        // Once she smiles, dawn comes up over the frozen sea: warm bands strongest at the horizon.
        private static readonly Color[] DawnBands = BuildDawn();

        private static Color[] BuildDawn()
        {
            var bands = new Color[8];
            for (int i = 0; i < bands.Length; i++) bands[i] = new Color(1f, 0.45f + 0.04f * i, 0.62f - 0.03f * i, 0.03f + 0.03f * i);
            return bands;
        }

        private void DrawDawn(float width, float now)
        {
            if (dawnAt < 0) return;
            float dawn = Mathf.SmoothStep(0, 1, (now - dawnAt) / 2.5f);
            for (int i = 0; i < DawnBands.Length; i++)
            {
                Color band = DawnBands[i];
                PixelUi.Fill(new Rect(0, 300 + i * 40, width, 40), new Color(band.r, band.g, band.b, band.a * dawn));
            }
            PixelUi.Fill(new Rect(0, 620, width, 6), new Color(1f, 0.8f, 0.5f, 0.35f * dawn));
        }
    }
}
