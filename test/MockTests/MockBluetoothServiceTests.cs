using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using BleLib.Services;
using BleLib.Models;
using BleLib.Exceptions;

namespace BleLib.Tests.MockTests
{
    public class MockBluetoothServiceTests
    {
        private readonly Mock<IBluetoothService> _mockService;

        public MockBluetoothServiceTests()
        {
            _mockService = new Mock<IBluetoothService>();
        }

        [Fact]
        public async Task InitializeAsync_ShouldCallServiceMethod()
        {
            // Arrange
            _mockService.Setup(x => x.InitializeAsync()).ReturnsAsync(true);

            // Act
            var result = await _mockService.Object.InitializeAsync();

            // Assert
            result.Should().BeTrue();
            _mockService.Verify(x => x.InitializeAsync(), Times.Once);
        }

        [Fact]
        public async Task StartScanningAsync_ShouldCallServiceMethod()
        {
            // Arrange
            _mockService.Setup(x => x.StartScanningAsync()).ReturnsAsync(true);

            // Act
            var result = await _mockService.Object.StartScanningAsync();

            // Assert
            result.Should().BeTrue();
            _mockService.Verify(x => x.StartScanningAsync(), Times.Once);
        }

        [Fact]
        public async Task StopScanningAsync_ShouldCallServiceMethod()
        {
            // Arrange
            _mockService.Setup(x => x.StopScanningAsync()).Returns(Task.CompletedTask);

            // Act
            await _mockService.Object.StopScanningAsync();

            // Assert
            _mockService.Verify(x => x.StopScanningAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDiscoveredDevicesAsync_ShouldCallServiceMethod()
        {
            // Arrange
            var devices = new List<BluetoothDevice>
            {
                new BluetoothDevice { Name = "Test Device 1", Address = "00:11:22:33:44:55" },
                new BluetoothDevice { Name = "Test Device 2", Address = "00:11:22:33:44:56" }
            };

            _mockService.Setup(x => x.GetDiscoveredDevicesAsync()).ReturnsAsync(devices);

            // Act
            var result = await _mockService.Object.GetDiscoveredDevicesAsync();

            // Assert
            result.Should().HaveCount(2);
            _mockService.Verify(x => x.GetDiscoveredDevicesAsync(), Times.Once);
        }

        [Fact]
        public async Task ConnectToDeviceAsync_WithValidDevice_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            _mockService.Setup(x => x.ConnectToDeviceAsync(device)).ReturnsAsync(true);

            // Act
            var result = await _mockService.Object.ConnectToDeviceAsync(device);

            // Assert
            result.Should().BeTrue();
            _mockService.Verify(x => x.ConnectToDeviceAsync(device), Times.Once);
        }

        [Fact]
        public async Task DisconnectFromDeviceAsync_WithValidDevice_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            _mockService.Setup(x => x.DisconnectFromDeviceAsync(device)).Returns(Task.CompletedTask);

            // Act
            await _mockService.Object.DisconnectFromDeviceAsync(device);

            // Assert
            _mockService.Verify(x => x.DisconnectFromDeviceAsync(device), Times.Once);
        }

        [Fact]
        public async Task GetServicesAsync_WithValidDevice_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var services = new List<BluetoothService>
            {
                new BluetoothService { ServiceUuid = Guid.NewGuid(), Name = "Test Service" }
            };

            _mockService.Setup(x => x.GetServicesAsync(device)).ReturnsAsync(services);

            // Act
            var result = await _mockService.Object.GetServicesAsync(device);

            // Assert
            result.Should().HaveCount(1);
            _mockService.Verify(x => x.GetServicesAsync(device), Times.Once);
        }

        [Fact]
        public async Task ReadCharacteristicAsync_WithValidParameters_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();
            var expectedValue = new byte[] { 1, 2, 3, 4 };

            _mockService.Setup(x => x.ReadCharacteristicAsync(device, serviceUuid, characteristicUuid))
                       .ReturnsAsync(expectedValue);

            // Act
            var result = await _mockService.Object.ReadCharacteristicAsync(device, serviceUuid, characteristicUuid);

            // Assert
            result.Should().BeEquivalentTo(expectedValue);
            _mockService.Verify(x => x.ReadCharacteristicAsync(device, serviceUuid, characteristicUuid), Times.Once);
        }

        [Fact]
        public async Task WriteCharacteristicAsync_WithValidParameters_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();
            var value = new byte[] { 1, 2, 3, 4 };

            _mockService.Setup(x => x.WriteCharacteristicAsync(device, serviceUuid, characteristicUuid, value))
                       .Returns(Task.CompletedTask);

            // Act
            await _mockService.Object.WriteCharacteristicAsync(device, serviceUuid, characteristicUuid, value);

            // Assert
            _mockService.Verify(x => x.WriteCharacteristicAsync(device, serviceUuid, characteristicUuid, value), Times.Once);
        }

        [Fact]
        public async Task SubscribeToCharacteristicAsync_WithValidParameters_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();
            var callback = new Action<byte[]>(data => { });

            _mockService.Setup(x => x.SubscribeToCharacteristicAsync(device, serviceUuid, characteristicUuid, callback))
                       .Returns(Task.CompletedTask);

            // Act
            await _mockService.Object.SubscribeToCharacteristicAsync(device, serviceUuid, characteristicUuid, callback);

            // Assert
            _mockService.Verify(x => x.SubscribeToCharacteristicAsync(device, serviceUuid, characteristicUuid, callback), Times.Once);
        }

        [Fact]
        public async Task UnsubscribeFromCharacteristicAsync_WithValidParameters_ShouldCallServiceMethod()
        {
            // Arrange
            var device = new BluetoothDevice { Name = "Test Device", Address = "00:11:22:33:44:55" };
            var serviceUuid = Guid.NewGuid();
            var characteristicUuid = Guid.NewGuid();

            _mockService.Setup(x => x.UnsubscribeFromCharacteristicAsync(device, serviceUuid, characteristicUuid))
                       .Returns(Task.CompletedTask);

            // Act
            await _mockService.Object.UnsubscribeFromCharacteristicAsync(device, serviceUuid, characteristicUuid);

            // Assert
            _mockService.Verify(x => x.UnsubscribeFromCharacteristicAsync(device, serviceUuid, characteristicUuid), Times.Once);
        }
    }
} 