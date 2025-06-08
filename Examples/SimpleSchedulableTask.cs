using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fluent.TaskScheduler.Interfaces;
using Fluent.TaskScheduler.Models;

namespace Fluent.TaskScheduler.Examples
{
    /// <summary>
    /// Simple example implementation of ISchedulableTask for demonstration purposes.
    /// This shows how to implement the interface for your own task types.
    /// </summary>
    public class SimpleSchedulableTask : ISchedulableTask
    {
        private string _taskId = string.Empty;
        private string _taskName = string.Empty;
        private string? _description;
        private string _taskType = "SimpleTask";
        private SchedulableTaskStatus _status = SchedulableTaskStatus.Inactive;
        private DateTime _created = DateTime.Now;
        private DateTime _modified = DateTime.Now;
        private DateTime? _lastExecuted;
        private DateTime? _nextScheduledExecution;
        private int _executionCount;
        private WindowsTaskDetails? _windowsTaskDetails;
        private TaskUserAccount? _userAccount;
        private TaskSchedule? _schedule;
        private string? _executablePath;
        private string? _arguments;
        private string? _workingDirectory;

        /// <inheritdoc/>
        public string TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        /// <inheritdoc/>
        public string TaskName
        {
            get => _taskName;
            set => SetProperty(ref _taskName, value);
        }

        /// <inheritdoc/>
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <inheritdoc/>
        public string TaskType
        {
            get => _taskType;
            set => SetProperty(ref _taskType, value);
        }

        /// <inheritdoc/>
        public SchedulableTaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <inheritdoc/>
        public DateTime Created
        {
            get => _created;
            set => SetProperty(ref _created, value);
        }

        /// <inheritdoc/>
        public DateTime Modified
        {
            get => _modified;
            set => SetProperty(ref _modified, value);
        }

        /// <inheritdoc/>
        public DateTime? LastExecuted
        {
            get => _lastExecuted;
            set => SetProperty(ref _lastExecuted, value);
        }

        /// <inheritdoc/>
        public DateTime? NextScheduledExecution
        {
            get => _nextScheduledExecution;
            set => SetProperty(ref _nextScheduledExecution, value);
        }

        /// <inheritdoc/>
        public int ExecutionCount
        {
            get => _executionCount;
            set => SetProperty(ref _executionCount, value);
        }

        /// <inheritdoc/>
        public WindowsTaskDetails? WindowsTaskDetails
        {
            get => _windowsTaskDetails;
            set => SetProperty(ref _windowsTaskDetails, value);
        }

        /// <inheritdoc/>
        public TaskUserAccount? UserAccount
        {
            get => _userAccount;
            set => SetProperty(ref _userAccount, value);
        }

        /// <inheritdoc/>
        public TaskSchedule? Schedule
        {
            get => _schedule;
            set => SetProperty(ref _schedule, value);
        }

        /// <inheritdoc/>
        public string? ExecutablePath
        {
            get => _executablePath;
            set => SetProperty(ref _executablePath, value);
        }

        /// <inheritdoc/>
        public string? Arguments
        {
            get => _arguments;
            set => SetProperty(ref _arguments, value);
        }

        /// <inheritdoc/>
        public string? WorkingDirectory
        {
            get => _workingDirectory;
            set => SetProperty(ref _workingDirectory, value);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        public void Touch()
        {
            Modified = DateTime.Now;
        }

        /// <inheritdoc/>
        public void MarkExecuted()
        {
            LastExecuted = DateTime.Now;
            ExecutionCount++;
            Touch();
            UpdateNextExecutionTime();
        }

        /// <inheritdoc/>
        public void UpdateNextExecutionTime()
        {
            if (Schedule == null || Schedule.IsOneTime)
            {
                NextScheduledExecution = null;
                return;
            }

            var baseTime = LastExecuted ?? Schedule.InitialDateTime;
            
            NextScheduledExecution = Schedule.IntervalType switch
            {
                ScheduleIntervalType.Minutes => baseTime.AddMinutes(Schedule.IntervalValue),
                ScheduleIntervalType.Hours => baseTime.AddHours(Schedule.IntervalValue),
                ScheduleIntervalType.Days => baseTime.AddDays(Schedule.IntervalValue),
                ScheduleIntervalType.Weeks => baseTime.AddDays(Schedule.IntervalValue * 7),
                _ => null
            };

            // Check if we've passed the end date
            if (Schedule.HasEndDate && NextScheduledExecution > Schedule.EndDate)
            {
                NextScheduledExecution = null;
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The backing field for the property.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property (automatically provided).</param>
        /// <returns>True if the property value changed, false otherwise.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 