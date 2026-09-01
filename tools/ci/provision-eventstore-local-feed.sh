#!/usr/bin/env bash
# Story 28.1 — provision a job-scoped, ephemeral local NuGet feed for Hexalith.EventStore.*
# packages at the Story 1.20 owner-approved source SHA, rebuilt under Memories' mandated SDK.
#
# WHY THIS EXISTS: EventStore Story 1.20's approved package identity
# (999.1.20-proof.fa2d1c9910f8) is not published on nuget.org and the proof packet's original
# SHA-256 hash manifest was sealed under SDK 10.0.302, which is not reproducible under Memories'
# mandated SDK 10.0.400 (see spec-28-1-adopt-owner-approved-eventstore-runtime-identity.md). The
# accepted mitigation (sprint-change-approved, "Option B") is to rebuild the approved SOURCE SHA
# under the mandated SDK and re-sign the result ourselves so the repo's
# signatureValidationMode=require policy still holds -- but NEVER to persist that signing
# certificate, its private key, or a prebuilt .nupkg anywhere in the repo, in CI caches, or in CI
# secrets. Every CI job run must generate its own certificate and its own signed packages from
# scratch and discard them when the job ends. This script does exactly that, once per job.
#
# USAGE
#   tools/ci/provision-eventstore-local-feed.sh [<eventstore-source-sha> [<package-version>]]
#
#   <eventstore-source-sha>  Defaults to fa2d1c9910f8976553adb33dcdb1c9ff2ea75594 (Story 1.20
#                            owner-approved SHA). The script refuses to run if the
#                            references/Hexalith.EventStore submodule is not checked out at
#                            exactly this commit -- it must never sign packages built from an
#                            unverified checkout.
#   <package-version>        Defaults to 999.1.20-proof.fa2d1c9910f8 (the approved proof version).
#
# OUTPUT
#   On success, prints the generated NuGet config's absolute path to stdout on its own line and:
#     - appends `EVENTSTORE_LOCAL_NUGET_CONFIG=<path>` to $GITHUB_ENV, when set (so every later
#       step in the same job can do `--configfile "$EVENTSTORE_LOCAL_NUGET_CONFIG"` without
#       re-reading step outputs), and
#     - appends `config_path=<path>` to $GITHUB_OUTPUT, when set (for callers that prefer
#       `${{ steps.<id>.outputs.config_path }}`).
#   Outside CI (neither variable set), only the stdout path line is produced -- callers should
#   capture it, e.g. `CONFIG=$(tools/ci/provision-eventstore-local-feed.sh)`.
#
# EVERYTHING THIS SCRIPT CREATES LIVES UNDER A FRESH mktemp/RUNNER_TEMP DIRECTORY AND IS NEVER
# WRITTEN INTO THE REPOSITORY TREE OR ANY PERSISTENT CACHE.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

EVENTSTORE_SOURCE_SHA="${1:-fa2d1c9910f8976553adb33dcdb1c9ff2ea75594}"
PACKAGE_VERSION="${2:-999.1.20-proof.fa2d1c9910f8}"
EVENTSTORE_DIR="$REPO_ROOT/references/Hexalith.EventStore"
RELEASE_MANIFEST="$EVENTSTORE_DIR/tools/release-packages.json"
TRACKED_NUGET_CONFIG="$REPO_ROOT/NuGet.config"

log() { echo "[provision-eventstore-local-feed] $*" >&2; }

if [[ ! -d "$EVENTSTORE_DIR/.git" ]] && [[ ! -f "$EVENTSTORE_DIR/.git" ]]; then
    log "ERROR: references/Hexalith.EventStore is not an initialized git checkout at $EVENTSTORE_DIR."
    exit 1
fi

ACTUAL_SHA="$(git -C "$EVENTSTORE_DIR" rev-parse HEAD)"
if [[ "$ACTUAL_SHA" != "$EVENTSTORE_SOURCE_SHA" ]]; then
    log "ERROR: references/Hexalith.EventStore is checked out at $ACTUAL_SHA, expected the" \
        "owner-approved source SHA $EVENTSTORE_SOURCE_SHA. Refusing to pack/sign an unverified" \
        "checkout."
    exit 1
fi

# A checkout at the right commit with uncommitted local edits is NOT the verified approved
# source -- it is arbitrary, unaudited working-tree content sitting on top of that commit. Fail
# closed rather than silently packing and signing it as if it were the approved SHA.
EVENTSTORE_DIRTY_STATUS="$(git -C "$EVENTSTORE_DIR" status --porcelain)"
if [[ -n "$EVENTSTORE_DIRTY_STATUS" ]]; then
    log "ERROR: references/Hexalith.EventStore has uncommitted local modifications; refusing to" \
        "pack/sign a working tree that is not exactly the approved source SHA. 'git status" \
        "--porcelain' output:"
    log "$EVENTSTORE_DIRTY_STATUS"
    exit 1
fi

if [[ ! -f "$RELEASE_MANIFEST" ]]; then
    log "ERROR: release package manifest not found at $RELEASE_MANIFEST."
    exit 1
fi

if [[ ! -f "$TRACKED_NUGET_CONFIG" ]]; then
    log "ERROR: tracked NuGet.config not found at $TRACKED_NUGET_CONFIG."
    exit 1
fi

# Job-scoped scratch space. Prefer $RUNNER_TEMP (a GitHub Actions job-scoped temp directory the
# runner wipes when the job ends) so nothing here can accidentally outlive the job even if a step
# forgets to clean up; fall back to mktemp for local/non-Actions dry runs.
WORK_ROOT="${RUNNER_TEMP:-$(mktemp -d)}/eventstore-local-feed-$$"
FEED_DIR="$WORK_ROOT/packages"
CERT_DIR="$WORK_ROOT/cert"
CONFIG_DIR="$WORK_ROOT/config"
mkdir -p "$FEED_DIR" "$CERT_DIR" "$CONFIG_DIR"

cleanup_private_key_material() {
    # Defense in depth: shred the private key the instant we're done signing, rather than
    # waiting for the job/runner teardown to remove it. The certificate and pfx never leave
    # $CERT_DIR and are never referenced again after this function runs.
    if [[ -d "$CERT_DIR" ]]; then
        find "$CERT_DIR" -type f -exec shred -u {} \; 2>/dev/null || rm -f "$CERT_DIR"/*
        rmdir "$CERT_DIR" 2>/dev/null || true
    fi
}
trap cleanup_private_key_material EXIT

log "Packing Hexalith.EventStore.* packages at $PACKAGE_VERSION from source SHA $EVENTSTORE_SOURCE_SHA into $FEED_DIR"
# Deliberately NOT invoking references/Hexalith.EventStore/tools/pack-release-packages.py
# directly: that script hardcodes its `dotnet pack` subprocess's cwd to the EventStore repo root
# (relative to the script's own file location), which makes `dotnet` resolve EventStore's own
# global.json (pinned to the exact SDK 10.0.302 used to seal the original proof-packet hashes --
# see spec-28-1) instead of Memories' mandated SDK 10.0.400. Packing instead reads the same
# authoritative project list from EventStore's own tools/release-packages.json and invokes
# `dotnet pack` per project with cwd=$REPO_ROOT (Memories), so Memories' own global.json
# (SDK 10.0.400, rollForward latestFeature) is what resolves -- this is the same mechanism, and
# produces the same package set, EventStore's own release recipe uses, just run under the SDK
# this repo actually mandates.

# Read the project list into a real file rather than a process-substitution pipe: a
# process-substitution's exit code is invisible to the consuming `while`/`set -e` (a crashed or
# truncated `python3` would silently look like "zero projects" instead of failing the script).
# Writing to a file lets us check python3's own exit code directly, and separately verify the
# parsed count matches the manifest's own package count before packing anything.
PROJECT_LIST_FILE="$WORK_ROOT/release-packages.projects.nul"
EXPECTED_PACKAGE_COUNT="$(python3 -c "
import json, sys
with open(sys.argv[1]) as f:
    data = json.load(f)
print(len(data['packages']))
" "$RELEASE_MANIFEST")"
if [[ -z "$EXPECTED_PACKAGE_COUNT" || "$EXPECTED_PACKAGE_COUNT" -eq 0 ]]; then
    log "ERROR: could not determine an expected package count from $RELEASE_MANIFEST."
    exit 1
fi

if ! python3 -c "
import json, sys
with open(sys.argv[1]) as f:
    data = json.load(f)
with open(sys.argv[2], 'wb') as out:
    for package in data['packages']:
        out.write(package['project'].encode('utf-8') + b'\0')
" "$RELEASE_MANIFEST" "$PROJECT_LIST_FILE"; then
    log "ERROR: failed to parse $RELEASE_MANIFEST (python3 exited non-zero) -- refusing to" \
        "continue with a possibly truncated or missing project list."
    exit 1
fi

PARSED_PROJECT_COUNT="$(tr -dc '\0' < "$PROJECT_LIST_FILE" | wc -c | tr -d ' ')"
if [[ "$PARSED_PROJECT_COUNT" -ne "$EXPECTED_PACKAGE_COUNT" ]]; then
    log "ERROR: parsed $PARSED_PROJECT_COUNT project path(s) from $RELEASE_MANIFEST but the" \
        "manifest itself lists $EXPECTED_PACKAGE_COUNT package(s) -- the project list may have" \
        "been truncated or the parser crashed mid-stream. Refusing to pack a partial/corrupt set."
    exit 1
fi

pack_one_project_with_retry() {
    local project_path="$1"
    local attempt
    local -r max_attempts=3
    local -r retry_delay_seconds=5
    for ((attempt = 1; attempt <= max_attempts; attempt++)); do
        # Redirect dotnet's own stdout to stderr: this script's stdout contract is "the generated
        # config path, and nothing else" (see USAGE/OUTPUT above), so build noise must not land
        # there.
        if ( cd "$REPO_ROOT" && dotnet pack "$project_path" \
            --configuration Release \
            --output "$FEED_DIR" \
            -p:Version="$PACKAGE_VERSION" \
            -p:GeneratePackageOnBuild=false \
            -p:UseHexalithProjectReferences=false ) 1>&2; then
            return 0
        fi
        log "  dotnet pack attempt $attempt/$max_attempts failed for $(basename "$project_path")."
        if [[ "$attempt" -lt "$max_attempts" ]]; then
            sleep "$retry_delay_seconds"
        fi
    done
    return 1
}

PACKED_PROJECT_COUNT=0
while IFS= read -r -d '' project_relative_path; do
    project_path="$EVENTSTORE_DIR/$project_relative_path"
    if [[ ! -f "$project_path" ]]; then
        log "ERROR: manifest project not found: $project_path"
        exit 1
    fi
    log "  dotnet pack $project_relative_path"
    if ! pack_one_project_with_retry "$project_path"; then
        log "ERROR: failed to pack $project_relative_path after retrying."
        exit 1
    fi
    PACKED_PROJECT_COUNT=$((PACKED_PROJECT_COUNT + 1))
done < "$PROJECT_LIST_FILE"

if [[ "$PACKED_PROJECT_COUNT" -ne "$EXPECTED_PACKAGE_COUNT" ]]; then
    log "ERROR: packed $PACKED_PROJECT_COUNT project(s) but expected $EXPECTED_PACKAGE_COUNT" \
        "per $RELEASE_MANIFEST."
    exit 1
fi

NUPKG_COUNT="$(find "$FEED_DIR" -maxdepth 1 -name '*.nupkg' | wc -l | tr -d ' ')"
if [[ "$NUPKG_COUNT" -ne "$EXPECTED_PACKAGE_COUNT" ]]; then
    log "ERROR: found $NUPKG_COUNT .nupkg file(s) in $FEED_DIR but expected" \
        "$EXPECTED_PACKAGE_COUNT per $RELEASE_MANIFEST."
    exit 1
fi
log "Packed $NUPKG_COUNT package(s)."

log "Generating a fresh, ephemeral code-signing certificate (this job run only)."
# X.509 CommonName is capped at 64 characters (ub-common-name); keep this fixed and short. It
# does not need to be unique -- the certificate is trusted by its SHA-256 fingerprint (computed
# below), not by name, and this cert/key never leaves this job run.
CERT_SUBJECT_CN="Hexalith Memories CI EventStore Rebuild"
CERT_CONF="$CERT_DIR/codesign.cnf"
CERT_KEY="$CERT_DIR/codesign.key"
CERT_CRT="$CERT_DIR/codesign.crt"
CERT_PFX="$CERT_DIR/codesign.pfx"
# Random per-run password: only ever held in this process's memory/local temp files, never
# printed, never exported, and shredded along with the rest of $CERT_DIR on exit.
#
# KNOWN LIMITATION: `dotnet nuget sign --certificate-password` only accepts the password as a
# plain CLI argument (checked via `dotnet nuget sign --help` on the SDK this script targets) --
# there is no environment-variable or stdin-based alternative. That makes the password briefly
# visible to other processes on the same machine via /proc/<pid>/cmdline (or `ps`) for the
# lifetime of each `dotnet nuget sign` invocation below. Accepted as a low-severity residual risk
# given: this certificate is generated fresh per job run, is never exported or reused, secures
# nothing beyond satisfying this job's own signatureValidationMode=require check, and its
# private-key material is shredded immediately after signing (see cleanup_private_key_material) --
# a process on the same ephemeral runner that could read another process's /proc/<pid>/cmdline
# could reach the key file directly anyway. If a future NuGet CLI version adds an
# env-var/stdin/file-based password option, switch to it here.
CERT_PASSWORD="$(openssl rand -base64 32)"

cat > "$CERT_CONF" <<EOF
[req]
default_bits = 2048
prompt = no
distinguished_name = dn
x509_extensions = v3_req

[dn]
CN = ${CERT_SUBJECT_CN}

[v3_req]
keyUsage = critical, digitalSignature
extendedKeyUsage = critical, codeSigning
basicConstraints = critical, CA:false
EOF

openssl req -x509 -newkey rsa:2048 -keyout "$CERT_KEY" -out "$CERT_CRT" \
    -days 2 -nodes -config "$CERT_CONF" -sha256 >/dev/null 2>&1

openssl pkcs12 -export -out "$CERT_PFX" -inkey "$CERT_KEY" -in "$CERT_CRT" \
    -passout "pass:${CERT_PASSWORD}" >/dev/null 2>&1

CERT_FINGERPRINT_SHA256="$(
    openssl x509 -in "$CERT_CRT" -noout -fingerprint -sha256 \
        | sed -e 's/^.*=//' -e 's/://g'
)"
log "Ephemeral signing certificate fingerprint (SHA-256): $CERT_FINGERPRINT_SHA256"

# RFC 3161 timestamping is required: signatureValidationMode=require elevates the "signature
# should be timestamped" warning (NU3027) to an error without it. Try a short list of public
# timestamp authorities for resilience against a single endpoint being unreachable from a CI
# runner.
TIMESTAMP_SERVERS=(
    "http://timestamp.digicert.com"
    "http://timestamp.sectigo.com"
    "http://timestamp.entrust.net/TSS/RFC3161sha2TS"
)

# Per-attempt wall-clock bound so a hung/slow TSA connection cannot stall the job indefinitely --
# each attempt gets at most 90s before being killed and falling through to the next server (or
# failing the package if all three are tried).
readonly TSA_ATTEMPT_TIMEOUT_SECONDS=90

sign_one_package() {
    local package_path="$1"
    local server
    for server in "${TIMESTAMP_SERVERS[@]}"; do
        if timeout "${TSA_ATTEMPT_TIMEOUT_SECONDS}s" dotnet nuget sign "$package_path" \
            --certificate-path "$CERT_PFX" \
            --certificate-password "$CERT_PASSWORD" \
            --overwrite \
            --timestamper "$server" >/dev/null 2>&1; then
            return 0
        fi
        log "  timestamper $server failed or timed out after ${TSA_ATTEMPT_TIMEOUT_SECONDS}s for" \
            "$(basename "$package_path"); trying next."
    done
    return 1
}

log "Signing and RFC-3161-timestamping $NUPKG_COUNT package(s)."
while IFS= read -r -d '' package_path; do
    package_name="$(basename "$package_path")"
    if ! sign_one_package "$package_path"; then
        log "ERROR: failed to sign/timestamp $package_name against every configured timestamp server."
        exit 1
    fi
    log "  signed: $package_name"
done < <(find "$FEED_DIR" -maxdepth 1 -name '*.nupkg' -print0)

log "Deriving the ephemeral NuGet.config from the tracked NuGet.config's nuget.org trust settings."
GENERATED_CONFIG="$CONFIG_DIR/NuGet.ci-ephemeral.config"

FEED_DIR="$FEED_DIR" \
CERT_FINGERPRINT_SHA256="$CERT_FINGERPRINT_SHA256" \
TRACKED_NUGET_CONFIG="$TRACKED_NUGET_CONFIG" \
GENERATED_CONFIG="$GENERATED_CONFIG" \
python3 - <<'PYEOF'
import os
import xml.etree.ElementTree as ET

tracked_path = os.environ["TRACKED_NUGET_CONFIG"]
feed_dir = os.environ["FEED_DIR"]
fingerprint = os.environ["CERT_FINGERPRINT_SHA256"]
out_path = os.environ["GENERATED_CONFIG"]

tree = ET.parse(tracked_path)
root = tree.getroot()


def require_element_with_children(parent, tag, path):
    """Find `tag` under `parent` and fail closed if it is missing OR present-but-empty.

    A present-but-childless element (e.g. `<packageSources></packageSources>`) would otherwise
    parse successfully and silently produce an ephemeral config missing nuget.org / its trust
    entries entirely -- fail loudly instead of generating that incomplete config.
    """
    element = parent.find(tag)
    if element is None:
        raise SystemExit(f"{path}: no <{tag}> element found.")
    if len(element) == 0:
        raise SystemExit(
            f"{path}: <{tag}> element has no children -- refusing to generate a config that "
            f"would silently drop it (expected at least one entry, e.g. nuget.org)."
        )
    return element


# Copy the tracked config's <config> block verbatim (signatureValidationMode etc.) -- this
# script must never weaken that policy, only add a scoped local source + its own trust entry.
config_block = require_element_with_children(root, "config", tracked_path)

# Copy every existing packageSource (nuget.org today) verbatim.
tracked_sources = require_element_with_children(root, "packageSources", tracked_path)

# Copy every existing packageSourceMapping <packageSource> block verbatim.
tracked_mapping = require_element_with_children(root, "packageSourceMapping", tracked_path)

# Copy every existing trustedSigners entry (nuget.org's repository certificates) verbatim, so
# this script never hand-duplicates/hardcodes them and silently drifts if they rotate.
tracked_trusted_signers = require_element_with_children(root, "trustedSigners", tracked_path)

configuration = ET.Element("configuration")

new_config = ET.SubElement(configuration, "config")
for child in config_block:
    new_config.append(child)

package_sources = ET.SubElement(configuration, "packageSources")
ET.SubElement(package_sources, "clear")
for child in tracked_sources:
    if child.tag == "clear":
        continue
    package_sources.append(child)
local_source = ET.SubElement(package_sources, "add")
local_source.set("key", "hexalith-eventstore-story-28-1-ci-ephemeral")
local_source.set("value", feed_dir)

package_source_mapping = ET.SubElement(configuration, "packageSourceMapping")
ET.SubElement(package_source_mapping, "clear")
local_mapping = ET.SubElement(package_source_mapping, "packageSource")
local_mapping.set("key", "hexalith-eventstore-story-28-1-ci-ephemeral")
local_pattern = ET.SubElement(local_mapping, "package")
local_pattern.set("pattern", "Hexalith.EventStore.*")
for child in tracked_mapping:
    if child.tag == "clear":
        continue
    package_source_mapping.append(child)

trusted_signers = ET.SubElement(configuration, "trustedSigners")
ET.SubElement(trusted_signers, "clear")
for child in tracked_trusted_signers:
    if child.tag == "clear":
        continue
    trusted_signers.append(child)
author = ET.SubElement(trusted_signers, "author")
author.set("name", "HexalithMemoriesCiStory281EphemeralEventStoreRebuild")
certificate = ET.SubElement(author, "certificate")
certificate.set("fingerprint", fingerprint)
certificate.set("hashAlgorithm", "SHA256")
certificate.set("allowUntrustedRoot", "true")

ET.indent(configuration, space="  ")
ET.ElementTree(configuration).write(out_path, encoding="utf-8", xml_declaration=True)
PYEOF

if [[ ! -f "$GENERATED_CONFIG" ]]; then
    log "ERROR: failed to generate $GENERATED_CONFIG."
    exit 1
fi

log "Ephemeral NuGet config written to $GENERATED_CONFIG."

if [[ -n "${GITHUB_ENV:-}" ]]; then
    echo "EVENTSTORE_LOCAL_NUGET_CONFIG=$GENERATED_CONFIG" >> "$GITHUB_ENV"
fi
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    echo "config_path=$GENERATED_CONFIG" >> "$GITHUB_OUTPUT"
fi

echo "$GENERATED_CONFIG"
