namespace Hexalith.Memories.Server.Tests.Infrastructure;

using NSubstitute;
using StackExchange.Redis;

/// <summary>Preserves existing payload-mapping unit fixtures through the atomic Redis writer.
/// Actual script fencing and visibility are qualified only by real Redis integration controls.</summary>
internal static class AtomicPublicationWriteFixture
{
    internal static void Configure(IDatabase database)
    {
        database.ScriptEvaluateAsync(Arg.Is<string>(script => script.Contains("source-publication-write-v1", StringComparison.Ordinal)),
            Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>()).Returns(async call =>
        {
            RedisKey[] keys = call.Arg<RedisKey[]>();
            RedisValue[] values = call.Arg<RedisValue[]>();
            HashEntry[] fields = [.. Enumerable.Range(0, (values.Length - 3) / 2)
                .Select(index => new HashEntry(values[3 + 2 * index], values[4 + 2 * index]))];
            await database.HashSetAsync(keys[0], fields, call.Arg<CommandFlags>()).ConfigureAwait(false);
            return RedisResult.Create((RedisValue)1);
        });
    }
}
