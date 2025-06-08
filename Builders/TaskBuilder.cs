using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Examples;
using Microsoft.Win32.TaskScheduler;

namespace Fluent.TaskScheduler.Builders
{
    /// <summary>
    /// Fluent builder implementation for creating and configuring scheduled tasks.
    /// Provides a clean, readable API for task configuration.
    /// </summary>
    public class TaskBuilder : ITaskBuilder
    {
        private readonly IFluentTaskScheduler _scheduler;
        private readonly SimpleSchedulableTask _task;

        public TaskBuilder(IFluentTaskScheduler scheduler, string taskName)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            
            if (string.IsNullOrWhiteSpace(taskName))
                throw new ArgumentException("Task name cannot be null or empty.", nameof(taskName));

            _task = new SimpleSchedulableTask
            {
                TaskId = Guid.NewGuid().ToString(),
                TaskName = taskName,
                TaskType = "General",
                Status = SchedulableTaskStatus.Active,
                Created = DateTime.Now,
                Modified = DateTime.Now
            };
        }

        /// <inheritdoc />
        public ITaskBuilder WithDescription(string description)
        {
            _task.Description = description;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder Category(string category)
        {
            _task.TaskType = category ?? "General";
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecuteProgram(string programPath, string? arguments = null, string? workingDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(programPath))
                throw new ArgumentException("Program path cannot be null or empty.", nameof(programPath));

            _task.ExecutablePath = programPath;
            _task.Arguments = arguments;
            _task.WorkingDirectory = workingDirectory;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecutePowerShellScript(string scriptPath, string? arguments = null)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                throw new ArgumentException("Script path cannot be null or empty.", nameof(scriptPath));

            _task.ExecutablePath = "powershell.exe";
            _task.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"" + 
                             (string.IsNullOrWhiteSpace(arguments) ? "" : $" {arguments}");
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecutePowerShellCommands(string commands)
        {
            if (string.IsNullOrWhiteSpace(commands))
                throw new ArgumentException("Commands cannot be null or empty.", nameof(commands));

            _task.ExecutablePath = "powershell.exe";
            _task.Arguments = $"-ExecutionPolicy Bypass -Command \"{commands.Replace("\"", "\\\"")}\"";
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecuteBatchFile(string batchFilePath, string? arguments = null)
        {
            if (string.IsNullOrWhiteSpace(batchFilePath))
                throw new ArgumentException("Batch file path cannot be null or empty.", nameof(batchFilePath));

            _task.ExecutablePath = "cmd.exe";
            _task.Arguments = $"/c \"{batchFilePath}\"" + 
                             (string.IsNullOrWhiteSpace(arguments) ? "" : $" {arguments}");
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecuteCommand(string command, string? arguments = null)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty.", nameof(command));

            _task.ExecutablePath = command;
            _task.Arguments = arguments;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder ExecuteConsoleApp(string consolePath, string[]? args = null)
        {
            if (string.IsNullOrWhiteSpace(consolePath))
                throw new ArgumentException("Console path cannot be null or empty.", nameof(consolePath));

            // Check if the file exists
            if (!File.Exists(consolePath))
                throw new FileNotFoundException($"Console application not found: {consolePath}");

            var extension = Path.GetExtension(consolePath).ToLowerInvariant();
            
            if (extension == ".dll")
            {
                // For .dll files, use dotnet runtime
                _task.ExecutablePath = "dotnet";
                var arguments = $"\"{consolePath}\"";
                
                if (args != null && args.Length > 0)
                {
                    // Properly escape and quote arguments
                    var escapedArgs = args.Select(arg => 
                        arg.Contains(' ') || arg.Contains('"') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg);
                    arguments += " " + string.Join(" ", escapedArgs);
                }
                
                _task.Arguments = arguments;
            }
            else if (extension == ".exe")
            {
                // For .exe files, execute directly
                _task.ExecutablePath = consolePath;
                
                if (args != null && args.Length > 0)
                {
                    // Properly escape and quote arguments
                    var escapedArgs = args.Select(arg => 
                        arg.Contains(' ') || arg.Contains('"') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg);
                    _task.Arguments = string.Join(" ", escapedArgs);
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported console application type. Expected .exe or .dll, got: {extension}", nameof(consolePath));
            }

            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunOnce(DateTime runTime)
        {
            _task.Schedule = new TaskSchedule
            {
                IsOneTime = true,
                InitialDateTime = runTime
            };
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunOnceAfter(TimeSpan delay)
        {
            return RunOnce(DateTime.Now.Add(delay));
        }

        /// <inheritdoc />
        public ITaskBuilder RunEvery(int interval, ScheduleIntervalType intervalType)
        {
            if (interval <= 0)
                throw new ArgumentException("Interval must be greater than zero.", nameof(interval));

            _task.Schedule = new TaskSchedule
            {
                IsOneTime = false,
                IntervalType = intervalType,
                IntervalValue = interval,
                InitialDateTime = DateTime.Now.AddMinutes(1) // Default to start in 1 minute
            };
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunEveryMinutes(int minutes)
        {
            return RunEvery(minutes, ScheduleIntervalType.Minutes);
        }

        /// <inheritdoc />
        public ITaskBuilder RunEveryHours(int hours)
        {
            return RunEvery(hours, ScheduleIntervalType.Hours);
        }

        /// <inheritdoc />
        public ITaskBuilder RunEveryDays(int days)
        {
            return RunEvery(days, ScheduleIntervalType.Days);
        }

        /// <inheritdoc />
        public ITaskBuilder RunEveryWeeks(int weeks)
        {
            return RunEvery(weeks, ScheduleIntervalType.Weeks);
        }

        /// <inheritdoc />
        public ITaskBuilder StartingAt(DateTime startTime)
        {
            EnsureScheduleExists();
            _task.Schedule!.InitialDateTime = startTime;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder StartingAfter(TimeSpan delay)
        {
            return StartingAt(DateTime.Now.Add(delay));
        }

        /// <inheritdoc />
        public ITaskBuilder StartingNow()
        {
            return StartingAt(DateTime.Now);
        }

        /// <inheritdoc />
        public ITaskBuilder EndingAt(DateTime endTime)
        {
            EnsureScheduleExists();
            _task.Schedule!.HasEndDate = true;
            _task.Schedule.EndDate = endTime;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder EndingAfter(TimeSpan duration)
        {
            EnsureScheduleExists();
            var endTime = _task.Schedule!.InitialDateTime.Add(duration);
            return EndingAt(endTime);
        }

        /// <inheritdoc />
        public ITaskBuilder RunAsCurrentUser()
        {
            EnsureUserAccountExists();
            _task.UserAccount!.RunAsType = TaskRunAsType.CurrentUser;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunAsLocalSystem()
        {
            EnsureUserAccountExists();
            _task.UserAccount!.RunAsType = TaskRunAsType.LocalSystem;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunAsUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));

            EnsureUserAccountExists();
            _task.UserAccount!.RunAsType = TaskRunAsType.SpecificUser;
            _task.UserAccount.Username = username;
            // Note: Password is handled separately for security reasons
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder WithHighestPrivileges()
        {
            EnsureUserAccountExists();
            _task.UserAccount!.RunWithHighestPrivileges = true;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunWhenLoggedOff()
        {
            EnsureUserAccountExists();
            _task.UserAccount!.RunWhenLoggedOff = true;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder RunOnlyWhenLoggedOn()
        {
            EnsureUserAccountExists();
            _task.UserAccount!.RunWhenLoggedOff = false;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder TaskEnabled()
        {
            _task.Status = SchedulableTaskStatus.Active;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ITaskBuilder TaskDisabled()
        {
            _task.Status = SchedulableTaskStatus.Inactive;
            _task.Touch();
            return this;
        }

        /// <inheritdoc />
        public ISchedulableTask Build()
        {
            ValidateTask();
            return _task;
        }

        /// <inheritdoc />
        public async Task<bool> CreateAsync()
        {
            ValidateTask();
            return await _scheduler.CreateTaskAsync(_task);
        }

        /// <inheritdoc />
        public async Task<bool> CreateAndStartAsync()
        {
            ValidateTask();
            var created = await _scheduler.CreateTaskAsync(_task);
            if (created)
            {
                return await _scheduler.StartTaskAsync(_task);
            }
            return false;
        }

        private void EnsureScheduleExists()
        {
            if (_task.Schedule == null)
            {
                _task.Schedule = new TaskSchedule
                {
                    IsOneTime = false,
                    IntervalType = ScheduleIntervalType.Hours,
                    IntervalValue = 1,
                    InitialDateTime = DateTime.Now.AddMinutes(1)
                };
            }
        }

        private void EnsureUserAccountExists()
        {
            if (_task.UserAccount == null)
            {
                _task.UserAccount = new TaskUserAccount
                {
                    RunAsType = TaskRunAsType.CurrentUser,
                    RunWhenLoggedOff = true,
                    RunWithHighestPrivileges = false
                };
            }
        }

        private void ValidateTask()
        {
            if (string.IsNullOrWhiteSpace(_task.TaskName))
                throw new InvalidOperationException("Task name is required.");

            if (_task.Schedule == null)
            {
                // Provide a default schedule if none was specified
                _task.Schedule = new TaskSchedule
                {
                    IsOneTime = true,
                    InitialDateTime = DateTime.Now.AddMinutes(1)
                };
            }

            if (_task.UserAccount == null)
            {
                // Provide a default user account if none was specified
                _task.UserAccount = new TaskUserAccount
                {
                    RunAsType = TaskRunAsType.CurrentUser,
                    RunWhenLoggedOff = true,
                    RunWithHighestPrivileges = false
                };
            }

            // Validate schedule
            if (_task.Schedule.InitialDateTime < DateTime.Now.AddSeconds(-30))
            {
                throw new InvalidOperationException("Task cannot be scheduled to run in the past.");
            }

            if (!_task.Schedule.IsOneTime && _task.Schedule.IntervalValue <= 0)
            {
                throw new InvalidOperationException("Recurring tasks must have a positive interval value.");
            }

            if (_task.Schedule.HasEndDate && _task.Schedule.EndDate <= _task.Schedule.InitialDateTime)
            {
                throw new InvalidOperationException("End date must be after the start date.");
            }

            // Validate user account
            if (_task.UserAccount.RunAsType == TaskRunAsType.SpecificUser)
            {
                if (string.IsNullOrWhiteSpace(_task.UserAccount.Username))
                {
                    throw new InvalidOperationException("Username is required when running as a specific user.");
                }
            }

                         // Validate execution details
             if (string.IsNullOrWhiteSpace(_task.ExecutablePath))
             {
                 throw new InvalidOperationException("Task must specify what to execute. Use ExecuteProgram(), ExecutePowerShellScript(), ExecuteBatchFile(), ExecuteCommand(), or ExecuteConsoleApp().");
             }
        }
    }
} 