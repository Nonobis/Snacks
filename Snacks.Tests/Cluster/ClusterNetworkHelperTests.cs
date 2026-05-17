using System.Net;
using FluentAssertions;
using Snacks.Models;
using Snacks.Services;
using Snacks.Services.Cluster;
using Xunit;

namespace Snacks.Tests.Cluster;

public sealed class ClusterNetworkHelperTests
{
    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("10.0.0.5")]
    [InlineData("100.64.12.34")]
    [InlineData("127.0.0.1")]
    public void IsWanNode_returns_false_for_lan_and_tailscale_ips(string ip)
    {
        var node = new ClusterNode { IpAddress = ip, Port = 6767 };
        ClusterNetworkHelper.IsWanNode(node).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("203.0.113.50")]
    public void IsWanNode_returns_true_for_public_ips(string ip)
    {
        var node = new ClusterNode { IpAddress = ip, Port = 6767 };
        ClusterNetworkHelper.IsWanNode(node).Should().BeTrue();
    }

    [Fact]
    public void IsLocalNetwork_recognizes_tailscale_cgnat()
    {
        ClusterNetworkHelper.IsLocalNetwork(IPAddress.Parse("100.64.1.1")).Should().BeTrue();
    }

    [Fact]
    public void TransferProfile_Wan_caps_chunk_at_25mb()
    {
        const int fiftyMb = 50 * 1024 * 1024;
        TransferProfile.Wan.EffectiveChunkBytes(fiftyMb).Should().Be(25 * 1024 * 1024);
    }

    [Fact]
    public void TransferProfile_Lan_does_not_cap_chunk()
    {
        const int fiftyMb = 50 * 1024 * 1024;
        TransferProfile.Lan.EffectiveChunkBytes(fiftyMb).Should().Be(fiftyMb);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(4, 40)]
    [InlineData(5, 60)]
    [InlineData(6, 60)]
    public void TransferProfile_Wan_exponential_backoff(int failure, int expectedSeconds)
    {
        TransferProfile.Wan.RetryDelaySeconds(failure).Should().Be(expectedSeconds);
    }

    [Fact]
    public void TransferProfile_Wan_max_session_failures_is_five()
    {
        TransferProfile.Wan.MaxSessionFailures.Should().Be(5);
    }
}
