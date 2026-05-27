// <copyright file="TenantEventRoutingOptionsValidator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Microsoft.Extensions.Options;

/// <summary>Validates <see cref="TenantEventRoutingOptions"/> against the compiled subscription contract.
/// The controller's <c>[Topic]</c> attribute is currently bound to the fixed Dapr component name
/// <see cref="EventIngestionController.PubSubName"/>, so hosts must not silently configure a different
/// pub/sub component name and assume the subscription will follow.</summary>
internal sealed class TenantEventRoutingOptionsValidator : IValidateOptions<TenantEventRoutingOptions>
{
    public ValidateOptionsResult Validate(string? name, TenantEventRoutingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            return ValidateOptionsResult.Success;
        }

        string configuredPubSub = string.IsNullOrWhiteSpace(options.PubSubName)
            ? EventIngestionController.PubSubName
            : options.PubSubName.Trim();

        return string.Equals(configuredPubSub, EventIngestionController.PubSubName, StringComparison.Ordinal)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"EventStoreIntegration:Routing:PubSubName must be '{EventIngestionController.PubSubName}' because the subscription endpoint is compiled against that Dapr component name.");
    }
}
