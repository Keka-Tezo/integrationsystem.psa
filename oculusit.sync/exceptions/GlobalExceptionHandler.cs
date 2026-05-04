using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace oculusit.sync.exceptions;

/// <summary>
/// Wires up process-wide unhandled-exception and unobserved-task-exception handlers.
/// Registered as the first hosted service so it runs before the worker.
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException      += OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException      -= OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;

        if (e.IsTerminating)
        {
            logger.LogCritical(ex,
                "A fatal unhandled exception has crashed the process. IsTerminating=true.");
        }
        else
        {
            logger.LogError(ex,
                "An unhandled exception was caught on the AppDomain. The process will continue.");
        }

        // Flush Serilog before the runtime tears down the process.
        Serilog.Log.CloseAndFlush();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        logger.LogError(e.Exception,
            "An unobserved task exception was detected. Marking as observed to prevent process crash.");

        // Prevent the runtime from re-throwing this as an AppDomain unhandled exception.
        e.SetObserved();
    }
}
