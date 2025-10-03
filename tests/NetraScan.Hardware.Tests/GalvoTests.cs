using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetraScan.Common.Configuration;
using NetraScan.Hardware.Galvo;
using NetraScan.Hardware.Interfaces;
using Xunit;

namespace NetraScan.Hardware.Tests;

public class GalvoTests
{
    private readonly ILogger<NIGalvoController> _logger;

    public GalvoTests()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        _logger = loggerFactory.CreateLogger<NIGalvoController>();
    }

    [Fact]
    public async System.Threading.Tasks.Task Galvo_Initialize_Should_Succeed()
    {
        // Arrange
        var config = ConfigurationManager.LoadConfiguration();
        var galvo = new NIGalvoController(_logger);

        // Act
        var result = await galvo.InitializeAsync(config.Galvo.DeviceName);

        // Assert
        Assert.True(result);
        Assert.True(galvo.IsInitialized);

        // Cleanup
        galvo.Dispose();
    }

    [Fact]
    public void Galvo_Should_Generate_BScan_Pattern()  // Removed async since no await
    {
        // Arrange & Act
        var pattern = ScanPatternGenerator.GenerateBScanPattern(
            samplesPerLine: 2048,
            linesPerBScan: 512,
            scanAmplitude: 5.0,
            sampleRate: 100000);

        // Assert
        Assert.Equal(2048 * 512, pattern.XVoltages.Length);
        Assert.Equal(pattern.XVoltages.Length, pattern.YVoltages.Length);
        Assert.Equal(ScanType.BScan, pattern.Type);
    }
}
