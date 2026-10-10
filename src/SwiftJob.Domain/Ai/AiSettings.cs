using SwiftJob.Domain.Common;

namespace SwiftJob.Domain.Ai;

/// <summary>Which AI provider and model SwiftJob uses. The API key is stored only in encrypted form.</summary>
public class AiSettings : AuditableEntity
{
    public AiProvider Provider { get; set; } = AiProvider.Anthropic;
    public string? Model { get; set; }
    public string? ProtectedApiKey { get; set; }

    /// <summary>Base URL for <see cref="AiProvider.OpenAICompatible"/>, e.g. <c>http://localhost:11434/v1</c>.</summary>
    public string? Endpoint { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Model)
        && (Provider == AiProvider.OpenAICompatible ? !string.IsNullOrWhiteSpace(Endpoint) : ProtectedApiKey is not null);
}
