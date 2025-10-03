using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetraScan.Common.Configuration;

/// <summary>
/// Camera configuration for Sapera LT SDK (Xtium2-CL_MX4_1)
/// </summary>
public class CameraConfig
{
    public string Model { get; set; } = "Xtium2-CL_MX4_1";
    public string ConfigFilePath { get; set; } = string.Empty;
    public int ServerIndex { get; set; } = 0;
    public int ResourceIndex { get; set; } = 0;
    public int BufferCount { get; set; } = 8;
    public int LineRate { get; set; } = 250000;
    public int PixelsPerLine { get; set; } = 2048;
    public int LinesPerBScan { get; set; } = 512;
    public int BitsPerPixel { get; set; } = 10;
    public bool EnableFrameLostDetection { get; set; } = true;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Model))
            errors.Add("Camera model cannot be empty");

        if (string.IsNullOrWhiteSpace(ConfigFilePath))
            errors.Add("Camera config file path cannot be empty");
        else if (!string.IsNullOrEmpty(ConfigFilePath) && !File.Exists(ConfigFilePath))
            errors.Add($"Camera config file not found: {ConfigFilePath}");

        if (ServerIndex < 0)
            errors.Add("Server index must be >= 0");

        if (ResourceIndex < 0)
            errors.Add("Resource index must be >= 0");

        if (BufferCount < 2)
            errors.Add("Buffer count must be at least 2");

        if (LineRate <= 0)
            errors.Add("Line rate must be positive");

        if (PixelsPerLine <= 0)
            errors.Add("Pixels per line must be positive");

        if (LinesPerBScan <= 0)
            errors.Add("Lines per B-scan must be positive");

        if (BitsPerPixel != 8 && BitsPerPixel != 10 && BitsPerPixel != 12 &&
            BitsPerPixel != 14 && BitsPerPixel != 16)
            errors.Add("Bits per pixel must be 8, 10, 12, 14, or 16");

        return errors.Count == 0;
    }
}
