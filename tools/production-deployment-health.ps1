# Shared health-response parsing for the production deployment verification lane.
#
# Extracted from tools/verify-production-deployment.ps1 so the parsing can be exercised
# directly with real input. While it lived inline it was pinned only by a source-text
# assertion (`assertIn("function Get-HealthJsonBody", verifier)`), so dropping the
# escape/in-string handling, or keeping the first rather than the last status-bearing
# object, passed every suite and the live kind job and failed only on the noisy
# transcripts the function exists for.
#
# Dot-source this file; it defines functions and executes nothing.

function Get-HealthJsonBody {
    param([AllowEmptyString()][string]$Text)

    # kubectl combines wget diagnostics, response bodies, and fallback output. Extract the
    # last balanced JSON object that has the aggregate-health status property instead of
    # accepting arbitrary braces from a diagnostic message.
    $depth = 0
    $start = -1
    $insideString = $false
    $escaped = $false
    $lastValidBody = $null
    for ($index = 0; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        if ($insideString) {
            if ($escaped) {
                $escaped = $false
            }
            elseif ($character -eq '\') {
                $escaped = $true
            }
            elseif ($character -eq '"') {
                $insideString = $false
            }
            continue
        }

        if ($character -eq '"') {
            $insideString = $true
        }
        elseif ($character -eq '{') {
            if ($depth -eq 0) {
                $start = $index
            }
            $depth++
        }
        elseif ($character -eq '}' -and $depth -gt 0) {
            $depth--
            if ($depth -eq 0 -and $start -ge 0) {
                $candidate = $Text.Substring($start, $index - $start + 1)
                try {
                    $parsed = $candidate | ConvertFrom-Json -ErrorAction Stop
                    if ($null -ne $parsed.PSObject.Properties['status']) {
                        $lastValidBody = $candidate
                    }
                }
                catch {
                    # Continue scanning; a later response body may still be valid.
                }
                $start = -1
            }
        }
    }

    if ($null -ne $lastValidBody) {
        return $lastValidBody
    }

    return $Text
}


function Get-HealthStatusCode {
    param([AllowEmptyString()][string]$Text)

    # BusyBox wget prints one status line per redirect/attempt and the netcat fallback adds
    # its own, so the LAST status line is the response the health decision is about.
    $statusMatches = [regex]::Matches($Text, 'HTTP/\d(?:\.\d)?\s+(?<status>\d{3})')
    if ($statusMatches.Count -eq 0) {
        return $null
    }

    return [int]$statusMatches[$statusMatches.Count - 1].Groups['status'].Value
}
