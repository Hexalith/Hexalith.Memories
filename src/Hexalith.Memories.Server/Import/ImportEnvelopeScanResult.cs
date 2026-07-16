// <copyright file="ImportEnvelopeScanResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Import;

using Hexalith.Memories.Contracts.V1;

/// <summary>Small aggregate returned by a bounded-memory envelope scan.</summary>
/// <param name="Manifest">Validated export manifest.</param>
/// <param name="Statistics">Validated envelope counts.</param>
internal sealed record ImportEnvelopeScanResult(ExportManifest Manifest, ExportStatistics Statistics);
