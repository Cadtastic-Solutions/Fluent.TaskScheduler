using System.Text.Json.Serialization;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents the type of interval for task scheduling.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleIntervalType
    {
        /// <summary>Schedule based on minutes (e.g., every 30 minutes).</summary>
        Minutes,
        /// <summary>Schedule based on hours (e.g., every 2 hours).</summary>
        Hours,
        /// <summary>Schedule based on days (e.g., every 3 days).</summary>
        Days,
        /// <summary>Schedule based on weeks (e.g., every 2 weeks).</summary>
        Weeks
    }
} 