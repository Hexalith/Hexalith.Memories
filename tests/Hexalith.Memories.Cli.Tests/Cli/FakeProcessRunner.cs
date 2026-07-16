// <copyright file="FakeProcessRunner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using System.Collections.Concurrent;

using Hexalith.Memories.Cli.Quickstart;

/// <summary>
/// Test double for <see cref="IProcessRunner"/>. Per-executable scripted responses — the wizard's
/// prerequisite checks key off the executable name so we can script each sub-check independently.
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly ConcurrentDictionary<string, Func<ProcessResult>> _responses = new(StringComparer.Ordinal);

    public List<(string FileName, string Arguments)> Calls { get; } = [];

    public void Register(string fileName, ProcessResult result)
    {
        _responses[fileName] = () => result;
    }

    public void Register(string fileName, Func<ProcessResult> factory)
    {
        _responses[fileName] = factory;
    }

    public Task<ProcessResult> RunAsync(string fileName, string arguments, TimeSpan timeout, CancellationToken ct)
    {
        Calls.Add((fileName, arguments));

        if (_responses.TryGetValue(fileName, out Func<ProcessResult>? factory))
        {
            return Task.FromResult(factory());
        }

        return Task.FromResult(new ProcessResult(
            ExitCode: -1,
            StdOut: string.Empty,
            StdErr: $"Unscripted call to '{fileName}'.",
            Elapsed: TimeSpan.Zero,
            NotFound: true));
    }
}
