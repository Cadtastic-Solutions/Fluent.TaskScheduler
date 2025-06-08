using System;

namespace Fluent.TaskScheduler.Configuration
{
    /// <summary>
    /// Configuration options for the Fluent Task Scheduler library.
    /// </summary>
    public class TaskSchedulerOptions
    {
        /// <summary>
        /// The folder name in Windows Task Scheduler where tasks will be created.
        /// Default: "Fluent.TaskScheduler"
        /// </summary>
        public string TaskFolder { get; set; } = "Fluent.TaskScheduler";

        /// <summary>
        /// Default number of retry attempts for failed operations.
        /// Default: 3
        /// </summary>
        public int DefaultRetryCount { get; set; } = 3;

        /// <summary>
        /// Base delay for exponential backoff retry strategy.
        /// Default: 1 second
        /// </summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Maximum delay for exponential backoff retry strategy.
        /// Default: 30 seconds
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for individual task operations.
        /// Default: 30 seconds
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum number of execution history entries to retrieve by default.
        /// Default: 50
        /// </summary>
        public int DefaultMaxHistoryEntries { get; set; } = 50;

        /// <summary>
        /// Whether to enable detailed logging for debugging purposes.
        /// Default: false
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Circuit breaker failure threshold before opening the circuit.
        /// Default: 5
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Circuit breaker timeout before attempting to close the circuit.
        /// Default: 60 seconds
        /// </summary>
        public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Whether to automatically create the task folder if it doesn't exist.
        /// Default: true
        /// </summary>
        public bool AutoCreateTaskFolder { get; set; } = true;

        /// <summary>
        /// Whether to validate task configurations before creating scheduled tasks.
        /// Default: true
        /// </summary>
        public bool ValidateTaskConfiguration { get; set; } = true;

        /// <summary>
        /// Pool size for TaskService instances to improve performance.
        /// Default: 5
        /// </summary>
        public int TaskServicePoolSize { get; set; } = 5;

        /// <summary>
        /// Maximum time to wait for a TaskService instance from the pool.
        /// Default: 10 seconds
        /// </summary>
        public TimeSpan TaskServicePoolTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Retry policy configuration for task operations.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Number of retry attempts.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Base delay for exponential backoff.
        /// </summary>
        public TimeSpan BaseDelay { get; set; }

        /// <summary>
        /// Maximum delay for exponential backoff.
        /// </summary>
        public TimeSpan MaxDelay { get; set; }

        /// <summary>
        /// Jitter factor to add randomness to retry delays (0.0 to 1.0).
        /// Default: 0.1 (10% jitter)
        /// </summary>
        public double JitterFactor { get; set; } = 0.1;

        /// <summary>
        /// Whether to use exponential backoff or linear backoff.
        /// Default: true (exponential)
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }
} 