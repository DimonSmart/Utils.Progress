using DimonSmart.Utils.Progress;

namespace DimonSmart.Utils.Progress.Tests
{
    public class AdvancedProgressIndicatorTests
    {
        [Fact]
        public void Update_IncrementsProcessedItems()
        {
            var totalItems = 10;
            using var progress = new AdvancedProgressIndicator(totalItems);
            Assert.Equal(totalItems, progress.ItemsLeft + 0);
            progress.Update();
            Assert.Equal(totalItems - 1, progress.ItemsLeft);
        }

        [Fact]
        public void BeginSubTask_RecordsTime()
        {
            using var progress = new AdvancedProgressIndicator(1);
            // Measure a short operation
            using (progress.BeginSubTask("TestTask"))
            {
                Thread.Sleep(50);
            }

            var averageTime = progress.GetTaskTime("TestTask");
            Assert.True(averageTime > TimeSpan.Zero);
        }

        [Fact]
        public void EstimatedEndTime_ReturnsFutureTime()
        {
            using var progress = new AdvancedProgressIndicator(5);
            // Initially should return null as no iterations completed
            Assert.Null(progress.EstimatedEndTime);

            // Perform a couple of iterations with an artificial delay
            for (var i = 0; i < 2; i++)
            {
                using (progress.BeginSubTask("Task"))
                {
                    Thread.Sleep(100);
                }
                progress.Update();
            }

            var estimate = progress.EstimatedEndTime;
            Assert.NotNull(estimate);
            Assert.True(estimate > DateTime.Now);
        }

        [Fact]
        public void MultipleSubTasks_RecordSeparateTimings()
        {
            using var progress = new AdvancedProgressIndicator(1);

            // First sub-task
            using (progress.BeginSubTask("Task1"))
            {
                Thread.Sleep(50);
            }

            // Second sub-task
            using (progress.BeginSubTask("Task2"))
            {
                Thread.Sleep(100);
            }

            var task1Time = progress.GetTaskTime("Task1");
            var task2Time = progress.GetTaskTime("Task2");

            Assert.True(task1Time > TimeSpan.Zero);
            Assert.True(task2Time > TimeSpan.Zero);
            Assert.True(task2Time > task1Time, "Task2 should take longer than Task1");
        }
    }
}
