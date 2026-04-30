namespace BlazorResearch;

/// <summary>Structured research report extracted from the synthesis agent's response.</summary>
internal record ResearchReport(
    /// <summary>The research topic as stated by the user.</summary>
    string Topic,
    /// <summary>Technology or market maturity: Emerging, Growing, or Mature.</summary>
    string MaturityLevel,
    /// <summary>The single most important finding from the research.</summary>
    string KeyFinding,
    /// <summary>3-5 year outlook in one sentence.</summary>
    string Outlook,
    /// <summary>Analyst confidence in the findings from 1 (very low) to 10 (very high).</summary>
    int ConfidenceScore);
