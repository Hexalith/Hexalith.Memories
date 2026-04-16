// <copyright file="LiveOptionsMonitor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli;

using Microsoft.Extensions.Options;

/// <summary>
/// <see cref="IOptionsMonitor{T}"/> wrapper over a mutable singleton — lets
/// <see cref="Execution.CliCommandExecutor"/> push new endpoint/token values into the live
/// <see cref="Client.Rest.MemoriesClientOptions"/> instance consumed by
/// <see cref="Client.Rest.MemoriesAuthHandler"/> mid-invocation.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
internal sealed class LiveOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class
{
    private readonly T _value;

    public LiveOptionsMonitor(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    public T CurrentValue => _value;

    public T Get(string? name) => _value;

    public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
