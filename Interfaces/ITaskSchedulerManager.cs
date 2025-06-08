using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;
using TaskStatus = Fluent.TaskScheduler.Models.TaskStatus;
using WinTask = Microsoft.Win32.TaskScheduler.Task;
using SystemTask = System.Threading.Tasks.Task;
using System.IO;
using Fluent.TaskScheduler.Models;


namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Interface for managing Windows Task Scheduler tasks for schedulable tasks.
    /// Provides comprehensive task scheduling operations including creation, deletion, status management, and execution control.
    /// </summary>
    public interface ITaskSchedulerManager
    {
        /// <summary>
        /// Creates and schedules a Windows Task for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to create a Windows Task for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task CreateScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Deletes the Windows Task for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task should be deleted.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task DeleteScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Gets the current status of the Windows Task for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to check Windows Task status for.</param>
        /// <returns>The current Windows Task status.</returns>
        Task<Models.TaskStatus> GetScheduledTaskStatusAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Starts (runs) the Windows Task for the specified schedulable task immediately.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task should be started.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task StartScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Stops the Windows Task for the specified schedulable task if it's currently running.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task should be stopped.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task StopScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Enables the Windows Task for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task should be enabled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task EnableScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Disables the Windows Task for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task should be disabled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task DisableScheduledTaskAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Gets the Windows Task name for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get Windows Task name for.</param>
        /// <returns>The Windows Task name.</returns>
        string GetWindowsTaskName(ISchedulableTask schedulableTask);

        /// <summary>
        /// Gets the full Windows Task path including folder structure for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get Windows Task path for.</param>
        /// <returns>The full Windows Task path.</returns>
        string GetWindowsTaskPath(ISchedulableTask schedulableTask);

        /// <summary>
        /// Gets the next scheduled run time directly from the Windows Task Scheduler.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get the next run time for.</param>
        /// <returns>The next scheduled run time, or null if not available.</returns>
        Task<DateTime?> GetNextRunTimeAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Gets a description for the Windows Task based on the schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get description for.</param>
        /// <returns>A description for the Windows Task.</returns>
        string GetWindowsTaskDescription(ISchedulableTask schedulableTask);

        /// <summary>
        /// Checks if a Windows Task exists for the specified schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to check for existing Windows Task.</param>
        /// <returns>True if Windows Task exists, false otherwise.</returns>
        Task<bool> ScheduledTaskExistsAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Updates the schedule for an existing Windows Task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows Task schedule should be updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        System.Threading.Tasks.Task UpdateScheduledTaskScheduleAsync(ISchedulableTask schedulableTask);

        /// <summary>
        /// Validates that the task executor executable is available for task execution.
        /// </summary>
        /// <returns>True if task executor is found, false otherwise.</returns>
        bool IsTaskExecutorAvailable();

        /// <summary>
        /// Gets the full path to the task executor executable.
        /// </summary>
        /// <returns>The full path to the task executor executable</returns>
        string GetTaskExecutorPath();

        /// <summary>
        /// Gets the execution history for a schedulable task's Windows Task from Task Scheduler.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get execution history for.</param>
        /// <param name="maxEntries">Maximum number of history entries to retrieve (default: 50)</param>
        /// <returns>A collection of task execution history entries.</returns>
        Task<IEnumerable<TaskExecutionHistory>> GetTaskExecutionHistoryAsync(ISchedulableTask schedulableTask, int maxEntries = 50);
    }
} 