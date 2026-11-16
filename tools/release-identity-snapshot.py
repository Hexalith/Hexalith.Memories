#!/usr/bin/env python3
"""Read published Memories identities; compare rollback or additive-release snapshots.

Only GET requests, git ls-remote, gh auth token and a credential-helper get are
used. Credentials stay in memory. Redirects and ambiguous responses fail closed.
"""

import argparse
import base64
import gzip
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request


ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = "Hexalith/Hexalith.Memories"
REGISTRY = "registry.hexalith.com"
SCHEMA = "hexalith.memories.release-identity.v1"
SHA = r"[0-9a-f]{40}"
DIGEST = r"sha256:[0-9a-f]{64}"
INDEX_TYPES = ("application/vnd.oci.image.index.v1+json", "application/vnd.docker.distribution.manifest.list.v2+json")
MANIFEST_TYPES = ("application/vnd.oci.image.manifest.v1+json", "application/vnd.docker.distribution.manifest.v2+json")
SECRETS = []


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate field {key}")
        result[key] = value
    return result


def parse(data):
    return json.loads(data, object_pairs_hook=unique)


def require(condition, member, reason="unparseable reply"):
    if not condition:
        raise ValueError(f"{member}: {reason}")


def normalize_version(value, member):
    require(isinstance(value, str), member, "invalid NuGet version")
    match = re.fullmatch(r"([0-9]+(?:\.[0-9]+){0,3})(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?", value)
    require(match is not None, member, "invalid NuGet version")
    numbers = [int(part) for part in match[1].split(".")]
    numbers += [0] * (3 - len(numbers))
    if len(numbers) == 4 and numbers[-1] == 0:
        numbers.pop()
    return ".".join(map(str, numbers)) + ("-" + match[2].lower() if match[2] else "")


def version_order(value):
    core, _, suffix = value.partition("-")
    numbers = tuple(map(int, core.split(".")))
    return (numbers + (0,) * (4 - len(numbers)), not bool(suffix),
            tuple((0, int(part)) if part.isdigit() else (1, part) for part in suffix.split(".")))


def command(arguments, member, input_text=None):
    try:
        result = subprocess.run(arguments, input=input_text, text=True, capture_output=True, timeout=60, check=False)
    except subprocess.TimeoutExpired:
        raise ValueError(f"{member}: command timeout") from None
    except OSError:
        raise ValueError(f"{member}: command unavailable") from None
    # Never echo command output: auth/credential responders may include secrets.
    require(result.returncode == 0, member, f"command status {result.returncode}")
    return result.stdout.strip()


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, url):
        return None


class Reader:
    def __init__(self):
        self.opener = urllib.request.build_opener(NoRedirect())
        token = command(["gh", "auth", "token", "--hostname", "github.com"], "GitHub credentials")
        require(bool(token), "GitHub credentials", "empty token")
        SECRETS.append(token)
        self.github_headers = {"Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
                               "X-GitHub-Api-Version": "2022-11-28"}
        self._github_repository_id = None
        self.registry_headers = self.registry_credentials()

    @staticmethod
    def registry_credentials():
        member = f"registry {REGISTRY} credentials"
        config_path = Path(os.environ.get("DOCKER_CONFIG", str(Path.home() / ".docker"))) / "config.json"
        try:
            config = parse(config_path.read_text(encoding="utf-8"))
            helper = config.get("credHelpers", {}).get(REGISTRY) or config.get("credsStore")
            if helper:
                require(bool(re.fullmatch(r"[\w.-]+", helper)), member, "invalid credential helper")
                credentials = parse(command([f"docker-credential-{helper}", "get"], member, REGISTRY + "\n"))
                username, secret = credentials["Username"], credentials["Secret"]
                require(isinstance(username, str) and isinstance(secret, str) and bool(username) and bool(secret), member)
                SECRETS.extend([username, secret])
                auth = base64.b64encode(f"{username}:{secret}".encode()).decode()
            else:
                auth = config["auths"][REGISTRY]["auth"]
                require(isinstance(auth, str) and ":" in base64.b64decode(auth, validate=True).decode(), member)
            SECRETS.append(auth)
            return {"Authorization": f"Basic {auth}"}
        except (OSError, KeyError, TypeError, ValueError) as error:
            raise ValueError(f"{member}: local credential store unavailable or invalid") from error

    def get(self, url, member, headers=None, absent=False):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": "Memories-release-identity", **(headers or {})}, method="GET")
            with self.opener.open(request, timeout=60) as response:
                require(response.status == 200, member, f"HTTP {response.status}")
                data = response.read()
                return data, response.headers
        except urllib.error.HTTPError as error:
            status = error.code
            error.close()
            if status == 404 and absent:
                return None, {}
            raise ValueError(f"{member}: HTTP {status}") from None
        except ValueError:
            raise
        except Exception as error:
            raise ValueError(f"{member}: ambiguous {type(error).__name__}") from None

    def json(self, url, member, headers=None, absent=False):
        data, response_headers = self.get(url, member, headers, absent)
        if data is None:
            return None, response_headers
        encoding = response_headers.get("Content-Encoding", "identity").strip().lower()
        require(encoding in ("identity", "gzip"), member, f"unsupported Content-Encoding {encoding}")
        try:
            if encoding == "gzip":
                data = gzip.decompress(data)
            document = parse(data)
            require(document is not None, member, "unparseable JSON null reply")
            return document, response_headers
        except (ValueError, UnicodeError, OSError, EOFError):
            raise ValueError(f"{member}: unparseable JSON reply") from None

    def github(self, path, absent=False):
        return self.json(f"https://api.github.com/repos/{REPOSITORY}/{path}", f"GitHub {path}", self.github_headers, absent)

    def github_repository_id(self):
        if self._github_repository_id is None:
            member = "GitHub repository metadata"
            repository, _ = self.json(f"https://api.github.com/repos/{REPOSITORY}", member, self.github_headers)
            require(isinstance(repository, dict) and type(repository.get("id")) is int and repository["id"] > 0
                    and isinstance(repository.get("full_name"), str) and repository["full_name"].casefold() == REPOSITORY.casefold(),
                    member, "repository identity mismatch")
            self._github_repository_id = repository["id"]
        return self._github_repository_id

    def github_list(self, path):
        result = []
        member = f"GitHub {path}"
        base = f"https://api.github.com/repos/{REPOSITORY}/"
        endpoint = base + path
        allowed_paths = {urllib.parse.urlsplit(endpoint).path, f"/repositories/{self.github_repository_id()}/{path}"}
        url = endpoint + "?per_page=100&page=1"
        seen = set()
        while url:
            require(url not in seen, member, "pagination cycle")
            seen.add(url)
            values, headers = self.github(url[len(base):])
            require(isinstance(values, list), member)
            result.extend(values)
            next_url = None
            link = headers.get("Link")
            for part in re.split(r",\s*(?=<)", link) if link else []:
                match = re.fullmatch(r'\s*<([^>]+)>;\s*rel="([a-z ]+)"\s*', part)
                require(match is not None, member, "unparseable pagination Link")
                candidate = urllib.parse.urljoin(url, match[1])
                parsed = urllib.parse.urlsplit(candidate)
                query = urllib.parse.parse_qs(parsed.query, keep_blank_values=True)
                require(parsed.scheme == "https" and parsed.netloc == "api.github.com" and not parsed.fragment
                        and parsed.path in allowed_paths
                        and set(query) == {"per_page", "page"} and query["per_page"] == ["100"]
                        and len(query["page"]) == 1 and re.fullmatch(r"[1-9][0-9]*", query["page"][0]), member, "unsafe pagination Link")
                if "next" in match[2].split():
                    require(next_url is None and int(query["page"][0]) > int(urllib.parse.parse_qs(urllib.parse.urlsplit(url).query)["page"][0]), member, "invalid next page")
                    # Both authenticated repository aliases identify this exact
                    # collection. Keep requests on the configured named route.
                    next_url = endpoint + "?" + parsed.query
            url = next_url
        return result


def inventories():
    packages = sorted(item["id"] for item in parse((ROOT / "tools/release-packages.json").read_bytes())["packages"])
    images = sorted(item["repository"] for item in parse((ROOT / "tools/release-containers.json").read_bytes())["images"])
    require(len(packages) == len(set(packages)) == 10, "package inventory")
    require(len(images) == len(set(images)) == 5, "image inventory")
    return packages, images


def tags():
    lines = command(["git", "-c", "http.followRedirects=false", "ls-remote", "--tags", f"https://github.com/{REPOSITORY}.git", "refs/tags/v*"], "Git v* tags")
    objects, peeled = {}, {}
    for line in lines.splitlines():
        parts = line.split()
        require(len(parts) == 2 and re.fullmatch(SHA, parts[0]) and parts[1].startswith("refs/tags/v"), "Git v* tags")
        ref = parts[1]
        target = peeled if ref.endswith("^{}") else objects
        ref = ref.removesuffix("^{}")
        require(ref not in target, ref, "duplicate ref")
        target[ref] = parts[0]
    require(set(peeled) <= set(objects), "Git v* tags", "peeled ref without object")
    return {ref: {"object": value, "commit": peeled.get(ref, value)} for ref, value in sorted(objects.items())}


def releases(reader):
    result = {}
    for release in reader.github_list("releases"):
        require(isinstance(release, dict), "GitHub releases")
        member = f"GitHub release {release.get('id', '?')}"
        require(type(release.get("id")) is int and isinstance(release.get("tag_name"), str), member)
        require(type(release.get("draft")) is bool and type(release.get("prerelease")) is bool, member)
        key = str(release["id"])
        require(key not in result, member, "duplicate release")
        assets = {}
        for asset in reader.github_list(f"releases/{key}/assets"):
            require(isinstance(asset, dict), member + " assets")
            name = asset.get("name")
            require(isinstance(name, str) and name and name not in assets, member, "invalid or duplicate asset name")
            require(type(asset.get("size")) is int and asset["size"] >= 0, f"{member}/{name}", "invalid asset size")
            digest = asset.get("digest")
            require(isinstance(digest, str) and re.fullmatch(DIGEST, digest), f"{member}/{name}", "missing or invalid asset SHA-256 digest")
            assets[name] = {"size": asset["size"], "digest": digest}
        result[key] = {"tag": release["tag_name"], "draft": release["draft"], "prerelease": release["prerelease"], "assets": assets}
    latest, _ = reader.github("releases/latest", absent=True)
    if latest is not None:
        require(isinstance(latest, dict) and type(latest.get("id")) is int and isinstance(latest.get("tag_name"), str), "GitHub releases/latest")
        require(str(latest["id"]) in result and result[str(latest["id"])]["tag"] == latest["tag_name"], "GitHub releases/latest", "Latest identity is absent from releases")
        collected = result[str(latest["id"])]
        require(latest.get("draft") is False and latest.get("prerelease") is False
                and collected["draft"] is False and collected["prerelease"] is False,
                "GitHub releases/latest", "Latest must match a published stable release")
        latest = str(latest["id"])
    else:
        require(not any(not item["draft"] and not item["prerelease"] for item in result.values()), "GitHub Latest", "404 with stable releases")
    return result, latest


def package(reader, identifier):
    member = f"NuGet {identifier}"
    slug = identifier.lower()
    flat, _ = reader.json(f"https://api.nuget.org/v3-flatcontainer/{slug}/index.json", member + " flat-container")
    require(isinstance(flat, dict) and isinstance(flat.get("versions"), list), member + " flat-container")
    flat_versions = [normalize_version(value, member + " flat-container") for value in flat["versions"]]
    require(len(flat_versions) == len(set(flat_versions)), member + " flat-container", "duplicate normalized version")
    base = f"https://api.nuget.org/v3/registration5-gz-semver2/{slug}/"
    registration, _ = reader.json(base + "index.json", member + " registration")
    versions = {}

    require(isinstance(registration, dict) and isinstance(registration.get("items"), list), member + " registration")
    require(type(registration.get("count")) is int and registration["count"] == len(registration["items"]), member + " registration", "incomplete index")
    for parent in registration["items"]:
        require(isinstance(parent, dict) and type(parent.get("count")) is int and parent["count"] > 0, member + " registration page", "invalid parent count")
        bounds = {key: normalize_version(parent.get(key), member + " registration page " + key) for key in ("lower", "upper")}
        page = parent
        if "items" not in parent:
            url = parent.get("@id", "")
            require(isinstance(url, str) and url.startswith(base), member, "unexpected registration page")
            page, _ = reader.json(url, member + " registration page")
            require(isinstance(page, dict) and type(page.get("count")) is int and page["count"] == parent["count"], member + " registration page", "parent/page count mismatch")
            require(all(normalize_version(page.get(key), member + " registration page " + key) == value for key, value in bounds.items()), member + " registration page", "parent/page bounds mismatch")
        require(isinstance(page.get("items"), list) and len(page["items"]) == parent["count"], member + " registration page", "incomplete page")
        page_versions = []
        for leaf in page["items"]:
            entry = leaf.get("catalogEntry") if isinstance(leaf, dict) else None
            require(isinstance(entry, dict), member + " registration leaf")
            version = normalize_version(entry.get("version"), member + " registration leaf")
            require(version not in versions and type(entry.get("listed")) is bool, member + "/" + version)
            versions[version] = {"listed": entry["listed"]}
            page_versions.append(version)
        require(min(page_versions, key=version_order) == bounds["lower"] and max(page_versions, key=version_order) == bounds["upper"], member + " registration page", "leaf/page bounds mismatch")
    require(set(flat_versions) == set(versions), member, "flat-container/registration version sets disagree")
    return dict(sorted(versions.items()))


def images(reader, repository):
    member = f"registry {repository}"
    base = f"https://{REGISTRY}/v2/{repository}/"
    url = base + "tags/list?n=100"
    found, pages = {}, set()
    while url:
        require(url not in pages and url.startswith(base), member, "invalid pagination")
        pages.add(url)
        document, headers = reader.json(url, member + " tags", reader.registry_headers)
        require(isinstance(document, dict), member + " tags")
        require(document.get("name") == repository and isinstance(document.get("tags"), list), member + " tags")
        for tag in document["tags"]:
            require(isinstance(tag, str) and re.fullmatch(r"[\w][\w.-]{0,127}", tag) and tag not in found, member, "invalid or duplicate tag")
            raw, manifest_headers = reader.get(base + "manifests/" + tag, member + ":" + tag,
                {**reader.registry_headers, "Accept": ", ".join(INDEX_TYPES + MANIFEST_TYPES)})
            image_member = member + ":" + tag
            require(manifest_headers.get("Content-Encoding", "identity") == "identity", image_member, "ambiguous manifest content encoding")
            try:
                manifest = parse(raw)
            except (ValueError, UnicodeError):
                raise ValueError(f"{member}:{tag}: unparseable manifest") from None
            require(isinstance(manifest, dict), image_member)
            media_type = manifest.get("mediaType")
            require(media_type in INDEX_TYPES + MANIFEST_TYPES and manifest.get("schemaVersion") == 2,
                    image_member, "unknown image format")
            require(manifest_headers.get("Content-Type", "").split(";", 1)[0].strip() == media_type,
                    image_member, "missing or conflicting manifest media type")
            if media_type in INDEX_TYPES:
                require(isinstance(manifest.get("manifests"), list) and manifest["manifests"], image_member, "invalid image index")
                descriptors = manifest["manifests"]
            else:
                require(isinstance(manifest.get("config"), dict) and isinstance(manifest.get("layers"), list), image_member, "invalid image manifest")
                descriptors = [manifest["config"], *manifest["layers"]]
            require(all(isinstance(item, dict) and isinstance(item.get("mediaType"), str)
                        and re.fullmatch(r"[A-Za-z0-9!#$&^_.+-]+/[A-Za-z0-9!#$&^_.+-]+", item["mediaType"])
                        and type(item.get("size")) is int and item["size"] >= 0
                        and isinstance(item.get("digest"), str) and re.fullmatch(DIGEST, item["digest"])
                        for item in descriptors), image_member, "invalid image descriptor")
            digest = "sha256:" + hashlib.sha256(raw).hexdigest()
            require(manifest_headers.get("Docker-Content-Digest") == digest, image_member, "manifest digest mismatch")
            found[tag] = {"digest": digest, "mediaType": media_type}
        link = headers.get("Link")
        require(not link or bool(re.fullmatch(r'<[^>]+>;\s*rel="next"', link)), member, "unparseable pagination")
        url = urllib.parse.urljoin(url, link[1:link.index(">")] ) if link else None
    return dict(sorted(found.items()))


def capture():
    packages, repositories = inventories()
    reader = Reader()
    snapshot = {"schemaVersion": SCHEMA, "repository": REPOSITORY, "registry": REGISTRY, "tags": tags()}
    snapshot["releases"], snapshot["latest"] = releases(reader)
    snapshot["packages"] = {identifier: package(reader, identifier) for identifier in packages}
    snapshot["images"] = {repository: images(reader, repository) for repository in repositories}
    validate_snapshot(snapshot)
    return snapshot


def write_snapshot(path, value):
    data = (json.dumps(value, indent=2, sort_keys=True) + "\n").encode()
    path.parent.mkdir(parents=True, exist_ok=True)
    receipt = Path(str(path) + ".sha256")
    try:
        # Reserve both names before writing. On collision/failure retain incomplete
        # pairs: deleting a pathname could destroy another writer's evidence.
        with path.open("xb") as snapshot_file, receipt.open("xb") as receipt_file:
            snapshot_file.write(data)
            receipt_file.write(f"{hashlib.sha256(data).hexdigest()}  {path.name}\n".encode())
    except FileExistsError as error:
        raise ValueError(f"{error.filename}: refuse to overwrite evidence; any incomplete pair is retained") from None


def read_snapshot(path):
    data = path.read_bytes()
    expected = f"{hashlib.sha256(data).hexdigest()}  {path.name}\n"
    require(Path(str(path) + ".sha256").read_text(encoding="utf-8") == expected, str(path), "SHA-256 receipt mismatch")
    document = parse(data)
    validate_snapshot(document)
    return document


def validate_snapshot(document):
    require(isinstance(document, dict) and set(document) == {"schemaVersion", "repository", "registry", "tags", "releases", "latest", "packages", "images"}
            and document["schemaVersion"] == SCHEMA and document["repository"] == REPOSITORY and document["registry"] == REGISTRY,
            "identity", "snapshot identity/schema mismatch")
    for domain in ("tags", "releases", "packages", "images"):
        require(isinstance(document[domain], dict) and bool(document[domain]), "identity/" + domain, "invalid or empty inventory")
    for ref, item in document["tags"].items():
        member = "identity/tags/" + ref
        require(re.fullmatch(r"refs/tags/v[0-9A-Za-z][0-9A-Za-z._/-]*", ref) and isinstance(item, dict)
                and set(item) == {"object", "commit"} and all(isinstance(value, str) and re.fullmatch(SHA, value) for value in item.values()), member, "invalid tag identity")
    for key, item in document["releases"].items():
        member = "identity/releases/" + key
        require(re.fullmatch(r"[1-9][0-9]*", key) and isinstance(item, dict) and set(item) == {"tag", "draft", "prerelease", "assets"}
                and isinstance(item["tag"], str) and bool(item["tag"]) and type(item["draft"]) is bool
                and type(item["prerelease"]) is bool and isinstance(item["assets"], dict), member, "invalid release identity")
        for name, asset in item["assets"].items():
            require(bool(name) and isinstance(asset, dict) and set(asset) == {"size", "digest"} and type(asset["size"]) is int
                    and asset["size"] >= 0 and isinstance(asset["digest"], str) and re.fullmatch(DIGEST, asset["digest"]), member + "/assets/" + name, "invalid asset identity")
    latest = document["latest"]
    stable = {key for key, item in document["releases"].items() if not item["draft"] and not item["prerelease"]}
    require((latest is None and not stable) or (isinstance(latest, str) and latest in stable), "identity/latest", "Latest must identify a published stable release")
    packages, repositories = inventories()
    require(set(document["packages"]) == set(packages), "identity/packages", "snapshot inventory mismatch")
    require(set(document["images"]) == set(repositories), "identity/images", "snapshot inventory mismatch")
    for identifier, versions in document["packages"].items():
        member = "identity/packages/" + identifier
        require(isinstance(versions, dict) and bool(versions), member, "invalid or empty version inventory")
        for version, item in versions.items():
            require(version == normalize_version(version, member) and isinstance(item, dict) and set(item) == {"listed"}
                    and type(item["listed"]) is bool, member + "/" + version, "invalid package identity")
    for repository, entries in document["images"].items():
        member = "identity/images/" + repository
        require(isinstance(entries, dict) and bool(entries), member, "invalid or empty tag inventory")
        for tag, item in entries.items():
            require(re.fullmatch(r"[\w][\w.-]{0,127}", tag) and isinstance(item, dict) and set(item) == {"digest", "mediaType"}
                    and isinstance(item["digest"], str) and re.fullmatch(DIGEST, item["digest"])
                    and item["mediaType"] in INDEX_TYPES + MANIFEST_TYPES, member + "/" + tag, "invalid image identity")


def differences(before, after, path="identity"):
    if isinstance(before, dict) and isinstance(after, dict):
        return [item for key in sorted(set(before) | set(after)) for item in
                ([f"{path}/{key}: {'added' if key not in before else 'removed'}"] if key not in before or key not in after
                 else differences(before[key], after[key], f"{path}/{key}"))]
    return [] if before == after else [f"{path}: changed"]


def compare(before, after, expect, version=None, source_sha=None):
    validate_snapshot(before)
    validate_snapshot(after)
    if expect == "identical":
        return differences(before, after)
    require(isinstance(version, str) and bool(re.fullmatch(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?", version)), "additive", "--version is required (without v)")
    require("-" not in version or not any(item.isdigit() and len(item) > 1 and item.startswith("0") for item in version.split("-", 1)[1].split(".")), "additive", "numeric prerelease identifier has a leading zero")
    require(isinstance(source_sha, str) and bool(re.fullmatch(SHA, source_sha)), "additive", "--source-sha is required (full lowercase SHA)")
    expected = parse(json.dumps(before))
    tag = "v" + version
    ref = "refs/tags/" + tag
    require(ref not in before["tags"], ref, "already existed before release")
    observed_tag = after["tags"].get(ref)
    require(isinstance(observed_tag, dict) and set(observed_tag) == {"object", "commit"}
            and re.fullmatch(SHA, observed_tag.get("object", "")) and observed_tag.get("commit") == source_sha, ref, "missing tag or wrong source")
    expected["tags"][ref] = observed_tag
    new_releases = {key: value for key, value in after["releases"].items() if key not in before["releases"]}
    require(len(new_releases) == 1, "GitHub releases", "expected exactly one new release")
    key, release = next(iter(new_releases.items()))
    require(release["tag"] == tag and release["draft"] is False and release["prerelease"] == ("-" in version), "GitHub release " + tag, "wrong tag or publication state")
    packages, _ = inventories()
    assets = {f"{identifier}.{version}.nupkg" for identifier in packages} | {
        "hexalith-memories-production.yaml", "hexalith-memories-release-evidence.tar.gz", "hexalith-memories-release-evidence.tar.gz.sha256.json"}
    require(set(release["assets"]) == assets and all(type(item.get("size")) is int and item["size"] > 0 and isinstance(item.get("digest"), str)
            and re.fullmatch(DIGEST, item["digest"]) for item in release["assets"].values()), "GitHub release " + tag, "expected the 13 nonempty digested assets")
    expected["releases"][key] = release
    require(after["latest"] in (before["latest"], key), "GitHub Latest", "moved elsewhere")
    expected["latest"] = after["latest"]
    for identifier in packages:
        require(version.lower() not in before["packages"][identifier], "NuGet " + identifier, "version already existed")
        expected["packages"][identifier][version.lower()] = {"listed": True}
    for repository in expected["images"]:
        require(version not in before["images"][repository], "registry " + repository, "tag already existed")
        identity = after["images"][repository].get(version)
        require(isinstance(identity, dict) and set(identity) == {"digest", "mediaType"}
                and isinstance(identity.get("digest"), str) and bool(re.fullmatch(DIGEST, identity["digest"]))
                and identity.get("mediaType") in INDEX_TYPES, "registry " + repository + ":" + version, "new release tag must be an image index")
        expected["images"][repository][version] = identity
    return differences(expected, after)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="operation", required=True)
    capture_parser = sub.add_parser("capture")
    capture_parser.add_argument("--output", type=Path, required=True)
    compare_parser = sub.add_parser("compare")
    compare_parser.add_argument("before", type=Path)
    compare_parser.add_argument("after", type=Path)
    compare_parser.add_argument("--expect", choices=("identical", "additive"), required=True)
    compare_parser.add_argument("--version")
    compare_parser.add_argument("--source-sha")
    args = parser.parse_args()
    try:
        if args.operation == "capture":
            require(not args.output.exists() and not Path(str(args.output) + ".sha256").exists(), str(args.output), "refuse to overwrite evidence")
            value = capture()
            write_snapshot(args.output, value)
            print(f"[release-identity] captured {args.output}")
        else:
            delta = compare(read_snapshot(args.before), read_snapshot(args.after), args.expect, args.version, args.source_sha)
            require(not delta, "comparison", "\n" + "\n".join(delta))
            print(f"[release-identity] pass: {args.expect}")
        return 0
    except Exception as error:
        message = str(error)
        for secret in sorted(filter(None, SECRETS), key=len, reverse=True):
            message = message.replace(secret, "[redacted]")
        print(f"[release-identity] refused: {message}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
