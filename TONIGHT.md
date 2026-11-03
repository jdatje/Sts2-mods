# Tonight: first test session

Goal: all three characters load, cards work, and one co-op act runs without a desync.
Do steps 1–5 on the Windows PC (Claude Desktop, with this chat linked), step 6 on the Steam Deck.

## 1. Link the PC (5 min)
- Open this conversation in the **Claude desktop app** and link it to the computer.
- Give access to: a folder for the repo (e.g. `Documents\sts2-mods`) and the game folder
  (`...\steamapps\common\Slay the Spire 2`).
- Check the game is on the **main branch** (Steam → Slay the Spire 2 → Properties → Betas: None).
- Subscribe to **BaseLib** on the Workshop.

## 2. Tools (10 min, Claude can do most of it)
- .NET 9 SDK (`winget install Microsoft.DotNet.SDK.9`) — accept the admin prompt.
- MegaDot 4.5.1 from megadot.megacrit.com, unzip to e.g. `C:\megadot\`.
- Git (`winget install Git.Git`), then `git clone https://github.com/jdatje/sts2-mods`.
- Copy `local.props.example` → `local.props`, set `GodotPath`.

## 3. Build against the real game (5 min)
In each mod folder: `dotnet build -c Release` then `dotnet publish -c Release`.
This is the first time the code meets the real `sts2.dll`. If your game is newer than 0.107.1
there may be small compile errors; Claude fixes them on the spot.
Check: `mods\Golem`, `mods\CardSharp`, `mods\Bard` each contain a `.dll`, `.json` and `.pck`.

## 4. Solo smoke test (20 min)
Start → Play Modded → enable BaseLib + the three mods → restart. For each character, one fight:
- [ ] Shows on character select with name, HP, description; starter deck and relic are right.
- [ ] All card text readable (no `{...}` or raw keys), also upgraded versions.
- **Golem**: Clay Arms makes Strike deal 8; a second Arms Part replaces the first; Heart of Clay gives 6 Block once.
- **Card Sharp**: cards show "Rank of Suit" in hand; Table counts up; Showdown at 5 shows a speech bubble
  with the hand and deals damage; Marked Deck bumps the first one; Sleight of Hand / Cold Deck change ranks.
- **Bard**: Battle Hymn starts with 4 Verses (1 + Worn Lute 3); Verses go up per card; Refrain at turn start
  gives Block; playing Jig ends Battle Hymn and its Finale deals damage.
- [ ] Save & quit mid-combat, continue. (Known risk: the Table's cards, card ranks and the relic
  "once per combat" flags may reset after loading.)

## 5. Art (optional, 15 min)
Art is generated on GitHub now: Actions → **Generate art** → Run workflow (needs the `GEMINI_API_KEY`
repository secret). Download the new build from the Build mods run that follows.

## 6. Steam Deck + co-op (30 min)
- Desktop Mode → subscribe to BaseLib → copy the three mod folders to
  `~/.local/share/Steam/steamapps/common/Slay the Spire 2/mods/` (or download the zips from GitHub Actions).
- Same branch, same mod versions as the PC.
- Co-op checklist:
  - [ ] Lobby connects with mods on both sides.
  - [ ] Co-op-only cards show up (Lend a Hand, Shared Pot, Duet).
  - [ ] Bard Refrain / Harmony give Block to **both** players.
  - [ ] Card Sharp Showdown deals the same damage on both screens (ranks must match on both sides).
  - [ ] Play one full act: no "desync" message.

If something breaks: tell Claude what you did; on the linked PC it can read the game log itself.
