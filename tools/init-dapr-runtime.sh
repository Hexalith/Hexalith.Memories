#!/usr/bin/env bash
# Initialize a slim Dapr runtime and the IPv4 placement/scheduler containers used by CI.
# Retries `dapr init --slim` because GitHub's Dapr binary CDN occasionally returns HTTP 500.
set -euo pipefail

runtime_version="${DAPR_RUNTIME_VERSION:?DAPR_RUNTIME_VERSION is required}"
max_attempts="${DAPR_INIT_ATTEMPTS:-5}"

# Give each background job a process group that includes its child processes.
# Bash can run signal traps immediately while waiting for a background job.
set -m
pending_jobs=()
docker_cleanup_log=""

start_job() {
  "$@" &
  started_job=$!
  pending_jobs[started_job]=1
}

kill_job_group() {
  kill -KILL -- "-$1" 2>/dev/null || true
}

wait_for_job() {
  local pid="$1" status=0
  wait "$pid" || status=$?
  # The launcher may have exited before its children, including on timeout.
  kill_job_group "$pid"
  unset "pending_jobs[$pid]"
  return "$status"
}

wait_for_job_after_kill() {
  kill_job_group "$1"
  wait "$1" 2>/dev/null || true
}

stop_jobs() {
  trap '' HUP INT TERM
  local pid
  for pid in "${!pending_jobs[@]}"; do
    wait_for_job_after_kill "$pid"
  done
  # A signal can arrive between launching a job and recording its PID.
  while read -r pid; do
    wait_for_job_after_kill "$pid"
  done < <(jobs -p)
  if [[ -n "$docker_cleanup_log" ]]; then
    # Keep the initializer's exit status if log removal fails.
    rm -f -- "$docker_cleanup_log" || true
  fi
}

trap stop_jobs EXIT
trap 'exit 129' HUP
trap 'exit 130' INT
trap 'exit 143' TERM

run_bounded() {
  local deadline="$1"
  shift
  start_job timeout --foreground --kill-after=5s "$deadline" "$@"
  wait_for_job "$started_job"
}

run_setup_command() {
  local deadline="$1" status=0
  shift
  run_bounded "$deadline" "$@" || status=$?
  if (( status == 124 || status == 137 )); then
    echo "Timed out after ${deadline} running $*" >&2
  fi
  return "$status"
}

only_missing_dapr_containers() {
  local log_file="$1" line
  local missing='^(Error|Error response from daemon): No such container: dapr_(placement|scheduler)$'
  [[ -s "$log_file" ]] || return 1
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ "$line" =~ $missing ]] || return 1
  done < "$log_file"
}

attempt=1
while true; do
  if run_setup_command 120s dapr init --slim --runtime-version "${runtime_version}"; then
    break
  fi
  if (( attempt >= max_attempts )); then
    echo "dapr init --slim failed after ${max_attempts} attempts" >&2
    exit 1
  fi
  echo "dapr init --slim failed (attempt ${attempt}/${max_attempts}); retrying..." >&2
  start_job sleep $((attempt * 5))
  wait_for_job "$started_job"
  attempt=$((attempt + 1))
done

placement_port=50005
scheduler_port=50006
docker_metadata_timeout=30s
# Older Docker CLIs may return 1 even if the only errors are missing containers.
docker_cleanup_log=$(mktemp -t dapr-docker-cleanup.XXXXXXXXXX)
cleanup_status=0
run_bounded "$docker_metadata_timeout" docker rm -f dapr_placement dapr_scheduler >/dev/null 2>"$docker_cleanup_log" || cleanup_status=$?
if (( cleanup_status == 1 )) && only_missing_dapr_containers "$docker_cleanup_log"; then
  cleanup_status=0
fi
if (( cleanup_status != 0 )); then
  if (( cleanup_status == 124 || cleanup_status == 137 )); then
    echo "Timed out after ${docker_metadata_timeout} removing dapr_placement and dapr_scheduler" >&2
  else
    echo "Docker cleanup failed while removing dapr_placement and dapr_scheduler (exit ${cleanup_status})" >&2
  fi
  cat "$docker_cleanup_log" >&2
  exit "$cleanup_status"
fi

# Starting containers may first need to pull the pinned Dapr image.
run_setup_command 120s docker run -d --name dapr_placement -p "${placement_port}:50005" "daprio/dapr:${runtime_version}" ./placement
run_setup_command "$docker_metadata_timeout" docker volume create dapr_scheduler
run_setup_command 120s docker run -d --name dapr_scheduler -p "${scheduler_port}:50006" -p 2379:2379 -v dapr_scheduler:/var/lock \
  "daprio/dapr:${runtime_version}" ./scheduler \
  --etcd-data-dir=/var/lock/dapr/scheduler \
  --override-broadcast-host-port="127.0.0.1:${scheduler_port}" \
  --etcd-client-listen-address=0.0.0.0

wait_for_listener() {
  local resource="$1" port="$2"
  local readiness_timeout=30
  # Dapr answers on gRPC even when the standard health service is unregistered.
  if ! timeout --foreground --kill-after=5s "${readiness_timeout}s" bash -c '
    until response=$(printf "\0\0\0\0\0" | curl -q --noproxy "*" \
        --http2-prior-knowledge --max-time 2 --silent --output /dev/null \
        --write-out "%{http_version} %{http_code} %{content_type}" \
        --header "Content-Type: application/grpc" --header "TE: trailers" \
        --data-binary @- "http://127.0.0.1:$1/grpc.health.v1.Health/Check") &&
      [[ "$response" == "2 200 application/grpc" ]]; do
      sleep 1
    done
  ' _ "$port"; then
    echo "Timed out after ${readiness_timeout}s waiting for Dapr ${resource} on 127.0.0.1:${port}" >&2
    return 1
  fi
}

# Check both listeners in parallel so one slow service cannot extend the other wait.
start_job wait_for_listener placement "$placement_port"
placement_check=$started_job
start_job wait_for_listener scheduler "$scheduler_port"
scheduler_check=$started_job
readiness_status=0
wait_for_job "$placement_check" || readiness_status=1
wait_for_job "$scheduler_check" || readiness_status=1
if (( readiness_status != 0 )); then
  exit "$readiness_status"
fi

run_setup_command 30s dapr --version
