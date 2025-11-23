using System;
using System.Device.I2c;
using System.Threading;
namespace NanoFM
{
public class I2cEeprom
{
 
        private I2cDevice _device;

        public I2cEeprom(I2cDevice device)
        {
            _device = device;
        }

        public void WriteByte(int address, byte data)
        {
            byte[] buffer = new byte[3];
            buffer[0] = (byte)((address >> 8) & 0xFF);
            buffer[1] = (byte)(address & 0xFF);
            buffer[2] = data;
            _device.Write(buffer);
            Thread.Sleep(5);
        }

        public byte ReadByte(int address)
        {
            byte[] addrBuffer = new byte[2];
            addrBuffer[0] = (byte)((address >> 8) & 0xFF);
            addrBuffer[1] = (byte)(address & 0xFF);
            _device.Write(addrBuffer);

            byte[] dataBuffer = new byte[1];
            _device.Read(dataBuffer);
            return dataBuffer[0];
        }
    }

}