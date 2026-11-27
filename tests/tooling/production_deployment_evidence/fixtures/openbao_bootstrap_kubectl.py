#!/usr/bin/env python3
"""Record a disposable OpenBao verifier's kubectl commands without a cluster."""

import json
import os
import sys
from pathlib import Path


ROOT_TOKEN = "fixture-root-token"
UNSEAL_KEY = "fixture-unseal-key"
RUNTIME_TOKEN = "fixture-runtime-token"
ACCESS_TOKEN = "fixture-access-token"
PROBE_TARGETS = {
    ("-field=password", "hexalith/memories/runtime/redis-secret"): "runtime",
    ("-field=access-telemetry-marker-key", "hexalith/memories/access-telemetry/access-telemetry-marker-key"): "access",
}
PUBLISHED_SECRETS = {
    "openbao-runtime-bootstrap": "runtime",
    "openbao-access-telemetry-bootstrap": "access",
}


def observe_pod_command(args):
    command = args[args.index("--") + 1:]
    bao = command[command.index("bao") + 1:]
    stdin = sys.stdin.read() if "-i" in args else ""
    token = None
    if "sh" in command:
        token, _, stdin = stdin.partition("\n")
    record = {"bao": bao, "token": token, "payload": stdin.rstrip("\r\n")}
    match bao:
        case ["operator", "init", *_]:
            return "initialize", record, 0, json.dumps({"root_token": ROOT_TOKEN, "unseal_keys_b64": [UNSEAL_KEY]})
        case ["write", "-format=json", "sys/unseal", "-"]:
            return "unseal", record, 0, ""
        case ["secrets", "enable", *_]:
            return "enable-kv", record, 0, ""
        case ["policy", "write", policy, "-"]:
            return f"{scope(policy)}-policy", record, 0, ""
        case ["kv", "put", _, target, "-"]:
            prefix, name = target.rsplit("/", 1)
            return f"seed-{scope(prefix)}-{name}", record, 0, ""
        case ["token", "create", "-policy", policy, *_]:
            name = scope(policy)
            client_token = ACCESS_TOKEN if name == "access" else RUNTIME_TOKEN
            return f"{name}-token", record, 0, json.dumps({"auth": {"client_token": client_token}})
        case ["kv", "get", "-mount=secret", field, target] if (field, target) in PROBE_TARGETS and token in (RUNTIME_TOKEN, ACCESS_TOKEN):
            name = "access" if token == ACCESS_TOKEN else "runtime"
            if name != PROBE_TARGETS[(field, target)]:
                return f"{name}-cross-prefix", record, 2, "permission denied (403)"
            return f"{name}-read", record, 0, f"fixture-{name}-read-value"
        case ["token", "revoke", "-self"]:
            return "revoke-root", record, 0, ""
        case _:
            raise AssertionError(f"Unexpected bao command: {bao}")


def scope(name):
    return "access" if name.endswith("access-telemetry") else "runtime"


def capture_files(options):
    files = {}
    for option in options:
        if option.startswith("--from-file="):
            _, key, path = option.split("=", 2)
            content = Path(path).read_text(encoding="ascii")
            files[key] = content.splitlines()[0] if key == "tls.key" else content
    return {"files": files}


def observe_cluster_command(args):
    match args:
        case ["config", "current-context"]:
            return "context", {}, 0, "kind-bootstrap-test"
        case ["create", "namespace", "openbao"]:
            return "namespace", {}, 0, ""
        case ["create", "secret", "generic", "openbao-server-tls", *options]:
            return "tls-secret", capture_files(options), 0, ""
        case ["create", "configmap", "hexalith-keys-config", *options]:
            return "config", capture_files(options), 0, ""
        case ["apply", "-f", "-"]:
            return "apply", {"manifest": sys.stdin.read()}, 0, ""
        case ["rollout", "status", "deployment/hexalith-keys", *_]:
            return "ready", {}, 0, ""
        case ["create", "secret", "generic", name, *options] if name in PUBLISHED_SECRETS:
            return f"publish-{PUBLISHED_SECRETS[name]}", capture_files(options), 0, ""
        case _:
            raise AssertionError(f"Unexpected kubectl command: {args}")


def main():
    args = sys.argv[1:]
    observe = observe_pod_command if args[1:2] == ["exec"] else observe_cluster_command
    action, record, exit_code, output = observe(args)
    capture = Path(sys.argv[0]).parent / "commands.jsonl"
    with capture.open("a", encoding="utf-8") as stream:
        stream.write(json.dumps({"action": action, "args": args, **record}) + "\n")

    if action == os.environ.get("STUB_OPENBAO_ACTION"):
        match os.environ["STUB_OPENBAO_RESPONSE"]:
            case "exit":
                exit_code, output = 1, f"transport failure with {ROOT_TOKEN} (AlreadyExists)"
            case "non-json":
                exit_code, output = 0, "invalid JSON"
            case "empty-object":
                exit_code, output = 0, "{}"
            case "success":
                exit_code, output = 0, "unexpected success"
            case "mount-exists":
                exit_code, output = 2, "path is already in use"
            case "sensitive-exit":
                print(
                    "rollout rejected (Forbidden)\n"
                    "-----BEGIN PRIVATE KEY-----\nfixture-private-key-canary\n-----END PRIVATE KEY-----\n"
                    "hvs.fixtureTokenCanary0123456789",
                    file=sys.stderr,
                )
                return 1
    if output:
        print(output)
    return exit_code


if __name__ == "__main__":
    sys.exit(main())
