using System;
using System.Collections.Generic;

namespace BleLib.Models
{
    public class BluetoothDevice
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int SignalStrength { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastSeen { get; set; }
        
        // Advertisement data properties
        public List<Guid> ServiceUuids { get; set; } = new List<Guid>();
        public Dictionary<ushort, byte[]> ManufacturerData { get; set; } = new Dictionary<ushort, byte[]>();
        public Dictionary<Guid, byte[]> ServiceData { get; set; } = new Dictionary<Guid, byte[]>();
        public byte[] RawAdvertisementData { get; set; } = Array.Empty<byte>();
        public bool IsConnectable { get; set; }
        public int TxPowerLevel { get; set; }
    }
} 