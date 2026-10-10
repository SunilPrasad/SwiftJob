namespace SwiftJob.Application.Abstractions;

/// <summary>Encrypts secrets (OAuth client secret, refresh tokens) before they are persisted.</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <exception cref="System.Security.Cryptography.CryptographicException">The value cannot be decrypted.</exception>
    string Unprotect(string protectedValue);
}
