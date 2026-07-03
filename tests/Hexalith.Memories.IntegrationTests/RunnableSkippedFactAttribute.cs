// <copyright file="RunnableSkippedFactAttribute.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Xunit;

using System.Runtime.CompilerServices;

/// <summary>Legacy fact attribute for integration tests that now run by default.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RunnableSkippedFactAttribute : FactAttribute
{
    /// <summary>Environment variable used to temporarily opt out of legacy runnable tests.</summary>
    public const string EnvironmentVariableName = "HEXALITH_SKIP_RUNNABLE_TESTS";

    /// <summary>Initializes a new instance of the <see cref="RunnableSkippedFactAttribute"/> class.</summary>
    /// <param name="reason">Default skip reason.</param>
    /// <param name="sourceFilePath">Compiler-provided source file path.</param>
    /// <param name="sourceLineNumber">Compiler-provided source line number.</param>
    public RunnableSkippedFactAttribute(
        string reason,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (IsDisabled())
        {
            Skip = reason;
        }
    }

    private static bool IsDisabled()
    {
        string? value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }
}
