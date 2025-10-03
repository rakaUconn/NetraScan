using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetraScan.Hardware.Interfaces;
using DALSA.SaperaLT.SapClassBasic;

namespace NetraScan.Hardware.Camera;

/// <summary>
/// Sapera LT implementation for Xtium2-CL_MX4_1 camera control
/// Handles frame acquisition, ring buffer management, and frame lost detection
/// </summary>
public class SaperaCamera : ICamera
{
    private readonly ILogger<SaperaCamera> _logger;

    // Sapera objects
    private SapAcqDevice? _acqDevice;
    private SapBuffer? _buffers;
    private SapTransfer? _transfer;

    // Statistics
    private long _totalFramesAcquired;
    private long _totalFramesLost;

    // State
    private bool _isGrabbing;
    private bool _isInitialized;

    // Configuration
    private int _width;
    private int _height;
    private int _bytesPerPixel;
    private int _bufferCount = 8;

    // Events
    public event EventHandler<FrameReceivedEventArgs>? FrameReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<FrameLostEventArgs>? FrameLost;

    // Properties
    public bool IsInitialized => _isInitialized;
    public bool IsGrabbing => _isGrabbing;
    public string ModelName { get; private set; } = "Xtium2-CL_MX4_1";

    public SaperaCamera(ILogger<SaperaCamera> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes camera with configuration file
    /// </summary>
    /// <param name="configFilePath">Path to .ccf camera configuration file</param>
    /// <returns>True if initialization successful</returns>

    public async System.Threading.Tasks.Task<bool> InitializeAsync(string configFilePath)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Initializing Sapera camera");
                _logger.LogInformation("Config file: {ConfigPath}", configFilePath);

                // Check if already initialized
                if (_isInitialized)
                {
                    _logger.LogWarning("Camera already initialized");
                    return true;
                }

                // Cleanup any previous resources
                CleanupResources();

                // Validate config file exists
                if (!File.Exists(configFilePath))
                {
                    _logger.LogError("Camera config file not found: {Path}", configFilePath);
                    ErrorOccurred?.Invoke(this, $"Config file not found: {configFilePath}");
                    return false;
                }

                // Find available servers and resources
                int serverCount = SapManager.GetServerCount();
                _logger.LogInformation("Found {ServerCount} servers", serverCount);
                
                // Log all available servers
                for (int i = 0; i < serverCount; i++)
                {
                    string serverName = SapManager.GetServerName(i);
                    _logger.LogInformation("Server {Index}: {Name}", i, serverName);
                }
                
                if (serverCount == 0)
                {
                    _logger.LogError("No Sapera servers found");
                    ErrorOccurred?.Invoke(this, "No Sapera servers found");
                    return false;
                }

                // Check if CamExpert or other camera applications are running
                CheckForCameraApplications();

                // Try to find a working server/resource combination
                bool deviceCreated = false;
                for (int serverIndex = 0; serverIndex < serverCount && !deviceCreated; serverIndex++)
                {
                    string serverName = SapManager.GetServerName(serverIndex);
                    
                    _logger.LogInformation("Trying server {Index}: {Name}", 
                        serverIndex, serverName);
                    
                    // Try different resource indices (typically 0-3 for most cameras)
                    for (int resourceIndex = 0; resourceIndex < 4 && !deviceCreated; resourceIndex++)
                    {
                        _logger.LogInformation("Checking resource {ResourceIndex} on server {ServerName}",
                            resourceIndex, serverName);
                        
                        try
                        {
                            SapLocation location = new SapLocation(serverName, resourceIndex);
                            _logger.LogInformation("Created location for server {ServerName}, resource {ResourceIndex}",
                                serverName, resourceIndex);
                            
                            // Create acquisition device with location and config file
                            _acqDevice = new SapAcqDevice(location, configFilePath);
                            _logger.LogInformation("Created SapAcqDevice object for server {ServerName}, resource {ResourceIndex}",
                                serverName, resourceIndex);
                            
                            _logger.LogInformation("Attempting to create device on server {ServerName}, resource {ResourceIndex}",
                                serverName, resourceIndex);
                            
                            if (_acqDevice.Create())
                            {
                                deviceCreated = true;
                                _logger.LogInformation("Successfully created device on server {ServerName}, resource {ResourceIndex}",
                                    serverName, resourceIndex);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create device on server {ServerName}, resource {ResourceIndex}",
                                    serverName, resourceIndex);
                                _acqDevice?.Destroy();
                                _acqDevice = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Exception creating device on server {ServerName}, resource {ResourceIndex}: {Message}\nStack trace: {StackTrace}",
                                serverName, resourceIndex, ex.Message, ex.StackTrace);
                            _acqDevice?.Destroy();
                            _acqDevice = null;
                        }
                    }
                }
                
                if (!deviceCreated || _acqDevice == null)
                {
                    _logger.LogError("Failed to create acquisition device on any available server/resource");
                    _logger.LogError("Possible causes:");
                    _logger.LogError("  1. Camera is in use by another application (e.g., CamExpert)");
                    _logger.LogError("  2. Camera driver or hardware issue");
                    _logger.LogError("  3. Incorrect configuration file");
                    _logger.LogError("  4. Insufficient permissions");
                    ErrorOccurred?.Invoke(this, "Failed to create acquisition device");
                    return false;
                }

                // Get camera parameters
                _width = _acqDevice.XferParams.Width;
                _height = _acqDevice.XferParams.Height;

                // Determine bytes per pixel based on format
                SapFormat format = _acqDevice.XferParams.Format;
                _bytesPerPixel = format switch
                {
                    SapFormat.Uint8 => 1,
                    SapFormat.Uint10 => 2,
                    SapFormat.Uint12 => 2,
                    SapFormat.Uint16 => 2,
                    _ => 2
                };

                _logger.LogInformation("Camera parameters: {Width}x{Height}, {Bpp} bytes/pixel, Format: {Format}",
                    _width, _height, _bytesPerPixel, format);

                // Create ring buffer - CORRECT CONSTRUCTOR
                _buffers = new SapBufferWithTrash(_bufferCount, _acqDevice, SapBuffer.MemoryType.ScatterGather);

                if (!_buffers.Create())
                {
                    _logger.LogError("Failed to create buffers");
                    ErrorOccurred?.Invoke(this, "Failed to create ring buffer");
                    CleanupResources();
                    return false;
                }

                _logger.LogInformation("Created ring buffer with {Count} frames", _bufferCount);

                // Create transfer object
                _transfer = new SapAcqDeviceToBuf(_acqDevice, _buffers);

                // Register callback for frame notifications
                _transfer.XferNotify += OnFrameReceived;
                _transfer.XferNotifyContext = this;

                if (!_transfer.Create())
                {
                    _logger.LogError("Failed to create transfer object");
                    ErrorOccurred?.Invoke(this, "Failed to create transfer object");
                    CleanupResources();
                    return false;
                }

                _isInitialized = true;
                _logger.LogInformation("Camera initialization complete");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during camera initialization");
                ErrorOccurred?.Invoke(this, $"Initialization error: {ex.Message}");
                CleanupResources();
                return false;
            }
        });
    }
  
    /// <summary>
    /// Starts continuous frame acquisition
    /// </summary>
    public async System.Threading.Tasks.Task<bool> StartGrabbingAsync()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogError("Cannot start grabbing: camera not initialized");
                    return false;
                }

                if (_isGrabbing)
                {
                    _logger.LogWarning("Camera is already grabbing");
                    return true;
                }

                if (_transfer == null)
                {
                    _logger.LogError("Transfer object is null");
                    return false;
                }

                // Reset statistics
                _totalFramesAcquired = 0;
                _totalFramesLost = 0;

                // Start continuous grab
                if (!_transfer.Grab())
                {
                    _logger.LogError("Failed to start grabbing");
                    ErrorOccurred?.Invoke(this, "Failed to start frame acquisition");
                    return false;
                }

                _isGrabbing = true;
                _logger.LogInformation("Started continuous frame acquisition");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while starting grab");
                ErrorOccurred?.Invoke(this, $"Start grab error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Stops frame acquisition
    /// </summary>
    public async System.Threading.Tasks.Task<bool> StopGrabbingAsync()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (!_isGrabbing)
                {
                    _logger.LogInformation("Camera is not grabbing");
                    return true;
                }

                if (_transfer == null)
                {
                    return true;
                }

                // Freeze the transfer
                _transfer.Freeze();

                // Wait for transfer to complete (5 second timeout)
                if (!_transfer.Wait(5000))
                {
                    _logger.LogWarning("Transfer wait timeout - aborting");
                    _transfer.Abort();
                }

                _isGrabbing = false;

                _logger.LogInformation("Stopped frame acquisition");
                _logger.LogInformation("Total frames acquired: {Acquired}, lost: {Lost}",
                    _totalFramesAcquired, _totalFramesLost);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while stopping grab");
                ErrorOccurred?.Invoke(this, $"Stop grab error: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Callback for frame received events
    /// </summary>
    private void OnFrameReceived(object sender, SapXferNotifyEventArgs args)
    {
        try
        {
            if (_buffers == null)
            {
                return;
            }

            // Check if frame is trash (lost/corrupted)
            if (args.Trash)
            {
                _totalFramesLost++;

                _logger.LogWarning("Frame lost detected (total: {Count})", _totalFramesLost);

                FrameLost?.Invoke(this, new FrameLostEventArgs
                {
                    FrameNumber = _totalFramesAcquired,
                    Timestamp = DateTime.Now,
                    Reason = "Buffer overflow - ring buffer full"
                });

                return;
            }

            // Get pointer to frame data
            IntPtr dataPtr = IntPtr.Zero;
            if (!_buffers.GetAddress(out dataPtr))
            {
                _logger.LogError("Failed to get buffer address");
                return;
            }

            // Raise frame received event
            FrameReceived?.Invoke(this, new FrameReceivedEventArgs
            {
                DataPointer = dataPtr,
                Width = _width,
                Height = _height,
                FrameNumber = _totalFramesAcquired++,
                Timestamp = DateTime.Now,
                BitsPerPixel = _bytesPerPixel * 8
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in frame callback");
            ErrorOccurred?.Invoke(this, $"Frame callback error: {ex.Message}");
        }
    }

    // Property getters
    public int GetWidth() => _width;
    public int GetHeight() => _height;
    public int GetBytesPerPixel() => _bytesPerPixel;
    public long GetTotalFramesAcquired() => _totalFramesAcquired;
    public long GetTotalFramesLost() => _totalFramesLost;

    /// <summary>
    /// Cleanup Sapera resources
    /// </summary>
    private void CleanupResources()
    {
        try
        {
            if (_transfer != null)
            {
                _transfer.XferNotify -= OnFrameReceived;
                _transfer.Destroy();
                _transfer.Dispose();
                _transfer = null;
            }

            if (_buffers != null)
            {
                _buffers.Destroy();
                _buffers.Dispose();
                _buffers = null;
            }

            if (_acqDevice != null)
            {
                _acqDevice.Destroy();
                _acqDevice.Dispose();
                _acqDevice = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource cleanup");
        }
    }

    private void CheckForCameraApplications()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcesses();
            var cameraApps = processes.Where(p => 
            {
                try
                {
                    return p.ProcessName.ToLower().Contains("camexpert") ||
                           p.ProcessName.ToLower().Contains("sapera") ||
                           p.ProcessName.ToLower().Contains("camera") ||
                           p.ProcessName.ToLower().Contains("xtium");
                }
                catch
                {
                    return false;
                }
            }).ToList();

            if (cameraApps.Any())
            {
                _logger.LogWarning("Found {Count} camera-related applications running:", cameraApps.Count);
                foreach (var app in cameraApps)
                {
                    try
                    {
                        _logger.LogWarning("  - {ProcessName} (PID: {Id})", app.ProcessName, app.Id);
                    }
                    catch
                    {
                        // Process might have exited
                    }
                }
                _logger.LogWarning("These applications might be using camera resources. Consider closing them if camera initialization fails.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking for camera applications: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing camera");

        if (_isGrabbing)
        {
            StopGrabbingAsync().Wait();
        }

        CleanupResources();

        _isInitialized = false;

        _logger.LogInformation("Camera disposed");
    }
}
