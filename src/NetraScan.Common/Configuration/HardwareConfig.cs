using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetraScan.Common.Configuration;

/// <summary>
/// Root configuration object for all hardware settings
/// </summary>
public class HardwareConfig
{
    /// <summary>
    /// Camera configuration settings
    /// </summary>
    public CameraConfig Camera { get; set; } = new();

    /// <summary>
    /// Galvanometer scanner configuration
    /// </summary>
    public GalvoConfig Galvo { get; set; } = new();

    /// <summary>
    /// GPU configuration settings
    /// </summary>
    public GpuConfig Gpu { get; set; } = new();

    /// <summary>
    /// Imaging parameters and settings
    /// </summary>
    public ImagingConfig Imaging { get; set; } = new();

    /// <summary>
    /// Validates all configuration settings
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (!Camera.Validate(out var cameraErrors))
            errors.AddRange(cameraErrors);

        if (!Galvo.Validate(out var galvoErrors))
            errors.AddRange(galvoErrors);

        if (!Gpu.Validate(out var gpuErrors))
            errors.AddRange(gpuErrors);

        if (!Imaging.Validate(out var imagingErrors))
            errors.AddRange(imagingErrors);

        return errors.Count == 0;
    }
}
