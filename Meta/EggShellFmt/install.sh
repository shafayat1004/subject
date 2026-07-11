#!/usr/bin/env bash
# One-time (per machine, or after a version bump) install of the EggShellFmt
# dotnet tool into the repo-local tool manifest. Analogous to `dotnet tool restore`.
#
#   Meta/EggShellFmt/install.sh
#
# Afterwards, run it from anywhere in the repo:
#   dotnet tool run eggshell-fmt -- [--check] [--quiet] <file.fs | dir> ...
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
repo_root="$(cd "$here/../.." && pwd)"

# Pack into the git-ignored local feed (Meta/EggShellFmt/nupkg, wired in nuget.config).
dotnet pack "$here/EggShellFmt.fsproj" -c Release

# Install or update the tool in the local manifest.
cd "$repo_root"
dotnet tool update --local EggShellFmt --version 1.4.1

echo
echo "Installed. Try:  dotnet tool run eggshell-fmt -- --check LibClient/src"
