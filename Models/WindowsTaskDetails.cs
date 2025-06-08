using System.Text.Json.Serialization;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents the Windows Task Scheduler details for a schedulable task.
    /// Contains information about the scheduled task created in Windows Task Scheduler.
    /// </summary>
    public class WindowsTaskDetails
    {
        /// <summary>
        /// The name of the Windows Task in Task Scheduler.
        /// </summary>
        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// The full path of the Windows Task including folder structure.
        /// </summary>
        [JsonPropertyName("taskPath")]
        public string TaskPath { get; set; } = string.Empty;

        /// <summary>
        /// Description of the Windows Task.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When the Windows Task was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Current status of the Windows Task.
        /// </summary>
        [JsonPropertyName("status")]
        public TaskStatus Status { get; set; }

        /// <summary>
        /// When the Windows Task status was last updated.
        /// </summary>
        [JsonPropertyName("statusUpdatedAt")]
        public DateTime StatusUpdatedAt { get; set; }

        /// <summary>
        /// Next scheduled run time from Windows Task Scheduler.
        /// </summary>
        [JsonPropertyName("nextRunTime")]
        public DateTime? NextRunTime { get; set; }

        /// <summary>
        /// Last time the Windows Task was executed.
        /// </summary>
        [JsonPropertyName("lastRunTime")]
        public DateTime? LastRunTime { get; set; }

        /// <summary>
        /// Result code from the last Windows Task execution.
        /// 0 typically indicates success, non-zero indicates failure.
        /// </summary>
        [JsonPropertyName("lastTaskResult")]
        public uint LastTaskResult { get; set; }

        /// <summary>
        /// Whether the last Windows Task execution was successful.
        /// </summary>
        [JsonIgnore]
        public bool LastExecutionSuccessful => LastTaskResult == 0;

        /// <summary>
        /// Updates the Windows Task status and timestamp.
        /// </summary>
        /// <param name="newStatus">The new status to set.</param>
        public void UpdateStatus(TaskStatus newStatus)
        {
            Status = newStatus;
            StatusUpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Updates the last execution information.
        /// </summary>
        /// <param name="resultCode">The result code from the execution.</param>
        public void UpdateLastExecution(uint resultCode)
        {
            LastRunTime = DateTime.Now;
            LastTaskResult = resultCode;
            StatusUpdatedAt = DateTime.Now;
        }
    }
} 