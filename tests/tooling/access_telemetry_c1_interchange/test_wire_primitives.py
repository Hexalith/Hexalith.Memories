"""Common-wire preparation tests; fixture success grants no C1 acceptance."""

from __future__ import annotations

from dataclasses import FrozenInstanceError
import hashlib
import json
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import tracemalloc
import unittest
from unittest.mock import patch


REPO_ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO_ROOT / "tools"))

from access_telemetry_c1_interchange import (  # noqa: E402
    EvidenceReference,
    InterchangeFormatError,
    JsonSnapshot,
    MAX_AGGREGATE_BYTES,
    MAX_ARTIFACT_BYTES,
    MAX_INTEGER,
    MAX_JSON_DEPTH,
    MAX_STRING_CHARACTERS,
    MIN_INTEGER,
    SnapshotReader,
    authenticate_snapshot,
    j1_bytes,
    parse_ref,
)


GOLDEN_VALUE = {
    "z": [MIN_INTEGER, MAX_INTEGER],
    "a": {"b": [True, None, 'quote"back\\slash\x00\t\n\r\b\f café雪😀'], "a": 0},
}
GOLDEN_J1 = (
    r'{"a":{"a":0,"b":[true,null,"quote\"back\\slash\u0000\t\n\r\b\f café雪😀"]},'
    r'"z":[-9223372036854775808,9223372036854775807]}'
).encode("utf-8")


class SnapshotTests(unittest.TestCase):
    def test_original_bytes_and_digest_are_preserved_without_normalization(self):
        raw = b'{ "z" : [true, null], "a" : "caf\\u00e9" }\r\n'
        snapshot = JsonSnapshot(raw)
        self.assertIs(snapshot.raw, raw)
        self.assertEqual(snapshot.sha256, hashlib.sha256(raw).hexdigest())
        self.assertEqual(snapshot.value["a"], "café")
        self.assertEqual(snapshot.value["z"], (True, None))
        self.assertNotEqual(snapshot.sha256, hashlib.sha256(j1_bytes(snapshot.value)).hexdigest())

    def test_snapshot_and_every_nested_container_are_immutable(self):
        snapshot = JsonSnapshot(b'{"a":[{"b":1}]}')
        with self.assertRaises(FrozenInstanceError):
            snapshot.raw = b"{}"
        with self.assertRaises(FrozenInstanceError):
            snapshot.sha256 = "0" * 64
        with self.assertRaises(TypeError):
            snapshot.value["a"] = ()
        with self.assertRaises(TypeError):
            snapshot.value["a"][0]["b"] = 2
        with self.assertRaises(TypeError):
            snapshot.value["a"][0] = {}

    def test_mutable_and_non_byte_inputs_refuse(self):
        for raw in (bytearray(b"{}"), memoryview(b"{}"), "{}", None):
            with self.subTest(raw_type=type(raw).__name__):
                with self.assertRaisesRegex(InterchangeFormatError, "immutable-bytes-required"):
                    JsonSnapshot(raw)

    def test_artifact_size_boundary_is_enforced(self):
        raw = b"{}" + b" " * (MAX_ARTIFACT_BYTES - 2)
        self.assertEqual(len(JsonSnapshot(raw).raw), 1_048_576)
        with self.assertRaisesRegex(InterchangeFormatError, "artifact-byte-budget-exceeded"):
            JsonSnapshot(raw + b" ")

    def test_bom_and_invalid_utf8_refuse(self):
        for raw in (b"\xef\xbb\xbf{}", b'{"x":"\xff"}', b'{"x":"\xed\xa0\x80"}'):
            with self.subTest(raw=repr(raw)):
                with self.assertRaises(InterchangeFormatError):
                    JsonSnapshot(raw)

    def test_duplicate_decoded_keys_refuse_at_every_depth(self):
        for raw in (
            b'{"a":1,"a":2}',
            b'{"a":1,"\\u0061":2}',
            b'{"outer":[{"a":1,"a":2}]}',
        ):
            with self.subTest(raw=raw):
                with self.assertRaisesRegex(InterchangeFormatError, "duplicate-json-field"):
                    JsonSnapshot(raw)

    def test_malformed_json_and_extra_values_refuse(self):
        for raw in (b"", b" ", b"{}{}", b"{} false", b"/*comment*/{}", b'{"x":1,}', b"[1,]", b"[", b"non-JSON"):
            with self.subTest(raw=raw):
                with self.assertRaisesRegex(InterchangeFormatError, "invalid-json"):
                    JsonSnapshot(raw)

    def test_float_and_nonfinite_syntax_refuse(self):
        for number in ("1.0", "1e0", "-0.0", "NaN", "Infinity", "-Infinity"):
            with self.subTest(number=number):
                with self.assertRaisesRegex(InterchangeFormatError, "non-integer-json-number"):
                    JsonSnapshot(f'{{"x":{number}}}'.encode())

    def test_signed_64_bit_boundaries_and_bool_are_distinct(self):
        snapshot = JsonSnapshot(b"[-9223372036854775808,9223372036854775807,true,false]")
        self.assertEqual(snapshot.value, (MIN_INTEGER, MAX_INTEGER, True, False))
        for number in (str(MIN_INTEGER - 1), str(MAX_INTEGER + 1), "1" * 5000):
            with self.subTest(length=len(number)):
                with self.assertRaisesRegex(InterchangeFormatError, "integer-out-of-range"):
                    JsonSnapshot(number.encode())

    def test_depth_boundary_precedes_decoder_recursion(self):
        raw = b"[" * MAX_JSON_DEPTH + b"0" + b"]" * MAX_JSON_DEPTH
        JsonSnapshot(raw)
        for depth in (MAX_JSON_DEPTH + 1, 2000):
            with self.subTest(depth=depth):
                with self.assertRaisesRegex(InterchangeFormatError, "json-depth-exceeded"):
                    JsonSnapshot(b"[" * depth + b"0" + b"]" * depth)

    def test_depth_scanner_honors_escaped_quotes_and_backslashes(self):
        value = {"a": '[{"\\\\\\\"' * 100, "b": {"c": 1}}
        snapshot = JsonSnapshot(json.dumps(value).encode())
        self.assertEqual(snapshot.value["a"], value["a"])
        self.assertEqual(snapshot.value["b"]["c"], 1)

    def test_strings_and_field_names_are_bounded(self):
        JsonSnapshot(json.dumps({"a": "x" * MAX_STRING_CHARACTERS}).encode())
        for value in ({"a": "x" * (MAX_STRING_CHARACTERS + 1)}, {"x" * (MAX_STRING_CHARACTERS + 1): 0}):
            with self.subTest(kind="value" if "a" in value else "key"):
                with self.assertRaisesRegex(InterchangeFormatError, "string-budget-exceeded"):
                    JsonSnapshot(json.dumps(value).encode())

    def test_unpaired_surrogates_refuse_but_valid_pair_is_preserved(self):
        for raw in (b'"\\ud800"', b'"\\udfff"', b'{"\\ud800":1}'):
            with self.subTest(raw=raw):
                with self.assertRaisesRegex(InterchangeFormatError, "unpaired-unicode-surrogate"):
                    JsonSnapshot(raw)
        self.assertEqual(JsonSnapshot(b'"\\ud83d\\ude00"').value, "😀")

    def test_rejection_diagnostics_do_not_echo_untrusted_fields_or_values(self):
        canary = "Bearer private-test-credential"
        for raw in (
            f'{{"{canary}":1,"{canary}":2}}'.encode(),
            f'{{"x":"{canary}"'.encode(),
        ):
            with self.subTest(raw_kind=len(raw)):
                with self.assertRaises(InterchangeFormatError) as result:
                    JsonSnapshot(raw)
                self.assertNotIn(canary, str(result.exception))
                self.assertLess(len(str(result.exception)), 80)
                self.assertTrue(result.exception.__suppress_context__)

    def test_reader_charges_identical_snapshots_only_once(self):
        reader = SnapshotReader()
        first = reader.read(b'{"a":1}')
        self.assertIs(reader.read(b'{"a":1}'), first)
        self.assertEqual(reader.byte_count, len(first.raw))
        reader.read(b'{"a":1}\n')
        self.assertEqual(reader.byte_count, 2 * len(first.raw) + 1)

    def test_full_aggregate_boundary_refuses_before_parsing_additional_snapshot(self):
        reader = SnapshotReader()
        for index in range(32):
            raw = f'{{"i":{index}}}'.encode()
            reader.read(raw + b" " * (MAX_ARTIFACT_BYTES - len(raw)))
        self.assertEqual(reader.byte_count, MAX_AGGREGATE_BYTES)
        with patch("access_telemetry_c1_interchange.JsonSnapshot", side_effect=AssertionError("must not parse")):
            with self.assertRaisesRegex(InterchangeFormatError, "aggregate-byte-budget-exceeded"):
                reader.read(b'{"i":33}')
        self.assertEqual(reader.byte_count, 33_554_432)

    def test_rejected_snapshot_does_not_consume_budget(self):
        reader = SnapshotReader()
        for raw in (b"not JSON", b"1.0", b"[" * 20):
            with self.subTest(raw=raw):
                with self.assertRaises(InterchangeFormatError):
                    reader.read(raw)
        self.assertEqual(reader.byte_count, 0)
        reader.read(b"{}")
        self.assertEqual(reader.byte_count, 2)

    def test_parser_and_reference_matching_have_no_file_network_or_process_calls(self):
        with (
            patch("builtins.open", side_effect=AssertionError("no filesystem")),
            patch.object(socket, "create_connection", side_effect=AssertionError("no network")),
            patch.object(subprocess, "run", side_effect=AssertionError("no process")),
        ):
            snapshot = SnapshotReader().read(b'{"a":1}')
            reference = parse_ref({"path": "gate/capture.json", "sha256": snapshot.sha256, "byteLength": len(snapshot.raw)})
            authenticate_snapshot(reference, snapshot)
            self.assertEqual(j1_bytes(snapshot.value), b'{"a":1}')


class ReferenceTests(unittest.TestCase):
    def setUp(self):
        self.snapshot = JsonSnapshot(b'{"a":1}\r\n')
        self.ref = {"path": "gate-1/capture.json", "sha256": self.snapshot.sha256, "byteLength": len(self.snapshot.raw)}

    def test_exact_ref_round_trip_matches_original_snapshot(self):
        reference = parse_ref(JsonSnapshot(json.dumps(self.ref).encode()).value)
        authenticate_snapshot(reference, self.snapshot)
        self.assertEqual(reference.path, "gate-1/capture.json")
        self.assertEqual(reference.byte_length, 9)

    def test_unknown_missing_and_wrong_root_fields_refuse(self):
        for value in ({**self.ref, "approved": True}, {key: value for key, value in self.ref.items() if key != "sha256"}, [], None, "ref"):
            with self.subTest(kind=type(value).__name__):
                with self.assertRaisesRegex(InterchangeFormatError, "reference-field-set"):
                    parse_ref(value)

    def test_noncanonical_paths_refuse(self):
        for path in ("", "/root/file", "//root/file", "../file", "./file", "a/../file", "a/./file", "a//file", "a/", "C:/file", "C:\\file", "a\\file", "https://example/file", "~/file", "a\x00file", "a\nfile", "a\x7ffile", "a\x85file", "a\x9bfile"):
            with self.subTest(path=repr(path)):
                with self.assertRaises(InterchangeFormatError):
                    parse_ref({**self.ref, "path": path})

    def test_path_types_length_and_secret_canaries_refuse(self):
        for path in (None, True, 1, "x" * 513, "gate/Bearer private-test-credential", "gate/password=private-test-credential", "gate/access_token=private-test-credential.json", "gate/authorization=Basic-private-test-credential.json", "gate/refresh-token=private-test-credential", "gate/idToken=private-test-credential"):
            with self.subTest(path_type=type(path).__name__):
                with self.assertRaises(InterchangeFormatError) as result:
                    parse_ref({**self.ref, "path": path})
                self.assertNotIn("private-test-credential", str(result.exception))
        parse_ref({**self.ref, "path": "x" * 512})

    def test_digest_requires_exact_lowercase_sha256(self):
        for digest in (self.snapshot.sha256.upper(), "sha256:" + self.snapshot.sha256, self.snapshot.sha256 + "\n", " " + self.snapshot.sha256, "0" * 63, "g" * 64, None, 1):
            with self.subTest(digest_type=type(digest).__name__):
                with self.assertRaisesRegex(InterchangeFormatError, "reference-digest-not-canonical"):
                    parse_ref({**self.ref, "sha256": digest})

    def test_length_requires_positive_bounded_integer_not_bool_float_or_string(self):
        for length in (True, False, 0, -1, 1.0, "9", None, MAX_ARTIFACT_BYTES + 1, MAX_INTEGER + 1):
            with self.subTest(length=length):
                with self.assertRaisesRegex(InterchangeFormatError, "reference-byte-length"):
                    parse_ref({**self.ref, "byteLength": length})
        parse_ref({**self.ref, "byteLength": MAX_ARTIFACT_BYTES})

    def test_direct_constructor_cannot_bypass_reference_validation(self):
        with self.assertRaises(InterchangeFormatError):
            EvidenceReference("../escape", self.snapshot.sha256, len(self.snapshot.raw))

    def test_changed_length_or_bytes_refuse(self):
        with self.assertRaisesRegex(InterchangeFormatError, "reference-length-mismatch"):
            authenticate_snapshot(parse_ref({**self.ref, "byteLength": 10}), self.snapshot)
        changed = JsonSnapshot(b'{"a":2}\r\n')
        with self.assertRaisesRegex(InterchangeFormatError, "reference-digest-mismatch"):
            authenticate_snapshot(parse_ref(self.ref), changed)
        with self.assertRaisesRegex(InterchangeFormatError, "reference-and-snapshot-required"):
            authenticate_snapshot(self.ref, self.snapshot)


class CanonicalEncodingTests(unittest.TestCase):
    def test_literal_cross_language_vector_has_exact_bytes(self):
        self.assertEqual(j1_bytes(GOLDEN_VALUE), GOLDEN_J1)
        self.assertEqual(j1_bytes(JsonSnapshot(GOLDEN_J1).value), GOLDEN_J1)

    def test_array_order_and_unicode_normalization_are_preserved(self):
        self.assertNotEqual(j1_bytes([1, 2]), j1_bytes([2, 1]))
        self.assertNotEqual(j1_bytes({"a": "é"}), j1_bytes({"a": "e\u0301"}))
        self.assertEqual(j1_bytes({"z": {"z": 1, "a": 2}, "a": False}), b'{"a":false,"z":{"a":2,"z":1}}')

    def test_non_json_values_unicode_keys_and_unbounded_inputs_refuse(self):
        for value in (1.0, float("nan"), float("inf"), {"a": {1, 2}}, {1: "a"}, {"é": 1}, "\ud800", MAX_INTEGER + 1):
            with self.subTest(value_type=type(value).__name__):
                with self.assertRaises(InterchangeFormatError):
                    j1_bytes(value)
        cycle = []
        cycle.append(cycle)
        with self.assertRaisesRegex(InterchangeFormatError, "json-depth-exceeded"):
            j1_bytes(cycle)

    def test_encoded_artifact_byte_budget_refuses(self):
        # Every string fits its own bound while the final serialized artifact exceeds 1 MiB.
        with self.assertRaisesRegex(InterchangeFormatError, "artifact-byte-budget-exceeded"):
            j1_bytes(["x" * MAX_STRING_CHARACTERS] * 256)

    def test_over_budget_encoding_never_allocates_the_complete_output(self):
        value = ["x" * MAX_STRING_CHARACTERS] * 8192
        tracemalloc.start()
        try:
            with self.assertRaisesRegex(InterchangeFormatError, "artifact-byte-budget-exceeded"):
                j1_bytes(value)
            _, peak = tracemalloc.get_traced_memory()
        finally:
            tracemalloc.stop()
        self.assertLess(peak, 4 * MAX_ARTIFACT_BYTES)

    def test_powershell_emits_the_same_literal_j1_vector(self):
        self.assertIsNotNone(shutil.which("pwsh"), "PowerShell is required; the independent vector cannot be skipped")
        script = r'''
$ErrorActionPreference = 'Stop'
function Write-J1 {
    param([object]$value)
    if ($null -eq $value) { return 'null' }
    if ($value -is [bool]) {
        if ($value) { return 'true' }
        return 'false'
    }
    if ($value -is [string]) {
        $builder = [Text.StringBuilder]::new()
        [void]$builder.Append('"')
        foreach ($character in $value.ToCharArray()) {
            $code = [int]$character
            switch ($code) {
                8 { [void]$builder.Append('\b') }
                9 { [void]$builder.Append('\t') }
                10 { [void]$builder.Append('\n') }
                12 { [void]$builder.Append('\f') }
                13 { [void]$builder.Append('\r') }
                34 { [void]$builder.Append('\"') }
                92 { [void]$builder.Append('\\') }
                default {
                    if ($code -lt 32) { [void]$builder.Append('\u' + $code.ToString('x4')) }
                    else { [void]$builder.Append($character) }
                }
            }
        }
        [void]$builder.Append('"')
        return $builder.ToString()
    }
    if ($value -is [Collections.IDictionary]) {
        $keys = [string[]]@($value.Keys)
        [Array]::Sort($keys, [StringComparer]::Ordinal)
        $entries = foreach ($key in $keys) { (Write-J1 $key) + ':' + (Write-J1 $value[$key]) }
        return '{' + ($entries -join ',') + '}'
    }
    if ($value -is [array]) {
        $entries = foreach ($item in $value) { Write-J1 $item }
        return '[' + ($entries -join ',') + ']'
    }
    if ($value -is [int] -or $value -is [long]) { return $value.ToString([Globalization.CultureInfo]::InvariantCulture) }
    throw 'Unsupported J1 fixture type'
}
$value = [Console]::In.ReadToEnd() | ConvertFrom-Json -AsHashtable -NoEnumerate
$encoded = Write-J1 $value
[Console]::Out.Write([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($encoded)))
'''
        import base64
        vectors = [
            (GOLDEN_VALUE, GOLDEN_J1),
            ({"a": 2, "A": 1}, b'{"A":1,"a":2}'),
            ({"a": "\x85\u2028\u2029", "b": r"\u0085\u2028\u2029"},
             ('{"a":"\x85\u2028\u2029","b":"\\\\u0085\\\\u2028\\\\u2029"}').encode("utf-8")),
            (None, b"null"), (True, b"true"), (False, b"false"),
            ([], b"[]"), ([1], b"[1]"), ({}, b"{}"),
        ]
        for value, expected in vectors:
            with self.subTest(value_type=type(value).__name__):
                self.assertEqual(j1_bytes(value), expected)
                result = subprocess.run(
                    ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
                    input=json.dumps(value, ensure_ascii=False),
                    capture_output=True,
                    text=True,
                    timeout=30,
                    check=False,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(base64.b64decode(result.stdout, validate=True), expected)


if __name__ == "__main__":
    unittest.main()
