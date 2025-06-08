using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Interface for task execution history records.
    /// Represents a single execution instance of a schedulable task.
    /// </summary>
    public interface ITaskExecutionHistory
    {
        /// <summary>
        /// Gets or sets the unique identifier for this execution history record.
        /// </summary>
        Guid ExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the task that was executed.
        /// </summary>
        Guid TaskId { get; set; }

        /// <summary>
        /// Gets or sets the name of the task that was executed.
        /// </summary>
        string TaskName { get; set; }

        /// <summary>
        /// Gets or sets the start time of the task execution.
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the task execution.
        /// Null if the task is still running or failed to start.
        /// </summary>
        DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the duration of the task execution.
        /// Calculated from StartTime and EndTime.
        /// </summary>
        TimeSpan? Duration { get; }

        /// <summary>
        /// Gets or sets the execution status/state.
        /// </summary>
        string State { get; set; }

        /// <summary>
        /// Gets or sets the result code from the task execution.
        /// 0 typically indicates success, non-zero indicates failure.
        /// </summary>
        int ResultCode { get; set; }

        /// <summary>
        /// Gets or sets a description of the execution result.
        /// </summary>
        string? ResultDescription { get; set; }

        /// <summary>
        /// Gets or sets additional details about the execution.
        /// </summary>
        string? Details { get; set; }

        /// <summary>
        /// Gets or sets the user account under which the task was executed.
        /// </summary>
        string? UserAccount { get; set; }

        /// <summary>
        /// Gets or sets the Windows Task Scheduler details at the time of execution.
        /// Provides a snapshot of the task configuration when it was executed.
        /// </summary>
        WindowsTaskDetails? WindowsTaskDetails { get; set; }

        /// <summary>
        /// Gets or sets when this execution history record was created.
        /// </summary>
        DateTime CreatedAt { get; set; }
    }
} 