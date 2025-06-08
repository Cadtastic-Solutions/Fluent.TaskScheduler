using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;
using TaskStatus = Fluent.TaskScheduler.Models.TaskStatus;
using WinTask = Microsoft.Win32.TaskScheduler.Task;
using SystemTask = System.Threading.Tasks.Task;
using System.IO;
namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Task status enumeration for scheduled tasks.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskStatus
    {
        /// <summary>Task is ready to run.</summary>
        Ready,
        /// <summary>Task is currently running.</summary>
        Running,
        /// <summary>Task is disabled.</summary>
        Disabled,
        /// <summary>Task is queued to run.</summary>
        Queued,
        /// <summary>Task execution is unknown.</summary>
        Unknown
    }
} 