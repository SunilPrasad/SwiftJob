using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using SwiftJob.Application.Gmail;

namespace SwiftJob.Infrastructure.Gmail;

/// <summary>Single-use OAuth state values kept in memory for a few minutes (single local process).</summary>
internal sealed class MemoryOAuthStateStore(IMemoryCache cache) : IOAuthStateStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public string Create()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(Key(state), true, Lifetime);
        return state;
    }

    public bool TryConsume(string state)
    {
        var key = Key(state);
        if (!cache.TryGetValue(key, out _))
        {
            return false;
        }

        cache.Remove(key);
        return true;
    }

    private static string Key(string state) => "oauth-state:" + state;
}
