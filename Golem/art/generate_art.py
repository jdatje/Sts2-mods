"""
Generate the Golem's art with Google's Gemini API and put it where the mod expects it.

Setup (once):
    pip install pillow
    set GEMINI_API_KEY=...        (Windows: setx GEMINI_API_KEY "..." then open a new terminal)

Usage (run from the repo root):
    python art/generate_art.py --list-models          # see which image models your key can use
    python art/generate_art.py --dry-run              # print prompts, call nothing
    python art/generate_art.py --only slam,tremor     # generate a few
    python art/generate_art.py                        # generate everything that doesn't exist yet
    python art/generate_art.py --only slam --force    # redo one
    python art/generate_art.py --process-only         # re-crop/resize from art/raw without calling the API

Raw images are kept in art/raw/ so you can re-process without paying again.
A review page is written to art/review.html.
"""
import argparse
import base64
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

try:
    from PIL import Image, ImageFilter, ImageOps
except ImportError:
    sys.exit("Pillow is missing: run  pip install pillow")

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "art"
RAW = ART / "raw"
IMG = ROOT / "Golem" / "images"
API = "https://generativelanguage.googleapis.com/v1beta"
KEY_COLOR = (255, 0, 255)  # magenta background for icons, removed afterwards

ICON_TEMPLATE = ("A single game icon of {subject}. Centered, filling most of the frame, bold readable silhouette, "
                 "painted style. Isolated on a perfectly flat, solid pure magenta (#FF00FF) background with no "
                 "shadows, no gradient and no other objects.")


def prompt_for(asset, cfg):
    kind = asset["kind"]
    style, golem = cfg["style"], cfg["golem"]
    if kind == "card":
        return f"{style}\n{golem}\nScene: {asset['prompt']}\nWide landscape composition (4:3), subject clearly readable at small size."
    if kind in ("power", "relic", "charui_icon"):
        return f"{style}\n{golem}\n" + ICON_TEMPLATE.format(subject=asset["prompt"])
    if kind == "charui_portrait":
        return f"{style}\n{golem}\nTall portrait composition (2:3): {asset['prompt']}."
    if kind == "mod_image":
        return f"{style}\n{golem}\nSquare composition: {asset['prompt']}."
    raise ValueError(kind)


def aspect_for(kind):
    return {"card": "4:3", "charui_portrait": "2:3"}.get(kind, "1:1")


def call_gemini(model, key, prompt, aspect):
    body = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {"responseModalities": ["IMAGE"], "imageConfig": {"aspectRatio": aspect}},
    }
    req = urllib.request.Request(f"{API}/models/{model}:generateContent", data=json.dumps(body).encode(),
                                 headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                data = json.load(r)
            break
        except urllib.error.HTTPError as e:
            msg = e.read().decode(errors="replace")[:500]
            if e.code in (429, 500, 503) and attempt < 2:
                print(f"    HTTP {e.code}, retrying in 20s...")
                time.sleep(20)
                continue
            raise RuntimeError(f"HTTP {e.code}: {msg}")
    images = [p["inlineData"]["data"] for c in data.get("candidates", []) for p in c.get("content", {}).get("parts", [])
              if "inlineData" in p and not p.get("thought")]
    if not images:
        raise RuntimeError("No image in response: " + json.dumps(data)[:500])
    return base64.b64decode(images[-1])


def remove_magenta(im):
    """Make the magenta background transparent, with soft edges."""
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # distance from magenta: magenta has high R, high B, low G
            d = max(abs(r - 255), abs(b - 255), g)
            if d < 60:
                px[x, y] = (r, g, b, 0)
            elif d < 110:
                alpha = int(255 * (d - 60) / 50)
                # pull the pink fringe towards neutral
                px[x, y] = (min(r, g + 40), g, min(b, g + 40), alpha)
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def fit_square(im, size, pad=0.06):
    """Fit a transparent image into a square canvas with a little padding."""
    inner = int(size * (1 - 2 * pad))
    im = ImageOps.contain(im, (inner, inner), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(im, ((size - im.width) // 2, (size - im.height) // 2), im)
    return canvas


def outline_of(im, width=4):
    """White silhouette slightly larger than the icon (used by relics)."""
    alpha = im.split()[3].point(lambda a: 255 if a > 40 else 0)
    grown = alpha.filter(ImageFilter.MaxFilter(width * 2 + 1))
    out = Image.new("RGBA", im.size, (255, 255, 255, 0))
    out.putalpha(grown)
    return out


def save(im, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)
    print(f"    -> {path.relative_to(ROOT)}")


def process(asset, raw_path):
    kind, name = asset["kind"], asset["id"]
    im = Image.open(raw_path)
    if kind == "card":
        big = ImageOps.fit(im.convert("RGB"), (1000, 760), Image.LANCZOS)
        save(big, IMG / "card_portraits" / "big" / f"{name}.png")
        save(big.resize((250, 190), Image.LANCZOS), IMG / "card_portraits" / f"{name}.png")
    elif kind == "power":
        icon = remove_magenta(im)
        save(fit_square(icon, 256), IMG / "powers" / "big" / f"{name}.png")
        save(fit_square(icon, 64, pad=0.02), IMG / "powers" / f"{name}.png")
    elif kind == "relic":
        icon = remove_magenta(im)
        big, small = fit_square(icon, 256), fit_square(icon, 94, pad=0.08)
        save(big, IMG / "relics" / "big" / f"{name}.png")
        save(small, IMG / "relics" / f"{name}.png")
        save(outline_of(small), IMG / "relics" / f"{name}_outline.png")
    elif kind == "charui_icon":
        save(fit_square(remove_magenta(im), 128), IMG / "charui" / f"{name}_golem.png")
    elif kind == "charui_portrait":
        port = ImageOps.fit(im.convert("RGB"), (132, 195), Image.LANCZOS)
        save(port, IMG / "charui" / "char_select_golem.png")
        save(ImageOps.grayscale(port).point(lambda v: v // 3).convert("RGB"), IMG / "charui" / "char_select_golem_locked.png")
    elif kind == "mod_image":
        save(ImageOps.fit(im.convert("RGB"), (420, 420), Image.LANCZOS), ROOT / "Golem" / "mod_image.png")


def write_review(assets):
    rows = []
    for a in assets:
        raw = RAW / f"{a['id']}.png"
        img = f'<img src="raw/{raw.name}">' if raw.exists() else "<i>not generated</i>"
        rows.append(f"<div class=c>{img}<b>{a['id']}</b><small>{a['kind']}</small></div>")
    html = ("<html><head><style>body{font-family:Arial;background:#222;color:#ddd}.c{display:inline-block;width:260px;"
            "margin:8px;vertical-align:top}.c img{width:250px;background:#555}b,small{display:block}</style></head><body>"
            "<h2>Golem art review</h2>" + "".join(rows) + "</body></html>")
    (ART / "review.html").write_text(html, encoding="utf-8")
    print(f"Review page: {ART / 'review.html'}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", help="comma-separated asset ids")
    ap.add_argument("--force", action="store_true", help="regenerate even if a raw image exists")
    ap.add_argument("--model", help="override the model from assets.json")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--process-only", action="store_true")
    ap.add_argument("--list-models", action="store_true")
    args = ap.parse_args()

    cfg = json.loads((ART / "assets.json").read_text(encoding="utf-8"))
    key = os.environ.get("GEMINI_API_KEY", "")
    model = args.model or cfg["model"]

    if args.list_models:
        if not key: sys.exit("GEMINI_API_KEY is not set.")
        req = urllib.request.Request(f"{API}/models?pageSize=200", headers={"x-goog-api-key": key})
        with urllib.request.urlopen(req) as r:
            for m in json.load(r).get("models", []):
                if "image" in m["name"].lower():
                    print(m["name"].removeprefix("models/"), "-", m.get("displayName", ""))
        return

    assets = cfg["assets"]
    if args.only:
        wanted = set(args.only.split(","))
        assets = [a for a in assets if a["id"] in wanted]
        missing = wanted - {a["id"] for a in assets}
        if missing: sys.exit(f"Unknown ids: {', '.join(sorted(missing))}")

    RAW.mkdir(parents=True, exist_ok=True)
    if not (args.dry_run or args.process_only) and not key:
        sys.exit("GEMINI_API_KEY is not set.")

    for i, a in enumerate(assets, 1):
        raw = RAW / f"{a['id']}.png"
        print(f"[{i}/{len(assets)}] {a['id']} ({a['kind']})")
        if args.dry_run:
            print("   ", prompt_for(a, cfg).replace("\n", "\n    "))
            continue
        if not args.process_only and (args.force or not raw.exists()):
            try:
                raw.write_bytes(call_gemini(model, key, prompt_for(a, cfg), aspect_for(a["kind"])))
            except Exception as e:
                print(f"    FAILED: {e}")
                continue
        if raw.exists():
            process(a, raw)
    if not args.dry_run:
        write_review(cfg["assets"])


if __name__ == "__main__":
    main()
