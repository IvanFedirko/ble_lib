# BLE Library

A comprehensive cross-platform Bluetooth Low Energy (BLE) library for .NET 9, supporting Windows and Android platforms with a modern, extensible architecture.

## 🚀 Features

- **Cross-platform support**: Windows 10+ and Android with unified API
- **Device discovery**: Advanced scanning with signal strength and advertisement data
- **Rich advertisement data**: Access manufacturer data, service UUIDs, service data, and raw advertisement data
- **Connection management**: Robust connect/disconnect with error handling
- **Service discovery**: Complete service and characteristic enumeration
- **Data operations**: Read, write, and subscribe to characteristics with notifications
- **Platform abstraction**: Clean separation with factory pattern for easy extension
- **Comprehensive testing**: Unit, mock, and integration tests with full coverage
- **Sample applications**: Console and MAUI examples demonstrating real-world usage

## 📋 Requirements

- **.NET 9** or later
- **Windows 10** (version 19041.0 or later) with Bluetooth support
- **Android** with Microsoft.Maui.Essentials support
- **Visual Studio 2022** or **VS Code** with .NET 9 workload

## 📋 Project Structure 

## 🚀 Quick Start

### Installation

Add the library to your project:

```xml
<PackageReference Include="ble_lib" Version="1.0.0" />
```

### Basic Usage

```csharp
using BleLib.Services;
using BleLib.Models;

// Create a Bluetooth service for the current platform
var bluetoothService = BluetoothServiceFactory.CreateBluetoothService();

// Initialize the service
await bluetoothService.InitializeAsync();

// Start scanning for devices
await bluetoothService.StartScanningAsync();

// Wait for devices to be discovered
await Task.Delay(5000);

// Get discovered devices
var devices = await bluetoothService.GetDiscoveredDevicesAsync();

// Stop scanning
await bluetoothService.StopScanningAsync();
```

## 📖 Detailed Usage

### Device Discovery

```csharp
// Start scanning for devices
await bluetoothService.StartScanningAsync();

// Wait for devices to be discovered
await Task.Delay(5000); // Scan for 5 seconds

// Get discovered devices
var devices = await bluetoothService.GetDiscoveredDevicesAsync();

// Stop scanning
await bluetoothService.StopScanningAsync();
```

### Accessing Advertisement Data

```csharp
foreach (var device in devices)
{
    Console.WriteLine($"Device: {device.Name} ({device.Address})");
    Console.WriteLine($"Signal Strength: {device.SignalStrength} dBm");
    Console.WriteLine($"TX Power Level: {device.TxPowerLevel} dBm");
    
    // Service UUIDs in advertisement
    if (device.ServiceUuids.Count > 0)
    {
        Console.WriteLine("Service UUIDs:");
        foreach (var serviceUuid in device.ServiceUuids)
        {
            Console.WriteLine($"  - {serviceUuid}");
        }
    }
    
    // Manufacturer data
    if (device.ManufacturerData.Count > 0)
    {
        Console.WriteLine("Manufacturer Data:");
        foreach (var manufacturerData in device.ManufacturerData)
        {
            var manufacturerId = manufacturerData.Key;
            var data = manufacturerData.Value;
            Console.WriteLine($"  - Company ID: 0x{manufacturerId:X4}, Data: {BitConverter.ToString(data)}");
        }
    }
    
    // Service data
    if (device.ServiceData.Count > 0)
    {
        Console.WriteLine("Service Data:");
        foreach (var serviceData in device.ServiceData)
        {
            var serviceUuid = serviceData.Key;
            var data = serviceData.Value;
            Console.WriteLine($"  - Service: {serviceUuid}, Data: {BitConverter.ToString(data)}");
        }
    }
    
    // Raw advertisement data
    if (device.RawAdvertisementData.Length > 0)
    {
        Console.WriteLine($"Raw Advertisement Data: {BitConverter.ToString(device.RawAdvertisementData)}");
    }
}
```

### Connecting to a Device

```csharp
// Connect to a specific device
var deviceToConnect = devices.FirstOrDefault(d => d.Name.Contains("MyDevice"));
if (deviceToConnect != null)
{
    var connected = await bluetoothService.ConnectToDeviceAsync(deviceToConnect);
    if (connected)
    {
        Console.WriteLine($"Connected to {deviceToConnect.Name}");
    }
}
```

### Working with Services and Characteristics

```csharp
// Get services from connected device
var services = await bluetoothService.GetServicesAsync(deviceToConnect);

foreach (var service in services)
{
    Console.WriteLine($"Service: {service.ServiceUuid}");
    
    foreach (var characteristic in service.Characteristics)
    {
        Console.WriteLine($"  Characteristic: {characteristic.CharacteristicUuid}");
        Console.WriteLine($"    Can Read: {characteristic.CanRead}");
        Console.WriteLine($"    Can Write: {characteristic.CanWrite}");
        Console.WriteLine($"    Can Notify: {characteristic.CanNotify}");
    }
}
```

### Reading and Writing Data

```csharp
// Example service and characteristic UUIDs (Heart Rate Monitor)
var heartRateServiceUuid = Guid.Parse("180D");
var heartRateMeasurementUuid = Guid.Parse("2A37");

// Read characteristic
try
{
    var data = await bluetoothService.ReadCharacteristicAsync(
        deviceToConnect, 
        heartRateServiceUuid, 
        heartRateMeasurementUuid);
    
    Console.WriteLine($"Read data: {BitConverter.ToString(data)}");
}
catch (BluetoothException ex)
{
    Console.WriteLine($"Failed to read: {ex.Message}");
}

// Write characteristic
try
{
    var writeData = new byte[] { 0x01, 0x02, 0x03 };
    await bluetoothService.WriteCharacteristicAsync(
        deviceToConnect, 
        heartRateServiceUuid, 
        heartRateMeasurementUuid, 
        writeData);
    
    Console.WriteLine("Data written successfully");
}
catch (BluetoothException ex)
{
    Console.WriteLine($"Failed to write: {ex.Message}");
}
```

### Subscribing to Notifications

```csharp
// Subscribe to characteristic notifications
await bluetoothService.SubscribeToCharacteristicAsync(
    deviceToConnect,
    heartRateServiceUuid,
    heartRateMeasurementUuid,
    (data) =>
    {
        Console.WriteLine($"Received notification: {BitConverter.ToString(data)}");
    });

// Unsubscribe when done
await bluetoothService.UnsubscribeFromCharacteristicAsync(
    deviceToConnect,
    heartRateServiceUuid,
    heartRateMeasurementUuid);
```

### Disconnecting

```csharp
// Disconnect from device
await bluetoothService.DisconnectFromDeviceAsync(deviceToConnect);
```

## 🧪 Testing

### Running Tests

```bash
# Navigate to test directory
cd test

# Run all tests
dotnet test

# Run tests for specific framework
dotnet test --framework net9.0-windows10.0.19041.0
dotnet test --framework net9.0-android

# Run specific test category
dotnet test --filter "Category=Unit"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Unit Tests**: Test individual components in isolation
- **Mock Tests**: Test with mocked dependencies
- **Integration Tests**: Test platform-specific implementations

## 📱 Sample Applications

### Console Application

A complete console application demonstrating all library features:

```bash
cd sample/ConsoleBLE
dotnet run
```

### MAUI Application

A modern MAUI application with web interface for testing BLE devices:

```bash
cd sample/MauiWinAnd
dotnet run
```

## 🔧 Development

### Building the Library

```bash
cd src
dotnet build
```

### Running Samples

```bash
# Console sample
cd sample/ConsoleBLE
dotnet run

# MAUI sample
cd sample/MauiWinAnd
dotnet run
```

## 📦 Dependencies

### Core Library
- **.NET 9**: Base framework
- **Microsoft.Maui.Essentials**: Android platform support

### Testing
- **xUnit**: Testing framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework
- **coverlet.collector**: Code coverage

### Samples
- **Microsoft.Maui**: MAUI framework for cross-platform UI
- **Bootstrap**: CSS framework for web UI

## 🏗️ Architecture

### Design Patterns

- **Factory Pattern**: `BluetoothServiceFactory` creates platform-specific implementations
- **Interface Segregation**: `IBluetoothService` defines the contract
- **Platform Abstraction**: Clean separation between core logic and platform-specific code

### Key Components

- **Models**: Data structures for devices, services, and characteristics
- **Services**: Business logic and platform abstraction
- **Platforms**: Platform-specific implementations
- **Exceptions**: Custom exception types for error handling

## 🔒 Permissions

### Windows
- Bluetooth permissions are handled automatically by the Windows runtime

### Android
- `BLUETOOTH` permission for basic operations
- `BLUETOOTH_ADMIN` permission for scanning and connecting
- `ACCESS_FINE_LOCATION` permission for device discovery (Android 6.0+)

## 🐛 Troubleshooting

### Common Issues

1. **Platform Not Supported**: Ensure you're running on Windows 10+ or Android
2. **No Devices Found**: Check Bluetooth is enabled and devices are advertising
3. **Permission Denied**: Ensure proper permissions are granted (especially on Android)
4. **Connection Failed**: Verify device is in range and not already connected

### Debug Mode

Enable detailed logging by setting environment variables:
```bash
set BLE_DEBUG=true
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/ble_lib/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/ble_lib/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/ble_lib/wiki)

---

**Made with ❤️ for the .NET community** 