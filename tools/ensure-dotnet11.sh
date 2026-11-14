#!/usr/bin/env bash
# Prepare net11.0 shared frameworks for the immutable shared CI workflow.
set -euo pipefail

has_framework() {
  dotnet --list-runtimes | awk -v framework="$1" '
    $1 == framework && $2 ~ /^11[.]0[.][0-9]+$/ { found = 1 }
    END { exit !found }'
}

if has_framework Microsoft.NETCore.App && has_framework Microsoft.AspNetCore.App; then
  exit 0
fi

task_dotnet_root="${DOTNET_ROOT:-$(dirname "$(readlink -f "$(command -v dotnet)")")}"
task_install_directory="$(mktemp -d)"
trap 'rm -rf "$task_install_directory"' EXIT
curl --fail --silent --show-error --location --proto '=https' --proto-redir '=https'   https://dot.net/v1/dotnet-install.sh --output "$task_install_directory/dotnet-install.sh"
# SDK 11 supplies both current shared frameworks and targeting packs.
bash "$task_install_directory/dotnet-install.sh" --channel 11.0   --install-dir "$task_dotnet_root" --no-path
has_framework Microsoft.NETCore.App
has_framework Microsoft.AspNetCore.App
