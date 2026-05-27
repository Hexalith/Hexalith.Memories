// Deterministic corpus generation tool for benchmark suite.
// Generates synthetic-corpus.json with memory units (including 768-dim vectors) and graph edges.
// All randomness uses seeded PRNG for reproducibility (NFR26).

using System.Text.Json;
using System.Text.Json.Serialization;

const int VectorDim = 768;
const int Seed = 42;

// --- Cluster definitions ---
// 0=payment, 1=deployment, 2=architecture, 3=investigation, 4=monitoring, 5=discussion, 6=postmortem
int[] clusterAssignments = [
    0, 0, 0, 0, 0,   // mu-001..005: payment
    1, 1, 1, 1, 1,   // mu-006..010: deployment
    2, 2, 2, 2, 2,   // mu-011..015: architecture
    3, 3, 3, 3, 3,   // mu-016..020: investigation
    4, 4, 4, 4, 4,   // mu-021..025: monitoring
    5, 5, 5, 5, 5,   // mu-026..030: discussion
    6, 6, 6, 6, 6,   // mu-031..035: postmortem
];

// --- Memory unit definitions ---
var units = new (string Id, string Content, string SourceUri, string SourceType)[]
{
    // Payment cluster (mu-001..005)
    ("mu-001", "Payment processing service experienced intermittent failures starting March 15th. Transaction success rate dropped from 99.8% to 94.2%. The payment gateway returned HTTP 504 timeout errors for requests exceeding 3 seconds. Root cause traced to database connection pool exhaustion caused by long-running queries introduced in the March deployment.", "file:///incidents/INC-2024-0315.md", "file"),
    ("mu-002", "Payment rejection rates spiked to 5.8% following the March 15th deployment. Customers reported declined transactions despite valid payment methods. The claim denied responses were triggered by the new fraud detection module that had overly aggressive thresholds. Rollback of fraud rules restored normal approval rates within 2 hours.", "file:///incidents/INC-2024-0315-payments.md", "file"),
    ("mu-003", "Database timeout analysis for the payment processing outage. Connection pool was configured for 50 max connections but the new reporting queries held connections for 15+ seconds each. Peak load generated 120 concurrent connection requests. The pool_exhaustion metric showed 100% utilization starting at 09:15 UTC on March 15th.", "file:///analysis/payment-db-timeouts.md", "file"),
    ("mu-004", "Payment service API latency measurements during the March incident. P99 latency increased from 450ms to 12,400ms. The payment processing endpoint /api/v2/transactions/process was the primary bottleneck. Connection pool saturation caused cascading timeouts across all payment-related endpoints.", "file:///metrics/payment-latency-march.md", "file"),
    ("mu-005", "Invoice generation module performance report. The batch invoice processing runs nightly at 02:00 UTC and completes within 45 minutes. No impact from the March deployment. The invoice module uses a separate database connection pool with 10 max connections, isolated from the main payment processing pool.", "file:///reports/invoice-performance.md", "file"),

    // Deployment cluster (mu-006..010)
    ("mu-006", "March deployment release notes v2.4.1. Changes include: updated fraud detection rules, new reporting dashboard queries, database index optimization for customer lookup, and API rate limiter configuration changes. Deployed to production at 08:00 UTC on March 15th via automated CI/CD pipeline.", "file:///releases/v2.4.1-notes.md", "file"),
    ("mu-007", "Deployment rollback procedure executed at 11:30 UTC on March 15th. The v2.4.1 release was rolled back to v2.4.0 after identifying the connection pool exhaustion issue. Rollback completed in 12 minutes. Service recovery confirmed at 11:45 UTC when transaction success rate returned to 99.7%.", "file:///deployments/rollback-2024-0315.md", "file"),
    ("mu-008", "CI/CD pipeline configuration for the payments microservice. Uses GitHub Actions with staging and production environments. Staging deployment triggers automated integration tests including payment flow validation. Production deployment requires manual approval from the on-call engineer.", "file:///devops/ci-cd-payments.md", "file"),
    ("mu-009", "Infrastructure change log for March 2024. Database server upgraded from 16GB to 32GB RAM on March 10th. Redis cache cluster scaled from 3 to 5 nodes on March 12th. Load balancer health check interval reduced from 30s to 10s on March 14th. No infrastructure changes were made between March 15-20.", "file:///infra/change-log-march.md", "file"),
    ("mu-010", "Deployment verification checklist for production releases. Steps include: run smoke tests, verify health endpoints, check error rate baseline, monitor P99 latency for 15 minutes, validate database connection pool metrics, and confirm no alert threshold breaches. The March 15th deployment passed all automated checks but the connection pool issue manifested under sustained load 75 minutes post-deployment.", "file:///devops/deployment-checklist.md", "file"),

    // Architecture cluster (mu-011..015)
    ("mu-011", "Architecture Decision Record ADR-042: API Redesign for Payment Processing. Decision: migrate from monolithic REST API to microservices architecture with dedicated payment, fraud detection, and reporting services. Rationale: the current monolith shares database connections across all modules, creating single-point-of-failure risk. Timeline: Q2 2024.", "file:///adrs/ADR-042-api-redesign.md", "file"),
    ("mu-012", "Architecture Decision Record ADR-038: Database Connection Pooling Strategy. Decision: implement per-service connection pools with configurable limits. Each microservice maintains its own pool with dedicated max connection count. Monitoring via Prometheus connection_pool_utilization gauge. This ADR was drafted in response to the March 15th incident.", "file:///adrs/ADR-038-connection-pooling.md", "file"),
    ("mu-013", "System architecture overview for the payments platform. Components: API Gateway, Payment Service, Fraud Detection Service, Reporting Service, PostgreSQL primary with read replicas, Redis cache layer, RabbitMQ message broker. All services communicate via REST APIs with circuit breakers. The architectural changes proposed in ADR-042 aim to decouple these services.", "file:///docs/architecture-overview.md", "file"),
    ("mu-014", "Technical specification for the new rate limiting middleware. Implements token bucket algorithm with configurable per-endpoint limits. Default: 100 requests/second for read endpoints, 20 requests/second for write endpoints. The rate limiter was deployed in v2.4.1 but was not related to the outage.", "file:///specs/rate-limiter-spec.md", "file"),
    ("mu-015", "Database migration plan for the microservices transition. Phase 1: split reporting queries to a dedicated read replica. Phase 2: move fraud detection to its own database. Phase 3: establish per-service connection pools. The March 15th incident accelerated Phase 3 priority from Q3 to Q2.", "file:///docs/db-migration-plan.md", "file"),

    // Investigation cluster (mu-016..020)
    ("mu-016", "Investigation timeline for the March 15th payment outage. 08:00 - v2.4.1 deployed. 09:15 - First connection pool exhaustion alerts. 09:30 - On-call engineer paged. 10:00 - Root cause identified as new reporting queries. 10:30 - Hotfix attempted (increase pool size to 100). 11:00 - Hotfix insufficient, rollback decided. 11:30 - Rollback executed. 11:45 - Service restored.", "file:///investigations/timeline-2024-0315.md", "file"),
    ("mu-017", "Service disruption impact analysis. During the 2.5-hour outage window: 12,847 transactions failed, estimated revenue impact $1.2M, 3,200 unique customers affected. Customer support received 487 tickets. SLA breach: 99.9% monthly availability target violated (actual: 99.65% for March).", "file:///investigations/impact-analysis.md", "file"),
    ("mu-018", "Database query analysis during the incident. The new reporting dashboard introduced 3 queries that performed full table scans on the transactions table (280M rows). Query execution time: 8-22 seconds each. These queries were designed for the read replica but were accidentally pointed at the primary database in the deployment configuration.", "file:///investigations/query-analysis.md", "file"),
    ("mu-019", "Connection pool monitoring data from the March 15th incident. Pool size: 50 (configured). Active connections at peak: 50 (100% utilization). Pending requests in queue: 70+. Average connection hold time jumped from 50ms to 15,000ms. The pool_wait_timeout metric showed 95th percentile at 8,200ms.", "file:///investigations/pool-metrics.md", "file"),
    ("mu-020", "Root cause analysis summary. Primary cause: reporting queries executing on primary database instead of read replica due to misconfigured connection string in v2.4.1. Contributing factor: connection pool sized for OLTP workload (50 connections) could not absorb OLAP-style queries. No circuit breaker between reporting module and payment processing module.", "file:///investigations/root-cause.md", "file"),

    // Monitoring cluster (mu-021..025)
    ("mu-021", "Grafana dashboard configuration for payment service monitoring. Panels: transaction success rate (target >99.5%), P99 latency (target <500ms), database connection pool utilization (alert >80%), error rate by endpoint, active connections over time. Alert rules trigger PagerDuty notifications when thresholds are breached.", "file:///monitoring/grafana-payments.json", "file"),
    ("mu-022", "Alert rule definitions for the payment processing service. Critical alerts: transaction success rate <98% for 5 minutes, P99 latency >2000ms for 3 minutes, connection pool utilization >90% for 2 minutes. Warning alerts: success rate <99.5% for 10 minutes, P99 latency >1000ms for 5 minutes. The March 15th incident triggered both critical alerts at 09:15 UTC.", "file:///monitoring/alert-rules.yaml", "file"),
    ("mu-023", "Prometheus metrics endpoint documentation. Exposed metrics: http_request_duration_seconds, payment_transaction_total (labels: status, method), db_connection_pool_active, db_connection_pool_idle, db_connection_pool_wait_total. Scrape interval: 15 seconds. Retention: 30 days.", "file:///monitoring/prometheus-metrics.md", "file"),
    ("mu-024", "On-call runbook for payment service incidents. Step 1: Check Grafana dashboard for anomalies. Step 2: Verify database connectivity. Step 3: Check connection pool metrics. Step 4: Review recent deployments. Step 5: If connection pool exhausted, consider increasing pool size as temporary mitigation. Step 6: Escalate to database team if queries are the root cause.", "file:///runbooks/payment-oncall.md", "file"),
    ("mu-025", "Monthly SLA compliance report for February 2024. Uptime: 99.97%. Transaction success rate: 99.82%. P99 latency: 380ms. Zero SLA breaches. Zero critical incidents. All metrics within target ranges. This represents the baseline before the March deployment.", "file:///reports/sla-february.md", "file"),

    // Discussion cluster (mu-026..030)
    ("mu-026", "Team discussion thread: Should we split the reporting module into a separate service? Alice: The March incident proves shared connection pools are dangerous. Bob: Agree, but splitting increases operational complexity. Carol: We can start by just pointing reporting at the read replica. Dave: That's a band-aid. ADR-042 proposes the right long-term solution.", "file:///discussions/reporting-split-thread.md", "discussion"),
    ("mu-027", "Code review comments on PR #847: Add new reporting dashboard queries. Reviewer note: These queries lack pagination and will scan the entire transactions table. Author response: We plan to add pagination in the next sprint. Reviewer: This should be a blocking requirement, not a follow-up. The PR was merged without pagination on March 14th.", "file:///reviews/PR-847-comments.md", "file"),
    ("mu-028", "Architecture review meeting notes, March 22nd. Attendees: Alice, Bob, Carol, Dave, Eve. Discussed: lessons from March 15th outage, ADR-042 timeline acceleration, per-service connection pool implementation, read replica routing for all reporting queries. Decision: fast-track ADR-038 (connection pooling) to April, keep ADR-042 (full API redesign) for Q2.", "file:///meetings/arch-review-0322.md", "discussion"),
    ("mu-029", "Slack channel #payments-team discussion about the fraud detection threshold tuning. The new thresholds introduced in v2.4.1 were too aggressive, causing legitimate transactions to be flagged. Team agreed to revert to previous thresholds and implement gradual rollout with A/B testing for future changes.", "file:///discussions/fraud-threshold-slack.md", "discussion"),
    ("mu-030", "Sprint retrospective notes for Sprint 24-06. What went well: quick incident response time (15 min to identify root cause). What went wrong: insufficient load testing of reporting queries, missing pagination guardrails, connection pool sized for OLTP only. Action items: add load testing to CI/CD, implement connection pool monitoring alerts, update deployment checklist.", "file:///retrospectives/sprint-24-06.md", "discussion"),

    // Postmortem cluster (mu-031..035)
    ("mu-031", "Post-mortem document for the March 15th payment processing outage. Incident severity: SEV-1. Duration: 2 hours 45 minutes. Impact: 12,847 failed transactions, $1.2M estimated revenue impact. Root cause: new reporting queries exhausted the shared database connection pool. Contributing factors: missing query pagination, OLTP-sized pool, no circuit breaker. Remediation: rollback to v2.4.0.", "file:///postmortems/PM-2024-0315.md", "file"),
    ("mu-032", "Post-mortem action items from the March 15th incident. Item 1: Implement per-service connection pools (owner: Dave, deadline: April 15). Item 2: Add query execution time limits (owner: Alice, deadline: April 1). Item 3: Route all reporting queries to read replica (owner: Carol, deadline: March 25). Item 4: Add connection pool utilization to deployment checklist (owner: Bob, deadline: March 20).", "file:///postmortems/PM-2024-0315-actions.md", "file"),
    ("mu-033", "Lessons learned from the March 15th outage. Key insight: deployment verification checks passed because they only test for 15 minutes under normal load. The connection pool issue manifested after 75 minutes of sustained load. Recommendation: extend monitoring window to 60 minutes for deployments that modify database queries. Also: require query plan review for any PR that adds or modifies SQL queries.", "file:///postmortems/lessons-learned-0315.md", "file"),
    ("mu-034", "Post-incident review: effectiveness of the rollback procedure. The rollback from v2.4.1 to v2.4.0 completed in 12 minutes. Zero data loss confirmed. Database state was consistent post-rollback. The automated rollback pipeline worked as designed. Improvement: add automatic rollback trigger when connection pool utilization exceeds 95% for more than 5 minutes.", "file:///postmortems/rollback-review.md", "file"),
    ("mu-035", "Quarterly incident trend analysis Q1 2024. Total incidents: 3 (1 SEV-1, 1 SEV-2, 1 SEV-3). The March 15th payment outage was the only SEV-1. MTTR improved from Q4 average of 4.2 hours to 2.1 hours. Incident frequency decreased from 5 (Q4) to 3 (Q1). Largest contributor to downtime: database-related issues (78% of total downtime minutes).", "file:///reports/incident-trends-q1.md", "file"),
};

// --- Graph edges ---
// Format: (sourceId, targetId, edgeType, confidence, origin)
var edges = new (string SourceId, string TargetId, string EdgeType, float Confidence, string Origin)[]
{
    // Causal chain: deployment → outage → investigation → fix
    ("mu-006", "mu-001", "causedBy", 0.95f, "explicit"),      // deployment caused payment outage
    ("mu-006", "mu-002", "causedBy", 0.90f, "explicit"),      // deployment caused payment rejections
    ("mu-001", "mu-003", "causedBy", 0.92f, "explicit"),      // outage caused by db timeouts
    ("mu-003", "mu-019", "correlatedWith", 0.88f, "explicit"), // db timeouts correlated with pool metrics
    ("mu-001", "mu-016", "causedBy", 0.90f, "explicit"),      // outage triggered investigation

    // Investigation references
    ("mu-016", "mu-017", "correlatedWith", 0.85f, "explicit"), // timeline correlated with impact analysis
    ("mu-016", "mu-018", "correlatedWith", 0.87f, "explicit"), // timeline references query analysis
    ("mu-016", "mu-019", "correlatedWith", 0.88f, "explicit"), // timeline references pool metrics
    ("mu-018", "mu-020", "causedBy", 0.93f, "explicit"),      // query analysis → root cause
    ("mu-019", "mu-020", "causedBy", 0.91f, "explicit"),      // pool metrics → root cause

    // Architecture references
    ("mu-011", "mu-001", "references", 0.85f, "explicit"),    // ADR-042 references the incident
    ("mu-012", "mu-001", "references", 0.88f, "explicit"),    // ADR-038 drafted due to incident
    ("mu-012", "mu-020", "references", 0.90f, "explicit"),    // ADR-038 references root cause
    ("mu-013", "mu-011", "references", 0.80f, "explicit"),    // overview references ADR-042
    ("mu-015", "mu-012", "references", 0.82f, "explicit"),    // migration plan references ADR-038

    // Deployment chain
    ("mu-010", "mu-006", "references", 0.80f, "explicit"),    // checklist references deployment
    ("mu-007", "mu-006", "causedBy", 0.95f, "explicit"),      // rollback caused by deployment
    ("mu-007", "mu-001", "references", 0.88f, "explicit"),    // rollback references outage

    // Monitoring references
    ("mu-022", "mu-001", "references", 0.82f, "explicit"),    // alerts triggered by outage
    ("mu-021", "mu-004", "references", 0.78f, "explicit"),    // dashboard shows latency data
    ("mu-024", "mu-022", "references", 0.80f, "explicit"),    // runbook references alert rules
    ("mu-025", "mu-004", "correlatedWith", 0.75f, "inferred"),// Feb SLA baseline vs March metrics

    // Discussion references
    ("mu-026", "mu-011", "references", 0.85f, "explicit"),    // discussion references ADR-042
    ("mu-026", "mu-020", "references", 0.80f, "explicit"),    // discussion references root cause
    ("mu-027", "mu-018", "references", 0.90f, "explicit"),    // code review about the bad queries
    ("mu-027", "mu-006", "references", 0.85f, "explicit"),    // code review is part of deployment
    ("mu-028", "mu-011", "references", 0.88f, "explicit"),    // meeting discusses ADR-042
    ("mu-028", "mu-012", "references", 0.85f, "explicit"),    // meeting discusses ADR-038
    ("mu-029", "mu-002", "references", 0.82f, "explicit"),    // fraud discussion references rejections
    ("mu-030", "mu-001", "references", 0.80f, "explicit"),    // retrospective references outage

    // Postmortem references
    ("mu-031", "mu-001", "references", 0.95f, "explicit"),    // postmortem about the outage
    ("mu-031", "mu-020", "references", 0.92f, "explicit"),    // postmortem references root cause
    ("mu-031", "mu-006", "references", 0.90f, "explicit"),    // postmortem references deployment
    ("mu-032", "mu-031", "references", 0.95f, "explicit"),    // action items from postmortem
    ("mu-032", "mu-012", "references", 0.85f, "explicit"),    // actions reference ADR-038
    ("mu-033", "mu-031", "references", 0.90f, "explicit"),    // lessons from postmortem
    ("mu-033", "mu-010", "references", 0.82f, "explicit"),    // lessons reference deployment checklist
    ("mu-034", "mu-007", "references", 0.88f, "explicit"),    // rollback review references rollback
    ("mu-034", "mu-031", "references", 0.85f, "explicit"),    // rollback review references postmortem
    ("mu-035", "mu-001", "references", 0.80f, "explicit"),    // trend analysis references incident
    ("mu-035", "mu-031", "references", 0.78f, "explicit"),    // trend analysis references postmortem

    // Cross-cluster causal connections
    ("mu-020", "mu-011", "causedBy", 0.85f, "explicit"),      // root cause accelerated API redesign
    ("mu-020", "mu-015", "causedBy", 0.83f, "explicit"),      // root cause accelerated db migration

    // Contains edges (case → memory units) — all in the same case
    // Handled at seeding level, not needed in corpus edges
};

// --- Generate vectors ---
Random rng = new(Seed);
const int ClusterCount = 7;
const float PerturbationScale = 0.005f;

float[][] baseVectors = new float[ClusterCount][];
for (int c = 0; c < ClusterCount; c++)
{
    float[] vec = new float[VectorDim];
    for (int i = 0; i < VectorDim; i++)
    {
        vec[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
    }
    Normalize(vec);
    baseVectors[c] = vec;
}

Dictionary<string, float[]> vectors = new();
for (int idx = 0; idx < units.Length; idx++)
{
    int cluster = clusterAssignments[idx];
    float[] baseVec = baseVectors[cluster];
    float[] vec = new float[VectorDim];
    for (int i = 0; i < VectorDim; i++)
    {
        float perturbation = (float)(rng.NextDouble() * 2.0 - 1.0) * PerturbationScale;
        vec[i] = baseVec[i] + perturbation;
    }
    Normalize(vec);
    vectors[units[idx].Id] = vec;
}

// --- Build corpus JSON ---
const string TenantId = "benchmark-tenant";
const string CaseId = "case-incident-march";

var memoryUnits = new List<object>();
for (int idx = 0; idx < units.Length; idx++)
{
    var u = units[idx];
    memoryUnits.Add(new
    {
        id = u.Id,
        content = u.Content,
        sourceUri = u.SourceUri,
        sourceType = u.SourceType,
        tenantId = TenantId,
        caseId = CaseId,
        vector = RoundAndRenormalize(vectors[u.Id]),
    });
}

var edgeList = edges.Select(e => new
{
    sourceId = e.SourceId,
    targetId = e.TargetId,
    edgeType = e.EdgeType,
    confidence = e.Confidence,
    origin = e.Origin,
}).ToList();

var corpus = new
{
    memoryUnits,
    edges = edgeList,
};

JsonSerializerOptions options = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
};

string json = JsonSerializer.Serialize(corpus, options);
Console.WriteLine(json);

static void Normalize(float[] vec)
{
    double norm = 0.0;
    for (int i = 0; i < vec.Length; i++)
    {
        norm += vec[i] * (double)vec[i];
    }
    norm = Math.Sqrt(norm);
    if (norm > 0)
    {
        for (int i = 0; i < vec.Length; i++)
        {
            vec[i] = (float)(vec[i] / norm);
        }
    }
}

static double[] RoundAndRenormalize(float[] vec)
{
    double[] rounded = vec.Select(v => Math.Round(v, 6)).ToArray();
    double norm = 0.0;
    for (int i = 0; i < rounded.Length; i++)
    {
        norm += rounded[i] * rounded[i];
    }
    norm = Math.Sqrt(norm);
    if (norm > 0)
    {
        for (int i = 0; i < rounded.Length; i++)
        {
            rounded[i] /= norm;
        }
    }
    return rounded;
}
