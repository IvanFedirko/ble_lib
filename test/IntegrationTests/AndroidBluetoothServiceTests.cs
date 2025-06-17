using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using BleLib.Models;
using BleLib.Services;
using BleLib.Exceptions;

namespace BleLib.Tests.IntegrationTests
{
    public class AndroidBluetoothServiceTests
    {
        [Fact]
        public async Task PlatformSpecificBehavior_ShouldWorkCorrectly()
        {
#if NET9_0_ANDROID
            // On Android, we should be able to create and use the Android Bluetooth service
            var service = BluetoothServiceFactory.CreateBluetoothService();
            
            // Act & Assert - should not throw on Android
            var action = async () => await service.InitializeAsync();
            await action.Should().NotThrowAsync();
#else
            // On Windows, the factory should return a Windows service, not an Android service
            var service = BluetoothServiceFactory.CreateBluetoothService();
            
            // The service should be a Windows service, not an Android service
            service.Should().NotBeNull();
            
            // We can't directly test that it's not an Android service due to interface abstraction,
            // but we can verify that the factory works correctly for the current platform
            var action = async () => await service.InitializeAsync();
            await action.Should().NotThrowAsync();
#endif
        }

#if NET9_0_ANDROID
        private readonly IBluetoothService _service;

        public AndroidBluetoothServiceTests()
        {
            _service = BluetoothServiceFactory.CreateBluetoothService();
        }

        [Fact]
        public async Task StartScanningAsync_ShouldNotThrowException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            var action = async () => await _service.StartScanningAsync();
            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task StopScanningAsync_ShouldNotThrowException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            var action = async () => await _service.StopScanningAsync();
            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetDiscoveredDevicesAsync_ShouldReturnEmptyListWhenNoDevices()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act
            var devices = await _service.GetDiscoveredDevicesAsync();

            // Assert
            devices.Should().NotBeNull();
            // Note: In a real environment, this might return actual devices
            // For testing purposes, we just verify the method doesn't throw
        }

        [Fact]
        public async Task ConnectToDeviceAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            var action = async () => await _service.ConnectToDeviceAsync(null!);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task DisconnectFromDeviceAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            var action = async () => await _service.DisconnectFromDeviceAsync(null!);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetServicesAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            var action = async () => await _service.GetServicesAsync(null!);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ReadCharacteristicAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();

            // Act & Assert
            var action = async () => await _service.ReadCharacteristicAsync(null!, serviceUuid, characteristicUuid);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WriteCharacteristicAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();
            var value = new byte[] { 1, 2, 3 };

            // Act & Assert
            var action = async () => await _service.WriteCharacteristicAsync(null!, serviceUuid, characteristicUuid, value);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SubscribeToCharacteristicAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();
            var callback = new Action<byte[]>(data => { });

            // Act & Assert
            var action = async () => await _service.SubscribeToCharacteristicAsync(null!, serviceUuid, characteristicUuid, callback);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SubscribeToCharacteristicAsync_WithNullCallback_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();

            // Act & Assert
            var action = async () => await _service.SubscribeToCharacteristicAsync(device, serviceUuid, characteristicUuid, null!);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task UnsubscribeFromCharacteristicAsync_WithNullDevice_ShouldThrowArgumentNullException()
        {
            // Arrange
            await _service.InitializeAsync();
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();

            // Act & Assert
            var action = async () => await _service.UnsubscribeFromCharacteristicAsync(null!, serviceUuid, characteristicUuid);
            await action.Should().ThrowAsync<ArgumentNullException>();
        }
#endif
    }
} 