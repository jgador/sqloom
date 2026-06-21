using System;
using System.Collections.Generic;
using System.Threading;
using Sqloom.AspNetCore.Endpoints;

namespace Sqloom.AspNetCore.Capture;

/// <summary>
/// Collects SQL commands captured during replay.
/// </summary>
public sealed class ReplaySqlCaptureCollector
{
    private readonly AsyncLocal<ScopeState?> _currentScope = new();
    private readonly object _completedGate = new();
    private readonly Dictionary<string, IReadOnlyList<CapturedSqlCommand>> _completedScopes =
        new(StringComparer.OrdinalIgnoreCase);

    internal ReplaySqlCaptureScope BeginScope(string operationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);

        var previous = _currentScope.Value;
        ScopeState current = new();
        _currentScope.Value = current;
        return new ReplaySqlCaptureScope(this, current, previous);
    }

    public void Record(CapturedSqlCommand command)
    {
        var current = _currentScope.Value;
        current?.Add(command);
    }

    internal void StoreCompleted(
        string captureKey,
        IReadOnlyList<CapturedSqlCommand> commands)
    {
        lock (_completedGate)
        {
            _completedScopes[captureKey] = commands;
        }
    }

    internal IReadOnlyList<CapturedSqlCommand> TakeCompleted(string captureKey)
    {
        lock (_completedGate)
        {
            if (_completedScopes.Remove(captureKey, out var commands))
            {
                return commands;
            }
        }

        return Array.Empty<CapturedSqlCommand>();
    }

    private void RestoreScope(ScopeState current, ScopeState? previous)
    {
        if (ReferenceEquals(_currentScope.Value, current))
        {
            _currentScope.Value = previous;
        }
    }

    /// <summary>
    /// Tracks one active replay SQL capture scope.
    /// </summary>
    internal sealed class ReplaySqlCaptureScope : IDisposable
    {
        private readonly ReplaySqlCaptureCollector _collector;
        private readonly ScopeState _current;
        private readonly ScopeState? _previous;
        private bool _disposed;

        internal ReplaySqlCaptureScope(
            ReplaySqlCaptureCollector collector,
            ScopeState current,
            ScopeState? previous)
        {
            _collector = collector;
            _current = current;
            _previous = previous;
        }

        public IReadOnlyList<CapturedSqlCommand> Complete()
        {
            Dispose();
            return _current.GetSnapshot();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _collector.RestoreScope(_current, _previous);
        }
    }

    /// <summary>
    /// Tracks the mutable state behind one replay SQL capture scope.
    /// </summary>
    internal sealed class ScopeState
    {
        private readonly object _gate = new();
        private readonly List<CapturedSqlCommand> _commands = [];

        public ScopeState()
        {
        }

        public void Add(CapturedSqlCommand command)
        {
            lock (_gate)
            {
                _commands.Add(command);
            }
        }

        public IReadOnlyList<CapturedSqlCommand> GetSnapshot()
        {
            lock (_gate)
            {
                return _commands.ToArray();
            }
        }
    }
}
