namespace Snacks.Services.Cluster;

/// <summary>
///     Retry and chunk limits for cluster file transfers. WAN profile targets 
///     reverse-proxy paths; LAN profile preserves historical behaviour for LAN/Tailscale.
/// </summary>
public readonly record struct TransferProfile(
    int MaxChunkBytes,
    int MaxSessionFailures,
    Func<int, int> RetryDelaySeconds)
{
    /// <summary> No chunk cap beyond user <c>ChunkSizeMB</c>; fixed 5 s retry; 60 failures. </summary>
    public static TransferProfile Lan { get; } = new(
        int.MaxValue,
        60,
        _ => 5);

    /// <summary> 25 MB chunk cap; exponential backoff; 5 session failures. </summary>
    public static TransferProfile Wan { get; } = new(
        25 * 1024 * 1024,
        5,
        failures => (int)Math.Min(5 * Math.Pow(2, failures - 1), 60));

    /// <summary> Effective chunk size for this profile and the configured base size. </summary>
    public int EffectiveChunkBytes(int baseChunkBytes) =>
        MaxChunkBytes == int.MaxValue
            ? baseChunkBytes
            : Math.Min(baseChunkBytes, MaxChunkBytes);
}
