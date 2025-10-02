using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetraScan.Hardware.Interfaces;

/// <summary>
/// Interface for GPU-accelerated OCT processing
/// </summary>
public interface IGpuProcessor : IDisposable
{
    /// <summary>
    /// Fired when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Indicates if GPU is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets CUDA device ID
    /// </summary>
    int DeviceId { get; }

    /// <summary>
    /// Gets GPU device name
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Gets compute capability (e.g., "8.9" for RTX 5070)
    /// </summary>
    string ComputeCapability { get; }

    /// <summary>
    /// Initializes GPU and loads CUDA kernels
    /// </summary>
    Task<bool> InitializeAsync(int deviceId, string kernelPath);

    /// <summary>
    /// Processes raw spectral data to produce OCT image
    /// </summary>
    Task<ProcessedFrame> ProcessFrameAsync(RawFrame rawFrame);

    /// <summary>
    /// Gets current GPU memory usage
    /// </summary>
    void GetMemoryInfo(out long freeBytes, out long totalBytes);

    /// <summary>
    /// Gets processing statistics
    /// </summary>
    ProcessingStats GetStatistics();

    /// <summary>
    /// Resets processing statistics
    /// </summary>
    void ResetStatistics();
}

/// <summary>
/// Raw frame data from camera
/// </summary>
public class RawFrame
{
    public required IntPtr DataPointer { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FrameNumber { get; init; }
    public int BitsPerPixel { get; init; } = 12;
    public DateTime AcquisitionTime { get; init; } = DateTime.Now;
}

/// <summary>
/// Processed OCT frame data
/// </summary>
public class ProcessedFrame
{
    public required float[] ImageData { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FrameNumber { get; init; }
    public double ProcessingTimeMs { get; init; }
    public DateTime ProcessedTime { get; init; } = DateTime.Now;
}

/// <summary>
/// GPU processing statistics
/// </summary>
public class ProcessingStats
{
    public long TotalFramesProcessed { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public double MinProcessingTimeMs { get; set; }
    public double MaxProcessingTimeMs { get; set; }
    public long TotalProcessingTimeMs { get; set; }
    public double FramesPerSecond { get; set; }
}
