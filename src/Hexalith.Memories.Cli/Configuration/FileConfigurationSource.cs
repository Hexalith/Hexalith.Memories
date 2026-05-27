// <copyright file="FileConfigurationSource.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads user-scoped config from <c>~/.hexalith/memories.json</c> (Windows:
/// <c>%USERPROFILE%\.hexalith\memories.json</c>). Project-local walk-up is intentionally not supported
/// (see ADR-7.1-004).
/// </summary>
public sealed class FileConfigurationSource : IConfigurationSource
{
    /// <summary>The relative path under the user home directory where the config file lives.</summary>
    public const string RelativeConfigPath = ".hexalith/memories.json";

    private readonly Func<string?> _getUserHome;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, string> _readAllText;

    /// <summary>Initializes a new instance using the real filesystem.</summary>
    public FileConfigurationSource()
        : this(DefaultUserHome, File.Exists, File.ReadAllText)
    {
    }

    /// <summary>Initializes a new instance using the supplied file-system abstractions (for tests).</summary>
    /// <param name="getUserHome">Returns the user home directory, or <see langword="null"/> if unavailable.</param>
    /// <param name="fileExists">Returns <see langword="true"/> if the file exists.</param>
    /// <param name="readAllText">Reads the file contents as text.</param>
    public FileConfigurationSource(
        Func<string?> getUserHome,
        Func<string, bool> fileExists,
        Func<string, string> readAllText)
    {
        ArgumentNullException.ThrowIfNull(getUserHome);
        ArgumentNullException.ThrowIfNull(fileExists);
        ArgumentNullException.ThrowIfNull(readAllText);

        _getUserHome = getUserHome;
        _fileExists = fileExists;
        _readAllText = readAllText;
    }

    /// <inheritdoc />
    public string SourceName => nameof(FileConfigurationSource);

    /// <summary>Gets the resolved path to the user config file.</summary>
    /// <returns>The absolute path, or <see langword="null"/> when no user home is available.</returns>
    public string? GetConfigFilePath()
    {
        string? home = _getUserHome();
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        return Path.GetFullPath(Path.Combine(home, RelativeConfigPath));
    }

    /// <inheritdoc />
    public bool TryResolve(out Uri? endpoint, out string? apiToken)
    {
        endpoint = null;
        apiToken = null;

        string? path = GetConfigFilePath();
        if (string.IsNullOrEmpty(path) || !_fileExists(path))
        {
            return false;
        }

        string content = _readAllText(path);
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        FileConfig? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FileConfig>(content, s_serializerOptions);
        }
        catch (JsonException jsonException)
        {
            throw new InvalidConfigurationException(path, "file is not valid JSON.", jsonException);
        }

        if (parsed is null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.Endpoint))
        {
            if (!Uri.TryCreate(parsed.Endpoint, UriKind.Absolute, out Uri? parsedUri))
            {
                throw new InvalidConfigurationException(path, $"'endpoint' value '{parsed.Endpoint}' is not an absolute URI.");
            }

            endpoint = parsedUri;
        }

        if (!string.IsNullOrEmpty(parsed.ApiToken))
        {
            apiToken = parsed.ApiToken;
        }

        return endpoint is not null || apiToken is not null;
    }

    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static string? DefaultUserHome()
    {
        string? home = Environment.GetEnvironmentVariable("USERPROFILE");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetEnvironmentVariable("HOME");
        }

        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return string.IsNullOrEmpty(home) ? null : home;
    }

    private sealed record FileConfig(
        [property: JsonPropertyName("endpoint")] string? Endpoint,
        [property: JsonPropertyName("apiToken")] string? ApiToken,
        [property: JsonPropertyName("timeoutSeconds")] int? TimeoutSeconds = null);
}
