// <copyright file="DerivedStoreRecordKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1.DerivedStores;

using System.Text.Json.Serialization;

using Hexalith.Memories.Contracts.V1;

/// <summary>Identifies the governed source record represented by a binding entry.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<DerivedStoreRecordKind>))]
public enum DerivedStoreRecordKind
{
    /// <summary>The single source message, which must occupy ordinal zero.</summary>
    Message,

    /// <summary>An attachment in governed manifest order.</summary>
    Attachment,
}
