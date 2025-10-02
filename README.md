# NetraScan OCT Imaging System

**Modern OCT Software Built on .NET 8 with WPF**

## Overview

NetraScan is a modernized optical coherence tomography imaging system, rebuilt from the ground up with .NET 8.0 and WPF, designed for real-time imaging with cutting-edge hardware.

## Hardware Configuration

### Camera
- **Model**: Xtium2-CL_MX4_1 (Teledyne DALSA)
- **SDK**: Sapera LT (Latest version)
- **Interface**: Camera Link
- **Line Rate**: Up to 250 kHz

### Galvanometer Scanner
- **Controller**: National Instruments Dev3
- **X-Galvo**: Dev3/ao0 (Fast axis)
- **Y-Galvo**: Dev3/ao1 (Slow axis)
- **Trigger Output**: Dev3/ctr0
- **Clock Sync Input**: Dev3/PFI0

### GPU Processor
- **Model**: NVIDIA RTX 5070
- **CUDA Version**: 12.x
- **Processing**: cuFFT for real-time FFT operations

## System Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- NVIDIA GPU with CUDA 12.x support
- Teledyne DALSA Sapera LT SDK
- National Instruments DAQmx 25.5+
- Visual Studio 2022 (for development)
- Minimum 16GB RAM
- Recommended 32GB RAM for optimal performance

## Solution Structure
NetraScan/
├── src/
│   ├── NetraScan.Common/      # Shared utilities and configuration
│   ├── NetraScan.Hardware/    # Hardware abstraction layer
│   ├── NetraScan.Processing/  # CUDA processing pipeline
│   ├── NetraScan.Core/        # Business logic orchestration
│   └── NetraScan.UI/          # WPF user interface
├── tests/                      # Unit and integration tests
├── docs/                       # Documentation
├── cuda/kernels/              # CUDA source files
└── config/                     # Configuration files

## Quick Start

### 1. Build the Solution
```bash
cd C:\Dev\OCTProjects\NetraScan
dotnet restore
dotnet build -c Release