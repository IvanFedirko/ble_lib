using System;
using BleLib.Services;

#if NET9_0_WINDOWS10_0_19041_0
using BleLib.Platforms.WindowsBLE;
#endif

#if NET9_0_ANDROID
using BleLib.Platforms.AndroidBLE;
#endif

namespace BleLib.Services
{
    public static class BluetoothServiceFactory
    {
        public static IBluetoothService CreateBluetoothService()
        {
            // Try conditional compilation first
#if NET9_0_WINDOWS10_0_19041_0
            return new WindowsBluetoothService();
#elif NET9_0_ANDROID
            return new AndroidBluetoothService();
#else
            // Fallback to runtime detection
            return CreateBluetoothServiceRuntime();
#endif
        }

        private static IBluetoothService CreateBluetoothServiceRuntime()
        {
            var platform = Environment.OSVersion.Platform;
            var framework = Environment.Version;

            if (platform == PlatformID.Win32NT)
            {
                // Windows platform - check if we can use Windows Bluetooth APIs
                try
                {
#if NET9_0_WINDOWS10_0_19041_0
                    return new WindowsBluetoothService();
#else
                    throw new PlatformNotSupportedException("Windows Bluetooth service not available for this framework");
#endif
                }
                catch (Exception ex)
                {
                    throw new PlatformNotSupportedException($"Windows Bluetooth service not available: {ex.Message}");
                }
            }
            else if (platform == PlatformID.Unix)
            {
                // Could be Android or other Unix-based system
                throw new PlatformNotSupportedException($"Unix platform detected but not supported. Framework: {framework}");
            }
            else
            {
                throw new PlatformNotSupportedException($"Platform {platform} is not supported. Framework: {framework}");
            }
        }
    }
}