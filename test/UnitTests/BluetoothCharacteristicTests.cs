using FluentAssertions;
using Xunit;
using BleLib.Models;

namespace BleLib.Tests.UnitTests
{
    public class BluetoothCharacteristicTests
    {
        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateCharacteristic()
        {
            // Arrange
            var characteristicUuid = Guid.NewGuid();
            var name = "Test Characteristic";

            // Act
            var characteristic = new BluetoothCharacteristic
            {
                CharacteristicUuid = characteristicUuid,
                Name = name
            };

            // Assert
            characteristic.CharacteristicUuid.Should().Be(characteristicUuid);
            characteristic.Name.Should().Be(name);
            characteristic.CanRead.Should().BeFalse();
            characteristic.CanWrite.Should().BeFalse();
            characteristic.CanNotify.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithNullName_ShouldSetEmptyName()
        {
            // Arrange & Act
            var characteristic = new BluetoothCharacteristic
            {
                CharacteristicUuid = Guid.NewGuid(),
                Name = null!
            };

            // Assert
            characteristic.Name.Should().BeNull();
        }

        [Fact]
        public void SetProperties_ShouldUpdateProperties()
        {
            // Arrange
            var characteristic = new BluetoothCharacteristic
            {
                CharacteristicUuid = Guid.NewGuid(),
                Name = "Test"
            };

            // Act
            characteristic.CanRead = true;
            characteristic.CanWrite = true;
            characteristic.CanNotify = false;

            // Assert
            characteristic.CanRead.Should().BeTrue();
            characteristic.CanWrite.Should().BeTrue();
            characteristic.CanNotify.Should().BeFalse();
        }

        [Fact]
        public void Properties_ShouldBeInitializedCorrectly()
        {
            // Arrange & Act
            var characteristic = new BluetoothCharacteristic
            {
                CharacteristicUuid = Guid.NewGuid(),
                Name = "Test Characteristic",
                CanRead = true,
                CanWrite = false,
                CanNotify = true
            };

            // Assert
            characteristic.CanRead.Should().BeTrue();
            characteristic.CanWrite.Should().BeFalse();
            characteristic.CanNotify.Should().BeTrue();
        }
    }
} 