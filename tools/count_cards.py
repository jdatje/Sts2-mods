"""Counts each mod's cards by rarity and type, by reading the C# sources."""
import re, sys, collections
from pathlib import Path
ROOT = Path(__file__).resolve().parent.parent
for mod in sys.argv[1:] or ["Golem", "CardSharp", "Bard"]:
    src = "\n".join(p.read_text(encoding="utf-8") for p in (ROOT / mod).glob(f"{mod}Code/**/*.cs"))
    found = collections.Counter()
    # explicit constructors: base(cost, CardType.X, CardRarity.Y, ...)
    for m in re.finditer(r"class (\w+)\b[^{]*\{[^}]*?base\(\s*\d+\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(3), m.group(2))] += 1
    # primary-ctor shorthands: PartCard<..>(cost, CardRarity.Y, ...) / PowerCard / SongCard base(cost, CardRarity.Y)
    for m in re.finditer(r"class \w+\(\)\s*:\s*(?:PartCard|PowerCard)<\w+>\(\s*\d+\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(1), "Power")] += 1
    for m in re.finditer(r"class \w+ : (?:PartCard|PowerCard)<\w+>\s*\{[^}]*?base\(\s*\d+\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(1), "Power")] += 1
    for m in re.finditer(r"class \w+\(\)\s*:\s*SharpPowerCard<\w+>\(\s*Suit\.\w+\s*,\s*\d+\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(1), "Power")] += 1
    for m in re.finditer(r"class \w+ : SharpPowerCard<\w+>\s*\{[^}]*?base\(\s*Suit\.\w+\s*,\s*\d+\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(1), "Power")] += 1
    for m in re.finditer(r"class \w+ : SongCard<\w+>\s*\{[^}]*?base\(\s*\d+\s*,\s*CardRarity\.(\w+)", src):
        found[(m.group(1), "Power")] += 1
    total = collections.Counter()
    for (r, t), n in found.items(): total[r] += n
    print(f"{mod}: " + ", ".join(f"{r} {total[r]}" for r in ["Basic","Common","Uncommon","Rare","Token"] if total[r]),
          f"| pool total {sum(v for k,v in total.items() if k!='Token')}",
          "| powers by rarity:", {r: found[(r,'Power')] for r in ['Common','Uncommon','Rare'] if found[(r,'Power')]})
