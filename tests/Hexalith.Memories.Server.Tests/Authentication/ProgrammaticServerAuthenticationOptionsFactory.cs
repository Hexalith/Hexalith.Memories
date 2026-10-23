namespace Hexalith.Memories.Server.Tests.Authentication;

using Hexalith.Memories.Server.Authentication;
using Microsoft.Extensions.Options;

/// <summary>Creates options through their public initializer, retaining every registered options callback.</summary>
internal sealed class ProgrammaticServerAuthenticationOptionsFactory(
    IEnumerable<IConfigureOptions<MemoriesServerAuthenticationOptions>> configure,
    IEnumerable<IPostConfigureOptions<MemoriesServerAuthenticationOptions>> postConfigure,
    IEnumerable<IValidateOptions<MemoriesServerAuthenticationOptions>> validate)
    : OptionsFactory<MemoriesServerAuthenticationOptions>(configure, postConfigure, validate)
{
    /// <inheritdoc/>
    protected override MemoriesServerAuthenticationOptions CreateInstance(string name)
        => new() { ValidAlgorithms = new List<string> { "HS256", "RS256", "ES256" } };
}
