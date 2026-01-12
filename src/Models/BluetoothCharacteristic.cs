using System;
using System.Collections.Generic;

namespace BleLib.Models
{
    public class BluetoothCharacteristic
    {
        public Guid CharacteristicUuid { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanWriteWithoutResponse { get; set; }
        public bool CanNotify { get; set; }
        public List<BluetoothDescriptor> Descriptors { get; set; } = new List<BluetoothDescriptor>();
    }

    public class BluetoothDescriptor
    {
        public Guid DescriptorUuid { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Value { get; set; } = Array.Empty<byte>();
    }
} 