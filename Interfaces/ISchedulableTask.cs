using System.ComponentModel;
using System.Text.Json.Serialization;
using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Generic interface for tasks that can be scheduled and executed by the Windows Task Scheduler.
    /// Contains the essential properties needed for scheduling and execution management.
    /// </summary>
    public interface ISchedulableTask : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique identifier for the schedulable task.
        /// </summary>
        string TaskId { get; set; }

        /// <summary>
        /// Human-readable name for the schedulable task.
        /// </summary>
        string TaskName { get; set; }

        /// <summary>
        /// Optional description of what the task does.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// The type of task - determines which executor will handle it.
        /// </summary>
        string TaskType { get; set; }

        /// <summary>
        /// Current status of the schedulable task.
        /// </summary>
        SchedulableTaskStatus Status { get; set; }

        /// <summary>
        /// When the task was created.
        /// </summary>
        DateTime Created { get; set; }

        /// <summary>
        /// When the task was last modified.
        /// </summary>
        DateTime Modified { get; set; }

        /// <summary>
        /// When the task was last executed.
        /// </summary>
        DateTime? LastExecuted { get; set; }

        /// <summary>
        /// When the task is scheduled to run next.
        /// </summary>
        DateTime? NextScheduledExecution { get; set; }

        /// <summary>
        /// Number of times the task has been executed.
        /// </summary>
        int ExecutionCount { get; set; }

        /// <summary>
        /// Windows Task Scheduler details for this task (null if task is inactive or no scheduled task created).
        /// </summary>
        WindowsTaskDetails? WindowsTaskDetails { get; set; }

        /// <summary>
        /// User account configuration for running the scheduled task.
        /// </summary>
        TaskUserAccount? UserAccount { get; set; }

        /// <summary>
        /// Schedule configuration for the task.
        /// </summary>
        TaskSchedule? Schedule { get; set; }

        /// <summary>
        /// The executable program path to run.
        /// </summary>
        string? ExecutablePath { get; set; }

        /// <summary>
        /// Command-line arguments for the executable.
        /// </summary>
        string? Arguments { get; set; }

        /// <summary>
        /// Working directory for the executable.
        /// </summary>
        string? WorkingDirectory { get; set; }

        /// <summary>
        /// Updates the task's modification timestamp.
        /// </summary>
        void Touch();

        /// <summary>
        /// Marks the task as executed and updates scheduling information.
        /// </summary>
        void MarkExecuted();

        /// <summary>
        /// Updates the next execution time based on the task's schedule.
        /// </summary>
        void UpdateNextExecutionTime();
    }


} 