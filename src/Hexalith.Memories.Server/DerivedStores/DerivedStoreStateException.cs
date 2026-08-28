// <copyright file="DerivedStoreStateException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.DerivedStores;

/// <summary>Represents an expected safe derived-store validation or convergence failure.</summary>
internal sealed class DerivedStoreStateException(string code, string message) : InvalidOperationException(message)
{
    /// <summary>Gets the metadata-only stable error code.</summary>
    public string Code { get; } = code;
}
