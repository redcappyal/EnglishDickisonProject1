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
            public float X, Speed, Phase;
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
            public float FilledAt = -1;
        }

        // A giant quill that slides in at the edge, charges, and fires a beam straight across.
        private sealed class Quill
        {
            public float Y, Born;
            public bool FromLeft, Charged, Fired;
        }

        private const float QuillIn = 0.35f, QuillCharge = 0.6f, QuillFire = 0.45f, QuillOut = 0.3f, BeamHalf = 0.42f;
        private const float BirdRadius = 0.19f, Invulnerable = 1.2f, HopePerHit = 8, CharsPerSecond = 26;
        private const float ColumnHalf = 0.5f, WindowHalf = 0.72f, WaveSpeed = 4.5f, WaveGrace = 1.2f;
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
        private static readonly Color Thread = new Color(0.75f, 0.2f, 0.2f), Rock = new Color(0.1f, 0.06f, 0.2f), Night = new Color(0.01f, 0.008f, 0.025f); // GUI colours are linear: this reads as deep night

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
        private string hint;
        private float hintUntil = -10;

        // Talking: the bird hovers. Fighting: the bird flies under its own wings.
        private bool hovering, fighting, attacking, sleeping, waitingToPerch, perching, advance;
        private string line;
        private readonly List<string> lineRows = new List<string>();
        private float lineStart, hurtUntil = -10, cutUntil = -10, eyeAt = -10, dawnAt = -10;
        private int blips;
        private float creditsAt = -10, creditsEnd = -10;

        // The end credits, rolling up over the dawn. (text, size, colour: 0 white, 1 gold, 2 muted; null = a gap)
        private static readonly (string Text, float Size, int Tone)[] CreditLines = {
            ("THE THING WITH FEATHERS", 5, 1), (null, 0, 0),
            ("A GAME BY", 2, 2), ("ALVIN, SHRIDHAR AND ISHAAN", 3, 0), (null, 0, 0),
            ("THE POEM", 2, 2), ("\"HOPE\" IS THE THING WITH FEATHERS", 3, 0), ("BY EMILY DICKINSON (1830 - 1886)", 2, 0), (null, 0, 0),
            ("STARRING", 2, 2), ("THE BIRD - AS HOPE", 3, 0), ("EMILY DICKINSON - AS HERSELF", 3, 0), (null, 0, 0),
            ("THE WORLDS", 2, 2), ("HOPE - THE GALE - WARMTH - EXTREMITY", 3, 0), (null, 0, 0),
            ("ART, MUSIC AND SOUND", 2, 2), ("PIXEL ART AND SYNTHESIZED MUSIC, ALL MADE IN CODE", 2, 0), (null, 0, 0),
            ("INSPIRED BY", 2, 2), ("FLAPPY BIRD BY DONG NGUYEN", 2, 0), ("UNDERTALE BY TOBY FOX", 2, 0), (null, 0, 0),
            ("POEM TEXT", 2, 2), ("PUBLIC DOMAIN, VIA THE ACADEMY OF AMERICAN POETS", 2, 0), (null, 0, 0),
            ("MADE WITH UNITY 6 FOR HONORS AMERICAN LITERATURE", 2, 2), (null, 0, 0), (null, 0, 0),
            ("AND NEVER STOPS - AT ALL", 4, 1)
        };
        private const float CreditsSpeed = 55, CreditsGap = 30, CreditsHold = 3;

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
            hurtUntil = cutUntil = eyeAt = dawnAt = creditsAt = creditsEnd = hintUntil = -10;
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

            yield return Cut(0.25f);
            music.SetLevel(DickinsonMusic.BossTheme);
            hovering = false;
            fighting = true;
            game.RestoreHope(100);
            game.Bird.Flap();
            // 1. Fascicles: she sewed her poems into little books by hand.
            yield return Talk("i sewed these - by hand - you know -");
            Hint("WAIT FOR THE PAGES TO OPEN");
            yield return Attack(Fascicles(12, 4.2f, 1.6f));
            yield return Talk("not bad - for a Bird -");
            // 2. Blanks: the poem, with a word missing.
            yield return Talk("let's see - if you know - the Words -");
            Hint("FLY THROUGH THE RIGHT WORD");
            yield return Attack(Blanks(3.6f));
            yield return Talk("hm - you do - know them -");
            // 3. The Wave: a slanted tunnel.
            yield return Talk("tell all the truth - but tell it - Slant -");
            yield return Cut(0.3f);
            yield return Attack(Wave(14, 1.3f, 0.95f));
            yield return Cut(0.3f);
            // 4. Quills.
            yield return Talk("this next one - is a real - Page-turner -");
            yield return Attack(Quills(10, 0.75f));
            // 5. Her Letter to the World: everything at once, in quick black cuts, and then the
            // last line - the one the whole game argues.
            yield return Talk("alright. this is my Letter - to the World -");
            yield return Cut(0.15f);
            yield return Attack(Fascicles(5.5f, 5.2f, 1.15f), Quills(5.5f, 1.4f));
            yield return Cut(0.2f);
            yield return Attack(Wave(7, 1.05f, 0.85f));
            yield return Cut(0.2f);
            yield return Attack(Quills(4, 0.45f));
            yield return Talk("finish it - for me -");
            yield return Attack(OneBlank(LastLine, 3.4f));
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
            yield return Credits();
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

        private void Hint(string text)
        {
            hint = text;
            hintUntil = Time.time + 3.5f;
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

        // Fascicles: pages stitched into gates. Each gate's gap opens and shuts on the music's
        // beat (every gate a quarter-bar out of step with the last), so the trick is timing.
        private IEnumerator Fascicles(float seconds, float speed, float every)
        {
            float gapY = BirdY;
            for (float t = 0, next = 0; t < seconds; t += Time.deltaTime)
            {
                if (t >= next)
                {
                    gapY = Mathf.Clamp(gapY + Random.Range(-1.6f, 1.6f), Floor + 1.7f, Top - 1.7f);
                    columns.Add(new Column
                    {
                        X = Right + 1, Speed = speed, Fascicle = true, Phase = Random.Range(0, 4) * 0.25f,
                        GapY = new[] { gapY }, Half = new[] { 1.3f }
                    });
                    next += every;
                }
                yield return null;
            }
        }

        // 0 shut .. 1 open, over a bar of four beats: open, stitch shut, hold, unpick.
        private float Openness(float phase)
        {
            float f = Mathf.Repeat(music.SongTime / (music.BeatLength * 4) + phase, 1);
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
        // zigzag tunnel that narrows as it goes. The bird leaves a trail.
        private IEnumerator Wave(float seconds, float startHalf, float endHalf)
        {
            game.BeginHover();
            waving = true;
            waveStart = Time.time;
            waveScroll = 0;
            waveHalf = startHalf;
            waveY = Mathf.Clamp(BirdY, Floor + 1.7f, Top - 1.7f);
            trail.Clear();
            BuildTunnel(waveY, seconds * WaveSpeed + 2 * Right + 4);
            Hint("HOLD TO RISE - LET GO TO FALL");
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                waveHalf = Mathf.Lerp(startHalf, endHalf, t / seconds);
                yield return null;
            }
            waving = false;
            path.Clear();
            trail.Clear();
            game.Bird.Flap();
        }

        // A straight run-up, then diagonals at the wave's own slope, kept clear of the floor and sky.
        private void BuildTunnel(float y, float length)
        {
            path.Clear();
            float low = Floor + 1.7f, high = Top - 1.7f, s = WaveGrace * WaveSpeed + 2;
            path.Add(new Vector2(-30, y));
            path.Add(new Vector2(s, y));
            int dir = Random.value < 0.5f ? 1 : -1;
            while (s < length)
            {
                float room = dir > 0 ? high - y : y - low;
                if (room < 0.6f) { dir = -dir; continue; }
                float run = Mathf.Min(Random.Range(0.8f, 2.2f), room);
                s += run;
                y += dir * run;
                path.Add(new Vector2(s, y));
                if (Random.value < 0.8f) dir = -dir; // mostly zigzag, sometimes a longer climb or dive
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
            else if (fighting)
            {
                // The edges of the sky sting too, and bounce the bird back.
                if (BirdY > Top - 0.25f || BirdY < Floor + 0.27f) Hurt(true);
            }
        }

        // A flap: a wingbeat in the fight, otherwise it moves the talking along.
        private void OnFlap()
        {
            if (fighting) { if (!waving) game.Bird.Flap(game.Energy > 0 ? 1 : 0.8f); }
            else advance = true;
        }

        private void UpdateColumns(float dt)
        {
            for (int i = columns.Count - 1; i >= 0; i--)
            {
                Column column = columns[i];
                column.X -= column.Speed * dt;
                if (column.Fascicle) column.Half[0] = Mathf.Lerp(0.15f, 1.3f, Openness(column.Phase));
                else if (!column.Inked && column.X - BirdX < column.Speed * 0.55f)
                {
                    // She strikes out the wrong words just before the bird gets there.
                    column.Inked = true;
                    sfx.Play(Sfx.Cut, 0.8f);
                    game.Shake(0.08f);
                }
                if (column.X < -Right - 2) { columns.RemoveAt(i); continue; }
                if (!fighting) continue;
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
            waveScroll += WaveSpeed * dt;
            bool ready = Time.time - waveStart > WaveGrace;
            bool rising = FlappyBirdGame.FlapHeld();
            if (ready) waveY += (rising ? WaveSpeed : -WaveSpeed) * dt;
            float centre = TunnelCentre(waveScroll), room = waveHalf - BirdRadius;
            if (Mathf.Abs(waveY - centre) > room)
            {
                if (ready) Hurt(false);
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
                if (!quill.Fired && age >= QuillIn + QuillCharge) { quill.Fired = true; sfx.Play(Sfx.Beam, 0.6f); game.Shake(0.1f); }
                bool firing = age >= QuillIn + QuillCharge && age < QuillIn + QuillCharge + QuillFire;
                if (firing && fighting && Mathf.Abs(BirdY - quill.Y) < BeamHalf + BirdRadius) Hurt(false);
                if (age > QuillIn + QuillCharge + QuillFire + QuillOut) quills.RemoveAt(i);
            }
        }

        // A hit costs hope and counts as a crash, then the bird flickers, safe, for a moment.
        public void Hurt(bool surface)
        {
            if (!fighting) return;
            if (surface) game.Bird.Bounce(BirdY < 0 ? 5.5f : -4.5f);
            if (Time.time < hurtUntil) return;
            hurtUntil = Time.time + Invulnerable;
            game.Wound(HopePerHit);
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
            DrawCredits(width, now);
            if (waitingToPerch && Mathf.Repeat(now, 1.1f) < 0.8f)
                PixelUi.Text("SPACE OR CLICK - FLY TO HER", width / 2, 520, 3, Color.white, 0.5f);
            if (now < hintUntil && fighting)
            {
                float a = Mathf.Clamp01((hintUntil - now) / 0.5f);
                PixelUi.Text(hint, width / 2, 690, 2, new Color(1, 1, 1, a), 0.5f);
            }
            if (now < cutUntil) PixelUi.Fill(new Rect(0, 0, width, 800), Color.black);
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
        }

        // The Wave's tunnel: night-ink walls with saw-tooth gold edges, and the bird's trail.
        private void DrawWave(float width)
        {
            const float step = 6;
            float birdX = BirdX;
            for (float ux = 0; ux < width; ux += step)
            {
                float wx = (ux / width * 2 - 1) * Right;
                float s = waveScroll + (wx - birdX);
                float centre = TunnelCentre(s);
                float tooth = Mathf.Abs(Mathf.Repeat(s * 3, 1) - 0.5f) * 0.24f;
                float upper = game.ToUi(new Vector2(wx, centre + waveHalf + tooth)).y;
                float lower = game.ToUi(new Vector2(wx, centre - waveHalf - tooth)).y;
                // One unit of overlap, so the columns never leave seams between them.
                PixelUi.Fill(new Rect(ux, 0, step + 1, Mathf.Round(upper)), Rock);
                PixelUi.Fill(new Rect(ux, Mathf.Round(upper) - 3, step + 1, 3), Gold);
                PixelUi.Fill(new Rect(ux, Mathf.Round(lower), step + 1, 800 - Mathf.Round(lower)), Rock);
                PixelUi.Fill(new Rect(ux, Mathf.Round(lower), step + 1, 3), Gold);
            }
            for (int i = 0; i < trail.Count; i++)
            {
                Vector2 point = game.ToUi(new Vector2(birdX + trail[i].x - waveScroll, trail[i].y));
                float age = (waveScroll - trail[i].x) / 5;
                PixelUi.Fill(new Rect(Mathf.Round(point.x / 3) * 3 - 3, Mathf.Round(point.y / 3) * 3 - 3, 6, 6), new Color(Glow.r, Glow.g, Glow.b, 0.8f * (1 - age)));
            }
        }

        private void DrawQuills(float width, float now)
        {
            const float scale = 6;
            Vector2 size = PixelUi.IconSize("quill", scale);
            foreach (Quill quill in quills)
            {
                float age = now - quill.Born;
                float leave = age - (QuillIn + QuillCharge + QuillFire);
                float slide = leave > 0 ? 1 - leave / QuillOut : Mathf.Clamp01(age / QuillIn);
                slide = Mathf.SmoothStep(0, 1, slide);
                float y = game.ToUi(new Vector2(0, quill.Y)).y;
                // The quill's back stays at the screen edge; its nib points across the sky.
                float x = quill.FromLeft ? -size.x + (size.x - 12) * slide : width - (size.x - 12) * slide;
                float nib = quill.FromLeft ? x + size.x : x;
                bool charging = age >= QuillIn && age < QuillIn + QuillCharge;
                bool firing = age >= QuillIn + QuillCharge && leave < 0;
                if (charging && Mathf.Repeat(now * 14, 1) < 0.6f)
                {
                    Rect warn = quill.FromLeft ? Rect.MinMaxRect(nib, y - 1.5f, width, y + 1.5f) : Rect.MinMaxRect(0, y - 1.5f, nib, y + 1.5f);
                    PixelUi.Fill(warn, new Color(Gold.r, Gold.g, Gold.b, 0.7f));
                }
                if (firing)
                {
                    // The beam narrows as it fades out.
                    float half = BeamHalf * 80 * Mathf.Clamp01((QuillIn + QuillCharge + QuillFire - age) / 0.15f);
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

        // The credits roll until the last line reaches the middle of the screen and rests there.
        // Space or a click skips them.
        private IEnumerator Credits()
        {
            creditsAt = Time.time;
            creditsEnd = -10;
            advance = false;
            float duration = (800 + CreditLines.Length * CreditsGap - 380) / CreditsSpeed + CreditsHold;
            while (Time.time - creditsAt < duration && !advance) yield return null;
            creditsEnd = Time.time;
            yield return Wait(0.5f);
            creditsAt = creditsEnd = -10;
        }

        private void DrawCredits(float width, float now)
        {
            if (creditsAt < 0) return;
            float alpha = HudHide;
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            // A soft dark column behind the text, so it reads over the dawn.
            PixelUi.Fill(new Rect(Mathf.Round(width / 2 - 330), 0, 660, 800), new Color(0.04f, 0.03f, 0.08f, 0.35f));
            float scroll = (now - creditsAt) * CreditsSpeed;
            float last = 800 + (CreditLines.Length - 1) * CreditsGap;
            scroll = Mathf.Min(scroll, last - 380); // the last line comes to rest in the middle
            Color[] tones = { Color.white, Gold, new Color(0.86f, 0.82f, 0.9f) };
            for (int i = 0; i < CreditLines.Length; i++)
            {
                var (text, size, tone) = CreditLines[i];
                if (text == null) continue;
                float y = 800 + i * CreditsGap - scroll;
                if (y < 90 || y > 800) continue;
                // Lines fade in at the bottom and out at the top.
                float edge = Mathf.Clamp01((y - 90) / 80) * Mathf.Clamp01((800 - y) / 80);
                Color color = tones[tone];
                color.a = edge;
                PixelUi.Text(text, width / 2, y, size, color, 0.5f);
            }
            GUI.color = previous;
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
