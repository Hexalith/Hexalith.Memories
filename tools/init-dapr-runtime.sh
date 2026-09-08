#!/usr/bin/env bash
# Initialize a slim Dapr runtime and the IPv4 placement/scheduler containers used by CI.
# Retries `dapr init --slim` because GitHub's Dapr binary CDN occasionally returns HTTP 500.
set -euo pipefail

runtime_version="${DAPR_RUNTIME_VERSION:?DAPR_RUNTIME_VERSION is required}"
max_attempts="${DAPR_INIT_ATTEMPTS:-5}"

attempt=1
while true; do
  if dapr init --slim --runtime-version "${runtime_version}"; then
    break
  fi
  if (( attempt >= max_attempts )); then
    echo "dapr init --slim failed after ${max_attempts} attempts" >&2
    exit 1
  fi
  echo "dapr init --slim failed (attempt ${attempt}/${max_attempts}); retrying..." >&2
  dapr uninstall --all >/dev/null 2>&1 || true
  sleep $((attempt * 5))
  attempt=$((attempt + 1))
done

docker rm -f dapr_placement dapr_scheduler >/dev/null 2>&1 || true
docker run -d --name dapr_placement -p 50005:50005 "daprio/dapr:${runtime_version}" ./placement
docker volume create dapr_scheduler
docker run -d --name dapr_scheduler -p 50006:50006 -p 2379:2379 -v dapr_scheduler:/var/lock \
  "daprio/dapr:${runtime_version}" ./scheduler \
  --etcd-data-dir=/var/lock/dapr/scheduler \
  --override-broadcast-host-port=127.0.0.1:50006 \
  --etcd-client-listen-address=0.0.0.0
dapr --version
