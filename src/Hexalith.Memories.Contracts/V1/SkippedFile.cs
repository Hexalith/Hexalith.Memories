// <copyright file="SkippedFile.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>A file the directory ingestion endpoint skipped at discovery time with a machine-readable reason.</summary>
/// <param name="Path">Canonical absolute path of the file.</param>
/// <param name="Reason">Skip reason code (e.g. UNSUPPORTED_EXTENSION, PAYLOAD_TOO_LARGE, FILE_UNREADABLE).</param>
public sealed record SkippedFile(string Path, string Reason);
