"""Fill workshop/<Mod>/content/ with the built mod files, ready for Mega Crit's ModUploader.

    python tools/workshop/stage.py                 # uses dist/<Mod>-<version>.zip (from tools/build-all.sh or CI)
    python tools/workshop/stage.py --zips DIR      # use zips from another folder
    python tools/workshop/stage.py --note "text"   # set the change note for this upload

Then, on a PC where Steam is running and logged in:
    ModUploader.exe upload -w workshop/Golem      (and CardSharp, Bard)

The first upload creates the Workshop item and writes workshop/<Mod>/mod_id.txt.
COMMIT that file: later uploads read it and update the same item instead of creating a new one.
"""
import argparse, json, shutil, sys, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MODS = ["Golem", "CardSharp", "Bard"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--zips", default=str(ROOT / "dist"))
    ap.add_argument("--note", default=None)
    args = ap.parse_args()
    zdir = Path(args.zips)
    for mod in MODS:
        version = json.loads((ROOT / mod / f"{mod}.json").read_text(encoding="utf-8-sig"))["version"]
        z = zdir / f"{mod}-{version}.zip"
        if not z.exists():
            sys.exit(f"missing {z} - build first (tools/build-all.sh) or pass --zips")
        content = ROOT / "workshop" / mod / "content"
        shutil.rmtree(content, ignore_errors=True)
        content.mkdir(parents=True)
        with zipfile.ZipFile(z) as f:
            for name in f.namelist():
                if name.endswith("/") or name.endswith(".pdb"):
                    continue
                # zip holds <Mod>/<file>; the Workshop content folder holds the files directly
                (content / Path(name).name).write_bytes(f.read(name))
        cfg_path = ROOT / "workshop" / mod / "workshop.json"
        cfg = json.loads(cfg_path.read_text(encoding="utf-8"))
        if args.note:
            cfg["changeNote"] = args.note
        elif not cfg.get("changeNote", "").startswith(version):
            cfg["changeNote"] = f"{version}"
        cfg_path.write_text(json.dumps(cfg, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        files = sorted(p.name for p in content.iterdir())
        print(f"{mod} {version}: {files}  (mod_id.txt: {'yes' if (content.parent / 'mod_id.txt').exists() else 'not yet - first upload'})")


if __name__ == "__main__":
    main()
