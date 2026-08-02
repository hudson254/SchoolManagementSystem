using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Provides retry policies for resilient operations.
    /// Implements graceful fallback for transient faults, database timeouts,
    /// and external service failures with exponential backoff.
    /// </summary>
    public static class RetryPolicyHelper
    {
        private const int DefaultRetryCount = 3;
        private const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// Executes a database operation with retry and exponential backoff.
        /// </summary>
        public static async Task<T> ExecuteDatabaseAsync<T>(
            Func<Task<T>> operation,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    return await operation();
                }
                catch (Exception ex) when (
                    ex is TimeoutException ||
                    ex is System.Data.Common.DbException ||
                    ex is OperationCanceledException)
                {
                    retryCount++;
                    if (retryCount > DefaultRetryCount)
                        throw;

                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100);
                    logger.LogWarning(ex,
                        "Database operation failed. Retrying {RetryCount}/{MaxRetries} after {Delay}ms. Error: {Message}",
                        retryCount, DefaultRetryCount, delay.TotalMilliseconds, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Executes a database operation with retry and exponential backoff (no return value).
        /// </summary>
        public static async Task ExecuteDatabaseAsync(
            Func<Task> operation,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    await operation();
                    return;
                }
                catch (Exception ex) when (
                    ex is TimeoutException ||
                    ex is System.Data.Common.DbException ||
                    ex is OperationCanceledException)
                {
                    retryCount++;
                    if (retryCount > DefaultRetryCount)
                        throw;

                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100);
                    logger.LogWarning(ex,
                        "Database operation failed. Retrying {RetryCount}/{MaxRetries} after {Delay}ms. Error: {Message}",
                        retryCount, DefaultRetryCount, delay.TotalMilliseconds, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Executes an external service call with retry and exponential backoff.
        /// </summary>
        public static async Task<T> ExecuteExternalAsync<T>(
            Func<Task<T>> operation,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    return await operation();
                }
                catch (Exception ex) when (
                    ex is System.Net.Http.HttpRequestException ||
                    ex is TimeoutException ||
                    ex is TaskCanceledException)
                {
                    retryCount++;
                    if (retryCount > DefaultRetryCount)
                        throw;

                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 200);
                    logger.LogWarning(ex,
                        "External service call failed. Retrying {RetryCount}/{MaxRetries} after {Delay}ms. Error: {Message}",
                        retryCount, DefaultRetryCount, delay.TotalMilliseconds, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Executes an external service call with retry and exponential backoff (no return value).
        /// </summary>
        public static async Task ExecuteExternalAsync(
            Func<Task> operation,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                    await operation();
                    return;
                }
                catch (Exception ex) when (
                    ex is System.Net.Http.HttpRequestException ||
                    ex is TimeoutException ||
                    ex is TaskCanceledException)
                {
                    retryCount++;
                    if (retryCount > DefaultRetryCount)
                        throw;

                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 200);
                    logger.LogWarning(ex,
                        "External service call failed. Retrying {RetryCount}/{MaxRetries} after {Delay}ms. Error: {Message}",
                        retryCount, DefaultRetryCount, delay.TotalMilliseconds, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }
}
