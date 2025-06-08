using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Win32.TaskScheduler;

namespace Fluent.TaskScheduler.Models
{
    /// <summary>
    /// Represents the scheduling configuration for a schedulable task.
    /// Defines when and how often a task should be executed by Windows Task Scheduler.
    /// </summary>
    public class TaskSchedule : INotifyPropertyChanged
    {
        private DateTime _initialDateTime = DateTime.Now.AddMinutes(10);
        private bool _isOneTime = false;
        private ScheduleIntervalType _intervalType = ScheduleIntervalType.Hours;
        private int _intervalValue = 1;
        private DaysOfTheWeek _selectedDays = DaysOfTheWeek.AllDays;
        private bool _hasEndDate = false;
        private DateTime _endDate = DateTime.Now.AddYears(1);

        /// <summary>
        /// The initial date and time when the task should first run.
        /// </summary>
        [JsonPropertyName("initialDateTime")]
        [Required]
        public DateTime InitialDateTime 
        { 
            get => _initialDateTime; 
            set 
            {
                // Ensure DateTime is always treated as Local time for Task Scheduler compatibility
                var localValue = value.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(value, DateTimeKind.Local)
                    : value.ToLocalTime();
                SetProperty(ref _initialDateTime, localValue);
            }
        }

        /// <summary>
        /// Whether this is a one-time task execution or a recurring schedule.
        /// </summary>
        [JsonPropertyName("isOneTime")]
        public bool IsOneTime 
        { 
            get => _isOneTime; 
            set => SetProperty(ref _isOneTime, value); 
        }

        /// <summary>
        /// The type of interval for recurring schedules (Minutes, Hours, Days, Weeks).
        /// </summary>
        [JsonPropertyName("intervalType")]
        public ScheduleIntervalType IntervalType 
        { 
            get => _intervalType; 
            set => SetProperty(ref _intervalType, value); 
        }

        /// <summary>
        /// The numeric value for the interval (e.g., every 2 hours, every 3 days).
        /// </summary>
        [JsonPropertyName("intervalValue")]
        [Range(1, int.MaxValue, ErrorMessage = "Interval value must be at least 1")]
        public int IntervalValue 
        { 
            get => _intervalValue; 
            set => SetProperty(ref _intervalValue, value); 
        }

        /// <summary>
        /// The days of the week when the task should run (for weekly schedules).
        /// </summary>
        [JsonPropertyName("selectedDays")]
        public DaysOfTheWeek SelectedDays 
        { 
            get => _selectedDays; 
            set => SetProperty(ref _selectedDays, value); 
        }

        /// <summary>
        /// Whether the schedule has an end date after which the task will no longer run.
        /// </summary>
        [JsonPropertyName("hasEndDate")]
        public bool HasEndDate 
        { 
            get => _hasEndDate; 
            set => SetProperty(ref _hasEndDate, value); 
        }

        /// <summary>
        /// The end date for the schedule (if HasEndDate is true).
        /// </summary>
        [JsonPropertyName("endDate")]
        public DateTime EndDate 
        { 
            get => _endDate; 
            set 
            {
                // Ensure DateTime is always treated as Local time for Task Scheduler compatibility
                var localValue = value.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(value, DateTimeKind.Local)
                    : value.ToLocalTime();
                SetProperty(ref _endDate, localValue);
            }
        }

        /// <summary>
        /// Gets a human-readable description of the schedule configuration.
        /// </summary>
        [JsonIgnore]
        public string Description
        {
            get
            {
                if (IsOneTime)
                {
                    return $"One-time execution at {InitialDateTime:g}";
                }

                var intervalText = IntervalValue == 1 
                    ? IntervalType.ToString().ToLower().TrimEnd('s')
                    : $"{IntervalValue} {IntervalType.ToString().ToLower()}";

                var description = $"Every {intervalText}, starting {InitialDateTime:g}";
                
                if (HasEndDate)
                {
                    description += $", ending {EndDate:g}";
                }

                return description;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Also notify that Description might have changed
            if (propertyName != nameof(Description))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
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

        /// <summary>
        /// Validates the schedule configuration to ensure it's properly set up.
        /// </summary>
        /// <returns>True if the schedule configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            // For one-time tasks, allow past dates only if they're more than a day old
            // For today or future dates, they're always valid
            if (IsOneTime)
            {
                // Allow any future date or time
                if (InitialDateTime >= DateTime.Now.AddMinutes(-5)) // 5 minute tolerance
                    return true;
                    
                // For past dates, only allow if they're intentionally old (more than 1 day)
                return InitialDateTime < DateTime.Now.AddDays(-1);
            }
            else
            {
                // For recurring tasks, be more lenient - allow today's date even if time has passed
                // because the next execution will be calculated correctly
                if (InitialDateTime.Date >= DateTime.Now.Date)
                    return IntervalValue >= 1;
                    
                // For past dates, still require valid interval
                return IntervalValue >= 1;
            }
        }
    }


} 