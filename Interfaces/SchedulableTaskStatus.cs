using System.Text.Json.Serialization;

namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Status enumeration for schedulable tasks.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SchedulableTaskStatus
    {
        /// <summary>Task is inactive and will not be scheduled.</summary>
        Inactive,
        /// <summary>Task is active and scheduled for execution.</summary>
        Active,
        /// <summary>Task is currently running.</summary>
        Running,
        /// <summary>Task is paused and will not be scheduled until resumed.</summary>
        Paused,
        /// <summary>Task has failed during execution.</summary>
        Failed,
        /// <summary>Task has completed successfully (for one-time tasks).</summary>
        Completed
    }
} 