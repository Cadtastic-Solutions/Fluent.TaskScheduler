using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Services;
using Fluent.TaskScheduler.Models;
using Fluent.TaskScheduler.Exceptions;

namespace Fluent.TaskScheduler.Examples
{
    /// <summary>
    /// Helper class for logger type parameter.
    /// </summary>
    public class ErrorHandlingExamplesLogger { }

    /// <summary>
    /// Examples demonstrating error handling, retry policies, and resilience features.
    /// </summary>
    public static class ErrorHandlingExamples
    {
        /// <summary>
        /// Example: Creating a task with comprehensive error handling.
        /// </summary>
        public static async Task<ISchedulableTask?> CreateTaskWithErrorHandlingExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<ErrorHandlingExamplesLogger>>();

            try
            {
                var task = new SimpleSchedulableTask
                {
                    TaskId = Guid.NewGuid().ToString(),
                    TaskName = "Error Handling Demo Task",
                    Description = "A task that demonstrates comprehensive error handling",
                    TaskType = "Demo",
                    Status = SchedulableTaskStatus.Active,
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

                // The task manager automatically handles retries and error recovery
                await taskManager.CreateScheduledTaskAsync(task);
                
                logger.LogInformation("Successfully created task: {TaskName}", task.TaskName);
                return task;
            }
            catch (TaskSchedulerPermissionException ex)
            {
                logger.LogError(ex, "Permission denied. Task: {TaskId}, TaskName: {TaskName}", 
                    ex.TaskId, ex.TaskName);
                logger.LogWarning("Try running as Administrator or check user privileges.");
                return null;
            }
            catch (TaskSchedulerServiceException ex)
            {
                logger.LogError(ex, "Task Scheduler service is unavailable. Task: {TaskId}, TaskName: {TaskName}", 
                    ex.TaskId, ex.TaskName);
                logger.LogWarning("Please check if the Task Scheduler service is running.");
                return null;
            }
            catch (TaskConfigurationException ex)
            {
                logger.LogError(ex, "Invalid task configuration. Property: {PropertyName}, Task: {TaskId}, TaskName: {TaskName}", 
                    ex.PropertyName, ex.TaskId, ex.TaskName);
                return null;
            }
            catch (TaskOperationTimeoutException ex)
            {
                logger.LogError(ex, "Operation timed out after {Timeout}. Task: {TaskId}, TaskName: {TaskName}", 
                    ex.Timeout, ex.TaskId, ex.TaskName);
                return null;
            }
            catch (TaskNotFoundException ex)
            {
                logger.LogError(ex, "Task not found. Task: {TaskId}, TaskName: {TaskName}", 
                    ex.TaskId, ex.TaskName);
                return null;
            }
            catch (Fluent.TaskScheduler.Exceptions.TaskSchedulerException ex)
            {
                logger.LogError(ex, "General task scheduler error. Task: {TaskId}, TaskName: {TaskName}", 
                    ex.TaskId, ex.TaskName);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred while creating task");
                return null;
            }
        }

        /// <summary>
        /// Example: Using custom retry policies for specific operations.
        /// </summary>
        public static async Task CustomRetryPolicyExample(IServiceProvider serviceProvider)
        {
            var retryPolicyService = serviceProvider.GetRequiredService<IRetryPolicyService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ErrorHandlingExamplesLogger>>();

            // Custom retry policy for critical operations
            var criticalRetryPolicy = new RetryPolicyOptions
            {
                RetryCount = 10,
                BaseDelay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromMinutes(5),
                UseExponentialBackoff = true,
                JitterFactor = 0.2
            };

            try
            {
                await retryPolicyService.ExecuteWithRetryAsync(async () =>
                {
                    // Simulate a critical operation that might fail
                    logger.LogInformation("Executing critical operation...");
                    
                    // Your critical operation here
                    await Task.Delay(100); // Simulate work
                    
                    // Simulate occasional failures for demonstration
                    if (Random.Shared.Next(1, 4) == 1)
                    {
                        throw new InvalidOperationException("Simulated failure");
                    }
                    
                    logger.LogInformation("Critical operation completed successfully");
                    
                }, criticalRetryPolicy, "CriticalOperation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical operation failed after all retries");
            }
        }

        /// <summary>
        /// Example: Handling different types of failures during task operations.
        /// </summary>
        public static async Task TaskOperationFailureHandlingExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<ErrorHandlingExamplesLogger>>();

            var task = new SimpleSchedulableTask
            {
                TaskId = Guid.NewGuid().ToString(),
                TaskName = "Failure Handling Test Task",
                Description = "A task for testing failure scenarios",
                TaskType = "Test"
            };

            // Test different operation failures
            await TestCreateTaskFailure(taskManager, task, logger);
            await TestStartTaskFailure(taskManager, task, logger);
            await TestStatusCheckFailure(taskManager, task, logger);
            await TestDeleteTaskFailure(taskManager, task, logger);
        }

        private static async Task TestCreateTaskFailure(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            try
            {
                // This might fail due to permissions, service unavailability, etc.
                await taskManager.CreateScheduledTaskAsync(task);
                logger.LogInformation("Task creation succeeded");
            }
            catch (TaskSchedulerPermissionException)
            {
                logger.LogWarning("Task creation failed due to insufficient permissions");
            }
            catch (TaskSchedulerServiceException)
            {
                logger.LogWarning("Task creation failed due to service unavailability");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during task creation");
            }
        }

        private static async Task TestStartTaskFailure(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            try
            {
                await taskManager.StartScheduledTaskAsync(task);
                logger.LogInformation("Task start succeeded");
            }
            catch (TaskNotFoundException)
            {
                logger.LogWarning("Cannot start task - task not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during task start");
            }
        }

        private static async Task TestStatusCheckFailure(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            try
            {
                var status = await taskManager.GetScheduledTaskStatusAsync(task);
                logger.LogInformation("Task status check succeeded: {Status}", status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking task status");
            }
        }

        private static async Task TestDeleteTaskFailure(ITaskSchedulerManager taskManager, ISchedulableTask task, ILogger logger)
        {
            try
            {
                await taskManager.DeleteScheduledTaskAsync(task);
                logger.LogInformation("Task deletion succeeded");
            }
            catch (TaskNotFoundException)
            {
                logger.LogWarning("Cannot delete task - task not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during task deletion");
            }
        }

        /// <summary>
        /// Example: Graceful degradation when Task Scheduler is unavailable.
        /// </summary>
        public static async Task GracefulDegradationExample(IServiceProvider serviceProvider)
        {
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<ErrorHandlingExamplesLogger>>();

            var task = new SimpleSchedulableTask
            {
                TaskId = Guid.NewGuid().ToString(),
                TaskName = "Graceful Degradation Test",
                Description = "Testing graceful degradation",
                TaskType = "Test"
            };

            try
            {
                // Attempt to create the task
                await taskManager.CreateScheduledTaskAsync(task);
                logger.LogInformation("Task scheduling is available and working");
            }
            catch (TaskSchedulerServiceException)
            {
                logger.LogWarning("Task Scheduler service is unavailable. Implementing fallback strategy...");
                
                // Implement fallback strategy
                await ImplementFallbackStrategy(task, logger);
            }
            catch (TaskSchedulerPermissionException)
            {
                logger.LogWarning("Insufficient permissions for task scheduling. Running in limited mode...");
                
                // Implement limited mode
                await ImplementLimitedMode(task, logger);
            }
        }

        private static async Task ImplementFallbackStrategy(ISchedulableTask task, ILogger logger)
        {
            // Example fallback: Use a timer-based approach instead of Windows Task Scheduler
            logger.LogInformation("Implementing timer-based fallback for task: {TaskName}", task.TaskName);
            
            // You could implement an in-memory scheduler here
            await Task.Delay(100); // Simulate fallback setup
            
            logger.LogInformation("Fallback strategy implemented successfully");
        }

        private static async Task ImplementLimitedMode(ISchedulableTask task, ILogger logger)
        {
            // Example limited mode: Log the task but don't schedule it
            logger.LogInformation("Running in limited mode for task: {TaskName}", task.TaskName);
            logger.LogInformation("Task would be scheduled with: {Schedule}", 
                task.Schedule?.ToString() ?? "No schedule");
            
            await Task.Delay(100); // Simulate limited mode setup
            
            logger.LogInformation("Limited mode implemented successfully");
        }
    }
} 