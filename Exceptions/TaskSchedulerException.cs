using System;

namespace Fluent.TaskScheduler.Exceptions
{
    /// <summary>
    /// Base exception for all Task Scheduler related errors.
    /// </summary>
    public class TaskSchedulerException : Exception
    {
        public string? TaskId { get; }
        public string? TaskName { get; }

        public TaskSchedulerException() : base() { }

        public TaskSchedulerException(string message) : base(message) { }

        public TaskSchedulerException(string message, Exception innerException) : base(message, innerException) { }

        public TaskSchedulerException(string message, string? taskId, string? taskName) : base(message)
        {
            TaskId = taskId;
            TaskName = taskName;
        }

        public TaskSchedulerException(string message, Exception innerException, string? taskId, string? taskName) 
            : base(message, innerException)
        {
            TaskId = taskId;
            TaskName = taskName;
        }
    }

    /// <summary>
    /// Exception thrown when a task operation fails due to insufficient permissions.
    /// </summary>
    public class TaskSchedulerPermissionException : TaskSchedulerException
    {
        public TaskSchedulerPermissionException() : base() { }

        public TaskSchedulerPermissionException(string message) : base(message) { }

        public TaskSchedulerPermissionException(string message, Exception innerException) : base(message, innerException) { }

        public TaskSchedulerPermissionException(string message, string? taskId, string? taskName) 
            : base(message, taskId, taskName) { }

        public TaskSchedulerPermissionException(string message, Exception innerException, string? taskId, string? taskName) 
            : base(message, innerException, taskId, taskName) { }
    }

    /// <summary>
    /// Exception thrown when the Task Scheduler service is unavailable or not responding.
    /// </summary>
    public class TaskSchedulerServiceException : TaskSchedulerException
    {
        public TaskSchedulerServiceException() : base() { }

        public TaskSchedulerServiceException(string message) : base(message) { }

        public TaskSchedulerServiceException(string message, Exception innerException) : base(message, innerException) { }

        public TaskSchedulerServiceException(string message, string? taskId, string? taskName) 
            : base(message, taskId, taskName) { }

        public TaskSchedulerServiceException(string message, Exception innerException, string? taskId, string? taskName) 
            : base(message, innerException, taskId, taskName) { }
    }

    /// <summary>
    /// Exception thrown when a task configuration is invalid.
    /// </summary>
    public class TaskConfigurationException : TaskSchedulerException
    {
        public string? PropertyName { get; }

        public TaskConfigurationException() : base() { }

        public TaskConfigurationException(string message) : base(message) { }

        public TaskConfigurationException(string message, Exception innerException) : base(message, innerException) { }

        public TaskConfigurationException(string message, string? propertyName) : base(message)
        {
            PropertyName = propertyName;
        }

        public TaskConfigurationException(string message, string? propertyName, string? taskId, string? taskName) 
            : base(message, taskId, taskName)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// Exception thrown when a task operation times out.
    /// </summary>
    public class TaskOperationTimeoutException : TaskSchedulerException
    {
        public TimeSpan Timeout { get; }

        public TaskOperationTimeoutException(TimeSpan timeout) : base($"Task operation timed out after {timeout}")
        {
            Timeout = timeout;
        }

        public TaskOperationTimeoutException(string message, TimeSpan timeout) : base(message)
        {
            Timeout = timeout;
        }

        public TaskOperationTimeoutException(string message, TimeSpan timeout, string? taskId, string? taskName) 
            : base(message, taskId, taskName)
        {
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Exception thrown when a task is not found.
    /// </summary>
    public class TaskNotFoundException : TaskSchedulerException
    {
        public TaskNotFoundException() : base() { }

        public TaskNotFoundException(string message) : base(message) { }

        public TaskNotFoundException(string message, string? taskId, string? taskName) 
            : base(message, taskId, taskName) { }
    }
} 