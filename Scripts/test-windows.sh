#!/bin/bash

# Runs the xUnit test suite via Windows PowerShell.
#
# WSL's mono/dotnet stack can't host the net472 test runner cleanly, but the
# Windows dotnet CLI handles it out of the box. This script shells out to
# powershell.exe, runs `dotnet test` from the repo root (translated to a
# Windows path), and pipes the output straight back to this terminal.
#
# Usage:
#   ./test-windows.sh
#   ./test-windows.sh --filter FullyQualifiedName~SweepHaul
#   ./test-windows.sh -c Release --logger "console;verbosity=detailed"
#
# Any extra arguments are forwarded to `dotnet test` verbatim.

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_ROOT="$(dirname "$SCRIPT_DIR")"

if ! command -v powershell.exe >/dev/null 2>&1; then
    echo -e "${RED}✗ powershell.exe not found on PATH — is this WSL with Windows interop enabled?${NC}"
    exit 1
fi

WIN_MOD_ROOT="$(wslpath -w "$MOD_ROOT")"
MAIN_PROJECT='Source\1.6\UniqueWeaponsUnbound.csproj'
TEST_PROJECT='Tests\1.6\UniqueWeaponsUnbound.Tests.csproj'

echo -e "${BLUE}=== Unique Weapons Unbound - Windows Test Runner ===${NC}"
# printf, not `echo -e`, so the backslashes in the UNC path print verbatim.
printf "${YELLOW}Repo:${NC} %s\n" "$WIN_MOD_ROOT"
printf "${YELLOW}Project:${NC} %s\n\n" "$TEST_PROJECT"

# Forward extra args. Bash splits "$@" per-arg; we re-join with spaces for
# PowerShell to re-parse. Args containing single quotes will need escaping.
EXTRA_ARGS="$*"

# Build phases are split into separate dotnet processes for two reasons:
#
# 1. WSL2's 9P bridge has an async metadata-visibility lag, so files written
#    by one stage aren't reliably visible to the next stage within the same
#    MSBuild graph. Splitting into separate processes flushes file handles
#    between stages. -p:BuildProjectReferences=false on stage 2 prevents
#    MSBuild from rebuilding the up-to-date main DLL and re-triggering it.
#
# 2. Test execution can't run from any \\wsl.localhost\... location (raw UNC
#    or drive-letter-mapped) because .NET Framework's CLR treats it as a
#    remote/untrusted source and refuses to load the xunit test adapter.
#    Stage 3 robocopies the test bin to %TEMP%\uwu-tests\ — a real local
#    NTFS path — and runs `dotnet test` against the copied DLL. Builds still
#    happen on the WSL filesystem; only test execution moves to local disk.
#
# `cmd /c "pushd <UNC> && ..."` is used for the build stages: cmd's pushd
# auto-maps a temporary drive letter for UNC paths (visible to child
# processes, unlike PowerShell's Push-Location) so MSBuild has a sensible
# cwd. The temp drive is released when cmd exits.
# Capture output to a temp file alongside live streaming. We can't use
# `$(... | tee ...)` here because PIPESTATUS would be stale by the time we
# read it from the parent shell — the pipeline runs inside the command-
# substitution subshell. Running the pipeline directly preserves PIPESTATUS.
TMPF=$(mktemp)
trap 'rm -f "$TMPF"' EXIT

# Set PowerShell's cwd to a local NTFS path before invoking cmd. Child
# processes inherit cwd, so this prevents cmd from emitting "UNC paths are
# not supported. Defaulting to Windows directory." at startup. cmd's pushd
# then maps the UNC repo to a temp drive letter for the build itself.
powershell.exe -NoProfile -Command "
    \$ErrorActionPreference = 'Stop'
    \$LocalBin = Join-Path \$env:TEMP 'uwu-tests'
    \$RemoteBin = Join-Path '$WIN_MOD_ROOT' 'Tests\1.6\bin\Debug\net472'
    Set-Location \$env:TEMP

    cmd /c 'pushd \"$WIN_MOD_ROOT\" && dotnet build $MAIN_PROJECT --nologo'
    if (\$LASTEXITCODE -ne 0) { exit \$LASTEXITCODE }

    cmd /c 'pushd \"$WIN_MOD_ROOT\" && dotnet build $TEST_PROJECT --nologo -p:BuildProjectReferences=false'
    if (\$LASTEXITCODE -ne 0) { exit \$LASTEXITCODE }

    Write-Host \"Mirroring test bin to \$LocalBin ...\"
    robocopy \$RemoteBin \$LocalBin /MIR /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
    # robocopy: 0-7 = success/info, >=8 = real failure.
    if (\$LASTEXITCODE -ge 8) { Write-Error \"robocopy failed (\$LASTEXITCODE)\"; exit \$LASTEXITCODE }

    Set-Location -LiteralPath \$LocalBin
    dotnet test 'UniqueWeaponsUnbound.Tests.dll' --nologo $EXTRA_ARGS
    exit \$LASTEXITCODE
" 2>&1 | tee "$TMPF"
STATUS=${PIPESTATUS[0]}
TEST_OUTPUT=$(cat "$TMPF")

# `dotnet test` can exit 0 even when zero tests were discovered (e.g. test
# adapter failed to load). Treat that as failure so a silently broken setup
# can't masquerade as success.
if [ $STATUS -eq 0 ] && echo "$TEST_OUTPUT" | grep -qE "No test is available|test source file .* was not found"; then
    echo -e "${YELLOW}⚠ dotnet test reported success but no tests ran — treating as failure.${NC}" >&2
    STATUS=2
fi

echo ""
if [ $STATUS -eq 0 ]; then
    echo -e "${GREEN}✓ Tests passed${NC}"
else
    echo -e "${RED}✗ Tests failed (exit $STATUS)${NC}"
fi
exit $STATUS
