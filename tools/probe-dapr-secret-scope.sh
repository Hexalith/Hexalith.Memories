#!/bin/sh
# Execute on stdin in the application container. Requires BusyBox sh, awk and nc.
set +x
set -eu
app=$1
check=$2
case "$app/$check" in
    memories/allowed|memories-mcp/unscoped|eventstore/allowed) key=redis-secret ;;
    memories/outside) key=access-telemetry-marker-key ;;
    *) exit 2 ;;
esac
port=${DAPR_HTTP_PORT:-3500}
case "$port" in ''|*[!0-9]*) exit 2 ;; esac
: "${DAPR_API_TOKEN:?missing in-pod Dapr API token}"
date -u '+%FT%TZ'

request() {
    # printf is a shell builtin. The header goes only to nc on pod loopback.
    printf 'GET %s HTTP/1.0\r\nHost: localhost\r\ndapr-api-token: %s\r\nConnection: close\r\n\r\n' "$1" "$DAPR_API_TOKEN" |
        busybox timeout 8 busybox nc -w 5 127.0.0.1 "$port" 2>/dev/null |
        busybox awk -v mode="$2" -v app="$app" '
        # Parse the JSON structure, including escaped strings. Compare only known top-level
        # fields and entries inside components. Never print input or server error messages.
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
                if (path=="/password" && length(text)>2) present=1
                if (path=="/errorCode") {
                    if (text=="\"ERR_PERMISSION_DENIED\"") policy=1
                    if (text=="\"ERR_SECRET_STORE_NOT_FOUND\"" || text=="\"ERR_SECRET_STORES_NOT_CONFIGURED\"") unavailable=1
                }
                if (path ~ /^\/components\/[0-9]+\/(name|type)$/) {
                    split(path,parts,"/")
                    if (parts[4]=="name" && text=="\"secretstore\"") names[parts[3]]=1
                    if (parts[4]=="type" && text=="\"secretstores.hashicorp.vault\"") types[parts[3]]=1
                }
            } else { token(match(body,/^(true|false|null|-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?)/)) }
        }
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
            if (!headersDone || bad) { print "0 invalid"; exit }
            ws(); if (substr(body,1,1)!="{") { print status,"invalid"; exit }
            value("",0); ws()
            if (bad || length(body)) { print status,"invalid"; exit }
            if (mode=="metadata") {
                for (i in names) { count++; vault+=types[i] }
                if (status!=200 || !identity || !inventory) print status,"invalid"
                else print status,(count==0 ? "absent" : (count==1 && vault==1 ? "vault" : "unexpected"))
            } else {
                print status,(present ? "field-present" : (policy ? "policy" : (unavailable ? "store-unavailable" : "unverified")))
            }
        }' 2>/dev/null
}

meta=$(request /v1.0/metadata metadata)
case "$check/$meta" in
    allowed/'200 vault'|outside/'200 vault'|unscoped/'200 absent') ;;
    *) printf 'app=%s authenticated metadata=%s; identity or registration unverified\n' "$app" "$meta"; exit 1 ;;
esac
observation=$(request "/v1.0/secrets/secretstore/$key" read)
printf 'app=%s store=secretstore metadata=%s key=%s field=password HTTP/result=%s\n' "$app" "$meta" "$key" "$observation"
case "$check/$observation" in
    allowed/'200 field-present'|outside/'403 policy'|unscoped/'403 policy') exit 0 ;;
    unscoped/[45][0-9][0-9]' store-unavailable') exit 0 ;;
    *) exit 1 ;;
esac
