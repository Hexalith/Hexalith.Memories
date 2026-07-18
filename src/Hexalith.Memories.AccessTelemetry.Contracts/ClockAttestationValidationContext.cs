// <copyright file="ClockAttestationValidationContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Expected caller context for attestation verification.</summary>
public sealed record ClockAttestationValidationContext(
    string DeploymentId,
    string AppId,
    string ComponentProfileHash,
    string Nonce,
    string RequestingProcessEpoch,
    string RequestingServiceInstanceId);
