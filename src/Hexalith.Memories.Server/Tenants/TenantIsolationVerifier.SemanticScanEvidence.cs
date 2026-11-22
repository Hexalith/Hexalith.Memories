// <copyright file="TenantIsolationVerifier.SemanticScanEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

public sealed partial class TenantIsolationVerifier
{
    /// <summary>Collects samples and totals across both semantic key prefixes.</summary>
    private sealed class SemanticScanEvidence
    {
        private const int SampleLimitPerCategory = 10;

        private readonly List<string> _classificationGapSamples = [];
        private readonly List<MarkerMismatchEvidence> _markerSamples = [];

        private int _classificationGapCount;
        private int _missingMarkerCount;
        private int _foreignMarkerCount;

        /// <summary>Gets a value indicating whether a key could not be classified.</summary>
        public bool HasClassificationGaps => _classificationGapCount > 0;

        /// <summary>Gets the retained marker-defect samples.</summary>
        public IReadOnlyList<MarkerMismatchEvidence> MarkerSamples => _markerSamples;

        /// <summary>Counts a classification gap and retains its detail within the sample limit.</summary>
        /// <param name="detail">The diagnostic text for the gap.</param>
        public void AddClassificationGap(string detail)
        {
            if (++_classificationGapCount <= SampleLimitPerCategory)
            {
                _classificationGapSamples.Add(detail);
            }
        }

        /// <summary>Counts a marker defect and retains its evidence within the category's sample limit.</summary>
        /// <param name="mismatch">The observed marker defect.</param>
        public void AddMarkerMismatch(MarkerMismatchEvidence mismatch)
        {
            int count = mismatch.Kind == MarkerDefectKind.Missing
                ? ++_missingMarkerCount
                : ++_foreignMarkerCount;
            if (count <= SampleLimitPerCategory)
            {
                _markerSamples.Add(mismatch);
            }
        }

        /// <summary>Appends retained samples and omitted counts to the check's problems.</summary>
        /// <param name="problems">The diagnostic details collected so far.</param>
        public void AppendDetailsTo(List<string> problems)
        {
            problems.AddRange(_classificationGapSamples);
            problems.AddRange(_markerSamples.Select(mismatch => mismatch.Detail));

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
