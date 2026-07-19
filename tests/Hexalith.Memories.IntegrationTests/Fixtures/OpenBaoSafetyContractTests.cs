// <copyright file="OpenBaoSafetyContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Hexalith.Memories.AppHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

/// <summary>Executable safety tests for the development-only OpenBao profile.</summary>
[Trait("Category", "Integration")]
public sealed class OpenBaoSafetyContractTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void EnsureAllowed_RequiresBothExplicitRunModeAndDevelopment(
        bool isRunMode,
        bool isDevelopment,
        bool allowed)
    {
        if (allowed)
        {
            Should.NotThrow(() => OpenBaoDevelopmentProfile.EnsureAllowed(isRunMode, isDevelopment));
            return;
        }

        _ = Should.Throw<InvalidOperationException>(() =>
            OpenBaoDevelopmentProfile.EnsureAllowed(isRunMode, isDevelopment));
    }

    [Fact]
    public void ProtectedFileSystem_EnforcesEffectiveOwnerOnlyPermissions()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"hexalith-openbao-safety-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "token");
        try
        {
            OpenBaoProtectedFileSystem.CreateDirectory(directory);
            OpenBaoProtectedFileSystem.WriteAllTextAtomically(path, "non-sensitive-test-value");

            if (OperatingSystem.IsWindows())
            {
                AssertWindowsOwnerOnly(directory, path);
            }
            else
            {
                File.GetUnixFileMode(directory).ShouldBe(
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.GetUnixFileMode(path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SessionLifetimeGuard_StopsTheAppHostAtTheMaximumSession()
    {
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        var timeProvider = new FakeTimeProvider();
        var guard = new OpenBaoSessionLifetimeGuard(lifetime, timeProvider);
        TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lifetime.When(instance => instance.StopApplication()).Do(_ => stopped.TrySetResult());

        await guard.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        for (int attempt = 0; attempt < 10 && !stopped.Task.IsCompleted; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), TestContext.Current.CancellationToken).ConfigureAwait(true);
            timeProvider.Advance(OpenBaoSessionLifetimeGuard.MaximumSession);
        }

        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken).ConfigureAwait(true);
        lifetime.Received(1).StopApplication();
        await guard.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Theory]
    [InlineData("root_token=")]
    [InlineData("unseal-key: ")]
    public void SensitiveMatcher_FingerprintsOnlyTheTokenInsideLabeledDiagnostics(string prefix)
    {
        const string token = "hvs.ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        AspireIngestionPipelineFixture.ContainsSensitiveValue(
            prefix + token,
            [],
            new HashSet<string>([fingerprint], StringComparer.Ordinal)).ShouldBeTrue();
    }

    [SupportedOSPlatform("windows")]
    private static void AssertWindowsOwnerOnly(string directory, string path)
    {
        SecurityIdentifier currentOwner = WindowsIdentity.GetCurrent().User!;
        DirectorySecurity directorySecurity = new DirectoryInfo(directory).GetAccessControl();
        FileSecurity fileSecurity = new FileInfo(path).GetAccessControl();

        directorySecurity.AreAccessRulesProtected.ShouldBeTrue();
        fileSecurity.AreAccessRulesProtected.ShouldBeTrue();
        directorySecurity.GetOwner(typeof(SecurityIdentifier)).ShouldBe(currentOwner);
        fileSecurity.GetOwner(typeof(SecurityIdentifier)).ShouldBe(currentOwner);
    }
}
