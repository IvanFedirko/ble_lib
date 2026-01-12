using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BleLib.Models;

namespace BleLib.Services
{
    public interface IBluetoothService
    {
        Task<bool> InitializeAsync();
        Task<bool> StartScanningAsync();
        Task StopScanningAsync();
        Task<IEnumerable<BluetoothDevice>> GetDiscoveredDevicesAsync();
        Task<bool> ConnectToDeviceAsync(BluetoothDevice device);
        Task DisconnectFromDeviceAsync(BluetoothDevice device);
        Task<IEnumerable<BluetoothService>> GetServicesAsync(BluetoothDevice device);
        Task<IEnumerable<BluetoothCharacteristic>> GetCharacteristicsAsync(BluetoothDevice device, Guid serviceUuid);
        Task<int> RequestMtuAsync(BluetoothDevice device, int mtu);
        Task<byte[]> ReadCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid);
        Task WriteCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data);
        Task WriteCharacteristicWithoutResponseAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data);
        Task<byte[]> ReadDescriptorAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Guid descriptorUuid);
        Task WriteDescriptorAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Guid descriptorUuid, byte[] data);
        Task SubscribeToCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> callback);
        Task UnsubscribeFromCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid);
        Task<string> GetBluetoothStateAsync();
    }
} 