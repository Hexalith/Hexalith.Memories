// <copyright file="DedupKeySaveStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Permanent dedup key save outcome.</summary>
public enum DedupKeySaveStatus
{
    /// <summary>The dedup key was written by this workflow.</summary>
    Saved = 0,

    /// <summary>The dedup key already existed and was not overwritten.</summary>
    DuplicateExisting = 1,
}
