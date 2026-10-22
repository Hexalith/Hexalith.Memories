"""Authentic 2026-10-22 native mapping/failure regression; offline evidence only."""
from copy import deepcopy
import json
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as functional

FIXTURES = Path(__file__).with_name("fixtures")
NATIVE_HASH = "47aa17bcd2667e7b03a921321c31257851619f686a8dd1609a076deff79123c8"


class SelectedMappingHashTests(unittest.TestCase):
    def mapping(self):
        return json.loads((FIXTURES / "native-selected-source-mapping.json").read_bytes())

    def test_authentic_producer_mapping_with_quoted_warning_is_accepted(self):
        mapping = self.mapping()
        self.assertEqual(NATIVE_HASH, mapping["ContentHash"])
        self.assertIn("'/home/administrator/projects/hexalith/platform/references/Hexalith.Builds'", mapping["IdentityFindings"][0]["Message"])
        functional._validate_selected_mapping(mapping, NATIVE_HASH, NATIVE_HASH)

    def test_default_encoder_escapes_match_the_real_dotnet_producer(self):
        mapping = self.mapping()
        # Reference SHA-256 from .NET 10 System.Text.Json.SerializeToUtf8Bytes
        # of this exact native payload, with only this diagnostic value replaced.
        mapping["IdentityFindings"][0]["Message"] = "".join(chr(value) for value in range(128)) + "é😀\u2028\u2029" + r"\u00e9"
        reference = "5b82e35437b893768bd04c0b439a2c4d91106be6fc8aa9dd1285b2353ca06469"
        functional._validate_selected_mapping(mapping, reference, reference)

    def test_actual_mapping_tampering_is_rejected(self):
        for field in ("root", "candidate", "warning", "platform"):
            with self.subTest(field=field):
                mapping = deepcopy(self.mapping())
                if field == "root":
                    mapping["Root"] += "-tampered"
                elif field == "candidate":
                    next(entry for entry in mapping["Entries"] if entry["Identity"] == "Hexalith.Memories")["Path"] += "-tampered"
                elif field == "warning":
                    mapping["IdentityFindings"][0]["Message"] += "-tampered"
                else:
                    mapping["PlatformIdentity"]["Commit"] = "0" * 40
                with self.assertRaisesRegex(functional.FunctionalRetentionError, "functional-selected-mapping-hash"):
                    functional._validate_selected_mapping(mapping, NATIVE_HASH, NATIVE_HASH)

    def test_plan_and_handoff_hashes_must_both_match(self):
        for plan, handoff in (("0" * 64, NATIVE_HASH), (NATIVE_HASH, "0" * 64)):
            with self.subTest(plan=plan, handoff=handoff), self.assertRaisesRegex(functional.FunctionalRetentionError, "functional-selected-mapping-hash"):
                functional._validate_selected_mapping(self.mapping(), plan, handoff)



if __name__ == "__main__":
    unittest.main()
