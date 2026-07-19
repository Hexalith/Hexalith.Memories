// <copyright file="OpenBaoGenerationGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

/// <summary>Tracks the refreshable readiness signal for disposable OpenBao generations.</summary>
internal sealed class OpenBaoGenerationGate
{
    private readonly object _sync = new();
    private bool _generationActive;
    private int _generationNumber;
    private TaskCompletionSource _readiness = CreateCompletionSource();

    /// <summary>Begins a generation and returns its number and exclusive completion source.</summary>
    /// <returns>The current generation lease.</returns>
    internal (int GenerationNumber, TaskCompletionSource Readiness) BeginGeneration()
    {
        lock (_sync)
        {
            if (!_generationActive)
            {
                if (_readiness.Task.IsCompleted)
                {
                    _readiness = CreateCompletionSource();
                }

                _generationActive = true;
                _generationNumber++;
            }

            return (_generationNumber, _readiness);
        }
    }

    /// <summary>Gets the readiness task belonging to the current or next generation.</summary>
    /// <returns>The generation readiness task.</returns>
    internal Task SnapshotReadiness()
    {
        lock (_sync)
        {
            return _readiness.Task;
        }
    }

    /// <summary>Closes the active generation so the next start obtains a fresh readiness signal.</summary>
    internal void MarkStopped()
    {
        lock (_sync)
        {
            _generationActive = false;
            if (_readiness.Task.IsCompleted)
            {
                _readiness = CreateCompletionSource();
            }
        }
    }

    private static TaskCompletionSource CreateCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
