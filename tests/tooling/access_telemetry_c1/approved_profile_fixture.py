"""Stage exact approved deployment inputs for offline qualification tests."""

import re
import shutil
from pathlib import Path


PRODUCTION_OVERLAY = Path("deploy/kubernetes/overlays/production")
APPROVED_PRODUCTION_OVERLAY = Path(__file__).parent / "fixtures/approved_pg2"


def stage_approved_inputs(repository: Path, target: Path, policy: Path) -> None:
    """Copy profile inputs, including overlay bytes from f5beeed52874f7feedf3d48d040feaea163cd3cd."""
    for relative in re.findall(r"'(deploy/[^']+)' = '[0-9a-f]{64}'", policy.read_text()):
        path = Path(relative)
        destination = target / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        if path.parent == PRODUCTION_OVERLAY and (APPROVED_PRODUCTION_OVERLAY / path.name).is_file():
            source = APPROVED_PRODUCTION_OVERLAY / path.name
        else:
            source = repository / path
        shutil.copyfile(source, destination)
