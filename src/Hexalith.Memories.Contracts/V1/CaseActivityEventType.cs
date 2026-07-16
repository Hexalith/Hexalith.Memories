// <copyright file="CaseActivityEventType.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>Types of activity events recorded against a case.</summary>
[JsonConverter(typeof(CamelCaseStringEnumConverter<CaseActivityEventType>))]
public enum CaseActivityEventType
{
    CaseCreated,
    MemoryUnitIngested,
    IngestionFailed,
    SearchExecuted,
    MemberAdded,
    MemberRemoved,
    MemoryUnitDeleted,
    CaseDeleted,
    AnnotationCreated,
}
