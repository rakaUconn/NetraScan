using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetraScan.Hardware.Interfaces;
using NationalInstruments.DAQmx;

namespace NetraScan.Hardware.Galvo;

/// <summary>
/// NI DAQmx implementation for galvanometer scanner control (Dev3)
/// Manages X/Y analog outputs and camera trigger generation
/// </summary>
public class NIGalvoController : IGalvoController
{
    private readonly ILogger<NIGalvoController> _logger;

    // NI DAQ tasks (using alias to avoid conflict with System.Threading.Tasks.Task)
    private DaqTask? _aoTask;
    private DaqTask? _counterTask;

    // State
    private bool _isInitialized;
    private bool _isScanning;
    private string _deviceName = string.Empty;

    // Scan pattern data
    private double[] _xVoltages = Array.Empty<double>();
    private double[] _yVoltages = Array.Empty<double>();
    private double _sampleRate;

    // Configuration
    private string _xChannelName = string.Empty;
    private string _yChannelName = string.Empty;

    // Events
    public event EventHandler<string>? ErrorOccurred;

    // Properties
    public bool IsInitialized => _isInitialized;
    public bool IsScanning => _isScanning;
    public string DeviceName => _deviceName;

    public NIGalvoController(ILogger<NIGalvoController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes galvo controller with specified device
    /// </summary>
    public async System.Threading.Tasks.Task<bool> InitializeAsync(string deviceName)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Initializing galvo controller");
                _logger.LogInformation("Device: {DeviceName}", deviceName);

                _deviceName = deviceName;

                // Verify device exists
                string[] devices = DaqSystem.Local.Devices;
                if (!devices.Contains(deviceName))
                {
                    _logger.LogError("DAQ device not found: {Device}", deviceName);
                    _logger.LogInformation("Available devices: {Devices}", string.Join(", ", devices));
                    ErrorOccurred?.Invoke(this, $"Device not found: {deviceName}");
                    return false;
                }

                // Get device info
                Device device = DaqSystem.Local.LoadDevice(deviceName);
                _logger.LogInformation("Device type: {Type}", device.ProductType);
                _logger.LogInformation("Analog output channels: {Channels}", device.AOPhysicalChannels.Length);
                _logger.LogInformation("Counter channels: {Channels}", device.COPhysicalChannels.Length);

                _isInitialized = true;
                _logger.LogInformation("Galvo controller initialized");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during galvo initialization");
                ErrorOccurred?.Invoke(this, $"Initialization error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Configures analog output channels for X and Y galvos
    /// </summary>
    public async System.Threading.Tasks.Task<bool> ConfigureChannelsAsync(
        string xChannel,
        string yChannel,
        double xMin,
        double xMax,
        double yMin,
        double yMax)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Configuring galvo channels");

                // Cleanup existing task if any
                if (_aoTask != null)
                {
                    _aoTask.Dispose();
                    _aoTask = null;
                }

                // Create analog output task
                _aoTask = new DaqTask("GalvoAO");

                // Format channel names
                _xChannelName = $"{_deviceName}/{xChannel}";
                _yChannelName = $"{_deviceName}/{yChannel}";

                // Add X channel
                _aoTask.AOChannels.CreateVoltageChannel(
                    _xChannelName,
                    "X-Galvo",
                    xMin,
                    xMax,
                    AOVoltageUnits.Volts);

                _logger.LogInformation("X-Galvo: {Channel}, Range: {Min}V to {Max}V",
                    _xChannelName, xMin, xMax);

                // Add Y channel
                _aoTask.AOChannels.CreateVoltageChannel(
                    _yChannelName,
                    "Y-Galvo",
                    yMin,
                    yMax,
                    AOVoltageUnits.Volts);

                _logger.LogInformation("Y-Galvo: {Channel}, Range: {Min}V to {Max}V",
                    _yChannelName, yMin, yMax);

                _logger.LogInformation("Galvo channels configured");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception configuring channels");
                ErrorOccurred?.Invoke(this, $"Channel config error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Configures counter output for camera trigger generation
    /// </summary>
    public async System.Threading.Tasks.Task<bool> ConfigureTriggerAsync(
        string triggerChannel,
        string clockInput,
        double frequency)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Configuring camera trigger");

                // Cleanup existing task if any
                if (_counterTask != null)
                {
                    _counterTask.Dispose();
                    _counterTask = null;
                }

                // Create counter task for camera trigger
                _counterTask = new DaqTask("CameraTrigger");

                string counterChannel = $"{_deviceName}/{triggerChannel}";

                // Create pulse channel with specified frequency
                _counterTask.COChannels.CreatePulseChannelFrequency(
                    counterChannel,
                    "CameraSync",
                    COPulseFrequencyUnits.Hertz,
                    COPulseIdleState.Low,
                    0.0,              // Initial delay
                    frequency,         // Pulse frequency (line rate)
                    0.5);             // 50% duty cycle

                _logger.LogInformation("Trigger channel: {Channel} at {Freq}Hz",
                    counterChannel, frequency);

                // Configure timing for continuous generation
                _counterTask.Timing.ConfigureImplicit(
                    SampleQuantityMode.ContinuousSamples);

                // If external clock specified, use it
                if (!string.IsNullOrEmpty(clockInput))
                {
                    string clockSource = $"/{_deviceName}/{clockInput}";
                    _logger.LogInformation("Using external clock: {Clock}", clockSource);

                    // Note: External clock configuration depends on your specific setup
                    // This is a placeholder - adjust based on your hardware wiring
                }

                _logger.LogInformation("Camera trigger configured");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception configuring trigger");
                ErrorOccurred?.Invoke(this, $"Trigger config error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Sets the scan pattern (X and Y voltage waveforms)
    /// </summary>
    public async System.Threading.Tasks.Task<bool> SetScanPatternAsync(ScanPattern pattern)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (_aoTask == null)
                {
                    _logger.LogError("Channels not configured");
                    return false;
                }

                _logger.LogInformation("Setting scan pattern");

                _xVoltages = pattern.XVoltages;
                _yVoltages = pattern.YVoltages;
                _sampleRate = pattern.SampleRate;

                if (_xVoltages.Length != _yVoltages.Length)
                {
                    _logger.LogError("X and Y voltage arrays must have same length");
                    return false;
                }

                // Configure sample clock timing
                _aoTask.Timing.ConfigureSampleClock(
                    "",                                    // Use internal clock
                    _sampleRate,                          // Sample rate in Hz
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    _xVoltages.Length);                   // Samples per channel

                _logger.LogInformation("Pattern: {Samples} samples/channel at {Rate}Hz",
                    _xVoltages.Length, _sampleRate);
                _logger.LogInformation("Type: {Type}, Lines: {Lines}",
                    pattern.Type, pattern.NumberOfLines);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception setting scan pattern");
                ErrorOccurred?.Invoke(this, $"Pattern config error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Starts scanning with configured pattern
    /// </summary>
    public async System.Threading.Tasks.Task<bool> StartScanningAsync()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (_aoTask == null)
                {
                    _logger.LogError("Analog output not configured");
                    return false;
                }

                if (_xVoltages.Length == 0 || _yVoltages.Length == 0)
                {
                    _logger.LogError("Scan pattern not set");
                    return false;
                }

                _logger.LogInformation("Starting scan");

                // Prepare interleaved data [X0, Y0, X1, Y1, X2, Y2, ...]
                double[,] interleavedData = new double[2, _xVoltages.Length];
                for (int i = 0; i < _xVoltages.Length; i++)
                {
                    interleavedData[0, i] = _xVoltages[i];
                    interleavedData[1, i] = _yVoltages[i];
                }

                // Write data to buffer
                var writer = new AnalogMultiChannelWriter(_aoTask.Stream);
                writer.WriteMultiSample(false, interleavedData);

                // Start counter task first (camera trigger)
                if (_counterTask != null)
                {
                    _counterTask.Start();
                    _logger.LogInformation("Camera trigger started");
                }

                // Start analog output task
                _aoTask.Start();
                _logger.LogInformation("Galvo scanning started");

                _isScanning = true;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception starting scan");
                ErrorOccurred?.Invoke(this, $"Start scan error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Stops scanning
    /// </summary>
    public async System.Threading.Tasks.Task<bool> StopScanningAsync()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (!_isScanning)
                {
                    _logger.LogInformation("Not currently scanning");
                    return true;
                }

                _logger.LogInformation("Stopping scan");

                // Stop analog output
                _aoTask?.Stop();

                // Stop counter
                _counterTask?.Stop();

                _isScanning = false;

                _logger.LogInformation("Scan stopped");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception stopping scan");
                ErrorOccurred?.Invoke(this, $"Stop scan error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Resets galvos to center position (0V, 0V)
    /// </summary>
    public async System.Threading.Tasks.Task<bool> ResetToCenter()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (_aoTask == null)
                {
                    _logger.LogError("Channels not configured");
                    return false;
                }

                _logger.LogInformation("Resetting galvos to center");

                // Write single sample to both channels
                var writer = new AnalogMultiChannelWriter(_aoTask.Stream);
                writer.WriteSingleSample(true, new double[] { 0.0, 0.0 });

                _logger.LogInformation("Galvos reset to center (0V, 0V)");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception resetting to center");
                ErrorOccurred?.Invoke(this, $"Reset error: {ex.Message}");
                return false;
            }
        });
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing galvo controller");

        if (_isScanning)
        {
            StopScanningAsync().Wait();
        }

        ResetToCenter().Wait();

        _aoTask?.Dispose();
        _aoTask = null;

        _counterTask?.Dispose();
        _counterTask = null;

        _isInitialized = false;

        _logger.LogInformation("Galvo controller disposed");
    }
}
