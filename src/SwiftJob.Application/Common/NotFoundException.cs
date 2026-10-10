namespace SwiftJob.Application.Common;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} '{key}' was not found.")
{
    public string EntityName { get; } = entityName;
    public object Key { get; } = key;
}
