// <copyright file="UrlHostValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Net;

using Hexalith.Memories.Server.Ingestion;

using Shouldly;

/// <summary>
/// Story 6.1 Task 7.2 — SSRF defense. Covers IPv4/IPv6 public/private/loopback/link-local/multicast,
/// scheme allow-list, AWS/GCP metadata endpoint (169.254.169.254), and AllowPrivateHosts toggle.
/// </summary>
public class UrlHostValidatorTests
{
    private static readonly UrlFetcherOptions _lockedDown = new() { AllowPrivateHosts = false };
    private static readonly UrlFetcherOptions _permissive = new() { AllowPrivateHosts = true };

    [Theory]
    [InlineData("https://8.8.8.8/", true)]
    [InlineData("http://8.8.8.8/", true)]
    [InlineData("https://1.1.1.1/", true)]
    [InlineData("http://10.0.0.1/", false)]
    [InlineData("http://10.255.255.1/", false)]
    [InlineData("http://172.16.0.1/", false)]
    [InlineData("http://172.20.0.1/", false)]
    [InlineData("http://172.31.255.1/", false)]
    [InlineData("http://192.168.1.1/", false)]
    [InlineData("http://127.0.0.1/", false)]
    [InlineData("http://169.254.169.254/", false)] // AWS/GCP metadata endpoint — CRITICAL
    [InlineData("http://100.64.0.1/", false)]
    [InlineData("http://0.0.0.0/", false)]
    [InlineData("http://224.0.0.1/", false)]
    [InlineData("http://240.0.0.1/", false)]
    [InlineData("http://[::1]/", false)]
    [InlineData("http://[fe80::1]/", false)]
    [InlineData("http://[fd00::1]/", false)]
    [InlineData("http://[ff02::1]/", false)]
    [InlineData("http://[2001:4860:4860::8888]/", true)]
    public void IsAllowedHost_WithLockedDownOptions_ClassifiesIpLiteralsCorrectly(string url, bool expected)
    {
        Uri uri = new(url);

        bool actual = UrlHostValidator.IsAllowedHost(uri, _lockedDown);

        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/")]
    [InlineData("gopher://example.com/")]
    public void IsAllowedHost_NonHttpScheme_ReturnsFalse(string url)
    {
        Uri uri = new(url);

        UrlHostValidator.IsAllowedHost(uri, _lockedDown).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowedHost_PrivateIp_WithAllowPrivateHostsTrue_ReturnsTrue()
    {
        Uri uri = new("http://10.0.0.1/");

        UrlHostValidator.IsAllowedHost(uri, _permissive).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowedHost_Loopback_WithAllowPrivateHostsTrue_ReturnsTrue()
    {
        Uri uri = new("http://127.0.0.1/");

        UrlHostValidator.IsAllowedHost(uri, _permissive).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowedHost_Resolver_ResolvesToPrivate_ReturnsFalse()
    {
        Uri uri = new("http://example.test/");
        static IPAddress[] Resolver(string host) => [IPAddress.Parse("10.0.0.5")];

        UrlHostValidator.IsAllowedHost(uri, _lockedDown, Resolver).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowedHost_Resolver_ResolvesToPublicAndPrivate_ReturnsFalse()
    {
        // Defensive: ANY private answer should block, not just all of them.
        Uri uri = new("http://example.test/");
        static IPAddress[] Resolver(string host) => [IPAddress.Parse("8.8.8.8"), IPAddress.Parse("10.0.0.5")];

        UrlHostValidator.IsAllowedHost(uri, _lockedDown, Resolver).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowedHost_Resolver_ResolvesToPublicOnly_ReturnsTrue()
    {
        Uri uri = new("http://example.test/");
        static IPAddress[] Resolver(string host) => [IPAddress.Parse("8.8.8.8")];

        UrlHostValidator.IsAllowedHost(uri, _lockedDown, Resolver).ShouldBeTrue();
    }

    [Fact]
    public void IsAllowedHost_Resolver_ReturnsEmpty_ReturnsFalse()
    {
        Uri uri = new("http://example.test/");
        static IPAddress[] Resolver(string host) => [];

        UrlHostValidator.IsAllowedHost(uri, _lockedDown, Resolver).ShouldBeFalse();
    }

    [Fact]
    public void IsAllowedHost_NullUri_Throws() =>
        Should.Throw<ArgumentNullException>(() => UrlHostValidator.IsAllowedHost(null!, _lockedDown));

    [Fact]
    public void IsAllowedHost_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => UrlHostValidator.IsAllowedHost(new Uri("http://8.8.8.8/"), null!));
}
