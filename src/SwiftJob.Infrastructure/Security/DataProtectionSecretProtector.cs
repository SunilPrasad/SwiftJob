using Microsoft.AspNetCore.DataProtection;
using SwiftJob.Application.Abstractions;

namespace SwiftJob.Infrastructure.Security;

/// <summary>
/// Encrypts secrets with ASP.NET Core Data Protection. Keys live outside the database
/// (App_Data/keys, additionally DPAPI-protected on Windows), so a copied database alone reveals nothing.
/// </summary>
internal sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SwiftJob.Secrets.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
