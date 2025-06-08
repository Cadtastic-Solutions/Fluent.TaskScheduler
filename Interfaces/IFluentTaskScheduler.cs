using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Configuration;

namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Main facade interface for simplified task scheduling operations.
    /// Provides a clean, easy-to-use API that hides the complexity of the underlying services.
    /// </summary>
    public interface IFluentTaskScheduler
    {
        /// <summary>
        /// Creates a new task with the specified name and returns a fluent builder for configuration.
        /// </summary>
        /// <param name="taskName">The name of the task to create.</param>
        /// <returns>A fluent task builder for configuring the task.</returns>
        ITaskBuilder CreateTask(string taskName);

        /// <summary>
        /// Creates a scheduled task directly without using the fluent builder.
        /// </summary>
        /// <param name="task">The task to create.</param>
        /// <returns>True if the task was created successfully, false otherwise.</returns>
        Task<bool> CreateTaskAsync(ISchedulableTask task);

        /// <summary>
        /// Starts an existing scheduled task.
        /// </summary>
        /// <param name="taskName">The name of the task to start.</param>
        /// <returns>True if the task was started successfully, false otherwise.</returns>
        Task<bool> StartTaskAsync(string taskName);

        /// <summary>
        /// Starts an existing scheduled task.
        /// </summary>
        /// <param name="task">The task to start.</param>
        /// <returns>True if the task was started successfully, false otherwise.</returns>
        Task<bool> StartTaskAsync(ISchedulableTask task);

        /// <summary>
        /// Stops a running scheduled task.
        /// </summary>
        /// <param name="taskName">The name of the task to stop.</param>
        /// <returns>True if the task was stopped successfully, false otherwise.</returns>
        Task<bool> StopTaskAsync(string taskName);

        /// <summary>
        /// Stops a running scheduled task.
        /// </summary>
        /// <param name="task">The task to stop.</param>
        /// <returns>True if the task was stopped successfully, false otherwise.</returns>
        Task<bool> StopTaskAsync(ISchedulableTask task);

        /// <summary>
        /// Deletes a scheduled task.
        /// </summary>
        /// <param name="taskName">The name of the task to delete.</param>
        /// <returns>True if the task was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteTaskAsync(string taskName);

        /// <summary>
        /// Deletes a scheduled task.
        /// </summary>
        /// <param name="task">The task to delete.</param>
        /// <returns>True if the task was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteTaskAsync(ISchedulableTask task);

        /// <summary>
        /// Gets the status of a scheduled task.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <returns>The task status, or null if the task doesn't exist.</returns>
        Task<Models.TaskStatus?> GetTaskStatusAsync(string taskName);

        /// <summary>
        /// Gets the status of a scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>The task status, or null if the task doesn't exist.</returns>
        Task<Models.TaskStatus?> GetTaskStatusAsync(ISchedulableTask task);

        /// <summary>
        /// Checks if a task exists.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <returns>True if the task exists, false otherwise.</returns>
        Task<bool> TaskExistsAsync(string taskName);

        /// <summary>
        /// Checks if a task exists.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>True if the task exists, false otherwise.</returns>
        Task<bool> TaskExistsAsync(ISchedulableTask task);

        /// <summary>
        /// Gets the next run time for a scheduled task.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <returns>The next run time, or null if the task doesn't exist or is not scheduled.</returns>
        Task<DateTime?> GetNextRunTimeAsync(string taskName);

        /// <summary>
        /// Gets the next run time for a scheduled task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>The next run time, or null if the task doesn't exist or is not scheduled.</returns>
        Task<DateTime?> GetNextRunTimeAsync(ISchedulableTask task);

        /// <summary>
        /// Performs a comprehensive health check of the task scheduler system.
        /// </summary>
        /// <returns>Health status information.</returns>
        Task<TaskSchedulerHealthStatus> CheckHealthAsync();

        /// <summary>
        /// Gets performance statistics for the task scheduler system.
        /// </summary>
        /// <returns>Performance statistics.</returns>
        Task<TaskSchedulerPerformanceStats> GetPerformanceStatsAsync();

        /// <summary>
        /// Executes an operation with retry logic using the configured retry policy.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">A name for the operation (used for logging).</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> ExecuteWithRetryAsync(Func<Task> operation, string operationName);

        /// <summary>
        /// Executes an operation with retry logic using a custom retry policy.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="retryPolicy">Custom retry policy options.</param>
        /// <param name="operationName">A name for the operation (used for logging).</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        Task<bool> ExecuteWithRetryAsync(Func<Task> operation, RetryPolicyOptions retryPolicy, string operationName);
    }
} 