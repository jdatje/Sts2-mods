# The Golem — a Slay the Spire 2 character mod

A rusty, muddy boulder construct that builds its body from **Parts**.

- **Parts** are Power cards that go into one of four slots: **Head, Arms, Legs, Core**.
  A new Part replaces the one in the same slot, so you're always choosing what to keep.
- Payoff cards scale with how many Parts you have (Assemble, Full Body Slam), and
  **Collapse** cashes all of them in for Block.
- Starts with 80 HP, 4 Strike, 4 Defend, Slam, Clay Arms and the relic **Heart of Clay**
  (the first Part you install each combat also gives 6 Block).

Version 0.1 = first playable slice: 21 cards, 6 Parts, 1 relic, placeholder art.
Built on [BaseLib](https://github.com/Alchyr/BaseLib-StS2) (required on every machine).

## Card list (v0.1)

| Card | Rarity | Cost | Text (unupgraded) |
|---|---|---|---|
| Strike | Basic | 1 | Deal 6 damage. |
| Defend | Basic | 1 | Gain 5 Block. |
| Slam | Basic | 2 | Deal 10 damage. Gain 5 Block. |
| Clay Arms | Basic | 1 | Part. Arms: your Attacks deal 2 additional damage. |
| Rockslide | Common | 1 | Deal 7 damage to ALL enemies. |
| Boulder Toss | Common | 2 | Deal 18 damage. |
| Mudslinger | Common | 1 | Deal 6 damage. Apply 1 Weak. |
| Pebble Barrage | Common | 1 | Deal 4 damage to a random enemy 3 times. |
| Stone Skin | Common | 1 | Gain 4 Plating. |
| Packed Earth | Common | 2 | Gain 15 Block. |
| Assemble | Common | 1 | Gain 6 Block, +3 per Part. |
| Full Body Slam | Common | 2 | Deal 10 damage, +5 per Part. |
| Boulder Fists | Uncommon | 1 | Part. Arms: whenever you play an Attack, gain 4 Block. |
| Lantern Eye | Uncommon | 1 (0) | Part. Head: draw 1 additional card each turn. |
| Rooted Legs | Uncommon | 1 | Part. Legs: at the end of your turn, gain 4 Block. |
| Mud Core | Uncommon | 1 | Part. Core: Block from cards +2. |
| Collapse | Uncommon | 0 | Remove all Parts. Gain 7 Block per Part removed. Exhaust. |
| Tremor | Uncommon | 1 | Apply 1 Weak and 1 Vulnerable to ALL enemies. |
| Lend a Hand | Uncommon | 1 | Co-op only. Give another player 10 Block. |
| Furnace Core | Rare | 3 (2) | Part. Core: at the start of your turn, gain 1 Energy. |
| Avalanche | Rare | 3 | Deal 8 damage to ALL enemies 3 times. |

## Building

See the root [README](../README.md): one `local.props` for the whole repo, then `dotnet build` / `dotnet publish` in this folder.

## Art

`Golem/art/generate_art.py` (run from the `Golem` folder: `python art/generate_art.py`) makes every card portrait, power icon, relic icon and the character
images with the Gemini API, from the prompts in `art/assets.json`. See the top of that file.
Your API key is read from the `GEMINI_API_KEY` environment variable; never put it in the repo.

## Multiplayer

All effects run through the game's own commands (damage, block, powers, energy), so they
go through the action queue and should stay in sync. `affects_gameplay` is true, so both
players need the same version. Test checklist for co-op is in `TESTING.md`.
