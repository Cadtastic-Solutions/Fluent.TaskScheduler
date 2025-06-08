using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Extensions;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Services;
using Fluent.TaskScheduler.Configuration;

namespace Fluent.TaskScheduler.Examples
{
    /// <summary>
    /// Helper class for logger type parameter.
    /// </summary>
    public class FacadePatternExamplesLogger { }

    /// <summary>
    /// Examples demonstrating the facade pattern and simplified fluent API for task scheduling.
    /// Shows how the facade provides a clean, easy-to-use interface while hiding complexity.
    /// </summary>
    public static class FacadePatternExamples
    {
        /// <summary>
        /// Example: Basic setup and usage with the facade pattern.
        /// </summary>
        public static async Task BasicFacadeUsageExample()
        {
            // Simple setup with facade
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder => builder.AddConsole());
                    services.AddFluentTaskScheduler(); // Registers facade + all underlying services
                })
                .Build();

            await host.StartAsync();

            // Get the facade - this is the ONLY service you need to inject!
            var taskScheduler = host.Services.GetRequiredService<IFluentTaskScheduler>();
            var logger = host.Services.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            // Simple task creation using fluent API
            var success = await taskScheduler.CreateTask("Daily Backup")
                .WithDescription("Backs up important files daily")
                .Category("Maintenance")
                .ExecutePowerShellScript(@"C:\Scripts\DailyBackup.ps1")
                .RunEveryDays(1)
                .StartingAt(DateTime.Today.AddHours(2)) // 2 AM
                .RunAsCurrentUser()
                .RunWhenLoggedOff()
                .CreateAsync();

            if (success)
            {
                logger.LogInformation("Successfully created daily backup task!");
            }
            else
            {
                logger.LogError("Failed to create daily backup task");
            }

            await host.StopAsync();
        }

        /// <summary>
        /// Example: Comprehensive task management using the facade.
        /// </summary>
        public static async Task ComprehensiveTaskManagementExample(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            // Create multiple tasks with different schedules
            var tasks = new[]
            {
                ("Email Reports", "Sends daily email reports", 1, ScheduleIntervalType.Days),
                ("Cache Cleanup", "Cleans up temporary cache files", 4, ScheduleIntervalType.Hours),
                ("Database Backup", "Backs up the main database", 1, ScheduleIntervalType.Weeks),
                ("Log Rotation", "Rotates application log files", 30, ScheduleIntervalType.Minutes)
            };

            foreach (var (name, description, interval, intervalType) in tasks)
            {
                var success = await taskScheduler.CreateTask(name)
                    .WithDescription(description)
                    .Category("Automated")
                    .ExecutePowerShellCommands($"Write-Host 'Executing {name} at $(Get-Date)'")
                    .RunEvery(interval, intervalType)
                    .StartingNow()
                    .RunAsCurrentUser()
                    .RunWhenLoggedOff()
                    .CreateAsync();

                if (success)
                {
                    logger.LogInformation("Created task: {TaskName}", name);
                    
                    // Check if task exists
                    var exists = await taskScheduler.TaskExistsAsync(name);
                    logger.LogInformation("Task {TaskName} exists: {Exists}", name, exists);
                    
                    // Get task status
                    var status = await taskScheduler.GetTaskStatusAsync(name);
                    logger.LogInformation("Task {TaskName} status: {Status}", name, status);
                    
                    // Get next run time
                    var nextRun = await taskScheduler.GetNextRunTimeAsync(name);
                    logger.LogInformation("Task {TaskName} next run: {NextRun}", name, nextRun);
                }
                else
                {
                    logger.LogError("Failed to create task: {TaskName}", name);
                }
            }
        }

        /// <summary>
        /// Example: Advanced fluent API usage with complex scheduling.
        /// </summary>
        public static async Task AdvancedFluentApiExample(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            // Complex task with multiple configuration options
            var success = await taskScheduler.CreateTask("Monthly Report Generator")
                .WithDescription("Generates comprehensive monthly reports for management")
                .Category("Reporting")
                .ExecutePowerShellScript(@"C:\Scripts\GenerateMonthlyReport.ps1", "-OutputPath C:\\Reports")
                .RunEveryDays(30) // Every 30 days
                .StartingAt(DateTime.Today.AddDays(1).AddHours(6)) // Tomorrow at 6 AM
                .EndingAt(DateTime.Today.AddYears(1)) // End after 1 year
                .RunAsCurrentUser()
                .WithHighestPrivileges()
                .RunWhenLoggedOff()
                .TaskEnabled()
                .CreateAndStartAsync(); // Create and start immediately

            if (success)
            {
                logger.LogInformation("Successfully created and started monthly report task");
            }

            // One-time task example
            var oneTimeSuccess = await taskScheduler.CreateTask("System Maintenance")
                .WithDescription("Performs one-time system maintenance")
                .Category("Maintenance")
                .ExecuteBatchFile(@"C:\Scripts\SystemMaintenance.bat")
                .RunOnceAfter(TimeSpan.FromHours(2)) // Run in 2 hours
                .RunAsLocalSystem()
                .WithHighestPrivileges()
                .CreateAsync();

            if (oneTimeSuccess)
            {
                logger.LogInformation("Successfully created one-time maintenance task");
            }
        }

        /// <summary>
        /// Example: Health monitoring and performance tracking with the facade.
        /// </summary>
        public static async Task HealthMonitoringExample(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            // Perform health check
            var healthStatus = await taskScheduler.CheckHealthAsync();
            
            logger.LogInformation("=== Task Scheduler Health Check ===");
            logger.LogInformation("Overall Health: {IsHealthy}", healthStatus.IsHealthy);
            logger.LogInformation("Health Score: {HealthScore:F2}", healthStatus.HealthScore);
            logger.LogInformation("Service Available: {ServiceAvailable}", healthStatus.ServiceAvailable);
            logger.LogInformation("Task Manager Healthy: {TaskManagerHealthy}", healthStatus.TaskManagerHealthy);
            logger.LogInformation("Retry Service Healthy: {RetryServiceHealthy}", healthStatus.RetryServiceHealthy);

            if (healthStatus.Warnings.Any())
            {
                logger.LogWarning("Health Warnings:");
                foreach (var warning in healthStatus.Warnings)
                {
                    logger.LogWarning("  - {Warning}", warning);
                }
            }

            if (healthStatus.Errors.Any())
            {
                logger.LogError("Health Errors:");
                foreach (var error in healthStatus.Errors)
                {
                    logger.LogError("  - {Error}", error);
                }
            }

            // Get performance statistics
            var perfStats = await taskScheduler.GetPerformanceStatsAsync();
            
            logger.LogInformation("=== Performance Statistics ===");
            logger.LogInformation("Tasks Created: {TasksCreated}", perfStats.TotalTasksCreated);
            logger.LogInformation("Tasks Started: {TasksStarted}", perfStats.TotalTasksStarted);
            logger.LogInformation("Tasks Stopped: {TasksStopped}", perfStats.TotalTasksStopped);
            logger.LogInformation("Tasks Deleted: {TasksDeleted}", perfStats.TotalTasksDeleted);
            logger.LogInformation("Success Rate: {SuccessRate:F2}%", perfStats.SuccessRate);
            logger.LogInformation("Average Operation Duration: {AvgDuration:F2}ms", perfStats.AverageOperationDuration);

            // Pool statistics
            if (perfStats.PoolStats != null)
            {
                logger.LogInformation("Pool Total Instances: {Total}", perfStats.PoolStats.TotalInstances);
                logger.LogInformation("Pool Available: {Available}", perfStats.PoolStats.AvailableInstances);
                logger.LogInformation("Pool In Use: {InUse}", perfStats.PoolStats.InUseInstances);
                logger.LogInformation("Pool Operations: {Operations}", perfStats.PoolStats.TotalOperations);
                logger.LogInformation("Pool Failures: {Failures}", perfStats.PoolStats.FailedOperations);
            }
        }

        /// <summary>
        /// Example: Custom retry operations using the facade.
        /// </summary>
        public static async Task CustomRetryExample(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            // Execute a custom operation with default retry policy
            var success = await taskScheduler.ExecuteWithRetryAsync(async () =>
            {
                logger.LogInformation("Executing custom operation...");
                
                // Simulate some work that might fail
                await Task.Delay(100);
                
                // Simulate occasional failures
                if (Random.Shared.Next(1, 4) == 1)
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                
                logger.LogInformation("Custom operation completed successfully");
            }, "CustomOperation");

            if (success)
            {
                logger.LogInformation("Custom operation succeeded");
            }
            else
            {
                logger.LogError("Custom operation failed after all retries");
            }

            // Execute with custom retry policy
            var customRetryPolicy = new RetryPolicyOptions
            {
                RetryCount = 5,
                BaseDelay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromMinutes(1),
                UseExponentialBackoff = true,
                JitterFactor = 0.3
            };

            var customSuccess = await taskScheduler.ExecuteWithRetryAsync(async () =>
            {
                logger.LogInformation("Executing critical operation with custom retry policy...");
                await Task.Delay(50);
                logger.LogInformation("Critical operation completed");
            }, customRetryPolicy, "CriticalOperation");

            if (customSuccess)
            {
                logger.LogInformation("Critical operation succeeded");
            }
        }

        /// <summary>
        /// Example: Different task execution methods using the facade.
        /// </summary>
        public static async Task TaskExecutionExamples(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            logger.LogInformation("=== Task Execution Examples ===");

            // 1. Execute a program/executable
            var programTask = await taskScheduler.CreateTask("Run Notepad")
                .WithDescription("Opens Notepad application")
                .Category("Demo")
                .ExecuteProgram(@"C:\Windows\System32\notepad.exe")
                .RunOnceAfter(TimeSpan.FromMinutes(1))
                .RunAsCurrentUser()
                .CreateAsync();

            logger.LogInformation("Program task created: {Success}", programTask);

            // 2. Execute a PowerShell script file
            var scriptTask = await taskScheduler.CreateTask("PowerShell Script")
                .WithDescription("Runs a PowerShell script for system maintenance")
                .Category("Maintenance")
                .ExecutePowerShellScript(@"C:\Scripts\SystemMaintenance.ps1", "-Verbose")
                .RunEveryHours(6)
                .StartingAt(DateTime.Today.AddHours(22)) // 10 PM
                .RunAsLocalSystem()
                .WithHighestPrivileges()
                .CreateAsync();

            logger.LogInformation("PowerShell script task created: {Success}", scriptTask);

            // 3. Execute inline PowerShell commands
            var commandTask = await taskScheduler.CreateTask("PowerShell Commands")
                .WithDescription("Runs inline PowerShell commands")
                .Category("Monitoring")
                .ExecutePowerShellCommands("Get-Process | Where-Object {$_.CPU -gt 100} | Export-Csv C:\\Logs\\HighCpuProcesses.csv")
                .RunEveryMinutes(30)
                .StartingNow()
                .RunAsCurrentUser()
                .CreateAsync();

            logger.LogInformation("PowerShell commands task created: {Success}", commandTask);

            // 4. Execute a batch file
            var batchTask = await taskScheduler.CreateTask("Batch File")
                .WithDescription("Runs a batch file for file cleanup")
                .Category("Cleanup")
                .ExecuteBatchFile(@"C:\Scripts\CleanupTemp.bat", "/quiet")
                .RunEveryDays(1)
                .StartingAt(DateTime.Today.AddHours(3)) // 3 AM
                .RunAsLocalSystem()
                .CreateAsync();

            logger.LogInformation("Batch file task created: {Success}", batchTask);

            // 5. Execute a custom command
            var customTask = await taskScheduler.CreateTask("Custom Command")
                .WithDescription("Runs a custom command with arguments")
                .Category("Utility")
                .ExecuteCommand("robocopy", @"C:\Source C:\Backup /MIR /LOG:C:\Logs\backup.log")
                .RunEveryWeeks(1)
                .StartingAt(DateTime.Today.AddDays(7).AddHours(1)) // Next week at 1 AM
                .RunAsCurrentUser()
                .WithHighestPrivileges()
                .CreateAsync();

            logger.LogInformation("Custom command task created: {Success}", customTask);

            // 6. Complex task with working directory
            var complexTask = await taskScheduler.CreateTask("Complex Task")
                .WithDescription("Runs a program with specific working directory")
                .Category("Development")
                .ExecuteProgram(@"C:\Tools\MyApp.exe", "--config production --verbose", @"C:\Projects\MyProject")
                .RunOnce(DateTime.Today.AddDays(1).AddHours(9)) // Tomorrow at 9 AM
                .RunAsCurrentUser()
                .RunOnlyWhenLoggedOn()
                .CreateAsync();

                         logger.LogInformation("Complex task created: {Success}", complexTask);

             // 7. .NET Console application (.dll)
             var consoleAppTask = await taskScheduler.CreateTask("Data Analytics")
                 .WithDescription("Runs .NET console app for data analytics")
                 .Category("Analytics")
                 .ExecuteConsoleApp(@"C:\Apps\DataAnalytics.dll", new[] 
                 { 
                     "--source", @"C:\Data\input", 
                     "--output", @"C:\Reports\analytics.json",
                     "--format", "detailed",
                     "--threads", "4"
                 })
                 .RunEveryDays(1)
                 .StartingAt(DateTime.Today.AddHours(4)) // 4 AM
                 .RunAsCurrentUser()
                 .CreateAsync();

             logger.LogInformation("Console app task created: {Success}", consoleAppTask);

             // 8. .NET Console application (.exe) without arguments
             var simpleConsoleTask = await taskScheduler.CreateTask("System Check")
                 .WithDescription("Runs system check console application")
                 .Category("Monitoring")
                 .ExecuteConsoleApp(@"C:\Tools\SystemCheck.exe")
                 .RunEveryHours(6)
                 .RunAsCurrentUser()
                 .CreateAsync();

             logger.LogInformation("Simple console task created: {Success}", simpleConsoleTask);
         }

        /// <summary>
        /// Example: Task lifecycle management using the facade.
        /// </summary>
        public static async Task TaskLifecycleExample(IServiceProvider serviceProvider)
        {
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            const string taskName = "Lifecycle Demo Task";

            try
            {
                // Create a task
                var created = await taskScheduler.CreateTask(taskName)
                    .WithDescription("Demonstrates task lifecycle management")
                    .Category("Demo")
                    .ExecutePowerShellCommands("Write-Host 'Lifecycle demo task executed at $(Get-Date)'")
                    .RunEveryHours(1)
                    .StartingNow()
                    .CreateAsync();

                if (!created)
                {
                    logger.LogError("Failed to create task");
                    return;
                }

                logger.LogInformation("✓ Task created successfully");

                // Check if it exists
                var exists = await taskScheduler.TaskExistsAsync(taskName);
                logger.LogInformation("✓ Task exists: {Exists}", exists);

                // Start the task
                var started = await taskScheduler.StartTaskAsync(taskName);
                logger.LogInformation("✓ Task started: {Started}", started);

                // Get status
                var status = await taskScheduler.GetTaskStatusAsync(taskName);
                logger.LogInformation("✓ Task status: {Status}", status);

                // Wait a moment
                await Task.Delay(2000);

                // Stop the task
                var stopped = await taskScheduler.StopTaskAsync(taskName);
                logger.LogInformation("✓ Task stopped: {Stopped}", stopped);

                // Clean up - delete the task
                var deleted = await taskScheduler.DeleteTaskAsync(taskName);
                logger.LogInformation("✓ Task deleted: {Deleted}", deleted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during task lifecycle demonstration");
            }
        }

        /// <summary>
        /// Example: Comparison between facade usage and direct service usage.
        /// </summary>
        public static async Task FacadeVsDirectServicesExample(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FacadePatternExamplesLogger>>();

            logger.LogInformation("=== Facade Pattern vs Direct Services Comparison ===");

            // === FACADE APPROACH (Simple) ===
            logger.LogInformation("--- Using Facade (Simple) ---");
            
            var taskScheduler = serviceProvider.GetRequiredService<IFluentTaskScheduler>();
            
            // Simple one-liner task creation
            var facadeSuccess = await taskScheduler.CreateTask("Facade Example Task")
                .WithDescription("Created using the facade pattern")
                .RunEveryHours(2)
                .StartingNow()
                .CreateAsync();

            logger.LogInformation("Facade approach success: {Success}", facadeSuccess);

            // === DIRECT SERVICES APPROACH (Complex) ===
            logger.LogInformation("--- Using Direct Services (Complex) ---");
            
            // Advanced users can still access underlying services directly
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var retryService = serviceProvider.GetRequiredService<IRetryPolicyService>();
            
            try
            {
                var directTask = new SimpleSchedulableTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    TaskName = "Direct Services Example Task",
                    Description = "Created using direct services",
                    TaskType = "Example",
                    Status = SchedulableTaskStatus.Active,
                    Schedule = new TaskSchedule
                    {
                        IsOneTime = false,
                        IntervalType = ScheduleIntervalType.Hours,
                        IntervalValue = 2,
                        InitialDateTime = DateTime.Now
                    },
                    UserAccount = new TaskUserAccount
                    {
                        RunAsType = TaskRunAsType.CurrentUser,
                        RunWhenLoggedOff = true
                    }
                };

                await retryService.ExecuteWithRetryAsync(async () =>
                {
                    await taskManager.CreateScheduledTaskAsync(directTask);
                }, "CreateDirectTask");

                logger.LogInformation("Direct services approach success: True");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Direct services approach failed");
            }

            logger.LogInformation("=== Summary ===");
            logger.LogInformation("Facade: Simple, clean, handles complexity automatically");
            logger.LogInformation("Direct Services: Full control, more complex, requires more knowledge");
            logger.LogInformation("Both approaches are available - choose based on your needs!");
        }
    }
} 