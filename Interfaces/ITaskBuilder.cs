using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Interfaces
{
    /// <summary>
    /// Fluent builder interface for creating and configuring scheduled tasks.
    /// Provides a clean, readable API for task configuration.
    /// </summary>
    public interface ITaskBuilder
    {
        /// <summary>
        /// Sets the description for the task.
        /// </summary>
        /// <param name="description">The task description.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder WithDescription(string description);

        /// <summary>
        /// Sets the task category for organization and filtering.
        /// </summary>
        /// <param name="category">The task category (e.g., "Maintenance", "Backup", "Reporting").</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder Category(string category);

        /// <summary>
        /// Configures the task to execute a program or executable file.
        /// </summary>
        /// <param name="programPath">The full path to the executable file.</param>
        /// <param name="arguments">Optional command-line arguments for the program.</param>
        /// <param name="workingDirectory">Optional working directory for the program.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecuteProgram(string programPath, string? arguments = null, string? workingDirectory = null);

        /// <summary>
        /// Configures the task to execute a PowerShell script.
        /// </summary>
        /// <param name="scriptPath">The full path to the PowerShell script file.</param>
        /// <param name="arguments">Optional arguments for the script.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecutePowerShellScript(string scriptPath, string? arguments = null);

        /// <summary>
        /// Configures the task to execute inline PowerShell commands.
        /// </summary>
        /// <param name="commands">The PowerShell commands to execute.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecutePowerShellCommands(string commands);

        /// <summary>
        /// Configures the task to execute a batch file or command script.
        /// </summary>
        /// <param name="batchFilePath">The full path to the batch file.</param>
        /// <param name="arguments">Optional arguments for the batch file.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecuteBatchFile(string batchFilePath, string? arguments = null);

        /// <summary>
        /// Configures the task to execute command line commands.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">Optional arguments for the command.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecuteCommand(string command, string? arguments = null);

        /// <summary>
        /// Configures the task to execute a .NET console application (.exe or .dll).
        /// Automatically handles .dll files using dotnet runtime.
        /// </summary>
        /// <param name="consolePath">The full path to the console application (.exe or .dll).</param>
        /// <param name="args">Optional array of arguments for the console application.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder ExecuteConsoleApp(string consolePath, string[]? args = null);

        /// <summary>
        /// Configures the task to run once at the specified time.
        /// </summary>
        /// <param name="runTime">When to run the task.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunOnce(DateTime runTime);

        /// <summary>
        /// Configures the task to run once after the specified delay.
        /// </summary>
        /// <param name="delay">How long to wait before running the task.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunOnceAfter(TimeSpan delay);

        /// <summary>
        /// Configures the task to run repeatedly at the specified interval.
        /// </summary>
        /// <param name="interval">The interval between runs.</param>
        /// <param name="intervalType">The type of interval (minutes, hours, days, weeks).</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunEvery(int interval, ScheduleIntervalType intervalType);

        /// <summary>
        /// Configures the task to run every specified number of minutes.
        /// </summary>
        /// <param name="minutes">The number of minutes between runs.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunEveryMinutes(int minutes);

        /// <summary>
        /// Configures the task to run every specified number of hours.
        /// </summary>
        /// <param name="hours">The number of hours between runs.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunEveryHours(int hours);

        /// <summary>
        /// Configures the task to run every specified number of days.
        /// </summary>
        /// <param name="days">The number of days between runs.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunEveryDays(int days);

        /// <summary>
        /// Configures the task to run every specified number of weeks.
        /// </summary>
        /// <param name="weeks">The number of weeks between runs.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunEveryWeeks(int weeks);

        /// <summary>
        /// Sets when the recurring task should start.
        /// </summary>
        /// <param name="startTime">When to start the recurring schedule.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder StartingAt(DateTime startTime);

        /// <summary>
        /// Sets the recurring task to start after the specified delay.
        /// </summary>
        /// <param name="delay">How long to wait before starting the recurring schedule.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder StartingAfter(TimeSpan delay);

        /// <summary>
        /// Sets the recurring task to start immediately.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder StartingNow();

        /// <summary>
        /// Sets when the recurring task should end.
        /// </summary>
        /// <param name="endTime">When to stop the recurring schedule.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder EndingAt(DateTime endTime);

        /// <summary>
        /// Sets the recurring task to end after the specified duration.
        /// </summary>
        /// <param name="duration">How long the recurring schedule should run.</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder EndingAfter(TimeSpan duration);

        /// <summary>
        /// Configures the task to run as the current user.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunAsCurrentUser();

        /// <summary>
        /// Configures the task to run as the local system account.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunAsLocalSystem();

        /// <summary>
        /// Configures the task to run as a specific user.
        /// Note: Password handling is managed separately for security reasons.
        /// </summary>
        /// <param name="username">The username to run as.</param>
        /// <param name="password">The password for the user account (for API compatibility).</param>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunAsUser(string username, string password);

        /// <summary>
        /// Configures the task to run with highest privileges.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder WithHighestPrivileges();

        /// <summary>
        /// Configures the task to run even when the user is not logged on.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunWhenLoggedOff();

        /// <summary>
        /// Configures the task to only run when the user is logged on.
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder RunOnlyWhenLoggedOn();

        /// <summary>
        /// Sets the task status to active (enabled).
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder TaskEnabled();

        /// <summary>
        /// Sets the task status to inactive (disabled).
        /// </summary>
        /// <returns>The task builder for method chaining.</returns>
        ITaskBuilder TaskDisabled();

        /// <summary>
        /// Gets the configured task without creating it in the scheduler.
        /// </summary>
        /// <returns>The configured schedulable task.</returns>
        ISchedulableTask Build();

        /// <summary>
        /// Creates the configured task in the Windows Task Scheduler.
        /// </summary>
        /// <returns>True if the task was created successfully, false otherwise.</returns>
        Task<bool> CreateAsync();

        /// <summary>
        /// Creates the configured task in the Windows Task Scheduler and starts it immediately.
        /// </summary>
        /// <returns>True if the task was created and started successfully, false otherwise.</returns>
        Task<bool> CreateAndStartAsync();
    }
} 