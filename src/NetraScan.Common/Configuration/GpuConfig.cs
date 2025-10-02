using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetraScan.Common.Configuration;

/// <summary>
/// GPU configuration for CUDA processing (RTX 5070)
/// </summary>
public class GpuConfig
{
    public string Model { get; set; } = "RTX5070";
    public int DeviceId { get; set; } = 0;
    public int CudaVersion { get; set; } = 12;
    public string KernelPath { get; set; } = "cuda\\compiled";
    public bool UsePinnedMemory { get; set; } = true;
    public bool EnableTiming { get; set; } = true;
    public int MaxMemoryUsageMB { get; set; } = 0;
    public int StreamCount { get; set; } = 2;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Model))
            errors.Add("GPU model cannot be empty");

        if (DeviceId < 0)
            errors.Add("Device ID must be >= 0");

        if (CudaVersion < 10)
            errors.Add("CUDA version must be at least 10");

        if (string.IsNullOrWhiteSpace(KernelPath))
            errors.Add("Kernel path cannot be empty");

        if (MaxMemoryUsageMB < 0)
            errors.Add("Max memory usage must be >= 0");

        if (StreamCount < 1)
            errors.Add("Stream count must be at least 1");

        return errors.Count == 0;
    }
}
