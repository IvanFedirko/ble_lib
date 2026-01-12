using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BleLib
{
    public static class CborUtil
    {
        // Only supports encoding a map with string keys and int, bool, or byte[] values
        public static byte[] EncodeMap(Dictionary<string, object> map)
        {
            var result = new List<byte>();
            // Major type 5: map
            result.Add((byte)(0xA0 | (byte)map.Count));
            foreach (var kvp in map)
            {
                EncodeString(result, kvp.Key);
                EncodeValue(result, kvp.Value);
            }
            return result.ToArray();
        }

        private static void EncodeString(List<byte> result, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length < 24)
                result.Add((byte)(0x60 | bytes.Length));
            else
                throw new NotSupportedException("Strings >= 24 bytes not supported in this minimal CBOR encoder");
            result.AddRange(bytes);
        }

        private static void EncodeValue(List<byte> result, object value)
        {
            switch (value)
            {
                case int i:
                    if (i >= 0 && i < 24)
                        result.Add((byte)(0x00 | i));
                    else if (i >= 24 && i < 256)
                    {
                        result.Add(0x18);
                        result.Add((byte)i);
                    }
                    else if (i >= 256 && i < 65536)
                    {
                        result.Add(0x19);
                        result.Add((byte)((i >> 8) & 0xFF));
                        result.Add((byte)(i & 0xFF));
                    }
                    else if (i >= 65536)
                    {
                        result.Add(0x1a);
                        result.Add((byte)((i >> 24) & 0xFF));
                        result.Add((byte)((i >> 16) & 0xFF));
                        result.Add((byte)((i >> 8) & 0xFF));
                        result.Add((byte)(i & 0xFF));
                    }
                    else
                        throw new NotSupportedException("Negative integers not supported");
                    break;
                case bool b:
                    result.Add((byte)(b ? 0xF5 : 0xF4));
                    break;
                case byte[] arr:
                    if (arr.Length < 24)
                    {
                        result.Add((byte)(0x40 | arr.Length));
                    }
                    else if (arr.Length < 256)
                    {
                        result.Add(0x58); // Byte string, 1-byte length
                        result.Add((byte)arr.Length);
                    }
                    else if (arr.Length < 65536)
                    {
                        result.Add(0x59); // Byte string, 2-byte length
                        result.Add((byte)((arr.Length >> 8) & 0xFF));
                        result.Add((byte)(arr.Length & 0xFF));
                    }
                    else
                    {
                        throw new NotSupportedException("Byte arrays >= 65536 bytes not supported");
                    }
                    result.AddRange(arr);
                    break;
                default:
                    throw new NotSupportedException($"Type {value.GetType()} not supported");
            }
        }

        public static async Task WriteDescriptorAsync(Guid serviceUuid, Guid characteristicUuid, Guid cccdUuid, byte[] value)
        {
            // Implementation of WriteDescriptorAsync method
        }

        public static async Task SubscribeToCharacteristicAsync(Guid serviceUuid, Guid characteristicUuid, Action<byte[]> notificationCallback)
        {
            // Implementation of SubscribeToCharacteristicAsync method
        }
    }
} 