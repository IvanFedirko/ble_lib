using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using BleLib.Models;

namespace BleLib.Tests.MockTests
{
    public class AdvertisementDataTests
    {
        [Fact]
        public void Constructor_ShouldInitializeEmptyCollections()
        {
            // Act
            var device = new BluetoothDevice();

            // Assert
            device.ManufacturerData.Should().NotBeNull();
            device.ManufacturerData.Should().BeEmpty();
            device.ServiceUuids.Should().NotBeNull();
            device.ServiceUuids.Should().BeEmpty();
            device.ServiceData.Should().NotBeNull();
            device.ServiceData.Should().BeEmpty();
            device.TxPowerLevel.Should().Be(0);
        }

        [Fact]
        public void SetManufacturerData_ShouldUpdateManufacturerData()
        {
            // Arrange
            var device = new BluetoothDevice();
            var manufacturerData = new Dictionary<ushort, byte[]>
            {
                { 0x004C, new byte[] { 1, 2, 3 } }, // Apple
                { 0x0006, new byte[] { 4, 5, 6 } }  // Microsoft
            };

            // Act
            device.ManufacturerData = manufacturerData;

            // Assert
            device.ManufacturerData.Should().BeEquivalentTo(manufacturerData);
        }

        [Fact]
        public void SetServiceUuids_ShouldUpdateServiceUuids()
        {
            // Arrange
            var device = new BluetoothDevice();
            var serviceUuids = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act
            device.ServiceUuids = serviceUuids;

            // Assert
            device.ServiceUuids.Should().BeEquivalentTo(serviceUuids);
        }

        [Fact]
        public void SetServiceData_ShouldUpdateServiceData()
        {
            // Arrange
            var device = new BluetoothDevice();
            var serviceData = new Dictionary<Guid, byte[]>
            {
                { Guid.NewGuid(), new byte[] { 1, 2, 3 } },
                { Guid.NewGuid(), new byte[] { 4, 5, 6 } }
            };

            // Act
            device.ServiceData = serviceData;

            // Assert
            device.ServiceData.Should().BeEquivalentTo(serviceData);
        }

        [Fact]
        public void SetTxPowerLevel_ShouldUpdateTxPowerLevel()
        {
            // Arrange
            var device = new BluetoothDevice();
            var txPowerLevel = -12;

            // Act
            device.TxPowerLevel = txPowerLevel;

            // Assert
            device.TxPowerLevel.Should().Be(txPowerLevel);
        }

        [Fact]
        public void SetManufacturerData_WithNull_ShouldClearManufacturerData()
        {
            // Arrange
            var device = new BluetoothDevice();
            device.ManufacturerData = new Dictionary<ushort, byte[]> { { 0x004C, new byte[] { 1 } } };

            // Act
            device.ManufacturerData = null!;

            // Assert
            device.ManufacturerData.Should().BeNull();
        }

        [Fact]
        public void SetServiceUuids_WithNull_ShouldClearServiceUuids()
        {
            // Arrange
            var device = new BluetoothDevice();
            device.ServiceUuids = new List<Guid> { Guid.NewGuid() };

            // Act
            device.ServiceUuids = null!;

            // Assert
            device.ServiceUuids.Should().BeNull();
        }

        [Fact]
        public void SetServiceData_WithNull_ShouldClearServiceData()
        {
            // Arrange
            var device = new BluetoothDevice();
            device.ServiceData = new Dictionary<Guid, byte[]> { { Guid.NewGuid(), new byte[] { 1 } } };

            // Act
            device.ServiceData = null!;

            // Assert
            device.ServiceData.Should().BeNull();
        }

        [Fact]
        public void RawAdvertisementData_ShouldBeAccessible()
        {
            // Arrange
            var device = new BluetoothDevice();
            var rawData = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            device.RawAdvertisementData = rawData;

            // Assert
            device.RawAdvertisementData.Should().BeEquivalentTo(rawData);
        }

        [Fact]
        public void IsConnectable_ShouldBeAccessible()
        {
            // Arrange
            var device = new BluetoothDevice();

            // Act
            device.IsConnectable = true;

            // Assert
            device.IsConnectable.Should().BeTrue();
        }
    }
} 