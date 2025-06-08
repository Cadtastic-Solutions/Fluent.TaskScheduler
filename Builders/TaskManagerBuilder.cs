using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Builders
{
    /// <summary>
    /// Fluent API for managing existing scheduled tasks.
    /// Provides an intuitive, chainable interface for task management operations.
    /// </summary>
    public class TaskManagerBuilder
    {
        private readonly ITaskSchedulerManager _taskManager;
        private readonly ISchedulableTask _task;

        private TaskManagerBuilder(ITaskSchedulerManager taskManager, ISchedulableTask task)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a new task manager builder for the specified task.
        /// </summary>
        /// <param name="taskManager">The task scheduler manager to use.</param>
        /// <param name="task">The schedulable task to manage.</param>
        /// <returns>A new TaskManagerBuilder instance for method chaining.</returns>
        public static TaskManagerBuilder For(ITaskSchedulerManager taskManager, ISchedulableTask task)
        {
            return new TaskManagerBuilder(taskManager, task);
        }

        /// <summary>
        /// Starts the task immediately.
        /// </summary>
        /// <returns>The TaskManagerBuilder instance for method chaining.</returns>
        public async System.Threading.Tasks.Task<TaskManagerBuilder> StartAsync()
        {
            await _taskManager.StartScheduledTaskAsync(_task);
            return this;
        }

        /// <summary>
        /// Stops the task if it's currently running.
        /// </summary>
        /// <returns>The TaskManagerBuilder instance for method chaining.</returns>
        public async System.Threading.Tasks.Task<TaskManagerBuilder> StopAsync()
        {
            await _taskManager.StopScheduledTaskAsync(_task);
            return this;
        }

        /// <summary>
        /// Enables the task for scheduled execution.
        /// </summary>
        /// <returns>The TaskManagerBuilder instance for method chaining.</returns>
        public async System.Threading.Tasks.Task<TaskManagerBuilder> EnableAsync()
        {
            await _taskManager.EnableScheduledTaskAsync(_task);
            return this;
        }

        /// <summary>
        /// Disables the task to prevent scheduled execution.
        /// </summary>
        /// <returns>The TaskManagerBuilder instance for method chaining.</returns>
        public async System.Threading.Tasks.Task<TaskManagerBuilder> DisableAsync()
        {
            await _taskManager.DisableScheduledTaskAsync(_task);
            return this;
        }

        /// <summary>
        /// Deletes the scheduled task.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async System.Threading.Tasks.Task DeleteAsync()
        {
            await _taskManager.DeleteScheduledTaskAsync(_task);
        }

        /// <summary>
        /// Updates the task's schedule.
        /// </summary>
        /// <returns>The TaskManagerBuilder instance for method chaining.</returns>
        public async System.Threading.Tasks.Task<TaskManagerBuilder> UpdateScheduleAsync()
        {
            await _taskManager.UpdateScheduledTaskScheduleAsync(_task);
            return this;
        }

        /// <summary>
        /// Gets the current status of the task.
        /// </summary>
        /// <returns>The current task status.</returns>
        public async System.Threading.Tasks.Task<Fluent.TaskScheduler.Models.TaskStatus> GetStatusAsync()
        {
            return await _taskManager.GetScheduledTaskStatusAsync(_task);
        }

        /// <summary>
        /// Gets the next scheduled run time for the task.
        /// </summary>
        /// <returns>The next scheduled run time, or null if not available.</returns>
        public async System.Threading.Tasks.Task<DateTime?> GetNextRunTimeAsync()
        {
            return await _taskManager.GetNextRunTimeAsync(_task);
        }

        /// <summary>
        /// Gets the execution history for the task.
        /// </summary>
        /// <param name="maxEntries">Maximum number of history entries to retrieve.</param>
        /// <returns>A collection of task execution history entries.</returns>
        public async System.Threading.Tasks.Task<IEnumerable<TaskExecutionHistory>> GetExecutionHistoryAsync(int maxEntries = 50)
        {
            return await _taskManager.GetTaskExecutionHistoryAsync(_task, maxEntries);
        }

        /// <summary>
        /// Checks if the scheduled task exists.
        /// </summary>
        /// <returns>True if the task exists, false otherwise.</returns>
        public async System.Threading.Tasks.Task<bool> ExistsAsync()
        {
            return await _taskManager.ScheduledTaskExistsAsync(_task);
        }

        /// <summary>
        /// Gets the underlying schedulable task.
        /// </summary>
        /// <returns>The schedulable task being managed.</returns>
        public ISchedulableTask GetTask()
        {
            return _task;
        }
    }
} 