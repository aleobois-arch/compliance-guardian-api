namespace ComplianceGuardian.Api;

/// <summary>
/// Core compliance analysis logic.
/// Performs heuristic contract analysis without external dependencies.
/// Replace <see cref="AnalyzeAsync"/> with an LLM call (Azure OpenAI, etc.) for production use.
/// </summary>
public sealed class ComplianceService
{
    // ── Keyword dictionaries ──────────────────────────────────────────────────

    private static readonly Dictionary<string, (RiskLevel Level, string Category, string Impact)> RiskKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["unlimited liability"]          = (RiskLevel.Critical, "Liability",       "Uncapped financial exposure for the organization."),
        ["indemnify and hold harmless"]  = (RiskLevel.High,     "Indemnification", "Broad indemnification obligation may expose the organization to third-party claims."),
        ["perpetual license"]            = (RiskLevel.High,     "IP / Licensing",  "Perpetual rights granted without revenue share or termination right."),
        ["automatic renewal"]            = (RiskLevel.Medium,   "Contract Term",   "Contract renews automatically; may be missed by procurement teams."),
        ["exclusive"]                    = (RiskLevel.Medium,   "Competition",     "Exclusivity clause may restrict other vendor relationships."),
        ["liquidated damages"]           = (RiskLevel.High,     "Financial",       "Pre-agreed damages clause may result in significant penalties."),
        ["termination for convenience"]  = (RiskLevel.Low,      "Termination",     "Either party may terminate without cause — advantageous but worth tracking."),
        ["non-compete"]                  = (RiskLevel.Medium,   "Employment",      "Non-compete may restrict future hiring or partnerships."),
        ["force majeure"]                = (RiskLevel.Low,      "Risk Allocation", "Force majeure clause present — verify scope and exclusions."),
        ["governing law"]                = (RiskLevel.Low,      "Jurisdiction",    "Jurisdiction specified — ensure alignment with organizational legal strategy."),
        ["arbitration"]                  = (RiskLevel.Medium,   "Dispute",         "Arbitration clause removes court recourse — review carefully."),
        ["confidentiality"]              = (RiskLevel.Low,      "Privacy",         "Confidentiality obligations present — verify duration and scope."),
        ["data processing"]              = (RiskLevel.High,     "Privacy / GDPR",  "Data processing terms detected — GDPR/CCPA compliance review required."),
        ["personal data"]                = (RiskLevel.High,     "Privacy / GDPR",  "Personal data handling present — DPA may be required."),
        ["warranty disclaimer"]          = (RiskLevel.Medium,   "Warranty",        "Vendor disclaims implied warranties — verify fitness-for-purpose coverage."),
        ["limitation of liability"]      = (RiskLevel.Medium,   "Liability",       "Liability cap present — ensure it is sufficient relative to contract value."),
    };

    private static readonly Dictionary<string, string[]> PolicyRequirements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GDPR"]  = new[] { "data processing", "personal data", "data subject rights", "lawful basis", "data retention" },
        ["SOC 2"] = new[] { "confidentiality", "security", "availability", "processing integrity" },
        ["HIPAA"] = new[] { "protected health information", "PHI", "covered entity", "business associate agreement" },
        ["ISO 27001"] = new[] { "information security", "risk assessment", "access control", "incident response" },
    };

    // ── Public API ────────────────────────────────────────────────────────────

    public Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request)
    {
        var text = request.DocumentText ?? request.Question ?? string.Empty;

        var findings   = DetectRisks(text);
        var gaps       = DetectGaps(text, request.Policies);
        var overallRisk = ComputeOverallRisk(findings);

        var response = new AnalyzeResponse
        {
            DocumentName  = request.DocumentName ?? "Inline text",
            Organization  = request.Organization,
            OverallRisk   = overallRisk,
            Summary       = BuildSummary(overallRisk, findings, gaps, request),
            Findings      = findings,
            Gaps          = gaps,
            Recommendations = BuildRecommendations(findings, gaps),
            ActionItems     = BuildActionItems(overallRisk, findings),
            AnalyzedAt    = DateTimeOffset.UtcNow,
        };

        return Task.FromResult(response);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static List<RiskFinding> DetectRisks(string text)
    {
        var findings = new List<RiskFinding>();
        foreach (var (keyword, (level, category, impact)) in RiskKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new RiskFinding
                {
                    Category     = category,
                    Level        = level,
                    Description  = $"Clause detected: \"{keyword}\".",
                    BusinessImpact = impact,
                });
            }
        }
        return findings;
    }

    private static List<ComplianceGap> DetectGaps(string text, IReadOnlyList<string> policies)
    {
        var gaps = new List<ComplianceGap>();
        foreach (var policy in policies)
        {
            if (!PolicyRequirements.TryGetValue(policy, out var required))
                continue;

            foreach (var clause in required)
            {
                if (!text.Contains(clause, StringComparison.OrdinalIgnoreCase))
                {
                    gaps.Add(new ComplianceGap
                    {
                        Policy       = policy,
                        MissingClause = clause,
                        Detail       = $"Required {policy} element \"{clause}\" not found in the document.",
                        Severity     = policy == "GDPR" || policy == "HIPAA" ? RiskLevel.High : RiskLevel.Medium,
                    });
                }
            }
        }
        return gaps;
    }

    private static RiskLevel ComputeOverallRisk(IReadOnlyList<RiskFinding> findings)
    {
        if (findings.Any(f => f.Level == RiskLevel.Critical)) return RiskLevel.Critical;
        if (findings.Any(f => f.Level == RiskLevel.High))     return RiskLevel.High;
        if (findings.Any(f => f.Level == RiskLevel.Medium))   return RiskLevel.Medium;
        if (findings.Any(f => f.Level == RiskLevel.Low))      return RiskLevel.Low;
        return RiskLevel.Low;
    }

    private static string BuildSummary(RiskLevel risk, List<RiskFinding> findings, List<ComplianceGap> gaps, AnalyzeRequest req)
    {
        var docRef = req.DocumentName is not null ? $"Document \"{req.DocumentName}\"" : "The submitted text";
        var riskLabel = risk.ToString().ToUpper();
        var findingCount = findings.Count;
        var gapCount = gaps.Count;

        return $"{docRef} has been analyzed for {req.Organization}. " +
               $"Overall risk level: {riskLabel}. " +
               $"{findingCount} risk finding(s) identified across {findings.Select(f => f.Category).Distinct().Count()} category(ies). " +
               (gapCount > 0
                   ? $"{gapCount} compliance gap(s) detected against the specified policies ({string.Join(", ", req.Policies)})."
                   : "No compliance gaps detected against the specified policies.");
    }

    private static List<string> BuildRecommendations(List<RiskFinding> findings, List<ComplianceGap> gaps)
    {
        var recs = new List<string>();

        if (findings.Any(f => f.Level == RiskLevel.Critical))
            recs.Add("Escalate to Legal immediately — critical risk clauses require renegotiation before signature.");
        if (findings.Any(f => f.Level == RiskLevel.High))
            recs.Add("Schedule a legal review for all High-risk clauses before approving the contract.");
        if (findings.Any(f => f.Category == "Privacy / GDPR"))
            recs.Add("Engage the Data Protection Officer to review GDPR obligations and confirm a DPA is in place.");
        if (gaps.Any())
            recs.Add("Address all compliance gaps before finalizing the agreement.");
        if (findings.Any(f => f.Category == "Contract Term"))
            recs.Add("Configure calendar reminders for auto-renewal dates at least 90 days in advance.");
        if (!recs.Any())
            recs.Add("No immediate action required — conduct standard procurement review.");

        return recs;
    }

    private static List<string> BuildActionItems(RiskLevel risk, List<RiskFinding> findings)
    {
        var items = new List<string>();
        if (risk >= RiskLevel.High)
            items.Add("Route for Legal sign-off (SLA: 5 business days).");
        if (findings.Any(f => f.Category == "Privacy / GDPR" || f.Category == "Privacy"))
            items.Add("Complete GDPR/Privacy impact assessment.");
        if (findings.Any(f => f.Category == "Liability"))
            items.Add("Validate liability cap against contract value and insurance coverage.");
        if (findings.Any(f => f.Category == "Financial"))
            items.Add("Finance review: assess penalty exposure under liquidated damages clause.");
        if (risk == RiskLevel.Low && !findings.Any())
            items.Add("Proceed with standard procurement approval workflow.");
        return items;
    }
}
