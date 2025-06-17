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
        Task<byte[]> ReadCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid);
        Task WriteCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, byte[] data);
        Task SubscribeToCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid, Action<byte[]> callback);
        Task UnsubscribeFromCharacteristicAsync(BluetoothDevice device, Guid serviceUuid, Guid characteristicUuid);
    }
} 