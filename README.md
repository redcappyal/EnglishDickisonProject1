# English Dickinson Project

**The Thing With Feathers**, a Flappy Bird game built on Emily Dickinson's "Hope" is the thing with feathers, in Unity 6.

**Play it in your browser:** https://flesheatingbacteria.github.io/EnglishDickisonProject1/ (Space, click or tap to flap; **M** mutes). The gear button opens **Settings**: volume, **easy mode** (a gentler boss fight) and the credits. **Levels** on the title screen starts from any stanza or straight at the boss (only a run from level 1 can set a record).

## Run

1. Open this folder in Unity Hub with **Unity 6000.6.5f1**.
2. Allow Unity to restore packages and import assets.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.
4. Press **Space**, click, or tap to launch from the nest and flap.

Three levels follow the three stanzas of "Hope" is the thing with feathers: **Hope**, **The Gale** and **Extremity**, 15 pipes each. Between levels (and at the start) a cutscene flies the bird through the next stanza while the world and its weather blend into the next one.

After the Gale's fifteenth pipe, a nine-second **Warmth** interlude brings out the sun over a village for "That kept so many warm -". Rain and storm clouds clear, the bird glides safely, and the music returns to the warm theme before the third stanza introduces the frozen land. Space or click continues once the storm has cleared.

v 1.4.1: **Hope never stops at all.** There is no game over: after a crash the game freezes for a second, then you flap and carry on. The score is simply how many times you crashed, and your record is the **fewest crashes** in a full run. Your **hope** bar drains as you fly and each crash costs some; grab hearts (placed a little out of the way) to refill it. With no hope left the bird still flies, just with weaker wingbeats. At the end of the Gale the storm surges ("And sore must be the storm"): heavier rain, lightning, gusts that shove the bird and faster pipes, until it breaks as you leave.

v 1.6.0: **The boss.** After Extremity's last pipe the world stops, the music goes quiet, and Emily Dickinson is waiting. She talks in dashes and Capitals and fights the way a certain skeleton does, with black cuts between attacks. She can't be hit back; you just have to keep flying until she tires. Hits count as crashes and cost hope, but the run never ends. When she falls asleep, fly to her, and stay for the credits. Sound effects (all synthesized in code) and a mute button (or **M**) are new too.

v 1.7.0 / 1.8.0: her attacks, in order. Between them a page of ink turns across the screen with the next attack's name on it:
1. **Fascicles** - she sewed her poems into little books by hand. Gates of stitched manuscript pages open and shut as they come; each is timed to be open as it reaches you, so it's about reading them and getting to the gap.
2. **Blanks** - a line of the poem with a word missing. Three windows, three words; fly through the right one before she inks the others shut.
3. **The Wave** ("tell all the truth - but tell it Slant -") - Geometry Dash's wave: hold to fly up at a slant, let go to fly down, through a narrowing zigzag tunnel whose walls close in around you.
4. **Quills** - giant quills charge and fire beams across the sky.
5. **My Letter to the World** - everything at once, with quick page flips between: gates and quills together, then a second, much harder Wave built like an easy demon level (narrow, speeding up, with flat stretches you tap through), more quills, and the line the whole game argues: "And never stops - at ___".

See [the framework guide](Assets/FlappyBird/README.md) for customization.

## Build for the web

In Unity, **Flappy Bird > Build WebGL** writes the site to `Builds/WebGL` (gzip with a built-in decompressor, so any static host works, GitHub Pages included). To check it locally, serve that folder (for example `python3 -m http.server --directory Builds/WebGL`) and open the page; opening `index.html` as a file won't work.

## Versions

The version is shown on the title screen and the poem page. Each is tagged in git (`v1.0.0` ... `v1.8.2`).

| Version | What changed |
|---|---|
| 1.0.0 | Pixel art; one level per stanza (Hope, The Gale, Extremity) |
| 1.1.0 | Pixel-art HUD, poem page and props |
| 1.2.0 | Upgraded to Unity 6000.6.5f1 |
| 1.3.0 | Hope bar and hearts, stanza cutscenes, weather, title screen |
| 1.4.0 | The nest's tree matches each world |
| 1.4.1 | Hope never stops: no game over, score is crashes; the Gale's storm surge |
| 1.5.0 | Smooth transitions everywhere; the tree glides in at each level; Reset Level button; rougher pipes; faster UI |
| 1.5.1 | Skipped cutscenes fade their words out; clearer reset icon |
| 1.6.0 | Boss fight against Emily Dickinson; sound effects; mute; WebGL build, playable online |
| 1.7.0 | New boss attacks (Fascicles, Blanks, the Wave, Quills, Letter to the World); end credits; no flap sound; the bird sits in its nest; rougher pixel art everywhere |
| 1.8.0 | One big tree from the ground in every world; page-turn transitions between boss attacks; longer Waves and a demon-level final Wave; Settings menu (volume slider, easy mode, credits); code tidied |
| 1.8.1 | Levels button on the title screen (any stanza, or straight to the boss); Stardew Valley in the credits |
| 1.8.2 | Fascicle gates are always open as they pass the bird (hard, never impossible); every Wave a little wider and slower |

A separate experimental version, **beta-version**, lives in the fork [flesheatingbacteria/EnglishDickisonProject1](https://github.com/flesheatingbacteria/EnglishDickisonProject1) ([play it](https://flesheatingbacteria.github.io/EnglishDickisonProject1/beta/)). It builds levels from the poem's form: meter, dashes and rhymes, and ends with "The Quiet" instead of the boss.

`Assets` (including `.meta` files), `Packages`, and `ProjectSettings` are versioned. Unity regenerates `Library`, `Temp`, and IDE project files locally.

Summary of changes written by Copilot. 
