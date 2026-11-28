#!/bin/sh
# Execute on stdin in the application container. Requires BusyBox sh, awk and nc.
# Usage: sh -s -- <app-id> <check> [expected-runtime-version]
set +x
set -eu
app=$1
check=$2
version=${3:-}
# The app ID reaches awk -v, which would interpret backslash escapes, so only a fixed shape passes.
case "$app" in ''|*[!a-z0-9-]*) exit 2 ;; esac
# A version must have the shape the parser reports, never its placeholder words.
case "$version" in *[!0-9A-Za-z.-]*) exit 2 ;; esac
if [ -n "$version" ] && ! printf '%s\n' "$version" | busybox grep -Eqx '[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?'; then exit 2; fi
store=secretstore
stype=secretstores.hashicorp.vault
field=password
case "$app/$check" in
    memories/allowed|memories-mcp/unscoped|memories-access-telemetry/unscoped|eventstore/allowed) key=redis-secret ;;
    memories/outside) key=access-telemetry-marker-key ;;
    memories-access-telemetry/allowed) store=access-telemetry-secrets key=redis-secret ;;
    memories-access-telemetry/outside) store=access-telemetry-secrets key=access-telemetry-clock-key ;;
    */builtin)
        # Certifying the built-in store denial requires a verified runtime version.
        [ -n "$version" ] || exit 2
        # No Secret carries this name. A deny policy rejects it before any Kubernetes API lookup, and
        # without that policy the lookup misses, so this check can never return a Secret value.
        store=kubernetes stype=secretstores.kubernetes key=dapr-scope-probe-synthetic-absent field=none ;;
    *) exit 2 ;;
esac
port=${DAPR_HTTP_PORT:-3500}
case "$port" in ''|*[!0-9]*) exit 2 ;; esac
: "${DAPR_API_TOKEN:?missing in-pod Dapr API token}"
date -u '+%FT%TZ'

request() {
    # nc -e hands the loopback socket to a child shell, which writes the request and reads
    # the response without closing its write side. BusyBox nc half-closes at stdin EOF, and
    # daprd cancels an in-flight secret-store read when its client closes. The child gets
    # the token only from the environment; the printf builtin writes it only to the socket.
    # The response, or an nc error, reaches the parser on stderr.
    busybox timeout 8 busybox nc -w 5 127.0.0.1 "$port" -e busybox sh -c '
        printf "GET %s HTTP/1.0\r\nHost: localhost\r\ndapr-api-token: %s\r\nConnection: close\r\n\r\n" "$1" "$DAPR_API_TOKEN"
        exec busybox cat >&2' probe-request "$1" 2>&1 >/dev/null |
        busybox awk -v mode="$2" -v app="$app" -v store="$store" -v stype="$stype" -v field="$field" '
        # Parse the JSON structure, including escaped strings. Compare only known top-level
        # fields and entries inside components. Never print input or server error messages.
        # The runtime version is printed only when it has the public version shape.
        function ws() { sub(/^[ \t\r\n]+/, "", body) }
        function token(start,    n,text) {
            if (start != 1) { bad=1; return "" }
            n=RLENGTH; text=substr(body,1,n); body=substr(body,n+1); return text
        }
        function value(path,depth,    c,k,i,text) {
            ws(); c=substr(body,1,1)
            if (depth>64 || bad) { bad=1; return }
            if (c=="{" || c=="[") {
                body=substr(body,2); ws()
                if (path=="/components" && c=="[") inventory=1
                if (substr(body,1,1)==(c=="{" ? "}" : "]")) { body=substr(body,2); return }
                for (i=0; !bad; i++) {
                    if (c=="{") {
                        k=token(match(body,/^"([^"\\]|\\.)*"/)); ws(); token(match(body,/^:/))
                        k=substr(k,2,length(k)-2)
                        # Slashes are never present in the public metadata property names.
                        if (index(k,"/")) k="-"
                    } else { k=i }
                    value(path "/" k,depth+1); ws()
                    if (substr(body,1,1)==(c=="{" ? "}" : "]")) { body=substr(body,2); return }
                    token(match(body,/^,/)); ws()
                }
            } else if (c=="\"") {
                text=token(match(body,/^"([^"\\]|\\.)*"/))
                if (path=="/id" && text=="\"" app "\"") identity=1
                if (path=="/runtimeVersion") {
                    runtime=substr(text,2,length(text)-2)
                    if (runtime !~ /^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?$/) runtime="unrecognized"
                }
                if (path=="/" field && length(text)>2) present=1
                if (path=="/errorCode") {
                    if (text=="\"ERR_PERMISSION_DENIED\"") policy=1
                    if (text=="\"ERR_SECRET_STORE_NOT_FOUND\"" || text=="\"ERR_SECRET_STORES_NOT_CONFIGURED\"") unavailable=1
                }
                if (path ~ /^\/components\/[0-9]+\/(name|type)$/) {
                    split(path,parts,"/")
                    if (parts[4]=="name" && text=="\"" store "\"") names[parts[3]]=1
                    if (parts[4]=="type" && text=="\"" stype "\"") types[parts[3]]=1
                }
            } else { token(match(body,/^(true|false|null|-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?)/)) }
        }
        BEGIN { runtime="absent" }
        NR==1 {
            if ($1 !~ /^HTTP\/1\.[01]$/ || $2 !~ /^[1-5][0-9][0-9]$/) bad=1
            else status=$2
            next
        }
        !headersDone {
            if ($0=="\r" || $0=="") headersDone=1
            if (tolower($1)=="transfer-encoding:") bad=1
            next
        }
        { body=body $0 "\n"; if (length(body)>1048576) { bad=1; exit } }
        END {
            if (!headersDone || bad) { print "0 invalid absent"; exit }
            ws(); if (substr(body,1,1)!="{") { print status,"invalid absent"; exit }
            value("",0); ws()
            if (bad || length(body)) { print status,"invalid absent"; exit }
            if (mode=="metadata") {
                for (i in names) { count++; matched+=types[i] }
                if (status!=200 || !identity || !inventory) print status,"invalid",runtime
                else print status,(count==0 ? "absent" : (count==1 && matched==1 ? "registered" : "unexpected")),runtime
            } else {
                print status,(present ? "field-present" : (policy ? "policy" : (unavailable ? "store-unavailable" : "unverified")))
            }
        }' 2>/dev/null
}

meta=$(request /v1.0/metadata metadata)
read -r meta_status registration runtime <<EOF
$meta
EOF
case "$check" in unscoped) expected=absent ;; *) expected=registered ;; esac
if [ "$meta_status/$registration" != "200/$expected" ] || { [ -n "$version" ] && [ "$runtime" != "$version" ]; }; then
    printf 'app=%s runtime=%s store=%s authenticated metadata=%s %s; identity, runtime or registration unverified\n' \
        "$app" "${runtime:-absent}" "$store" "${meta_status:-0}" "${registration:-invalid}"
    exit 1
fi
observation=$(request "/v1.0/secrets/$store/$key" read)
printf 'app=%s runtime=%s store=%s registration=%s key=%s field=%s HTTP/result=%s\n' \
    "$app" "$runtime" "$store" "$registration" "$key" "$field" "$observation"
case "$check/$observation" in
    allowed/'200 field-present'|outside/'403 policy'|unscoped/'403 policy'|builtin/'403 policy') exit 0 ;;
    unscoped/[45][0-9][0-9]' store-unavailable') exit 0 ;;
    *) exit 1 ;;
esac
