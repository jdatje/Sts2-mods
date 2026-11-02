#!/usr/bin/env bash
# Builds a fake "Slay the Spire 2" folder with the reference-only game assemblies from NuGet,
# so the mods compile without the game installed (CI, or a machine without the game).
#   tools/fetch-stubs.sh [version]      default: 0.107.1 (stable branch)
# Then build with:  dotnet build -c Release -p:Sts2Path="$PWD/.stubs/sts2"
set -euo pipefail
VERSION="${1:-0.107.1}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/.stubs/sts2"
PKG="bschneppe.sts2.referenceassemblies"
mkdir -p "$OUT/data_sts2_linuxbsd_x86_64" "$OUT/mods" "$ROOT/.stubs/pkg"
curl -fsSL -o "$ROOT/.stubs/pkg/stubs.nupkg" "https://api.nuget.org/v3-flatcontainer/$PKG/$VERSION/$PKG.$VERSION.nupkg"
unzip -qo "$ROOT/.stubs/pkg/stubs.nupkg" -d "$ROOT/.stubs/pkg/stubs"
cp "$ROOT"/.stubs/pkg/stubs/ref/net9.0/*.dll "$OUT/data_sts2_linuxbsd_x86_64/"
echo "Stub game folder ready: $OUT (version $VERSION)"
