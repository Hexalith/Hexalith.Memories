// <copyright file="OperationalRunbookSetTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Deployment;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Memories.Telemetry;

using Shouldly;

using MemoriesRoutes = Hexalith.Memories.Contracts.V1.MemoriesRoutes;

/// <summary>Story 26.5 — source-tied contract for the operational runbook set.</summary>
public sealed class OperationalRunbookSetTests
{
    private static readonly string[] RequiredRunbookFileNames =
    [
        "capacity-planning.md",
        "incident-response.md",
        "index-rebuild.md",
        "tenant-onboarding-offboarding.md",
        "upgrade-migration.md",
        "monitoring-alerting-thresholds.md",
    ];

    private static readonly string[] CommonHeadings =
    [
        "Purpose and scope",
        "Prerequisites and authorization",
        "Signals and evidence",
        "Procedure",
        "Verification and evidence",
        "Rollback, recovery, and stop conditions",
        "Escalation evidence",
        "Related runbooks and sources",
    ];

    private static readonly string[] NavigationAnchorPaths =
    [
        "docs/operations/deployment-configuration.md",
        "docs/operations/failure-recovery.md",
    ];

    private static readonly string[] ChangedRunbookPaths =
    [
        .. RequiredRunbookFileNames.Select(name => $"docs/operations/{name}"),
        .. NavigationAnchorPaths,
        "docs/operations/backup-restore.md",
        "docs/operations/disaster-recovery.md",
    ];

    private static readonly Regex MarkdownLinkRegex = new(
        @"\[[^\]]+\]\((?<target><[^>]+>|[^)\s]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AuthoringMarkerRegex = new(
        @"\b(?:TODO|TBD|REPLACE_ME|changeme|password123|example-password)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex StaleExecutableRegex = new(
        @"(?m)(?:\bkubectl\s+-n\s+memories\b|\bredis-0\b|statefulset/redis(?=\s|$)|\bpvc\s+(?:redis-data|falkordb-data)\b|persistentVolumeClaimName:\s+(?:redis-data|falkordb-data)\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void RequiredRunbooks_ExistAndHaveExactSharedHeadings()
    {
        RequiredRunbookFileNames.Length.ShouldBe(6);
        RequiredRunbookFileNames.Distinct(StringComparer.Ordinal).Count().ShouldBe(6);

        foreach (string fileName in RequiredRunbookFileNames)
        {
            string path = ResolveRepoPath("docs", "operations", fileName);
            File.Exists(path).ShouldBeTrue($"Required operational runbook not found at {path}");

            string[] headings = GetLevelTwoHeadings(File.ReadAllText(path));
            headings.ShouldBe(CommonHeadings, $"{fileName} must keep the exact shared Story 26.5 section contract.");
        }
    }

    [Fact]
    public void NavigationAnchors_LinkDirectlyToEveryRequiredRunbook()
    {
        foreach (string anchorPath in NavigationAnchorPaths)
        {
            string content = ReadRepoFile(anchorPath);
            string[] links = GetMarkdownLinkTargets(content).ToArray();

            foreach (string fileName in RequiredRunbookFileNames)
            {
                links.ShouldContain(
                    $"./{fileName}",
                    $"{anchorPath} must contain a direct Markdown link to {fileName}.");
            }
        }
    }

    [Fact]
    public void ChangedRunbookRelativeLinks_StayInsideRepositoryAndResolve()
    {
        string repoRoot = ResolveRepoRoot();

        foreach (string runbookPath in ChangedRunbookPaths)
        {
            string sourcePath = ResolveRepoPath(runbookPath.Split('/'));
            foreach (string target in GetMarkdownLinkTargets(File.ReadAllText(sourcePath)))
            {
                if (IsExternalOrDocumentAnchor(target))
                {
                    continue;
                }

                string pathTarget = RemoveQueryAndFragment(Uri.UnescapeDataString(target));
                if (pathTarget.Length == 0)
                {
                    continue;
                }

                Path.IsPathRooted(pathTarget).ShouldBeFalse(
                    $"Runbook link must be repository-relative: {runbookPath} -> {target}");

                string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, pathTarget));
                string relativeToRoot = Path.GetRelativePath(repoRoot, resolved);
                bool staysInRepository = !Path.IsPathRooted(relativeToRoot)
                    && !relativeToRoot.Equals("..", StringComparison.Ordinal)
                    && !relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

                staysInRepository.ShouldBeTrue(
                    $"Runbook link traverses outside the repository: {runbookPath} -> {target}");
                (File.Exists(resolved) || Directory.Exists(resolved)).ShouldBeTrue(
                    $"Runbook link target does not resolve: {runbookPath} -> {target} ({resolved})");
            }
        }
    }

    [Fact]
    public void ChangedRunbooks_HaveNoUnfinishedOrUnsafeExecutableExamples()
    {
        foreach (string runbookPath in ChangedRunbookPaths)
        {
            string content = ReadRepoFile(runbookPath);
            AuthoringMarkerRegex.IsMatch(content).ShouldBeFalse(
                $"{runbookPath} contains an unfinished or example-password authoring marker.");

            string executableExamples = GetFencedCode(content);
            StaleExecutableRegex.IsMatch(executableExamples).ShouldBeFalse(
                $"{runbookPath} contains an executable example using a stale production resource role.");
            Regex.IsMatch(
                    executableExamples,
                    @"(?im)^\s*(?:echo|printf|printenv)\b[^\r\n]*(?:PASSWORD|TOKEN)|^\s*set\s+-x\b",
                    RegexOptions.CultureInvariant)
                .ShouldBeFalse($"{runbookPath} contains an executable example that can print a secret.");
            Regex.IsMatch(
                    executableExamples,
                    "(?im)\\b(?:PASSWORD|TOKEN)\\s*=\\s*(?:[\\\"']{0,2}|changeme|password|secret|example)",
                    RegexOptions.CultureInvariant)
                .ShouldBeFalse($"{runbookPath} contains an empty or example credential assignment.");
            Regex.IsMatch(
                    executableExamples,
                    @"(?im)\b(?:memories|hexalith)\s+tenant\s+(?:create|delete|provision|remove)\b",
                    RegexOptions.CultureInvariant)
                .ShouldBeFalse($"{runbookPath} contains a nonexistent tenant lifecycle CLI example.");
            Regex.IsMatch(
                    executableExamples,
                    @"(?im)\bkubectl\b[^\r\n]*\scp\s+(?:redis-stack-0|falkordb-0):",
                    RegexOptions.CultureInvariant)
                .ShouldBeFalse($"{runbookPath} copies a live data-plane directory instead of a quiesced mount.");

            if (executableExamples.Contains("redis-cli", StringComparison.Ordinal))
            {
                Regex.IsMatch(
                        executableExamples,
                        @"\bredis-cli\s+-a\b",
                        RegexOptions.CultureInvariant)
                    .ShouldBeFalse($"redis-cli credentials must not be expanded into process arguments: {runbookPath}");
                executableExamples.ShouldContain(
                    "REDISCLI_AUTH",
                    Case.Sensitive,
                    $"Every redis-cli runbook must use the in-container REDISCLI_AUTH mechanism: {runbookPath}");
            }
        }
    }

    [Fact]
    public void BackupRecoveryRunbooks_EnforceExecutableRecoveryContracts()
    {
        string backup = ReadRepoFile("docs/operations/backup-restore.md");
        string disaster = ReadRepoFile("docs/operations/disaster-recovery.md");
        string verifier = ReadRepoFile("tools/verify-backup-recovery.py");

        foreach (string policyField in new[]
                 {
                     "RPO",
                     "RETENTION",
                     "BACKUP_DESTINATION",
                     "QUIESCE_PLAYBOOK",
                     "activeWorkflows == 0",
                 })
        {
            backup.ShouldContain(policyField, Case.Sensitive);
        }

        backup.ShouldContain("memories export tenant", Case.Sensitive);
        backup.ShouldContain("memories export case", Case.Sensitive);
        backup.ShouldContain("statusLocation", Case.Sensitive);
        backup.ShouldContain("instanceId", Case.Sensitive);
        backup.ShouldContain("Completed", Case.Sensitive);
        backup.ShouldContain("Failed|Canceled|Terminated", Case.Sensitive);
        backup.ShouldContain(".restoredMemoryUnits == $units", Case.Sensitive);
        backup.ShouldContain(".restoredCases == $cases", Case.Sensitive);
        backup.ShouldContain(".restoredEdges == $edges", Case.Sensitive);
        backup.ShouldContain(".skippedRecords == 0", Case.Sensitive);
        backup.ShouldContain("RESTORE_TARGET_NOT_CLEAN", Case.Sensitive);
        backup.ShouldContain("edgeCount + statistics.memoryUnitCount", Case.Sensitive);
        backup.ShouldContain("tools/verify-backup-recovery.py", Case.Sensitive);

        disaster.ShouldContain("name: data-redis-stack-0", Case.Sensitive);
        disaster.ShouldContain("name: data-falkordb-0", Case.Sensitive);
        disaster.ShouldContain("apiGroup: snapshot.storage.k8s.io", Case.Sensitive);
        disaster.ShouldContain("case_id=", Case.Sensitive);
        disaster.ShouldContain("/cases/$case_id/import", Case.Sensitive);
        disaster.ShouldContain("statusLocation", Case.Sensitive);
        disaster.ShouldContain("instanceId", Case.Sensitive);
        disaster.ShouldContain(".restoredMemoryUnits == $units", Case.Sensitive);
        disaster.ShouldContain(".restoredCases == $cases", Case.Sensitive);
        disaster.ShouldContain(".restoredEdges == $edges", Case.Sensitive);
        disaster.ShouldContain("SkippedRecords", Case.Insensitive);
        disaster.ShouldContain("tenant-onboarding-offboarding.md#onboarding", Case.Sensitive);

        verifier.ShouldContain("exportedEdgeCount", Case.Sensitive);
        verifier.ShouldContain("expected_graph_edges", Case.Sensitive);
        verifier.ShouldContain("REDISCLI_AUTH", Case.Sensitive);
        verifier.ShouldNotContain("redis-cli -a", Case.Sensitive);
    }

    [Fact]
    public void Runbooks_AreTiedToCanonicalOperationalSources()
    {
        string productionKustomization = ReadRepoFile("deploy/kubernetes/overlays/production/kustomization.yaml");
        Match namespaceMatch = Regex.Match(
            productionKustomization,
            @"(?m)^namespace:\s*(?<value>\S+)\s*$",
            RegexOptions.CultureInvariant);
        namespaceMatch.Success.ShouldBeTrue("Production Kustomize overlay must declare a namespace.");
        string productionNamespace = namespaceMatch.Groups["value"].Value;
        productionNamespace.ShouldBe("hexalith-memories");

        string backupDoc = ReadRepoFile("docs/operations/backup-restore.md");
        string disasterDoc = ReadRepoFile("docs/operations/disaster-recovery.md");
        backupDoc.ShouldContain(productionNamespace, Case.Sensitive);
        disasterDoc.ShouldContain(productionNamespace, Case.Sensitive);

        string redisManifest = ReadRepoFile("deploy/kubernetes/base/redis-statefulset.yaml");
        string falkorManifest = ReadRepoFile("deploy/kubernetes/base/falkordb-statefulset.yaml");
        redisManifest.ShouldContain("name: redis-stack", Case.Sensitive);
        falkorManifest.ShouldContain("name: falkordb", Case.Sensitive);
        foreach (string resourceName in new[] { "redis-stack-0", "falkordb-0", "data-redis-stack-0", "data-falkordb-0" })
        {
            (backupDoc + disasterDoc).ShouldContain(
                resourceName,
                Case.Sensitive,
                $"Recovery docs must publish the manifest-derived production resource name {resourceName}.");
        }

        string tenantDoc = ReadRepoFile("docs/operations/tenant-onboarding-offboarding.md");
        foreach (string route in new[]
                 {
                     MemoriesRoutes.Tenants,
                     MemoriesRoutes.Tenant,
                     MemoriesRoutes.TenantProvisionStatus,
                     MemoriesRoutes.TenantDeletionStatus,
                     MemoriesRoutes.TenantVerify,
                 })
        {
            tenantDoc.ShouldContain(route, Case.Sensitive, $"Tenant runbook must publish canonical MemoriesRoutes value {route}.");
        }

        string monitoringDoc = ReadRepoFile("docs/operations/monitoring-alerting-thresholds.md");
        foreach (string metric in new[]
                 {
                     MemoriesMeter.IngestionDocumentsName,
                     MemoriesMeter.IngestionFailuresName,
                     MemoriesMeter.SearchDurationName,
                     MemoriesMeter.IndexSizeName,
                     MemoriesMeter.PipelineQueueDepthName,
                     MemoriesMeter.RateLimitRejectionsName,
                     MemoriesMeter.HandlerMismatchesName,
                     MemoriesMeter.ObservationsDroppedName,
                 })
        {
            monitoringDoc.ShouldContain(metric, Case.Sensitive, $"Monitoring runbook must publish emitted metric {metric}.");
        }

        string dashboardPath = ResolveRepoPath("deploy", "grafana", "dashboards", "memories-operability.json");
        File.Exists(dashboardPath).ShouldBeTrue($"Committed operability dashboard not found at {dashboardPath}");
        File.ReadAllText(dashboardPath).ShouldContain(
            MemoriesMeter.IndexSizeName.Replace('.', '_'),
            Case.Sensitive,
            "Dashboard must query the Prometheus-normalized index-size metric.");

        string serviceDefaults = ReadRepoFile("src/Hexalith.Memories.ServiceDefaults/Extensions.cs");
        serviceDefaults.ShouldContain(
            "[HealthStatus.Degraded] = StatusCodes.Status200OK",
            Case.Sensitive,
            "Health source must retain the documented Degraded-to-200 mapping.");
        string healthDoc = ReadRepoFile("docs/dev/health-checks.md");
        healthDoc.ShouldContain("Degraded` → 200", Case.Sensitive);
        ReadRepoFile("docs/operations/incident-response.md").ShouldContain("Degraded` returns HTTP 200", Case.Sensitive);
        monitoringDoc.ShouldContain("`Degraded` can be HTTP 200", Case.Sensitive);
    }

    [Fact]
    public void ReviewHardenedRunbooks_KeepLifecycleRolloutAndAlertGates()
    {
        string tenantDoc = ReadRepoFile("docs/operations/tenant-onboarding-offboarding.md");
        Regex.Matches(tenantDoc, Regex.Escape("test \"$HTTP_STATUS\" = 202"), RegexOptions.CultureInvariant)
            .Count.ShouldBe(2);
        Regex.Matches(tenantDoc, "RETURNED_LOCATION", RegexOptions.CultureInvariant)
            .Count.ShouldBeGreaterThanOrEqualTo(6);
        Regex.Matches(tenantDoc, "POLL_DEADLINE_EPOCH=", RegexOptions.CultureInvariant)
            .Count.ShouldBe(2);
        tenantDoc.ShouldContain("dedup:{tenantId}:*", Case.Sensitive);
        tenantDoc.ShouldContain("{tenantId}:vec:nl:*", Case.Sensitive);
        tenantDoc.ShouldNotContain("drain or record", Case.Sensitive);
        tenantDoc.ShouldContain("isolated recovery environment", Case.Sensitive);
        tenantDoc.ShouldContain("CompensationFailed", Case.Sensitive);

        string deletionActivity = ReadRepoFile(
            "src/Hexalith.Memories.Server/Activities/Tenants/DeleteTenantDataKeysActivity.cs");
        deletionActivity.ShouldContain("$\"dedup:{input.TenantId}:*\"", Case.Sensitive);
        deletionActivity.ShouldContain("GetLegacyNaturalLanguageSemanticKeyPrefix", Case.Sensitive);

        string upgradeDoc = ReadRepoFile("docs/operations/upgrade-migration.md");
        upgradeDoc.ShouldContain("PREVIOUS_RENDER_SHA256", Case.Sensitive);
        upgradeDoc.ShouldContain("--dry-run=server", Case.Sensitive);
        upgradeDoc.ShouldContain("KUBECTL_DIFF_RC", Case.Sensitive);
        upgradeDoc.ShouldContain("--timeout=\"$ROLLOUT_TIMEOUT\"", Case.Sensitive);
        upgradeDoc.ShouldContain("complete production render is an all-tenant environment change", Case.Sensitive);
        upgradeDoc.ShouldContain("kubectl apply` is not atomic", Case.Sensitive);
        upgradeDoc.ShouldContain("target runtime/backend versions", Case.Sensitive);
        upgradeDoc.ShouldContain("target data layout", Case.Sensitive);
        upgradeDoc.ShouldContain("reapply the complete", Case.Sensitive);
        upgradeDoc.ShouldContain("`PREVIOUS_RENDER` as a stateless rollback", Case.Sensitive);
        upgradeDoc.ShouldNotContain("structured healthy or explicitly accepted capability state", Case.Sensitive);

        string monitoringDoc = ReadRepoFile("docs/operations/monitoring-alerting-thresholds.md");
        monitoringDoc.ShouldContain("before workflow completion/indexing", Case.Sensitive);
        monitoringDoc.ShouldContain("multiply by 60", Case.Sensitive);
        monitoringDoc.ShouldContain("no accepted/total-request counter", Case.Sensitive);
        monitoringDoc.ShouldContain("EventId 9153", Case.Sensitive);
        monitoringDoc.ShouldContain("EventId 9180", Case.Sensitive);
        monitoringDoc.ShouldContain("access-controlled incident record", Case.Sensitive);

        string alertMatrix = monitoringDoc[
            monitoringDoc.IndexOf("| Signal / class |", StringComparison.Ordinal)..
            monitoringDoc.IndexOf("The application counter", StringComparison.Ordinal)];
        string[] alertRows = alertMatrix
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Where(static line => line.StartsWith("| ", StringComparison.Ordinal)
                && !line.StartsWith("| Signal / class |", StringComparison.Ordinal)
                && !line.StartsWith("|---", StringComparison.Ordinal))
            .ToArray();
        alertRows.Length.ShouldBe(18);
        foreach (string row in alertRows)
        {
            row.ShouldContain("](", Case.Sensitive, "Every recommended alert row must link its response procedure.");
        }
    }

    private static string[] GetLevelTwoHeadings(string content)
    {
        var headings = new List<string>();
        bool insideFence = false;
        bool insideComment = false;

        foreach (string originalLine in content.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = originalLine.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                continue;
            }

            string visibleLine = RemoveHtmlComments(originalLine, ref insideComment);
            if (!insideFence && visibleLine.StartsWith("## ", StringComparison.Ordinal))
            {
                headings.Add(visibleLine[3..].Trim());
            }
        }

        return [.. headings];
    }

    private static IEnumerable<string> GetMarkdownLinkTargets(string content)
    {
        bool insideFence = false;
        bool insideComment = false;

        foreach (string originalLine in content.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = originalLine.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                continue;
            }

            string visibleLine = RemoveHtmlComments(originalLine, ref insideComment);
            if (insideFence || visibleLine.Length == 0)
            {
                continue;
            }

            foreach (Match match in MarkdownLinkRegex.Matches(visibleLine))
            {
                string target = match.Groups["target"].Value;
                yield return target.Length >= 2 && target[0] == '<' && target[^1] == '>'
                    ? target[1..^1]
                    : target;
            }
        }
    }

    private static string GetFencedCode(string content)
    {
        var lines = new List<string>();
        bool insideFence = false;

        foreach (string line in content.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                continue;
            }

            if (insideFence)
            {
                lines.Add(line);
            }
        }

        return string.Join('\n', lines);
    }

    private static string RemoveHtmlComments(string line, ref bool insideComment)
    {
        string remaining = line;
        string visible = string.Empty;

        while (remaining.Length > 0)
        {
            if (insideComment)
            {
                int end = remaining.IndexOf("-->", StringComparison.Ordinal);
                if (end < 0)
                {
                    return visible;
                }

                insideComment = false;
                remaining = remaining[(end + 3)..];
                continue;
            }

            int start = remaining.IndexOf("<!--", StringComparison.Ordinal);
            if (start < 0)
            {
                return visible + remaining;
            }

            visible += remaining[..start];
            insideComment = true;
            remaining = remaining[(start + 4)..];
        }

        return visible;
    }

    private static bool IsExternalOrDocumentAnchor(string target)
    {
        if (target.StartsWith('#'))
        {
            return true;
        }

        return Uri.TryCreate(target, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase));
    }

    private static string RemoveQueryAndFragment(string target)
    {
        int delimiter = target.IndexOfAny(['?', '#']);
        return delimiter < 0 ? target : target[..delimiter];
    }

    private static string ReadRepoFile(string relativePath)
    {
        string path = ResolveRepoPath(relativePath.Split('/'));
        File.Exists(path).ShouldBeTrue($"Repository file not found at {path}");
        return File.ReadAllText(path);
    }

    private static string ResolveRepoPath(params string[] segments)
    {
        string[] parts = new string[segments.Length + 1];
        parts[0] = ResolveRepoRoot();
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    private static string ResolveRepoRoot()
    {
        string candidate = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(candidate, "Hexalith.Memories.slnx")))
            {
                return candidate;
            }

            candidate = Path.GetFullPath(Path.Combine(candidate, ".."));
        }

        return AppContext.BaseDirectory;
    }
}
