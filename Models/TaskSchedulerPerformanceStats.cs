using Fluent.TaskScheduler.Services;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents performance statistics for the task scheduler system.
    /// </summary>
    public class TaskSchedulerPerformanceStats
    {
        /// <summary>
        /// Gets or sets when the statistics were collected.
        /// </summary>
        public DateTime CollectedAt { get; set; }

        /// <summary>
        /// Gets or sets the TaskService pool statistics.
        /// </summary>
        public TaskServicePoolStats PoolStats { get; set; } = new TaskServicePoolStats();

        /// <summary>
        /// Gets or sets the total number of tasks created since startup.
        /// </summary>
        public long TotalTasksCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of tasks deleted since startup.
        /// </summary>
        public long TotalTasksDeleted { get; set; }

        /// <summary>
        /// Gets or sets the total number of tasks started since startup.
        /// </summary>
        public long TotalTasksStarted { get; set; }

        /// <summary>
        /// Gets or sets the total number of tasks stopped since startup.
        /// </summary>
        public long TotalTasksStopped { get; set; }

        /// <summary>
        /// Gets or sets the total number of successful operations since startup.
        /// </summary>
        public long TotalSuccessfulOperations { get; set; }

        /// <summary>
        /// Gets or sets the total number of failed operations since startup.
        /// </summary>
        public long TotalFailedOperations { get; set; }

        /// <summary>
        /// Gets or sets the average operation duration in milliseconds.
        /// </summary>
        public double AverageOperationDuration { get; set; }

        /// <summary>
        /// Gets or sets the success rate as a percentage (0.0 to 100.0).
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the number of retry attempts made since startup.
        /// </summary>
        public long TotalRetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the number of operations that succeeded after retries.
        /// </summary>
        public long OperationsSucceededAfterRetry { get; set; }

        /// <summary>
        /// Gets or sets the number of circuit breaker activations since startup.
        /// </summary>
        public long CircuitBreakerActivations { get; set; }

        /// <summary>
        /// Gets or sets additional performance metrics.
        /// </summary>
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new Dictionary<string, object>();
    }
} 