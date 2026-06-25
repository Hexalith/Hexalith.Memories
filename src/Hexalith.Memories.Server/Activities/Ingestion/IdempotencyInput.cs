// <copyright file="IdempotencyInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Input for the idempotency check activity.</summary>
/// <param name="SourceUri">The URI of the source content.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="CaseId">The case identifier.</param>
/// <param name="IdempotencyToken">
/// Optional explicit idempotency token (Story 18.4). When non-blank it is checked first (precedence);
/// the <see cref="SourceUri"/> natural key is the fallback. Defaults to <see langword="null"/> so existing
/// callers and serialized payloads are unaffected.
/// </param>
public sealed record IdempotencyInput(string SourceUri, string TenantId, string CaseId, string? IdempotencyToken = null);
