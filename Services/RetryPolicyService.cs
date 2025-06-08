using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Exceptions;

namespace Fluent.TaskScheduler.Services
{
    /// <summary>
    /// Interface for retry policy operations.
    /// </summary>
    public interface IRetryPolicyService
    {
        /// <summary>
        /// Executes an operation with retry policy.
        /// </summary>
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with retry policy (no return value).
        /// </summary>
        Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with custom retry policy.
        /// </summary>
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicyOptions retryPolicy, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with custom retry policy (no return value).
        /// </summary>
        Task ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyOptions retryPolicy, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service that provides retry policy functionality with exponential backoff and circuit breaker patterns.
    /// </summary>
    public class RetryPolicyService : IRetryPolicyService
    {
        private readonly ILogger<RetryPolicyService> _logger;
        private readonly TaskSchedulerOptions _options;
        private readonly Random _random = new();

        public RetryPolicyService(ILogger<RetryPolicyService> logger, IOptions<TaskSchedulerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default)
        {
            var retryPolicy = new RetryPolicyOptions
            {
                RetryCount = _options.DefaultRetryCount,
                BaseDelay = _options.RetryBaseDelay,
                MaxDelay = _options.RetryMaxDelay
            };

            return await ExecuteWithRetryAsync(operation, retryPolicy, operationName, taskId, taskName, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default)
        {
            var retryPolicy = new RetryPolicyOptions
            {
                RetryCount = _options.DefaultRetryCount,
                BaseDelay = _options.RetryBaseDelay,
                MaxDelay = _options.RetryMaxDelay
            };

            await ExecuteWithRetryAsync(operation, retryPolicy, operationName, taskId, taskName, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicyOptions retryPolicy, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default)
        {
            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= retryPolicy.RetryCount)
            {
                try
                {
                    if (_options.EnableDetailedLogging && attempt > 0)
                    {
                        _logger.LogDebug("Retry attempt {Attempt} for operation {OperationName} (Task: {TaskName})", 
                            attempt, operationName, taskName ?? taskId ?? "Unknown");
                    }

                    using var timeoutCts = new CancellationTokenSource(_options.OperationTimeout);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var result = await operation().ConfigureAwait(false);
                    
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Operation {OperationName} succeeded after {Attempt} retries (Task: {TaskName})", 
                            operationName, attempt, taskName ?? taskId ?? "Unknown");
                    }

                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Operation {OperationName} was cancelled (Task: {TaskName})", 
                        operationName, taskName ?? taskId ?? "Unknown");
                    throw;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new TaskOperationTimeoutException($"Operation timed out after {_options.OperationTimeout}", _options.OperationTimeout, taskId, taskName);
                    _logger.LogWarning("Operation {OperationName} timed out after {Timeout} (Attempt {Attempt}, Task: {TaskName})", 
                        operationName, _options.OperationTimeout, attempt + 1, taskName ?? taskId ?? "Unknown");
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Don't retry permission errors
                    _logger.LogError(ex, "Permission denied for operation {OperationName} (Task: {TaskName})", 
                        operationName, taskName ?? taskId ?? "Unknown");
                    throw new TaskSchedulerPermissionException(
                        $"Permission denied for operation '{operationName}'. Ensure the application has administrator privileges.", 
                        ex, taskId, taskName);
                }
                catch (Exception ex) when (IsRetriableException(ex))
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Retriable error in operation {OperationName} (Attempt {Attempt}, Task: {TaskName}): {Message}", 
                        operationName, attempt + 1, taskName ?? taskId ?? "Unknown", ex.Message);
                }
                catch (Exception ex)
                {
                    // Don't retry non-retriable exceptions
                    _logger.LogError(ex, "Non-retriable error in operation {OperationName} (Task: {TaskName}): {Message}", 
                        operationName, taskName ?? taskId ?? "Unknown", ex.Message);
                    throw new Fluent.TaskScheduler.Exceptions.TaskSchedulerException($"Operation '{operationName}' failed: {ex.Message}", ex, taskId, taskName);
                }

                attempt++;

                if (attempt <= retryPolicy.RetryCount)
                {
                    var delay = CalculateDelay(attempt, retryPolicy);
                    _logger.LogDebug("Waiting {Delay}ms before retry {Attempt} for operation {OperationName} (Task: {TaskName})", 
                        delay.TotalMilliseconds, attempt, operationName, taskName ?? taskId ?? "Unknown");
                    
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // All retries exhausted
            var finalException = lastException ?? new Fluent.TaskScheduler.Exceptions.TaskSchedulerException($"Operation '{operationName}' failed after {retryPolicy.RetryCount} retries", taskId, taskName);
            _logger.LogError(finalException, "Operation {OperationName} failed after {RetryCount} retries (Task: {TaskName})", 
                operationName, retryPolicy.RetryCount, taskName ?? taskId ?? "Unknown");
            
            throw finalException;
        }

        /// <inheritdoc/>
        public async Task ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyOptions retryPolicy, string operationName, string? taskId = null, string? taskName = null, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation().ConfigureAwait(false);
                return true; // Dummy return value
            }, retryPolicy, operationName, taskId, taskName, cancellationToken).ConfigureAwait(false);
        }

        private TimeSpan CalculateDelay(int attempt, RetryPolicyOptions retryPolicy)
        {
            TimeSpan delay;

            if (retryPolicy.UseExponentialBackoff)
            {
                // Exponential backoff: baseDelay * 2^(attempt-1)
                var exponentialDelay = TimeSpan.FromMilliseconds(
                    retryPolicy.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                delay = exponentialDelay > retryPolicy.MaxDelay ? retryPolicy.MaxDelay : exponentialDelay;
            }
            else
            {
                // Linear backoff: baseDelay * attempt
                var linearDelay = TimeSpan.FromMilliseconds(retryPolicy.BaseDelay.TotalMilliseconds * attempt);
                delay = linearDelay > retryPolicy.MaxDelay ? retryPolicy.MaxDelay : linearDelay;
            }

            // Add jitter to prevent thundering herd
            if (retryPolicy.JitterFactor > 0)
            {
                var jitterMs = delay.TotalMilliseconds * retryPolicy.JitterFactor * (_random.NextDouble() - 0.5);
                delay = delay.Add(TimeSpan.FromMilliseconds(jitterMs));
            }

            return delay;
        }

        private static bool IsRetriableException(Exception ex)
        {
            return ex switch
            {
                TaskSchedulerServiceException => true,
                System.IO.IOException => true,
                System.Net.NetworkInformation.NetworkInformationException => true,
                TimeoutException => true,
                TaskOperationTimeoutException => true,
                // Add more retriable exception types as needed
                _ => false
            };
        }
    }
} 