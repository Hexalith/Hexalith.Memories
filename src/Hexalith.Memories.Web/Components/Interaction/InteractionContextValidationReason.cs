// <copyright file="InteractionContextValidationReason.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Interaction;

/// <summary>Reason a captured interaction context is unavailable or disabled.</summary>
public enum InteractionContextValidationReason
{
    /// <summary>The context is valid.</summary>
    Valid = 0,

    /// <summary>The tenant is missing.</summary>
    MissingTenant,

    /// <summary>The captured tenant no longer matches the active tenant.</summary>
    TenantChanged,

    /// <summary>The captured case no longer matches the active case.</summary>
    CaseChanged,

    /// <summary>The packet scope is unauthorized or otherwise restrictive.</summary>
    UnauthorizedScope,

    /// <summary>The captured target is no longer present in the packet.</summary>
    MissingTarget,

    /// <summary>The captured contract version is not supported.</summary>
    ContractVersionMismatch,
}
