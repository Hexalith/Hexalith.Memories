// <copyright file="TenantEventRoutingOptionsValidatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using Shouldly;

public sealed class TenantEventRoutingOptionsValidatorTests
{
    [Fact]
    public void Validate_AllowsDisabledIntegration()
    {
        TenantEventRoutingOptionsValidator validator = new();
        TenantEventRoutingOptions options = new()
        {
            Topic = string.Empty,
            PubSubName = "not-used-when-disabled",
        };

        validator.Validate(name: null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RejectsPubSubNameDrift_WhenTopicConfigured()
    {
        TenantEventRoutingOptionsValidator validator = new();
        TenantEventRoutingOptions options = new()
        {
            Topic = "memories-events",
            PubSubName = "other-pubsub",
        };

        validator.Validate(name: null, options).Failed.ShouldBeTrue();
    }
}
