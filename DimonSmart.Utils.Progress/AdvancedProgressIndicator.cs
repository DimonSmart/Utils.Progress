using System.Diagnostics;

namespace DimonSmart.Utils.Progress
{
    /// <summary>
    /// Advanced progress indicator that times subtasks and calculates the estimated end time.
    /// The calculation includes both measured and other lengthy operations in the main loop.
    /// </summary>
    public class AdvancedProgressIndicator : IDisposable
    {
        private readonly int _totalItems;
        private int _processedItems;
        private readonly Stopwatch _overallStopwatch;
        private readonly Dictionary<string, SubTaskInfo> _subTasks;

        private class SubTaskInfo
        {
            public TimeSpan TotalTime = TimeSpan.Zero;
            public int Count = 0;
        }

        /// <summary>
        /// Initializes with the specified total number of iterations.
        /// </summary>
        /// <param name="totalItems">Total number of iterations/operations.</param>
        public AdvancedProgressIndicator(int totalItems)
        {
            _totalItems = totalItems;
            _processedItems = 0;
            _overallStopwatch = Stopwatch.StartNew();
            _subTasks = new Dictionary<string, SubTaskInfo>();
        }

        public int ItemsLeft => _totalItems - _processedItems;

        /// <summary>
        /// Average iteration time computed from total elapsed time (including non-measured operations).
        /// </summary>
        public TimeSpan AverageItemTime
        {
            get
            {
                if (_processedItems == 0)
                    return TimeSpan.Zero;
                return TimeSpan.FromTicks(_overallStopwatch.Elapsed.Ticks / _processedItems);
            }
        }

        /// <summary>
        /// Estimated completion time calculated as (average iteration time * total iterations).
        /// Note: Long non-measured operations in the main loop will affect this estimation.
        /// </summary>
        public DateTime EstimatedEndTime
        {
            get
            {
                var estimatedTotalTime = TimeSpan.FromTicks(AverageItemTime.Ticks * _totalItems);
                var remainingTime = estimatedTotalTime - _overallStopwatch.Elapsed;
                return DateTime.Now + remainingTime;
            }
        }

        public void Update()
        {
            _processedItems++;
        }

        /// <summary>
        /// Starts timing for a specific subtask. Use with a using block to ensure proper disposal.
        /// </summary>
        /// <param name="taskName">Unique name for the subtask.</param>
        /// <returns>IDisposable object to end timing.</returns>
        public IDisposable BeginSubTask(string taskName)
        {
            return new SubTaskTimer(this, taskName);
        }

        /// <summary>
        /// Accumulates timing result for a subtask.
        /// </summary>
        internal void AddSubTaskTime(string taskName, TimeSpan elapsed)
        {
            if (!_subTasks.ContainsKey(taskName))
            {
                _subTasks[taskName] = new SubTaskInfo();
            }
            _subTasks[taskName].TotalTime += elapsed;
            _subTasks[taskName].Count++;
        }

        /// <summary>
        /// Returns the average execution time for the specified subtask.
        /// </summary>
        /// <param name="taskName">Name of the subtask</param>
        /// <returns>Average execution time</returns>
        public TimeSpan GetTaskTime(string taskName)
        {
            if (_subTasks.ContainsKey(taskName) && _subTasks[taskName].Count > 0)
            {
                return TimeSpan.FromTicks(_subTasks[taskName].TotalTime.Ticks / _subTasks[taskName].Count);
            }
            return TimeSpan.Zero;
        }

        public void Dispose()
        {
            _overallStopwatch.Stop();
        }

        /// <summary>
        /// Helper class for timing a subtask.
        /// </summary>
        private class SubTaskTimer : IDisposable
        {
            private readonly AdvancedProgressIndicator _indicator;
            private readonly string _taskName;
            private readonly Stopwatch _stopwatch;

            public SubTaskTimer(AdvancedProgressIndicator indicator, string taskName)
            {
                _indicator = indicator;
                _taskName = taskName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _indicator.AddSubTaskTime(_taskName, _stopwatch.Elapsed);
            }
        }
    }
}
