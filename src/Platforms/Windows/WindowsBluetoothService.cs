#if NET9_0_WINDOWS10_0_19041_0
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using BleLib.Models;
using BleLib.Services;
using BleLib.Exceptions;

namespace BleLib.Platforms.WindowsBLE
{
    public class WindowsBluetoothService : IBluetoothService
    {
        private Dictionary<string, BluetoothLEDevice> _connectedDevices;
        private List<BleLib.Models.BluetoothDevice> _discoveredDevices;
        private bool _isScanning;
        private BluetoothLEAdvertisementWatcher? _advertisementWatcher;
        private HashSet<string> _discoveredDeviceIds;

        public WindowsBluetoothService()
        {
            _connectedDevices = new Dictionary<string, BluetoothLEDevice>();
            _discoveredDevices = new List<BleLib.Models.BluetoothDevice>();
            _discoveredDeviceIds = new HashSet<string>();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                // Check if Bluetooth is available
                var adapter = await BluetoothAdapter.GetDefaultAsync();
                if (adapter == null)
                {
                    throw new Exception("Bluetooth adapter not found");
                }


                return true;
            }
            catch (Exception ex)
            {
                throw new BluetoothException($"Failed to initialize Bluetooth: {ex.Message}", ex);
            }
        }

        public async Task<bool> StartScanningAsync()
        {
            if (_isScanning)
                return true;

            try
            {
                _isScanning = true;
                _discoveredDevices.Clear();
                _discoveredDeviceIds.Clear();

                // Create advertisement watcher
                _advertisementWatcher = new BluetoothLEAdvertisementWatcher();
                
                // Configure watcher settings
                _advertisementWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                _advertisementWatcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter
                {
                    InRangeThresholdInDBm = -80,
                    OutOfRangeThresholdInDBm = -90,
                    OutOfRangeTimeout = TimeSpan.FromSeconds(1)
                };

                // Subscribe to advertisement received events
                _advertisementWatcher.Received += AdvertisementWatcher_Received;

                // Start watching
                _advertisementWatcher.Start();
                return true;
            }
            catch (Exception ex)
            {
                _isScanning = false;
                throw new BluetoothException($"Failed to start scanning: {ex.Message}", ex);
            }
        }

        private async void AdvertisementWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                var deviceId = args.BluetoothAddress.ToString("X12");
                
                // Check if device is already discovered
                if (_discoveredDeviceIds.Contains(deviceId))
                {
                    // Update existing device's last seen time and signal strength
                    var existingDevice = _discoveredDevices.FirstOrDefault(d => d.Address == deviceId);
                    if (existingDevice != null)
                    {
                        existingDevice.LastSeen = DateTime.Now;
                        existingDevice.SignalStrength = args.RawSignalStrengthInDBm;
                    }
                    return;
                }

                // Add new device to discovered list
                _discoveredDeviceIds.Add(deviceId);

                var device = new BleLib.Models.BluetoothDevice
                {
                    Name = args.Advertisement.LocalName ?? "Unknown Device",
                    Address = deviceId,
                    LastSeen = DateTime.Now,
                    SignalStrength = args.RawSignalStrengthInDBm
                };

                // Parse advertisement data
                ParseAdvertisementDataAsync(args.Advertisement, device);

                // Try to get additional device information
                try
                {
                    var bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                    if (bluetoothDevice != null)
                    {
                        if (!string.IsNullOrEmpty(bluetoothDevice.Name))
                        {
                            device.Name = bluetoothDevice.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - device info might not be available
                    System.Diagnostics.Debug.WriteLine($"Could not get device info for {deviceId}: {ex.Message}");
                }

                _discoveredDevices.Add(device);
            }
            catch (Exception ex)
            {
                // Log errors but don't fail the scan
                System.Diagnostics.Debug.WriteLine($"Error processing advertisement: {ex.Message}");
            }
        }

        public async Task StopScanningAsync()
        {
            if (_advertisementWatcher != null)
            {
                _advertisementWatcher.Stop();
                _advertisementWatcher.Received -= AdvertisementWatcher_Received;
                _advertisementWatcher = null;
            }
            _isScanning = false;
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothDevice>> GetDiscoveredDevicesAsync()
        {
            return await Task.FromResult(_discoveredDevices);
        }

        public async Task<bool> ConnectToDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Convert hex address to BluetoothAddress
                if (ulong.TryParse(device.Address, System.Globalization.NumberStyles.HexNumber, null, out ulong bluetoothAddress))
                {
                    var bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
                    if (bluetoothDevice == null)
                    {
                        throw new Exception("Device not found");
                    }

                    _connectedDevices[device.Address] = bluetoothDevice;
                    device.IsConnected = true;
                    return true;
                }
                else
                {
                    throw new Exception("Invalid device address format");
                }
            }
            catch (Exception ex)
            {
                throw new BluetoothException($"Failed to connect to device: {ex.Message}", ex);
            }
        }

        public async Task DisconnectFromDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            if (_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                bluetoothDevice.Dispose();
                _connectedDevices.Remove(device.Address);
                device.IsConnected = false;
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothService>> GetServicesAsync(BleLib.Models.BluetoothDevice device)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            var services = new List<BleLib.Models.BluetoothService>();
            var result = await bluetoothDevice.GetGattServicesAsync();
            
            foreach (var service in result.Services)
            {
                var bluetoothService = new BleLib.Models.BluetoothService
                {
                    ServiceUuid = service.Uuid,
                    Name = service.Uuid.ToString()
                };

                var characteristics = await service.GetCharacteristicsAsync();
                foreach (var characteristic in characteristics.Characteristics)
                {
                    bluetoothService.Characteristics.Add(new BleLib.Models.BluetoothCharacteristic
                    {
                        CharacteristicUuid = characteristic.Uuid,
                        Name = characteristic.Uuid.ToString(),
                        CanRead = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read),
                        CanWrite = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write),
                        CanNotify = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
                    });
                }

                services.Add(bluetoothService);
            }

            return services;
        }

        public async Task<byte[]> ReadCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            var result = await bluetoothDevice.GetGattServicesAsync();
            var service = result.Services.FirstOrDefault(s => s.Uuid == serviceUuid);
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristics = await service.GetCharacteristicsAsync();
            var characteristic = characteristics.Characteristics.FirstOrDefault(c => c.Uuid == characteristicUuid);
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
            {
                throw new BluetoothException("Characteristic does not support reading");
            }

            var readResult = await characteristic.ReadValueAsync();
            if (readResult.Status != GattCommunicationStatus.Success)
            {
                throw new BluetoothException($"Failed to read characteristic: {readResult.Status}");
            }

            return ConvertIBufferToBytes(readResult.Value);
        }

        public async Task WriteCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            var result = await bluetoothDevice.GetGattServicesAsync();
            var service = result.Services.FirstOrDefault(s => s.Uuid == serviceUuid);
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristics = await service.GetCharacteristicsAsync();
            var characteristic = characteristics.Characteristics.FirstOrDefault(c => c.Uuid == characteristicUuid);
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write))
            {
                throw new BluetoothException("Characteristic does not support writing");
            }

            var buffer = CreateBufferFromBytes(data);
            var writeResult = await characteristic.WriteValueAsync(buffer);
            if (writeResult != GattCommunicationStatus.Success)
            {
                throw new BluetoothException($"Failed to write characteristic: {writeResult}");
            }
        }

        public async Task SubscribeToCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> callback)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            var result = await bluetoothDevice.GetGattServicesAsync();
            var service = result.Services.FirstOrDefault(s => s.Uuid == serviceUuid);
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristics = await service.GetCharacteristicsAsync();
            var characteristic = characteristics.Characteristics.FirstOrDefault(c => c.Uuid == characteristicUuid);
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            if (!characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                throw new BluetoothException("Characteristic does not support notifications");
            }

            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            if (status != GattCommunicationStatus.Success)
            {
                throw new BluetoothException($"Failed to subscribe to characteristic: {status}");
            }

            characteristic.ValueChanged += (sender, args) =>
            {
                var data = ConvertIBufferToBytes(args.CharacteristicValue);
                callback(data);
            };
        }

        public async Task UnsubscribeFromCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var bluetoothDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            var result = await bluetoothDevice.GetGattServicesAsync();
            var service = result.Services.FirstOrDefault(s => s.Uuid == serviceUuid);
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristics = await service.GetCharacteristicsAsync();
            var characteristic = characteristics.Characteristics.FirstOrDefault(c => c.Uuid == characteristicUuid);
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            if (status != GattCommunicationStatus.Success)
            {
                throw new BluetoothException($"Failed to unsubscribe from characteristic: {status}");
            }
        }

        private void ParseAdvertisementDataAsync(BluetoothLEAdvertisement advertisement, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Parse service UUIDs
                if (advertisement.ServiceUuids.Count > 0)
                {
                    device.ServiceUuids.AddRange(advertisement.ServiceUuids);
                }

                // Parse manufacturer data
                if (advertisement.ManufacturerData.Count > 0)
                {
                    foreach (var manufacturerData in advertisement.ManufacturerData)
                    {
                        var dataBytes = ConvertIBufferToBytes(manufacturerData.Data);
                        device.ManufacturerData[manufacturerData.CompanyId] = dataBytes;
                    }
                }

                // Parse data sections for TX Power Level and other data
                if (advertisement.DataSections.Count > 0)
                {
                    foreach (var dataSection in advertisement.DataSections)
                    {
                        var dataBytes = ConvertIBufferToBytes(dataSection.Data);
                        
                        // Check for TX Power Level in data sections (data type 0x0A)
                        if (dataSection.DataType == 0x0A && dataBytes.Length >= 1)
                        {
                            // TX Power Level is a signed 8-bit integer
                            device.TxPowerLevel = (sbyte)dataBytes[0];
                        }
                        
                        // Store other data sections in ServiceData
                        var key = Guid.NewGuid(); // This is a placeholder - you might want to use a different approach
                        device.ServiceData[key] = dataBytes;
                    }
                }

                // Check if device is connectable (this is typically available in the advertisement flags)
                // For Windows, we can infer this from the advertisement type
                device.IsConnectable = true; // Most BLE advertisements are connectable
            }
            catch (Exception ex)
            {
                // Log errors but don't fail the device processing
                System.Diagnostics.Debug.WriteLine($"Error parsing advertisement data: {ex.Message}");
            }
        }

        private static byte[] ConvertIBufferToBytes(IBuffer buffer)
        {
            if (buffer == null) return new byte[0];
            
            var reader = DataReader.FromBuffer(buffer);
            var bytes = new byte[buffer.Length];
            reader.ReadBytes(bytes);
            return bytes;
        }

        private static IBuffer CreateBufferFromBytes(byte[] data)
        {
            var writer = new DataWriter();
            writer.WriteBytes(data);
            return writer.DetachBuffer();
        }
    }
}
#endif