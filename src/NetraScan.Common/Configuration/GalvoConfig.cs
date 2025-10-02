using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NetraScan.Common.Configuration;

/// <summary>
/// Galvanometer scanner configuration for NI DAQmx (Dev3)
/// </summary>
public class GalvoConfig
{
    public string DeviceName { get; set; } = "Dev3";
    public string XGalvoChannel { get; set; } = "ao0";
    public string YGalvoChannel { get; set; } = "ao1";
    public string TriggerCounterChannel { get; set; } = "ctr0";
    public string ExternalClockInput { get; set; } = "PFI0";

    public double XVoltageMin { get; set; } = -10.0;
    public double XVoltageMax { get; set; } = 10.0;
    public double YVoltageMin { get; set; } = -10.0;
    public double YVoltageMax { get; set; } = 10.0;
    public double ScanAmplitude { get; set; } = 5.0;
    public double ScanOffset { get; set; } = 0.0;

    public bool UseExternalClock { get; set; } = true;
    public double SampleRate { get; set; } = 100000.0;

    public string GetXChannelName() => $"{DeviceName}/{XGalvoChannel}";
    public string GetYChannelName() => $"{DeviceName}/{YGalvoChannel}";
    public string GetTriggerChannelName() => $"{DeviceName}/{TriggerCounterChannel}";
    public string GetClockInputName() => $"/{DeviceName}/{ExternalClockInput}";

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DeviceName))
            errors.Add("Device name cannot be empty");

        if (string.IsNullOrWhiteSpace(XGalvoChannel))
            errors.Add("X galvo channel cannot be empty");

        if (string.IsNullOrWhiteSpace(YGalvoChannel))
            errors.Add("Y galvo channel cannot be empty");

        if (string.IsNullOrWhiteSpace(TriggerCounterChannel))
            errors.Add("Trigger counter channel cannot be empty");

        if (UseExternalClock && string.IsNullOrWhiteSpace(ExternalClockInput))
            errors.Add("External clock input cannot be empty when external clock is enabled");

        if (XVoltageMin >= XVoltageMax)
            errors.Add("X voltage min must be less than max");

        if (YVoltageMin >= YVoltageMax)
            errors.Add("Y voltage min must be less than max");

        if (ScanAmplitude <= 0)
            errors.Add("Scan amplitude must be positive");

        if (SampleRate <= 0)
            errors.Add("Sample rate must be positive");

        return errors.Count == 0;
    }
}
