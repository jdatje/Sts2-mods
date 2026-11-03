"""
Generate art for all mods with Google's Gemini API and put it where the mods expect it.

Assets are discovered from the code and localization (every card, power, relic, token and the character
images), so new cards get art automatically. Prompts come from tools/art/<mod>.json: a shared style, a
character description, and optional hand-written per-asset overrides; otherwise the prompt is built from
the card's name, type and text.

    pip install pillow
    export GEMINI_API_KEY=...                       # never commit it; in CI it comes from a repo secret

    python tools/art/generate_art.py --list-models
    python tools/art/generate_art.py --dry-run --mod Golem --only slam
    python tools/art/generate_art.py                 # everything missing, all mods
    python tools/art/generate_art.py --mod Bard --only lullaby,dirge --force
    python tools/art/generate_art.py --process-only  # re-crop/resize from art/raw without calling the API

Raw images go to art/raw/<mod>/ (not committed; CI uploads them as an artifact) and a review page to
art/review.html. Card art is saved as JPG (portraits), icons as transparent PNG.
"""
import argparse
import base64
import concurrent.futures as cf
import io
import json
import os
import re
import sys
import threading
import time
import urllib.error
import urllib.request
from pathlib import Path

try:
    from PIL import Image, ImageFilter, ImageOps
except ImportError:
    sys.exit("Pillow is missing: run  pip install pillow")

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import card_list  # noqa: E402  (reuses the card discovery + text rendering)

CFG_DIR = ROOT / "tools" / "art"
RAW_ROOT = ROOT / "art" / "raw"
API = "https://generativelanguage.googleapis.com/v1beta"
MODS = {"Golem": "GOLEM", "CardSharp": "CARDSHARP", "Bard": "BARD"}
SHARED = json.loads((CFG_DIR / "shared.json").read_text(encoding="utf-8"))

ICON = ("A single game icon of {subject}. Centered, filling most of the frame, bold readable silhouette, painted style. "
        "Isolated on a perfectly flat, solid pure magenta (#FF00FF) background with no shadows, no gradient and no other objects.")


# ----------------------------------------------------------------------------- discovery

def discover(mod):
    prefix = MODS[mod]
    cfg = json.loads((CFG_DIR / f"{mod.lower()}.json").read_text(encoding="utf-8"))
    loc_dir = ROOT / mod / mod / "localization" / "eng"
    cards_loc = json.loads((loc_dir / "cards.json").read_text(encoding="utf-8"))
    powers_loc = json.loads((loc_dir / "powers.json").read_text(encoding="utf-8"))
    relics_loc = json.loads((loc_dir / "relics.json").read_text(encoding="utf-8"))
    src = "\n".join(p.read_text(encoding="utf-8") for p in (ROOT / mod).glob(f"{mod}Code/Cards/*.cs"))
    assets = []
    for name, base, header_args, body in card_list.class_blocks(src):
        info = card_list.card_info(base, header_args, body)
        if not info:
            continue
        cost, ctype, rarity, values = info
        values = card_list.var_values(body, values)
        key = f"{prefix}-{card_list.snake(name)}"
        aid = card_list.snake(name).lower()
        title = cards_loc.get(key + ".title", name)
        text = card_list.render(cards_loc.get(key + ".description", ""), values)
        assets.append(dict(id=aid, kind="card", title=title, ctype=ctype, rarity=rarity, text=text))
    for key, title in powers_loc.items():
        if key.endswith(".title") and "SHOWDOWN." not in key:
            pid = key[len(prefix) + 1:-len(".title")].lower()
            desc = powers_loc.get(key.replace(".title", ".description"), "")
            assets.append(dict(id=pid, kind="power", title=title, text=re.sub(r"\[/?\w+\]", "", desc)))
    for key, title in relics_loc.items():
        if key.endswith(".title"):
            rid = key[len(prefix) + 1:-len(".title")].lower()
            desc = relics_loc.get(key.replace(".title", ".description"), "")
            assets.append(dict(id=rid, kind="relic", title=title, text=re.sub(r"\[/?\w+\]|\{\w+\}", "", desc)))
    short = mod.lower()
    assets += [dict(id="character_icon", kind="charui_icon", title=cfg["name"]),
               dict(id="map_marker", kind="charui_icon", title=cfg["name"]),
               dict(id="char_select", kind="charui_portrait", title=cfg["name"]),
               dict(id="mod_image", kind="mod_image", title=cfg["name"])]
    for a in assets:
        a["mod"], a["short"] = mod, short
    return cfg, assets


def prompt_for(a, cfg):
    style = SHARED["style"] + " " + cfg.get("palette", "")
    who = cfg["character"]
    override = cfg.get("overrides", {}).get(a["id"])
    kind = a["kind"]
    if kind == "card":
        scene = override or (
            f"Illustration for a card called \"{a['title']}\" ({a['ctype'].lower()}): {a['text']} "
            f"Show this as one clear, dramatic moment" + (" featuring the character" if a["ctype"] != "Power" or cfg.get("powers_show_character") else "")
            + ". Do not draw any text, cards with writing, or numbers.")
        return f"{style}\n{who}\nScene: {scene}\nWide landscape composition (4:3), subject clearly readable at small size."
    if kind in ("power", "relic"):
        subject = override or f"an object or symbol that represents \"{a['title']}\" ({a['text']}), in the theme of {cfg['name']}"
        return f"{style}\n" + ICON.format(subject=subject)
    if kind == "charui_icon":
        return f"{style}\n{who}\n" + ICON.format(subject=override or cfg["icon_subject"])
    if kind == "charui_portrait":
        return f"{style}\n{who}\nTall portrait composition (2:3): {override or cfg['portrait']}."
    if kind == "mod_image":
        return f"{style}\n{who}\nSquare composition: {override or cfg['banner']}."
    raise ValueError(kind)


def aspect_for(kind):
    return {"card": "4:3", "charui_portrait": "2:3"}.get(kind, "1:1")


# ----------------------------------------------------------------------------- API

def style_refs(cfg):
    """Own character's reference first (keeps the character consistent), then the other characters' references."""
    refdir = ROOT / "tools" / "art"
    own = cfg.get("style_ref")
    paths = ([refdir / own] if own else []) + sorted(p for p in (refdir / "reference" / "style").glob("*.jpg")
                                                       if not own or p.name != Path(own).name)
    out = []
    for p in paths[:3]:
        im = Image.open(p).convert("RGB")
        im.thumbnail((768, 768))
        buf = io.BytesIO(); im.save(buf, "JPEG", quality=85)
        out.append(base64.b64encode(buf.getvalue()).decode())
    return out


def call_gemini(model, key, prompt, aspect, refs=()):
    parts = [{"inlineData": {"mimeType": "image/jpeg", "data": r}} for r in refs]
    if refs:
        prompt = SHARED.get("style_refs_note", "") + "\n\n" + prompt
    body = {"contents": [{"parts": parts + [{"text": prompt}]}],
            "generationConfig": {"responseModalities": ["IMAGE"], "imageConfig": {"aspectRatio": aspect}}}
    req = urllib.request.Request(f"{API}/models/{model}:generateContent", data=json.dumps(body).encode(),
                                 headers={"x-goog-api-key": key, "Content-Type": "application/json"})
    for attempt in range(5):
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                data = json.load(r)
            break
        except urllib.error.HTTPError as e:
            body_txt = e.read().decode(errors="replace")
            msg = " ".join(body_txt.split())[:500]
            if e.code == 429 and "limit: 0" in body_txt:
                raise RuntimeError("No image quota on this key (free tier allows 0 images). Turn on billing for the key's "
                                   "project in Google AI Studio. Details: " + msg)
            if e.code in (429, 500, 502, 503, 504) and attempt < 4:
                time.sleep(15 * (attempt + 1))
                continue
            raise RuntimeError(f"HTTP {e.code}: {msg}")
        except (urllib.error.URLError, TimeoutError) as e:
            if attempt < 4:
                time.sleep(10)
                continue
            raise RuntimeError(str(e))
    images = [p["inlineData"]["data"] for c in data.get("candidates", []) for p in c.get("content", {}).get("parts", [])
              if "inlineData" in p and not p.get("thought")]
    if not images:
        raise RuntimeError("No image in response: " + json.dumps(data)[:300])
    return base64.b64decode(images[-1])


# ----------------------------------------------------------------------------- processing

def remove_magenta(im):
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, _ = px[x, y]
            d = max(abs(r - 255), abs(b - 255), g)
            if d < 60:
                px[x, y] = (r, g, b, 0)
            elif d < 110:
                px[x, y] = (min(r, g + 40), g, min(b, g + 40), int(255 * (d - 60) / 50))
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def fit_square(im, size, pad=0.06):
    inner = int(size * (1 - 2 * pad))
    im = ImageOps.contain(im, (inner, inner), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(im, ((size - im.width) // 2, (size - im.height) // 2), im)
    return canvas


def outline_of(im, width=4):
    alpha = im.split()[3].point(lambda a: 255 if a > 40 else 0).filter(ImageFilter.MaxFilter(width * 2 + 1))
    out = Image.new("RGBA", im.size, (255, 255, 255, 0))
    out.putalpha(alpha)
    return out


def save(im, path, jpg=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    if jpg:
        im.convert("RGB").save(path, "JPEG", quality=82, optimize=True, progressive=True)
        png = path.with_suffix(".png")
        if png.exists():
            png.unlink()  # replace the placeholder
    else:
        im.save(path, optimize=True)


def process(a, raw_path):
    img = ROOT / a["mod"] / a["mod"] / "images"
    kind, aid = a["kind"], a["id"]
    im = Image.open(raw_path)
    if kind == "card":
        big = ImageOps.fit(im.convert("RGB"), (1000, 760), Image.LANCZOS)
        save(big, img / "card_portraits" / "big" / f"{aid}.jpg", jpg=True)
        save(big.resize((250, 190), Image.LANCZOS), img / "card_portraits" / f"{aid}.jpg", jpg=True)
    elif kind == "power":
        icon = remove_magenta(im)
        save(fit_square(icon, 256), img / "powers" / "big" / f"{aid}.png")
        save(fit_square(icon, 64, pad=0.02), img / "powers" / f"{aid}.png")
    elif kind == "relic":
        icon = remove_magenta(im)
        small = fit_square(icon, 94, pad=0.08)
        save(fit_square(icon, 256), img / "relics" / "big" / f"{aid}.png")
        save(small, img / "relics" / f"{aid}.png")
        save(outline_of(small), img / "relics" / f"{aid}_outline.png")
    elif kind == "charui_icon":
        save(fit_square(remove_magenta(im), 128), img / "charui" / f"{aid}_{a['short']}.png")
    elif kind == "charui_portrait":
        port = ImageOps.fit(im.convert("RGB"), (132, 195), Image.LANCZOS)
        save(port, img / "charui" / f"char_select_{a['short']}.png")
        save(ImageOps.grayscale(port).point(lambda v: v // 3).convert("RGB"), img / "charui" / f"char_select_{a['short']}_locked.png")
    elif kind == "mod_image":
        save(ImageOps.fit(im.convert("RGB"), (420, 420), Image.LANCZOS), ROOT / a["mod"] / a["mod"] / "mod_image.png")


def write_review(all_assets):
    rows = []
    for a in all_assets:
        raw = RAW_ROOT / a["mod"] / f"{a['id']}.png"
        img = f'<img src="raw/{a["mod"]}/{raw.name}">' if raw.exists() else "<i>not generated</i>"
        rows.append(f"<div class=c>{img}<b>{a['mod']}: {a.get('title', a['id'])}</b><small>{a['kind']} · {a['id']}</small></div>")
    html = ("<html><head><meta charset=utf-8><style>body{font-family:Arial;background:#222;color:#ddd}.c{display:inline-block;"
            "width:260px;margin:8px;vertical-align:top}.c img{width:250px;background:#555}b,small{display:block}</style></head>"
            "<body><h2>Art review</h2>" + "".join(rows) + "</body></html>")
    (ROOT / "art").mkdir(exist_ok=True)
    (ROOT / "art" / "review.html").write_text(html, encoding="utf-8")


# ----------------------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--mod", choices=list(MODS) + ["all"], default="all")
    ap.add_argument("--only", help="comma-separated asset ids")
    ap.add_argument("--kinds", help="comma-separated kinds (card,power,relic,charui_icon,charui_portrait,mod_image)")
    ap.add_argument("--force", action="store_true", help="regenerate even if a raw image exists")
    ap.add_argument("--model", default=SHARED["model"])
    ap.add_argument("--workers", type=int, default=4)
    ap.add_argument("--limit", type=int, default=0, help="generate at most N images this run (0 = no limit)")
    ap.add_argument("--no-refs", action="store_true", help="do not send the style reference images")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--process-only", action="store_true")
    ap.add_argument("--list-models", action="store_true")
    args = ap.parse_args()
    key = os.environ.get("GEMINI_API_KEY", "")

    if args.list_models:
        if not key: sys.exit("GEMINI_API_KEY is not set.")
        req = urllib.request.Request(f"{API}/models?pageSize=200", headers={"x-goog-api-key": key})
        with urllib.request.urlopen(req) as r:
            for m in json.load(r).get("models", []):
                if "image" in m["name"].lower():
                    print(m["name"].removeprefix("models/"), "-", m.get("displayName", ""))
        return

    mods = list(MODS) if args.mod == "all" else [args.mod]
    all_assets, todo = [], []
    for mod in mods:
        cfg, assets = discover(mod)
        all_assets += assets
        for a in assets:
            if args.only and a["id"] not in args.only.split(","): continue
            if args.kinds and a["kind"] not in args.kinds.split(","): continue
            todo.append((a, cfg))
    print(f"{len(todo)} assets selected ({', '.join(mods)})")

    if args.dry_run:
        for a, cfg in todo[:args.limit or len(todo)]:
            print(f"--- {a['mod']}/{a['id']} ({a['kind']}, {aspect_for(a['kind'])})\n{prompt_for(a, cfg)}\n")
        return
    if not args.process_only and not key:
        sys.exit("GEMINI_API_KEY is not set.")

    to_generate = [(a, cfg) for a, cfg in todo
                   if not args.process_only and a["id"] not in cfg.get("keep", [])
                   and (args.force or not (RAW_ROOT / a["mod"] / f"{a['id']}.png").exists())]
    if args.limit: to_generate = to_generate[:args.limit]
    print(f"generating {len(to_generate)} images with {args.model} ({args.workers} at a time)")
    lock, done, failed = threading.Lock(), [0], []

    def work(item):
        a, cfg = item
        raw = RAW_ROOT / a["mod"] / f"{a['id']}.png"
        raw.parent.mkdir(parents=True, exist_ok=True)
        try:
            data = call_gemini(args.model, key, prompt_for(a, cfg), aspect_for(a["kind"]),
                               [] if args.no_refs else style_refs(cfg))
            Image.open(__import__("io").BytesIO(data)).save(raw)
            process(a, raw)
            status = "ok"
        except Exception as e:  # keep going; report at the end
            status = f"FAILED: {e}"
            with lock: failed.append(f"{a['mod']}/{a['id']}: {e}")
        with lock:
            done[0] += 1
            print(f"[{done[0]}/{len(to_generate)}] {a['mod']}/{a['id']} {status}", flush=True)

    with cf.ThreadPoolExecutor(max_workers=max(1, args.workers)) as pool:
        list(pool.map(work, to_generate))

    if args.process_only:
        for a, _ in todo:
            raw = RAW_ROOT / a["mod"] / f"{a['id']}.png"
            if raw.exists(): process(a, raw)
    write_review(all_assets)
    print(f"done; {len(failed)} failed")
    for f in failed: print("  " + f)
    if failed and len(failed) == len(to_generate): sys.exit(1)


if __name__ == "__main__":
    main()
