using Xunit;
using NetraScan.Common.Configuration;

namespace NetraScan.Common.Tests;

public class ConfigurationTests
{
    [Fact]
    public void CreateDefaultConfiguration_ShouldNotBeNull()
    {
        // Arrange & Act
        var config = new HardwareConfig();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Camera);
        Assert.NotNull(config.Galvo);
        Assert.NotNull(config.Gpu);
        Assert.NotNull(config.Imaging);
    }

    [Fact]
    public void CameraConfig_DefaultValues_ShouldBeValid()
    {
        // Arrange
        var config = new CameraConfig
        {
            Model = "TestCamera",
            ConfigFilePath = Path.GetTempFileName(), // Creates a temp file
            ServerIndex = 0,
            ResourceIndex = 0,
            BufferCount = 4,
            LineRate = 50000,
            PixelsPerLine = 1024,
            LinesPerBScan = 256,
            BitsPerPixel = 12
        };

        // Act
        var isValid = config.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);

        // Cleanup
        File.Delete(config.ConfigFilePath);
    }

    [Fact]
    public void CameraConfig_InvalidBufferCount_ShouldFail()
    {
        // Arrange
        var config = new CameraConfig
        {
            Model = "TestCamera",
            ConfigFilePath = "test.ccf",
            BufferCount = 1 // Invalid: less than 2
        };

        // Act
        var isValid = config.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Buffer count"));
    }

    [Fact]
    public void GalvoConfig_DefaultChannelNames_ShouldBeFormatted()
    {
        // Arrange
        var config = new GalvoConfig
        {
            DeviceName = "Dev3",
            XGalvoChannel = "ao0",
            YGalvoChannel = "ao1"
        };

        // Act
        var xChannel = config.GetXChannelName();
        var yChannel = config.GetYChannelName();

        // Assert
        Assert.Equal("Dev3/ao0", xChannel);
        Assert.Equal("Dev3/ao1", yChannel);
    }

    [Fact]
    public void GpuConfig_ValidConfiguration_ShouldPass()
    {
        // Arrange
        var config = new GpuConfig
        {
            Model = "RTX5070",
            DeviceId = 0,
            CudaVersion = 12,
            KernelPath = "cuda\\compiled"
        };

        // Act
        var isValid = config.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void HardwareConfig_Validation_ShouldAggregateErrors()
    {
        // Arrange
        var config = new HardwareConfig
        {
            Camera = new CameraConfig
            {
                Model = "", // Invalid
                ConfigFilePath = "",
                BufferCount = 1 // Invalid
            },
            Galvo = new GalvoConfig
            {
                DeviceName = "", // Invalid
                ScanAmplitude = -1 // Invalid
            }
        };

        // Act
        var isValid = config.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.True(errors.Count >= 4); // Should have multiple errors
    }

    [Fact]
    public void ConfigurationManager_LoadConfiguration_ShouldCreateDefaultIfNotExists()
    {
        // Arrange
        var originalPath = ConfigurationManager.GetConfigFilePath();

        // Ensure clean state
        if (File.Exists(originalPath))
        {
            File.Delete(originalPath);
        }

        // Act
        var config = ConfigurationManager.LoadConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.True(ConfigurationManager.ConfigFileExists());

        // Cleanup
        if (File.Exists(originalPath))
        {
            File.Delete(originalPath);
        }
    }

    [Fact]
    public void ConfigurationManager_SaveAndLoad_ShouldPersist()
    {
        // Arrange
        var originalConfig = new HardwareConfig
        {
            Camera = new CameraConfig
            {
                Model = "Xtium2-CL_MX4_1",
                LineRate = 147000
            }
        };

        var configPath = ConfigurationManager.GetConfigFilePath();

        try
        {
            // Act
            ConfigurationManager.SaveConfiguration(originalConfig);
            var loadedConfig = ConfigurationManager.LoadConfiguration();

            // Assert
            Assert.Equal(originalConfig.Camera.Model, loadedConfig.Camera.Model);
            Assert.Equal(originalConfig.Camera.LineRate, loadedConfig.Camera.LineRate);
        }
        finally
        {
            // Cleanup
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }
}
