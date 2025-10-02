using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetraScan.Hardware.Interfaces;

/// <summary>
/// Interface for camera control and frame acquisition
/// </summary>
public interface ICamera : IDisposable
{
    /// <summary>
    /// Fired when a new frame is available
    /// </summary>
    event EventHandler<FrameReceivedEventArgs>? FrameReceived;

    /// <summary>
    /// Fired when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Fired when a frame is lost
    /// </summary>
    event EventHandler<FrameLostEventArgs>? FrameLost;

    /// <summary>
    /// Indicates if camera is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Indicates if camera is currently grabbing frames
    /// </summary>
    bool IsGrabbing { get; }

    /// <summary>
    /// Gets the camera model name
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Initializes camera with configuration file
    /// </summary>
    Task<bool> InitializeAsync(string configFilePath);

    /// <summary>
    /// Starts continuous frame acquisition
    /// </summary>
    Task<bool> StartGrabbingAsync();

    /// <summary>
    /// Stops frame acquisition
    /// </summary>
    Task<bool> StopGrabbingAsync();

    /// <summary>
    /// Gets frame width in pixels
    /// </summary>
    int GetWidth();

    /// <summary>
    /// Gets frame height in lines
    /// </summary>
    int GetHeight();

    /// <summary>
    /// Gets bytes per pixel
    /// </summary>
    int GetBytesPerPixel();

    /// <summary>
    /// Gets total frames acquired
    /// </summary>
    long GetTotalFramesAcquired();

    /// <summary>
    /// Gets total frames lost
    /// </summary>
    long GetTotalFramesLost();
}

/// <summary>
/// Event arguments for frame received event
/// </summary>
public class FrameReceivedEventArgs : EventArgs
{
    public required IntPtr DataPointer { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FrameNumber { get; init; }
    public required DateTime Timestamp { get; init; }
    public int BitsPerPixel { get; init; }
}

/// <summary>
/// Event arguments for frame lost event
/// </summary>
public class FrameLostEventArgs : EventArgs
{
    public required long FrameNumber { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? Reason { get; init; }
}
