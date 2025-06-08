# Fluent.TaskScheduler

A comprehensive .NET 8 library for integrating with Windows Task Scheduler, providing a clean, fluent API for managing scheduled tasks in Windows applications with enterprise-grade reliability, error handling, and performance optimization.

**Last Updated:** 06-08-2025 02:19:10

## Features

### Core Functionality
- **Generic Task Interface**: Work with any schedulable task through the `ISchedulableTask` interface
- **Comprehensive Task Management**: Create, delete, start, stop, enable, disable, and monitor scheduled tasks
- **Flexible Scheduling**: Support for one-time and recurring schedules (minutes, hours, days, weeks)
- **Task Execution History**: Track and retrieve execution history with detailed information
- **User Account Management**: Configure tasks to run under different user accounts with various privileges

### Enterprise Features
- **Dependency Injection Integration**: Full support for Microsoft.Extensions.DependencyInjection with multiple configuration options
- **Structured Exception Handling**: Comprehensive exception hierarchy with specific error types and detailed context
- **Retry Policies**: Configurable retry mechanisms with exponential backoff, jitter, and circuit breaker patterns
- **Performance Optimization**: Connection pooling for TaskService instances with automatic cleanup and monitoring
- **Health Monitoring**: Built-in health checks and performance metrics for observability
- **Configuration Management**: Options pattern support with validation and configuration from appsettings.json

### Developer Experience
- **Async/Await Support**: Modern async programming patterns throughout
- **Structured Logging**: Built-in logging support using Microsoft.Extensions.Logging
- **Type Safety**: Strong typing with comprehensive interfaces and models
- **SOLID Principles**: Clean architecture following dependency inversion and single responsibility

## Quick Start

### Installation

```bash
dotnet add package Fluent.TaskScheduler
```

### Basic Setup with Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Extensions;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Fluent Task Scheduler with default configuration
        services.AddFluentTaskScheduler();
    })
    .Build();

await host.StartAsync();
```

### Custom Configuration

```csharp
services.AddFluentTaskScheduler(options =>
{
    options.TaskFolder = "MyApp.Tasks";
    options.DefaultRetryCount = 5;
    options.RetryBaseDelay = TimeSpan.FromSeconds(2);
    options.OperationTimeout = TimeSpan.FromMinutes(1);
    options.EnableDetailedLogging = true;
    options.TaskServicePoolSize = 10;
    options.CircuitBreakerFailureThreshold = 3;
});
```

### Configuration from appsettings.json

```json
{
  "TaskScheduler": {
    "TaskFolder": "MyApp.ScheduledTasks",
    "DefaultRetryCount": 5,
    "RetryBaseDelay": "00:00:02",
    "RetryMaxDelay": "00:02:00",
    "OperationTimeout": "00:01:00",
    "EnableDetailedLogging": true,
    "CircuitBreakerFailureThreshold": 5,
    "TaskServicePoolSize": 10
  }
}
```

```csharp
services.AddFluentTaskScheduler("TaskScheduler");
```

### Basic Usage

```csharp
public class TaskService
{
    private readonly ITaskSchedulerManager _taskManager;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskSchedulerManager taskManager, ILogger<TaskService> logger)
    {
        _taskManager = taskManager;
        _logger = logger;
    }

    public async Task CreateAndScheduleTaskAsync()
    {
        var task = new SimpleSchedulableTask
        {
            TaskId = Guid.NewGuid().ToString(),
            TaskName = "Data Processing Task",
            Description = "Processes data every hour",
            TaskType = "DataProcessing",
            Status = SchedulableTaskStatus.Active,
            ExecutablePath = "powershell.exe",
            Arguments = "-ExecutionPolicy Bypass -File \"C:\\Scripts\\DataProcessing.ps1\"",
            Schedule = new TaskSchedule
            {
                IsOneTime = false,
                IntervalType = ScheduleIntervalType.Hours,
                IntervalValue = 1,
                InitialDateTime = DateTime.Now.AddMinutes(5)
            },
            UserAccount = new TaskUserAccount
            {
                RunAsType = TaskRunAsType.CurrentUser,
                RunWhenLoggedOff = true
            }
        };

        try
        {
            await _taskManager.CreateScheduledTaskAsync(task);
            _logger.LogInformation("Successfully created task: {TaskName}", task.TaskName);
        }
        catch (TaskSchedulerPermissionException ex)
        {
            _logger.LogError(ex, "Permission denied creating task: {TaskName}", ex.TaskName);
        }
        catch (TaskSchedulerServiceException ex)
        {
            _logger.LogError(ex, "Task Scheduler service unavailable: {TaskName}", ex.TaskName);
        }
    }
}
```

## Task Execution Methods

The library provides multiple ways to define what your scheduled tasks should execute. All execution methods are part of the fluent API and can be chained with scheduling and configuration methods.

### Execute Programs and Applications

Run any executable file with optional arguments and working directory:

```csharp
var task = await taskScheduler.CreateTask("Run Backup Tool")
    .WithDescription("Runs the backup application")
    .Category("Backup")
    .ExecuteProgram(@"C:\Tools\BackupTool.exe", "--full --compress", @"C:\Data")
    .RunEveryDays(1)
    .StartingAt(DateTime.Today.AddHours(2)) // 2 AM
    .RunAsCurrentUser()
    .CreateAsync();
```

### Execute PowerShell Scripts

Run PowerShell script files with arguments:

```csharp
var task = await taskScheduler.CreateTask("System Maintenance")
    .WithDescription("Runs PowerShell maintenance script")
    .Category("Maintenance")
    .ExecutePowerShellScript(@"C:\Scripts\SystemMaintenance.ps1", "-Verbose -LogPath C:\Logs")
    .RunEveryHours(6)
    .RunAsLocalSystem()
    .WithHighestPrivileges()
    .CreateAsync();
```

### Execute Inline PowerShell Commands

Run PowerShell commands directly without a script file:

```csharp
var task = await taskScheduler.CreateTask("Process Monitor")
    .WithDescription("Monitors high CPU processes")
    .Category("Monitoring")
    .ExecutePowerShellCommands("Get-Process | Where-Object {$_.CPU -gt 100} | Export-Csv C:\\Logs\\HighCpuProcesses.csv")
    .RunEveryMinutes(30)
    .RunAsCurrentUser()
    .CreateAsync();
```

### Execute Batch Files

Run batch files or command scripts:

```csharp
var task = await taskScheduler.CreateTask("Cleanup Task")
    .WithDescription("Runs cleanup batch file")
    .Category("Cleanup")
    .ExecuteBatchFile(@"C:\Scripts\CleanupTemp.bat", "/quiet /force")
    .RunEveryDays(1)
    .StartingAt(DateTime.Today.AddHours(3)) // 3 AM
    .RunAsLocalSystem()
    .CreateAsync();
```

### Execute Custom Commands

Run any command with arguments:

```csharp
var task = await taskScheduler.CreateTask("File Sync")
    .WithDescription("Synchronizes files using robocopy")
    .Category("Utility")
    .ExecuteCommand("robocopy", @"C:\Source C:\Backup /MIR /LOG:C:\Logs\sync.log")
    .RunEveryWeeks(1)
    .RunAsCurrentUser()
    .WithHighestPrivileges()
    .CreateAsync();
```

### Execute .NET Console Applications

Run .NET console applications (both .exe and .dll files) with proper argument handling:

```csharp
// Execute a .NET console application (.exe)
var exeTask = await taskScheduler.CreateTask("Data Processor")
    .WithDescription("Runs data processing console application")
    .Category("Processing")
    .ExecuteConsoleApp(@"C:\Apps\DataProcessor.exe", new[] { "--input", @"C:\Data\input.csv", "--output", @"C:\Data\output.json" })
    .RunEveryHours(2)
    .RunAsCurrentUser()
    .CreateAsync();

// Execute a .NET console application (.dll) - automatically uses dotnet runtime
var dllTask = await taskScheduler.CreateTask("Report Generator")
    .WithDescription("Generates reports using .NET console app")
    .Category("Reporting")
    .ExecuteConsoleApp(@"C:\Apps\ReportGenerator.dll", new[] { "--format", "pdf", "--template", "monthly" })
    .RunEveryDays(30)
    .StartingAt(DateTime.Today.AddDays(1).AddHours(6)) // Tomorrow at 6 AM
    .RunAsCurrentUser()
    .CreateAsync();

// Execute without arguments
var simpleTask = await taskScheduler.CreateTask("Health Check")
    .WithDescription("Runs health check console app")
    .Category("Monitoring")
    .ExecuteConsoleApp(@"C:\Tools\HealthCheck.exe")
    .RunEveryMinutes(15)
    .RunAsCurrentUser()
    .CreateAsync();
```

### Fluent Task Builder (Facade Pattern)

For even simpler usage, use the fluent facade:

```csharp
// Inject IFluentTaskScheduler instead of ITaskSchedulerManager
public class MyService
{
    private readonly IFluentTaskScheduler _taskScheduler;

    public MyService(IFluentTaskScheduler taskScheduler)
    {
        _taskScheduler = taskScheduler;
    }

    public async Task CreateDailyBackupTask()
    {
        var success = await _taskScheduler.CreateTask("Daily Backup")
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
            // Task created successfully
            var status = await _taskScheduler.GetTaskStatusAsync("Daily Backup");
            var nextRun = await _taskScheduler.GetNextRunTimeAsync("Daily Backup");
        }
    }
}
```

### Task Execution Examples

Here are comprehensive examples showing different execution scenarios:

```csharp
public async Task CreateVariousTaskTypes(IFluentTaskScheduler taskScheduler)
{
    // 1. Database backup using SQL Server tools
    await taskScheduler.CreateTask("Database Backup")
        .Category("Database")
        .ExecuteCommand("sqlcmd", "-S localhost -E -Q \"BACKUP DATABASE MyDB TO DISK='C:\\Backups\\MyDB.bak'\"")
        .RunEveryDays(1)
        .StartingAt(DateTime.Today.AddHours(1)) // 1 AM
        .RunAsLocalSystem()
        .CreateAsync();

    // 2. Log rotation using PowerShell
    await taskScheduler.CreateTask("Log Rotation")
        .Category("Maintenance")
        .ExecutePowerShellCommands(@"
            Get-ChildItem C:\Logs\*.log | 
            Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | 
            Remove-Item -Force")
        .RunEveryDays(7)
        .RunAsCurrentUser()
        .CreateAsync();

    // 3. Application health check
    await taskScheduler.CreateTask("Health Check")
        .Category("Monitoring")
        .ExecutePowerShellScript(@"C:\Scripts\HealthCheck.ps1", "-Endpoint https://api.myapp.com/health")
        .RunEveryMinutes(5)
        .RunAsCurrentUser()
        .CreateAsync();

         // 4. File compression and archival
     await taskScheduler.CreateTask("Archive Files")
         .Category("Archival")
         .ExecuteProgram(@"C:\Tools\7zip\7z.exe", "a C:\\Archives\\monthly-$(Get-Date -Format 'yyyy-MM').7z C:\\Data\\*", @"C:\Data")
         .RunEveryDays(30)
         .StartingAt(DateTime.Today.AddDays(30).AddHours(4)) // Next month at 4 AM
         .RunAsCurrentUser()
         .WithHighestPrivileges()
         .CreateAsync();

     // 5. .NET console application (.dll) with complex arguments
     await taskScheduler.CreateTask("Data Analytics")
         .Category("Analytics")
         .ExecuteConsoleApp(@"C:\Apps\DataAnalytics.dll", new[] 
         { 
             "--source", @"C:\Data\raw", 
             "--destination", @"C:\Data\processed",
             "--format", "json",
             "--parallel", "4",
             "--log-level", "verbose"
         })
         .RunEveryDays(1)
         .StartingAt(DateTime.Today.AddHours(5)) // 5 AM
         .RunAsCurrentUser()
         .CreateAsync();
 }
```

### Validation and Error Handling

The library validates that execution details are provided:

```csharp
try
{
    // This will throw an exception because no execution method was called
    await taskScheduler.CreateTask("Invalid Task")
        .Category("Test")
        .RunEveryHours(1)
        .CreateAsync();
}
 catch (InvalidOperationException ex)
 {
     // ex.Message: "Task must specify what to execute. Use ExecuteProgram(), ExecutePowerShellScript(), ExecuteBatchFile(), ExecuteCommand(), or ExecuteConsoleApp()."
 }
```

### Working Directory and Environment

When using `ExecuteProgram()`, you can specify a working directory:

```csharp
await taskScheduler.CreateTask("Build Project")
    .Category("Development")
    .ExecuteProgram(@"C:\Tools\MSBuild\MSBuild.exe", "MyProject.sln /p:Configuration=Release", @"C:\Projects\MyProject")
    .RunOnce(DateTime.Now.AddMinutes(5))
    .RunAsCurrentUser()
    .CreateAsync();
```

## Error Handling & Resilience

### Exception Hierarchy

The library provides a comprehensive exception hierarchy for precise error handling:

```csharp
try
{
    await taskManager.CreateScheduledTaskAsync(task);
}
catch (TaskSchedulerPermissionException ex)
{
    // Handle permission issues - may need administrator privileges
    logger.LogError("Permission denied. Try running as Administrator.");
}
catch (TaskSchedulerServiceException ex)
{
    // Handle Task Scheduler service unavailability
    logger.LogError("Task Scheduler service is not available.");
}
catch (TaskConfigurationException ex)
{
    // Handle invalid task configuration
    logger.LogError("Invalid configuration for property: {Property}", ex.PropertyName);
}
catch (TaskOperationTimeoutException ex)
{
    // Handle operation timeouts
    logger.LogError("Operation timed out after {Timeout}", ex.Timeout);
}
catch (TaskNotFoundException ex)
{
    // Handle missing tasks
    logger.LogError("Task not found: {TaskName}", ex.TaskName);
}
```

### Retry Policies

Configure custom retry policies for critical operations:

```csharp
var retryPolicy = new RetryPolicyOptions
{
    RetryCount = 10,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(5),
    UseExponentialBackoff = true,
    JitterFactor = 0.2
};

await retryPolicyService.ExecuteWithRetryAsync(async () =>
{
    // Your critical operation here
    await SomeCriticalOperation();
}, retryPolicy, "CriticalOperation");
```

### Graceful Degradation

```csharp
try
{
    await taskManager.CreateScheduledTaskAsync(task);
}
catch (TaskSchedulerServiceException)
{
    // Implement fallback strategy
    await ImplementTimerBasedFallback(task);
}
catch (TaskSchedulerPermissionException)
{
    // Run in limited mode
    await LogTaskForManualScheduling(task);
}
```

## Performance & Monitoring

### Connection Pooling

The library automatically manages TaskService connections through a connection pool:

```csharp
// Pool statistics are automatically tracked
var poolStats = taskServicePool.GetStats();
logger.LogInformation("Pool: {Total} total, {Available} available, {InUse} in use", 
    poolStats.TotalInstances, poolStats.AvailableInstances, poolStats.InUseInstances);
```

### Health Checks

Implement health monitoring for your task scheduler:

```csharp
public async Task<bool> CheckTaskSchedulerHealth()
{
    var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
    var taskServicePool = serviceProvider.GetRequiredService<ITaskServicePool>();
    
    // Check pool health
    var poolStats = taskServicePool.GetStats();
    var poolHealthy = poolStats.TotalInstances > 0 && 
                     poolStats.FailedOperations < poolStats.TotalOperations * 0.5;
    
    return poolHealthy;
}
```

### Performance Benchmarking

```csharp
// Benchmark task operations
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 100; i++)
{
    await taskManager.CreateScheduledTaskAsync(tasks[i]);
}
stopwatch.Stop();

logger.LogInformation("Created 100 tasks in {Duration}ms", stopwatch.ElapsedMilliseconds);
```

## Architecture

### Core Interfaces

- **`ITaskSchedulerManager`**: Main interface for managing Windows scheduled tasks
- **`ISchedulableTask`**: Represents any task that can be scheduled
- **`IRetryPolicyService`**: Service for executing operations with retry logic
- **`ITaskServicePool`**: Connection pool for TaskService instances

### Configuration Options

- **`TaskSchedulerOptions`**: Main configuration class with retry policies, timeouts, and pool settings
- **`RetryPolicyOptions`**: Configurable retry behavior with exponential backoff
- **`TaskSchedulerOptionsValidator`**: Validates configuration options

### Exception Types

- **`TaskSchedulerException`**: Base exception class
- **`TaskSchedulerPermissionException`**: Permission-related errors
- **`TaskSchedulerServiceException`**: Service unavailability errors
- **`TaskConfigurationException`**: Configuration validation errors
- **`TaskOperationTimeoutException`**: Timeout-related errors
- **`TaskNotFoundException`**: Missing task errors

### Key Models

- **`TaskSchedule`**: Defines when and how often a task should run
- **`TaskUserAccount`**: Configures user account and privilege settings
- **`WindowsTaskDetails`**: Windows-specific task configuration
- **`TaskExecutionHistory`**: Historical execution information
- **`TaskServicePoolStats`**: Pool performance statistics

## Configuration Options

### TaskSchedulerOptions

```csharp
public class TaskSchedulerOptions
{
    public string TaskFolder { get; set; } = "Fluent.TaskScheduler";
    public int DefaultRetryCount { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    public bool UseExponentialBackoff { get; set; } = true;
    public double JitterFactor { get; set; } = 0.1;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int DefaultMaxHistoryEntries { get; set; } = 50;
    public bool EnableDetailedLogging { get; set; } = false;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public int TaskServicePoolSize { get; set; } = 5;
    public TimeSpan TaskServicePoolTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### User Account Configuration

```csharp
var userAccount = new TaskUserAccount
{
    RunAsType = TaskRunAsType.CurrentUser,
    RunWhenLoggedOff = true,
    RunWithHighestPrivileges = false,
    Username = "DOMAIN\\Username",  // For SpecificUser type
    Password = "password"           // Use SecureString in production
};
```

### Scheduling Options

```csharp
// One-time execution
var oneTimeSchedule = new TaskSchedule
{
    IsOneTime = true,
    InitialDateTime = DateTime.Now.AddHours(1)
};

// Recurring every 30 minutes
var recurringSchedule = new TaskSchedule
{
    IsOneTime = false,
    IntervalType = ScheduleIntervalType.Minutes,
    IntervalValue = 30,
    InitialDateTime = DateTime.Now.AddMinutes(5),
    HasEndDate = true,
    EndDate = DateTime.Now.AddDays(30)
};
```

## Examples

The library includes comprehensive examples demonstrating various features:

- **`DependencyInjectionExamples.cs`**: Shows how to set up and configure the library with DI
- **`ErrorHandlingExamples.cs`**: Demonstrates error handling, retry policies, and resilience patterns
- **`PerformanceMonitoringExamples.cs`**: Shows performance monitoring, health checks, and observability

## Requirements

- .NET 8.0 or later
- Windows operating system
- Administrator privileges may be required for certain operations
- Windows Task Scheduler service must be running

## Dependencies

- **TaskScheduler** (2.12.1): Windows Task Scheduler COM wrapper
- **System.Management** (8.0.0): Windows management APIs
- **Microsoft.Extensions.Logging.Abstractions** (8.0.0): Logging abstractions
- **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0): DI abstractions
- **Microsoft.Extensions.Options** (8.0.0): Options pattern support
- **Microsoft.Extensions.Hosting** (8.0.0): Hosting abstractions

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For questions, issues, or feature requests, please create an issue on the GitHub repository. 