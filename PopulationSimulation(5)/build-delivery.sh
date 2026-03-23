#!/bin/bash
# ============================================
# Run this to prepare the delivery ZIP
# Works on macOS, builds for Windows
# ============================================
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo "=== 1. Building for Windows x64 (self-contained) ==="
dotnet publish PopulationSimulation/PopulationSimulation.csproj \
    -c Release -r win-x64 --self-contained true \
    -o delivery/executable

echo ""
echo "=== 2. Copying source files ==="
rm -rf delivery/source
mkdir -p delivery/source/PopulationSimulation
cp PopulationSimulation.sln delivery/source/
rsync -av --exclude='bin/' --exclude='obj/' \
    PopulationSimulation/ delivery/source/PopulationSimulation/

echo ""
echo "=== 3. Creating ZIP ==="
FIRSTNAME=$(python3 -c "import json; print(json.load(open('delivery/competitor.json'))['firstName'])" 2>/dev/null || echo "firstname")
LASTNAME=$(python3 -c "import json; print(json.load(open('delivery/competitor.json'))['lastName'])" 2>/dev/null || echo "lastname")
ZIPNAME="skill09-2026-${FIRSTNAME}-${LASTNAME}.zip"

cd delivery
rm -f "../$ZIPNAME"
zip -r "../$ZIPNAME" source/ executable/ database/ competitor.json readme.md
cd ..

echo ""
echo "=== DONE ==="
echo "Created: $ZIPNAME"
echo ""
echo "REMINDER: Edit delivery/competitor.json with your real name!"
