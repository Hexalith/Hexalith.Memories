// <copyright file="ValidateContentActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

/// <summary>DAPR Workflow activity that validates ingestion input before processing.</summary>
public sealed class ValidateContentActivity : WorkflowActivity<IngestionInput, ValidateResult>
{
    /// <inheritdoc/>
    public override Task<ValidateResult> RunAsync(
        WorkflowActivityContext context,
        IngestionInput input)
    {
        IngestionInputValidator.Validate(input);

        return Task.FromResult(new ValidateResult(true, null));
    }
}
