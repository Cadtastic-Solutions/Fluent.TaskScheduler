using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.TaskScheduler;
using TaskStatus = Fluent.TaskScheduler.Models.TaskStatus;
using WinTask = Microsoft.Win32.TaskScheduler.Task;
using SystemTask = System.Threading.Tasks.Task;
using System.IO;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Exceptions;


namespace Fluent.TaskScheduler.Services
{
    /// <summary>
    /// Manages Windows Task Scheduler tasks for schedulable tasks.
    /// Provides comprehensive task scheduling operations including creation, deletion, status management, and execution control.
    /// </summary>
    public class TaskSchedulerManager : ITaskSchedulerManager
    {
        private readonly ILogger<TaskSchedulerManager> _logger;
        private readonly TaskSchedulerOptions _options;
        private readonly IRetryPolicyService _retryPolicyService;
        private readonly ITaskServicePool _taskServicePool;

        public TaskSchedulerManager(
            ILogger<TaskSchedulerManager> logger,
            IOptions<TaskSchedulerOptions> options,
            IRetryPolicyService retryPolicyService,
            ITaskServicePool taskServicePool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _retryPolicyService = retryPolicyService ?? throw new ArgumentNullException(nameof(retryPolicyService));
            _taskServicePool = taskServicePool ?? throw new ArgumentNullException(nameof(taskServicePool));
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task CreateScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                await _taskServicePool.ExecuteAsync(async taskService =>
                {
                    await CreateScheduledTaskInternalAsync(schedulableTask, taskService).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }, "CreateScheduledTask", schedulableTask.TaskId, schedulableTask.TaskName).ConfigureAwait(false);
        }

        private async SystemTask CreateScheduledTaskInternalAsync(ISchedulableTask schedulableTask, TaskService taskService)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                
                // Create task folder if it doesn't exist
                var folder = GetOrCreateTaskFolder(taskService);
                
                if (folder == null)
                    throw new TaskSchedulerServiceException("Unable to create or access Task Scheduler folder. Please ensure the Task Scheduler service is running and you have administrator privileges.", schedulableTask.TaskId, schedulableTask.TaskName);
                
                // Check if task already exists
                if (await ScheduledTaskExistsAsync(schedulableTask))
                {
                    _logger.LogWarning("Windows Task already exists: {TaskName}", taskName);
                    return;
                }

                // Create task definition
                var taskDefinition = taskService.NewTask();
                taskDefinition.RegistrationInfo.Description = GetWindowsTaskDescription(schedulableTask);
                taskDefinition.RegistrationInfo.Author = "Easy Windows Task Scheduling";
                
                // Set principal based on UserAccount configuration
                ConfigureTaskPrincipal(taskDefinition.Principal, schedulableTask);
                
                // Configure settings
                taskDefinition.Settings.Enabled = true;
                taskDefinition.Settings.AllowDemandStart = true;
                taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                taskDefinition.Settings.StopIfGoingOnBatteries = false;
                taskDefinition.Settings.StartWhenAvailable = true;
                taskDefinition.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(24); // Max 24 hours
                taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero; // Don't auto-delete
                taskDefinition.Settings.RestartCount = 3; // Retry failed tasks up to 3 times
                taskDefinition.Settings.RestartInterval = TimeSpan.FromMinutes(1); // Wait 1 minute between retries
                
                // TaskEnabled execution history (Task Scheduler maintains logs)
                // This is enabled by default, but we're being explicit
                taskDefinition.Settings.UseUnifiedSchedulingEngine = true;
                
                // Create trigger based on task schedule
                CreateTriggerForSchedulableTask(taskDefinition, schedulableTask);
                
                // Create action to run the task executor
                var workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var fullExecutorPath = ((ITaskSchedulerManager)this).GetTaskExecutorPath();
                
                // Validate that the task executor exists
                if (!File.Exists(fullExecutorPath))
                {
                    _logger.LogError("Task executor executable not found at expected path: {ExecutorPath}", fullExecutorPath);
                    _logger.LogError("Working directory: {WorkingDirectory}", workingDirectory);
                    _logger.LogError("Current executable location: {ExecutableLocation}", System.Reflection.Assembly.GetExecutingAssembly().Location);
                    throw new FileNotFoundException($"Task executor executable not found at: {fullExecutorPath}. Ensure the task executor has been built and deployed.");
                }
                else
                {
                    _logger.LogInformation("Task executor found at: {ExecutorPath}", fullExecutorPath);
                }
                
                // Validate working directory exists and is accessible
                if (!Directory.Exists(workingDirectory))
                {
                    _logger.LogWarning("Working directory does not exist: {WorkingDirectory}, using task executor directory instead", workingDirectory);
                    workingDirectory = Path.GetDirectoryName(fullExecutorPath) ?? workingDirectory;
                }
                
                // Use full absolute path to executable with proper working directory
                // This ensures the Task Scheduler can find and execute the task even when
                // running in different contexts (e.g., when user is logged off)
                var action = new ExecAction(fullExecutorPath, $"--task-id {schedulableTask.TaskId}", workingDirectory);
                taskDefinition.Actions.Add(action);
                
                _logger.LogInformation("Task action configured:");
                _logger.LogInformation("  Executable: {Executable}", fullExecutorPath);
                _logger.LogInformation("  Arguments: --task-id {TaskId}", schedulableTask.TaskId);
                _logger.LogInformation("  WorkingDirectory: {WorkingDirectory}", workingDirectory);
                _logger.LogInformation("  Executable exists: {ExecutableExists}", File.Exists(fullExecutorPath));
                _logger.LogInformation("  Working dir exists: {WorkingDirExists}", Directory.Exists(workingDirectory));
                
                // Register the task
                folder.RegisterTaskDefinition(taskName, taskDefinition);
                
                _logger.LogInformation("Created Windows scheduled task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when creating Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
                throw new TaskSchedulerPermissionException(
                    $"Access denied when creating Windows scheduled task '{taskName}'. " +
                    "The application needs administrator privileges to create scheduled tasks. " +
                    "Please restart the application as Administrator or configure it to run with elevated permissions.", 
                    ex, schedulableTask.TaskId, schedulableTask.TaskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Windows scheduled task for task: {TaskName}", schedulableTask.TaskName);
                throw new Fluent.TaskScheduler.Exceptions.TaskSchedulerException($"Failed to create Windows scheduled task '{taskName}': {ex.Message}", ex, schedulableTask.TaskId, schedulableTask.TaskName);
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task DeleteScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                await _taskServicePool.ExecuteAsync(async taskService =>
                {
                    await DeleteScheduledTaskInternalAsync(schedulableTask, taskService).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }, "DeleteScheduledTask", schedulableTask.TaskId, schedulableTask.TaskName).ConfigureAwait(false);
        }

        private async SystemTask DeleteScheduledTaskInternalAsync(ISchedulableTask schedulableTask, TaskService taskService)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                
                if (!await ScheduledTaskExistsAsync(schedulableTask))
                {
                    _logger.LogWarning("Windows Task does not exist: {TaskName}", taskName);
                    return;
                }
                
                // Stop task if running
                await StopScheduledTaskAsync(schedulableTask);
                
                // Get the folder
                var folder = GetOrCreateTaskFolder(taskService);
                
                // Delete the task
                folder.DeleteTask(taskName);
                
                _logger.LogInformation("Deleted Windows scheduled task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Windows scheduled task: {TaskName}", taskName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<TaskStatus> GetScheduledTaskStatusAsync(ISchedulableTask schedulableTask)
        {
            return await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                return await _taskServicePool.ExecuteAsync(async taskService =>
                {
                    return await GetScheduledTaskStatusInternalAsync(schedulableTask, taskService).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }, "GetScheduledTaskStatus", schedulableTask.TaskId, schedulableTask.TaskName).ConfigureAwait(false);
        }

        private async Task<TaskStatus> GetScheduledTaskStatusInternalAsync(ISchedulableTask schedulableTask, TaskService taskService)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                
                // Use try-catch to handle folder not existing
                try
                {
                    var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                    if (folder == null)
                        return TaskStatus.Unknown;
                        
                    WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                    
                    if (task == null)
                        return TaskStatus.Unknown;
                    
                    await SystemTask.CompletedTask;
                    
                    return task.State switch
                    {
                        TaskState.Ready => TaskStatus.Ready,
                        TaskState.Running => TaskStatus.Running,
                        TaskState.Disabled => TaskStatus.Disabled,
                        TaskState.Queued => TaskStatus.Queued,
                        _ => TaskStatus.Unknown
                    };
                }
                catch (Exception)
                {
                    // Folder doesn't exist
                    return TaskStatus.Unknown;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not get status for Windows task {TaskName}: {Message}", taskName, ex.Message);
                return TaskStatus.Unknown;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task StartScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                var folder = GetOrCreateTaskFolder(taskService);
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                
                if (task == null)
                    throw new InvalidOperationException($"Windows Task not found: {taskName}");
                
                task.Run();
                _logger.LogInformation("Started Windows scheduled task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Windows scheduled task: {TaskName}", taskName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task StopScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                
                // Use try-catch to handle folder not existing
                try
                {
                    var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                    if (folder == null)
                        return;
                        
                    WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                    
                    if (task == null)
                        return;
                    
                    if (task.State == TaskState.Running)
                    {
                        task.Stop();
                        _logger.LogInformation("Stopped Windows scheduled task: {TaskName}", taskName);
                    }
                }
                catch (Exception)
                {
                    // Folder doesn't exist, nothing to stop
                    return;
                }
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop Windows scheduled task: {TaskName}", taskName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task EnableScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                var folder = GetOrCreateTaskFolder(taskService);
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                
                if (task == null)
                    throw new InvalidOperationException($"Windows Task not found: {taskName}");
                
                task.Enabled = true;
                _logger.LogInformation("Enabled Windows scheduled task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable Windows scheduled task: {TaskName}", taskName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task DisableScheduledTaskAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                var folder = GetOrCreateTaskFolder(taskService);
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                
                if (task == null)
                    throw new InvalidOperationException($"Windows Task not found: {taskName}");
                
                task.Enabled = false;
                _logger.LogInformation("Disabled Windows scheduled task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable Windows scheduled task: {TaskName}", taskName);
                throw;
            }
        }

        /// <inheritdoc/>
        public string GetWindowsTaskName(ISchedulableTask schedulableTask)
        {
            // Create a safe task name from task name and ID
            var safeName = MakeTaskNameSafe(schedulableTask.TaskName);
            return $"{safeName}_{schedulableTask.TaskId[..8]}";
        }

        /// <inheritdoc/>
        public string GetWindowsTaskPath(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            return $"\\{_options.TaskFolder}\\{taskName}";
        }

        /// <inheritdoc/>
        public async Task<DateTime?> GetNextRunTimeAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                
                if (folder == null)
                {
                    await SystemTask.CompletedTask;
                    return null;
                }
                
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                
                if (task == null)
                {
                    await SystemTask.CompletedTask;
                    return null;
                }
                
                // Get the next run time from the task
                var nextRunTime = task.NextRunTime;
                
                await SystemTask.CompletedTask;
                return nextRunTime == DateTime.MinValue ? null : nextRunTime;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error getting next run time for Windows task {TaskName}: {Message}", taskName, ex.Message);
                await SystemTask.CompletedTask;
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ScheduledTaskExistsAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                
                // Check if folder exists first
                try
                {
                    var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                    if (folder == null)
                    {
                        await SystemTask.CompletedTask;
                        return false;
                    }
                    
                    WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                    await SystemTask.CompletedTask;
                    return task != null;
                }
                catch (Exception)
                {
                    // Folder doesn't exist, so task doesn't exist
                    await SystemTask.CompletedTask;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error checking if Windows task exists {TaskName}: {Message}", taskName, ex.Message);
                await SystemTask.CompletedTask;
                return false;
            }
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task UpdateScheduledTaskScheduleAsync(ISchedulableTask schedulableTask)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            
            try
            {
                using var taskService = new TaskService();
                var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                
                if (folder == null)
                    throw new TaskSchedulerServiceException($"Task folder not found: {_options.TaskFolder}");
                
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                
                if (task == null)
                    throw new InvalidOperationException($"Windows Task not found: {taskName}");
                
                // Get existing task definition
                var taskDefinition = task.Definition;
                
                // Clear existing triggers
                taskDefinition.Triggers.Clear();
                
                // Create new trigger based on current task schedule
                CreateTriggerForSchedulableTask(taskDefinition, schedulableTask);
                
                // Re-register the task with updated schedule
                folder.RegisterTaskDefinition(taskName, taskDefinition);
                
                _logger.LogInformation("Updated schedule for Windows task: {TaskName}", taskName);
                
                await SystemTask.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update schedule for Windows task: {TaskName}", taskName);
                throw;
            }
        }

        private TaskFolder GetOrCreateTaskFolder(TaskService taskService)
        {
            if (taskService == null)
                throw new InvalidOperationException("TaskService is null");

            try
            {
                var folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                if (folder != null)
                    return folder;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Folder {TaskFolder} does not exist, attempting to create: {Message}", _options.TaskFolder, ex.Message);
            }

            // Folder doesn't exist or GetFolder returned null, create it
            try
            {
                if (taskService.RootFolder == null)
                    throw new InvalidOperationException("TaskService.RootFolder is null - Task Scheduler may not be available");

                // Create nested folders if they don't exist
                var folderParts = _options.TaskFolder.Split('\\');
                var currentFolder = taskService.RootFolder;
                var currentPath = "";

                for (int i = 0; i < folderParts.Length; i++)
                {
                    var folderName = folderParts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? folderName : $"{currentPath}\\{folderName}";
                    
                    try
                    {
                        // Try to get existing folder
                        var existingFolder = taskService.GetFolder($"\\{currentPath}");
                        if (existingFolder != null)
                        {
                            currentFolder = existingFolder;
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        // Folder doesn't exist, we'll create it below
                    }

                    // Create the folder
                    _logger.LogDebug("Creating folder: {CurrentPath}", currentPath);
                    currentFolder = currentFolder.CreateFolder(folderName);
                    
                    if (currentFolder == null)
                        throw new InvalidOperationException($"Failed to create task folder: {currentPath}");
                }

                _logger.LogInformation("Created/accessed task folder hierarchy: {TaskFolder}", _options.TaskFolder);
                return currentFolder;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when creating task folder hierarchy: {TaskFolder}", _options.TaskFolder);
                throw new TaskSchedulerPermissionException(
                    $"Access denied when creating Task Scheduler folder '{_options.TaskFolder}'. " +
                    "This usually happens when the application doesn't have administrator privileges. " +
                    "Please try one of the following solutions:\n" +
                    "1. Run the application as Administrator\n" +
                    "2. Use a different user account with Task Scheduler permissions\n" +
                    "3. Contact your system administrator to grant Task Scheduler permissions", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task folder hierarchy: {TaskFolder}", _options.TaskFolder);
                throw new TaskSchedulerServiceException(
                    $"Unable to create or access Task Scheduler folder '{_options.TaskFolder}'. " +
                    "Common causes:\n" +
                    "• Task Scheduler service is not running\n" +
                    "• Insufficient permissions (try running as Administrator)\n" +
                    "• System policy restrictions\n" +
                    "• Corrupted Task Scheduler database", ex);
            }
        }

        private void CreateTriggerForSchedulableTask(TaskDefinition taskDefinition, ISchedulableTask schedulableTask)
        {
            // Get the schedule from the schedulable task
            var schedule = schedulableTask.Schedule;
            if (schedule == null)
            {
                // Fallback to a default schedule if no schedule property found
                _logger.LogWarning("Task {TaskName} does not have schedule information, using default one-time execution", schedulableTask.TaskName);
                var fallbackTrigger = new TimeTrigger(DateTime.Now.AddMinutes(1));
                taskDefinition.Triggers.Add(fallbackTrigger);
                return;
            }

            if (schedule.IsOneTime)
            {
                // One-time trigger - ensure we use local time properly
                var localDateTime = schedule.InitialDateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(schedule.InitialDateTime, DateTimeKind.Local)
                    : schedule.InitialDateTime.ToLocalTime();
                    
                var trigger = new TimeTrigger(localDateTime);
                taskDefinition.Triggers.Add(trigger);
                
                _logger.LogDebug("Created one-time trigger for {DateTime} (Kind: {Kind})", localDateTime.ToString("yyyy-MM-dd HH:mm:ss"), localDateTime.Kind);
            }
            else
            {
                // Recurring trigger based on interval type
                switch (schedule.IntervalType)
                {
                    case ScheduleIntervalType.Minutes:
                        // For minute intervals, use daily trigger with repetition
                        var localDateTime = schedule.InitialDateTime.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(schedule.InitialDateTime, DateTimeKind.Local)
                            : schedule.InitialDateTime.ToLocalTime();
                            
                        var minuteTrigger = new DailyTrigger
                        {
                            StartBoundary = localDateTime,
                            Repetition = new RepetitionPattern(TimeSpan.FromMinutes(schedule.IntervalValue), TimeSpan.Zero) // No duration limit
                        };
                        
                        // Set end date if specified
                        if (schedule.HasEndDate)
                        {
                            minuteTrigger.EndBoundary = schedule.EndDate;
                        }
                        
                        taskDefinition.Triggers.Add(minuteTrigger);
                        
                        _logger.LogDebug("Created minute interval trigger starting {DateTime} every {IntervalValue} minutes", localDateTime.ToString("yyyy-MM-dd HH:mm:ss"), schedule.IntervalValue);
                        break;
                        
                    case ScheduleIntervalType.Hours:
                        // For hour intervals, use daily trigger with repetition
                        var localDateTimeHours = schedule.InitialDateTime.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(schedule.InitialDateTime, DateTimeKind.Local)
                            : schedule.InitialDateTime.ToLocalTime();
                            
                        var hourTrigger = new DailyTrigger
                        {
                            StartBoundary = localDateTimeHours,
                            Repetition = new RepetitionPattern(TimeSpan.FromHours(schedule.IntervalValue), TimeSpan.Zero) // No duration limit
                        };
                        
                        // Set end date if specified
                        if (schedule.HasEndDate)
                        {
                            hourTrigger.EndBoundary = schedule.EndDate;
                        }
                        
                        taskDefinition.Triggers.Add(hourTrigger);
                        
                        _logger.LogDebug("Created hour interval trigger starting {DateTime} every {IntervalValue} hours", localDateTimeHours.ToString("yyyy-MM-dd HH:mm:ss"), schedule.IntervalValue);
                        break;
                        
                    case ScheduleIntervalType.Days:
                        var localDateTimeDays = schedule.InitialDateTime.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(schedule.InitialDateTime, DateTimeKind.Local)
                            : schedule.InitialDateTime.ToLocalTime();
                            
                        var dailyTrigger = new DailyTrigger((short)schedule.IntervalValue)
                        {
                            StartBoundary = localDateTimeDays
                        };
                        
                        // Set end date if specified
                        if (schedule.HasEndDate)
                        {
                            dailyTrigger.EndBoundary = schedule.EndDate;
                        }
                        
                        taskDefinition.Triggers.Add(dailyTrigger);
                        
                        _logger.LogDebug("Created daily trigger starting {DateTime} every {IntervalValue} days", localDateTimeDays.ToString("yyyy-MM-dd HH:mm:ss"), schedule.IntervalValue);
                        break;
                        
                    case ScheduleIntervalType.Weeks:
                        var localDateTimeWeeks = schedule.InitialDateTime.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(schedule.InitialDateTime, DateTimeKind.Local)
                            : schedule.InitialDateTime.ToLocalTime();
                            
                        var weeklyTrigger = new WeeklyTrigger()
                        {
                            StartBoundary = localDateTimeWeeks,
                            WeeksInterval = (short)schedule.IntervalValue,
                            DaysOfWeek = schedule.SelectedDays // Use selected days
                        };
                        
                        // Set end date if specified
                        if (schedule.HasEndDate)
                        {
                            weeklyTrigger.EndBoundary = schedule.EndDate;
                        }
                        
                        taskDefinition.Triggers.Add(weeklyTrigger);
                        
                        _logger.LogDebug("Created weekly trigger starting {DateTime} every {IntervalValue} weeks", localDateTimeWeeks.ToString("yyyy-MM-dd HH:mm:ss"), schedule.IntervalValue);
                        break;
                }
            }
        }



        private string GetTaskExecutorPath()
        {
            // Use a generic task executor name
            return "TaskExecutor.exe";
        }

        /// <summary>
        /// Validates that the task executor executable is available for task execution.
        /// </summary>
        /// <returns>True if task executor is found, false otherwise.</returns>
        public bool IsTaskExecutorAvailable()
        {
            var taskExecutorPath = GetTaskExecutorPath();
            return File.Exists(taskExecutorPath);
        }

        /// <summary>
        /// Gets the full path to the task executor executable.
        /// </summary>
        /// <returns>The full path to TaskExecutor.exe</returns>
        string ITaskSchedulerManager.GetTaskExecutorPath()
        {
            // First try the same directory as the current executable
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var taskExecutorPath = Path.Combine(currentDirectory, GetTaskExecutorPath());
            
            if (File.Exists(taskExecutorPath))
            {
                _logger.LogDebug("Found task executor at: {TaskExecutorPath}", taskExecutorPath);
                return taskExecutorPath;
            }
            
            // Try the TaskExecutor subdirectory (development scenario)
            var taskExecutorDirectory = Path.Combine(currentDirectory, "..", "TaskExecutor", "bin", "Debug", "net8.0-windows");
            taskExecutorPath = Path.Combine(taskExecutorDirectory, GetTaskExecutorPath());
            
            if (File.Exists(taskExecutorPath))
            {
                var fullPath = Path.GetFullPath(taskExecutorPath);
                _logger.LogDebug("Found task executor in development directory: {TaskExecutorPath}", fullPath);
                return fullPath;
            }
            
            // Try looking for it relative to the solution directory
            var solutionDir = FindSolutionDirectory(currentDirectory);
            if (!string.IsNullOrEmpty(solutionDir))
            {
                taskExecutorPath = Path.Combine(solutionDir, "TaskExecutor", "bin", "Debug", "net8.0-windows", GetTaskExecutorPath());
                if (File.Exists(taskExecutorPath))
                {
                    var fullPath = Path.GetFullPath(taskExecutorPath);
                    _logger.LogDebug("Found task executor relative to solution: {TaskExecutorPath}", fullPath);
                    return fullPath;
                }
            }
            
            // Fallback to original path (will likely fail, but preserves existing behavior)
            _logger.LogWarning("Could not locate task executor executable. Tried paths: {CurrentDirectory}, {TaskExecutorDirectory}, {SolutionDirectory}", 
                currentDirectory, taskExecutorDirectory, string.IsNullOrEmpty(solutionDir) ? "N/A" : Path.Combine(solutionDir, "TaskExecutor", "bin", "Debug", "net8.0-windows"));
            return Path.Combine(currentDirectory, GetTaskExecutorPath());
        }
        
        /// <summary>
        /// Searches for the solution directory starting from the given path.
        /// </summary>
        private string? FindSolutionDirectory(string startPath)
        {
            var directory = new DirectoryInfo(startPath);
            
            while (directory != null)
            {
                // Look for .sln file
                if (directory.GetFiles("*.sln").Length > 0)
                {
                    return directory.FullName;
                }
                
                directory = directory.Parent;
            }
            
            return null;
        }

        private string MakeTaskNameSafe(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                return "UnnamedJob";

            // Replace invalid characters for task names (Task Scheduler doesn't allow: \ / : * ? " < > |)
            var safeName = System.Text.RegularExpressions.Regex.Replace(jobName, @"[\\/:*?""<>|]", "_");
            
            // Replace multiple spaces/underscores with single underscore
            safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"[\s_]+", "_");
            
            // Remove leading/trailing underscores
            safeName = safeName.Trim('_');
            
            // Task Scheduler has a practical limit of ~240 characters, but we'll be more conservative
            if (safeName.Length > 100)
                safeName = safeName.Substring(0, 100).TrimEnd('_');
            
            return string.IsNullOrEmpty(safeName) ? $"Job_{DateTime.Now:yyyyMMddHHmmss}" : safeName;
        }

        /// <summary>
        /// Gets a description for the Windows Task based on the schedulable task.
        /// </summary>
        /// <param name="schedulableTask">The schedulable task to get description for.</param>
        /// <returns>A description for the Windows Task.</returns>
        public string GetWindowsTaskDescription(ISchedulableTask schedulableTask)
        {
            if (!string.IsNullOrWhiteSpace(schedulableTask.Description))
                return schedulableTask.Description;
            
            return $"Automated task: {schedulableTask.TaskName} (Type: {schedulableTask.TaskType})";
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TaskExecutionHistory>> GetTaskExecutionHistoryAsync(ISchedulableTask schedulableTask, int maxEntries = 50)
        {
            var taskName = GetWindowsTaskName(schedulableTask);
            var history = new List<TaskExecutionHistory>();
            
            try
            {
                using var taskService = new TaskService();
                
                // Get the task folder
                TaskFolder? folder;
                try
                {
                    folder = taskService.GetFolder($"\\{_options.TaskFolder}");
                    if (folder == null)
                        return history;
                }
                catch (Exception)
                {
                    // Folder doesn't exist
                    return history;
                }
                
                // Get the task
                WinTask? task = folder.GetTasks().FirstOrDefault(t => t.Name == taskName);
                if (task == null)
                    return history;
                
                // Get running instances
                var runningTasks = task.GetInstances();
                foreach (var runningTask in runningTasks.Take(maxEntries))
                {
                    history.Add(new TaskExecutionHistory
                    {
                        StartTime = DateTime.Now, // RunningTask doesn't expose StartTime, use current time as estimate
                        EndTime = null, // Still running
                        ResultCode = 0, // Unknown for running tasks
                        State = "Running",
                        UserAccount = runningTask.EnginePID > 0 ? Environment.UserName : null,
                        Details = "Task is currently running",
                        ResultDescription = "In Progress"
                    });
                }
                
                // Get execution history from Windows Event Log for more detailed information
                try
                {
                    await GetTaskSchedulerEventHistoryAsync(taskName, history, maxEntries - history.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not retrieve detailed execution history from Event Log for task {TaskName}: {Message}", taskName, ex.Message);
                    
                    // Fallback to basic GetRunTimes method
                    try
                    {
                        var events = task.GetRunTimes(DateTime.Now.AddDays(-30), DateTime.Now); // Last 30 days
                        var completedRuns = events.Take(maxEntries - history.Count);
                        
                        foreach (var eventTime in completedRuns)
                        {
                            history.Add(new TaskExecutionHistory
                            {
                                StartTime = eventTime,
                                EndTime = eventTime.AddMinutes(1), // Estimate
                                ResultCode = 0, // Unknown
                                State = "Completed",
                                UserAccount = Environment.UserName,
                                Details = "Historical execution (limited details available)",
                                ResultDescription = "Unknown result"
                            });
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogDebug("Fallback GetRunTimes also failed for task {TaskName}: {Message}", taskName, fallbackEx.Message);
                    }
                }
                
                await System.Threading.Tasks.Task.CompletedTask;
                return history.OrderByDescending(h => h.StartTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get execution history for task: {TaskName}", taskName);
                return history;
            }
        }
        
        /// <summary>
        /// Gets Task Scheduler execution history from Windows Event Log.
        /// </summary>
        private async SystemTask GetTaskSchedulerEventHistoryAsync(string taskName, List<TaskExecutionHistory> history, int maxEntries)
        {
            try
            {
                using var eventLog = new System.Diagnostics.EventLog("Microsoft-Windows-TaskScheduler/Operational");
                
                var entries = eventLog.Entries.Cast<System.Diagnostics.EventLogEntry>()
                    .Where(entry => entry.Source == "Microsoft-Windows-TaskScheduler" && 
                                   entry.Message.Contains(taskName))
                    .OrderByDescending(entry => entry.TimeGenerated)
                    .Take(maxEntries * 3) // Get more to account for multiple events per execution
                    .ToList();
                
                var executionGroups = new Dictionary<DateTime, TaskExecutionHistory>();
                
                foreach (var entry in entries)
                {
                    var executionTime = entry.TimeGenerated;
                    var roundedTime = new DateTime(executionTime.Year, executionTime.Month, executionTime.Day, 
                                                  executionTime.Hour, executionTime.Minute, 0); // Group by minute
                    
                    if (!executionGroups.ContainsKey(roundedTime))
                    {
                        executionGroups[roundedTime] = new TaskExecutionHistory
                        {
                            StartTime = executionTime,
                            UserAccount = Environment.UserName,
                            State = "Unknown"
                        };
                    }
                    
                    var execution = executionGroups[roundedTime];
                    
                    // Parse event based on Event ID
                    switch (entry.InstanceId)
                    {
                        case 100: // Task started
                            execution.State = "Started";
                            execution.StartTime = executionTime;
                            execution.Details = "Task execution started";
                            break;
                            
                        case 102: // Task completed successfully  
                            execution.State = "Completed";
                            execution.EndTime = executionTime;
                            execution.ResultCode = 0;
                            execution.ResultDescription = "Success";
                            execution.Details = "Task completed successfully";
                            break;
                            
                        case 101: // Task start failed
                            execution.State = "Failed";
                            execution.EndTime = executionTime;
                            execution.ResultCode = 1;
                            execution.ResultDescription = "Launch Failure";
                            execution.Details = "Task failed to start - " + entry.Message;
                            break;
                            
                        case 103: // Task failed
                            execution.State = "Failed";
                            execution.EndTime = executionTime;
                            execution.ResultCode = 1;
                            execution.ResultDescription = "Execution Failed";
                            execution.Details = "Task execution failed - " + entry.Message;
                            break;
                            
                        case 110: // Task triggered
                            execution.Details = $"Task triggered - {entry.Message}";
                            break;
                    }
                }
                
                // Add the execution history, taking only the requested number
                history.AddRange(executionGroups.Values
                    .OrderByDescending(h => h.StartTime)
                    .Take(maxEntries));
                
                await System.Threading.Tasks.Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to read Task Scheduler event log: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Configures the task principal based on the schedulable task's UserAccount settings.
        /// </summary>
        private void ConfigureTaskPrincipal(TaskPrincipal principal, ISchedulableTask schedulableTask)
        {
            var userAccount = schedulableTask.UserAccount ?? new TaskUserAccount(); // Default fallback

            _logger.LogDebug("Configuring task principal for task {TaskName}: AccountType={AccountType}, RunWhenLoggedOff={RunWhenLoggedOff}, HighestPrivileges={HighestPrivileges}",
                schedulableTask.TaskName, userAccount.RunAsType, userAccount.RunWhenLoggedOff, userAccount.RunWithHighestPrivileges);

            // Configure based on account type
            switch (userAccount.RunAsType)
            {
                case TaskRunAsType.CurrentUser:
                    if (userAccount.RunWhenLoggedOff)
                    {
                        // S4U allows running when user is logged off, without storing password
                        principal.LogonType = TaskLogonType.S4U;
                        _logger.LogDebug("Using S4U logon type for current user (run when logged off)");
                    }
                    else
                    {
                        // Interactive only runs when user is logged on
                        principal.LogonType = TaskLogonType.InteractiveToken;
                        _logger.LogDebug("Using InteractiveToken logon type for current user (logged on only)");
                    }
                    break;

                case TaskRunAsType.LocalSystem:
                    principal.LogonType = TaskLogonType.ServiceAccount;
                    principal.UserId = "SYSTEM";
                    principal.RunLevel = TaskRunLevel.Highest; // SYSTEM always has highest privileges
                    _logger.LogDebug("Using SYSTEM account");
                    break;

                case TaskRunAsType.LocalService:
                    principal.LogonType = TaskLogonType.ServiceAccount;
                    principal.UserId = "NT AUTHORITY\\LocalService";
                    principal.RunLevel = TaskRunLevel.LUA; // Local Service has limited privileges
                    _logger.LogDebug("Using LocalService account");
                    break;

                case TaskRunAsType.NetworkService:
                    principal.LogonType = TaskLogonType.ServiceAccount;
                    principal.UserId = "NT AUTHORITY\\NetworkService";
                    principal.RunLevel = TaskRunLevel.LUA; // Network Service has limited privileges
                    _logger.LogDebug("Using NetworkService account");
                    break;

                case TaskRunAsType.SpecificUser:
                    if (!string.IsNullOrWhiteSpace(userAccount.Username))
                    {
                        principal.LogonType = userAccount.RunWhenLoggedOff ? TaskLogonType.Password : TaskLogonType.InteractiveToken;
                        
                        // Format username with domain if provided
                        if (!string.IsNullOrWhiteSpace(userAccount.Domain))
                        {
                            principal.UserId = $"{userAccount.Domain}\\{userAccount.Username}";
                        }
                        else
                        {
                            principal.UserId = userAccount.Username;
                        }
                        
                        _logger.LogDebug("Using specific user account: {UserId}", principal.UserId);
                        
                        // Note: For Password logon type, the password would need to be set when registering the task
                        // This is not implemented in this method as it requires secure credential handling
                        if (userAccount.RunWhenLoggedOff && principal.LogonType == TaskLogonType.Password)
                        {
                            _logger.LogWarning("Password logon type requires credential handling during task registration - not implemented");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Specific user account selected but no username provided, falling back to current user");
                        goto case TaskRunAsType.CurrentUser; // Fallback to current user
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown RunAsType: {RunAsType}, falling back to current user", userAccount.RunAsType);
                    goto case TaskRunAsType.CurrentUser; // Fallback to current user
            }

            // Set privilege level (only applies to user accounts, not service accounts)
            if (userAccount.RunAsType != TaskRunAsType.LocalSystem && userAccount.RunAsType != TaskRunAsType.LocalService && userAccount.RunAsType != TaskRunAsType.NetworkService)
            {
                principal.RunLevel = userAccount.RunWithHighestPrivileges ? TaskRunLevel.Highest : TaskRunLevel.LUA;
                _logger.LogDebug("Set privilege level: {RunLevel}", principal.RunLevel);
            }
        }
    }
} 