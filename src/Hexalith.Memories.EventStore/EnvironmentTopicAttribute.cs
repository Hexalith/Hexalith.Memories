// <copyright file="EnvironmentTopicAttribute.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System;

using Dapr;

/// <summary>Subscription metadata attribute that resolves the topic name from an environment variable
/// before Dapr's canonical <c>/dapr/subscribe</c> discovery endpoint reads endpoint metadata.
///
/// <para>Dapr's built-in <see cref="TopicAttribute"/> does not expand environment-variable placeholders.
/// This adapter keeps the standard controller + <c>MapSubscribeHandler()</c> subscription model while still
/// allowing the topic name to be configured per deployment.</para>
///
/// <para>spec-infrastructure-dependency-abstraction (F8, Decision D30): this attribute reads the topic env
/// var directly by design — attributes cannot receive DI / <c>IConfiguration</c>, and Dapr's
/// <c>/dapr/subscribe</c> discovery reads <see cref="ITopicMetadata"/> from the attribute when the
/// subscribe endpoint is served. It is a sanctioned D30 exception (a Dapr subscription-discovery env
/// adapter), not a product-code infrastructure leak.</para></summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class EnvironmentTopicAttribute : Attribute, ITopicMetadata
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentTopicAttribute"/> class.</summary>
    /// <param name="pubsubName">The Dapr pub/sub component name.</param>
    /// <param name="topicEnvironmentVariable">The environment variable containing the topic name.</param>
    public EnvironmentTopicAttribute(string pubsubName, string topicEnvironmentVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pubsubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicEnvironmentVariable);

        PubsubName = pubsubName.Trim();
        Name = ResolveTopic(topicEnvironmentVariable)!;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string PubsubName { get; }

    string ITopicMetadata.Match => string.Empty;

    int ITopicMetadata.Priority => 0;

    private static string? ResolveTopic(string topicEnvironmentVariable)
    {
        string? value = Environment.GetEnvironmentVariable(topicEnvironmentVariable);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}