using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Services
{
    /// <summary>
    /// Service that integrates Windows Task Scheduler management with schedulable task lifecycle events.
    /// Handles creating, updating, and deleting scheduled tasks based on task status changes.
    /// </summary>
    public class TaskIntegrationService
    {
        private readonly ITaskSchedulerManager _taskManager;
        private readonly ILogger<TaskIntegrationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskIntegrationService"/> class.
        /// </summary>
        /// <param name="taskManager">The task scheduler manager for managing Windows scheduled tasks.</param>
        /// <param name="logger">The logger instance for logging operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="taskManager"/> or <paramref name="logger"/> is null.</exception>
        public TaskIntegrationService(
            ITaskSchedulerManager taskManager,
            ILogger<TaskIntegrationService> logger)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles schedulable task status changes and manages Windows scheduled tasks accordingly.
        /// Creates, enables, disables, or deletes scheduled tasks based on the new task status.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose status changed.</param>
        /// <param name="previousStatus">The previous task status before the change.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> is null.</exception>
        public async Task HandleTaskStatusChangeAsync(ISchedulableTask schedulableTask, SchedulableTaskStatus previousStatus)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));

            try
            {
                switch (schedulableTask.Status)
                {
                    case SchedulableTaskStatus.Active:
                        await HandleTaskActivatedAsync(schedulableTask, previousStatus);
                        break;

                    case SchedulableTaskStatus.Inactive:
                        await HandleTaskDeactivatedAsync(schedulableTask, previousStatus);
                        break;

                    case SchedulableTaskStatus.Running:
                        await HandleTaskRunningAsync(schedulableTask);
                        break;

                    default:
                        // For other statuses (Paused, Failed, Completed), update task status if Windows task exists
                        if (schedulableTask.WindowsTaskDetails != null)
                        {
                            await UpdateWindowsTaskStatusAsync(schedulableTask);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle task status change for task: {TaskName}", schedulableTask.TaskName);
                throw;
            }
        }

        /// <summary>
        /// Updates execution history with current Windows task details for record keeping.
        /// Creates a snapshot of the current task state for the execution history.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task being executed.</param>
        /// <param name="executionHistory">The execution history record to update with task details.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> or <paramref name="executionHistory"/> is null.</exception>
        public void UpdateExecutionHistoryWithTaskDetails(ISchedulableTask schedulableTask, ITaskExecutionHistory executionHistory)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));
            if (executionHistory == null)
                throw new ArgumentNullException(nameof(executionHistory));

            if (schedulableTask.WindowsTaskDetails != null)
            {
                // Clone the task details for the execution history
                executionHistory.WindowsTaskDetails = new WindowsTaskDetails
                {
                    TaskName = schedulableTask.WindowsTaskDetails.TaskName,
                    TaskPath = schedulableTask.WindowsTaskDetails.TaskPath,
                    Description = schedulableTask.WindowsTaskDetails.Description,
                    CreatedAt = schedulableTask.WindowsTaskDetails.CreatedAt,
                    Status = schedulableTask.WindowsTaskDetails.Status,
                    StatusUpdatedAt = schedulableTask.WindowsTaskDetails.StatusUpdatedAt,
                    NextRunTime = schedulableTask.WindowsTaskDetails.NextRunTime,
                    LastRunTime = DateTime.Now,
                    LastTaskResult = 0 // Will be updated by the task runner
                };
            }
        }

        /// <summary>
        /// Handles schedulable task schedule updates by synchronizing the Windows scheduled task with the new schedule.
        /// Updates the task schedule in Windows Task Scheduler and refreshes the next run time.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose schedule was updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> is null.</exception>
        public async Task HandleTaskScheduleUpdatedAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));

            if (schedulableTask.WindowsTaskDetails != null && schedulableTask.Status == SchedulableTaskStatus.Active)
            {
                try
                {
                    await _taskManager.UpdateScheduledTaskScheduleAsync(schedulableTask);
                    schedulableTask.WindowsTaskDetails.NextRunTime = schedulableTask.NextScheduledExecution;
                    schedulableTask.WindowsTaskDetails.StatusUpdatedAt = DateTime.Now;

                    _logger.LogInformation("Updated schedule for Windows task: {TaskName}", schedulableTask.WindowsTaskDetails.TaskName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update schedule for task: {TaskName}", schedulableTask.TaskName);
                    throw;
                }
            }
        }

        /// <summary>
        /// Manually runs a schedulable task immediately, bypassing the normal schedule.
        /// Triggers the Windows scheduled task to execute right away regardless of its next scheduled time.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to run immediately.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no Windows scheduled task exists for the specified schedulable task.</exception>
        public async Task RunTaskNowAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));

            if (schedulableTask.WindowsTaskDetails == null)
            {
                throw new InvalidOperationException($"No Windows scheduled task exists for task: {schedulableTask.TaskName}");
            }

            try
            {
                await _taskManager.StartScheduledTaskAsync(schedulableTask);
                _logger.LogInformation("Started immediate execution of Windows task: {TaskName}", schedulableTask.WindowsTaskDetails.TaskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start immediate execution of task: {TaskName}", schedulableTask.TaskName);
                throw;
            }
        }

        /// <summary>
        /// Enables the Windows scheduled task for a schedulable task, allowing it to run according to its schedule.
        /// Changes the task status from disabled to ready in both Task Scheduler and the task's Windows task details.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows task should be enabled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> is null.</exception>
        public async Task EnableScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));

            try
            {
                await _taskManager.EnableScheduledTaskAsync(schedulableTask);
                if (schedulableTask.WindowsTaskDetails != null)
                {
                    schedulableTask.WindowsTaskDetails.UpdateStatus(Fluent.TaskScheduler.Models.TaskStatus.Ready);
                }
                _logger.LogInformation("Enabled Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
                throw;
            }
        }

        /// <summary>
        /// Disables the Windows scheduled task for a schedulable task, preventing it from running automatically.
        /// Changes the task status to disabled in both Task Scheduler and the task's Windows task details.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows task should be disabled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedulableTask"/> is null.</exception>
        public async Task DisableScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask == null)
                throw new ArgumentNullException(nameof(schedulableTask));

            try
            {
                await _taskManager.DisableScheduledTaskAsync(schedulableTask);
                if (schedulableTask.WindowsTaskDetails != null)
                {
                    schedulableTask.WindowsTaskDetails.UpdateStatus(Fluent.TaskScheduler.Models.TaskStatus.Disabled);
                }
                _logger.LogInformation("Disabled Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
                throw;
            }
        }

        /// <summary>
        /// Handles when a schedulable task is activated by creating or enabling its Windows scheduled task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task that was activated.</param>
        /// <param name="previousStatus">The previous status before activation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTaskActivatedAsync(ISchedulableTask schedulableTask, SchedulableTaskStatus previousStatus)
        {
            if (schedulableTask.WindowsTaskDetails == null)
            {
                // Create new Windows scheduled task
                await CreateScheduledTaskAsync(schedulableTask);
            }
            else
            {
                // TaskEnabled existing Windows scheduled task
                await EnableScheduledTaskAsync(schedulableTask);
            }
        }

        /// <summary>
        /// Handles when a schedulable task is deactivated by disabling its Windows scheduled task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task that was deactivated.</param>
        /// <param name="previousStatus">The previous status before deactivation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTaskDeactivatedAsync(ISchedulableTask schedulableTask, SchedulableTaskStatus previousStatus)
        {
            if (schedulableTask.WindowsTaskDetails != null)
            {
                await DisableScheduledTaskAsync(schedulableTask);
            }
        }

        /// <summary>
        /// Handles when a schedulable task is running by updating its Windows task status.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task that is running.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleTaskRunningAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask.WindowsTaskDetails != null)
            {
                await UpdateWindowsTaskStatusAsync(schedulableTask);
            }
        }

        /// <summary>
        /// Creates a Windows scheduled task for the specified schedulable task.
        /// Initializes the task's Windows task details with the created task information.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task for which to create a Windows scheduled task.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Rethrows any exception that occurs during task creation.</exception>
        private async Task CreateScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            try
            {
                await _taskManager.CreateScheduledTaskAsync(schedulableTask);

                // Create Windows task details
                var taskName = _taskManager.GetWindowsTaskName(schedulableTask);
                var taskPath = _taskManager.GetWindowsTaskPath(schedulableTask);
                var description = _taskManager.GetWindowsTaskDescription(schedulableTask);

                schedulableTask.WindowsTaskDetails = new WindowsTaskDetails
                {
                    TaskName = taskName,
                    TaskPath = taskPath,
                    Description = description,
                    CreatedAt = DateTime.Now,
                    Status = Fluent.TaskScheduler.Models.TaskStatus.Ready,
                    StatusUpdatedAt = DateTime.Now,
                    NextRunTime = await _taskManager.GetNextRunTimeAsync(schedulableTask)
                };

                _logger.LogInformation("Created Windows scheduled task for task: {TaskName} (Windows Task: {WindowsTaskName})", 
                    schedulableTask.TaskName, taskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
                throw;
            }
        }

        /// <summary>
        /// Updates the Windows task status for a schedulable task by querying the current status from Task Scheduler.
        /// Also refreshes the next run time from the Windows scheduled task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task whose Windows task status should be updated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateWindowsTaskStatusAsync(ISchedulableTask schedulableTask)
        {
            if (schedulableTask.WindowsTaskDetails == null)
                return;

            try
            {
                var currentStatus = await _taskManager.GetScheduledTaskStatusAsync(schedulableTask);
                schedulableTask.WindowsTaskDetails.UpdateStatus(currentStatus);

                // Update next run time from Task Scheduler
                schedulableTask.WindowsTaskDetails.NextRunTime = await _taskManager.GetNextRunTimeAsync(schedulableTask);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to update Windows task status for task {TaskName}: {Message}", 
                    schedulableTask.TaskName, ex.Message);
            }
        }
    }

    /// <summary>
    /// Interface for task execution history records.
    /// </summary>
    public interface ITaskExecutionHistory
    {
        /// <summary>
        /// Windows Task Scheduler details for this execution.
        /// </summary>
        WindowsTaskDetails? WindowsTaskDetails { get; set; }
    }
} 