# STS2 Mods

Three character mods for Slay the Spire 2, built on [BaseLib](https://github.com/Alchyr/BaseLib-StS2) and made to work in co-op.

| Mod | Character | Mechanic | HP | Starter relic |
|---|---|---|---|---|
| [Golem](Golem/README.md) | The Golem | **Parts**: power cards that go into Head / Arms / Legs / Core slots; a new Part replaces the old one | 80 | Heart of Clay |
| [CardSharp](CardSharp/README.md) | The Card Sharp | **Poker**: every card has a suit and a random rank; played cards go on the Table; at 5 cards a **Showdown** scores the poker hand and hits ALL enemies | 68 | Marked Deck |
| [Bard](Bard/README.md) | The Bard | **Songs**: one Song at a time; every card adds a Verse; the Refrain helps ALL players each turn; the Finale scales with Verses | 70 | Worn Lute |

Status: **v0.1, first playable versions** (~20 cards each, placeholder art). They compile against
the game's reference assemblies (stable 0.107.1); they haven't been tested in the real game yet.
See [TONIGHT.md](TONIGHT.md) for the first test session.

## Installing (players)

1. Subscribe to **BaseLib** on the Steam Workshop.
2. Download the mod zips from the latest [GitHub Actions run](../../actions) (artifact) or [Release](../../releases).
3. Unzip into the game's `mods` folder, so you get `mods/Golem/Golem.dll` etc.
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\`
   - Steam Deck (Desktop Mode): `~/.local/share/Steam/steamapps/common/Slay the Spire 2/mods/`
4. Start the game, choose **Play Modded**, enable the mods in the Mods menu, restart.

For co-op, **both players need the same mods, the same mod versions and the same game branch**
(stable vs beta).

## Building (developers)

Needs the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) and, for the `.pck` (images and text),
[MegaDot 4.5.1](https://megadot.megacrit.com/) or Godot 4.5.1 mono.

1. Copy `local.props.example` to `local.props` and set `GodotPath`. The game is found through Steam;
   set `Sts2Path` only if that fails.
2. In a mod folder (e.g. `Golem/`):
   ```
   dotnet build -c Release     # dll + json -> <game>/mods/Golem/
   dotnet publish -c Release   # also packs Golem.pck with MegaDot
   ```

**Without the game** (Linux/macOS/CI): `tools/build-all.sh` downloads reference-only game assemblies from
NuGet (`BSchneppe.Sts2.ReferenceAssemblies`), runs the tests and builds all three mods into `dist/*.zip`.
Set `GODOT=/path/to/Godot_v4.5.1-stable_mono` to include the `.pck`. GitHub Actions does exactly this on
every push; tags `v*` become a Release.

Placeholder images: `python tools/placeholder_art.py` (never overwrites existing images unless `--force`).
Real art: `Golem/art/generate_art.py` uses the Gemini API (key in `GEMINI_API_KEY`).

## Layout

```
Directory.Build.props   shared MSBuild settings (imports local.props)
local.props.example     machine-specific paths (copy to local.props, gitignored)
Golem/ CardSharp/ Bard/ one Godot + C# project per mod (from Alchyr's ModTemplate-StS2)
tests/PokerTests        unit tests for the Showdown hand evaluator
tools/                  build-all.sh, fetch-stubs.sh, placeholder_art.py
.github/workflows/      CI build
```
