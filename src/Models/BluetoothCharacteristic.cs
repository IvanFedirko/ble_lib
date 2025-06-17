using System;

namespace BleLib.Models
{
    public class BluetoothCharacteristic
    {
        public Guid CharacteristicUuid { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanNotify { get; set; }
    }
} 