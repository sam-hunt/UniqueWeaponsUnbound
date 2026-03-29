#!/bin/bash

# Cleans the deployed mod folder and rebuilds, ensuring stale Defs/Patches are removed.
#
# Usage:
#   ./clean-build.sh

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}=== Unique Weapons Unbound - Clean Build ===${NC}"
echo ""

# Step 1: Clean deployed mod folder
echo -e "${YELLOW}[1/2] Cleaning mod folder...${NC}"
cd "$MOD_ROOT"
dotnet build Source/1.6/UniqueWeaponsUnbound.csproj -t:CleanModFolder --verbosity quiet

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Clean failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Mod folder cleaned${NC}"
echo ""

# Step 2: Build and deploy
echo -e "${YELLOW}[2/2] Building and deploying...${NC}"
dotnet build Source/1.6/UniqueWeaponsUnbound.csproj -c Release --verbosity quiet

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Build failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Build succeeded${NC}"
echo ""

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ Clean build complete${NC}"
echo -e "${GREEN}========================================${NC}"
