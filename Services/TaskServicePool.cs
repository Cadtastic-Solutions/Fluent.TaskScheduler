using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.TaskScheduler;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Exceptions;

namespace Fluent.TaskScheduler.Services
{
    /// <summary>
    /// Interface for TaskService pool operations.
    /// </summary>
    public interface ITaskServicePool : IDisposable
    {
        /// <summary>
        /// Executes an operation with a pooled TaskService instance.
        /// </summary>
        System.Threading.Tasks.Task<T> ExecuteAsync<T>(Func<TaskService, System.Threading.Tasks.Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with a pooled TaskService instance (no return value).
        /// </summary>
        System.Threading.Tasks.Task ExecuteAsync(Func<TaskService, System.Threading.Tasks.Task> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current pool statistics.
        /// </summary>
        TaskServicePoolStats GetStats();
    }

    /// <summary>
    /// Statistics for the TaskService pool.
    /// </summary>
    public class TaskServicePoolStats
    {
        public int TotalInstances { get; set; }
        public int AvailableInstances { get; set; }
        public int InUseInstances { get; set; }
        public long TotalOperations { get; set; }
        public long FailedOperations { get; set; }
        public TimeSpan AverageWaitTime { get; set; }
    }

    /// <summary>
    /// Pooled TaskService wrapper that tracks usage and provides automatic disposal.
    /// </summary>
    internal class PooledTaskService : IDisposable
    {
        public TaskService TaskService { get; }
        public DateTime LastUsed { get; set; }
        public bool IsInUse { get; set; }
        public int UsageCount { get; set; }

        public PooledTaskService()
        {
            TaskService = new TaskService();
            LastUsed = DateTime.UtcNow;
            IsInUse = false;
            UsageCount = 0;
        }

        public void MarkUsed()
        {
            LastUsed = DateTime.UtcNow;
            UsageCount++;
            IsInUse = true;
        }

        public void MarkAvailable()
        {
            IsInUse = false;
        }

        public void Dispose()
        {
            TaskService?.Dispose();
        }
    }

    /// <summary>
    /// Pool of TaskService instances to improve performance and resource management.
    /// </summary>
    public class TaskServicePool : ITaskServicePool
    {
        private readonly ILogger<TaskServicePool> _logger;
        private readonly TaskSchedulerOptions _options;
        private readonly ConcurrentQueue<PooledTaskService> _availableServices = new();
        private readonly ConcurrentDictionary<int, PooledTaskService> _allServices = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new();

        private long _totalOperations;
        private long _failedOperations;
        private long _totalWaitTimeMs;
        private bool _disposed;

        public TaskServicePool(ILogger<TaskServicePool> logger, IOptions<TaskSchedulerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _semaphore = new SemaphoreSlim(_options.TaskServicePoolSize, _options.TaskServicePoolSize);
            
            // Initialize the pool with minimum instances
            InitializePool();
            
            // Set up cleanup timer to run every 5 minutes
            _cleanupTimer = new Timer(CleanupUnusedServices, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("TaskService pool initialized with {PoolSize} instances", _options.TaskServicePoolSize);
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task<T> ExecuteAsync<T>(Func<TaskService, System.Threading.Tasks.Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TaskServicePool));

            var startTime = DateTime.UtcNow;
            PooledTaskService? pooledService = null;

            try
            {
                // Wait for an available slot in the pool
                var acquired = await _semaphore.WaitAsync(_options.TaskServicePoolTimeout, cancellationToken).ConfigureAwait(false);
                if (!acquired)
                {
                    Interlocked.Increment(ref _failedOperations);
                    throw new TaskOperationTimeoutException($"Timeout waiting for TaskService from pool after {_options.TaskServicePoolTimeout}", _options.TaskServicePoolTimeout);
                }

                var waitTime = DateTime.UtcNow - startTime;
                Interlocked.Add(ref _totalWaitTimeMs, (long)waitTime.TotalMilliseconds);

                // Get or create a TaskService instance
                pooledService = GetOrCreateTaskService();
                pooledService.MarkUsed();

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("Acquired TaskService from pool (Wait: {WaitTime}ms, Usage: {UsageCount})", 
                        waitTime.TotalMilliseconds, pooledService.UsageCount);
                }

                // Execute the operation
                var result = await operation(pooledService.TaskService).ConfigureAwait(false);
                
                Interlocked.Increment(ref _totalOperations);
                return result;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedOperations);
                _logger.LogError(ex, "Error executing operation with pooled TaskService");
                
                // If the TaskService might be corrupted, dispose it
                if (pooledService != null && IsTaskServiceCorrupted(ex))
                {
                    _logger.LogWarning("TaskService appears corrupted, removing from pool");
                    RemoveFromPool(pooledService);
                    pooledService = null; // Don't return to pool
                }
                
                throw;
            }
            finally
            {
                if (pooledService != null)
                {
                    pooledService.MarkAvailable();
                    ReturnToPool(pooledService);
                }
                
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task ExecuteAsync(Func<TaskService, System.Threading.Tasks.Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async taskService =>
            {
                await operation(taskService).ConfigureAwait(false);
                return true; // Dummy return value
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public TaskServicePoolStats GetStats()
        {
            var availableCount = _availableServices.Count;
            var totalCount = _allServices.Count;
            var totalOps = Interlocked.Read(ref _totalOperations);
            var failedOps = Interlocked.Read(ref _failedOperations);
            var totalWaitMs = Interlocked.Read(ref _totalWaitTimeMs);

            return new TaskServicePoolStats
            {
                TotalInstances = totalCount,
                AvailableInstances = availableCount,
                InUseInstances = totalCount - availableCount,
                TotalOperations = totalOps,
                FailedOperations = failedOps,
                AverageWaitTime = totalOps > 0 ? TimeSpan.FromMilliseconds(totalWaitMs / (double)totalOps) : TimeSpan.Zero
            };
        }

        private void InitializePool()
        {
            // Pre-create a few instances to avoid initial delay
            var initialCount = Math.Min(2, _options.TaskServicePoolSize);
            for (int i = 0; i < initialCount; i++)
            {
                var pooledService = new PooledTaskService();
                _allServices.TryAdd(pooledService.GetHashCode(), pooledService);
                _availableServices.Enqueue(pooledService);
            }
        }

        private PooledTaskService GetOrCreateTaskService()
        {
            // Try to get an available service from the queue
            if (_availableServices.TryDequeue(out var pooledService))
            {
                return pooledService;
            }

            // Create a new service if we haven't reached the pool limit
            lock (_lockObject)
            {
                if (_allServices.Count < _options.TaskServicePoolSize)
                {
                    pooledService = new PooledTaskService();
                    _allServices.TryAdd(pooledService.GetHashCode(), pooledService);
                    return pooledService;
                }
            }

            // Wait for an available service (this should be rare due to semaphore)
            var timeout = DateTime.UtcNow.Add(TimeSpan.FromSeconds(1));
            while (DateTime.UtcNow < timeout)
            {
                if (_availableServices.TryDequeue(out pooledService))
                {
                    return pooledService;
                }
                System.Threading.Tasks.Task.Delay(10).Wait();
            }

            // Fallback: create a temporary service (not pooled)
            _logger.LogWarning("Pool exhausted, creating temporary TaskService instance");
            return new PooledTaskService();
        }

        private void ReturnToPool(PooledTaskService pooledService)
        {
            if (!_allServices.ContainsKey(pooledService.GetHashCode()))
            {
                // This was a temporary service, dispose it
                pooledService.Dispose();
                return;
            }

            _availableServices.Enqueue(pooledService);
        }

        private void RemoveFromPool(PooledTaskService pooledService)
        {
            _allServices.TryRemove(pooledService.GetHashCode(), out _);
            pooledService.Dispose();
        }

        private void CleanupUnusedServices(object? state)
        {
            if (_disposed) return;

            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-10); // Remove services unused for 10+ minutes
                var servicesToRemove = new List<PooledTaskService>();

                // Find services that haven't been used recently
                foreach (var kvp in _allServices)
                {
                    var service = kvp.Value;
                    if (!service.IsInUse && service.LastUsed < cutoffTime && _allServices.Count > 2)
                    {
                        servicesToRemove.Add(service);
                    }
                }

                // Remove old services
                foreach (var service in servicesToRemove)
                {
                    RemoveFromPool(service);
                }

                if (servicesToRemove.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} unused TaskService instances from pool", servicesToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TaskService pool cleanup");
            }
        }

        private static bool IsTaskServiceCorrupted(Exception ex)
        {
            return ex switch
            {
                System.Runtime.InteropServices.COMException => true,
                System.ComponentModel.Win32Exception => true,
                ObjectDisposedException => true,
                _ => false
            };
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _semaphore?.Dispose();

            // Dispose all pooled services
            foreach (var kvp in _allServices)
            {
                kvp.Value.Dispose();
            }

            _allServices.Clear();
            
            // Clear the queue
            while (_availableServices.TryDequeue(out var service))
            {
                // Services are already disposed above
            }

            _logger.LogInformation("TaskService pool disposed");
        }
    }
} 