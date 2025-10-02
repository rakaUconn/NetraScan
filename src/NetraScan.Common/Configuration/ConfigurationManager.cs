using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace NetraScan.Common.Configuration;

/// <summary>
/// Manages loading and saving of hardware configuration
/// </summary>
public static class ConfigurationManager
{
    private const string ConfigFileName = "hardware_config.json";

    private static readonly string ConfigPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        ConfigFileName
    );

    /// <summary>
    /// Loads configuration from JSON file, creates default if not exists
    /// </summary>
    public static HardwareConfig LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = CreateDefaultConfiguration();
                SaveConfiguration(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<HardwareConfig>(json);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration");
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error loading configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves configuration to JSON file
    /// </summary>
    public static void SaveConfiguration(HardwareConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates default configuration with standard settings
    /// </summary>
    private static HardwareConfig CreateDefaultConfiguration()
    {
        return new HardwareConfig
        {
            Camera = new CameraConfig
            {
                Model = "Xtium2-CL_MX4_1",
                ConfigFilePath = @"C:\Users\Public\Documents\Sapera\CamFiles\User\camera_config.ccf",
                ServerIndex = 0,
                ResourceIndex = 0,
                BufferCount = 8,
                LineRate = 147000,
                PixelsPerLine = 2048,
                LinesPerBScan = 512,
                BitsPerPixel = 12,
                EnableFrameLostDetection = true
            },
            Galvo = new GalvoConfig
            {
                DeviceName = "Dev3",
                XGalvoChannel = "ao0",
                YGalvoChannel = "ao1",
                TriggerCounterChannel = "ctr0",
                ExternalClockInput = "PFI0",
                XVoltageMin = -10.0,
                XVoltageMax = 10.0,
                YVoltageMin = -10.0,
                YVoltageMax = 10.0,
                ScanAmplitude = 5.0,
                ScanOffset = 0.0,
                UseExternalClock = true,
                SampleRate = 100000.0
            },
            Gpu = new GpuConfig
            {
                Model = "RTX5070",
                DeviceId = 0,
                CudaVersion = 12,
                KernelPath = "cuda\\compiled",
                UsePinnedMemory = true,
                EnableTiming = true,
                MaxMemoryUsageMB = 0,
                StreamCount = 2
            },
            Imaging = new ImagingConfig
            {
                BScansPerVolume = 512,
                AveragesPerBScan = 1,
                EnableRealTimeDisplay = true,
                DisplayUpdateRate = 30,
                EnableAutoSave = false,
                SaveDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "NetraScan", "Data"
                ),
                SaveFormat = "raw",
                ApplyDispersionCompensation = true,
                DispersionCoefficients = new double[] { 0, 0, 0 },
                UseLogScale = true,
                DynamicRangeDB = 50.0
            }
        };
    }

    /// <summary>
    /// Gets the full path to the configuration file
    /// </summary>
    public static string GetConfigFilePath() => ConfigPath;

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    public static bool ConfigFileExists() => File.Exists(ConfigPath);
}

