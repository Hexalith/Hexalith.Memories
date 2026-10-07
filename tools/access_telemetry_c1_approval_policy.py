"""Owner-approved separation rule for the future authenticated C1 bundle.

This is a pure predicate over canonical GitHub principals supplied by a required
authority adapter. It does not authenticate identities, prove role permission,
verify receipts or create accepted evidence. No deployed consumer uses it yet.
"""

from __future__ import annotations

import re


OWNER_GITHUB_PRINCIPAL = "github:user:6775094"
MAX_BUNDLE_PRODUCERS = 25
_GITHUB_PRINCIPAL = re.compile(r"github:user:([1-9][0-9]{0,18})\Z")
_MAX_PRINCIPAL_ID = 2**63 - 1


class BundlePrincipalPolicyError(ValueError):
    """A content-free refusal of invalid principal separation."""


def _refuse(code: str) -> None:
    raise BundlePrincipalPolicyError(code) from None


def _require_principal(value: str) -> None:
    if type(value) is not str or len(value) > 31:
        _refuse("principal-format-invalid")
    match = _GITHUB_PRINCIPAL.fullmatch(value)
    if match is None or int(match.group(1)) > _MAX_PRINCIPAL_ID:
        _refuse("principal-format-invalid")


def validate_bundle_principal_separation(
    operations_principal: str,
    security_principal: str,
    producer_principals: frozenset[str],
) -> None:
    """Check only separation after authentication and role/scope verification.

    The caller must derive all producer principals from every actual capture and
    authenticate both distinct role decisions on the same manifest. This
    predicate cannot supply missing identity, receipt, session or custody facts.
    The named owner may hold both reviewer roles, but never a producer role.
    """
    _require_principal(operations_principal)
    _require_principal(security_principal)
    if type(producer_principals) is not frozenset or not 1 <= len(producer_principals) <= MAX_BUNDLE_PRODUCERS:
        _refuse("producer-principals-required")
    for principal in producer_principals:
        _require_principal(principal)
    if operations_principal in producer_principals or security_principal in producer_principals:
        _refuse("bundle-producer-self-approval")
    if operations_principal == security_principal and operations_principal != OWNER_GITHUB_PRINCIPAL:
        _refuse("bundle-distinct-reviewers-required")
