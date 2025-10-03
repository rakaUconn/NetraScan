using Microsoft.Extensions.Logging;
using NetraScan.Hardware.Camera;
using NetraScan.Hardware.Galvo;
using NetraScan.Common.Configuration;
using Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NetraScan.Hardware.Tests;

public class IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task Camera_And_Galvo_Should_Work_Together()
    {
        // Arrange
        var config = ConfigurationManager.LoadConfiguration();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var camera = new SaperaCamera(loggerFactory.CreateLogger<SaperaCamera>());
        var galvo = new NIGalvoController(loggerFactory.CreateLogger<NIGalvoController>());

        int framesReceived = 0;
        camera.FrameReceived += (sender, args) =>
        {
            framesReceived++;
            if (framesReceived % 10 == 0)
            {
                _output.WriteLine($"Received {framesReceived} frames");
            }
        };

        // Act - Initialize
        Assert.True(await camera.InitializeAsync(config.Camera.ConfigFilePath));
        Assert.True(await galvo.InitializeAsync(config.Galvo.DeviceName));

        // Configure galvo
        Assert.True(await galvo.ConfigureChannelsAsync(
            config.Galvo.XGalvoChannel,
            config.Galvo.YGalvoChannel,
            config.Galvo.XVoltageMin,
            config.Galvo.XVoltageMax,
            config.Galvo.YVoltageMin,
            config.Galvo.YVoltageMax));

        // Configure trigger
        Assert.True(await galvo.ConfigureTriggerAsync(
            config.Galvo.TriggerCounterChannel,
            config.Galvo.ExternalClockInput,
            config.Camera.LineRate));

        // Generate scan pattern
        var pattern = ScanPatternGenerator.GenerateBScanPattern(
            samplesPerLine: config.Camera.PixelsPerLine,
            linesPerBScan: config.Camera.LinesPerBScan,
            scanAmplitude: config.Galvo.ScanAmplitude,
            sampleRate: config.Galvo.SampleRate);

        Assert.True(await galvo.SetScanPatternAsync(pattern));

        // Start synchronized acquisition
        Assert.True(await galvo.StartScanningAsync());
        Assert.True(await camera.StartGrabbingAsync());

        // Run for 5 seconds
        await System.Threading.Tasks.Task.Delay(5000);

        // Stop
        Assert.True(await camera.StopGrabbingAsync());
        Assert.True(await galvo.StopScanningAsync());

        // Assert
        Assert.True(framesReceived > 0);
        Assert.Equal(0, camera.GetTotalFramesLost());

        _output.WriteLine($"Total frames acquired: {camera.GetTotalFramesAcquired()}");
        _output.WriteLine($"Total frames lost: {camera.GetTotalFramesLost()}");
        _output.WriteLine($"Frame rate: {framesReceived / 5.0:F2} fps");

        // Cleanup
        camera.Dispose();
        galvo.Dispose();
    }
}
