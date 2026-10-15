"""Sprites for the Dickinson Flappy Bird levels. Needs Pillow.

Level 1 - "Hope" is the thing with feathers   (dawn)
Level 2 - The Gale                            (storm)
Level 3 - Extremity                           (frozen night sea)

Run from the repo root:  python Art/make_sprites.py
Writes the full art set to Art/sprites/, preview screenshots + GIFs to
Art/previews/, and the pieces the game loads to
Assets/Resources/DickinsonSprites/ (see PixelArt.cs).
"""
import math
import os
import random

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SPRITES = os.path.join(HERE, "sprites")
PREVIEWS = os.path.join(HERE, "previews")
GAME = os.path.join(HERE, "..", "Assets", "Resources", "DickinsonSprites")

W, H = 144, 256
GROUND_Y = 200
BLACK = (34, 34, 34, 255)
CLEAR = (0, 0, 0, 0)


def c(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def draw_map(rows, pal):
    img = Image.new("RGBA", (max(len(r) for r in rows), len(rows)), CLEAR)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in pal:
                px[x, y] = pal[ch]
    return img


def outline(img):
    px = img.load()
    w, h = img.size
    pts = [(x, y) for x in range(w) for y in range(h) if px[x, y][3] and px[x, y] != BLACK]
    for x, y in pts:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 0:
                px[x + dx, y + dy] = BLACK


def box(px, x0, y0, x1, y1, col, w=W, h=H):
    for y in range(max(0, y0), min(h, y1)):
        for x in range(max(0, x0), min(w, x1)):
            px[x, y] = col


def circle(px, cx, cy, r, col, w=W, h=H):
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if 0 <= x < w and 0 <= y < h and (x - cx) ** 2 + (y - cy) ** 2 <= r * r + r:
                px[x, y] = col


def paste(dst, src, x, y):
    dst.alpha_composite(src, (int(x), int(y)))


def big(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


# ------------------------------------------------------------------ bird
BIRD = [
    ".....KKKK......",
    "...KKBBBBKK....",
    "..KBBBBBWWWK...",
    ".KBBBBBBWWkWK..",
    ".KBBBBBBWWkWKK.",
    "KBBBBBBBBBBKOOK",
    "KBBBBBBBBBKOOK.",
    "KBBBBBBLLLLKK..",
    ".KKKBLLLLLLK...",
    "...KKLLLLLKK...",
    "....KKKKKK.....",
]
WING = {
    "up": (0, 1, ["KKK..", "KDDK.", "KDDDK", "KDDDD"]),
    "mid": (0, 5, ["KDDDD", "KDDDDD", "KDDDD"]),
    "down": (0, 6, ["KDDDD", "KDDDD", "KDDDK", ".KDK.", "..K.."]),
}
BIRD_PAL = {"K": BLACK, "B": c("#9b6a3c"), "L": c("#e8a65a"), "W": c("#ffffff"),
            "k": BLACK, "O": c("#f5b335"), "D": c("#6e4826")}


def make_bird(frame, hurt=False):
    rows = [list(r) for r in BIRD]
    ox, oy, wing = WING[frame]
    for dy, r in enumerate(wing):
        for dx, ch in enumerate(r):
            if ch != ".":
                rows[oy + dy][ox + dx] = ch
    if hurt:
        rows[3][10] = "W"
        rows[4][10] = "W"
        rows[3][8:11] = list("kkk")
    return draw_map(["".join(r) for r in rows], BIRD_PAL)


# ------------------------------------------------------------------ text
FONT = {
    "A": [".#.", "#.#", "###", "#.#", "#.#"], "B": ["##.", "#.#", "##.", "#.#", "##."],
    "C": [".##", "#..", "#..", "#..", ".##"], "D": ["##.", "#.#", "#.#", "#.#", "##."],
    "E": ["###", "#..", "##.", "#..", "###"], "F": ["###", "#..", "##.", "#..", "#.."],
    "G": [".##", "#..", "#.#", "#.#", ".##"], "H": ["#.#", "#.#", "###", "#.#", "#.#"],
    "I": ["###", ".#.", ".#.", ".#.", "###"], "K": ["#.#", "#.#", "##.", "#.#", "#.#"],
    "L": ["#..", "#..", "#..", "#..", "###"], "M": ["#...#", "##.##", "#.#.#", "#...#", "#...#"],
    "N": ["#..#", "##.#", "#.##", "#..#", "#..#"], "O": [".#.", "#.#", "#.#", "#.#", ".#."],
    "P": ["##.", "#.#", "##.", "#..", "#.."], "R": ["##.", "#.#", "##.", "#.#", "#.#"],
    "S": [".##", "#..", ".#.", "..#", "##."], "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "U": ["#.#", "#.#", "#.#", "#.#", "###"], "V": ["#.#", "#.#", "#.#", "#.#", ".#."],
    "W": ["#...#", "#...#", "#.#.#", "##.##", "#...#"], "X": ["#.#", "#.#", ".#.", "#.#", "#.#"],
    "Y": ["#.#", "#.#", ".#.", ".#.", ".#."],
    "0": ["###", "#.#", "#.#", "#.#", "###"], "1": [".#.", "##.", ".#.", ".#.", "###"],
    "2": ["##.", "..#", ".#.", "#..", "###"], "3": ["##.", "..#", ".#.", "..#", "##."],
    "4": ["#.#", "#.#", "###", "..#", "..#"], "5": ["###", "#..", "##.", "..#", "##."],
    "6": [".##", "#..", "###", "#.#", "###"], "7": ["###", "..#", ".#.", ".#.", ".#."],
    "8": ["###", "#.#", "###", "#.#", "###"], "9": ["###", "#.#", "###", "..#", "##."],
    "-": ["...", "...", "###", "...", "..."], ",": [".", ".", ".", "#", "#"],
    ".": [".", ".", ".", ".", "#"], "'": ["#", "#", ".", ".", "."], '"': ["#.#", "#.#", "...", "...", "..."],
    "J": ["..#", "..#", "..#", "#.#", ".#."], "Q": [".#.", "#.#", "#.#", "##.", ".##"],
    "Z": ["###", "..#", ".#.", "#..", "###"], "!": ["#", "#", "#", ".", "#"],
    "?": ["##.", "..#", ".#.", "...", ".#."], ":": [".", "#", ".", "#", "."],
    ";": [".", "#", ".", "#", "#"], "/": ["..#", "..#", ".#.", "#..", "#.."],
    "(": [".#", "#.", "#.", "#.", ".#"], ")": ["#.", ".#", ".#", ".#", "#."],
    "|": ["#", "#", "#", "#", "#"],
    " ": ["..", "..", "..", "..", ".."],
}


def text(s, col="#ffffff", scale=1, edge=True):
    glyphs = [FONT[ch] for ch in s]
    w = sum((len(g[0]) + 1) * scale for g in glyphs) + 2
    img = Image.new("RGBA", (w, 5 * scale + 2), CLEAR)
    px = img.load()
    x0 = 1
    for g in glyphs:
        for gy, row in enumerate(g):
            for gx, bit in enumerate(row):
                if bit == "#":
                    for sy in range(scale):
                        for sx in range(scale):
                            px[x0 + gx * scale + sx, 1 + gy * scale + sy] = c(col)
        x0 += (len(g[0]) + 1) * scale
    if edge:
        outline(img)
    return img


def textbox(lines, col="#ffffff", hi=None, hi_col="#ffd84a"):
    parts = [text(l, hi_col if i == hi else col, edge=False) for i, l in enumerate(lines)]
    w = max(p.width for p in parts) + 8
    h = len(parts) * 8 + 6
    img = Image.new("RGBA", (w, h), BLACK)
    px = img.load()
    box(px, 1, 1, w - 1, h - 1, c("#ffffff"), w, h)
    box(px, 2, 2, w - 2, h - 2, c("#3a3a4a"), w, h)
    for i, p in enumerate(parts):
        paste(img, p, (w - p.width) // 2, 4 + i * 8)
    return img


# ------------------------------------------------------------------ pipes
def make_pipe(length, top, body, body_hi, cap, cap_hi, extra=None):
    bw, cw, ch = 22, 26, 8
    img = Image.new("RGBA", (cw + 2, length + 8), CLEAR)
    px = img.load()
    cap_y = length - ch if top else 0
    b0, b1 = (0, cap_y) if top else (ch, length)
    box(px, 3, b0, 3 + bw, b1, c(body), cw + 2, length + 8)
    # a wobbly, hand-drawn highlight: its width changes every 6 rows (repeating every 18,
    # the body tile height, so tiled pipes still line up)
    for y in range(b0, b1):
        box(px, 5, y, 5 + (3, 2, 4)[(y % 18) // 6], y + 1, c(body_hi), cw + 2, length + 8)
    box(px, 1, cap_y, 1 + cw, cap_y + ch, c(cap), cw + 2, length + 8)
    box(px, 3, cap_y + 1, 6, cap_y + ch - 1, c(cap_hi), cw + 2, length + 8)
    if extra:
        extra(px, cap_y, b0, b1)
    if not top:
        padded = Image.new("RGBA", img.size, CLEAR)
        padded.paste(img.crop((0, 0, img.width, img.height - 1)), (0, 1))
        img = padded
    outline(img)
    return img


# In-game pipes are 9-slice tiled sprites (see PixelArt.cs): the plain body
# repeats, the cap end stays fixed. Both pieces keep exactly BODY_TILE rows of body.
BODY_TILE = 18
TOP_OVERHANG = 5  # rows of drips/spikes kept below the top pipe's gap edge


def game_pipes(fn):
    top = fn(80, True)  # cap occupies rows 72..79, gap edge outline at row 80
    top = top.crop((0, 72 - BODY_TILE, top.width, 81 + TOP_OVERHANG))
    bottom = fn(80, False)  # outline row 0, cap rows 1..8
    bottom = bottom.crop((0, 0, bottom.width, 9 + BODY_TILE))
    return top, bottom


# level 1: green vine w/ purple flowers on top, birch tree on bottom
def l1_pipe(length, top):
    if top:
        def drips(px, cy, b0, b1):
            for x, n in ((6, 4), (12, 2), (19, 5)):
                box(px, x, cy + 8, x + 2, cy + 8 + n, c("#a77fd8"), 28, length + 8)
        return make_pipe(length, True, "#4fa040", "#78c860", "#a77fd8", "#c9a8f0", drips)

    def marks(px, cy, b0, b1):
        rnd = random.Random(length)
        for y in range(b0 + 5, b1, 9):
            x = rnd.choice((6, 10, 14))
            box(px, x, y, x + 5, y + 1, BLACK, 28, length + 8)
    return make_pipe(length, False, "#eeeeee", "#ffffff", "#5cb84a", "#8ad870", marks)


# level 2: grey storm cloud on top, rock with white foam on bottom
def l2_pipe(length, top):
    if top:
        def bumps(px, cy, b0, b1):
            for x in range(1, 27, 6):
                box(px, x, cy + 8, x + 4, cy + 10, c("#9aa1b0"), 28, length + 8)
        return make_pipe(length, True, "#7d8597", "#9aa1b0", "#9aa1b0", "#c0c6d2", bumps)
    return make_pipe(length, False, "#4a4f5c", "#62687a", "#f0f4f8", "#ffffff")


# level 3: icicle hanging on top, ice pillar w/ snow on bottom
def l3_pipe(length, top):
    if top:
        def spikes(px, cy, b0, b1):
            for x0 in (2, 10, 18):
                for i in range(4):
                    box(px, x0 + i, cy + 8 + i, x0 + 7 - i, cy + 9 + i, c("#c8ecff"), 28, length + 8)
        return make_pipe(length, True, "#a8d8f0", "#d8f0ff", "#c8ecff", "#ffffff", spikes)

    def snow(px, cy, b0, b1):
        for x in range(2, 26, 7):
            box(px, x, 0, x + 4, 2, c("#ffffff"), 28, length + 8)

    img = make_pipe(length + 2, False, "#7fc0e0", "#a8d8f0", "#ffffff", "#ffffff", snow)
    return img


# ------------------------------------------------------------------ backgrounds
# Backgrounds are wider than the 144px screen so wide monitors see more scenery, not
# mirrored copies of the landmarks. The original 144px composition sits in the middle.
BG_W = 512
MID = (BG_W - W) // 2


def cloud(px, x, y, col, w=W):
    box(px, x + 4, y, x + 14, y + 4, col, w)
    box(px, x, y + 3, x + 22, y + 9, col, w)
    box(px, x + 12, y - 2, x + 19, y + 4, col, w)


def house(px, x, y, wall, roof, win, w=W):
    box(px, x, y, x + 16, y + 12, wall, w)
    for i in range(7):
        box(px, x - 1 + i, y - 1 - i, x + 17 - i, y - i, roof, w)
    box(px, x + 12, y - 9, x + 15, y - 3, roof, w)
    box(px, x + 3, y + 3, x + 6, y + 6, win, w)
    box(px, x + 10, y + 3, x + 13, y + 6, win, w)
    box(px, x + 7, y + 6, x + 10, y + 12, c("#4a2a1a"), w)


# Backgrounds wrap around: the right edge continues into the left, so the game can
# scroll them forever. Wrapped drawing helpers below keep every shape seamless.
def wbox(px, x0, y0, x1, y1, col):
    for y in range(max(0, y0), min(H, y1)):
        for x in range(x0, x1):
            px[x % BG_W, y] = col


def wcircle(px, cx, cy, r, col):
    for y in range(max(0, cy - r), min(H, cy + r + 1)):
        for x in range(cx - r, cx + r + 1):
            if (x - cx) ** 2 + (y - cy) ** 2 <= r * r + r:
                px[x % BG_W, y] = col


def wcloud(px, x, y, col):
    wbox(px, x + 4, y, x + 14, y + 4, col)
    wbox(px, x, y + 3, x + 22, y + 9, col)
    wbox(px, x + 12, y - 2, x + 19, y + 4, col)


def whouse(px, x, y, wall, roof, win):
    wbox(px, x, y, x + 16, y + 12, wall)
    for i in range(7):
        wbox(px, x - 1 + i, y - 1 - i, x + 17 - i, y - i, roof)
    wbox(px, x + 12, y - 9, x + 15, y - 3, roof)
    wbox(px, x + 3, y + 3, x + 6, y + 6, win)
    wbox(px, x + 10, y + 3, x + 13, y + 6, win)
    wbox(px, x + 7, y + 6, x + 10, y + 12, c("#4a2a1a"))


def l1_background():
    img = Image.new("RGBA", (BG_W, H), c("#c7b3ea"))
    px = img.load()
    wcircle(px, MID + 108, 60, 14, c("#ffe680"))
    for x, y in ((14, 70), (70, 30), (130, 110), (MID + 20, 40), (MID + 70, 90), (MID + 112, 130),
                 (360, 56), (412, 104), (470, 36), (500, 140)):
        wcloud(px, x, y, c("#ffffff"))
    for x, y, r, col in ((40, 226, 50, "#6cbf5a"), (128, 222, 46, "#5aa84a"), (MID + 30, 215, 50, "#6cbf5a"),
                         (MID + 120, 225, 55, "#5aa84a"), (384, 220, 48, "#6cbf5a"), (470, 226, 52, "#5aa84a")):
        wcircle(px, x, y, r, c(col))
    whouse(px, MID + 40, 166, c("#a0603a"), c("#c0392b"), c("#ffe680"))
    for i in range(4):  # smoke
        wcircle(px, MID + 54 + i, 150 - i * 7, 2, c("#eeeeee"))
    return img


def l2_background(flash=False):
    img = Image.new("RGBA", (BG_W, H), c("#4a5270" if flash else "#2d3348"))
    px = img.load()
    for x in range(0, BG_W, 32):
        wcloud(px, x - 6, 6 if (x // 32) % 2 else 0, c("#555d72"))
    rnd = random.Random(2)
    for _ in range(140):
        x, y = rnd.randrange(BG_W), rnd.randrange(30, 180)
        for i in range(4):
            px[(x - i // 2) % BG_W, y + i] = c("#56607e")
    pts = [(MID + 100, 20), (MID + 92, 46), (MID + 102, 46), (MID + 90, 80)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for t in range(n + 1):
            x, y = round(x0 + (x1 - x0) * t / n), round(y0 + (y1 - y0) * t / n)
            wbox(px, x, y, x + 2, y + 1, c("#ffe866"))
    wbox(px, 0, 176, BG_W, GROUND_Y, c("#233048"))
    for x in range(0, BG_W, 16):
        wbox(px, x + 2, 174, x + 9, 177, c("#dfe8f2"))
    for dx, top in ((-24, 184), (-16, 172), (-8, 160), (0, 150)):  # the cliff steps up from the sea
        wbox(px, MID + dx, top, MID + 60, GROUND_Y, c("#1a1d28"))
    wbox(px, MID + 60, 162, MID + 70, GROUND_Y, c("#1a1d28"))
    whouse(px, MID + 30, 138, c("#1a1d28"), c("#1a1d28"), c("#ffd860"))
    for i in range(4):  # smoke blown sideways
        wcircle(px, MID + 40 - i * 6, 126 - i, 2, c("#555d72"))
    return img


def l3_background():
    img = Image.new("RGBA", (BG_W, H), c("#10183a"))
    px = img.load()
    rnd = random.Random(3)
    for _ in range(160):
        px[rnd.randrange(BG_W), rnd.randrange(150)] = c("#ffffff")
    for band, col, y0 in ((0, "#3fe0a0", 50), (1, "#2aa0c0", 60)):  # aurora, 6 waves per loop
        for x in range(BG_W):
            y = y0 + int(math.sin(x * math.tau * 6 / BG_W + band) * 8)
            wbox(px, x, y, x + 1, y + 4, c(col))
    wcircle(px, MID + 116, 104, 11, c("#f4f4ff"))
    wcircle(px, MID + 112, 101, 2, c("#c8c8dc"))
    wcircle(px, MID + 119, 108, 3, c("#c8c8dc"))
    wbox(px, 0, 168, BG_W, GROUND_Y, c("#1c3a6a"))
    for x0, w0, h0 in ((8, 22, 16), (70, 30, 26), (130, 16, 10), (MID + 20, 26, 22), (MID + 66, 18, 14),
                       (MID + 96, 30, 28), (352, 20, 12), (400, 34, 30), (480, 24, 18)):  # icebergs
        for i in range(h0):
            half = w0 // 2 * (i + 1) // h0
            cx = x0 + w0 // 2
            wbox(px, cx - half, 168 - h0 + i, cx, 169 - h0 + i, c("#e6f4ff"))
            wbox(px, cx, 168 - h0 + i, cx + half, 169 - h0 + i, c("#a8cce8"))
    for x in range(0, BG_W, 8):
        wbox(px, x, 178 + (x // 8) % 4 * 5, x + 4, 179 + (x // 8) % 4 * 5, c("#3a6aa0"))
    return img


def ground(top, top_dark, fill, speck, width=W + 24):
    img = Image.new("RGBA", (width, H - GROUND_Y), c(fill))
    px = img.load()
    box(px, 0, 0, width, 1, BLACK, width, H)
    box(px, 0, 1, width, 7, c(top), width, H)
    for x in range(0, width, 8):  # stripes
        box(px, x, 1, x + 4, 7, c(top_dark), width, H)
    box(px, 0, 7, width, 8, BLACK, width, H)
    rnd = random.Random(1)
    for _ in range(30):
        x, y = rnd.randrange(0, width - 2), rnd.randrange(12, H - GROUND_Y - 2)
        x -= x % 24 if x > width - 24 else 0
        box(px, x, y, x + 2, y + 1, c(speck), width, H)
    return img


# ------------------------------------------------------------------ pickups
NOTE = draw_map(["...##", "...#.#", "...#..", ".###..", "####..", ".##..."], {"#": c("#ffd84a")})
FEATHER = draw_map(["....#", "...##", "..##.", ".##..", "#...."], {"#": c("#ffd84a")})
CRUMB = draw_map([".##.", "#oo#", "#o##", ".##."], {"#": c("#c88a3a"), "o": c("#f0c070")})


def with_outline(p):
    out = Image.new("RGBA", (p.width + 2, p.height + 2), CLEAR)
    paste(out, p, 1, 1)
    outline(out)
    return out


NOTE, FEATHER, CRUMB = with_outline(NOTE), with_outline(FEATHER), with_outline(CRUMB)


# ------------------------------------------------------------------ levels
LEVELS = [
    dict(
        folder="level1_hope", n=1, title="HOPE", title_col="#ffb84a",
        bg=l1_background, ground=lambda: ground("#8ccf5a", "#74b84a", "#c89b6a", "#a87a4a"),
        pipe=l1_pipe, pickup=NOTE,
        stanza=['"HOPE" IS THE THING', "WITH FEATHERS -", "THAT PERCHES IN THE SOUL -",
                "AND SINGS THE TUNE", "WITHOUT THE WORDS -", "AND NEVER STOPS - AT ALL -"],
        over="NEVER STOPS", over_lines=["- AT ALL -", "TAP TO SING AGAIN"],
    ),
    dict(
        folder="level2_gale", n=2, title="THE GALE", title_col="#cfd8e8",
        bg=l2_background, ground=lambda: ground("#7a8aa8", "#66768f", "#4a4458", "#3a3448"),
        pipe=l2_pipe, pickup=FEATHER, flash=lambda: l2_background(True),
        stanza=["AND SWEETEST - IN THE GALE -", "IS HEARD -", "AND SORE MUST BE THE STORM -",
                "THAT COULD ABASH THE", "LITTLE BIRD", "THAT KEPT SO MANY WARM -"],
        over="ABASHED", over_lines=["AND SORE MUST BE THE STORM -", "THAT COULD ABASH", "THE LITTLE BIRD"],
    ),
    dict(
        folder="level3_extremity", n=3, title="EXTREMITY", title_col="#a8e8ff",
        bg=l3_background, ground=lambda: ground("#f4f8ff", "#d8e8f8", "#9cc4e4", "#7aaad0"),
        pipe=l3_pipe, pickup=CRUMB,
        stanza=["I'VE HEARD IT IN THE", "CHILLEST LAND -", "AND ON THE STRANGEST SEA -",
                "YET - NEVER - IN EXTREMITY,", "IT ASKED A CRUMB - OF ME."],
        over="NOT A CRUMB", over_lines=["YET - NEVER - IN EXTREMITY,", "IT ASKED A CRUMB - OF ME."],
    ),
]


def pipe_pair(fn, x, gap_y, gap=60):
    return [(fn(gap_y, True), x, 0), (fn(GROUND_Y - gap_y - gap, False), x, gap_y + gap)]


def build(level, birds, hurt):
    out = os.path.join(SPRITES, level["folder"])
    prev = os.path.join(PREVIEWS, level["folder"])
    os.makedirs(out, exist_ok=True)
    os.makedirs(prev, exist_ok=True)
    n = level["n"]
    wide = scuff(level["bg"](), 20 + n, shape=False, tint=0.015)
    gr = scuff(level["ground"](), 30 + n, shape=False, tint=0.04)
    fn = lambda length, top, make=level["pipe"]: scuff(make(length, top), 40 + n, shape=True, tint=0.07)
    bg = wide.crop((MID, 0, MID + W, H))
    title = text(level["title"], level["title_col"], scale=3 if len(level["title"]) < 9 else 2)

    wide.save(f"{out}/background.png")
    gr.save(f"{out}/ground.png")
    fn(160, True).save(f"{out}/pipe_top.png")
    fn(160, False).save(f"{out}/pipe_bottom.png")
    level["pickup"].save(f"{out}/pickup.png")
    game = os.path.join(GAME, level["folder"])
    os.makedirs(game, exist_ok=True)
    wide.save(f"{game}/background.png")
    gr.save(f"{game}/ground.png")
    gtop, gbottom = game_pipes(fn)
    gtop.save(f"{game}/pipe_top.png")
    gbottom.save(f"{game}/pipe_bottom.png")
    title.save(f"{out}/title.png")
    textbox(level["stanza"], hi=len(level["stanza"]) - 1).save(f"{out}/stanza.png")

    # title screen
    t = bg.copy()
    paste(t, gr, 0, GROUND_Y)
    lv = text(f"LEVEL {level['n']}")
    paste(t, lv, (W - lv.width) // 2, 20)
    paste(t, title, (W - title.width) // 2, 30)
    tb = textbox(level["stanza"], hi=len(level["stanza"]) - 1)
    paste(t, tb, (W - tb.width) // 2, 62)
    paste(t, birds["mid"], (W - 15) // 2, 130)
    big(t, 4).save(f"{prev}/title_screen.png")

    # gameplay
    g = bg.copy()
    for img, x, y in pipe_pair(fn, 60, 62) + pipe_pair(fn, 132, 92):
        paste(g, img, x, y)
    paste(g, gr, 0, GROUND_Y)
    paste(g, level["pickup"], 100, 120)
    paste(g, birds["mid"], 26, 104)
    s = text("7", scale=3)
    paste(g, s, (W - s.width) // 2, 14)
    big(g, 4).save(f"{prev}/gameplay.png")

    # game over
    o = bg.copy()
    for img, x, y in pipe_pair(fn, 44, 56) + pipe_pair(fn, 118, 90):
        paste(o, img, x, y)
    paste(o, gr, 0, GROUND_Y)
    ov = text(level["over"], level["title_col"], scale=2)
    paste(o, ov, (W - ov.width) // 2, 64)
    ob = textbox(level["over_lines"], hi=0 if level["n"] == 1 else None)
    paste(o, ob, (W - ob.width) // 2, 82)
    paste(o, hurt, 64, GROUND_Y - hurt.height + 1)
    big(o, 4).save(f"{prev}/game_over.png")

    # gif
    seq = ["up", "mid", "down", "mid"]
    pairs = [pipe_pair(fn, 0, gy) for gy in (60, 94, 74)]
    flash = level["flash"]().crop((MID, 0, MID + W, H)) if "flash" in level else None
    frames = []
    for i in range(60):
        f = (flash if flash and i in (34, 36) else bg).copy()
        off = i * 2
        for k, pp in enumerate(pairs):
            x = ((150 + k * 72 - off + 60) % 216) - 60
            for img, _, y in pp:
                paste(f, img, x, y)
        paste(f, gr, -(off % 24), GROUND_Y)
        paste(f, birds[seq[(i // 3) % 4]], 26, 100 + int(math.sin(i / 60 * math.tau * 2) * 14))
        s = text(str(3 + i // 20), scale=3)
        paste(f, s, (W - s.width) // 2, 14)
        frames.append(big(f, 3).convert("RGB").quantize(colors=64, dither=Image.NONE))
    frames[0].save(f"{prev}/gameplay.gif", save_all=True, append_images=frames[1:], duration=60, loop=0)


# ------------------------------------------------------------------ scuffing
def _noise(x, y, salt):
    n = (x * 374761393 + y * 668265263 + salt * 2147483647) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return (n ^ (n >> 16)) / 0xFFFFFFFF


def scuff(img, salt, shape=True, tint=0.05):
    """Hand-made wobble: a few colours a shade off, and (if shape) a few outline pixels
    dropped or doubled. Tiling art should pass shape=False so its edges still match."""
    img = img.copy()
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if px[x, y] != BLACK and _noise(x, y, salt) < tint:
                d = 14 if _noise(y, x, salt + 1) < 0.5 else -14
                px[x, y] = (max(0, min(255, r + d)), max(0, min(255, g + d)), max(0, min(255, b + d)), a)
            elif shape and px[x, y] == BLACK and _noise(x, y, salt + 2) < 0.04:
                # nudge this outline pixel one step outward, as if drawn a bit freehand
                for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] == 0:
                        px[nx, ny] = BLACK
                        break
    return img


# Tiling art keeps its exact shape so its seams still line up.
TILES = ("background", "ground", "pipe_top", "pipe_bottom", "panel", "parchment", "bar", "button", "button_hover",
         "screen_frame", "wave_block")
# 9-slice UI frames stretch their middle, so a single off pixel there would become a big block.
STRETCHED = ("panel", "parchment", "bar", "button", "button_hover", "screen_frame", "wave_block")
# v1.8: the big pieces are drawn at half resolution (chunkier pixels), as if painted with a bigger brush.
CHUNKY = ("background", "tree_0", "tree_1", "tree_2", "crown_0", "crown_1", "crown_2", "storm_cloud")


def worsen(img, salt, tiles, stretched=False):
    """v1.7: rougher still, as if drawn quickly with a cheap palette. Colours snap to coarse
    steps (so shading bands), more pixels land a shade off, and on non-tiling art the odd row
    slips a pixel sideways and a few stray pixels bleed past the edges."""
    img = img.copy()
    px = img.load()
    w, h = img.size
    if not tiles:
        for y in range(h):
            if _noise(y, 3, salt + 9) < 0.12:  # this row was drawn a pixel too far right
                row = [px[x, y] for x in range(w)]
                for x in range(w):
                    px[x, y] = row[x - 1] if x > 0 else CLEAR
        bleed = []
        for y in range(h):
            for x in range(w):
                if px[x, y][3] == 0 and _noise(x, y, salt + 7) < 0.08:
                    for dx, dy in ((-1, 0), (0, -1), (1, 0), (0, 1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3]:
                            bleed.append((x, y, px[nx, ny]))
                            break
        for x, y, col in bleed:
            px[x, y] = col
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            if not stretched and _noise(x, y, salt + 5) < (0.012 if tiles else 0.03):
                d = 26 if _noise(y, x, salt + 6) < 0.5 else -26
                r, g, b = r + d, g + d, b + d
            r, g, b = (max(0, min(255, int(round(v / 24)) * 24)) for v in (r, g, b))
            px[x, y] = (r, g, b, a)
    return img


def cheapen(img, salt, stem):
    """v1.8: the look of three students and a weekend. A smaller palette (each sprite keeps just
    its dozen or so main colours), chunkier pixels on the big pieces, and the odd gap in an outline."""
    img = img.copy()
    w, h = img.size
    if stem in CHUNKY and w % 2 == 0:
        px = img.load()
        for y in range(0, h, 2):
            for x in range(0, w, 2):
                col = px[x, y]
                for dx, dy in ((1, 0), (0, 1), (1, 1)):
                    if x + dx < w and y + dy < h:
                        px[x + dx, y + dy] = col
    alpha = img.getchannel("A")
    colours = 10 if stem in CHUNKY else 12
    flat = img.convert("RGB").quantize(colors=colours, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGB")
    img = Image.merge("RGBA", (*flat.split(), alpha))
    if stem not in TILES:
        px = img.load()
        gaps = [(x, y) for y in range(h) for x in range(w)
                if px[x, y][3] and sum(px[x, y][:3]) < 150 and _noise(x, y, salt + 11) < 0.06
                and any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for x, y in gaps:
            px[x, y] = CLEAR
    return img


def worsen_all(folder):
    """Runs worsen() over every PNG the game loads (the font is drawn in code and stays crisp)."""
    for root, _, files in os.walk(folder):
        for name in sorted(files):
            if not name.endswith(".png"):
                continue
            path = os.path.join(root, name)
            stem = name[:-4]
            salt = sum(ord(ch) for ch in os.path.relpath(path, folder))
            img = worsen(Image.open(path).convert("RGBA"), salt, stem in TILES, stem in STRETCHED)
            (img if stem in STRETCHED else cheapen(img, salt, stem)).save(path)


# ------------------------------------------------------------------ UI + props
# 9-slice frames: K outline, L rim, D inner band, F fill. Borders are 3px.
def frame(rows, pal):
    return draw_map(rows, pal)


FRAME_ROWS = [
    ".KKKKKKKKKK.",
    "KLLLLLLLLLLK",
    "KLDDDDDDDDLK",
    "KLDFFFFFFDLK",
    "KLDFFFFFFDLK",
    "KLDFFFFFFDLK",
    "KLDFFFFFFDLK",
    "KLDFFFFFFDLK",
    "KLDFFFFFFDLK",
    "KLDDDDDDDDLK",
    "KLLLLLLLLLLK",
    ".KKKKKKKKKK.",
]
BUTTON_ROWS = [
    ".KKKKKKKKKK.",
    "KHHHHHHHHHHK",
    "KHFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KFFFFFFFFFFK",
    "KSSSSSSSSSSK",
    "KSSSSSSSSSSK",
    ".KKKKKKKKKK.",
]
SCREEN_ROWS = [
    "KKKKKKKKKKKK",
    "KKKKKKKKKKKK",
    "KKLLLLLLLLKK",
    "KKLKKKKKKLKK",
    "KKLK....KLKK",
    "KKLK....KLKK",
    "KKLK....KLKK",
    "KKLK....KLKK",
    "KKLKKKKKKLKK",
    "KKLLLLLLLLKK",
    "KKKKKKKKKKKK",
    "KKKKKKKKKKKK",
]


def reset_icon():
    """A circular arrow: back to the start of the level."""
    return draw_map([
        "...###.#.",
        "..#...##.",
        ".#...###.",
        ".#.......",
        ".#.....#.",
        "..#...#..",
        "...###...",
    ], {"#": c("#3a1f0d")})


def heart():
    return with_outline(draw_map([
        ".RR..RRR.",
        "RWWRRRRRR",
        "RWRRRRRRR",
        "RRRRRRRRD",
        ".RRRRRRD.",
        "..RRRRD..",
        "...RRD...",
        "....R....",
    ], {"R": c("#e8403a"), "W": c("#ffd0c8"), "D": c("#a82828")}))


def gear():
    """The settings button: a cog."""
    return draw_map([
        "...#.#...",
        ".#.###.#.",
        "..#####..",
        "####.####",
        "###...###",
        "####.####",
        "..#####..",
        ".#.###.#.",
        "...#.#...",
    ], {"#": c("#3a1f0d")})


def wave_block():
    """One block of the boss Wave's walls, Geometry Dash style: it tiles in every direction."""
    return draw_map([
        "kkkkkkkkkkkkkkkk",
        "kLLLLLLLLLLLLLDk",
        "kLMMMMMMMMMMMMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFHHFFFFFFFMDk",
        "kLMFHFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMFFFFFFFFFFMDk",
        "kLMMMMMMMMMMMMDk",
        "kDDDDDDDDDDDDDDk",
        "kkkkkkkkkkkkkkkk",
    ], {"k": c("#120c22"), "L": c("#6a52b0"), "M": c("#3e2c78"), "F": c("#2a1e56"), "D": c("#1c1440"), "H": c("#8a74d0")})


def quill(open_nib):
    """The boss's 'blaster': a giant quill pen, nib pointing left. Open, it fires ink-light."""
    img = Image.new("RGBA", (30, 14), CLEAR)
    q = img.load()
    vane, shade, rib = c("#f4f0e6"), c("#c8c0b4"), c("#8a7a6a")
    for x in range(9, 30):  # the feather: a long vane above the shaft, a narrow one below, curling up at the back
        top = int(6 * math.sin(math.pi * (x - 8) / 23) ** 0.7)
        bottom = int(2.5 * math.sin(math.pi * (x - 8) / 23))
        lift = (x - 20) // 4 if x > 20 else 0
        for y in range(7 - top - lift, 8 + bottom - lift):
            if 0 <= y < 14:
                q[x, y] = shade if y > 7 - lift else vane
        if x % 4 == 1 and top > 2:  # notches in the vane
            q[x, 7 - top - lift] = CLEAR
    box(q, 6, 7, 30, 8, rib, 30, 14)  # the shaft
    nib, ink = c("#d8b04a"), c("#3a2a1a")
    if open_nib:
        for x, y in ((0, 4), (1, 4), (2, 5), (3, 5), (4, 6), (5, 6), (0, 10), (1, 10), (2, 9), (3, 9), (4, 8), (5, 8)):
            q[x, y] = nib
        box(q, 6, 5, 9, 10, nib, 30, 14)
        for x in range(0, 6):
            q[x, 7] = c("#fff6c0")
    else:
        for x in range(0, 9):
            half = max(0, (x - 1) // 3)
            for y in range(7 - half, 8 + half):
                q[x, y] = nib
        q[0, 7] = ink; q[1, 7] = ink; q[5, 7] = ink
    outline(img)
    return img


def dickinson(face):
    """Emily Dickinson, white dress and all: face is 'idle', 'sleep' or 'smile'."""
    w, h = 26, 48
    img = Image.new("RGBA", (w, h), CLEAR)
    q = img.load()
    skin, skin_d = c("#f2d2b0"), c("#d8ae8a")
    hair, hair_l = c("#3a2418"), c("#5a3a28")
    dress, dress_d, lace = c("#ece6da"), c("#c8c0b4"), c("#ffffff")
    drop = 1 if face == "sleep" else 0  # her head nods when she dozes off
    # the dress: a fitted bodice, then a full skirt down to the hem
    for y in range(18, h):
        half = 5 if y < 28 else min(12, 5 + int((y - 28) * 0.45))
        for x in range(13 - half, 13 + half + 1):
            q[x, y] = dress_d if (x - 13 + 30) % 4 == 0 and y > 30 else dress
        q[13 + half, y] = dress_d
    for x in range(1, 25):
        if q[x, h - 1][3]:
            q[x, h - 1] = dress_d
    # sleeves, and hands clasped at the waist holding a quill
    for y in range(19, 30):
        for x in (6, 7, 19, 20):
            q[x, y] = dress_d if x in (6, 20) else dress
    box(q, 8, 27, 19, 29, c("#2a1a2a"), w, h)  # a dark sash at the waist
    for y in range(24, 30):  # forearms bending in to the hands
        q[8 + (y - 24) // 2, y] = dress_d; q[18 - (y - 24) // 2, y] = dress_d
    box(q, 11, 28, 16, 31, skin, w, h)
    box(q, 11, 30, 16, 31, skin_d, w, h)
    for i, (x, y) in enumerate(((16, 28), (17, 27), (18, 26), (19, 25), (20, 24), (21, 23), (22, 22))):
        q[x, y] = c("#fff6e0") if i else c("#3a2a1a")
        if i > 2:
            q[x, y - 1] = c("#f4f0e6")
    # lace collar and the velvet ribbon at her neck
    box(q, 12, 16 + drop, 15, 18 + drop, skin, w, h)
    box(q, 11, 17 + drop, 16, 18 + drop, c("#2a1a2a"), w, h)
    for x in range(9, 18):
        q[x, 18] = lace
        if x % 2:
            q[x, 19] = lace
    # her head: hair parted down the middle and smoothed over the ears
    cy = 10 + drop
    for y in range(2, 18):
        for x in range(6, 21):
            if ((x - 13) / 5.2) ** 2 + ((y - cy) / 6.2) ** 2 <= 1:
                q[x, y] = skin
    for y in range(1, 18):
        for x in range(5, 22):
            inside = ((x - 13) / 6.3) ** 2 + ((y - cy + 1) / 7) ** 2 <= 1
            # pulled back from the forehead, smoothed down over the ears
            if inside and (y < cy - 4 + drop - (1 if 10 <= x <= 16 else 0) or x < 8 or x > 18) and y < cy + 3:
                q[x, y] = hair
    for y in range(cy - 7, cy - 4):
        q[13, y] = hair_l  # the centre part
    q[12, cy - 6] = hair_l; q[14, cy - 6] = hair_l
    eye = c("#1a1418")
    if face == "sleep":
        for x in (10, 11, 15, 16):
            q[x, cy + 1] = eye
        q[13, cy + 4] = c("#9a4a3a")
    elif face == "smile":
        for x, y in ((10, cy + 1), (11, cy), (12, cy + 1), (14, cy + 1), (15, cy), (16, cy + 1)):
            q[x, y] = eye
        for x, y in ((11, cy + 3), (12, cy + 4), (13, cy + 4), (14, cy + 4), (15, cy + 3)):
            q[x, y] = c("#9a4a3a")
    else:  # a small, knowing smirk
        q[11, cy] = eye; q[11, cy + 1] = eye; q[15, cy] = eye; q[15, cy + 1] = eye
        for x, y in ((12, cy + 4), (13, cy + 4), (14, cy + 4), (15, cy + 3)):
            q[x, y] = c("#9a4a3a")
    q[9, cy + 3] = c("#f0b0a0"); q[17, cy + 3] = c("#f0b0a0")  # a little colour in her cheeks
    outline(img)
    return img


def ui_sprites():
    ink = c("#141018")
    out = {
        "panel": frame(FRAME_ROWS, {"K": ink, "L": c("#f2e6c8"), "D": c("#3a3550"), "F": (35, 32, 58, 235)}),
        "parchment": frame(FRAME_ROWS, {"K": c("#3a2418"), "L": c("#8a5a3a"), "D": c("#d8c8a0"), "F": c("#f2e6c8")}),
        "bar": frame(FRAME_ROWS, {"K": ink, "L": c("#f2e6c8"), "D": ink, "F": ink}),
        "button": frame(BUTTON_ROWS, {"K": ink, "H": c("#ffe2a0"), "F": c("#ffb84a"), "S": c("#c8782a")}),
        "button_hover": frame(BUTTON_ROWS, {"K": ink, "H": c("#fff0c8"), "F": c("#ffcc70"), "S": c("#d88a3a")}),
        "screen_frame": frame(SCREEN_ROWS, {"K": c("#1a1726"), "L": c("#f2e6c8")}),
        "feather": with_outline(draw_map([
            "......YY", ".....YWY", "....YWY.", "...YWY..", "..YWY...", ".YWY....", ".YY.....", "Y......."],
            {"Y": c("#ffd84a"), "W": c("#fff2b0")})),
        "heart": heart(),
        "reset": reset_icon(),
        "quill": quill(False),
        "quill_open": quill(True),
        "settings": gear(),
        "wave_block": wave_block(),
    }
    return out


# The nest's tree. Both layers share one canvas; y counts up from the trunk's base, which is
# buried half a unit below the ground line. PixelArt.cs uses the same numbers.
TREE_W, TREE_H, TREE_BASE_X, TREE_CROWN_Y, TREE_NEST_Y = 128, 200, 68, 135, 117


def tree(level):
    """One big tree growing out of the ground: flared roots, a thick trunk that tapers as it
    climbs, the branch the nest sits on, and a crown to match the world - a full summer
    canopy, storm-bent bare limbs with a few torn leaves, or frozen limbs capped with snow."""
    W, H, bx = TREE_W, TREE_H, TREE_BASE_X
    trunk, crown = Image.new("RGBA", (W, H), CLEAR), Image.new("RGBA", (W, H), CLEAR)
    t, k = trunk.load(), crown.load()
    rnd = random.Random(300 + level)
    light, mid, dark = [(c("#a0703f"), c("#7a4e2a"), c("#4e3018")), (c("#8a7a72"), c("#5e4a44"), c("#3a2c28")),
                        (c("#6a5a5a"), c("#4a3a3a"), c("#2e2424"))][level]

    def put(px, x, y, col):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < W and 0 <= y < H:
            px[x, H - 1 - y] = col

    def solid(px, x, y):
        x, y = int(round(x)), int(round(y))
        return 0 <= x < W and 0 <= y < H and px[x, H - 1 - y][3] > 0

    def limb(px, x0, y0, x1, y1, thick, col=None):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        flat = abs(x1 - x0) > abs(y1 - y0)
        for i in range(int(n) + 1):
            x, y = x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n
            th = max(1, round(thick * (1 - 0.55 * i / n)))
            for d in range(th):
                tone = col or (light if d == 0 else dark if d == th - 1 and th > 2 else mid)
                put(px, x, y + d - th // 2, tone) if flat else put(px, x + d - th // 2, y, tone)

    # the trunk: wide where the roots spread into the ground, tapering towards the crown
    top = TREE_CROWN_Y + 16
    for y in range(top):
        half = 10 - 4.5 * y / top + max(0, 34 - y) ** 1.6 * 0.045
        wob = round(math.sin(y * 0.21) * 1.2)
        x0, x1 = round(bx - half + wob), round(bx + half + wob)
        for x in range(x0, x1 + 1):
            f = (x - x0) / max(1, x1 - x0)
            put(t, x, y, light if f < 0.25 else dark if f > 0.72 else mid)
    for _ in range(45):  # bark: dark streaks
        x, y = bx + rnd.randint(-6, 6), rnd.randint(14, top - 6)
        for i in range(rnd.randint(2, 6)):
            if solid(t, x, y + i):
                put(t, x, y + i, dark)
    for kx, ky in ((bx - 3, 60), (bx + 2, 96)):  # a couple of knots
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            put(t, kx + dx, ky + dy, dark)
    # the nest's branch, growing out to the right and rising a little, with a twig
    limb(t, bx + 4, TREE_NEST_Y - 4, bx + 52, TREE_NEST_Y - 1, 6)
    limb(t, bx + 42, TREE_NEST_Y, bx + 49, TREE_NEST_Y + 9, 2)

    if level == 0:  # summer: limbs into a full canopy, a few apples
        limb(t, bx, top - 4, bx - 26, top + 26, 5)
        limb(t, bx, top - 4, bx + 22, top + 24, 5)
        puffs = [(-42, 150, 16), (-24, 170, 22), (2, 178, 20), (24, 166, 20), (38, 148, 15), (-34, 132, 15),
                 (-10, 148, 24), (16, 142, 20), (32, 128, 12), (-52, 140, 10), (4, 192, 8)]
        for dx, cy, r in puffs:
            for y in range(cy - r, cy + r + 1):
                for x in range(bx + dx - r, bx + dx + r + 1):
                    if (x - bx - dx) ** 2 + (y - cy) ** 2 <= r * r + r:
                        put(k, x, y, c("#4fa040"))
        for dx, cy, r in puffs:  # sunlit upper-left of each clump
            hx, hy, hr = bx + dx - r // 4, cy + r // 4, r // 2
            for y in range(hy - hr, hy + hr + 1):
                for x in range(hx - hr, hx + hr + 1):
                    if (x - hx) ** 2 + (y - hy) ** 2 <= hr * hr:
                        put(k, x, y, c("#7cc860"))
        for _ in range(140):
            x, y = rnd.randrange(W), rnd.randrange(110, H)
            if solid(k, x, y):
                put(k, x, y, c("#3a7a32") if rnd.random() < 0.7 else c("#b6e48a"))
        for dx, cy in ((-30, 160), (8, 172), (26, 150), (-14, 140), (-44, 146)):
            put(k, bx + dx, cy, c("#e8403a")); put(k, bx + dx + 1, cy, c("#e8403a"))
            put(k, bx + dx, cy - 1, c("#a82828")); put(k, bx + dx + 1, cy - 1, c("#e8403a"))
    else:
        # bare limbs from the top of the trunk: bent away from the wind in the storm, frozen in winter
        if level == 1:
            limbs = [(0, 135, -14, 160, 5), (-14, 160, -32, 182, 4), (0, 140, 14, 160, 4), (14, 160, 6, 182, 3),
                     (-14, 160, -42, 164, 3), (-32, 182, -50, 188, 2), (14, 160, 30, 168, 2), (-6, 148, -34, 144, 3),
                     (6, 182, -6, 196, 2), (-42, 164, -56, 158, 1), (30, 168, 38, 178, 1)]
        else:
            limbs = [(0, 135, -24, 166, 5), (-24, 166, -40, 182, 3), (0, 135, 22, 164, 5), (22, 164, 38, 180, 3),
                     (0, 140, 0, 188, 4), (-24, 166, -48, 160, 2), (22, 164, 48, 156, 2), (0, 170, -14, 192, 2),
                     (0, 170, 14, 194, 2), (-40, 182, -50, 192, 1), (38, 180, 46, 192, 1)]
        for x0, y0, x1, y1, th in limbs:
            limb(k, bx + x0, y0, bx + x1, y1, th)
        if level == 1:  # a few ragged leaves still hanging on
            for x0, y0, x1, y1, th in limbs[3:]:
                if rnd.random() < 0.8:
                    put(k, bx + x1, y1, c("#7a7a3a")); put(k, bx + x1 + 1, y1, c("#a8a85a")); put(k, bx + x1, y1 - 1, c("#5a5a2a"))
        else:  # snow on every upward-facing edge, and icicles hanging below
            for y in range(H - 2, 0, -1):
                for x in range(W):
                    if solid(k, x, y) and not solid(k, x, y + 1) and rnd.random() < 0.9:
                        put(k, x, y + 1, c("#f4f8ff"))
            for x0, y0, x1, y1, th in limbs[:6]:
                ix, iy = bx + (x0 + x1) // 2, (y0 + y1) // 2
                for i in range(1, rnd.randint(3, 6)):
                    if not solid(k, ix, iy - th // 2 - i):
                        put(k, ix, iy - th // 2 - i, c("#bfe6ff"))
            # snow along the nest branch and on the roots: find the top edges first, then paint,
            # so new snow is never mistaken for more branch
            caps = [(x, y + 1) for x in range(W) for y in list(range(TREE_NEST_Y - 8, TREE_NEST_Y + 4)) + list(range(14, 30))
                    if solid(t, x, y) and not solid(t, x, y + 1) and (x > bx + 8 if y > 60 else abs(x - bx) > 7)]
            for x, y in caps:
                put(t, x, y, c("#f4f8ff"))
    outline(trunk)
    outline(crown)
    return trunk, crown


def prop_sprites():
    note = with_outline(draw_map(["...##", "...#.#", "...#..", ".###..", "####..", ".##..."], {"#": c("#ffffff")}))
    bolt = with_outline(draw_map(["..YYY", "..YY.", ".YY..", ".YYYY", "..YY.", ".YY..", "YY...", "Y...."],
                                 {"Y": c("#ffe866")}))
    cloud = Image.new("RGBA", (40, 20), CLEAR)
    px = cloud.load()
    puffs = [(10, 12, 6), (19, 8, 8), (29, 11, 6), (14, 14, 5), (25, 14, 5)]
    for cx, cy, r in puffs:
        circle(px, cx, cy, r, c("#4a4f63"), 40, 20)
    for cx, cy, r in puffs[:3]:
        circle(px, cx - 2, cy - 2, r // 2, c("#5e6479"), 40, 20)
    box(px, 0, 18, 40, 20, CLEAR, 40, 20)
    outline(cloud)
    nest = Image.new("RGBA", (28, 11), CLEAR)
    px = nest.load()
    for x in range(1, 27):
        depth = int(8 * (1 - ((x - 13.5) / 13.5) ** 2)) + 1
        for y in range(1, 1 + depth):
            px[x, y] = c("#c89a5a") if (x + y * 2) % 5 < 2 else c("#8a5a2e") if (x + y) % 3 else c("#5a3418")
    for x0, y0 in ((4, 0), (11, 0), (19, 0), (24, 1)):  # sticks poking out of the rim
        px[x0, y0] = c("#8a5a2e")
    outline(nest)
    feather_bit = with_outline(draw_map(["..LL", "LLD.", "D..."], {"L": c("#c9935a"), "D": c("#6e4826")}))
    trees = [tree(level) for level in range(3)]
    return {"note": note, "bolt": bolt, "storm_cloud": cloud, "nest": nest, "heart": heart(),
            "feather_bit": feather_bit,
            # one big tree per world (trunk with the nest's branch, and its crown as a layer that can sway)
            "tree_0": trees[0][0], "crown_0": trees[0][1], "tree_1": trees[1][0], "crown_1": trees[1][1],
            "tree_2": trees[2][0], "crown_2": trees[2][1],
            # what falls from each tree: summer leaves, storm-torn leaves, snow
            "leaf_0": with_outline(draw_map(["GG.", ".GL"], {"G": c("#5cb84a"), "L": c("#a6e070")})),
            "leaf_1": with_outline(draw_map(["BB.", ".BD"], {"B": c("#7a6a3a"), "D": c("#4a3a20")})),
            "leaf_2": draw_map([".W.", "WLW", ".W."], {"W": c("#ffffff"), "L": c("#d8eeff")}),
            # the finale's boss
            "dickinson": dickinson("idle"), "dickinson_sleep": dickinson("sleep"), "dickinson_smile": dickinson("smile")}


def main():
    birds = {f: scuff(make_bird(f), 11) for f in ("up", "mid", "down")}
    hurt = scuff(make_bird("down", hurt=True), 11)
    os.makedirs(os.path.join(SPRITES, "bird"), exist_ok=True)
    for f, b in birds.items():
        b.save(os.path.join(SPRITES, "bird", f"bird_{f}.png"))
    hurt.save(os.path.join(SPRITES, "bird", "bird_hurt.png"))
    os.makedirs(os.path.join(GAME, "bird"), exist_ok=True)
    for f, b in list(birds.items()) + [("hurt", hurt)]:
        b.save(os.path.join(GAME, "bird", f"bird_{f}.png"))
    for level in LEVELS:
        build(level, birds, hurt)
        print("built", level["folder"])
    for group, sprites in (("ui", ui_sprites()), ("props", prop_sprites())):
        for folder in (os.path.join(SPRITES, group), os.path.join(GAME, group)):
            os.makedirs(folder, exist_ok=True)
            for i, (name, im) in enumerate(sprites.items()):
                tiles = name in ("panel", "parchment", "bar", "button", "button_hover", "screen_frame")
                scuff(im, 60 + i, shape=not tiles, tint=0.03 if tiles else 0.05).save(os.path.join(folder, name + ".png"))
        print("built", group)
    for folder in (SPRITES, GAME):
        worsen_all(folder)
    print("roughened everything")


if __name__ == "__main__":
    main()
