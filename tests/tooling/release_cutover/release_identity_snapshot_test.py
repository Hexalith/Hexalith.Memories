"""Exercise read-only responders, rollback identity and the additive-only release gate."""

import copy
from email.message import Message
import gzip
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import hashlib
import importlib.util
import io
import json
import os
import shutil
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
import unittest
from unittest import mock
import urllib.error


ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location("release_identity_snapshot", ROOT / "tools/release-identity-snapshot.py")
TOOL = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(TOOL)
SOURCE = "a" * 40
OLD_SOURCE = "b" * 40
GITHUB_ID = 1188027961
DESCRIPTOR = {"mediaType": "application/vnd.example.unknown-child+json", "size": 10, "digest": "sha256:" + "c" * 64}
INDEX = json.dumps({"schemaVersion": 2, "mediaType": "application/vnd.oci.image.index.v1+json", "manifests": [DESCRIPTOR]}).encode()
INDEX_DIGEST = "sha256:" + hashlib.sha256(INDEX).hexdigest()
INDEX_IDENTITY = {"digest": INDEX_DIGEST, "mediaType": TOOL.INDEX_TYPES[0]}
PACKAGES, IMAGES = TOOL.inventories()


def asset(name):
    return {"name": name, "size": 10, "digest": "sha256:" + "d" * 64}


def release(identifier=1, version="4.0.0", **extra):
    return {"id": identifier, "tag_name": "v" + version, "draft": False, "prerelease": False, **extra}


class Response:
    def __init__(self, value, headers=None):
        self.status = 200
        self.data = value if isinstance(value, bytes) else json.dumps(value).encode()
        self.headers = Message()
        for key, value in (headers or {}).items():
            self.headers[key] = value

    def __enter__(self):
        return self

    def __exit__(self, *args):
        return False

    def read(self):
        return self.data


class Responders:
    """Fake the git, gh, credential helper, NuGet, GitHub and registry read seams."""
    def __init__(self):
        self.requests = []
        self.fault = None

    def run(self, arguments, **kwargs):
        if arguments[:3] == ["gh", "auth", "token"]:
            output = "fixture-github-secret"
        elif arguments[0] == "git" and "ls-remote" in arguments:
            if "http.followRedirects=false" not in arguments:
                raise AssertionError("git HTTP redirects must be refused")
            output = f"{OLD_SOURCE}\trefs/tags/v3.0.0\n{'c' * 40}\trefs/tags/v4.0.0\n{SOURCE}\trefs/tags/v4.0.0^{{}}\n"
        elif arguments == ["docker-credential-fixture", "get"]:
            if kwargs["input"] != TOOL.REGISTRY + "\n":
                raise AssertionError("credential get must use the exact registry")
            output = json.dumps({"Username": "fixture-user", "Secret": "fixture-registry-secret"})
        else:
            raise AssertionError(f"unexpected command {arguments}")
        return subprocess.CompletedProcess(arguments, 0, output, "")

    def open(self, request, **kwargs):
        url = request.full_url
        self.requests.append(request)
        if request.get_method() != "GET":
            raise AssertionError("only GET is authorized")
        if self.fault:
            result = self.fault(url)
            if result is not None:
                return result
        if "api.github.com" in url:
            if "Authorization" not in request.headers:
                raise AssertionError("GitHub must be authenticated")
            if url == f"https://api.github.com/repos/{TOOL.REPOSITORY}":
                return Response({"id": GITHUB_ID, "full_name": TOOL.REPOSITORY})
            if "/assets?" in url:
                return Response([asset("historical.yaml")])
            if url.endswith("/releases/latest"):
                return Response(release())
            return Response([release(), release(2, "4.1.0-preview", draft=True, prerelease=True)])
        if "/v3-flatcontainer/" in url:
            return Response({"versions": ["3.9.0", "4.0.0"]})
        if "/registration5-gz-semver2/" in url:
            return Response({"count": 1, "items": [{"count": 2, "lower": "3.9.0", "upper": "4.0.0", "items": [
                {"catalogEntry": {"version": "4.0.0", "listed": True}},
                {"catalogEntry": {"version": "3.9.0", "listed": False}}]}]})
        if TOOL.REGISTRY in url:
            if not request.headers.get("Authorization", "").startswith("Basic "):
                raise AssertionError("registry must use local stored credentials")
            if "/tags/list?" in url:
                name = url.split("/v2/", 1)[1].split("/", 1)[0]
                return Response({"name": name, "tags": ["4.0.0"]})
            return Response(INDEX, {"Docker-Content-Digest": INDEX_DIGEST, "Content-Type": TOOL.INDEX_TYPES[0]})
        raise AssertionError(f"unexpected URL {url}")


class ReleaseIdentitySnapshotTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        (self.directory / "config.json").write_text(json.dumps({"credsStore": "fixture"}))
        self.responders = Responders()
        self.env = mock.patch.dict("os.environ", {"DOCKER_CONFIG": str(self.directory)})
        self.env.start()
        self.addCleanup(self.env.stop)
        self.run_patch = mock.patch.object(TOOL.subprocess, "run", side_effect=self.responders.run)
        self.run_patch.start()
        self.addCleanup(self.run_patch.stop)
        self.open_patch = mock.patch.object(TOOL.urllib.request, "build_opener", return_value=self.responders)
        self.open_patch.start()
        self.addCleanup(self.open_patch.stop)
        TOOL.SECRETS.clear()

    def captured(self):
        return TOOL.capture()

    def invoke(self, *arguments):
        output = io.StringIO()
        with mock.patch.object(sys, "argv", ["release-identity-snapshot.py", *map(str, arguments)]), mock.patch("sys.stderr", output), mock.patch("sys.stdout", output):
            code = TOOL.main()
        return code, output.getvalue()

    def additive(self, before, version="4.0.1"):
        after = copy.deepcopy(before)
        after["tags"]["refs/tags/v" + version] = {"object": SOURCE, "commit": SOURCE}
        names = [f"{identifier}.{version}.nupkg" for identifier in PACKAGES] + [
            "hexalith-memories-production.yaml", "hexalith-memories-release-evidence.tar.gz", "hexalith-memories-release-evidence.tar.gz.sha256.json"]
        after["releases"]["3"] = {"tag": "v" + version, "draft": False, "prerelease": "-" in version,
            "assets": {name: {key: value for key, value in asset(name).items() if key != "name"} for name in names}}
        for identifier in PACKAGES:
            after["packages"][identifier][version.lower()] = {"listed": True}
        for repository in IMAGES:
            after["images"][repository][version] = copy.deepcopy(INDEX_IDENTITY)
        return after

    def test_capture_all_domains_sorted_json_and_sha256_receipt(self):
        path = self.directory / "before.json"
        code, output = self.invoke("capture", "--output", path)
        self.assertEqual(0, code, output)
        document = TOOL.read_snapshot(path)
        self.assertEqual({"object": "c" * 40, "commit": SOURCE}, document["tags"]["refs/tags/v4.0.0"])
        self.assertEqual({"object": OLD_SOURCE, "commit": OLD_SOURCE}, document["tags"]["refs/tags/v3.0.0"])
        self.assertEqual(10, len(document["packages"]))
        self.assertEqual(5, len(document["images"]))
        self.assertTrue(document["releases"]["2"]["draft"])
        self.assertTrue(document["releases"]["2"]["prerelease"])
        self.assertFalse(document["packages"][PACKAGES[0]]["3.9.0"]["listed"])
        self.assertEqual((json.dumps(document, indent=2, sort_keys=True) + "\n").encode(), path.read_bytes())
        self.assertNotIn("fixture-github-secret", path.read_text())
        self.assertNotIn("fixture-registry-secret", path.read_text())
        self.assertTrue(all(request.get_method() == "GET" for request in self.responders.requests))
        code, output = self.invoke("capture", "--output", path)
        self.assertEqual(1, code)
        self.assertIn("refuse to overwrite", output)

    def test_missing_or_null_asset_digest_refuses_capture_without_evidence(self):
        for label, metadata in (
            ("missing", {"name": "historical.yaml", "size": 10}),
            ("null", {"name": "historical.yaml", "size": 10, "digest": None}),
            ("invalid", {"name": "historical.yaml", "size": 10, "digest": "unknown"}),
        ):
            with self.subTest(digest=label):
                self.responders.fault = lambda url: Response([metadata]) if "/releases/1/assets?" in url else None
                path = self.directory / "failed.json"
                code, output = self.invoke("capture", "--output", path)
                self.assertEqual(1, code, output)
                self.assertIn("GitHub release 1/historical.yaml", output)
                self.assertIn("missing or invalid asset SHA-256 digest", output)
                self.assertFalse(path.exists())
                self.assertFalse(Path(str(path) + ".sha256").exists())

    def test_malformed_latest_identity_names_member_and_writes_no_snapshot(self):
        for latest in ([], [release()], "not-an-object", 7, {}, {"id": [], "tag_name": "v4.0.0"}, None):
            with self.subTest(latest=latest):
                self.responders.fault = lambda url: Response(latest) if url.endswith("/releases/latest") else None
                path = self.directory / "failed.json"
                code, output = self.invoke("capture", "--output", path)
                self.assertEqual(1, code, output)
                self.assertIn("GitHub releases/latest", output)
                self.assertNotIn("AttributeError", output)
                self.assertNotIn("has no attribute", output)
                self.assertFalse(path.exists())
                self.assertFalse(Path(str(path) + ".sha256").exists())

    def test_identical_positive_and_every_added_removed_changed_domain(self):
        before = self.captured()
        self.assertEqual([], TOOL.compare(before, copy.deepcopy(before), "identical"))
        for domain in ("tags", "releases", "packages", "images"):
            for operation in ("added", "removed", "changed"):
                with self.subTest(domain=domain, operation=operation):
                    after = copy.deepcopy(before)
                    key = next(iter(after[domain]))
                    if operation == "added":
                        after[domain]["extra-member"] = {}
                    elif operation == "removed":
                        del after[domain][key]
                    else:
                        after[domain][key] = "changed"
                    try:
                        delta = TOOL.compare(before, after, "identical")
                        self.assertTrue(delta)
                        self.assertIn(domain, delta[0])
                    except ValueError as error:
                        self.assertIn("identity/latest" if domain == "releases" and operation == "removed" else domain, str(error))

    def test_additive_positive_with_latest_unchanged_or_moved_to_new_release(self):
        before = self.captured()
        for latest in ("1", "3"):
            with self.subTest(latest=latest):
                after = self.additive(before)
                after["latest"] = latest
                self.assertEqual([], TOOL.compare(before, after, "additive", "4.0.1", SOURCE))

    def assert_refused_naming(self, expected, before, after, version="4.0.1"):
        """Require the exact refusal reason or delta member, not merely any failure."""
        try:
            delta = TOOL.compare(before, after, "additive", version, SOURCE)
        except ValueError as error:
            self.assertIn(expected, str(error))
        else:
            self.assertTrue(any(expected in item for item in delta), delta)

    def test_additive_refuses_any_other_delta_or_latest_moved_elsewhere(self):
        before = self.captured()
        package, image = PACKAGES[0], IMAGES[0]
        mutations = {
            "extra-tag": (lambda d: d["tags"].update({"refs/tags/v9.0.0": {"commit": SOURCE, "object": SOURCE}}),
                          "identity/tags/refs/tags/v9.0.0: added"),
            "retag": (lambda d: d["tags"]["refs/tags/v4.0.0"].update(commit=OLD_SOURCE),
                      "identity/tags/refs/tags/v4.0.0/commit: changed"),
            "wrong-source": (lambda d: d["tags"]["refs/tags/v4.0.1"].update(commit=OLD_SOURCE), "missing tag or wrong source"),
            "asset-missing": (lambda d: d["releases"]["3"]["assets"].pop("hexalith-memories-production.yaml"),
                              "expected the 13 nonempty digested assets"),
            "zero-size-asset": (lambda d: d["releases"]["3"]["assets"]["hexalith-memories-production.yaml"].update(size=0),
                                "expected the 13 nonempty digested assets"),
            "extra-release": (lambda d: d["releases"].update({"4": copy.deepcopy(d["releases"]["3"])}), "expected exactly one new release"),
            "draft": (lambda d: d["releases"]["3"].update(draft=True), "wrong tag or publication state"),
            "new-tag-mismatch": (lambda d: d["releases"]["3"].update(tag="v4.0.2"), "wrong tag or publication state"),
            "stable-as-prerelease": (lambda d: d["releases"]["3"].update(prerelease=True), "wrong tag or publication state"),
            "old-asset-size": (lambda d: d["releases"]["1"]["assets"]["historical.yaml"].update(size=11),
                               "identity/releases/1/assets/historical.yaml/size: changed"),
            "old-asset-digest": (lambda d: d["releases"]["1"]["assets"]["historical.yaml"].update(digest="sha256:" + "e" * 64),
                                 "identity/releases/1/assets/historical.yaml/digest: changed"),
            "malformed-old-asset-digest": (lambda d: d["releases"]["1"]["assets"]["historical.yaml"].update(digest=None),
                                           "invalid asset identity"),
            "missing-package": (lambda d: d["packages"][package].pop("4.0.1"), f"identity/packages/{package}/4.0.1: removed"),
            "unlisted": (lambda d: d["packages"][package]["4.0.0"].update(listed=False), f"identity/packages/{package}/4.0.0/listed: changed"),
            "new-version-unlisted": (lambda d: d["packages"][package]["4.0.1"].update(listed=False),
                                     f"identity/packages/{package}/4.0.1/listed: changed"),
            "missing-image": (lambda d: d["images"][image].pop("4.0.1"), "new release tag must be an image index"),
            "old-image-changed": (lambda d: d["images"][image]["4.0.0"].update(digest="sha256:" + "f" * 64),
                                  f"identity/images/{image}/4.0.0/digest: changed"),
            "old-media-type-changed": (lambda d: d["images"][image]["4.0.0"].update(mediaType=TOOL.INDEX_TYPES[1]),
                                       f"identity/images/{image}/4.0.0/mediaType: changed"),
            "new-single-image": (lambda d: d["images"][image]["4.0.1"].update(mediaType=TOOL.MANIFEST_TYPES[0]),
                                 "new release tag must be an image index"),
            "latest-draft": (lambda d: d.update(latest="2"), "Latest must identify a published stable release"),
        }
        for label, (mutate, expected) in mutations.items():
            with self.subTest(label=label):
                after = self.additive(before)
                mutate(after)
                self.assert_refused_naming(expected, before, after)
        with self.assertRaisesRegex(ValueError, "refs/tags/v4.0.0: already existed"):
            TOOL.compare(before, self.additive(before, "4.0.0"), "additive", "4.0.0", SOURCE)

    def test_additive_refuses_latest_moved_to_an_older_stable_release(self):
        before = self.captured()
        before["releases"]["2"].update(draft=False, prerelease=False)
        after = self.additive(before)
        after["latest"] = "2"
        with self.assertRaisesRegex(ValueError, "GitHub Latest: moved elsewhere"):
            TOOL.compare(before, after, "additive", "4.0.1", SOURCE)

    def test_additive_refuses_preexisting_package_version_or_image_tag_without_git_tag(self):
        package, image = PACKAGES[0], IMAGES[0]
        before = self.captured()
        before["packages"][package]["4.0.1"] = {"listed": True}
        with self.assertRaisesRegex(ValueError, f"NuGet {package}: version already existed"):
            TOOL.compare(before, self.additive(before), "additive", "4.0.1", SOURCE)
        before = self.captured()
        before["images"][image]["4.0.1"] = {"digest": "sha256:" + "9" * 64, "mediaType": TOOL.INDEX_TYPES[0]}
        with self.assertRaisesRegex(ValueError, f"registry {image}: tag already existed"):
            TOOL.compare(before, self.additive(before), "additive", "4.0.1", SOURCE)

    def test_additive_prerelease_positive_control_and_argument_refusals(self):
        before = self.captured()
        version = "4.1.0-RC.1"
        after = self.additive(before, version)
        self.assertEqual([], TOOL.compare(before, after, "additive", version, SOURCE))
        stable = copy.deepcopy(after)
        stable["releases"]["3"]["prerelease"] = False
        self.assert_refused_naming("wrong tag or publication state", before, stable, version)
        promoted = copy.deepcopy(after)
        promoted["latest"] = "3"
        self.assert_refused_naming("Latest must identify a published stable release", before, promoted, version)
        refusals = {
            (None, SOURCE): "--version is required (without v)",
            ("v4.0.1", SOURCE): "--version is required (without v)",
            ("4.1.0-rc.01", SOURCE): "numeric prerelease identifier has a leading zero",
            ("4.0.1", None): "--source-sha is required",
            ("4.0.1", SOURCE[:12]): "--source-sha is required",
            ("4.0.1", SOURCE.upper()): "--source-sha is required",
        }
        for (candidate, source), expected in refusals.items():
            with self.subTest(version=candidate, source=source):
                with self.assertRaises(ValueError) as refusal:
                    TOOL.compare(before, self.additive(before), "additive", candidate, source)
                self.assertIn(expected, str(refusal.exception))

    def test_identical_names_every_well_formed_added_removed_changed_member(self):
        before = self.captured()
        package, image = PACKAGES[0], IMAGES[0]
        mutations = {
            "tag-added": (lambda d: d["tags"].update({"refs/tags/v9.9.9": {"object": SOURCE, "commit": SOURCE}}),
                          "identity/tags/refs/tags/v9.9.9: added"),
            "tag-object-changed": (lambda d: d["tags"]["refs/tags/v4.0.0"].update(object=OLD_SOURCE),
                                   "identity/tags/refs/tags/v4.0.0/object: changed"),
            "release-added": (lambda d: d["releases"].update({"5": {"tag": "v5.0.0-preview", "draft": True, "prerelease": True, "assets": {}}}),
                              "identity/releases/5: added"),
            "release-state-changed": (lambda d: d["releases"]["2"].update(draft=False), "identity/releases/2/draft: changed"),
            "asset-digest-changed": (lambda d: d["releases"]["1"]["assets"]["historical.yaml"].update(digest="sha256:" + "e" * 64),
                                     "identity/releases/1/assets/historical.yaml/digest: changed"),
            "package-version-removed": (lambda d: d["packages"][package].pop("3.9.0"), f"identity/packages/{package}/3.9.0: removed"),
            "package-listed-changed": (lambda d: d["packages"][package]["3.9.0"].update(listed=True),
                                       f"identity/packages/{package}/3.9.0/listed: changed"),
            "image-tag-added": (lambda d: d["images"][image].update({"4.0.9": copy.deepcopy(INDEX_IDENTITY)}),
                                f"identity/images/{image}/4.0.9: added"),
            "image-digest-changed": (lambda d: d["images"][image]["4.0.0"].update(digest="sha256:" + "f" * 64),
                                     f"identity/images/{image}/4.0.0/digest: changed"),
        }
        for label, (mutate, expected) in mutations.items():
            with self.subTest(label=label):
                after = copy.deepcopy(before)
                mutate(after)
                delta = TOOL.compare(before, after, "identical")
                self.assertEqual([expected], delta)

    def test_registry_pagination_refuses_off_host_link_before_sending_credentials(self):
        repository = IMAGES[0]
        for link in (f'<https://elsewhere.invalid/v2/{repository}/tags/list?n=100&last=4.0.0>; rel="next"',
                     f'<https://{TOOL.REGISTRY}/v2/other-repository/tags/list?n=100&last=4.0.0>; rel="next"'):
            with self.subTest(link=link):
                self.responders.requests.clear()
                def paged(url, link=link):
                    if "/tags/list?n=100" in url and "last=" not in url:
                        return Response({"name": repository, "tags": ["4.0.0"]}, {"Link": link})
                    if "elsewhere.invalid" in url or "other-repository" in url:
                        raise AssertionError("credentials must not leave the configured repository")
                self.responders.fault = paged
                with self.assertRaisesRegex(ValueError, f"registry {repository}: invalid pagination"):
                    TOOL.images(TOOL.Reader(), repository)
                self.assertFalse([request for request in self.responders.requests
                                  if "elsewhere.invalid" in request.full_url or "other-repository" in request.full_url])

    def test_ambiguous_http_statuses_name_member_and_write_no_snapshot(self):
        for host_path, name in (("/releases?", "GitHub"), ("/v3-flatcontainer/", "NuGet"), ("/registration5-gz-semver2/", "NuGet"), ("/tags/list?", "registry"), ("/manifests/", "registry")):
            for status in (301, 302, 307, 401, 403, 500, 503):
                with self.subTest(member=name, status=status):
                    def fault(url):
                        if host_path in url:
                            raise urllib.error.HTTPError(url, status, "fixture", {}, None)
                    self.responders.fault = fault
                    path = self.directory / "failed.json"
                    code, output = self.invoke("capture", "--output", path)
                    self.assertEqual(1, code, output)
                    self.assertIn(name, output)
                    self.assertIn(str(status), output)
                    self.assertFalse(path.exists())
                    self.assertFalse(Path(str(path) + ".sha256").exists())

    def test_unparseable_and_timeout_replies_refuse_without_evidence(self):
        for host_path in ("/releases?", "/v3-flatcontainer/", "/registration5-gz-semver2/", "/tags/list?", "/manifests/"):
            for bad in (b"invalid", b'{"duplicate":1,"duplicate":2}'):
                with self.subTest(path=host_path, bad=bad):
                    self.responders.fault = lambda url: Response(bad) if host_path in url else None
                    path = self.directory / "failed.json"
                    code, output = self.invoke("capture", "--output", path)
                    self.assertEqual(1, code, output)
                    self.assertIn("unparseable", output)
                    self.assertFalse(path.exists())
        def timeout(url):
            raise TimeoutError("fixture-github-secret fixture-registry-secret")
        self.responders.fault = timeout
        code, output = self.invoke("capture", "--output", self.directory / "failed.json")
        self.assertEqual(1, code)
        self.assertIn("GitHub", output)
        self.assertNotIn("fixture-github-secret", output)
        self.assertNotIn("fixture-registry-secret", output)
        with mock.patch.object(TOOL.subprocess, "run", side_effect=subprocess.TimeoutExpired("git", 60)):
            with self.assertRaisesRegex(ValueError, "Git v\\* tags: command timeout"):
                TOOL.tags()

    def test_registration_external_pages_and_incomplete_listing_refusal(self):
        slug = PACKAGES[0].lower()
        base = f"https://api.nuget.org/v3/registration5-gz-semver2/{slug}/"
        def paged(url):
            if "/v3-flatcontainer/" in url:
                return Response({"versions": ["4.0.0"]})
            if url == base + "index.json":
                return Response({"count": 1, "items": [{"@id": base + "page/1.json", "count": 1, "lower": "4.0.0", "upper": "4.0.0"}]})
            if url == base + "page/1.json":
                return Response({"count": 1, "lower": "4.0.0", "upper": "4.0.0", "items": [{"catalogEntry": {"version": "4.0.0", "listed": True}}]})
        self.responders.fault = paged
        self.assertEqual({"4.0.0": {"listed": True}}, TOOL.package(TOOL.Reader(), PACKAGES[0]))
        self.responders.fault = lambda url: Response({"versions": ["5.0.0"]}) if "/v3-flatcontainer/" in url else None
        with self.assertRaisesRegex(ValueError, "version sets disagree"):
            TOOL.package(TOOL.Reader(), PACKAGES[0])

    def test_registry_pagination_and_bad_digest_or_unknown_format_refusal(self):
        repository = IMAGES[0]
        def paged(url):
            if "/tags/list?n=100" in url and "last=" not in url:
                return Response({"name": repository, "tags": ["4.0.0"]}, {"Link": f'</v2/{repository}/tags/list?n=100&last=4.0.0>; rel="next"'})
            if "last=4.0.0" in url:
                return Response({"name": repository, "tags": ["4.0.1"]})
        self.responders.fault = paged
        self.assertEqual({"4.0.0": INDEX_IDENTITY, "4.0.1": INDEX_IDENTITY}, TOOL.images(TOOL.Reader(), repository))
        self.responders.fault = lambda url: Response(INDEX, {"Docker-Content-Digest": "sha256:" + "0" * 64, "Content-Type": TOOL.INDEX_TYPES[0]}) if "/manifests/" in url else None
        with self.assertRaisesRegex(ValueError, "digest mismatch"):
            TOOL.images(TOOL.Reader(), repository)
        self.responders.fault = lambda url: Response({"schemaVersion": 2, "mediaType": "application/unknown"}) if "/manifests/" in url else None
        with self.assertRaisesRegex(ValueError, "unknown image format"):
            TOOL.images(TOOL.Reader(), repository)

    def test_historical_manifests_and_indexes_preserve_original_bytes_and_media_type(self):
        for media_type in TOOL.MANIFEST_TYPES + TOOL.INDEX_TYPES:
            with self.subTest(mediaType=media_type):
                document = {"schemaVersion": 2, "mediaType": media_type}
                if media_type in TOOL.MANIFEST_TYPES:
                    document.update(config=DESCRIPTOR, layers=[])
                else:
                    document["manifests"] = [DESCRIPTOR]
                raw = (json.dumps(document, indent=4) + "\n").encode()
                digest = "sha256:" + hashlib.sha256(raw).hexdigest()
                self.responders.fault = lambda url: Response(raw, {"Docker-Content-Digest": digest, "Content-Type": media_type}) if "/manifests/" in url else None
                before = self.captured()
                self.assertEqual({"digest": digest, "mediaType": media_type}, before["images"][IMAGES[0]]["4.0.0"])
                self.assertEqual([], TOOL.compare(before, copy.deepcopy(before), "identical"))
                self.assertEqual([], TOOL.compare(before, self.additive(before), "additive", "4.0.1", SOURCE))
                for field, value in (("digest", "sha256:" + "f" * 64), ("mediaType", TOOL.INDEX_TYPES[1] if media_type != TOOL.INDEX_TYPES[1] else TOOL.INDEX_TYPES[0])):
                    after = copy.deepcopy(before)
                    after["images"][IMAGES[0]]["4.0.0"][field] = value
                    self.assertIn("images/" + IMAGES[0] + "/4.0.0/" + field, TOOL.compare(before, after, "identical")[0])
                self.assertTrue(all(set(request.headers["Accept"].split(", ")) == set(TOOL.INDEX_TYPES + TOOL.MANIFEST_TYPES)
                                    for request in self.responders.requests if "/manifests/" in request.full_url))

    def test_ambiguous_manifest_format_or_bytes_refuses_capture_without_evidence(self):
        malformed = [
            (INDEX, {"Content-Type": TOOL.INDEX_TYPES[0], "Docker-Content-Digest": "sha256:" + "0" * 64}, "digest mismatch"),
            (INDEX, {"Docker-Content-Digest": INDEX_DIGEST}, "media type"),
            (INDEX, {"Content-Type": TOOL.MANIFEST_TYPES[0], "Docker-Content-Digest": INDEX_DIGEST}, "media type"),
            (INDEX, {"Content-Type": TOOL.INDEX_TYPES[0], "Docker-Content-Digest": INDEX_DIGEST, "Content-Encoding": "gzip"}, "content encoding"),
            (b'{"schemaVersion":2,"mediaType":"application/unknown"}', {}, "unknown image format"),
            (json.dumps({"schemaVersion": 2, "mediaType": TOOL.MANIFEST_TYPES[0], "config": {}, "layers": []}).encode(),
             {"Content-Type": TOOL.MANIFEST_TYPES[0]}, "invalid image descriptor"),
        ]
        for raw, headers, reason in malformed:
            with self.subTest(reason=reason):
                self.responders.fault = lambda url: Response(raw, headers) if "/manifests/" in url else None
                path = self.directory / "failed.json"
                code, output = self.invoke("capture", "--output", path)
                self.assertEqual(1, code, output)
                self.assertIn("registry " + IMAGES[0] + ":4.0.0", output)
                self.assertIn(reason, output)
                self.assertFalse(path.exists())
                self.assertFalse(Path(str(path) + ".sha256").exists())

    def test_github_pagination_covers_all_releases_and_assets(self):
        reader = TOOL.Reader()
        responses = [([{"id": i} for i in range(100)], {"Link": f'<https://api.github.com/repos/{TOOL.REPOSITORY}/releases?per_page=100&page=2>; rel="next"'}), ([{"id": 100}], {})]
        with mock.patch.object(reader, "github", side_effect=responses) as call:
            self.assertEqual(101, len(reader.github_list("releases")))
            self.assertEqual("releases?per_page=100&page=2", call.call_args.args[0])

    def test_no_redirect_handler_refuses_instead_of_creating_redirect_request(self):
        self.assertIsNone(TOOL.NoRedirect().redirect_request(None, None, 302, "moved", {}, "https://elsewhere.invalid"))

    def test_compare_cli_positive_and_integrity_receipt_refusal(self):
        before = self.captured()
        one, two = self.directory / "one.json", self.directory / "two.json"
        TOOL.write_snapshot(one, before)
        TOOL.write_snapshot(two, before)
        self.assertEqual(0, self.invoke("compare", one, two, "--expect", "identical")[0])
        two = self.directory / "additive.json"
        TOOL.write_snapshot(two, self.additive(before))
        code, output = self.invoke("compare", one, two, "--expect", "additive", "--version", "4.0.1", "--source-sha", SOURCE)
        self.assertEqual(0, code, output)
        two.write_text(two.read_text() + " ")
        code, output = self.invoke("compare", one, two, "--expect", "identical")
        self.assertEqual(1, code)
        self.assertIn("receipt mismatch", output)

    def test_malformed_tags_and_credentials_are_named_and_never_echoed(self):
        with mock.patch.object(TOOL.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "bad ref", "")):
            with self.assertRaisesRegex(ValueError, "Git v\\* tags"):
                TOOL.tags()
        with mock.patch.object(TOOL.subprocess, "run", return_value=subprocess.CompletedProcess([], 1, "fixture-secret", "fixture-secret")):
            code, output = self.invoke("capture", "--output", self.directory / "failed.json")
            self.assertEqual(1, code)
            self.assertIn("credentials", output)
            self.assertNotIn("fixture-secret", output)

    def test_receipt_valid_malformed_nested_snapshots_are_refused(self):
        before = self.captured()
        mutations = [
            ("tags", lambda d: d.update(tags=42)),
            ("releases", lambda d: d.update(releases=[])),
            ("tags", lambda d: d["tags"].update({"refs/tags/v4.0.0": {"object": SOURCE}})),
            ("releases/1", lambda d: d["releases"]["1"].update(assets=[])),
            ("releases/1/assets/historical.yaml", lambda d: d["releases"]["1"]["assets"]["historical.yaml"].update(size=True)),
            ("packages/" + PACKAGES[0], lambda d: d["packages"].update({PACKAGES[0]: {}})),
            ("packages/" + PACKAGES[0], lambda d: d["packages"][PACKAGES[0]]["4.0.0"].update(listed=1)),
            ("images/" + IMAGES[0], lambda d: d["images"].update({IMAGES[0]: {}})),
            ("images/" + IMAGES[0], lambda d: d["images"][IMAGES[0]]["4.0.0"].update(mediaType="unknown")),
        ]
        for index, (member, mutate) in enumerate(mutations):
            with self.subTest(member=member):
                document = copy.deepcopy(before)
                mutate(document)
                path = self.directory / f"malformed-{index}.json"
                TOOL.write_snapshot(path, document)
                code, output = self.invoke("compare", path, path, "--expect", "identical")
                self.assertEqual(1, code, output)
                self.assertIn("identity/" + member, output)

    def test_flat_container_and_registration_full_normalized_sets_must_agree(self):
        reader = TOOL.Reader()
        for versions in ([], ["4.0.0"], ["3.9.0", "4.0.0", "5.0.0"]):
            with self.subTest(versions=versions):
                self.responders.fault = lambda url: Response({"versions": versions}) if "/v3-flatcontainer/" in url else None
                with self.assertRaisesRegex(ValueError, "NuGet .*version sets disagree"):
                    TOOL.package(reader, PACKAGES[0])
        self.responders.fault = lambda url: Response({"versions": ["03.09.0.0+build", "04.0"]}) if "/v3-flatcontainer/" in url else None
        self.assertEqual({"3.9.0": {"listed": False}, "4.0.0": {"listed": True}}, TOOL.package(reader, PACKAGES[0]))

    def test_external_registration_parent_count_and_bounds_are_reconciled(self):
        base = f"https://api.nuget.org/v3/registration5-gz-semver2/{PACKAGES[0].lower()}/"
        parent = {"@id": base + "page.json", "count": 2, "lower": "3.9.0", "upper": "4.0.0"}
        for changed in ({"count": 1}, {"lower": "3.8.0"}, {"upper": "4.1.0"},
                        {"items": [{"catalogEntry": {"version": "4.0.0", "listed": True}}]}):
            with self.subTest(changed=changed):
                page = {"count": 2, "lower": "3.9.0", "upper": "4.0.0", "items": [
                    {"catalogEntry": {"version": "3.9.0", "listed": False}},
                    {"catalogEntry": {"version": "4.0.0", "listed": True}}], **changed}
                def paged(url):
                    if url == base + "index.json":
                        return Response({"count": 1, "items": [parent]})
                    if url == parent["@id"]:
                        return Response(page)
                self.responders.fault = paged
                with self.assertRaisesRegex(ValueError, "NuGet .*registration page"):
                    TOOL.package(TOOL.Reader(), PACKAGES[0])

    def test_github_short_pages_follow_safe_next_links_for_releases_and_assets(self):
        reader = TOOL.Reader()
        for path in ("releases", "releases/1/assets"):
            with self.subTest(path=path):
                link = f'<https://api.github.com/repos/{TOOL.REPOSITORY}/{path}?page=2&per_page=100>; rel="next"'
                with mock.patch.object(reader, "github", side_effect=[([1], {"Link": link}), ([2], {})]) as call:
                    self.assertEqual([1, 2], reader.github_list(path))
                    self.assertEqual(path + "?page=2&per_page=100", call.call_args.args[0])
                for unsafe in ("https://elsewhere.invalid/", f"https://api.github.com/repos/other/repo/{path}?page=2&per_page=100",
                               f"http://api.github.com/repos/{TOOL.REPOSITORY}/{path}?page=2&per_page=100",
                               f"https://api.github.com/repos/{TOOL.REPOSITORY}/{path}?page=1&per_page=100"):
                    with self.subTest(link=unsafe), mock.patch.object(reader, "github", return_value=([1], {"Link": f'<{unsafe}>; rel="next"'})) as call:
                        with self.assertRaisesRegex(ValueError, "GitHub .*pagination|GitHub .*next page"):
                            reader.github_list(path)
                        self.assertEqual(1, call.call_count)

    def test_latest_refuses_draft_prerelease_or_disagreeing_endpoint_state(self):
        for latest in (release(2, "4.1.0-preview", draft=True, prerelease=True),
                       release(2, "4.1.0-preview"), release(draft=True), release(prerelease=True)):
            with self.subTest(latest=latest):
                self.responders.fault = lambda url: Response(latest) if url.endswith("/releases/latest") else None
                with self.assertRaisesRegex(ValueError, "GitHub releases/latest: Latest must match a published stable release"):
                    self.captured()

    def test_github_numeric_links_bind_cached_authenticated_repository_identity(self):
        reader = TOOL.Reader()
        for path in ("releases", "releases/1/assets"):
            with self.subTest(path=path):
                link = f'<https://api.github.com/repositories/{GITHUB_ID}/{path}?per_page=100&page=2>; rel="next"'
                with mock.patch.object(reader, "github", side_effect=[([1], {"Link": link}), ([2], {})]) as call:
                    self.assertEqual([1, 2], reader.github_list(path))
                    self.assertEqual(path + "?per_page=100&page=2", call.call_args.args[0])
                for wrong_path in (f"/repositories/{GITHUB_ID + 1}/{path}", f"/repositories/{GITHUB_ID}/issues"):
                    with self.subTest(wrong_path=wrong_path), mock.patch.object(reader, "github", return_value=(
                            [1], {"Link": f'<https://api.github.com{wrong_path}?per_page=100&page=2>; rel="next"'})) as call:
                        with self.assertRaisesRegex(ValueError, "GitHub .*unsafe pagination Link"):
                            reader.github_list(path)
                        self.assertEqual(1, call.call_count)
        metadata = [request for request in self.responders.requests
                    if request.full_url == f"https://api.github.com/repos/{TOOL.REPOSITORY}"]
        self.assertEqual(1, len(metadata))
        self.assertEqual("GET", metadata[0].get_method())
        self.assertEqual("Bearer fixture-github-secret", metadata[0].headers["Authorization"])

    def test_github_repository_metadata_must_bind_the_named_repository(self):
        for metadata in ({"id": GITHUB_ID, "full_name": "other/repository"},
                         {"id": str(GITHUB_ID), "full_name": TOOL.REPOSITORY},
                         {"id": True, "full_name": TOOL.REPOSITORY}, {}):
            with self.subTest(metadata=metadata):
                self.responders.fault = lambda url: Response(metadata) if url == f"https://api.github.com/repos/{TOOL.REPOSITORY}" else None
                with self.assertRaisesRegex(ValueError, "GitHub repository metadata: repository identity mismatch"):
                    TOOL.Reader().github_list("releases")

    def test_descriptor_required_shapes_and_unknown_child_media_types(self):
        for media_type in TOOL.MANIFEST_TYPES + TOOL.INDEX_TYPES:
            for field, invalid in (("mediaType", None), ("mediaType", "invalid"), ("size", None), ("size", True), ("size", -1), ("digest", None), ("digest", "invalid")):
                with self.subTest(top=media_type, field=field, invalid=invalid):
                    descriptor = {**DESCRIPTOR, field: invalid}
                    document = {"schemaVersion": 2, "mediaType": media_type}
                    if media_type in TOOL.INDEX_TYPES:
                        document["manifests"] = [descriptor]
                    else:
                        document.update(config=descriptor, layers=[])
                    raw = json.dumps(document).encode()
                    self.responders.fault = lambda url: Response(raw, {"Content-Type": media_type, "Docker-Content-Digest": "sha256:" + hashlib.sha256(raw).hexdigest()}) if "/manifests/" in url else None
                    with self.assertRaisesRegex(ValueError, "registry .*invalid image descriptor"):
                        TOOL.images(TOOL.Reader(), IMAGES[0])
        # DESCRIPTOR's valid but unknown child media type is accepted in every
        # positive manifest/index control; only top-level media types are frozen.

    def test_json_encoding_support_is_explicit_and_names_the_member(self):
        reader = TOOL.Reader()
        for encoding, raw in (("identity", b'{"ok":true}'), ("gzip", gzip.compress(b'{"ok":true}'))):
            self.responders.fault = lambda url: Response(raw, {"Content-Encoding": encoding})
            self.assertEqual({"ok": True}, reader.json("https://fixture.invalid/", "fixture member")[0])
        for encoding in ("br", "deflate", "gzip, br"):
            self.responders.fault = lambda url: Response(b'{"ok":true}', {"Content-Encoding": encoding})
            with self.assertRaisesRegex(ValueError, "fixture member: unsupported Content-Encoding"):
                reader.json("https://fixture.invalid/", "fixture member")

    def test_capture_collision_preserves_other_writer_and_incomplete_pairs(self):
        before = self.captured()
        for suffix in ("", ".sha256"):
            with self.subTest(collision=suffix):
                path = self.directory / ("receipt-collision.json" if suffix else "output-collision.json")
                other = Path(str(path) + suffix)
                def capture_with_writer():
                    other.write_bytes(b"other writer evidence\r\n")
                    return before
                with mock.patch.object(TOOL, "capture", side_effect=capture_with_writer):
                    code, output = self.invoke("capture", "--output", path)
                self.assertEqual(1, code, output)
                self.assertIn("refuse to overwrite", output)
                self.assertEqual(b"other writer evidence\r\n", other.read_bytes())
                if suffix:
                    self.assertEqual(b"", path.read_bytes())
                else:
                    self.assertFalse(Path(str(path) + ".sha256").exists())

    def test_real_reader_opener_refuses_redirect_without_requesting_destination(self):
        self.open_patch.stop()
        requested = []
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                requested.append(self.path)
                self.send_response(302 if self.path == "/redirect" else 200)
                self.send_header("Location", "/destination")
                self.send_header("Content-Length", "0")
                self.end_headers()

            def log_message(self, *args):
                pass
        server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        thread = threading.Thread(target=server.serve_forever, kwargs={"poll_interval": 0.01})
        thread.start()
        try:
            reader = TOOL.Reader()
            with self.assertRaisesRegex(ValueError, "loopback redirect: HTTP 302"):
                reader.get(f"http://127.0.0.1:{server.server_port}/redirect", "loopback redirect")
            self.assertEqual(["/redirect"], requested)
        finally:
            server.shutdown()
            server.server_close()
            thread.join()

    def test_identical_cli_refuses_latest_only_change_between_valid_snapshots(self):
        before = self.captured()
        before["releases"]["2"].update(draft=False, prerelease=False)
        after = copy.deepcopy(before)
        after["latest"] = "2"
        one, two = self.directory / "latest-before.json", self.directory / "latest-after.json"
        TOOL.write_snapshot(one, before)
        TOOL.write_snapshot(two, after)
        code, output = self.invoke("compare", one, two, "--expect", "identical")
        self.assertEqual(1, code, output)
        self.assertIn("identity/latest: changed", output)


class EvidenceCheckoutTests(unittest.TestCase):
    def test_git_index_checkout_preserves_every_story_evidence_byte(self):
        evidence = Path("_bmad-output/implementation-artifacts/30-6-cutover")
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            repository, checkout = directory / "repository", directory / "checkout"
            repository.mkdir()
            checkout.mkdir()
            (repository / ".gitattributes").write_bytes((ROOT / ".gitattributes").read_bytes())
            shutil.copytree(ROOT / evidence, repository / evidence)
            env = {**os.environ, "GIT_INDEX_FILE": str(directory / "fixture.index"),
                   "GIT_CONFIG_GLOBAL": os.devnull, "GIT_CONFIG_SYSTEM": os.devnull}
            def git(*arguments):
                subprocess.run(["git", "-c", "core.autocrlf=true", "-c", "core.eol=crlf", *arguments],
                               cwd=repository, env=env, check=True, capture_output=True)
            git("init", "--quiet")
            git("add", "--", ".gitattributes", str(evidence))
            git("checkout-index", "--all", "--prefix=" + str(checkout) + os.sep)
            originals = [path for path in (ROOT / evidence).rglob("*") if path.is_file()]
            self.assertTrue(originals)
            for original in originals:
                with self.subTest(path=original.name):
                    self.assertEqual(original.read_bytes(), (checkout / original.relative_to(ROOT)).read_bytes())
            for snapshot in (checkout / evidence).glob("identity-*.json"):
                if Path(str(snapshot) + ".sha256").exists():
                    TOOL.read_snapshot(snapshot)


if __name__ == "__main__":
    unittest.main()
