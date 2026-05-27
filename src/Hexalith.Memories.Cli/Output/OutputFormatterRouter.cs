// <copyright file="OutputFormatterRouter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Output;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Resolves an <see cref="IOutputFormatter{T}"/> per <see cref="OutputFormat"/> via
/// <see cref="IServiceProvider"/>. Throws a typed <see cref="FormatterNotRegisteredException"/> when the pair
/// is missing so the dev agent's diagnostic path stays clear (Task 1.3).
/// </summary>
public sealed class OutputFormatterRouter
{
    private readonly IServiceProvider _services;

    /// <summary>Initializes a new instance of the <see cref="OutputFormatterRouter"/> class.</summary>
    /// <param name="services">The service provider holding the registered formatters.</param>
    public OutputFormatterRouter(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <summary>Writes <paramref name="value"/> using the formatter registered for (<typeparamref name="T"/>, <paramref name="format"/>).</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="format">The selected format.</param>
    /// <param name="value">The payload.</param>
    /// <param name="writer">The target writer.</param>
    public void Write<T>(OutputFormat format, T value, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        IOutputFormatter<T>? formatter = _services
            .GetServices<IOutputFormatter<T>>()
            .FirstOrDefault(f => f.Format == format);

        if (formatter is null)
        {
            throw new FormatterNotRegisteredException(typeof(T), format);
        }

        formatter.Write(value, writer);
    }
}
