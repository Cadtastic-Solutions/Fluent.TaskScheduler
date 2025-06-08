using Fluent.TaskScheduler.Services;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents the health status of the task scheduler system.
    /// </summary>
    public class TaskSchedulerHealthStatus
    {
        /// <summary>
        /// Gets or sets whether the task scheduler system is healthy overall.
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the overall health score (0.0 to 1.0).
        /// </summary>
        public double HealthScore { get; set; }

        /// <summary>
        /// Gets or sets when the health check was performed.
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Gets or sets the TaskService pool statistics.
        /// </summary>
        public TaskServicePoolStats? PoolStats { get; set; }

        /// <summary>
        /// Gets or sets whether the Task Scheduler service is available.
        /// </summary>
        public bool ServiceAvailable { get; set; }

        /// <summary>
        /// Gets or sets whether the task manager is functioning properly.
        /// </summary>
        public bool TaskManagerHealthy { get; set; }

        /// <summary>
        /// Gets or sets whether the retry policy service is functioning properly.
        /// </summary>
        public bool RetryServiceHealthy { get; set; }

        /// <summary>
        /// Gets or sets any health check warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets any health check errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets additional health check details.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
} 