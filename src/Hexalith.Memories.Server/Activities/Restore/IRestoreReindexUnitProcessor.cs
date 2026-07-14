// <copyright file="IRestoreReindexUnitProcessor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

/// <summary>Re-indexes one restored unit for a bounded restore page.</summary>
internal interface IRestoreReindexUnitProcessor
{
    /// <summary>Rebuilds semantic chunks for one memory unit.</summary>
    Task<RestoreReindexResult> ReindexOneAsync(RestoreReindexInput input);
}
