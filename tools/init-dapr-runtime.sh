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
  sleep $((attempt * 5))
  attempt=$((attempt + 1))
done

placement_port=50005
scheduler_port=50006
docker rm -f dapr_placement dapr_scheduler >/dev/null 2>&1 || true
docker run -d --name dapr_placement -p "${placement_port}:50005" "daprio/dapr:${runtime_version}" ./placement
docker volume create dapr_scheduler
docker run -d --name dapr_scheduler -p "${scheduler_port}:50006" -p 2379:2379 -v dapr_scheduler:/var/lock \
  "daprio/dapr:${runtime_version}" ./scheduler \
  --etcd-data-dir=/var/lock/dapr/scheduler \
  --override-broadcast-host-port="127.0.0.1:${scheduler_port}" \
  --etcd-client-listen-address=0.0.0.0

wait_for_listener() {
  local resource="$1" port="$2"
  local readiness_timeout=30
  if ! timeout "${readiness_timeout}s" bash -c '
    until ( : >"/dev/tcp/127.0.0.1/$1" ) 2>/dev/null; do
      sleep 1
    done
  ' _ "$port"; then
    echo "Timed out after ${readiness_timeout}s waiting for Dapr ${resource} on 127.0.0.1:${port}" >&2
    return 1
  fi
}

# Check both listeners in parallel so one slow service cannot extend the other wait.
wait_for_listener placement "$placement_port" &
placement_check=$!
wait_for_listener scheduler "$scheduler_port" &
scheduler_check=$!
readiness_status=0
wait "$placement_check" || readiness_status=1
wait "$scheduler_check" || readiness_status=1
if (( readiness_status != 0 )); then
  exit "$readiness_status"
fi

dapr --version
