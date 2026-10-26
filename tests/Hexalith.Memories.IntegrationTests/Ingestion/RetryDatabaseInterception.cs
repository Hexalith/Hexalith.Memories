namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Reflection;
using StackExchange.Redis;

/// <summary>Forwards every Redis operation while pausing one selected atomic maintenance command.</summary>
public class RetryDatabaseInterception : DispatchProxy
{
    private IDatabase _database = null!;
    private Func<string, RedisValue[], bool> _select = null!;
    private Func<Task> _interleave = null!;
    private Func<Task>? _after;
    private Action<string>? _observe;

    /// <summary>Creates a forwarding adapter with one controlled command boundary.</summary>
    public static IDatabase Create(IDatabase database, Func<string, RedisValue[], bool> select, Func<Task> interleave, Func<Task>? after = null, Action<string>? observe = null)
    {
        IDatabase proxy = Create<IDatabase, RetryDatabaseInterception>();
        var interception = (RetryDatabaseInterception)proxy;
        interception._database = database;
        interception._select = select;
        interception._interleave = interleave;
        interception._after = after;
        interception._observe = observe;
        return proxy;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        ArgumentNullException.ThrowIfNull(args);
        _observe?.Invoke(targetMethod.Name);
        if (targetMethod.Name == nameof(IDatabase.ScriptEvaluateAsync) && args[0] is string script && args[2] is RedisValue[] values && _select(script, values))
            return ExecuteAsync(targetMethod, args);
        return targetMethod.Invoke(_database, args);
    }

    private async Task<RedisResult> ExecuteAsync(MethodInfo method, object?[] args)
    {
        await _interleave();
        RedisResult result = await (Task<RedisResult>)method.Invoke(_database, args)!;
        if (_after is not null) await _after();
        return result;
    }
}
