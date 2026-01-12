# BLE Library

Cross-platform Bluetooth Low Energy library for .NET 9, supporting Windows and Android.

## Features

- Cross-platform support for Windows and Android
- Device discovery and scanning
- Advertisement data access
- Connection management
- Service and characteristic discovery
- Read, write, and notification support

## Installation

Add to your project:

```xml
<PackageReference Include="ble_lib" Version="1.0.0" />
```

## Usage

### Basic Setup

```csharp
using BleLib.Services;
using BleLib.Models;

// Create a Bluetooth service for the current platform
var bluetoothService = BluetoothServiceFactory.CreateBluetoothService();

// Initialize the service
await bluetoothService.InitializeAsync();
```

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

### Advertisement Data

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

### Services and Characteristics

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

### Reading and Writing

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

### Notifications

```csharp
await bluetoothService.SubscribeToCharacteristicAsync(
    deviceToConnect,
    heartRateServiceUuid,
    heartRateMeasurementUuid,
    (data) =>
    {
        Console.WriteLine($"Received notification: {BitConverter.ToString(data)}");
    });

await bluetoothService.UnsubscribeFromCharacteristicAsync(device, serviceUuid, characteristicUuid);
```

### Disconnecting

```csharp
await bluetoothService.DisconnectFromDeviceAsync(device);
```

## Platform Notes

**Android**: Requires location permission for scanning.

**Windows**: Requires Bluetooth capability in app manifest. Supports Windows 10 version 19041.0 and later.

## Error Handling

```csharp
try
{
    await bluetoothService.ConnectToDeviceAsync(device);
}
catch (BluetoothException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## License

Provided as-is for educational and development purposes. 