using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using NetraScan.Hardware.Interfaces;

namespace NetraScan.Hardware.Galvo;

/// <summary>
/// Generates scan patterns for galvanometer control
/// </summary>
public static class ScanPatternGenerator
{
    /// <summary>
    /// Generates B-scan (2D) pattern with sawtooth on X-axis
    /// </summary>
    /// <param name="samplesPerLine">Number of samples per A-line</param>
    /// <param name="linesPerBScan">Number of A-lines per B-scan</param>
    /// <param name="scanAmplitude">Scan amplitude in volts</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="yPosition">Y position in volts (default 0)</param>
    /// <param name="returnSamples">Number of samples for flyback/return (default 0)</param>
    /// <returns>Scan pattern for B-scan</returns>
    public static ScanPattern GenerateBScanPattern(
        int samplesPerLine,
        int linesPerBScan,
        double scanAmplitude,
        double sampleRate,
        double yPosition = 0.0,
        int returnSamples = 0)
    {
        // Total samples includes forward scan + return
        int samplesPerCycle = samplesPerLine + returnSamples;
        int totalSamples = samplesPerCycle * linesPerBScan;

        double[] xVoltages = new double[totalSamples];
        double[] yVoltages = new double[totalSamples];

        for (int line = 0; line < linesPerBScan; line++)
        {
            // Forward scan
            for (int sample = 0; sample < samplesPerLine; sample++)
            {
                int index = line * samplesPerCycle + sample;

                // Sawtooth pattern for X (fast axis)
                double progress = (double)sample / (samplesPerLine - 1);
                xVoltages[index] = (progress * 2.0 - 1.0) * scanAmplitude;

                // Constant Y position
                yVoltages[index] = yPosition;
            }

            // Flyback/return (linear ramp back to start)
            for (int sample = 0; sample < returnSamples; sample++)
            {
                int index = line * samplesPerCycle + samplesPerLine + sample;

                // Linear ramp from +amplitude to -amplitude
                double progress = (double)sample / (returnSamples - 1);
                xVoltages[index] = scanAmplitude * (1.0 - 2.0 * progress);

                yVoltages[index] = yPosition;
            }
        }

        return new ScanPattern
        {
            XVoltages = xVoltages,
            YVoltages = yVoltages,
            SampleRate = sampleRate,
            SamplesPerLine = samplesPerLine,
            NumberOfLines = linesPerBScan,
            Type = ScanType.BScan
        };
    }

    /// <summary>
    /// Generates C-scan (3D volume) pattern with stepped Y positions
    /// </summary>
    public static ScanPattern GenerateCScanPattern(
        int samplesPerLine,
        int linesPerBScan,
        int bScansPerVolume,
        double scanAmplitude,
        double sampleRate,
        int returnSamples = 0)
    {
        int samplesPerCycle = samplesPerLine + returnSamples;
        int totalSamples = samplesPerCycle * linesPerBScan * bScansPerVolume;

        double[] xVoltages = new double[totalSamples];
        double[] yVoltages = new double[totalSamples];

        for (int bScan = 0; bScan < bScansPerVolume; bScan++)
        {
            // Calculate Y position for this B-scan
            double yProgress = (double)bScan / (bScansPerVolume - 1);
            double yPos = (yProgress * 2.0 - 1.0) * scanAmplitude;

            for (int line = 0; line < linesPerBScan; line++)
            {
                // Forward scan
                for (int sample = 0; sample < samplesPerLine; sample++)
                {
                    int index = (bScan * linesPerBScan + line) * samplesPerCycle + sample;

                    double xProgress = (double)sample / (samplesPerLine - 1);
                    xVoltages[index] = (xProgress * 2.0 - 1.0) * scanAmplitude;
                    yVoltages[index] = yPos;
                }

                // Flyback
                for (int sample = 0; sample < returnSamples; sample++)
                {
                    int index = (bScan * linesPerBScan + line) * samplesPerCycle + samplesPerLine + sample;

                    double xProgress = (double)sample / (returnSamples - 1);
                    xVoltages[index] = scanAmplitude * (1.0 - 2.0 * xProgress);
                    yVoltages[index] = yPos;
                }
            }
        }

        return new ScanPattern
        {
            XVoltages = xVoltages,
            YVoltages = yVoltages,
            SampleRate = sampleRate,
            SamplesPerLine = samplesPerLine,
            NumberOfLines = linesPerBScan * bScansPerVolume,
            Type = ScanType.CScan
        };
    }

    /// <summary>
    /// Generates circular scan pattern
    /// </summary>
    public static ScanPattern GenerateCircularPattern(
        int samplesPerCircle,
        double radius,
        double sampleRate,
        double centerX = 0.0,
        double centerY = 0.0)
    {
        double[] xVoltages = new double[samplesPerCircle];
        double[] yVoltages = new double[samplesPerCircle];

        for (int i = 0; i < samplesPerCircle; i++)
        {
            double angle = 2.0 * Math.PI * i / samplesPerCircle;
            xVoltages[i] = centerX + radius * Math.Cos(angle);
            yVoltages[i] = centerY + radius * Math.Sin(angle);
        }

        return new ScanPattern
        {
            XVoltages = xVoltages,
            YVoltages = yVoltages,
            SampleRate = sampleRate,
            SamplesPerLine = samplesPerCircle,
            NumberOfLines = 1,
            Type = ScanType.Custom
        };
    }

    /// <summary>
    /// Generates raster scan pattern (for imaging)
    /// </summary>
    public static ScanPattern GenerateRasterPattern(
        int xPoints,
        int yPoints,
        double xRange,
        double yRange,
        double sampleRate)
    {
        int totalPoints = xPoints * yPoints;
        double[] xVoltages = new double[totalPoints];
        double[] yVoltages = new double[totalPoints];

        int index = 0;
        for (int y = 0; y < yPoints; y++)
        {
            double yVolt = (yRange / (yPoints - 1)) * y - yRange / 2.0;

            for (int x = 0; x < xPoints; x++)
            {
                double xVolt = (xRange / (xPoints - 1)) * x - xRange / 2.0;

                xVoltages[index] = xVolt;
                yVoltages[index] = yVolt;
                index++;
            }
        }

        return new ScanPattern
        {
            XVoltages = xVoltages,
            YVoltages = yVoltages,
            SampleRate = sampleRate,
            SamplesPerLine = xPoints,
            NumberOfLines = yPoints,
            Type = ScanType.Custom
        };
    }
}
