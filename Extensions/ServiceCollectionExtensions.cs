using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Fluent.TaskScheduler.Configuration;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Services;

namespace Fluent.TaskScheduler.Extensions
{
    /// <summary>
    /// Extension methods for configuring Fluent Task Scheduler services in dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Fluent Task Scheduler services to the service collection with default configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddFluentTaskScheduler(this IServiceCollection services)
        {
            return services.AddFluentTaskScheduler(_ => { });
        }

        /// <summary>
        /// Adds Fluent Task Scheduler services to the service collection with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure the task scheduler options.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddFluentTaskScheduler(this IServiceCollection services, Action<TaskSchedulerOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);

            // Add core services
            services.TryAddSingleton<ITaskServicePool, TaskServicePool>();
            services.TryAddSingleton<IRetryPolicyService, RetryPolicyService>();
            services.TryAddSingleton<ITaskSchedulerManager, TaskSchedulerManager>();
            services.TryAddTransient<TaskIntegrationService>();
            
            // Add facade
            services.TryAddSingleton<IFluentTaskScheduler, FluentTaskScheduler>();

            // Add validation for options
            services.TryAddSingleton<IValidateOptions<TaskSchedulerOptions>, TaskSchedulerOptionsValidator>();

            return services;
        }

        /// <summary>
        /// Adds Fluent Task Scheduler services to the service collection with configuration from IConfiguration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSectionName">The name of the configuration section. Default: "TaskScheduler"</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddFluentTaskScheduler(this IServiceCollection services, string configurationSectionName = "TaskScheduler")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // This will be bound from configuration automatically
            services.AddOptions<TaskSchedulerOptions>()
                .BindConfiguration(configurationSectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Add core services
            services.TryAddSingleton<ITaskServicePool, TaskServicePool>();
            services.TryAddSingleton<IRetryPolicyService, RetryPolicyService>();
            services.TryAddSingleton<ITaskSchedulerManager, TaskSchedulerManager>();
            services.TryAddTransient<TaskIntegrationService>();
            
            // Add facade
            services.TryAddSingleton<IFluentTaskScheduler, FluentTaskScheduler>();

            // Add validation for options
            services.TryAddSingleton<IValidateOptions<TaskSchedulerOptions>, TaskSchedulerOptionsValidator>();

            return services;
        }

        /// <summary>
        /// Adds Fluent Task Scheduler services with custom implementations.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure the task scheduler options.</param>
        /// <param name="configureServices">Action to configure custom service implementations.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddFluentTaskScheduler(
            this IServiceCollection services,
            Action<TaskSchedulerOptions> configureOptions,
            Action<IServiceCollection> configureServices)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Configure options first
            services.Configure(configureOptions);

            // Allow custom service configuration
            configureServices?.Invoke(services);

            // Add default implementations for services not already registered
            services.TryAddSingleton<ITaskServicePool, TaskServicePool>();
            services.TryAddSingleton<IRetryPolicyService, RetryPolicyService>();
            services.TryAddSingleton<ITaskSchedulerManager, TaskSchedulerManager>();
            services.TryAddTransient<TaskIntegrationService>();
            
            // Add facade
            services.TryAddSingleton<IFluentTaskScheduler, FluentTaskScheduler>();

            // Add validation for options
            services.TryAddSingleton<IValidateOptions<TaskSchedulerOptions>, TaskSchedulerOptionsValidator>();

            return services;
        }
    }

    /// <summary>
    /// Validator for TaskSchedulerOptions to ensure configuration is valid.
    /// </summary>
    internal class TaskSchedulerOptionsValidator : IValidateOptions<TaskSchedulerOptions>
    {
        public ValidateOptionsResult Validate(string? name, TaskSchedulerOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.TaskFolder))
            {
                failures.Add("TaskFolder cannot be null or empty.");
            }

            if (options.DefaultRetryCount < 0)
            {
                failures.Add("DefaultRetryCount cannot be negative.");
            }

            if (options.RetryBaseDelay <= TimeSpan.Zero)
            {
                failures.Add("RetryBaseDelay must be greater than zero.");
            }

            if (options.RetryMaxDelay <= TimeSpan.Zero)
            {
                failures.Add("RetryMaxDelay must be greater than zero.");
            }

            if (options.RetryMaxDelay < options.RetryBaseDelay)
            {
                failures.Add("RetryMaxDelay must be greater than or equal to RetryBaseDelay.");
            }

            if (options.OperationTimeout <= TimeSpan.Zero)
            {
                failures.Add("OperationTimeout must be greater than zero.");
            }

            if (options.DefaultMaxHistoryEntries <= 0)
            {
                failures.Add("DefaultMaxHistoryEntries must be greater than zero.");
            }

            if (options.CircuitBreakerFailureThreshold <= 0)
            {
                failures.Add("CircuitBreakerFailureThreshold must be greater than zero.");
            }

            if (options.CircuitBreakerTimeout <= TimeSpan.Zero)
            {
                failures.Add("CircuitBreakerTimeout must be greater than zero.");
            }

            if (options.TaskServicePoolSize <= 0)
            {
                failures.Add("TaskServicePoolSize must be greater than zero.");
            }

            if (options.TaskServicePoolTimeout <= TimeSpan.Zero)
            {
                failures.Add("TaskServicePoolTimeout must be greater than zero.");
            }

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
} 