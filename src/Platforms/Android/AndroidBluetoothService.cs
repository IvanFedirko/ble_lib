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
using Android.Content.PM;

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
        private Dictionary<string, BluetoothGatt> _deviceGatts = new();
        private Dictionary<string, Dictionary<Guid, Action<byte[]>>> _characteristicCallbacks = new();
        private TaskCompletionSource<bool>? _pendingDescriptorWrite;

        public AndroidBluetoothService()
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] AndroidBluetoothService constructor called");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] AndroidBluetoothService constructor - GetType(): {this.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] AndroidBluetoothService constructor - Assembly: {this.GetType().Assembly.FullName}");
            _connectedDevices = new Dictionary<string, BleLib.Models.BluetoothDevice>();
            _discoveredDevices = new List<BleLib.Models.BluetoothDevice>();
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] AndroidBluetoothService constructor completed");
        }

        public async Task<bool> InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] InitializeAsync called");
            try
            {
                if (_bluetoothAdapter == null)
                {
                    throw new BluetoothException("Bluetooth is not supported on this device");
                }

                // Check if Bluetooth is enabled
                if (!_bluetoothAdapter.IsEnabled)
                {
                    throw new BluetoothException("Bluetooth is not enabled. Please enable Bluetooth in device settings.");
                }

                // Request necessary permissions
                var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    throw new BluetoothException("Location permission is required for Bluetooth scanning");
                }

                // Check for Bluetooth permissions (Android 12+)
                if (OperatingSystem.IsAndroidVersionAtLeast(31)) // Android 12 (API 31)
                {
                    // For Android 12+, we need to check runtime permissions
                    // These are handled by the Android manifest permissions
                    // The actual permission checks happen at runtime
                }

                // Verify Bluetooth LE scanner is available
                if (_bluetoothAdapter.BluetoothLeScanner == null)
                {
                    throw new BluetoothException("Bluetooth LE Scanner is not available on this device");
                }

                System.Diagnostics.Debug.WriteLine("Android Bluetooth Service: Initialization successful");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: Initialization failed - {ex.Message}");
                throw new BluetoothException($"Failed to initialize Bluetooth: {ex.Message}", ex);
            }
        }

        // Test method to verify the service is working
        public string TestMethod()
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] TestMethod called");
            return "Android Bluetooth Service is working!";
        }

        public async Task<bool> StartScanningAsync()
        {
            if (_isScanning)
                return true;

            try
            {
                // Double-check Bluetooth state
                if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
                {
                    throw new BluetoothException("Bluetooth is not enabled");
                }

                _isScanning = true;
                _discoveredDevices.Clear();

                if (_bluetoothAdapter.BluetoothLeScanner == null)
                {
                    throw new BluetoothException("Bluetooth LE Scanner is not available");
                }
                
                _scanner = _bluetoothAdapter.BluetoothLeScanner!;
                
                // Create scan settings with more permissive settings
                 var settings = new ScanSettings.Builder()
                    .SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency)
                    .SetReportDelay(0) // Report results immediately
                    .Build();
    
                // Create scan filters (optional - can be null to scan for all devices)
                var filters = new List<ScanFilter>();
                // You can add specific filters here if needed, e.g.:
                // filters.Add(new ScanFilter.Builder().SetServiceUuid(ParcelUuid.FromString("0000180f-0000-1000-8000-00805f9b34fb")).Build());

                var scanCallback = new CustomScanCallback
                {
                    OnScanResultCallback = (callbackType, result) =>
                    {
                        try
                        {
                            if (result?.Device != null && !string.IsNullOrEmpty(result.Device.Address))
                            {
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Scan result: {result.Device.Address} {result.Device.Name ?? "Unknown"} RSSI: {result.Rssi}");
                            var device = new BleLib.Models.BluetoothDevice
                            {
                                Name = result.Device.Name ?? "Unknown Device",
                                Address = result.Device.Address,
                                SignalStrength = result.Rssi,
                                LastSeen = DateTime.Now,
                                    IsConnectable = result.IsConnectable,
                                RawAdvertisementData = result.ScanRecord?.GetBytes() ?? Array.Empty<byte>()
                            };

                            // Parse advertisement data
                            if (result.ScanRecord != null)
                            {
                                ParseAdvertisementData(result.ScanRecord, device);
                            }

                                // Check if device already exists
                            var existingDevice = _discoveredDevices.FirstOrDefault(d => d.Address == device.Address);
                            if (existingDevice != null)
                            {
                                    // Update existing device
                                existingDevice.SignalStrength = device.SignalStrength;
                                existingDevice.LastSeen = device.LastSeen;
                                    existingDevice.IsConnectable = device.IsConnectable;
                                    
                                // Update advertisement data if available
                                if (device.ServiceUuids.Count > 0)
                                    existingDevice.ServiceUuids = device.ServiceUuids;
                                if (device.ManufacturerData.Count > 0)
                                    existingDevice.ManufacturerData = device.ManufacturerData;
                                if (device.ServiceData.Count > 0)
                                    existingDevice.ServiceData = device.ServiceData;
                                if (device.RawAdvertisementData.Length > 0)
                                    existingDevice.RawAdvertisementData = device.RawAdvertisementData;
                                    
                                  //  System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: Updated device {device.Name} ({device.Address})");
                            }
                            else
                            {
                                    // Add new device
                                _discoveredDevices.Add(device);
                                    System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: New device discovered {device.Name} ({device.Address}) RSSI: {device.SignalStrength}");
                            }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: Error processing scan result: {ex.Message}");
                        }
                    }
                };

                if (scanCallback == null)
                {
                    throw new BluetoothException("Failed to create scan callback");
                }
                
                _scanCallback = scanCallback;
                
                // Start scanning with filters (null for all devices) and settings
                _scanner.StartScan(filters, settings, scanCallback);
                
                System.Diagnostics.Debug.WriteLine("Android Bluetooth Service: Scan started successfully");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _isScanning = false;
                System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: Failed to start scanning - {ex.Message}");
                throw new BluetoothException($"Failed to start scanning: {ex.Message}", ex);
            }
        }

        public async Task StopScanningAsync()
        {
            try
        {
            if (_scanner != null && _scanCallback != null)
            {
                _scanner.StopScan(_scanCallback);
                    System.Diagnostics.Debug.WriteLine("Android Bluetooth Service: Scan stopped successfully");
                }
                _scanner = null;
                _scanCallback = null;
            _isScanning = false;
            await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: Error stopping scan - {ex.Message}");
                _isScanning = false;
                throw new BluetoothException($"Failed to stop scanning: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothDevice>> GetDiscoveredDevicesAsync()
        {
            await Task.CompletedTask;
            return _discoveredDevices;
        }

        public async Task<string> GetBluetoothStateAsync()
        {
            try
            {
                if (_bluetoothAdapter == null)
                {
                    return "Bluetooth not supported";
                }

                var state = _bluetoothAdapter.State;
                var isEnabled = _bluetoothAdapter.IsEnabled;
                var hasLeScanner = _bluetoothAdapter.BluetoothLeScanner != null;
                
                var stateInfo = $"Adapter State: {state}, Enabled: {isEnabled}, LE Scanner Available: {hasLeScanner}";
                
                // Check permissions
                var locationPermission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                stateInfo += $", Location Permission: {locationPermission}";
                
                System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: {stateInfo}");
                return stateInfo;
            }
            catch (Exception ex)
            {
                var errorInfo = $"Error getting Bluetooth state: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Android Bluetooth Service: {errorInfo}");
                return errorInfo;
            }
        }

        public bool IsDeviceConnected(BleLib.Models.BluetoothDevice device)
        {
            var isConnected = _connectedDevices.ContainsKey(device.Address);
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] IsDeviceConnected({device.Address}) = {isConnected}");
            return isConnected;
        }

        public async Task<bool> ConnectToDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] ===== ConnectToDeviceAsync CALLED for {device.Address} =====");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] ConnectToDeviceAsync called for {device.Address}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Stack trace: {Environment.StackTrace}");
                
                if (_bluetoothAdapter == null)
                {
                    throw new Exception("Bluetooth adapter is not available");
                }
                if (IsDeviceConnected(device))
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Already connected to {device.Address}, skipping ConnectGatt.");
                    return true;
                }
                var androidDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
                if (androidDevice == null)
                {
                    throw new Exception("Device not found");
                }
                var tcs = new TaskCompletionSource<bool>();
                var gattCallback = new CustomGattCallback
                {
                    OnConnectionStateChangedCallback = (gatt, status, newState) =>
                    {
                        if (newState == ProfileState.Connected)
                        {
                            device.IsConnected = true;
                            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Connected to device {device.Address}, starting service discovery...");
                            gatt.DiscoverServices();
                        }
                        else if (newState == ProfileState.Disconnected)
                        {
                            device.IsConnected = false;
                        }
                    },
                    OnServicesDiscoveredCallback = (g, status) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[AndroidBLE] onServicesDiscovered: status={status}");
                        tcs.TrySetResult(true);
                    },
                    OnCharacteristicChangedCallback = (gatt, characteristic) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[AndroidBLE] OnCharacteristicChanged called for characteristic {characteristic.Uuid}");
                        System.Diagnostics.Debug.WriteLine($"[AndroidBLE] OnCharacteristicChanged - device address: {gatt?.Device?.Address}");
                        System.Diagnostics.Debug.WriteLine($"[AndroidBLE] OnCharacteristicChanged - characteristic value: {characteristic.GetValue()?.Length ?? 0} bytes");
                        
                        // Handle notifications for all subscribed characteristics
                        if (_characteristicCallbacks.TryGetValue(device.Address, out var callbacks))
                        {
                            var characteristicUuid = Guid.Parse(characteristic.Uuid.ToString());
                            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Looking for callback for characteristic {characteristicUuid}");
                            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Available callbacks for device {device.Address}: {string.Join(", ", callbacks.Keys)}");
                            
                            if (callbacks.TryGetValue(characteristicUuid, out var callback))
                            {
                                var value = characteristic.GetValue();
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Found callback, invoking with {value?.Length} bytes");
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Callback data: {BitConverter.ToString(value ?? Array.Empty<byte>())}");
                                callback?.Invoke(value);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] No callback found for characteristic {characteristicUuid}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] No callbacks dictionary found for device {device.Address}");
                        }
                    },
                    OnDescriptorWriteCallback = (gattObj, descriptor, status) =>
                    {
                        if (_pendingDescriptorWrite != null)
                        {
                            if (status == GattStatus.Success)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] OnDescriptorWrite: Success");
                                _pendingDescriptorWrite.TrySetResult(true);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] OnDescriptorWrite: Failed with status {status}");
                                _pendingDescriptorWrite.TrySetException(new Exception($"Descriptor write failed: {status}"));
                            }
                            _pendingDescriptorWrite = null;
                        }
                    }
                };
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] About to call ConnectGatt for {device.Address}");
                var gatt = androidDevice.ConnectGatt(Android.App.Application.Context, false, gattCallback);
                if (gatt == null)
                {
                    throw new Exception("Failed to connect to GATT server");
                }
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] ConnectGatt completed for {device.Address}, waiting for service discovery...");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Waiting for service discovery to complete for {device.Address}...");
                await tcs.Task; // Wait for onServicesDiscovered
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Service discovery complete for {device.Address}");
                _connectedDevices[device.Address] = device;
                _deviceGatts[device.Address] = gatt;
                _characteristicCallbacks[device.Address] = new Dictionary<Guid, Action<byte[]>>();
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Stored GATT for {device.Address}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] ===== ConnectToDeviceAsync COMPLETED for {device.Address} =====");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] ConnectToDeviceAsync failed for {device.Address}: {ex.Message}");
                throw new BluetoothException($"Failed to connect to device: {ex.Message}", ex);
            }
        }

        public async Task DisconnectFromDeviceAsync(BleLib.Models.BluetoothDevice device)
        {
            if (_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                _connectedDevices.Remove(device.Address);
                device.IsConnected = false;
            }
            if (_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                gatt.Close();
                gatt.Dispose();
                _deviceGatts.Remove(device.Address);
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Disposed GATT for {device.Address}");
            }
            if (_characteristicCallbacks.TryGetValue(device.Address, out var callbacks))
            {
                _characteristicCallbacks.Remove(device.Address);
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Cleaned up callbacks for {device.Address}");
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothService>> GetServicesAsync(BleLib.Models.BluetoothDevice device)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }
            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for service discovery: {device.Address}");
            var services = new List<BleLib.Models.BluetoothService>();
            var gattServices = gatt.Services;
            if (gattServices != null)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Discovered {gattServices.Count} services:");
                foreach (var gattService in gattServices)
                {
                    if (gattService?.Uuid != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Service: {gattService.Uuid}");
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
                                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE]   Characteristic: {characteristic.Uuid} Props: {characteristic.Properties}");
                                    service.Characteristics.Add(new BluetoothCharacteristic
                                    {
                                        CharacteristicUuid = Guid.Parse(characteristic.Uuid.ToString()),
                                        Name = characteristic.Uuid.ToString(),
                                        CanRead = (characteristic.Properties & GattProperty.Read) != 0,
                                        CanWrite = (characteristic.Properties & GattProperty.Write) != 0,
                                        CanWriteWithoutResponse = (characteristic.Properties & GattProperty.WriteNoResponse) != 0,
                                        CanNotify = (characteristic.Properties & GattProperty.Notify) != 0
                                    });
                                }
                            }
                        }
                        services.Add(service);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[AndroidBLE] No services discovered.");
            }
            await Task.CompletedTask;
            return services;
        }

        public async Task<IEnumerable<BleLib.Models.BluetoothCharacteristic>> GetCharacteristicsAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }
            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for characteristic discovery: {device.Address}");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Available services in GATT for {device.Address}:");
            foreach (var s in gatt.Services)
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE]   Service: {s.Uuid}");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Looking for service: {serviceUuid}");
            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Service {serviceUuid} not found in GATT for {device.Address}");
                throw new BluetoothException("Service not found");
            }
            var characteristics = new List<BleLib.Models.BluetoothCharacteristic>();
            var gattCharacteristics = service.Characteristics;
            if (gattCharacteristics != null)
            {
                foreach (var characteristic in gattCharacteristics)
                {
                    if (characteristic?.Uuid != null)
                    {
                        var bluetoothCharacteristic = new BluetoothCharacteristic
                        {
                            CharacteristicUuid = Guid.Parse(characteristic.Uuid.ToString()),
                            Name = characteristic.Uuid.ToString(),
                            CanRead = (characteristic.Properties & GattProperty.Read) != 0,
                            CanWrite = (characteristic.Properties & GattProperty.Write) != 0,
                            CanWriteWithoutResponse = (characteristic.Properties & GattProperty.WriteNoResponse) != 0,
                            CanNotify = (characteristic.Properties & GattProperty.Notify) != 0
                        };
                        // Get descriptors for this characteristic
                        try
                        {
                            var descriptors = characteristic.Descriptors;
                            if (descriptors != null)
                            {
                                foreach (var descriptor in descriptors)
                                {
                                    if (descriptor?.Uuid != null)
                                    {
                                        bluetoothCharacteristic.Descriptors.Add(new BleLib.Models.BluetoothDescriptor
                                        {
                                            DescriptorUuid = Guid.Parse(descriptor.Uuid.ToString()),
                                            Name = descriptor.Uuid.ToString()
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to get descriptors for characteristic {characteristic.Uuid}: {ex.Message}");
                        }
                        characteristics.Add(bluetoothCharacteristic);
                    }
                }
            }
            await Task.CompletedTask;
            return characteristics;
        }

        public async Task<int> RequestMtuAsync(BleLib.Models.BluetoothDevice device, int mtu)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for MTU request: {device.Address}");

            try
            {
                // Request MTU negotiation
                var result = await Task.Run(() => gatt.RequestMtu(mtu));
                if (result)
                {
                    System.Diagnostics.Debug.WriteLine($"MTU negotiation successful: {mtu} bytes");
                    return mtu;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"MTU negotiation failed: {result}");
                    // Return a reasonable default if negotiation fails
                    return Math.Min(mtu, 23);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MTU negotiation error: {ex.Message}");
                // Return a reasonable default if negotiation fails
                return Math.Min(mtu, 23);
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for reading characteristic: {device.Address}");

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

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for writing characteristic: {device.Address}");

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

        public async Task WriteCharacteristicWithoutResponseAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for writing characteristic without response: {device.Address}");

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
            characteristic.WriteType = GattWriteType.NoResponse; // Set write type to Write Without Response
            var result = await Task.Run(() => gatt.WriteCharacteristic(characteristic));
            if (!result)
            {
                throw new BluetoothException("Failed to write characteristic without response");
            }
        }

        public async Task SubscribeToCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> callback)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] METHOD CALLED - SubscribeToCharacteristicAsync");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] TEST - This should appear if the method is called");
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] === SubscribeToCharacteristicAsync ENTRY ===");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] SubscribeToCharacteristicAsync called for {device.Address}, service={serviceUuid}, characteristic={characteristicUuid}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Stack trace: {Environment.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] _connectedDevices count: {_connectedDevices.Count}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] _deviceGatts count: {_deviceGatts.Count}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} in _connectedDevices: {_connectedDevices.ContainsKey(device.Address)}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} in _deviceGatts: {_deviceGatts.ContainsKey(device.Address)}");
                
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} not in _connectedDevices");
                    throw new Exception("Device not connected");
                }
                if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} not in _deviceGatts");
                    throw new Exception("Device not connected (no GATT)");
                }

                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for subscribing to characteristic: {device.Address}");

                // Find the service
                var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Service {serviceUuid} not found in GATT for {device.Address}");
                    throw new Exception("Service not found");
                }

                // Find the characteristic
                var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
                if (characteristic == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Characteristic {characteristicUuid} not found in service {serviceUuid} for {device.Address}");
                    throw new Exception("Characteristic not found");
                }

                // Store the callback for this characteristic
                if (_characteristicCallbacks.TryGetValue(device.Address, out var callbacks))
                {
                    callbacks[characteristicUuid] = callback;
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Stored callback for characteristic {characteristicUuid}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] No callbacks dictionary found for {device.Address}");
                }

                // Enable notifications (don't create new callback or call SetGattCallback)
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Enabling notifications for characteristic {characteristicUuid}");
                bool notificationSet = gatt.SetCharacteristicNotification(characteristic, true);
                if (!notificationSet)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Failed to enable notifications for characteristic {characteristicUuid}");
                    throw new Exception("Failed to enable notifications");
                }

                // Write to CCCD descriptor to enable notifications on the device
                var cccdUuid = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
                var cccd = characteristic.GetDescriptor(cccdUuid);
                if (cccd == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] CCCD descriptor not found for characteristic {characteristicUuid}");
                    throw new Exception("CCCD descriptor not found");
                }
                cccd.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Writing to CCCD for characteristic {characteristicUuid}");
                _pendingDescriptorWrite = new TaskCompletionSource<bool>();
                bool writeResult = gatt.WriteDescriptor(cccd);
                if (!writeResult)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Failed to write CCCD for characteristic {characteristicUuid}");
                    throw new Exception("Failed to write CCCD descriptor");
                }
                await _pendingDescriptorWrite.Task;

                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Subscribed to characteristic {characteristicUuid} for notifications on {device.Address}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] === SubscribeToCharacteristicAsync EXIT ===");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] EXCEPTION in SubscribeToCharacteristicAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Exception stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task UnsubscribeFromCharacteristicAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for unsubscribing from characteristic: {device.Address}");

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

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Unsubscribed from characteristic {characteristicUuid} on {device.Address}");
        }

        public async Task<byte[]> ReadDescriptorAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Guid descriptorUuid)
        {
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for reading descriptor: {device.Address}");

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

            var descriptor = characteristic.GetDescriptor(Java.Util.UUID.FromString(descriptorUuid.ToString()));
            if (descriptor == null)
            {
                throw new BluetoothException("Descriptor not found");
            }

            var result = await Task.Run(() => gatt.ReadDescriptor(descriptor));
            if (!result)
            {
                throw new BluetoothException("Failed to read descriptor");
            }

            var value = descriptor.GetValue();
            return value ?? Array.Empty<byte>();
        }

        public async Task WriteDescriptorAsync(BleLib.Models.BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Guid descriptorUuid, byte[] data)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] WriteDescriptorAsync called for {device.Address}, service={serviceUuid}, characteristic={characteristicUuid}, descriptor={descriptorUuid}");
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Stack trace: {Environment.StackTrace}");
            
            if (!_connectedDevices.TryGetValue(device.Address, out var connectedDevice))
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} not in _connectedDevices");
                throw new BluetoothException("Device not connected");
            }

            if (!_deviceGatts.TryGetValue(device.Address, out var gatt))
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Device {device.Address} not in _deviceGatts");
                throw new BluetoothException("Device not connected (no GATT)");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Using stored GATT for writing descriptor: {device.Address}");

            var service = gatt.GetService(Java.Util.UUID.FromString(serviceUuid.ToString()));
            if (service == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Service {serviceUuid} not found in GATT for {device.Address}");
                throw new BluetoothException("Service not found");
            }

            var characteristic = service.GetCharacteristic(Java.Util.UUID.FromString(characteristicUuid.ToString()));
            if (characteristic == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Characteristic {characteristicUuid} not found in GATT for {device.Address}");
                throw new BluetoothException("Characteristic not found");
            }

            var descriptor = characteristic.GetDescriptor(Java.Util.UUID.FromString(descriptorUuid.ToString()));
            if (descriptor == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Descriptor {descriptorUuid} not found in GATT for {device.Address}");
                throw new BluetoothException("Descriptor not found");
            }

            descriptor.SetValue(data);
            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Writing descriptor {descriptorUuid} with data: {BitConverter.ToString(data)}");
            var result = await Task.Run(() => gatt.WriteDescriptor(descriptor));
            if (!result)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Failed to write descriptor {descriptorUuid}");
                throw new BluetoothException("Failed to write descriptor");
            }

            System.Diagnostics.Debug.WriteLine($"[AndroidBLE] Wrote descriptor {descriptorUuid} on {device.Address}");
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
        public Action<Android.Bluetooth.BluetoothGatt, Android.Bluetooth.GattStatus>? OnServicesDiscoveredCallback { get; set; }
        public Action<BluetoothGatt, BluetoothGattDescriptor, GattStatus>? OnDescriptorWriteCallback { get; set; }
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
        public override void OnServicesDiscovered(Android.Bluetooth.BluetoothGatt? gatt, Android.Bluetooth.GattStatus status)
        {
            OnServicesDiscoveredCallback?.Invoke(gatt, status);
            base.OnServicesDiscovered(gatt, status);
        }
        public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        {
            if (gatt != null && descriptor != null)
                OnDescriptorWriteCallback?.Invoke(gatt, descriptor, status);
        }
    }
}
#endif