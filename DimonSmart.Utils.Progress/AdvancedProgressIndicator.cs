using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DimonSmart.Utils.Progress
{
    /// <summary>
    /// Advanced progress indicator that times subtasks and calculates the estimated end time.
    /// The calculation includes both measured and other lengthy operations in the main loop.
    /// Uses a sliding window of the most recent iteration durations to compute the average iteration time.
    /// </summary>
    public class AdvancedProgressIndicator : IDisposable
    {
        private readonly int _totalItems;
        private int _processedItems;
        private readonly Stopwatch _overallStopwatch;
        private readonly Dictionary<string, SubTaskInfo> _subTasks;

        // Fields for implementing the sliding window
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
        /// <param name="windowSize">
        /// Number of iterations to consider for the sliding window average.
        /// Default value is 10.
        /// </param>
        public AdvancedProgressIndicator(int totalItems, int windowSize = 10)
        {
            _totalItems = totalItems;
            _processedItems = 0;
            _overallStopwatch = Stopwatch.StartNew();
            _subTasks = new Dictionary<string, SubTaskInfo>();

            // Initialize the sliding window
            _windowSize = windowSize;
            _iterationTicksQueue = new Queue<long>();
            _lastIterationTicks = _overallStopwatch.ElapsedTicks;
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
        /// Average iteration time computed over the sliding window of recent iterations.
        /// </summary>
        public TimeSpan SlidingAverageItemTime
        {
            get
            {
                if (_iterationTicksQueue.Count == 0)
                    return TimeSpan.Zero;
                long sumTicks = _iterationTicksQueue.Sum();
                return TimeSpan.FromTicks(sumTicks / _iterationTicksQueue.Count);
            }
        }

        /// <summary>
        /// Estimated completion time calculated using sliding window average.
        /// Note: Long non-measured operations in the main loop will affect this estimation.
        /// </summary>
        public DateTime EstimatedEndTime
        {
            get
            {
                // Use the sliding window for a more adaptive calculation of average iteration time.
                var average = SlidingAverageItemTime;
                // If there is no data in the sliding window yet, use the overall average.
                if (average == TimeSpan.Zero)
                    average = AverageItemTime;

                // Estimate the total duration of the loop based on the average time and total iterations.
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
            // Calculate the time spent on the current iteration.
            long nowTicks = _overallStopwatch.ElapsedTicks;
            long iterationTicks = nowTicks - _lastIterationTicks;
            _lastIterationTicks = nowTicks;

            // Add the iteration time to the queue (sliding window)
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
