using System.Diagnostics;

namespace DimonSmart.Utils.Progress
{
    /// <summary>
    /// Advanced progress indicator that times subtasks and calculates the estimated end time.
    /// Uses a sliding window of the most recent iteration durations to compute the effective average iteration time.
    /// </summary>
    public class AdvancedProgressIndicator : IDisposable
    {
        private readonly int _totalItems;
        private int _processedItems;
        private readonly Stopwatch _overallStopwatch;
        private readonly Dictionary<string, SubTaskInfo> _subTasks;

        // Sliding window fields
        private readonly int _windowSize;
        private readonly Queue<long> _iterationTicksQueue;
        private long _lastIterationTicks;

        private class SubTaskInfo
        {
            public TimeSpan TotalTime = TimeSpan.Zero;
            public int Count = 0;
        }

        /// <summary>
        /// Initializes with the specified total number of iterations.
        /// </summary>
        /// <param name="totalItems">Total number of iterations/operations.</param>
        /// <param name="windowSize">Number of iterations to consider for the sliding window. Default value is 10.</param>
        public AdvancedProgressIndicator(int totalItems, int windowSize = 10)
        {
            _totalItems = totalItems;
            _processedItems = 0;
            _overallStopwatch = Stopwatch.StartNew();
            _subTasks = [];

            _windowSize = windowSize;
            _iterationTicksQueue = new Queue<long>();
            _lastIterationTicks = _overallStopwatch.ElapsedTicks;
        }

        /// <summary>
        /// Number of remaining iterations.
        /// </summary>
        public int ItemsLeft => _totalItems - _processedItems;

        /// <summary>
        /// Overall average iteration time computed from the entire elapsed time.
        /// </summary>
        public TimeSpan OverallAverageItemTime
        {
            get
            {
                if (_processedItems == 0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromTicks(_overallStopwatch.Elapsed.Ticks / _processedItems);
            }
        }

        /// <summary>
        /// Average iteration time computed over the sliding window of the most recent iterations.
        /// If the window is not yet filled, the overall average can be used.
        /// </summary>
        public TimeSpan SlidingAverageItemTime
        {
            get
            {
                if (_iterationTicksQueue.Count == 0)
                {
                    return OverallAverageItemTime;
                }

                var sumTicks = _iterationTicksQueue.Sum();
                return TimeSpan.FromTicks(sumTicks / _iterationTicksQueue.Count);
            }
        }

        /// <summary>
        /// Effective average time used for estimation.
        /// If the window contains data, the sliding average is used; otherwise, the overall average is used.
        /// </summary>
        public TimeSpan EffectiveAverageItemTime => _iterationTicksQueue.Count > 0 ? SlidingAverageItemTime : OverallAverageItemTime;

        /// <summary>
        /// Estimated completion time calculated using the effective average iteration time.
        /// </summary>
        public DateTime EstimatedEndTime
        {
            get
            {
                var average = EffectiveAverageItemTime;
                var estimatedTotalDuration = TimeSpan.FromTicks(average.Ticks * _totalItems);
                var remainingTime = estimatedTotalDuration - _overallStopwatch.Elapsed;
                return DateTime.Now + remainingTime;
            }
        }

        /// <summary>
        /// Should be called at the end of each iteration to update the progress.
        /// Records the iteration time in the sliding window.
        /// </summary>
        public void Update()
        {
            var nowTicks = _overallStopwatch.ElapsedTicks;
            var iterationTicks = nowTicks - _lastIterationTicks;
            _lastIterationTicks = nowTicks;

            _iterationTicksQueue.Enqueue(iterationTicks);
            if (_iterationTicksQueue.Count > _windowSize)
            {
                _iterationTicksQueue.Dequeue();
            }

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
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper class for timing a subtask.
        /// </summary>
        private class SubTaskTimer(AdvancedProgressIndicator indicator, string taskName) : IDisposable
        {
            private readonly AdvancedProgressIndicator _indicator = indicator;
            private readonly string _taskName = taskName;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

            public void Dispose()
            {
                _stopwatch.Stop();
                _indicator.AddSubTaskTime(_taskName, _stopwatch.Elapsed);
                GC.SuppressFinalize(this);
            }
        }
    }
}
