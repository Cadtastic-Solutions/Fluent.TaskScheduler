using System.ComponentModel.DataAnnotations;
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
    /// Represents the user account configuration for running a scheduled task.
    /// Data Transfer Object - contains only data, no UI concerns.
    /// </summary>
    public class TaskUserAccount
    {
        private TaskRunAsType _runAsType = TaskRunAsType.CurrentUser;
        private string _username = string.Empty;
        private string _domain = string.Empty;
        private bool _runWhenLoggedOff = true;
        private bool _runWithHighestPrivileges = true;

        /// <summary>
        /// The type of account to run the task as.
        /// </summary>
        [JsonPropertyName("runAsType")]
        public TaskRunAsType RunAsType 
        { 
            get => _runAsType; 
            set => _runAsType = value;
        }

        /// <summary>
        /// Username for the account (required for SpecificUser type).
        /// </summary>
        [JsonPropertyName("username")]
        public string Username 
        { 
            get => _username; 
            set => _username = value;
        }

        /// <summary>
        /// Domain for the account (optional, defaults to local machine).
        /// </summary>
        [JsonPropertyName("domain")]
        public string Domain 
        { 
            get => _domain; 
            set => _domain = value;
        }

        /// <summary>
        /// Whether the task should run even when the user is not logged on.
        /// </summary>
        [JsonPropertyName("runWhenLoggedOff")]
        public bool RunWhenLoggedOff 
        { 
            get => _runWhenLoggedOff; 
            set => _runWhenLoggedOff = value;
        }

        /// <summary>
        /// Whether to run the task with highest available privileges.
        /// </summary>
        [JsonPropertyName("runWithHighestPrivileges")]
        public bool RunWithHighestPrivileges 
        { 
            get => _runWithHighestPrivileges; 
            set => _runWithHighestPrivileges = value;
        }

        /// <summary>
        /// Gets the display name for the account configuration.
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                return RunAsType switch
                {
                    TaskRunAsType.CurrentUser => "Current User",
                    TaskRunAsType.LocalSystem => "Local System",
                    TaskRunAsType.LocalService => "Local Service", 
                    TaskRunAsType.NetworkService => "Network Service",
                    TaskRunAsType.SpecificUser => string.IsNullOrEmpty(Domain) 
                        ? Username 
                        : $"{Domain}\\{Username}",
                    _ => "Unknown"
                };
            }
        }

        /// <summary>
        /// Gets whether this configuration requires a password.
        /// </summary>
        [JsonIgnore]
        public bool RequiresPassword => RunAsType == TaskRunAsType.SpecificUser;

        /// <summary>
        /// Gets whether this configuration is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                return RunAsType switch
                {
                    TaskRunAsType.CurrentUser => true,
                    TaskRunAsType.LocalSystem => true,
                    TaskRunAsType.LocalService => true,
                    TaskRunAsType.NetworkService => true,
                    TaskRunAsType.SpecificUser => !string.IsNullOrWhiteSpace(Username),
                    _ => false
                };
            }
        }


    }


} 