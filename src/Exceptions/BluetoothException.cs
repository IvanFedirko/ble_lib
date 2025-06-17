using System;

namespace BleLib.Exceptions
{
    public class BluetoothException : Exception
    {
        public BluetoothException(string message) : base(message)
        {
        }

        public BluetoothException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
} 