namespace SwiftJob.Application.Sync;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    /// <summary>Emails downloaded from Gmail at the same time (well within Gmail API quotas).</summary>
    public int DownloadConcurrency { get; set; } = 5;

    /// <summary>
    /// Emails sent to the AI model at the same time. Higher is faster but may hit provider rate limits
    /// (the SDKs retry rate-limited requests automatically).
    /// </summary>
    public int ExtractionConcurrency { get; set; } = 4;
}
