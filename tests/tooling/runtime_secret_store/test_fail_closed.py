"""Regression tests for the disposable bootstrap probe process."""

import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from fail_closed import missing_bootstrap


COMPONENT = Path(__file__).resolve().parents[3] / "deploy/kubernetes/base/dapr/secretstore.yaml"


@unittest.skipUnless(os.name == "posix", "requires an executable fixture script")
class BootstrapProbeTests(unittest.TestCase):
    def test_collects_startup_failure_after_noisy_diagnostics(self):
        with tempfile.TemporaryDirectory() as scratch:
            binary = Path(scratch) / "daprd"
            binary.write_text(
                "#!/usr/bin/env python3\n"
                "import sys\n"
                "sys.stdout.buffer.write(b'synthetic startup diagnostic\\n' * 65536)\n"
                "print('openbao-runtime-bootstrap not found')\n"
                "print('failed to load components')\n"
                "print('initialization error occurred for secretstore (secretstores.hashicorp.vault/v1)')\n"
                "sys.exit(1)\n",
                encoding="utf-8",
            )
            binary.chmod(0o700)

            with patch.dict(os.environ, {"DAPRD": str(binary)}):
                self.assertTrue(missing_bootstrap(COMPONENT))
