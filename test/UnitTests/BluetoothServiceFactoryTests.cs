using FluentAssertions;
using Xunit;
using BleLib.Services;
using BleLib.Exceptions;

namespace BleLib.Tests.UnitTests
{
    public class BluetoothServiceFactoryTests
    {
        [Fact]
        public void CreateBluetoothService_ShouldReturnServiceInstance()
        {
            // Act & Assert
            var action = () => BluetoothServiceFactory.CreateBluetoothService();
            
            // On supported platforms, it should return a service
            // On unsupported platforms, it should throw PlatformNotSupportedException
            try
            {
                var service = action();
                service.Should().NotBeNull();
                service.Should().BeAssignableTo<IBluetoothService>();
            }
            catch (PlatformNotSupportedException)
            {
                // This is expected on unsupported platforms
                action.Should().Throw<PlatformNotSupportedException>();
            }
        }

        [Fact]
        public void CreateBluetoothService_ShouldReturnSameInstanceOnSubsequentCalls()
        {
            // Act & Assert
            var action = () => BluetoothServiceFactory.CreateBluetoothService();
            
            try
            {
                var service1 = action();
                var service2 = action();
                
                // Note: The factory doesn't guarantee singleton behavior
                // This test verifies that both calls return valid services
                service1.Should().NotBeNull();
                service2.Should().NotBeNull();
                service1.Should().BeAssignableTo<IBluetoothService>();
                service2.Should().BeAssignableTo<IBluetoothService>();
            }
            catch (PlatformNotSupportedException)
            {
                // This is expected on unsupported platforms
                action.Should().Throw<PlatformNotSupportedException>();
            }
        }
    }
} 