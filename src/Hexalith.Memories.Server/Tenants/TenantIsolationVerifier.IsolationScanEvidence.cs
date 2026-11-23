// <copyright file="TenantIsolationVerifier.IsolationScanEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using StackExchange.Redis;

public sealed partial class TenantIsolationVerifier
{
    /// <summary>Collects samples and totals for tenant prefix scans.</summary>
    private sealed class IsolationScanEvidence
    {
        private const int SampleLimitPerCategory = 10;

        private readonly List<string> _classificationGapSamples = [];
        private readonly List<MarkerMismatchEvidence> _markerSamples = [];

        private readonly List<string> _wrongTypeSamples = [];

        private int _wrongTypeCount;
        private int _classificationGapCount;
        private int _missingMarkerCount;
        private int _foreignMarkerCount;

        /// <summary>Gets a value indicating whether a key could not be classified.</summary>
        public bool HasClassificationGaps => _classificationGapCount > 0;

        /// <summary>Gets a value indicating whether at least one category exceeded its display limit.</summary>
        public bool HasOmittedFindings =>
            _classificationGapCount > SampleLimitPerCategory || _missingMarkerCount > SampleLimitPerCategory
            || _foreignMarkerCount > SampleLimitPerCategory || _wrongTypeCount > SampleLimitPerCategory;

        /// <summary>Counts a non-hash syntactic key and retains a bounded sample.</summary>
        /// <param name="key">The scanned key.</param>
        /// <param name="tenantId">The tenant under investigation.</param>
        public void AddWrongType(RedisKey key, string tenantId)
        {
            if (++_wrongTypeCount <= SampleLimitPerCategory)
            {
                _wrongTypeSamples.Add($"syntactic key '{FormatKey(key)}' under tenant '{tenantId}' has the wrong Redis value type (expected a hash)");
            }
        }

        /// <summary>Gets a value indicating whether a key reference was abbreviated.</summary>
        public bool HasAbbreviatedKeys { get; private set; }

        /// <summary>Builds the operator guidance for syntactic key findings collected so far.</summary>
        /// <param name="tenantId">The requested tenant identifier.</param>
        public List<string> GetSyntacticRemediation(string tenantId)
        {
            List<string> remediation = [];
            if (_wrongTypeCount > 0)
            {
                remediation.Add("Inspect the reported non-hash keys and identify their writer before repairing tenant data.");
            }

            if (_markerSamples.Count > 0)
            {
                remediation.Add(
                    $"Inspect and quarantine the keys named in the tenantId findings. Verify their provenance before applying tenant-scoped marker repair or reindexing for tenant '{tenantId}'.");
            }

            if (HasAbbreviatedKeys)
            {
                remediation.Add("Match abbreviated key references by the SHA256 of the original Redis key bytes before applying any repair.");
            }

            if (HasOmittedFindings)
            {
                remediation.Add("Review the remaining findings in the tenant prefix before making repairs.");
            }

            return remediation;
        }

        /// <summary>Gets the retained marker-defect samples.</summary>
        public IReadOnlyList<MarkerMismatchEvidence> MarkerSamples => _markerSamples;

        /// <summary>Counts a classification gap and retains its detail within the sample limit.</summary>
        /// <param name="key">The scanned key.</param>
        /// <param name="tenantId">The tenant under investigation.</param>
        /// <param name="reason">The classification result or wrong-type diagnostic.</param>
        public void AddClassificationGap(RedisKey key, string tenantId, string reason)
        {
            if (++_classificationGapCount <= SampleLimitPerCategory)
            {
                _classificationGapSamples.Add(
                    $"Semantic key '{FormatKey(key)}' under tenant '{tenantId}' has an evidence-classification gap ({reason})");
            }
        }

        /// <summary>Counts a marker defect and retains its evidence within the category's sample limit.</summary>
        /// <param name="kind">The type of marker defect.</param>
        /// <param name="key">The scanned key.</param>
        /// <param name="tenantId">The tenant under investigation.</param>
        /// <param name="storedMarker">The marker read from the hash.</param>
        /// <param name="storageName">The semantic key family, or null for a syntactic key.</param>
        public void AddMarkerMismatch(
            MarkerDefectKind kind, RedisKey key, string tenantId, RedisValue storedMarker, string? storageName = null)
        {
            bool missing = kind == MarkerDefectKind.Missing;
            int count = missing ? ++_missingMarkerCount : ++_foreignMarkerCount;
            if (count > SampleLimitPerCategory)
            {
                return;
            }

            string displayKey = FormatKey(key);
            string marker = missing ? string.Empty : FormatEvidenceValue(((byte[]?)storedMarker)!, storedMarker.ToString());
            string detail = (storageName, missing) switch
            {
                (null, true) => $"syntactic key '{displayKey}' under tenant '{tenantId}' is missing tenantId field",
                (null, false) => $"syntactic key '{displayKey}' under tenant '{tenantId}' has tenantId field '{marker}'",
                (_, true) => $"{storageName} key '{displayKey}' under tenant '{tenantId}' is missing its tenantId marker: incomplete evidence, not confirmed cross-tenant leakage — expected tenant '{tenantId}'",
                _ => $"{storageName} key '{displayKey}' under tenant '{tenantId}' has a foreign tenantId marker '{marker}': confirmed marker mismatch (possible contamination) — expected tenant '{tenantId}', observed tenant '{marker}'",
            };
            _markerSamples.Add(new MarkerMismatchEvidence(kind, displayKey, detail));
        }

        private string FormatKey(RedisKey key)
        {
            string fullKey = key.ToString();
            string displayKey = FormatEvidenceValue(((byte[]?)key)!, fullKey);
            HasAbbreviatedKeys |= !string.Equals(displayKey, fullKey, StringComparison.Ordinal);
            return displayKey;
        }

        /// <summary>Appends retained samples and omitted counts to the check's problems.</summary>
        /// <param name="problems">The diagnostic details collected so far.</param>
        public void AppendDetailsTo(List<string> problems)
        {
            problems.AddRange(_classificationGapSamples);
            problems.AddRange(_wrongTypeSamples);
            problems.AddRange(_markerSamples.Select(mismatch => mismatch.Detail));

            AppendOmittedCount(problems, _wrongTypeCount, "non-hash");
            AppendOmittedCount(problems, _classificationGapCount, "classification-gap");
            AppendOmittedCount(problems, _missingMarkerCount, "missing-marker");
            AppendOmittedCount(problems, _foreignMarkerCount, "foreign-marker");
        }

        private static void AppendOmittedCount(List<string> problems, int count, string category)
        {
            if (count > SampleLimitPerCategory)
            {
                problems.Add($"Omitted {count - SampleLimitPerCategory} additional {category} finding(s)");
            }
        }
    }
}
