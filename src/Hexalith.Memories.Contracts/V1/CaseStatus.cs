// <copyright file="CaseStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Represents the lifecycle status of a case.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<CaseStatus>))]
public enum CaseStatus
{
    Active,
    Closed,
    Deleting,
}
