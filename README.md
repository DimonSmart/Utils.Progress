### DimonSmart.Progress

**Advanced Progress Indicator for .NET Applications**

[![NuGet](https://img.shields.io/nuget/v/DimonSmart.Utils.Progress.svg)](https://www.nuget.org/packages/DimonSmart.Utils.Progress/)
[![License](https://img.shields.io/badge/License-0BSD-blue.svg)](https://opensource.org/licenses/0BSD)

## Overview

DimonSmart.Progress is a .NET library providing an advanced progress indicator that helps track, measure, and estimate completion times for long-running operations. Unlike simple progress bars, this indicator can measure individual subtasks and provide accurate time estimates based on actual performance.

## Features

- **Progress Tracking**: Monitor the progress of long-running loops with accurate item counting
- **Subtask Measurement**: Measure and analyze the time taken by different components of your process
- **Estimated Completion Time**: Get real-time predictions of when the entire operation will finish
- **Performance Analysis**: Calculate average times for individual subtasks and iterations

## Installation

```
dotnet add package DimonSmart.Utils.Progress
```

## Usage

### Basic Example

```csharp
// Initialize with the total number of items to process
using var progress = new AdvancedProgressIndicator(totalItems: 1000);

// Process items in a loop
for (int i = 0; i < 1000; i++)
{
    // Measure a specific subtask
    using (progress.BeginSubTask("DataProcessing"))
    {
        // Perform your operation here
        ProcessData(data);
    }
    
    // Optionally measure another subtask in the same iteration
    using (progress.BeginSubTask("FileSaving"))
    {
        // Perform another operation
        SaveResults(result);
    }
    
    // Update the progress counter after completing an iteration
    progress.Update();
    
    // Optionally display progress information
    Console.WriteLine($"Processed: {i+1}/1000");
    Console.WriteLine($"Estimated completion: {progress.EstimatedEndTime}");
    Console.WriteLine($"Average time per item: {progress.AverageItemTime}");
}
```

### Getting Timing Information

You can retrieve timing information for specific subtasks:

```csharp
TimeSpan dataProcessingTime = progress.GetTaskTime("DataProcessing");
TimeSpan fileSavingTime = progress.GetTaskTime("FileSaving");

Console.WriteLine($"Average data processing time: {dataProcessingTime}");
Console.WriteLine($"Average file saving time: {fileSavingTime}");
```

## API Reference

### AdvancedProgressIndicator

The main class providing progress tracking functionality.

#### Constructor

- `AdvancedProgressIndicator(int totalItems)` - Initialize with the total number of items to process

#### Properties

- `ItemsLeft` - Number of items remaining to be processed
- `AverageItemTime` - Average time per iteration
- `EstimatedEndTime` - Projected completion time based on current performance

#### Methods

- `Update()` - Increment the processed items count
- `BeginSubTask(string taskName)` - Start timing a specific subtask (returns IDisposable)
- `GetTaskTime(string taskName)` - Get average execution time for a specific subtask

## License

This project is licensed under the 0BSD License - see the [LICENSE](LICENSE) file for details.

## Repository

[https://github.com/DimonSmart/Progress](https://github.com/DimonSmart/Progress)