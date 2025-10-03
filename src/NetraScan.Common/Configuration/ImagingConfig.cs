using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetraScan.Common.Configuration;
/// <summary>
/// public class ImagingConfig
/// </summary>
public class ImagingConfig
{
    public int BScansPerVolume { get; set; } = 512;
    /// <summary>
    /// Number of averages per B-scan for noise reduction
    /// </summary>
    public int AveragesPerBScan { get; set; } = 1;

    /// <summary>
    /// Enable real-time display during acquisition
    /// </summary>
    public bool EnableRealTimeDisplay { get; set; } = true;

    /// <summary>
    /// Display update rate in Hz (frames per second)
    /// </summary>
    public int DisplayUpdateRate { get; set; } = 30;

    /// <summary>
    /// Enable automatic saving of acquired data
    /// </summary>
    public bool EnableAutoSave { get; set; } = false;

    /// <summary>
    /// Default save directory for acquired data
    /// </summary>
    public string SaveDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\NetraScan\\Data";

    /// <summary>
    /// File format for saving (e.g., "raw", "tiff", "mat")
    /// </summary>
    public string SaveFormat { get; set; } = "raw";

    /// <summary>
    /// Apply dispersion compensation
    /// </summary>
    public bool ApplyDispersionCompensation { get; set; } = true;

    /// <summary>
    /// Dispersion compensation coefficients
    /// </summary>
    public double[] DispersionCoefficients { get; set; } = new double[] { 0, 0, 0 };

    /// <summary>
    /// Enable log scale display
    /// </summary>
    public bool UseLogScale { get; set; } = true;

    /// <summary>
    /// Display dynamic range in dB
    /// </summary>
    public double DynamicRangeDB { get; set; } = 50.0;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (BScansPerVolume <= 0)
            errors.Add("B-scans per volume must be positive");

        if (AveragesPerBScan < 1)
            errors.Add("Averages per B-scan must be at least 1");

        if (DisplayUpdateRate <= 0 || DisplayUpdateRate > 120)
            errors.Add("Display update rate must be between 1 and 120 Hz");

        if (EnableAutoSave && string.IsNullOrWhiteSpace(SaveDirectory))
            errors.Add("Save directory cannot be empty when auto-save is enabled");

        if (DynamicRangeDB <= 0)
            errors.Add("Dynamic range must be positive");

        return errors.Count == 0;
    }
}


