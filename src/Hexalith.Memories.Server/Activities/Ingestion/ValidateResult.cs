// <copyright file="ValidateResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Result of content validation.</summary>
/// <param name="IsValid">Whether the input passed all validation checks.</param>
/// <param name="ErrorMessage">The validation error message if invalid; null on success.</param>
public sealed record ValidateResult(bool IsValid, string? ErrorMessage);
