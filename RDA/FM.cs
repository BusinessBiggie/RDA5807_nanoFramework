using System;
using System.Device.I2c;
using System.Threading;

namespace NanoFM
{
    public class FM
    {
        private readonly I2cDevice _device;
        private const byte DirectI2CAddress = 0x11;

        public FM(I2cDevice device)
        {
            _device = device;
        }

        public void Setup()
        {
            WriteRegister(0x02, 0b11000000, 0b00000011);
            Thread.Sleep(100);

            WriteRegister(0x02, 0b11010000, 0b00000001);
            Thread.Sleep(100);

            byte channel = 16;
            WriteRegister(0x03, (byte)(channel >> 2), (byte)(((channel & 0x03) << 6) | 0b00010000));
            Thread.Sleep(100);

            // Register 0x05
            WriteRegister(0x05, 0b10001000, 0b10001111);
        }

        public void SeekUp()
        {
            WriteRegister(0x02, 0b11100011, 0b10000001);
            Thread.Sleep(200);
        }

        public void SeekDown()
        {
            WriteRegister(0x02, 0b11100001, 0b10000001);
            Thread.Sleep(200);
        }

        public void ChangeChannel(ushort channel)
        {
            WriteRegister(0x03, (byte)(channel >> 2), (byte)(((channel & 0x03) << 6) | 0b00010000));
            Thread.Sleep(100);
        }

        public ushort GetFrequency()
        {
            ushort chan = GetChannelFromReg();
            return (ushort)(chan * 10 + 8700);
        }

        public ushort GetChannelFromReg()
        {
            byte[] buffer = new byte[2];
            _device.WriteByte(0x0A);
            _device.Read(buffer);

            ushort channel = (ushort)((buffer[0] << 8) | buffer[1]);
            return channel;
        }

        private void WriteRegister(byte reg, byte msb, byte lsb)
        {
            byte[] data = new byte[3] { (byte)reg, (byte)msb, (byte)lsb };
            _device.Write(data);
        }
    }
}
