namespace oculusit.sync.core.exceptions;

/// <summary>
/// Represents a recoverable error during a sync operation.
/// </summary>
public class SyncException : Exception
{
    public SyncException(string message) : base(message) { }
    public SyncException(string message, Exception inner) : base(message, inner) { }
}
