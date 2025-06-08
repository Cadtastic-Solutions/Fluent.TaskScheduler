using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;
using TaskStatus = Fluent.TaskScheduler.Models.TaskStatus;
using WinTask = Microsoft.Win32.TaskScheduler.Task;
using SystemTask = System.Threading.Tasks.Task;
using System.IO;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents a single execution history entry from Windows Task Scheduler.
    /// </summary>
    public class TaskExecutionHistory
    {
        /// <summary>
        /// The date and time when the task execution started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The date and time when the task execution finished (null if still running).
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The duration of the task execution.
        /// </summary>
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        /// <summary>
        /// The result/exit code of the task execution.
        /// 0 typically indicates success, non-zero indicates failure.
        /// </summary>
        public uint ResultCode { get; set; }

        /// <summary>
        /// Whether the task execution was successful.
        /// </summary>
        public bool IsSuccess => ResultCode == 0;

        /// <summary>
        /// The task result status description.
        /// </summary>
        public string? ResultDescription { get; set; }

        /// <summary>
        /// The user account under which the task was executed.
        /// </summary>
        public string? UserAccount { get; set; }

        /// <summary>
        /// Additional details about the task execution.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// The task execution state (Running, Completed, Failed, etc.).
        /// </summary>
        public string? State { get; set; }
    }
} 