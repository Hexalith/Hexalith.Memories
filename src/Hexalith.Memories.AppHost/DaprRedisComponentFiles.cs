// <copyright file="DaprRedisComponentFiles.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Writes the Redis-backed Dapr component files that the AppHost generates for its sidecars.</summary>
/// <remarks>
/// The files are rewritten with Aspire's allocated Redis endpoint before the sidecars start. Each file is staged
/// and installed by atomic replacement, so a reader never observes a truncated or partially written component.
/// </remarks>
internal static class DaprRedisComponentFiles
{
    /// <summary>Writes the state store, pub/sub, access-telemetry store and access-telemetry configuration components.</summary>
    /// <param name="stateStorePath">The state store component path.</param>
    /// <param name="pubSubPath">The pub/sub component path.</param>
    /// <param name="accessTelemetryStorePath">The access-telemetry state store component path.</param>
    /// <param name="accessTelemetryConfigPath">The access-telemetry configuration component path.</param>
    /// <param name="redisHost">The Redis host and port.</param>
    /// <param name="daprAppId">The Memories Dapr application identifier used in component scopes.</param>
    internal static void Write(
        string stateStorePath,
        string pubSubPath,
        string accessTelemetryStorePath,
        string accessTelemetryConfigPath,
        string redisHost,
        string daprAppId)
    {
        OpenBaoProtectedFileSystem.WriteAllTextAtomically(
            stateStorePath,
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: statestore
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "{redisHost}"
                - name: redisPassword
                  value: ""
                - name: redisMaxRetries
                  value: "60"
                - name: redisMinRetryInterval
                  value: "500ms"
                - name: redisMaxRetryInterval
                  value: "2s"
                - name: actorStateStore
                  value: "true"
            scopes:
              - {daprAppId}
            """);

        OpenBaoProtectedFileSystem.WriteAllTextAtomically(
            pubSubPath,
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: pubsub
            spec:
              type: pubsub.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "{redisHost}"
                - name: redisPassword
                  value: ""
                - name: redisMaxRetries
                  value: "60"
                - name: redisMinRetryInterval
                  value: "500ms"
                - name: redisMaxRetryInterval
                  value: "2s"
            """);

        OpenBaoProtectedFileSystem.WriteAllTextAtomically(
            accessTelemetryStorePath,
            $$"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: access-telemetry-store
            spec:
              type: state.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "{{redisHost}}"
                - name: redisPassword
                  value: ""
                - name: actorStateStore
                  value: "true"
                - name: queryIndexes
                  value: |
                    [
                      {
                        "name": "expiryMinute",
                        "indexes": [
                          { "key": "expiryMinute", "type": "NUMERIC" }
                        ]
                      }
                    ]
            scopes:
              - memories-access-telemetry
            """);

        OpenBaoProtectedFileSystem.WriteAllTextAtomically(
            accessTelemetryConfigPath,
            $"""
            apiVersion: dapr.io/v1alpha1
            kind: Component
            metadata:
              name: access-telemetry-config
            spec:
              type: configuration.redis
              version: v1
              metadata:
                - name: redisHost
                  value: "{redisHost}"
                - name: redisPassword
                  value: ""
            scopes:
              - {daprAppId}
              - memories-access-telemetry
            """);
    }
}
