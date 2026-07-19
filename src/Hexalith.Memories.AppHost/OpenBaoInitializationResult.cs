// <copyright file="OpenBaoInitializationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Contains the scoped identities created for one disposable OpenBao generation.</summary>
/// <param name="RuntimeToken">The runtime-secret read token.</param>
/// <param name="AccessTelemetryToken">The access-telemetry-secret read token.</param>
/// <param name="BootstrapTokenSha256">The fingerprint used only for disclosure scans.</param>
/// <param name="UnsealKeySha256">The fingerprint used only for disclosure scans.</param>
internal sealed record OpenBaoInitializationResult(
    string RuntimeToken,
    string AccessTelemetryToken,
    string BootstrapTokenSha256,
    string UnsealKeySha256);
