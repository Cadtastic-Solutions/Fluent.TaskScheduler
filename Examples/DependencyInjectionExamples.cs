using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Fluent.TaskScheduler.Extensions;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Services;

namespace Fluent.TaskScheduler.Examples
{
    /// <summary>
    /// Helper class for logger type parameter.
    /// </summary>
    public class DependencyInjectionExamplesLogger { }

    /// <summary>
    /// Examples demonstrating how to set up Fluent Task Scheduler with dependency injection
    /// and various configuration options.
    /// </summary>
    public static class DependencyInjectionExamples
    {
        /// <summary>
        /// Example: Basic setup with default configuration.
        /// </summary>
        public static async Task<IHost> BasicSetupExample()
        {
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
            return host;
        }

        /// <summary>
        /// Example: Setup with custom configuration options.
        /// </summary>
        public static async Task<IHost> CustomConfigurationExample()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add logging
                    services.AddLogging(builder => builder.AddConsole());
                    
                    // Add Fluent Task Scheduler with custom configuration
                    services.AddFluentTaskScheduler(options =>
                    {
                        options.TaskFolder = "MyApp.Tasks";
                        options.DefaultRetryCount = 5;
                        options.RetryBaseDelay = TimeSpan.FromSeconds(2);
                        options.RetryMaxDelay = TimeSpan.FromMinutes(2);
                        options.OperationTimeout = TimeSpan.FromMinutes(1);
                        options.EnableDetailedLogging = true;
                        options.TaskServicePoolSize = 10;
                        options.CircuitBreakerFailureThreshold = 3;
                        options.CircuitBreakerTimeout = TimeSpan.FromMinutes(5);
                    });
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        /// <summary>
        /// Example: Setup with configuration from appsettings.json.
        /// </summary>
        public static async Task<IHost> ConfigurationFileExample()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices((context, services) =>
                {
                    // Add logging
                    services.AddLogging(builder => builder.AddConsole());
                    
                    // Add Fluent Task Scheduler with configuration from appsettings.json
                    // Expects a "TaskScheduler" section in appsettings.json
                    services.AddFluentTaskScheduler("TaskScheduler");
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        /// <summary>
        /// Example: Using the configured services in your application.
        /// </summary>
        public static Task UsingConfiguredServicesExample(IServiceProvider serviceProvider)
        {
            // Get the configured services
            var taskManager = serviceProvider.GetRequiredService<ITaskSchedulerManager>();
            var retryPolicyService = serviceProvider.GetRequiredService<IRetryPolicyService>();
            var taskServicePool = serviceProvider.GetRequiredService<ITaskServicePool>();
            var logger = serviceProvider.GetRequiredService<ILogger<DependencyInjectionExamplesLogger>>();

            logger.LogInformation("Successfully retrieved all configured services");

            // Check pool statistics
            var stats = taskServicePool.GetStats();
            logger.LogInformation("Task Service Pool initialized with {TotalInstances} instances", stats.TotalInstances);
            
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Example appsettings.json configuration for TaskScheduler section:
    /// 
    /// {
    ///   "TaskScheduler": {
    ///     "TaskFolder": "MyApp.ScheduledTasks",
    ///     "DefaultRetryCount": 5,
    ///     "RetryBaseDelay": "00:00:02",
    ///     "RetryMaxDelay": "00:02:00",
    ///     "OperationTimeout": "00:01:00",
    ///     "DefaultMaxHistoryEntries": 100,
    ///     "EnableDetailedLogging": true,
    ///     "CircuitBreakerFailureThreshold": 5,
    ///     "CircuitBreakerTimeout": "00:05:00",
    ///     "TaskServicePoolSize": 10,
    ///     "TaskServicePoolTimeout": "00:00:30"
    ///   }
    /// }
    /// </summary>
    public static class ConfigurationReference
    {
        // This class serves as documentation for the configuration structure
    }
} 