# Compliance Guardian API

Lightweight ASP.NET Core 8 REST API for contract and compliance risk analysis.  
Companion to [Compliance Guardian Copilot](https://github.com/aleobois-arch/Compliance-Guardian-Copilot).

## Quick start

```bash
git clone https://github.com/aleobois-arch/compliance-guardian-api
cd compliance-guardian-api
dotnet run --project src/ComplianceGuardian.Api
```

Open **http://localhost:5000/swagger** to explore the interactive docs.

---

## Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/` | Redirects to Swagger UI |
| `GET`  | `/health` | Liveness check |
| `POST` | `/analyze` | Analyze a contract or compliance question |

---

## POST /analyze

### Request body

```json
{
  "documentName": "Vendor-NDA-2026.pdf",
  "documentText": "This agreement includes unlimited liability and automatic renewal...",
  "question": "Is this contract GDPR compliant?",
  "organization": "Acme Corp",
  "policies": ["GDPR", "SOC 2"]
}
```

`documentText` and/or `question` are required. All other fields are optional.

**Supported policies:** `GDPR`, `SOC 2`, `HIPAA`, `ISO 27001`

### Example response

```json
{
  "documentName": "Vendor-NDA-2026.pdf",
  "organization": "Acme Corp",
  "overallRisk": "Critical",
  "summary": "Document \"Vendor-NDA-2026.pdf\" has been analyzed for Acme Corp. Overall risk level: CRITICAL. 2 risk finding(s) identified...",
  "findings": [
    {
      "category": "Liability",
      "level": "Critical",
      "description": "Clause detected: \"unlimited liability\".",
      "businessImpact": "Uncapped financial exposure for the organization."
    }
  ],
  "gaps": [
    {
      "policy": "GDPR",
      "missingClause": "data subject rights",
      "detail": "Required GDPR element \"data subject rights\" not found in the document.",
      "severity": "High"
    }
  ],
  "recommendations": [
    "Escalate to Legal immediately — critical risk clauses require renegotiation before signature.",
    "Engage the Data Protection Officer to review GDPR obligations and confirm a DPA is in place."
  ],
  "actionItems": [
    "Route for Legal sign-off (SLA: 5 business days).",
    "Complete GDPR/Privacy impact assessment."
  ],
  "analyzedAt": "2026-06-28T02:14:00.000Z"
}
```

---

## Run with Docker

```bash
docker build -t compliance-guardian-api .
docker run -p 8080:8080 compliance-guardian-api
```

Then open **http://localhost:8080/swagger**.

---

## Architecture

```
POST /analyze
    └── ComplianceService
            ├── DetectRisks()    — keyword-based clause scanning
            ├── DetectGaps()     — policy requirements check
            └── BuildSummary()   — executive summary generation
```

The analysis engine uses heuristic keyword matching.  
Replace `ComplianceService.AnalyzeAsync` with an LLM call (Azure OpenAI, Anthropic, etc.) for production-grade results.

---

## Related

- [Compliance Guardian Copilot](https://github.com/aleobois-arch/Compliance-Guardian-Copilot) — Microsoft Copilot Studio agent with portal UI
