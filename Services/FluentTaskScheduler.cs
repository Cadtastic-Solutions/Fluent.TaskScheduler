using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Exceptions;
using Fluent.TaskScheduler.Builders;
using Fluent.TaskScheduler.Examples;

namespace Fluent.TaskScheduler.Services
{
    /// <summary>
    /// Main facade implementation for simplified task scheduling operations.
    /// Provides a clean, easy-to-use API that hides the complexity of the underlying services.
    /// </summary>
    public class FluentTaskScheduler : IFluentTaskScheduler
    {
        private readonly ITaskSchedulerManager _taskManager;
        private readonly IRetryPolicyService _retryService;
        private readonly ITaskServicePool _taskServicePool;
        private readonly ILogger<FluentTaskScheduler> _logger;

        // Performance tracking
        private long _tasksCreated = 0;
        private long _tasksDeleted = 0;
        private long _tasksStarted = 0;
        private long _tasksStopped = 0;
        private long _successfulOperations = 0;
        private long _failedOperations = 0;
        private readonly List<double> _operationDurations = new List<double>();
        private readonly object _statsLock = new object();

        public FluentTaskScheduler(
            ITaskSchedulerManager taskManager,
            IRetryPolicyService retryService,
            ITaskServicePool taskServicePool,
            ILogger<FluentTaskScheduler> logger)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
            _taskServicePool = taskServicePool ?? throw new ArgumentNullException(nameof(taskServicePool));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public ITaskBuilder CreateTask(string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskName))
                throw new ArgumentException("Task name cannot be null or empty.", nameof(taskName));

            _logger.LogDebug("Creating task builder for task: {TaskName}", taskName);
            return new TaskBuilder(this, taskName);
        }

        /// <inheritdoc />
        public async Task<bool> CreateTaskAsync(ISchedulableTask task)
        {
            return await ExecuteOperationAsync(async () =>
            {
                await _taskManager.CreateScheduledTaskAsync(task);
                Interlocked.Increment(ref _tasksCreated);
                _logger.LogInformation("Successfully created task: {TaskName}", task.TaskName);
            }, $"CreateTask-{task.TaskName}");
        }

        /// <inheritdoc />
        public async Task<bool> StartTaskAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await StartTaskAsync(task);
        }

        /// <inheritdoc />
        public async Task<bool> StartTaskAsync(ISchedulableTask task)
        {
            return await ExecuteOperationAsync(async () =>
            {
                await _taskManager.StartScheduledTaskAsync(task);
                Interlocked.Increment(ref _tasksStarted);
                _logger.LogInformation("Successfully started task: {TaskName}", task.TaskName);
            }, $"StartTask-{task.TaskName}");
        }

        /// <inheritdoc />
        public async Task<bool> StopTaskAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await StopTaskAsync(task);
        }

        /// <inheritdoc />
        public async Task<bool> StopTaskAsync(ISchedulableTask task)
        {
            return await ExecuteOperationAsync(async () =>
            {
                await _taskManager.StopScheduledTaskAsync(task);
                Interlocked.Increment(ref _tasksStopped);
                _logger.LogInformation("Successfully stopped task: {TaskName}", task.TaskName);
            }, $"StopTask-{task.TaskName}");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTaskAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await DeleteTaskAsync(task);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteTaskAsync(ISchedulableTask task)
        {
            return await ExecuteOperationAsync(async () =>
            {
                await _taskManager.DeleteScheduledTaskAsync(task);
                Interlocked.Increment(ref _tasksDeleted);
                _logger.LogInformation("Successfully deleted task: {TaskName}", task.TaskName);
            }, $"DeleteTask-{task.TaskName}");
        }

        /// <inheritdoc />
        public async Task<Models.TaskStatus?> GetTaskStatusAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await GetTaskStatusAsync(task);
        }

        /// <inheritdoc />
        public async Task<Models.TaskStatus?> GetTaskStatusAsync(ISchedulableTask task)
        {
            try
            {
                var status = await _taskManager.GetScheduledTaskStatusAsync(task);
                _logger.LogDebug("Retrieved status for task {TaskName}: {Status}", task.TaskName, status);
                return status;
            }
            catch (TaskNotFoundException)
            {
                _logger.LogDebug("Task not found: {TaskName}", task.TaskName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for task: {TaskName}", task.TaskName);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TaskExistsAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await TaskExistsAsync(task);
        }

        /// <inheritdoc />
        public async Task<bool> TaskExistsAsync(ISchedulableTask task)
        {
            try
            {
                return await _taskManager.ScheduledTaskExistsAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if task exists: {TaskName}", task.TaskName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetNextRunTimeAsync(string taskName)
        {
            var task = CreateTaskFromName(taskName);
            return await GetNextRunTimeAsync(task);
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetNextRunTimeAsync(ISchedulableTask task)
        {
            try
            {
                return await _taskManager.GetNextRunTimeAsync(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next run time for task: {TaskName}", task.TaskName);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<TaskSchedulerHealthStatus> CheckHealthAsync()
        {
            var healthStatus = new TaskSchedulerHealthStatus
            {
                CheckedAt = DateTime.UtcNow
            };

            var healthChecks = new List<(string Name, bool IsHealthy, string Details)>();

            // Check TaskService pool health
            try
            {
                var poolStats = _taskServicePool.GetStats();
                var poolHealthy = poolStats.TotalInstances > 0 && 
                                 (poolStats.TotalOperations == 0 || poolStats.FailedOperations < poolStats.TotalOperations * 0.5);
                
                healthChecks.Add(("TaskService Pool", poolHealthy, 
                    $"Instances: {poolStats.TotalInstances}, Operations: {poolStats.TotalOperations}, Failures: {poolStats.FailedOperations}"));
                
                healthStatus.PoolStats = poolStats;
            }
            catch (Exception ex)
            {
                healthChecks.Add(("TaskService Pool", false, $"Error: {ex.Message}"));
                healthStatus.Errors.Add($"TaskService Pool error: {ex.Message}");
            }

            // Check task manager functionality
            try
            {
                var testTask = new SimpleSchedulableTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    TaskName = "Health Check Test Task",
                    Description = "Temporary task for health checking",
                    TaskType = "HealthCheck",
                    Status = SchedulableTaskStatus.Inactive
                };

                var taskName = _taskManager.GetWindowsTaskName(testTask);
                var taskPath = _taskManager.GetWindowsTaskPath(testTask);
                var exists = await _taskManager.ScheduledTaskExistsAsync(testTask);

                healthChecks.Add(("Task Manager", true, "Basic operations functional"));
                healthStatus.TaskManagerHealthy = true;
            }
            catch (Exception ex)
            {
                healthChecks.Add(("Task Manager", false, $"Error: {ex.Message}"));
                healthStatus.TaskManagerHealthy = false;
                healthStatus.Errors.Add($"Task Manager error: {ex.Message}");
            }

            // Check retry service
            try
            {
                await _retryService.ExecuteWithRetryAsync(async () =>
                {
                    await Task.Delay(1);
                }, new RetryPolicyOptions { RetryCount = 1 }, "HealthCheck");

                healthStatus.RetryServiceHealthy = true;
            }
            catch (Exception ex)
            {
                healthStatus.RetryServiceHealthy = false;
                healthStatus.Errors.Add($"Retry Service error: {ex.Message}");
            }

            // Calculate overall health
            var healthyChecks = healthChecks.Count(c => c.IsHealthy);
            healthStatus.HealthScore = healthChecks.Count > 0 ? (double)healthyChecks / healthChecks.Count : 0.0;
            healthStatus.IsHealthy = healthStatus.HealthScore >= 0.8; // 80% threshold
            healthStatus.ServiceAvailable = healthStatus.TaskManagerHealthy;

            // Add performance warnings
            if (healthStatus.PoolStats != null)
            {
                if (healthStatus.PoolStats.AverageWaitTime.TotalMilliseconds > 1000)
                {
                    healthStatus.Warnings.Add("High average wait time detected. Consider increasing TaskServicePoolSize.");
                }

                if (healthStatus.PoolStats.InUseInstances == healthStatus.PoolStats.TotalInstances)
                {
                    healthStatus.Warnings.Add("Pool may be at capacity. Consider increasing TaskServicePoolSize.");
                }
            }

            _logger.LogInformation("Health check completed. Overall health: {IsHealthy}, Score: {HealthScore:F2}", 
                healthStatus.IsHealthy, healthStatus.HealthScore);

            return healthStatus;
        }

        /// <inheritdoc />
        public async Task<TaskSchedulerPerformanceStats> GetPerformanceStatsAsync()
        {
            var stats = new TaskSchedulerPerformanceStats
            {
                CollectedAt = DateTime.UtcNow,
                PoolStats = _taskServicePool.GetStats()
            };

            lock (_statsLock)
            {
                stats.TotalTasksCreated = _tasksCreated;
                stats.TotalTasksDeleted = _tasksDeleted;
                stats.TotalTasksStarted = _tasksStarted;
                stats.TotalTasksStopped = _tasksStopped;
                stats.TotalSuccessfulOperations = _successfulOperations;
                stats.TotalFailedOperations = _failedOperations;

                var totalOperations = _successfulOperations + _failedOperations;
                stats.SuccessRate = totalOperations > 0 ? (_successfulOperations / (double)totalOperations) * 100.0 : 100.0;

                if (_operationDurations.Count > 0)
                {
                    stats.AverageOperationDuration = _operationDurations.Average();
                }
            }

            return await Task.FromResult(stats);
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteWithRetryAsync(Func<Task> operation, string operationName)
        {
            return await ExecuteOperationAsync(operation, operationName);
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyOptions retryPolicy, string operationName)
        {
            return await ExecuteOperationAsync(operation, operationName, retryPolicy);
        }

        private async Task<bool> ExecuteOperationAsync(Func<Task> operation, string operationName, RetryPolicyOptions? customRetryPolicy = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (customRetryPolicy != null)
                {
                    await _retryService.ExecuteWithRetryAsync(operation, customRetryPolicy, operationName);
                }
                else
                {
                    await _retryService.ExecuteWithRetryAsync(operation, operationName);
                }

                stopwatch.Stop();
                RecordOperationSuccess(stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordOperationFailure(stopwatch.ElapsedMilliseconds);
                _logger.LogError(ex, "Operation failed: {OperationName}", operationName);
                return false;
            }
        }

        private void RecordOperationSuccess(long durationMs)
        {
            Interlocked.Increment(ref _successfulOperations);
            lock (_statsLock)
            {
                _operationDurations.Add(durationMs);
                // Keep only the last 1000 durations to prevent memory growth
                if (_operationDurations.Count > 1000)
                {
                    _operationDurations.RemoveAt(0);
                }
            }
        }

        private void RecordOperationFailure(long durationMs)
        {
            Interlocked.Increment(ref _failedOperations);
            lock (_statsLock)
            {
                _operationDurations.Add(durationMs);
                if (_operationDurations.Count > 1000)
                {
                    _operationDurations.RemoveAt(0);
                }
            }
        }

        private static SimpleSchedulableTask CreateTaskFromName(string taskName)
        {
            return new SimpleSchedulableTask
            {
                TaskId = taskName,
                TaskName = taskName,
                TaskType = "Generic"
            };
        }
    }
} 