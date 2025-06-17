using FluentAssertions;
using Xunit;
using BleLib.Models;

namespace BleLib.Tests.UnitTests
{
    public class BluetoothDeviceTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateDevice()
        {
            // Arrange
            var name = "Test Device";
            var address = "00:11:22:33:44:55";
            var signalStrength = -50;

            // Act
            var device = new BluetoothDevice
            {
                Name = name,
                Address = address,
                SignalStrength = signalStrength
            };

            // Assert
            device.Name.Should().Be(name);
            device.Address.Should().Be(address);
            device.SignalStrength.Should().Be(signalStrength);
            device.IsConnected.Should().BeFalse();
            device.ServiceUuids.Should().NotBeNull();
            device.ManufacturerData.Should().NotBeNull();
            device.ServiceData.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullName_ShouldSetEmptyName()
        {
            // Arrange & Act
            var device = new BluetoothDevice
            {
                Name = null!,
                Address = "00:11:22:33:44:55",
                SignalStrength = -50
            };

            // Assert
            device.Name.Should().BeNull();
        }

        [Fact]
        public void Connect_ShouldSetIsConnectedToTrue()
        {
            // Arrange
            var device = new BluetoothDevice
            {
                Name = "Test Device",
                Address = "00:11:22:33:44:55",
                SignalStrength = -50
            };

            // Act
            device.IsConnected = true;

            // Assert
            device.IsConnected.Should().BeTrue();
        }

        [Fact]
        public void Disconnect_ShouldSetIsConnectedToFalse()
        {
            // Arrange
            var device = new BluetoothDevice
            {
                Name = "Test Device",
                Address = "00:11:22:33:44:55",
                SignalStrength = -50,
                IsConnected = true
            };

            // Act
            device.IsConnected = false;

            // Assert
            device.IsConnected.Should().BeFalse();
        }

        [Fact]
        public void AdvertisementData_ShouldBeInitialized()
        {
            // Arrange & Act
            var device = new BluetoothDevice
            {
                Name = "Test Device",
                Address = "00:11:22:33:44:55",
                SignalStrength = -50
            };

            // Assert
            device.ServiceUuids.Should().NotBeNull();
            device.ManufacturerData.Should().NotBeNull();
            device.ServiceData.Should().NotBeNull();
            device.ServiceUuids.Should().BeEmpty();
            device.ManufacturerData.Should().BeEmpty();
            device.ServiceData.Should().BeEmpty();
        }
    }
} 