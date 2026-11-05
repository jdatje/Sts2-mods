"""
Writes docs/cards-<mod>.md: every card with rarity, type, cost and its text, rendered from the C# sources
(base values) and the English localization. Run after changing cards:

    python tools/card_list.py
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MODS = {"Golem": "GOLEM", "CardSharp": "CARDSHARP", "Bard": "BARD"}
ORDER = ["Basic", "Common", "Uncommon", "Rare", "Token"]


def snake(name):
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_", name).upper()


def class_blocks(src):
    for m in re.finditer(r"public sealed class (\w+)(\(\))?\s*:\s*([\w<>]+)(\(([^;{]*)\))?\s*(;|\{)", src):
        name, header_args, end = m.group(1), m.group(5) or "", m.end()
        body = ""
        if m.group(6) == "{":
            depth, i = 1, end
            while depth and i < len(src):
                depth += {"{": 1, "}": -1}.get(src[i], 0)
                i += 1
            body = src[end:i - 1]
        yield name, m.group(3), header_args, body


def card_info(base, header_args, body):
    b = re.search(r"base\(\s*(\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)", body)
    if b:
        return int(b.group(1)), b.group(2), b.group(3), {}
    generic = header_args or (re.search(r"base\(([^)]*)\)", body) or [None, ""])[1]
    if "CardRarity" in generic and "<" in base:
        nums = [int(x) for x in re.findall(r"(?<![\w.])-?\d+", generic)]
        rarity = re.search(r"CardRarity\.(\w+)", generic).group(1)
        power = re.search(r"<(\w+)>", base).group(1)
        values = {}
        if base.startswith("SongCard"):
            sv = re.search(r"Song\((\d+),\s*(\d+)\)", body)
            if sv: values["Verses"], values["Refrain"] = int(sv.group(1)), int(sv.group(2))
            return nums[0], "Power", rarity, values
        # PartCard / PowerCard / BardPowerCard: (cost, rarity, amount, ...); SharpPowerCard: (suit, cost, rarity, amount, ...)
        cost, amount = nums[0], nums[1] if len(nums) > 1 else 0
        values[power] = amount
        return cost, "Power", rarity, values
    return None


def var_values(body, values):
    for k, rx in [("Damage", r"WithDamage\((\d+)"), ("Block", r"WithBlock\((\d+)"), ("Cards", r"WithCards\((\d+)"),
                  ("Energy", r"WithEnergy\((\d+)"), ("Heal", r"WithHeal\((\d+)")]:
        m = re.search(rx, body)
        if m: values[k] = int(m.group(1))
    for p, v in re.findall(r"WithPower<(\w+)>\((\d+)", body):
        values[p] = int(v)
    for n, v in re.findall(r'WithVar\("(\w+)",\s*(-?\d+)', body):
        values[n] = int(v)
    m = re.search(r"WithCalculated(Damage|Block)\((\d+),\s*(\d+)", body)
    if m:
        values["CalculationBase"] = int(m.group(2))
        values["CalculationExtra" if m.group(1) == "Block" else "ExtraDamage"] = int(m.group(3))
    return values


def render(text, values):
    text = re.sub(r"\[/?\w+\]", "", text)
    text = text.replace("{RankLine}\n", "").replace("{RankLine}", "")
    text = re.sub(r"\{IfUpgraded:show:[^|]*\|\}", "", text)
    text = re.sub(r"\{InCombat:[^|]*\|\}", "", text)

    def plural(m):
        n = values.get(m.group(1), 2)
        return m.group(2) if n == 1 else m.group(3)
    text = re.sub(r"\{(\w+):plural:([^|}]*)\|([^}]*)\}", plural, text)
    text = re.sub(r"\{(\w+):energyIcons\(\)\}", lambda m: f"{values.get(m.group(1), '?')} Energy", text)
    text = re.sub(r"\{(\w+)(?::diff\(\))?\}", lambda m: str(values.get(m.group(1), "X")), text)
    return text.replace("\n", " ")


def main():
    (ROOT / "docs").mkdir(exist_ok=True)
    for mod, prefix in MODS.items():
        src = "\n".join(p.read_text(encoding="utf-8") for p in (ROOT / mod).glob(f"{mod}Code/Cards/*.cs"))
        loc = json.loads((ROOT / mod / mod / "localization" / "eng" / "cards.json").read_text(encoding="utf-8"))
        rows = []
        for name, base, header_args, body in class_blocks(src):
            info = card_info(base, header_args, body)
            if not info:
                continue
            cost, ctype, rarity, values = info
            values = var_values(body, values)
            key = f"{prefix}-{snake(name)}"
            title = loc.get(key + ".title", name)
            text = render(loc.get(key + ".description", ""), values)
            kws = [k for k in ["Exhaust", "Retain", "Innate", "Ethereal"] if f"CardKeyword.{k}" in body]
            if "MultiplayerOnly" in body: kws.insert(0, "Co-op only")
            suit = re.search(r"Suit => Suit\.(\w+)", body) or re.search(r"Suit\.(\w+)", header_args or "") \
                or re.search(r"base\(\s*Suit\.(\w+)", body)
            if "IsWild => true" in body: suit = re.match(r"(Wild)", "Wild")
            if base.startswith("PartCard") or "GolemKeywords.Part)" in body.split("WithTip")[0]: text = "Part. " + text
            if base.startswith("SongCard"): text = "Song. " + text
            rows.append((ORDER.index(rarity), ctype, cost, title, rarity, suit.group(1) if suit else "", text, kws))
        rows.sort(key=lambda r: (r[0], r[1], r[2], r[3]))
        has_suit = any(r[5] for r in rows)
        lines = [f"# {mod}: all cards", "", f"{sum(1 for r in rows if r[4] != 'Token')} cards in the pool. Generated by `tools/card_list.py` (unupgraded values).", ""]
        for rarity in ORDER:
            group = [r for r in rows if r[4] == rarity]
            if not group: continue
            lines += [f"## {rarity} ({len(group)})", "", "| Card | Type | Cost |" + (" Suit |" if has_suit else "") + " Text |",
                      "|---|---|---|" + ("---|" if has_suit else "") + "---|"]
            for _, ctype, cost, title, _, suit, text, kws in group:
                extra = f" *{', '.join(kws)}.*" if kws else ""
                lines.append(f"| {title} | {ctype} | {cost} |" + (f" {suit} |" if has_suit else "") + f" {text}{extra} |")
            lines.append("")
        (ROOT / "docs" / f"cards-{mod.lower()}.md").write_text("\n".join(lines), encoding="utf-8")
        print(f"{mod}: {len(rows)} cards -> docs/cards-{mod.lower()}.md")


if __name__ == "__main__":
    main()
