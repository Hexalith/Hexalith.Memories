#!/usr/bin/env bash
# Prepare net10.0 support for the immutable shared CI workflow running SDK 11.
set -euo pipefail

has_framework() {
  dotnet --list-runtimes | awk -v framework="$1" '
    $1 == framework && $2 ~ /^10[.]0[.][0-9]+$/ { found = 1 }
    END { exit !found }'
}

if has_framework Microsoft.NETCore.App && has_framework Microsoft.AspNetCore.App; then
  exit 0
fi

task_dotnet_root="${DOTNET_ROOT:-$(dirname "$(readlink -f "$(command -v dotnet)")")}"
task_install_directory="$(mktemp -d)"
trap 'rm -rf "$task_install_directory"' EXIT
curl --fail --silent --show-error --location --proto '=https' --proto-redir '=https'   https://dot.net/v1/dotnet-install.sh --output "$task_install_directory/dotnet-install.sh"
# SDK 10 supplies both shared frameworks and targeting packs. global.json keeps SDK 11 selected.
bash "$task_install_directory/dotnet-install.sh" --channel 10.0   --install-dir "$task_dotnet_root" --no-path
has_framework Microsoft.NETCore.App
has_framework Microsoft.AspNetCore.App
