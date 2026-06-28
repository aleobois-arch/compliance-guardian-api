namespace ComplianceGuardian.Api;

// ── Enums ────────────────────────────────────────────────────────────────────

public enum RiskLevel { Low, Medium, High, Critical }

// ── Request / Response ───────────────────────────────────────────────────────

/// <summary>POST /analyze — analyze a contract or compliance question.</summary>
public sealed record AnalyzeRequest
{
    /// <summary>Free-text compliance question (optional if DocumentText is provided).</summary>
    public string? Question { get; init; }

    /// <summary>Plain-text content of the contract or policy document to analyze.</summary>
    public string? DocumentText { get; init; }

    /// <summary>Logical name of the document (e.g. "Vendor-NDA-2026.pdf").</summary>
    public string? DocumentName { get; init; }

    /// <summary>Organization name — used to contextualize the analysis.</summary>
    public string Organization { get; init; } = "Unnamed Organization";

    /// <summary>Optional list of applicable policies (e.g. ["GDPR", "SOC 2"]).</summary>
    public IReadOnlyList<string> Policies { get; init; } = Array.Empty<string>();
}

public sealed record AnalyzeResponse
{
    public string DocumentName { get; init; } = default!;
    public string Organization { get; init; } = default!;
    public RiskLevel OverallRisk { get; init; }
    public string Summary { get; init; } = default!;
    public IReadOnlyList<RiskFinding> Findings { get; init; } = Array.Empty<RiskFinding>();
    public IReadOnlyList<ComplianceGap> Gaps { get; init; } = Array.Empty<ComplianceGap>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ActionItems { get; init; } = Array.Empty<string>();
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record RiskFinding
{
    public string Category { get; init; } = default!;
    public RiskLevel Level { get; init; }
    public string Description { get; init; } = default!;
    public string BusinessImpact { get; init; } = default!;
}

public sealed record ComplianceGap
{
    public string Policy { get; init; } = default!;
    public string MissingClause { get; init; } = default!;
    public string Detail { get; init; } = default!;
    public RiskLevel Severity { get; init; }
}

// ── Health ────────────────────────────────────────────────────────────────────

public sealed record HealthResponse(string Status, string Version, DateTimeOffset Timestamp);
