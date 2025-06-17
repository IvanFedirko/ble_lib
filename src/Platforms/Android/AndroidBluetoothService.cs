#if NET9_0_ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using BleLib.Models;
using BleLib.Services;
using BleLib.Exceptions;
using Microsoft.Maui.ApplicationModel;

namespace BleLib.Platforms.AndroidBLE
{
    public class AndroidBluetoothService : IBluetoothService
    {
        private BluetoothAdapter? _bluetoothAdapter;
        private Dictionary<string, BleLib.Models.BluetoothDevice> _connectedDevices;
        private List<BleLib.Models.BluetoothDevice> _discoveredDevices;
        private bool _isScanning;
        private BluetoothLeScanner? _scanner;
        private CustomScanCallback? _scanCallback;

        public AndroidBluetoothService()
        {
            _connectedDevices = new Dictionary<string, BleLib.Models.BluetoothDevice>();
            _discoveredDevices = new List<BleLib.Models.BluetoothDevice>();
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_bluetoothAdapter == null)
                {
                    throw new Exception("Bluetooth is not supported on this device");
                }

                // Request necessary permissions
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    throw new Exception("Location permission is required for Bluetooth scanning");
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

                if (_bluetoothAdapter == null || _bluetoothAdapter.BluetoothLeScanner == null)
                {
                    throw new Exception("Bluetooth LE Scanner is not available");
                }
                _scanner = _bluetoothAdapter.BluetoothLeScanner!;
                
   
                 var settings = new ScanSettings.Builder()
                    .SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency)
                    .Build();
    


                var scanCallback = new CustomScanCallback
                {
                    OnScanResultCallback = (callbackType, result) =>
                    {
                        if (result?.Device != null && result.Device.Address!=null)
                        {
                            var device = new BleLib.Models.BluetoothDevice
                            {
                                Name = result.Device.Name ?? "Unknown Device",
                                Address = result.Device.Address,
                                SignalStrength = result.Rssi,
                                LastSeen = DateTime.Now,
                                RawAdvertisementData = result.ScanRecord?.GetBytes() ?? Array.Empty<byte>()
                            };

                            // Parse advertisement data
                            if (result.ScanRecord != null)
                            {
                                ParseAdvertisementData(result.ScanRecord, device);
                            }

                            var existingDevice = _discoveredDevices.FirstOrDefault(d => d.Address == device.Address);
                            if (existingDevice != null)
                            {
                                existingDevice.SignalStrength = device.SignalStrength;
                                existingDevice.LastSeen = device.LastSeen;
                                // Update advertisement data if available
                                if (device.ServiceUuids.Count > 0)
                                    existingDevice.ServiceUuids = device.ServiceUuids;
                                if (device.ManufacturerData.Count > 0)
                                    existingDevice.ManufacturerData = device.ManufacturerData;
                                if (device.ServiceData.Count > 0)
                                    existingDevice.ServiceData = device.ServiceData;
                                if (device.RawAdvertisementData.Length > 0)
                                    existingDevice.RawAdvertisementData = device.RawAdvertisementData;
                            }
                            else
                            {
                                _discoveredDevices.Add(device);
                            }
                        }
                    }
                };

                if (scanCallback == null)
                {
                    throw new Exception("ScanCallback is null");
                }
                _scanCallback = scanCallback!;
                _scanner.StartScan(null, settings, scanCallback);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _isScanning = false;
                throw new BluetoothException($"Failed to start scanning: {ex.Message}", ex);
            }
        }

        public async Task StopScanningAsync()
        {
            if (_scanner != null && _scanCallback != null)
            {
                _scanner.StopScan(_scanCallback);
                _scanner = null;
                _scanCallback = null;
            }
            _isScanning = false;
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothDevice>> GetDiscoveredDevicesAsync()
        {
            await Task.CompletedTask;
            return _discoveredDevices;
        }

        public async Task<bool> ConnectToDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            try
            {
                if (_bluetoothAdapter == null)
                {
                    throw new Exception("Bluetooth adapter is not available");
                }

                var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
                if (androidDevice == null)
                {
                    throw new Exception("Device not found");
                }

                // Connect to GATT server
                var gattCallback = new CustomGattCallback
                {
                    OnConnectionStateChangedCallback = (gatt, status, newState) =>
                    {
                        if (newState == ProfileState.Connected)
                        {
                            device.IsConnected = true;
                        }
                        else if (newState == ProfileState.Disconnected)
                        {
                            device.IsConnected = false;
                        }
                    }
                };

                var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, gattCallback);
                if (gatt == null)
                {
                    throw new Exception("Failed to connect to GATT server");
                }

                _connectedDevices[device.Address] = device;
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                throw new BluetoothException($"Failed to connect to device: {ex.Message}", ex);
            }
        }

        public async Task DisconnectFromDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            if (_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                // In Android, we don't need to explicitly disconnect the device
                // The GATT connection will be managed by the system
                _connectedDevices.Remove(device.Address);
                device.IsConnected = false;
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothService>> GetServicesAsync(BleLib.Models.BluetoothDevice device)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (_bluetoothAdapter == null)
            {
                throw new BluetoothException("Bluetooth adapter is not available");
            }

            var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (androidDevice == null)
            {
                throw new BluetoothException("Failed to get remote device");
            }

            var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, new CustomGattCallback());
            if (gatt == null)
            {
                throw new BluetoothException("Failed to connect to GATT server");
            }

            var services = new List<BleLib.Models.BluetoothService>();
            var gattServices = gatt.Services;

            if (gattServices != null)
            {
                foreach (var gattService in gattServices)
                {
                    if (gattService?.Uuid != null)
                    {
                        var service = new BleLib.Models.BluetoothService
                        {
                            ServiceUuid = Guid.Parse(gattService.Uuid.ToString()),
                            Name = gattService.Uuid.ToString()
                        };

                        var characteristics = gattService.Characteristics;
                        if (characteristics != null)
                        {
                            foreach (var characteristic in characteristics)
                            {
                                if (characteristic?.Uuid != null)
                                {
                                    service.Characteristics.Add(new BluetoothCharacteristic
                                    {
                                        CharacteristicUuid = Guid.Parse(characteristic.Uuid.ToString()),
                                        Name = characteristic.Uuid.ToString(),
                                        CanRead = (characteristic.Properties & GattProperty.Read) != 0,
                                        CanWrite = (characteristic.Properties & GattProperty.Write) != 0,
                                        CanNotify = (characteristic.Properties & GattProperty.Notify) != 0
                                    });
                                }
                            }
                        }

                        services.Add(service);
                    }
                }
            }

            await Task.CompletedTask;
            return services;
        }

        public async Task<byte[]> ReadCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (_bluetoothAdapter == null)
            {
                throw new BluetoothException("Bluetooth adapter is not available");
            }

            var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (androidDevice == null)
            {
                throw new BluetoothException("Failed to get remote device");
            }

            var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, new CustomGattCallback());
            if (gatt == null)
            {
                throw new BluetoothException("Failed to connect to GATT server");
            }

            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            var result = await Task.Run(() => gatt.ReadCharacteristic(characteristic));
            if (!result)
            {
                throw new BluetoothException("Failed to read characteristic");
            }

            var value = characteristic.GetValue();
            return value ?? Array.Empty<byte>();
        }

        public async Task WriteCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (_bluetoothAdapter == null)
            {
                throw new BluetoothException("Bluetooth adapter is not available");
            }

            var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (androidDevice == null)
            {
                throw new BluetoothException("Failed to get remote device");
            }

            var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, new CustomGattCallback());
            if (gatt == null)
            {
                throw new BluetoothException("Failed to connect to GATT server");
            }

            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            characteristic.SetValue(data);
            var result = await Task.Run(() => gatt.WriteCharacteristic(characteristic));
            if (!result)
            {
                throw new BluetoothException("Failed to write characteristic");
            }
        }

        public async Task SubscribeToCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> callback)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (_bluetoothAdapter == null)
            {
                throw new BluetoothException("Bluetooth adapter is not available");
            }

            var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (androidDevice == null)
            {
                throw new BluetoothException("Failed to get remote device");
            }

            var gattCallback = new CustomGattCallback
            {
                OnCharacteristicChangedCallback = (gatt, characteristic) =>
                {
                    if (characteristic != null)
                    {
                        var value = characteristic.GetValue();
                        if (value != null)
                        {
                            callback(value);
                        }
                    }
                }
            };

            var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, gattCallback);
            if (gatt == null)
            {
                throw new BluetoothException("Failed to connect to GATT server");
            }

            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            var result = await Task.Run(() => gatt.SetCharacteristicNotification(characteristic, true));
            if (!result)
            {
                throw new BluetoothException("Failed to subscribe to characteristic");
            }
        }

        public async Task UnsubscribeFromCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (_bluetoothAdapter == null)
            {
                throw new BluetoothException("Bluetooth adapter is not available");
            }

            var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (androidDevice == null)
            {
                throw new BluetoothException("Failed to get remote device");
            }

            var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, new CustomGattCallback());
            if (gatt == null)
            {
                throw new BluetoothException("Failed to connect to GATT server");
            }

            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                throw new BluetoothException("Service not found");
            }

            var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
            {
                throw new BluetoothException("Characteristic not found");
            }

            var result = await Task.Run(() => gatt.SetCharacteristicNotification(characteristic, false));
            if (!result)
            {
                throw new BluetoothException("Failed to unsubscribe from characteristic");
            }
        }

        private void ParseAdvertisementData(Android.Bluetooth.LE.ScanRecord scanRecord, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Parse all advertisement data from raw bytes
                var rawData = scanRecord.GetBytes();
                if (rawData != null)
                {
                    ParseManufacturerDataFromRawData(rawData, device);
                    ParseServiceUuidsFromRawData(rawData, device);
                    ParseServiceDataFromRawData(rawData, device);
                    ParseTxPowerFromRawData(rawData, device);
                }
            }
            catch (Exception ex)
            {
                // Log parsing errors but don't fail the scan
                System.Diagnostics.Debug.WriteLine($"Error parsing advertisement data: {ex.Message}");
            }
        }

        private void ParseManufacturerDataFromRawData(byte[] rawData, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Manual parsing of manufacturer data from raw advertisement data
                int offset = 0;
                while (offset < rawData.Length)
                {
                    if (offset + 1 >= rawData.Length) break;
                    
                    var length = rawData[offset];
                    if (length == 0 || offset + length + 1 > rawData.Length) break;
                    
                    var type = rawData[offset + 1];
                    
                    // Manufacturer Specific Data (0xFF)
                    if (type == 0xFF && length >= 3)
                    {
                        var companyIdBytes = new byte[2];
                        Array.Copy(rawData, offset + 2, companyIdBytes, 0, 2);
                        var companyId = BitConverter.ToUInt16(companyIdBytes, 0);
                        
                        var manufacturerData = new byte[length - 3];
                        Array.Copy(rawData, offset + 4, manufacturerData, 0, manufacturerData.Length);
                        
                        device.ManufacturerData[companyId] = manufacturerData;
                    }
                    
                    offset += length + 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing manufacturer data: {ex.Message}");
            }
        }

        private void ParseServiceDataFromRawData(byte[] rawData, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Manual parsing of service data from raw advertisement data
                // This is a simplified implementation - you may need to enhance based on your needs
                int offset = 0;
                while (offset < rawData.Length)
                {
                    if (offset + 1 >= rawData.Length) break;
                    
                    var length = rawData[offset];
                    if (length == 0 || offset + length + 1 > rawData.Length) break;
                    
                    var type = rawData[offset + 1];
                    
                    // Service Data (0x16 for 16-bit UUID, 0x20 for 32-bit UUID, 0x21 for 128-bit UUID)
                    if (type == 0x16 && length >= 3)
                    {
                        var serviceUuidBytes = new byte[2];
                        Array.Copy(rawData, offset + 2, serviceUuidBytes, 0, 2);
                        var serviceUuid = BitConverter.ToUInt16(serviceUuidBytes, 0);
                        
                        var serviceData = new byte[length - 3];
                        Array.Copy(rawData, offset + 4, serviceData, 0, serviceData.Length);
                        
                        // Convert 16-bit UUID to full GUID format
                        var fullUuid = new Guid($"0000{serviceUuid:X4}-0000-1000-8000-00805f9b34fb");
                        device.ServiceData[fullUuid] = serviceData;
                    }
                    
                    offset += length + 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing service data: {ex.Message}");
            }
        }

        private void ParseTxPowerFromRawData(byte[] rawData, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Manual parsing of TX power from raw advertisement data
                int offset = 0;
                while (offset < rawData.Length)
                {
                    if (offset + 1 >= rawData.Length) break;
                    
                    var length = rawData[offset];
                    if (length == 0 || offset + length + 1 > rawData.Length) break;
                    
                    var type = rawData[offset + 1];
                    
                    // TX Power Level (0x0A)
                    if (type == 0x0A && length >= 2)
                    {
                        device.TxPowerLevel = (sbyte)rawData[offset + 2];
                        break;
                    }
                    
                    offset += length + 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing TX power: {ex.Message}");
            }
        }

        private void ParseServiceUuidsFromRawData(byte[] rawData, BleLib.Models.BluetoothDevice device)
        {
            try
            {
                // Parse service UUIDs from raw advertisement data
                int offset = 0;
                while (offset < rawData.Length)
                {
                    if (offset + 1 >= rawData.Length) break;
                    
                    var length = rawData[offset];
                    if (length == 0 || offset + length + 1 > rawData.Length) break;
                    
                    var type = rawData[offset + 1];
                    
                    // Service UUID (0x02 for 16-bit UUID, 0x03 for 32-bit UUID, 0x04 for 128-bit UUID)
                    if (type == 0x02 && length >= 2)
                    {
                        var serviceUuidBytes = new byte[2];
                        Array.Copy(rawData, offset + 2, serviceUuidBytes, 0, 2);
                        var serviceUuid = BitConverter.ToUInt16(serviceUuidBytes, 0);
                        
                        if (Guid.TryParse($"{serviceUuid:X4}-0000-1000-8000-00805f9b34fb", out var guid))
                        {
                            device.ServiceUuids.Add(guid);
                        }
                    }
                    
                    offset += length + 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing service UUIDs: {ex.Message}");
            }
        }
    }

    internal class CustomScanCallback : Android.Bluetooth.LE.ScanCallback
    {
        public Action<Android.Bluetooth.LE.ScanCallbackType, Android.Bluetooth.LE.ScanResult>? OnScanResultCallback { get; set; }
        public override void OnScanResult(Android.Bluetooth.LE.ScanCallbackType callbackType, Android.Bluetooth.LE.ScanResult? result)
        {
            if (result != null)
            {
                OnScanResultCallback?.Invoke(callbackType, result);
            }
        }
    }

    internal class CustomGattCallback : Android.Bluetooth.BluetoothGattCallback
    {
        public Action<Android.Bluetooth.BluetoothGatt, Android.Bluetooth.GattStatus, Android.Bluetooth.ProfileState>? OnConnectionStateChangedCallback { get; set; }
        public Action<Android.Bluetooth.BluetoothGatt, Android.Bluetooth.BluetoothGattCharacteristic>? OnCharacteristicChangedCallback { get; set; }
        public override void OnConnectionStateChange(Android.Bluetooth.BluetoothGatt? gatt, Android.Bluetooth.GattStatus status, Android.Bluetooth.ProfileState newState)
        {
            if (gatt != null)
            {
                OnConnectionStateChangedCallback?.Invoke(gatt, status, newState);
            }
        }
        public override void OnCharacteristicChanged(Android.Bluetooth.BluetoothGatt? gatt, Android.Bluetooth.BluetoothGattCharacteristic? characteristic)
        {
            if (gatt != null && characteristic != null)
            {
                OnCharacteristicChangedCallback?.Invoke(gatt, characteristic);
            }
        }
    }
}
#endif