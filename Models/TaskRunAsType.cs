using System.Text.Json.Serialization;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Defines the types of accounts that can run a scheduled task.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskRunAsType
    {
        /// <summary>
        /// Run as the currently logged-in user.
        /// </summary>
        CurrentUser,

        /// <summary>
        /// Run as the Local System account (highest privileges).
        /// </summary>
        LocalSystem,

        /// <summary>
        /// Run as the Local Service account (reduced privileges).
        /// </summary>
        LocalService,

        /// <summary>
        /// Run as the Network Service account (network access).
        /// </summary>
        NetworkService,

        /// <summary>
        /// Run as a specific user account (requires username/password).
        /// </summary>
        SpecificUser
    }
} 