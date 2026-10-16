"""Execute only an outside-approved, owner-bound metadata discovery plan.

This collector creates observations, never qualification passes or approval.
It does not use the owner predecessor APIs or change their refusal policy.
PyYAML (already installed for repository tooling) is used only for safe local
kubeconfig decoding; exec/auth-provider, external credentials and proxies refuse.
"""

from __future__ import annotations

import argparse
import base64
from contextlib import ExitStack
from dataclasses import dataclass
from datetime import datetime, timezone
import fcntl
import hashlib
import json
import os
import re
import selectors
import signal
import stat
import subprocess
import time
import uuid


SCHEMA = "hexalith.access-telemetry.owner-discovery-"
OWNER = "github:user:6775094"
ASSURANCE = "sole-owner/self-reviewed"
POLICY = "sole-owner-qualification/2026-10-12"
PROFILE = "PG-ONPREM-2"
PROFILE_SHA256 = "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"
WORKLOAD_SHA256 = "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f"
ACTION = "bounded-read-only-discovery"
MAX_BYTES = 1048576
KUBECTL = "/usr/local/bin/kubectl"
NOT_GRANTED = ["pod-exec", "logs", "secret-read", "mutation", "qualification-gates",
               "fault", "purge", "reclaim", "publish", "production-activation"]
_HEX = re.compile(r"[0-9a-f]{64}\Z")
_UTC = re.compile(r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ\Z")
_ID = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,127}\Z")
_NAMESPACE = '{.metadata.name}{"\\t"}{.metadata.uid}{"\\t"}{.metadata.labels.environment}{"\\t"}{.metadata.labels.hexalith\\.io/environment}{"\\n"}'
_DEPLOYMENT = '{.metadata.name}{"\\t"}{.metadata.uid}{"\\t"}{.spec.replicas}{"\\t"}{.status.readyReplicas}{"\\t"}{range .spec.template.spec.containers[*]}{.name}{"="}{.image}{";"}{end}{"\\n"}'
_PODS = '{range .items[*]}{.metadata.name}{"\\t"}{.metadata.uid}{"\\t"}{.status.phase}{"\\t"}{range .status.containerStatuses[*]}{.name}{"="}{.imageID}{";"}{end}{"\\n"}{end}'
_PLAN_FIELDS = set(("schemaVersion planId status ownerPrincipal assuranceMode policyId policySha256 "
                    "profileId profileSha256 workloadSha256 "
                    "sourceCommit repositoryRoot evidenceRoot credentialPath credentialMetadata "
                    "context server caPemSha256 targetClassification notBeforeUtc expiresAtUtc "
                    "maximumDurationSeconds maximumCommands perCommandTimeoutSeconds maximumStreamBytes "
                    "allowedAction queries authorization notGranted runner").split())
_DECISION_FIELDS = set(("schemaVersion planId planSha256 runnerSha256 ownerPrincipal assuranceMode "
                        "decision allowedAction approvedAtUtc expiresAtUtc").split())
_STATUS_FIELDS = set("schemaVersion planId planSha256 decisionSha256 withdrawn".split())


class DiscoveryError(ValueError):
    """Bounded content-free refusal; retained observations confer no gate credit."""


def _deny(code):
    raise DiscoveryError("owner-discovery-" + code) from None


def _sha(raw):
    return hashlib.sha256(raw).hexdigest()


def _path(value):
    if (type(value) is not str or len(value) > 4096 or not value.startswith("/")
            or value == "/" or "\\" in value or "\x00" in value
            or any(p in ("", ".", "..") for p in value[1:].split("/"))):
        _deny("path-invalid")
    return value


def _digest(value):
    if type(value) is not str or _HEX.fullmatch(value) is None:
        _deny("digest-invalid")


def _instant(value):
    if type(value) is not str or _UTC.fullmatch(value) is None:
        _deny("time-invalid")
    return datetime.strptime(value, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)


def _now():
    return datetime.now(timezone.utc)


def _stamp(value):
    return value.isoformat(timespec="microseconds").replace("+00:00", "Z")


def _pairs(items):
    result = {}
    for key, value in items:
        if key in result:
            _deny("json-duplicate-key")
        result[key] = value
    return result


def _json(raw):
    if type(raw) is not bytes or len(raw) > MAX_BYTES:
        _deny("artifact-bound")
    return json.loads(raw.decode("utf-8"), object_pairs_hook=_pairs,
                      parse_constant=lambda _: _deny("json-nonfinite"))


def _closed(value, fields):
    if type(value) is not dict or set(value) != fields:
        _deny("schema-invalid")
    return value


def _metadata(info):
    return {"device": info.st_dev, "inode": info.st_ino, "uid": info.st_uid,
            "gid": info.st_gid, "mode": stat.S_IMODE(info.st_mode),
            "nlink": info.st_nlink, "size": info.st_size,
            "mtimeNs": info.st_mtime_ns, "ctimeNs": info.st_ctime_ns}


def _directory(info):
    return info.st_dev, info.st_ino, info.st_mode, info.st_uid, info.st_gid


class _File:
    """A held no-follow file and ancestor chain with exact bounded readback."""

    def __init__(self, path, stack, *, private_root=None):
        self.path = _path(path)
        self.uid = os.geteuid()
        self.parts = path[1:].split("/")
        self.private_depth = (len(_path(private_root)[1:].split("/"))
                              if private_root is not None else None)
        if private_root is not None and not path.startswith(private_root + "/"):
            _deny("outside-private-root")
        self.ancestors = []
        parent = os.open("/", os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        stack.callback(os.close, parent)
        self.ancestors.append((parent, os.fstat(parent)))
        for depth, part in enumerate(self.parts[:-1], 1):
            parent = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW,
                             dir_fd=parent)
            stack.callback(os.close, parent)
            info = os.fstat(parent)
            if self.private_depth is not None and depth >= self.private_depth:
                self._private(info)
            self.ancestors.append((parent, info))
        self.fd = os.open(self.parts[-1], os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK,
                          dir_fd=parent)
        stack.callback(os.close, self.fd)
        self.before = os.fstat(self.fd)
        if not stat.S_ISREG(self.before.st_mode) or self.before.st_nlink != 1:
            _deny("regular-single-link-required")
        if self.private_depth is not None:
            self._private(self.before)
        if self.before.st_size > MAX_BYTES:
            _deny("artifact-bound")
        self.raw = self.read()

    def _private(self, info):
        if info.st_uid != self.uid or info.st_mode & 0o077:
            _deny("not-private")

    def validate(self):
        if os.geteuid() != self.uid:
            _deny("effective-user-drift")
        named = os.stat(self.parts[-1], dir_fd=self.ancestors[-1][0],
                        follow_symlinks=False)
        if (_metadata(named) != _metadata(self.before)
                or _metadata(os.fstat(self.fd)) != _metadata(self.before)
                or named.st_mode != self.before.st_mode):
            _deny("file-drift")
        for depth in range(len(self.ancestors) - 1, 0, -1):
            descriptor, before = self.ancestors[depth]
            named = os.stat(self.parts[depth - 1], dir_fd=self.ancestors[depth - 1][0],
                            follow_symlinks=False)
            current = os.fstat(descriptor)
            if _directory(named) != _directory(before) or _directory(current) != _directory(before):
                _deny("ancestor-drift")
            if self.private_depth is not None and depth >= self.private_depth:
                if named.st_ctime_ns != before.st_ctime_ns or current.st_ctime_ns != before.st_ctime_ns:
                    _deny("private-directory-drift")
        if _directory(os.stat("/", follow_symlinks=False)) != _directory(self.ancestors[0][1]):
            _deny("ancestor-drift")

    def read(self):
        self.validate()
        os.lseek(self.fd, 0, os.SEEK_SET)
        chunks, remaining = [], MAX_BYTES + 1
        while remaining:
            chunk = os.read(self.fd, remaining)
            if not chunk:
                break
            chunks.append(chunk)
            remaining -= len(chunk)
        raw = b"".join(chunks)
        if len(raw) != self.before.st_size or len(raw) > MAX_BYTES:
            _deny("file-drift")
        self.validate()
        return raw

    def recheck(self):
        if self.read() != self.raw:
            _deny("bytes-drift")


def discovery_queries(credential_path, context):
    """Return exactly the twelve prepared fixed metadata projections, in order."""
    prefix = ["kubectl", "--kubeconfig", credential_path, "--context", context,
              "--request-timeout=10s"]
    definitions = [
        ("cluster-correlation", ["get", "namespace", "kube-system"], _NAMESPACE),
        ("existing-namespace", ["get", "namespace", "hexalith-memories"], _NAMESPACE),
        ("qualification-namespace", ["get", "namespace", "hexalith-memories-qualification"], _NAMESPACE),
    ]
    for name in ("memories-access-telemetry", "memories-access-telemetry-clock", "memories"):
        definitions.append(("existing-" + name,
                            ["-n", "hexalith-memories", "get", "deployment", name], _DEPLOYMENT))
    definitions.extend([
        ("existing-lifecycle-pods", ["-n", "hexalith-memories", "get", "pods", "-l",
                                    "app.kubernetes.io/name=memories-access-telemetry"], _PODS),
        ("qualification-deployments", ["-n", "hexalith-memories-qualification", "get", "deployments",
                                        "memories-access-telemetry", "memories-access-telemetry-clock", "memories"],
         "{range .items[*]}" + _DEPLOYMENT + "{end}"),
        ("qualification-pods", ["-n", "hexalith-memories-qualification", "get", "pods", "-l",
                                "app.kubernetes.io/part-of=hexalith-memories"], _PODS),
        ("qualification-gate", ["-n", "hexalith-memories-qualification", "get", "configmap",
                                "access-telemetry-qualification-gate"],
         '{.metadata.uid}{"\\t"}{.data.gate\\.json}{"\\n"}'),
        ("qualification-lease", ["-n", "hexalith-memories-qualification", "get", "lease",
                                 "access-telemetry-qualification"],
         '{.metadata.uid}{"\\t"}{.spec.holderIdentity}{"\\t"}{.spec.leaseDurationSeconds}{"\\n"}'),
        ("shared-dapr-pods", ["-n", "dapr-system", "get", "pods"], _PODS),
    ])
    return [{"id": name, "argv": prefix + args + ["-o=jsonpath=" + projection]}
            for name, args, projection in definitions]


def _plan(raw, outside_digest):
    _digest(outside_digest)
    if _sha(raw) != outside_digest:
        _deny("plan-digest-mismatch")
    plan = _closed(_json(raw), _PLAN_FIELDS)
    required = {"schemaVersion": SCHEMA + "plan/v1", "status": "proposed",
                "ownerPrincipal": OWNER, "assuranceMode": ASSURANCE, "policyId": POLICY,
                "profileId": PROFILE, "profileSha256": PROFILE_SHA256,
                "workloadSha256": WORKLOAD_SHA256,
                "targetClassification": "unknown", "allowedAction": ACTION,
                "maximumDurationSeconds": 300, "maximumCommands": 12,
                "perCommandTimeoutSeconds": 20, "maximumStreamBytes": MAX_BYTES,
                "notGranted": NOT_GRANTED}
    for key, value in required.items():
        if type(plan[key]) is not type(value) or plan[key] != value:
            _deny("plan-scope-invalid")
    if type(plan["planId"]) is not str or _ID.fullmatch(plan["planId"]) is None:
        _deny("plan-id-invalid")
    if type(plan["sourceCommit"]) is not str or re.fullmatch("[0-9a-f]{40}", plan["sourceCommit"]) is None:
        _deny("source-commit-invalid")
    for key in ("policySha256", "caPemSha256"):
        _digest(plan[key])
    for key in ("repositoryRoot", "evidenceRoot", "credentialPath"):
        _path(plan[key])
    if type(plan["context"]) is not str or _ID.fullmatch(plan["context"].replace("@", ".")) is None:
        _deny("context-invalid")
    if (type(plan["server"]) is not str
            or re.fullmatch(r"https://[A-Za-z0-9.:-]+(?::[0-9]+)?", plan["server"]) is None):
        _deny("server-invalid")
    start, end = _instant(plan["notBeforeUtc"]), _instant(plan["expiresAtUtc"])
    if not 0 < (end - start).total_seconds() <= 86400:
        _deny("window-invalid")
    if plan["queries"] != discovery_queries(plan["credentialPath"], plan["context"]):
        _deny("query-not-allowed")
    auth = _closed(plan["authorization"], {"approvalPath", "statusPath", "approvedPlanSha256"})
    if auth["approvedPlanSha256"] != "outside-owner-decision-required":
        _deny("outside-approval-required")
    runner = _closed(plan["runner"], {"path", "sha256"})
    _path(runner["path"])
    _digest(runner["sha256"])
    for key in ("approvalPath", "statusPath"):
        if not _path(auth[key]).startswith(plan["evidenceRoot"] + "/"):
            _deny("authority-outside-custody")
    metadata = plan["credentialMetadata"]
    fields = {"device", "inode", "uid", "gid", "mode", "nlink", "size", "mtimeNs", "ctimeNs"}
    _closed(metadata, fields)
    if any(type(x) is not int or x < 0 for x in metadata.values()):
        _deny("credential-metadata-invalid")
    return plan


def _credential(raw, plan):
    # Decode locally, without invoking kubectl, plugins or credential helpers.
    import yaml

    class UniqueLoader(yaml.SafeLoader):
        pass

    def mapping(loader, node):
        if any(key.tag != "tag:yaml.org,2002:str" for key, _ in node.value):
            _deny("credential-invalid")
        return _pairs([(loader.construct_object(k), loader.construct_object(v)) for k, v in node.value])

    UniqueLoader.add_constructor("tag:yaml.org,2002:map", mapping)
    # Disallow aliases/anchors/tags before construction, and bound node counts.
    tokens = list(yaml.scan(raw))
    if len(tokens) > 8192 or any(isinstance(t, (yaml.tokens.AliasToken, yaml.tokens.AnchorToken,
                                               yaml.tokens.TagToken)) for t in tokens):
        _deny("credential-invalid")
    config = yaml.load(raw, Loader=UniqueLoader)
    _closed(config, {"apiVersion", "kind", "clusters", "contexts", "current-context", "users"})
    if config["apiVersion"] != "v1" or config["kind"] != "Config":
        _deny("credential-invalid")

    def selected(entries, name, field):
        if type(entries) is not list or not 0 < len(entries) <= 32:
            _deny("credential-invalid")
        names = []
        for entry in entries:
            _closed(entry, {"name", field})
            if type(entry["name"]) is not str or entry["name"] in names:
                _deny("credential-invalid")
            names.append(entry["name"])
        matches = [e[field] for e in entries if e["name"] == name]
        if len(matches) != 1:
            _deny("credential-identity-mismatch")
        return matches[0]

    if config["current-context"] != plan["context"]:
        _deny("credential-context-drift")
    context = selected(config["contexts"], plan["context"], "context")
    if type(context) is not dict or set(context) not in ({"cluster", "user"}, {"cluster", "user", "namespace"}):
        _deny("credential-invalid")
    if any(type(v) is not str or not v for v in context.values()):
        _deny("credential-invalid")
    cluster = selected(config["clusters"], context["cluster"], "cluster")
    _closed(cluster, {"server", "certificate-authority-data"})
    if cluster["server"] != plan["server"]:
        _deny("credential-api-drift")
    ca = base64.b64decode(cluster["certificate-authority-data"], validate=True)
    if _sha(ca) != plan["caPemSha256"]:
        _deny("credential-ca-drift")
    user = selected(config["users"], context["user"], "user")
    _closed(user, {"client-certificate-data", "client-key-data"})
    secrets = []
    for value in user.values():
        if type(value) is not str or not value:
            _deny("credential-invalid")
        decoded = base64.b64decode(value, validate=True)
        if not decoded:
            _deny("credential-invalid")
        secrets.extend((value.encode(), decoded))
    return tuple(secrets)


def _decision(raw, plan, digest, now):
    decision = _closed(_json(raw), _DECISION_FIELDS)
    expected = {"schemaVersion": SCHEMA + "decision/v1", "planId": plan["planId"],
                "planSha256": digest, "runnerSha256": plan["runner"]["sha256"],
                "ownerPrincipal": OWNER, "assuranceMode": ASSURANCE, "decision": "approved",
                "allowedAction": ACTION, "expiresAtUtc": plan["expiresAtUtc"]}
    if any(type(decision[k]) is not str or decision[k] != v for k, v in expected.items()):
        _deny("decision-binding-mismatch")
    approved = _instant(decision["approvedAtUtc"])
    if not _instant(plan["notBeforeUtc"]) <= approved <= now < _instant(decision["expiresAtUtc"]):
        _deny("decision-time-invalid")


def _status(raw, plan, digest, decision_digest):
    status = _closed(_json(raw), _STATUS_FIELDS)
    expected = {"schemaVersion": SCHEMA + "status/v1", "planId": plan["planId"],
                "planSha256": digest, "decisionSha256": decision_digest}
    if any(type(status[k]) is not str or status[k] != v for k, v in expected.items()):
        _deny("status-binding-mismatch")
    if type(status["withdrawn"]) is not bool or status["withdrawn"]:
        _deny("withdrawn-or-unknown")


def _kill(process):
    try:
        os.killpg(process.pid, signal.SIGKILL)
    except ProcessLookupError:
        pass
    finally:
        try:
            process.wait(timeout=2)
        finally:
            for stream in (process.stdout, process.stderr):
                if stream is not None:
                    stream.close()


def _sealed_credential(raw, stack):
    """Pin validated bytes anonymously; the client never reopens the mutable input."""
    descriptor = os.memfd_create("owner-discovery-credential", os.MFD_ALLOW_SEALING | os.MFD_CLOEXEC)
    stack.callback(os.close, descriptor)
    os.fchmod(descriptor, 0o600)
    view = memoryview(raw)
    while view:
        written = os.write(descriptor, view)
        if written <= 0:
            _deny("credential-pinning-unavailable")
        view = view[written:]
    os.lseek(descriptor, 0, os.SEEK_SET)
    seals = fcntl.F_SEAL_WRITE | fcntl.F_SEAL_GROW | fcntl.F_SEAL_SHRINK | fcntl.F_SEAL_SEAL
    fcntl.fcntl(descriptor, fcntl.F_ADD_SEALS, seals)
    if fcntl.fcntl(descriptor, fcntl.F_GET_SEALS) != seals:
        _deny("credential-pinning-unavailable")
    return descriptor


def _execute(argv, deadline, secrets, credential_fd):
    """Bound both streams while retaining partial output and always closing children."""
    stdout, stderr = bytearray(), bytearray()
    reason, code = None, None
    process = None
    try:
        process = subprocess.Popen([KUBECTL, *argv[1:]], stdin=subprocess.DEVNULL,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                   cwd="/", env={"PATH": "/usr/local/bin:/usr/bin:/bin", "HOME": "/nonexistent",
                                                 "LANG": "C.UTF-8", "KUBECTL_KUBERC": "false"},
                                   start_new_session=True, close_fds=True, pass_fds=(credential_fd,))
        with selectors.DefaultSelector() as poller:
            for stream, buffer in ((process.stdout, stdout), (process.stderr, stderr)):
                os.set_blocking(stream.fileno(), False)
                poller.register(stream, selectors.EVENT_READ, buffer)
            while poller.get_map():
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    reason = "deadline-exceeded"
                    break
                for key, _ in poller.select(min(remaining, 0.1)):
                    chunk = os.read(key.fileobj.fileno(), min(65536, MAX_BYTES - len(key.data) + 1))
                    if not chunk:
                        poller.unregister(key.fileobj)
                        continue
                    available = MAX_BYTES - len(key.data)
                    key.data.extend(chunk[:available])
                    if len(chunk) > available:
                        reason = "stream-bound-exceeded"
                        break
                if reason:
                    break
            if reason is None:
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    reason = "deadline-exceeded"
                else:
                    try:
                        code = process.wait(timeout=remaining)
                    except subprocess.TimeoutExpired:
                        reason = "deadline-exceeded"
    except OSError:
        reason = "process-unavailable"
    except (KeyboardInterrupt, SystemExit):
        reason = "interrupted"
    finally:
        if process is not None:
            _kill(process)
            if code is None:
                code = process.returncode
    output, error = bytes(stdout), bytes(stderr)
    if any(secret in stream for secret in secrets for stream in (output, error)):
        # Raw projection bytes are retained unless they contain actual credential
        # material. Never preserve that material even in a failure packet.
        return b"", b"", code, "credential-output-refused"
    return output, error, code, reason


class _Evidence:
    """Exclusive private result directory, descriptor-relative files and readback."""

    def __init__(self, root, stack):
        # A sentinel is opened through a held existing plan instead of trusting
        # path-based makedirs. The parent directory is already validated private.
        self.name = "discovery-" + uuid.uuid4().hex
        self.path = root.path.rsplit("/", 1)[0] + "/" + self.name
        self.parent = root.ancestors[-1][0]
        root.validate()
        os.mkdir(self.name, 0o700, dir_fd=self.parent)
        self.fd = os.open(self.name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW,
                          dir_fd=self.parent)
        stack.callback(os.close, self.fd)
        self.identity = _directory(os.fstat(self.fd))
        self.anchor = root
        root.ancestors[-1] = (self.parent, os.fstat(self.parent))
        self.files = []

    def validate(self):
        self.anchor.recheck()
        named = os.stat(self.name, dir_fd=self.parent, follow_symlinks=False)
        if _directory(named) != self.identity or _directory(os.fstat(self.fd)) != self.identity:
            _deny("evidence-directory-drift")

    def write(self, name, raw):
        self.validate()
        if len(raw) > MAX_BYTES:
            _deny("artifact-bound")
        fd = os.open(name, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600,
                     dir_fd=self.fd)
        try:
            os.fchmod(fd, 0o600)
            view = memoryview(raw)
            while view:
                count = os.write(fd, view)
                if count <= 0:
                    _deny("evidence-write-failed")
                view = view[count:]
            os.fsync(fd)
        finally:
            os.close(fd)
        self.files.append((name, raw))
        self.readback()
        return {"path": name, "sha256": _sha(raw), "byteLength": len(raw)}

    def readback(self):
        self.validate()
        for name, raw in self.files:
            fd = os.open(name, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=self.fd)
            try:
                info = os.fstat(fd)
                if (not stat.S_ISREG(info.st_mode) or info.st_nlink != 1 or info.st_uid != os.geteuid()
                        or info.st_mode & 0o077 or info.st_size != len(raw)):
                    _deny("evidence-file-drift")
                if os.read(fd, MAX_BYTES + 1) != raw:
                    _deny("evidence-bytes-drift")
                named = os.stat(name, dir_fd=self.fd, follow_symlinks=False)
                if _metadata(named) != _metadata(info) or _metadata(os.fstat(fd)) != _metadata(info):
                    _deny("evidence-file-drift")
            finally:
                os.close(fd)
        os.fsync(self.fd)
        self.validate()


@dataclass(frozen=True)
class DiscoveryResult:
    """Receipt identity only; every observation requires owner disposition."""

    receipt_path: str
    receipt_sha256: str
    command_count: int
    disposition: str = "requires-owner-disposition"


def _encode(value):
    return (json.dumps(value, ensure_ascii=True, indent=2) + "\n").encode()


def collect_owner_discovery(plan_path: str, approved_plan_sha256: str) -> DiscoveryResult:
    """Collect the exact approved metadata set; refuse absent or drifting authority.

    The digest must come from the actual owner's decision outside candidate
    files. A matching local record cannot independently authenticate that owner.
    No approval writer or permissive alternate execution mode exists.
    """
    refusal = None
    try:
        return _collect(plan_path, approved_plan_sha256)
    except DiscoveryError as error:
        refusal = str(error)
    except Exception:
        refusal = "owner-discovery-input-or-runtime-unavailable"
    # Normalize after handlers/descriptor/child cleanup: never expose parser,
    # credential, path or subprocess exception text through cause/context.
    raise DiscoveryError(refusal) from None


def _collect(plan_path, approved_digest):
    _path(plan_path)
    _digest(approved_digest)
    began, monotonic_start = _now(), time.monotonic()
    with ExitStack() as stack:
        # The plan itself is a private file; its exact approved root is checked
        # before any other candidate-specified input is followed.
        plan_root = plan_path.rsplit("/", 1)[0]
        held_plan = _File(plan_path, stack, private_root=plan_root)
        plan = _plan(held_plan.raw, approved_digest)
        if plan["evidenceRoot"] != plan_root:
            _deny("plan-root-mismatch")
        now = _now()
        if not _instant(plan["notBeforeUtc"]) <= now < _instant(plan["expiresAtUtc"]):
            _deny("approval-window-inactive")
        auth = plan["authorization"]
        try:
            held_decision = _File(auth["approvalPath"], stack, private_root=plan_root)
        except FileNotFoundError:
            _deny("owner-decision-required")
        _decision(held_decision.raw, plan, approved_digest, now)
        # Acquire actual runner and canonical credentials only after actual
        # approval, and before process or target dependencies are reachable.
        if plan["runner"]["path"] != os.path.abspath(__file__):
            _deny("runner-path-mismatch")
        runner = _File(plan["runner"]["path"], stack)
        if _sha(runner.raw) != plan["runner"]["sha256"]:
            _deny("runner-bytes-mismatch")
        credential = _File(plan["credentialPath"], stack)
        credential._private(credential.before)
        if _metadata(credential.before) != plan["credentialMetadata"]:
            _deny("credential-metadata-drift")
        secrets = _credential(credential.raw, plan)
        identities = {(f.before.st_dev, f.before.st_ino) for f in (held_plan, held_decision, runner, credential)}
        if len(identities) != 4:
            _deny("distinct-inputs-required")

        def check():
            held_plan.recheck()
            held_decision.recheck()
            runner.recheck()
            credential.recheck()
            current = _now()
            _decision(held_decision.raw, plan, approved_digest, current)
            if not _instant(plan["notBeforeUtc"]) <= current < _instant(plan["expiresAtUtc"]):
                _deny("approval-window-inactive")
            if time.monotonic() - monotonic_start >= 300:
                _deny("aggregate-deadline-exceeded")
            # Status is freshly opened each time. An atomic withdrawal update
            # is observed; no previously false value can authorize another get.
            with ExitStack() as status_stack:
                try:
                    status = _File(auth["statusPath"], status_stack, private_root=plan_root)
                except FileNotFoundError:
                    _deny("withdrawal-state-required")
                if (status.before.st_dev, status.before.st_ino) in identities:
                    _deny("distinct-inputs-required")
                _status(status.raw, plan, approved_digest, _sha(held_decision.raw))
                status.recheck()
            return current

        check()
        credential_fd = _sealed_credential(credential.raw, stack)
        # Creating output changes private directory ctime. Keep evidence in its
        # own existing private discovery directory, away from control ancestors.
        with ExitStack() as output_stack:
            output_anchor = _File(plan_root + "/discovery/.anchor", output_stack, private_root=plan_root)
            evidence = _Evidence(output_anchor, output_stack)
            # Own creation intentionally changes discovery's ctime; no control
            # ancestor changes. The anchor is no longer used after mkdir.
            commands, failure = [], None
            try:
                for index, query in enumerate(plan["queries"]):
                    started = check()
                    deadline = min(monotonic_start + 300, time.monotonic() + 20,
                                   time.monotonic() + (_instant(plan["expiresAtUtc"]) - started).total_seconds())
                    executed_argv = [KUBECTL, *query["argv"][1:]]
                    executed_argv[2] = f"/proc/self/fd/{credential_fd}"
                    out, err, code, reason = _execute(executed_argv, deadline, secrets, credential_fd)
                    ended = _now()
                    record = {"id": query["id"], "argv": query["argv"],
                              "executedArgv": executed_argv,
                              "startedUtc": _stamp(started), "finishedUtc": _stamp(ended),
                              "exitCode": code, "captureFailure": reason,
                              "disposition": "requires-owner-disposition",
                              "stdout": evidence.write(f"{index:02d}.stdout", out),
                              "stderr": evidence.write(f"{index:02d}.stderr", err)}
                    commands.append(record)
                    evidence.write(f"{index:02d}.json", _encode(record))
                    check()
                    if reason:
                        _deny(reason)
                    # NotFound/Forbidden and other nonzero exits remain bounded
                    # observations; continue the separately approved query set.
                evidence.readback()
                check()  # status last, after final retained evidence readback
            except DiscoveryError as error:
                failure = str(error)
            except Exception:
                failure = "owner-discovery-runtime-unavailable"
            receipt = {"schemaVersion": SCHEMA + "receipt/v1", "planId": plan["planId"],
                       "planSha256": approved_digest, "decisionSha256": _sha(held_decision.raw),
                       "runnerSha256": plan["runner"]["sha256"], "baselineSourceCommit": plan["sourceCommit"],
                       "profileId": PROFILE, "profileSha256": PROFILE_SHA256, "workloadSha256": WORKLOAD_SHA256,
                       "ownerPrincipal": OWNER, "assuranceMode": ASSURANCE, "allowedAction": ACTION,
                       "startedUtc": _stamp(began), "finishedUtc": _stamp(_now()),
                       "disposition": "refused" if failure else "requires-owner-disposition",
                       "failure": failure, "commands": commands, "qualificationCredit": False,
                       "notGranted": NOT_GRANTED}
            raw = _encode(receipt)
            evidence.write("receipt.json", raw)
            evidence.readback()
            # The receipt's own readback precedes the final current status check.
            # A refusal still leaves its bounded partial observations available.
            if failure is None:
                try:
                    check()
                except DiscoveryError as error:
                    evidence.write("final-refusal.json", _encode({"disposition": "refused", "qualificationCredit": False,
                                                                  "receiptSha256": _sha(raw), "planSha256": approved_digest,
                                                                  "failure": str(error)}))
                    raise
            if failure:
                raise DiscoveryError(failure) from None
            return DiscoveryResult(evidence.path + "/receipt.json", _sha(raw), len(commands))


def main():
    """Run the collector only with the separately supplied approved raw digest."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", required=True)
    parser.add_argument("--approved-plan-sha256", required=True)
    args = parser.parse_args()
    try:
        result = collect_owner_discovery(args.plan, args.approved_plan_sha256)
    except DiscoveryError as error:
        print(str(error))
        return 2
    print(json.dumps({"receiptPath": result.receipt_path, "receiptSha256": result.receipt_sha256,
                      "commandCount": result.command_count, "disposition": result.disposition}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
