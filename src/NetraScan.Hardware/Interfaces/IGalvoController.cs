using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetraScan.Hardware.Interfaces;

/// <summary>
/// Interface for galvanometer scanner control
/// </summary>
public interface IGalvoController : IDisposable
{
    /// <summary>
    /// Fired when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Indicates if galvo is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Indicates if galvo is currently scanning
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Gets the DAQ device name
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Initializes galvo controller with device name
    /// </summary>
    Task<bool> InitializeAsync(string deviceName);

    /// <summary>
    /// Configures analog output channels for X and Y galvos
    /// </summary>
    Task<bool> ConfigureChannelsAsync(
        string xChannel,
        string yChannel,
        double xMin,
        double xMax,
        double yMin,
        double yMax);

    /// <summary>
    /// Configures trigger output for camera synchronization
    /// </summary>
    Task<bool> ConfigureTriggerAsync(
        string triggerChannel,
        string clockInput,
        double frequency);

    /// <summary>
    /// Sets the scan pattern (voltages for X and Y)
    /// </summary>
    Task<bool> SetScanPatternAsync(ScanPattern pattern);

    /// <summary>
    /// Starts scanning with configured pattern
    /// </summary>
    Task<bool> StartScanningAsync();

    /// <summary>
    /// Stops scanning
    /// </summary>
    Task<bool> StopScanningAsync();

    /// <summary>
    /// Resets galvos to center position
    /// </summary>
    Task<bool> ResetToCenter();
}

/// <summary>
/// Scan pattern definition for galvo movement
/// </summary>
public class ScanPattern
{
    public required double[] XVoltages { get; init; }
    public required double[] YVoltages { get; init; }
    public required double SampleRate { get; init; }
    public required int SamplesPerLine { get; init; }
    public required int NumberOfLines { get; init; }
    public ScanType Type { get; init; } = ScanType.BScan;
}

/// <summary>
/// Type of scan pattern
/// </summary>
public enum ScanType
{
    BScan,      // 2D cross-sectional scan
    CScan,      // 3D volumetric scan
    Point,      // Single point
    Custom      // User-defined pattern
}
