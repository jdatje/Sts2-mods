# The Golem — a Slay the Spire 2 character mod

A rusty, muddy boulder construct that builds its body from **Parts**.

- **Parts** are Power cards that go into one of four slots: **Head, Arms, Legs, Core**.
  A new Part replaces the one in the same slot, so you're always choosing what to keep.
- Payoff cards scale with how many Parts you have (Assemble, Full Body Slam), and
  **Collapse** cashes all of them in for Block.
- Starts with 80 HP, 4 Strike, 4 Defend, Slam, Clay Arms and the relic **Heart of Clay**
  (the first Part you install each combat also gives 6 Block).

**Version 0.2: full card pool, 86 cards** (4 basic, 20 common, 36 uncommon, 26 rare) plus the Rubble token.
Placeholder art. Built on [BaseLib](https://github.com/Alchyr/BaseLib-StS2) (required on every machine).

## Archetypes

- **Parts:** 20 Parts across the four slots, e.g. Lantern Eye / Crystal Skull / Thinking Stone (Head),
  Clay Arms / Hammer Arms / Spiked Knuckles (Arms), Rooted Legs / Iron Treads / Unmovable Legs (Legs),
  Mud Core / Furnace Core / Geode Core (Core). Powers like Rock Solid, Assembly Line and Perfect Form trigger
  whenever you install a Part.
- **Part payoffs:** cards that scale with your number of Parts (Iron Grip, Reinforce, Colossus Slam, Monolith),
  cash them in (Collapse, Overhaul, Reassemble) or install several at once (Final Form).
- **Fortress:** Block, Plating, Rubble and Barricade (Unmovable Legs), with Crushing Weight to hit back.
- **Boulders:** big, slow hits and AoE (Haymaker, Mountainfall, Earthquake, Stone Rain).

**Full card list:** [docs/cards-golem.md](../docs/cards-golem.md). Balance: [docs/BALANCE.md](../docs/BALANCE.md).

## Building

See the root [README](../README.md): one `local.props` for the whole repo, then `dotnet build` / `dotnet publish` in this folder.

## Art

See the root README: `tools/art/generate_art.py` / the **Generate art** workflow. The Golem's style and
hand-written prompts are in `tools/art/golem.json`.

## Multiplayer

All effects run through the game's own commands (damage, block, powers, energy), so they
go through the action queue and should stay in sync. `affects_gameplay` is true, so both
players need the same version. Test checklist for co-op is in `TESTING.md`.
