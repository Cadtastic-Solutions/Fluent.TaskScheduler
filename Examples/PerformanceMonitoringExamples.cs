using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Services;
using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Examples
{
    /// <summary>
    /// Helper class for logger type parameter.
    /// </summary>
    public class PerformanceMonitoringExamplesLogger { }

    /// <summary>
    /// Examples demonstrating performance monitoring, observability, and pool management features.
    /// </summary>
    public static class PerformanceMonitoringExamples
    {
        /// <summary>
        /// Example: Monitoring TaskService pool performance and statistics.
        /// </summary>
        public static async Task TaskServicePoolMonitoringExample(IServiceProvider serviceProvider)
        {
            var taskServicePool = serviceProvider.GetRequiredService<ITaskServicePool>();
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringExamplesLogger>>();

            // Get initial pool statistics
            var initialStats = taskServicePool.GetStats();
            LogPoolStatistics("Initial", initialStats, logger);

            // Simulate some operations to generate statistics
            await SimulatePoolOperations(taskServicePool, logger);

            // Get updated statistics
            var finalStats = taskServicePool.GetStats();
            LogPoolStatistics("Final", finalStats, logger);

            // Analyze performance
            AnalyzePoolPerformance(initialStats, finalStats, logger);
        }

        /// <summary>
        /// Example: Monitoring task execution history and performance.
        /// </summary>
        public static async Task TaskExecutionMonitoringExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringExamplesLogger>>();

            // Create a sample task for monitoring
            var task = new SimpleSchedulableTask
            {
                TaskId = Guid.NewGuid().ToString(),
                TaskName = "Performance Monitoring Test Task",
                Description = "A task for demonstrating execution monitoring",
                TaskType = "Monitoring",
                Status = SchedulableTaskStatus.Active,
                Schedule = new TaskSchedule
                {
                    IsOneTime = false,
                    IntervalType = ScheduleIntervalType.Minutes,
                    IntervalValue = 5,
                    InitialDateTime = DateTime.Now.AddMinutes(1)
                }
            };

            try
            {
                // Create and start the task
                await taskManager.CreateScheduledTaskAsync(task);
                logger.LogInformation("Created monitoring task: {TaskName}", task.TaskName);

                // Monitor task status
                await MonitorTaskStatus(taskManager, task, logger);

                // Get execution history
                await AnalyzeExecutionHistory(taskManager, task, logger);

                // Clean up
                await taskManager.DeleteScheduledTaskAsync(task);
                logger.LogInformation("Cleaned up monitoring task");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during task execution monitoring");
            }
        }

        /// <summary>
        /// Example: Performance benchmarking of task operations.
        /// </summary>
        public static async Task TaskOperationBenchmarkExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringExamplesLogger>>();

            const int numberOfTasks = 10;
            var tasks = new List<ISchedulableTask>();

            // Benchmark task creation
            var createStartTime = DateTime.UtcNow;
            for (int i = 0; i < numberOfTasks; i++)
            {
                var task = new SimpleSchedulableTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    TaskName = $"Benchmark Task {i + 1}",
                    Description = $"Benchmark task number {i + 1}",
                    TaskType = "Benchmark",
                    Status = SchedulableTaskStatus.Active,
                    Schedule = new TaskSchedule
                    {
                        IsOneTime = true,
                        InitialDateTime = DateTime.Now.AddHours(1)
                    }
                };

                try
                {
                    await taskManager.CreateScheduledTaskAsync(task);
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create benchmark task {TaskNumber}", i + 1);
                }
            }
            var createEndTime = DateTime.UtcNow;
            var createDuration = createEndTime - createStartTime;

            logger.LogInformation("Created {TaskCount} tasks in {Duration}ms (avg: {AvgDuration}ms per task)",
                tasks.Count, createDuration.TotalMilliseconds, createDuration.TotalMilliseconds / tasks.Count);

            // Benchmark status checks
            var statusStartTime = DateTime.UtcNow;
            var statusResults = new List<Fluent.TaskScheduler.Models.TaskStatus>();
            foreach (var task in tasks)
            {
                try
                {
                    var status = await taskManager.GetScheduledTaskStatusAsync(task);
                    statusResults.Add(status);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get status for task {TaskName}", task.TaskName);
                }
            }
            var statusEndTime = DateTime.UtcNow;
            var statusDuration = statusEndTime - statusStartTime;

            logger.LogInformation("Checked status for {TaskCount} tasks in {Duration}ms (avg: {AvgDuration}ms per task)",
                statusResults.Count, statusDuration.TotalMilliseconds, statusDuration.TotalMilliseconds / statusResults.Count);

            // Benchmark task deletion
            var deleteStartTime = DateTime.UtcNow;
            var deletedCount = 0;
            foreach (var task in tasks)
            {
                try
                {
                    await taskManager.DeleteScheduledTaskAsync(task);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete task {TaskName}", task.TaskName);
                }
            }
            var deleteEndTime = DateTime.UtcNow;
            var deleteDuration = deleteEndTime - deleteStartTime;

            logger.LogInformation("Deleted {TaskCount} tasks in {Duration}ms (avg: {AvgDuration}ms per task)",
                deletedCount, deleteDuration.TotalMilliseconds, deleteDuration.TotalMilliseconds / deletedCount);

            // Overall benchmark summary
            var totalDuration = deleteEndTime - createStartTime;
            logger.LogInformation("=== Benchmark Summary ===");
            logger.LogInformation("Total operations: {Operations}", numberOfTasks * 3); // Create, Status, Delete
            logger.LogInformation("Total time: {Duration}ms", totalDuration.TotalMilliseconds);
            logger.LogInformation("Average time per operation: {AvgDuration}ms", totalDuration.TotalMilliseconds / (numberOfTasks * 3));
        }

        /// <summary>
        /// Example: Health check implementation for task scheduler services.
        /// </summary>
        public static async Task<bool> HealthCheckExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var taskServicePool = serviceProvider.GetRequiredService<ITaskServicePool>();
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringExamplesLogger>>();

            var healthChecks = new List<(string Name, bool IsHealthy, string Details)>();

            // Check TaskService pool health
            try
            {
                var poolStats = taskServicePool.GetStats();
                var poolHealthy = poolStats.TotalInstances > 0 && 
                                 poolStats.FailedOperations < poolStats.TotalOperations * 0.5;
                
                healthChecks.Add(("TaskService Pool", poolHealthy, 
                    $"Instances: {poolStats.TotalInstances}, Operations: {poolStats.TotalOperations}, Failures: {poolStats.FailedOperations}"));
            }
            catch (Exception ex)
            {
                healthChecks.Add(("TaskService Pool", false, $"Error: {ex.Message}"));
            }

            // Check task manager functionality
            try
            {
                var testTask = new SimpleSchedulableTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    TaskName = "Health Check Test Task",
                    Description = "Temporary task for health checking",
                    TaskType = "HealthCheck",
                    Status = SchedulableTaskStatus.Inactive // Don't actually schedule it
                };

                // Test basic operations without actually scheduling
                var taskName = taskManager.GetWindowsTaskName(testTask);
                var taskPath = taskManager.GetWindowsTaskPath(testTask);
                var exists = await taskManager.ScheduledTaskExistsAsync(testTask);

                healthChecks.Add(("Task Manager", true, "Basic operations functional"));
            }
            catch (Exception ex)
            {
                healthChecks.Add(("Task Manager", false, $"Error: {ex.Message}"));
            }

            // Log health check results
            logger.LogInformation("=== Health Check Results ===");
            var overallHealthy = true;
            foreach (var (name, isHealthy, details) in healthChecks)
            {
                var status = isHealthy ? "HEALTHY" : "UNHEALTHY";
                logger.LogInformation("{Name}: {Status} - {Details}", name, status, details);
                overallHealthy &= isHealthy;
            }

            logger.LogInformation("Overall Health: {Status}", overallHealthy ? "HEALTHY" : "UNHEALTHY");
            return overallHealthy;
        }

        private static async Task SimulatePoolOperations(ITaskServicePool taskServicePool, ILogger logger)
        {
            logger.LogInformation("Simulating pool operations...");

            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(taskServicePool.ExecuteAsync(async taskService =>
                {
                    await Task.Delay(Random.Shared.Next(100, 500));
                    return true;
                }));
            }

            await Task.WhenAll(tasks);
            logger.LogInformation("Pool operations simulation completed");
        }

        private static void LogPoolStatistics(string label, TaskServicePoolStats stats, ILogger logger)
        {
            logger.LogInformation("=== {Label} Pool Statistics ===", label);
            logger.LogInformation("Total Instances: {Total}", stats.TotalInstances);
            logger.LogInformation("Available Instances: {Available}", stats.AvailableInstances);
            logger.LogInformation("In Use Instances: {InUse}", stats.InUseInstances);
            logger.LogInformation("Total Operations: {Operations}", stats.TotalOperations);
            logger.LogInformation("Failed Operations: {Failed}", stats.FailedOperations);
            logger.LogInformation("Average Wait Time: {WaitTime}ms", stats.AverageWaitTime.TotalMilliseconds);
        }

        private static void AnalyzePoolPerformance(TaskServicePoolStats initial, TaskServicePoolStats final, ILogger logger)
        {
            logger.LogInformation("=== Pool Performance Analysis ===");
            
            var operationsDelta = final.TotalOperations - initial.TotalOperations;
            var failuresDelta = final.FailedOperations - initial.FailedOperations;
            
            logger.LogInformation("Operations performed: {Operations}", operationsDelta);
            logger.LogInformation("Failures occurred: {Failures}", failuresDelta);
            
            if (operationsDelta > 0)
            {
                var successRate = ((double)(operationsDelta - failuresDelta) / operationsDelta) * 100;
                logger.LogInformation("Success rate: {SuccessRate:F2}%", successRate);
            }

            // Performance recommendations
            if (final.AverageWaitTime.TotalMilliseconds > 1000)
            {
                logger.LogWarning("High average wait time detected. Consider increasing TaskServicePoolSize.");
            }

            if (final.FailedOperations > final.TotalOperations * 0.1)
            {
                logger.LogWarning("High failure rate detected. Check system health and retry policies.");
            }
        }

        private static async Task MonitorTaskStatus(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            logger.LogInformation("Monitoring task status for: {TaskName}", task.TaskName);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var status = await taskManager.GetScheduledTaskStatusAsync(task);
                    var nextRun = await taskManager.GetNextRunTimeAsync(task);
                    
                    logger.LogInformation("Status check {CheckNumber}: Status={Status}, NextRun={NextRun}", 
                        i + 1, status, nextRun);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed status check {CheckNumber}", i + 1);
                }

                if (i < 2) await Task.Delay(1000); // Wait between checks
            }
        }

        private static async Task AnalyzeExecutionHistory(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            try
            {
                var history = await taskManager.GetTaskExecutionHistoryAsync(task, 20);
                var historyList = history.ToList();

                logger.LogInformation("=== Execution History Analysis ===");
                logger.LogInformation("Total history entries: {Count}", historyList.Count);

                if (historyList.Any())
                {
                    var successfulRuns = historyList.Count(h => h.ResultCode == 0);
                    var failedRuns = historyList.Count(h => h.ResultCode != 0);
                    var runningTasks = historyList.Count(h => h.State == "Running");

                    logger.LogInformation("Successful runs: {Successful}", successfulRuns);
                    logger.LogInformation("Failed runs: {Failed}", failedRuns);
                    logger.LogInformation("Currently running: {Running}", runningTasks);

                    if (historyList.Count > 0)
                    {
                        var successRate = ((double)successfulRuns / historyList.Count) * 100;
                        logger.LogInformation("Success rate: {SuccessRate:F2}%", successRate);
                    }

                    // Show recent executions
                    var recentExecutions = historyList.OrderByDescending(h => h.StartTime).Take(5);
                    logger.LogInformation("Recent executions:");
                    foreach (var execution in recentExecutions)
                    {
                        logger.LogInformation("  {StartTime}: {State} (Result: {ResultCode})", 
                            execution.StartTime, execution.State, execution.ResultCode);
                    }
                }
                else
                {
                    logger.LogInformation("No execution history available");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze execution history");
            }
        }
    }
} 