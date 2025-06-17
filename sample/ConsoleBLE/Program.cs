using BleLib.Models;
using BleLib.Services;

namespace ConsoleBLE
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a Bluetooth service using the factory
            var bluetoothService = BluetoothServiceFactory.CreateBluetoothService();

            try
            {
                // Initialize the service
                Console.WriteLine("Initializing Bluetooth service...");
                var initialized = await bluetoothService.InitializeAsync();
                
                if (!initialized)
                {
                    Console.WriteLine("Failed to initialize Bluetooth service.");
                    return;
                }
                
                Console.WriteLine("Bluetooth service initialized successfully.");
                
                // Start scanning for devices
                Console.WriteLine("Starting device scan...");
                var scanStarted = await bluetoothService.StartScanningAsync();
                
                if (!scanStarted)
                {
                    Console.WriteLine("Failed to start scanning.");
                    return;
                }
                
                // Wait for devices to be discovered
                Console.WriteLine("Scanning for devices (5 seconds)...");
                await Task.Delay(5000);
                
                // Get discovered devices
                var devices = await bluetoothService.GetDiscoveredDevicesAsync();
                
                // Stop scanning
                await bluetoothService.StopScanningAsync();
                
                Console.WriteLine($"Found {devices.Count()} devices:");
                Console.WriteLine(new string('=', 60));
                
                foreach (var device in devices)
                {
                    Console.WriteLine($"Device: {device.Name} ({device.Address})");
                    Console.WriteLine($"Signal Strength: {device.SignalStrength} dBm");
                    Console.WriteLine($"Last Seen: {device.LastSeen}");
                    Console.WriteLine($"Is Connectable: {device.IsConnectable}");
                    Console.WriteLine($"TX Power Level: {device.TxPowerLevel}");
                    
                    // Display advertisement data
                    DisplayAdvertisementData(device);
                    
                    Console.WriteLine(new string('-', 40));
                }
                
                // If we found devices, try to connect to the first one
                if (devices.Any())
                {
                    var firstDevice = devices.First();
                    Console.WriteLine($"\nAttempting to connect to {firstDevice.Name}...");
                    
                    var connected = await bluetoothService.ConnectToDeviceAsync(firstDevice);
                    
                    if (connected)
                    {
                        Console.WriteLine($"Connected to {firstDevice.Name}!");
                        
                        // Get services from the connected device
                        var services = await bluetoothService.GetServicesAsync(firstDevice);
                        
                        Console.WriteLine($"\nFound {services.Count()} services:");
                        
                        foreach (var service in services)
                        {
                            Console.WriteLine($"- Service: {service.ServiceUuid}");
                            Console.WriteLine($"  Characteristics: {service.Characteristics.Count}");
                            
                            foreach (var characteristic in service.Characteristics)
                            {
                                Console.WriteLine($"    - {characteristic.CharacteristicUuid} (R:{characteristic.CanRead}, W:{characteristic.CanWrite}, N:{characteristic.CanNotify})");
                            }

                            // Read and display MTU and PHY
                            try
                            {
                                var mtu = await service.ReadMtuAsync();
                                Console.WriteLine($"  MTU: {mtu}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  MTU: Not available ({ex.Message})");
                            }
                            try
                            {
                                var phy = await service.ReadPhyAsync();
                                Console.WriteLine($"  PHY: {phy}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  PHY: Not available ({ex.Message})");
                            }
                        }
                        
                        // Disconnect when done
                        await bluetoothService.DisconnectFromDeviceAsync(firstDevice);
                        Console.WriteLine($"Disconnected from {firstDevice.Name}.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to connect to {firstDevice.Name}.");
                    }
                }
                else
                {
                    Console.WriteLine("No devices found.");
                }
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("This platform is not supported for Bluetooth operations.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void DisplayAdvertisementData(BluetoothDevice device)
        {
            Console.WriteLine("Advertisement Data:");
            
            // Display service UUIDs
            if (device.ServiceUuids.Count > 0)
            {
                Console.WriteLine($"  Service UUIDs ({device.ServiceUuids.Count}):");
                foreach (var serviceUuid in device.ServiceUuids)
                {
                    Console.WriteLine($"    - {serviceUuid}");
                }
            }
            else
            {
                Console.WriteLine("  Service UUIDs: None");
            }
            
            // Display manufacturer data
            if (device.ManufacturerData.Count > 0)
            {
                Console.WriteLine($"  Manufacturer Data ({device.ManufacturerData.Count}):");
                foreach (var manufacturerData in device.ManufacturerData)
                {
                    var companyId = manufacturerData.Key;
                    var data = manufacturerData.Value;
                    var hexData = BitConverter.ToString(data).Replace("-", " ");
                    Console.WriteLine($"    - Company ID: 0x{companyId:X4} ({companyId})");
                    Console.WriteLine($"      Data: {hexData} ({data.Length} bytes)");
                }
            }
            else
            {
                Console.WriteLine("  Manufacturer Data: None");
            }
            
            // Display service data
            if (device.ServiceData.Count > 0)
            {
                Console.WriteLine($"  Service Data ({device.ServiceData.Count}):");
                foreach (var serviceData in device.ServiceData)
                {
                    var serviceUuid = serviceData.Key;
                    var data = serviceData.Value;
                    var hexData = BitConverter.ToString(data).Replace("-", " ");
                    Console.WriteLine($"    - Service: {serviceUuid}");
                    Console.WriteLine($"      Data: {hexData} ({data.Length} bytes)");
                }
            }
            else
            {
                Console.WriteLine("  Service Data: None");
            }
            
            // Display raw advertisement data
            if (device.RawAdvertisementData.Length > 0)
            {
                var hexData = BitConverter.ToString(device.RawAdvertisementData).Replace("-", " ");
                Console.WriteLine($"  Raw Advertisement Data: {hexData} ({device.RawAdvertisementData.Length} bytes)");
            }
            else
            {
                Console.WriteLine("  Raw Advertisement Data: None");
            }
        }
    }
}
