using FluentAssertions;
using Xunit;
using BleLib.Models;

namespace BleLib.Tests.UnitTests
{
    public class BluetoothServiceTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateService()
        {
            // Arrange
            var serviceUuid = Guid.NewGuid();
            var name = "Test Service";

            // Act
            var service = new BluetoothService
            {
                ServiceUuid = serviceUuid,
                Name = name
            };

            // Assert
            service.ServiceUuid.Should().Be(serviceUuid);
            service.Name.Should().Be(name);
            service.Characteristics.Should().NotBeNull();
            service.Characteristics.Should().BeEmpty();
        }

        [Fact]
        public void AddCharacteristic_ShouldAddToCharacteristicsList()
        {
            // Arrange
            var service = new BluetoothService
            {
                ServiceUuid = Guid.NewGuid(),
                Name = "Test Service"
            };
            var characteristic = new BluetoothCharacteristic
            {
                CharacteristicUuid = Guid.NewGuid(),
                Name = "test-char"
            };

            // Act
            service.Characteristics.Add(characteristic);

            // Assert
            service.Characteristics.Should().ContainSingle();
            service.Characteristics.Should().Contain(characteristic);
        }

        [Fact]
        public void AddCharacteristic_WithNullCharacteristic_ShouldAllowNull()
        {
            // Arrange
            var service = new BluetoothService
            {
                ServiceUuid = Guid.NewGuid(),
                Name = "Test Service"
            };

            // Act
            service.Characteristics.Add(null!);

            // Assert
            service.Characteristics.Should().ContainSingle();
            service.Characteristics.Should().Contain((BluetoothCharacteristic?)null);
        }
    }
} 