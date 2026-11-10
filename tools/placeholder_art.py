"""
Simple placeholder art for every mod, so cards are recognisable in game before real art exists.
Reads card / power / relic ids from each mod's localization files and draws a coloured
image with the name on it. Never overwrites a file unless --force is given, so real art is safe.

    python tools/placeholder_art.py            # all mods, only missing files
    python tools/placeholder_art.py --force    # redraw everything (overwrites real art!)
"""
import argparse
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent

MODS = {
    # mod: (loc prefix, character id, base colour, accent colour)
    "Golem": ("GOLEM", "THE_GOLEM", (110, 82, 60), (215, 140, 60)),
    "CardSharp": ("CARDSHARP", "THE_CARD_SHARP", (24, 90, 60), (201, 162, 39)),
    "Bard": ("BARD", "THE_BARD", (78, 50, 110), (230, 190, 120)),
    "Falconer": ("FALCONER", "THE_FALCONER", (110, 75, 40), (225, 170, 90)),
}
SUIT_COLOURS = {"Spades": (40, 40, 48), "Hearts": (150, 30, 40), "Diamonds": (190, 80, 30), "Clubs": (30, 70, 40)}
SUIT_SYMBOL = {"Spades": "♠", "Hearts": "♥", "Diamonds": "♦", "Clubs": "♣"}


def font(size, bold=True):
    for name in (["DejaVuSerif-Bold.ttf", "DejaVuSans-Bold.ttf"] if bold else ["DejaVuSans.ttf"]):
        for base in ("/usr/share/fonts/truetype/dejavu/", "C:/Windows/Fonts/", ""):
            try:
                return ImageFont.truetype(base + name, size)
            except OSError:
                continue
    for win in ("georgiab.ttf", "arialbd.ttf"):
        try:
            return ImageFont.truetype("C:/Windows/Fonts/" + win, size)
        except OSError:
            pass
    return ImageFont.load_default()


def snake(key, prefix):
    return key[len(prefix) + 1:].split(".")[0].lower()


def gradient(size, top, bottom):
    w, h = size
    im = Image.new("RGB", size)
    d = ImageDraw.Draw(im)
    for y in range(h):
        t = y / max(h - 1, 1)
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] * (1 - t) + bottom[i] * t) for i in range(3)))
    return im


def darker(c, f=0.45):
    return tuple(int(v * f) for v in c)


def centred_text(d, box, text, fnt, fill):
    x0, y0, x1, y1 = box
    words, lines, line = text.split(), [], ""
    for w in words:
        trial = (line + " " + w).strip()
        if d.textlength(trial, font=fnt) <= (x1 - x0) * 0.9 or not line:
            line = trial
        else:
            lines.append(line)
            line = w
    lines.append(line)
    lh = fnt.size * 1.15
    y = y0 + ((y1 - y0) - lh * len(lines)) / 2
    for ln in lines:
        d.text(((x0 + x1) / 2, y), ln, font=fnt, fill=fill, anchor="ma", stroke_width=max(2, fnt.size // 18),
               stroke_fill=(0, 0, 0))
        y += lh


def card_suits(mod):
    """Card Sharp only: map class name -> suit by reading the C# source."""
    suits = {}
    for cs in (ROOT / mod).glob(f"{mod}Code/Cards/*.cs"):
        src = cs.read_text(encoding="utf-8")
        for m in re.finditer(r"class (\w+) : \w+\s*\{\s*public override Suit Suit => Suit\.(\w+);", src):
            suits[m.group(1)] = m.group(2)
    return suits


def class_to_id(cls):
    return re.sub(r"(?<!^)(?=[A-Z])", "_", cls).lower()


def save(im, path, force):
    """Card portraits are JPG (a PNG placeholder of the same name counts as existing art)."""
    jpg = path.suffix == ".jpg"
    exists = path.exists() or (jpg and path.with_suffix(".png").exists())
    if exists and not force:
        return 0
    path.parent.mkdir(parents=True, exist_ok=True)
    if jpg:
        im.convert("RGB").save(path, "JPEG", quality=85, optimize=True)
        if path.with_suffix(".png").exists():
            path.with_suffix(".png").unlink()
    else:
        im.save(path)
    return 1


def build(mod, force):
    prefix, char_id, base, accent = MODS[mod]
    loc = ROOT / mod / mod / "localization" / "eng"
    img = ROOT / mod / mod / "images"
    n = 0
    suits = {class_to_id(k): v for k, v in card_suits(mod).items()}

    cards = json.loads((loc / "cards.json").read_text(encoding="utf-8"))
    for key, title in cards.items():
        if not key.endswith(".title"):
            continue
        cid = snake(key, prefix)
        top = SUIT_COLOURS.get(suits.get(cid, ""), base)
        im = gradient((1000, 760), top, darker(top))
        d = ImageDraw.Draw(im)
        d.rectangle([24, 24, 975, 735], outline=accent, width=10)
        if cid in suits:
            d.text((500, 330), SUIT_SYMBOL[suits[cid]], font=font(300), fill=accent, anchor="mm")
        centred_text(d, (40, 470, 960, 720), title, font(96), (245, 235, 215))
        n += save(im, img / "card_portraits" / "big" / f"{cid}.jpg", force)
        n += save(im.resize((250, 190), Image.LANCZOS), img / "card_portraits" / f"{cid}.jpg", force)

    def icon(text, size, colour):
        im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        pad = size // 16
        d.ellipse([pad, pad, size - pad, size - pad], fill=colour + (255,), outline=accent + (255,), width=max(2, size // 20))
        d.text((size / 2, size / 2), text, font=font(int(size * (0.5 if len(text) == 1 else 0.34))), fill=(250, 240, 220), anchor="mm",
               stroke_width=max(1, size // 40), stroke_fill=(0, 0, 0))
        return im

    powers = json.loads((loc / "powers.json").read_text(encoding="utf-8"))
    for key, title in powers.items():
        if not key.endswith(".title"):
            continue
        pid = snake(key, prefix)
        letters = "".join(w[0] for w in title.split()[:2]).upper()
        n += save(icon(letters, 256, base), img / "powers" / "big" / f"{pid}.png", force)
        n += save(icon(letters, 64, base), img / "powers" / f"{pid}.png", force)

    relics = json.loads((loc / "relics.json").read_text(encoding="utf-8"))
    for key, title in relics.items():
        if not key.endswith(".title"):
            continue
        rid = snake(key, prefix)
        letters = "".join(w[0] for w in title.split()[:2]).upper()
        big, small = icon(letters, 256, darker(accent, 0.6)), icon(letters, 94, darker(accent, 0.6))
        n += save(big, img / "relics" / "big" / f"{rid}.png", force)
        n += save(small, img / "relics" / f"{rid}.png", force)
        outline = Image.new("RGBA", (94, 94), (0, 0, 0, 0))
        ImageDraw.Draw(outline).ellipse([2, 2, 92, 92], fill=(255, 255, 255, 255))
        n += save(outline, img / "relics" / f"{rid}_outline.png", force)

    # character UI
    chars = json.loads((loc / "characters.json").read_text(encoding="utf-8"))
    name = chars.get(f"{prefix}-{char_id}.title", mod)
    short = mod.lower()
    initial = "".join(w[0] for w in name.replace("The ", "").split())[:2].upper()
    n += save(icon(initial, 128, base), img / "charui" / f"character_icon_{short}.png", force)
    n += save(icon(initial, 128, base), img / "charui" / f"map_marker_{short}.png", force)
    port = gradient((132, 195), base, darker(base))
    dp = ImageDraw.Draw(port)
    dp.rectangle([3, 3, 128, 191], outline=accent, width=4)
    centred_text(dp, (6, 60, 126, 135), name.replace("The ", ""), font(22), (245, 235, 215))
    n += save(port, img / "charui" / f"char_select_{short}.png", force)
    n += save(port.convert("L").point(lambda v: v // 3).convert("RGB"), img / "charui" / f"char_select_{short}_locked.png", force)
    mod_img = gradient((420, 420), base, darker(base))
    dm = ImageDraw.Draw(mod_img)
    dm.rectangle([10, 10, 409, 409], outline=accent, width=8)
    centred_text(dm, (20, 120, 400, 300), name, font(54), (245, 235, 215))
    mi = ROOT / mod / mod / "mod_image.png"
    is_template = mi.exists() and Image.open(mi).mode == "P"  # the template's placeholder
    n += save(mod_img, mi, force or is_template)
    print(f"{mod}: wrote {n} images")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args()
    for m in MODS:
        build(m, a.force)
