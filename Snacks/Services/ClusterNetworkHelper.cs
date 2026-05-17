using System.Net;
using System.Net.Sockets;
using Snacks.Models;

namespace Snacks.Services;

/// <summary>
///     Classifies cluster node reachability for transfer tuning (LAN/Tailscale vs WAN/Pangolin).
/// </summary>
public static class ClusterNetworkHelper
{
    /// <summary>
    ///     Returns <see langword="true"/> when the node is reached over a path that benefits
    ///     from WAN transfer settings (public IP or hostname resolving only to public addresses),
    ///     e.g. a worker exposed through a reverse proxy via HTTPS. Tailscale (<c>100.64.0.0/10</c>) and
    ///     RFC1918 addresses are treated as LAN.
    /// </summary>
    public static bool IsWanNode(ClusterNode node)
    {
        if (string.IsNullOrWhiteSpace(node.IpAddress))
            return false;

        var host = node.IpAddress.Trim();

        if (IPAddress.TryParse(host, out var ip))
            return !IsLocalNetwork(ip);

        return !HostResolvesToLocalNetwork(host);
    }

    /// <summary>
    ///     Returns <see langword="true"/> when <paramref name="host"/> resolves to at least one
    ///     address in a LAN/overlay range.
    /// </summary>
    public static bool HostResolvesToLocalNetwork(string host)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
                return false;

            return addresses.Any(IsLocalNetwork);
        }
        catch
        {
            // Unresolvable hostname — prefer WAN profile (proxy-safe chunks).
            return false;
        }
    }

    /// <summary>
    ///     Returns <see langword="true"/> for loopback, RFC1918, CGNAT (Tailscale), link-local,
    ///     or IPv6 unique-local addresses.
    /// </summary>
    public static bool IsLocalNetwork(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 10) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 169 && b[1] == 254) return true;
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal) return true;
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return true;
            return false;
        }

        return false;
    }
}
