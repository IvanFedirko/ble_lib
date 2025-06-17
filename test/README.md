# BLE Library Test Suite

This directory contains comprehensive tests for the BLE library.

## Test Structure

### Unit Tests
- **BluetoothDeviceTests**: Tests for the BluetoothDevice model
- **BluetoothServiceTests**: Tests for the BluetoothService model  
- **BluetoothCharacteristicTests**: Tests for the BluetoothCharacteristic model
- **BluetoothServiceFactoryTests**: Tests for the service factory

### Mock Tests
- **MockBluetoothServiceTests**: Tests using mocked IBluetoothService interface
- **AdvertisementDataTests**: Tests for advertisement data parsing and handling

### Integration Tests
- **AndroidBluetoothServiceTests**: Integration tests for Android platform
- **WindowsBluetoothServiceTests**: Integration tests for Windows platform

## Running Tests

### Using dotnet CLI
```bash
# Navigate to test directory (if not already there)
cd test

# Run all tests (will run for all available frameworks)
dotnet test

# Run tests for Windows framework (recommended for Windows development)
dotnet test --framework net9.0-windows10.0.19041.0

# Run tests for Android framework (requires Android runtime)
dotnet test --framework net9.0-android

# Run specific test category
dotnet test --filter "Category=Unit"

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests with diagnostic output (most verbose)
dotnet test --verbosity diagnostic

# Run specific test class
dotnet test --filter "FullyQualifiedName~BluetoothDeviceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run from parent directory (if needed)
dotnet test test/BleLib.Tests.csproj
```

### Platform-Specific Notes

**Windows Development:**
- Use `--framework net9.0-windows10.0.19041.0` for Windows-specific testing
- All tests should pass on Windows with appropriate Bluetooth hardware
- No additional runtime installation required

**Android Development:**
- Use `--framework net9.0-android` for Android-specific testing
- Requires Microsoft.Android runtime to be installed
- If you get "You must install or update .NET to run this application" error:
  1. Install Android workload: `dotnet workload install android`
  2. Install Android runtime: Download from [Microsoft.Android Runtime](https://aka.ms/dotnet-core-applaunch?framework=Microsoft.Android&arch=x64&rid=win-x64&os=win10)
  3. Or use Visual Studio 2022 with Android development workload

**Cross-Platform Testing:**
- Run `dotnet test` without framework specification to test all available platforms
- Tests will automatically skip platforms where runtime is not available
- Mock tests work on all platforms without hardware dependencies

### Using Visual Studio Code
1. Open the Tests folder in VS Code
2. Use the "Run Tests" configuration in launch.json
3. Or use the "test" task in tasks.json

## Test Coverage

The test suite covers:
- ✅ Model classes (Device, Service, Characteristic)
- ✅ Service factory pattern
- ✅ Platform-specific implementations
- ✅ Error handling and exceptions
- ✅ Advertisement data parsing
- ✅ Mock scenarios for isolated testing
- ✅ Integration scenarios for real platform testing

## Dependencies

- **xUnit**: Testing framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking framework
- **coverlet.collector**: Code coverage collection

## Notes

- Integration tests require appropriate platform permissions
- Some tests may fail in environments without Bluetooth hardware
- Mock tests provide isolated testing without hardware dependencies 