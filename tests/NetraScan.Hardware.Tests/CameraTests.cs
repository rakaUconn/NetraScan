using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using NetraScan.Hardware.Camera;
using NetraScan.Common.Configuration;
using Xunit;

namespace NetraScan.Hardware.Tests;

public class CameraTests
{
    private readonly ILogger<SaperaCamera> _logger;

    public CameraTests()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        _logger = loggerFactory.CreateLogger<SaperaCamera>();
    }

    [Fact]
    public async System.Threading.Tasks.Task Camera_Initialize_Should_Succeed()
    {
        // Arrange
        var config = ConfigurationManager.LoadConfiguration();
        var camera = new SaperaCamera(_logger);

        // Act
        var result = await camera.InitializeAsync(config.Camera.ConfigFilePath);

        // Assert
        Assert.True(result);
        Assert.True(camera.IsInitialized);
        Assert.False(camera.IsGrabbing);

        // Cleanup
        camera.Dispose();
    }

    [Fact]
    public async System.Threading.Tasks.Task Camera_Should_Acquire_Frames()
    {
        // Arrange
        var config = ConfigurationManager.LoadConfiguration();
        var camera = new SaperaCamera(_logger);

        int framesReceived = 0;
        camera.FrameReceived += (sender, args) =>
        {
            framesReceived++;
        };

        // Act
        await camera.InitializeAsync(config.Camera.ConfigFilePath);
        await camera.StartGrabbingAsync();

        // Wait for frames
        await System.Threading.Tasks.Task.Delay(1000);

        await camera.StopGrabbingAsync();

        // Assert
        Assert.True(framesReceived > 0);
        Assert.Equal(0, camera.GetTotalFramesLost());

        // Cleanup
        camera.Dispose();
    }
}
