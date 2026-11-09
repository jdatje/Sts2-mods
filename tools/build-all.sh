#!/usr/bin/env bash
# Builds every mod into dist/<Mod>-<version>.zip (dll + json + pck), against the stub game assemblies.
# Usage: tools/build-all.sh [game-version]     (default 0.107.1 = stable branch)
# Needs: .NET 9 SDK. For the .pck also set GODOT=/path/to/Godot_v4.5.1-stable_mono (MegaDot works too).
set -euo pipefail
VERSION="${1:-0.107.1}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODS=(Golem CardSharp Bard Falconer)
cd "$ROOT"

tools/fetch-stubs.sh "$VERSION"
STS2="$ROOT/.stubs/sts2"
rm -rf "$STS2/mods" dist && mkdir -p "$STS2/mods" dist

echo "== Tests"
dotnet run --project tests/PokerTests -c Release

for MOD in "${MODS[@]}"; do
  echo "== Building $MOD"
  dotnet build "$MOD/$MOD.csproj" -c Release -p:Sts2Path="$STS2" -warnaserror:CS8602,CS8604
  OUT="$STS2/mods/$MOD"
  if [[ -n "${GODOT:-}" ]]; then
    echo "== Packing $MOD.pck"
    (cd "$MOD" && "$GODOT" --headless --import >/dev/null 2>&1 || true)
    (cd "$MOD" && "$GODOT" --headless --export-pack BasicExport "$OUT/$MOD.pck" >/dev/null 2>&1 || true)
    [[ -f "$OUT/$MOD.pck" ]] || { echo "pck export failed for $MOD"; exit 1; }
  else
    echo "   (GODOT not set: skipping .pck)"
  fi
  rm -f "$OUT"/*.pdb
  MODVER=$(python3 -c "import json,sys;print(json.load(open('$MOD/$MOD.json',encoding='utf-8-sig'))['version'])")
  (cd "$STS2/mods" && zip -qr "$ROOT/dist/$MOD-$MODVER.zip" "$MOD")
  echo "   -> dist/$MOD-$MODVER.zip"
done
