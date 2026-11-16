namespace Hexalith.Memories.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Owns a private test-executable worker and always reaps it, including failed starts and cancellation.</summary>
internal sealed class MemoryErasureProcessProtocol : IAsyncDisposable
{
    private readonly AnonymousPipeServerStream _requests = new(PipeDirection.Out, HandleInheritability.Inheritable);
    private readonly AnonymousPipeServerStream _responses = new(PipeDirection.In, HandleInheritability.Inheritable);
    private readonly string _secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private readonly Process _process = new();
    private long _sequence;
    private bool _started;
    private bool _disposed;
    private Task _stdout = Task.CompletedTask;
    private Task _stderr = Task.CompletedTask;

    /// <summary>Gets the observed OS birth identity and executed assembly hashes.</summary>
    internal JsonElement Identity { get; private set; }

    /// <summary>Gets whether owned cleanup observed process exit.</summary>
    internal bool Exited { get; private set; }

    /// <summary>Starts the actual built test executable without rebuilding or replacing its bytes.</summary>
    internal static async Task<MemoryErasureProcessProtocol> StartAsync(CancellationToken token, bool failStart = false, Action<int, long>? observedStart = null)
    {
        var worker = new MemoryErasureProcessProtocol();
        try
        {
            string assembly = typeof(MemoryErasureProcessWorker).Assembly.Location;
            worker._process.StartInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(assembly)!,
            };
            worker._process.StartInfo.ArgumentList.Add(assembly);
            worker._process.StartInfo.ArgumentList.Add("-method");
            worker._process.StartInfo.ArgumentList.Add("Hexalith.Memories.IntegrationTests.Fixtures.MemoryErasureProcessWorker.RunPrivateWorker");
            worker._process.StartInfo.ArgumentList.Add("-parallelMode");
            worker._process.StartInfo.ArgumentList.Add("none");
            worker._process.StartInfo.Environment["MEMORIES_ERASURE_REQUEST_PIPE"] = worker._requests.GetClientHandleAsString();
            worker._process.StartInfo.Environment["MEMORIES_ERASURE_RESPONSE_PIPE"] = worker._responses.GetClientHandleAsString();
            worker._process.StartInfo.Environment["MEMORIES_ERASURE_WORKER_SECRET"] = worker._secret;
            worker._process.StartInfo.Environment["MEMORIES_ERASURE_FAIL_START"] = failStart ? "1" : "0";
            if (!worker._process.Start()) throw new IOException("Private erasure worker did not start.");
            worker._started = true;
            worker._requests.DisposeLocalCopyOfClientHandle();
            worker._responses.DisposeLocalCopyOfClientHandle();
            worker._stdout = DrainAsync(worker._process.StandardOutput);
            worker._stderr = DrainAsync(worker._process.StandardError);
            observedStart?.Invoke(worker._process.Id, StartIdentity(worker._process.Id));
            worker.Identity = await worker.CallAsync("identity", new { }, token);
            if (worker.Identity.GetProperty("Pid").GetInt32() != worker._process.Id
                || worker.Identity.GetProperty("StartTicks").GetInt64() != StartIdentity(worker._process.Id))
                throw new InvalidOperationException("Private worker birth identity disagrees with the observed process.");
            foreach (JsonElement dependency in worker.Identity.GetProperty("Assemblies").EnumerateArray())
            {
                string file = Path.Combine(Path.GetDirectoryName(assembly)!, dependency.GetProperty("Path").GetString()!);
                if (dependency.GetProperty("Identity").GetString() != System.Reflection.AssemblyName.GetAssemblyName(file).FullName)
                    throw new InvalidOperationException("Private worker executed assembly identity disagrees with the prepared parent.");
                if (dependency.GetProperty("Sha256").GetString() != Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(file, token))))
                    throw new InvalidOperationException("Private worker executed dependency bytes disagree with the prepared parent.");
            }
            return worker;
        }
        catch { await worker.DisposeAsync(); throw; }
    }

    /// <summary>Sends one bounded authenticated request and verifies the corresponding response.</summary>
    internal async Task<JsonElement> CallAsync(string operation, object payload, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        long sequence = ++_sequence;
        await WriteAsync(_requests, Create(sequence, operation, JsonSerializer.SerializeToElement(payload), _secret), deadline.Token);
        MemoryErasureProcessMessage response = await ReadAsync(_responses, deadline.Token);
        if (response.Sequence != sequence || !Authentic(response, _secret)) throw new IOException("Foreign private worker response.");
        if (response.Operation != "ok") throw new InvalidOperationException(response.Payload.GetProperty("Error").GetString());
        return response.Payload;
    }

    /// <summary>Exercises a foreign secret or a replay without advancing the worker's accepted sequence.</summary>
    internal async Task<string> RejectedAsync(bool replay, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        long sequence = replay ? _sequence : _sequence + 1;
        await WriteAsync(_requests, Create(sequence, "identity", JsonSerializer.SerializeToElement(new { }), replay ? _secret : "foreign"), deadline.Token);
        MemoryErasureProcessMessage response = await ReadAsync(_responses, deadline.Token);
        if (!Authentic(response, _secret) || response.Sequence != sequence) throw new IOException("Unbound refusal.");
        return response.Operation;
    }

    /// <summary>Kills only this owned process tree and awaits exit and both redirected readers.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            if (_started)
            {
                if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(cleanup.Token);
                await Task.WhenAll(_stdout, _stderr).WaitAsync(cleanup.Token);
                Exited = true;
            }
        }
        finally { _requests.Dispose(); _responses.Dispose(); _process.Dispose(); }
    }

    /// <summary>Signs the unambiguous serialized request tuple.</summary>
    internal static MemoryErasureProcessMessage Create(long sequence, string operation, JsonElement payload, string secret)
        => new(sequence, operation, payload, Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
            JsonSerializer.SerializeToUtf8Bytes(new { sequence, operation, payload }))));

    /// <summary>Authenticates all operation and payload bytes in constant time.</summary>
    internal static bool Authentic(MemoryErasureProcessMessage message, string secret)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(message.Signature),
            Encoding.UTF8.GetBytes(Create(message.Sequence, message.Operation, message.Payload, secret).Signature));

    /// <summary>Reads the kernel birth counter, avoiding independent wall-clock estimates of Linux process start time.</summary>
    internal static long StartIdentity(int pid)
    {
        if (!OperatingSystem.IsLinux())
        {
            using var process = Process.GetProcessById(pid);
            return process.StartTime.ToUniversalTime().Ticks;
        }
        string stat = File.ReadAllText("/proc/" + pid + "/stat");
        string[] fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(fields[19], System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Writes a length-prefixed frame with an explicit one-MiB limit.</summary>
    internal static async Task WriteAsync(Stream stream, MemoryErasureProcessMessage message, CancellationToken token)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message);
        if (bytes.Length > 1024 * 1024) throw new IOException("Private worker frame exceeds limit.");
        await stream.WriteAsync(BitConverter.GetBytes(bytes.Length), token);
        await stream.WriteAsync(bytes, token);
        await stream.FlushAsync(token);
    }

    /// <summary>Reads exactly one bounded frame, honoring cancellation through body completion.</summary>
    internal static async Task<MemoryErasureProcessMessage> ReadAsync(Stream stream, CancellationToken token)
    {
        byte[] header = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(header, token);
        int length = BitConverter.ToInt32(header);
        if (length is <= 0 or > 1024 * 1024) throw new IOException("Invalid private worker frame length.");
        byte[] bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, token);
        return JsonSerializer.Deserialize<MemoryErasureProcessMessage>(bytes) ?? throw new IOException("Missing private worker frame.");
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        char[] buffer = new char[4096];
        while (await reader.ReadAsync(buffer) != 0) { }
    }
}
