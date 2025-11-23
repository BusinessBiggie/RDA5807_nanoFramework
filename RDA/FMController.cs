using System;
using System.Device.I2c;
using System.Threading;

namespace NanoFM
{
    public class FMController
    {
        private readonly I2cDevice _device;
        private const byte Address = 0x11;
        private ushort _oldChannel = 0;

        public FMController(I2cDevice device)
        {
            _device = device;
        }

        public void Setup()
        {
            WriteRegister(0x02, 0b1100000000000011);
            Thread.Sleep(100);

            WriteRegister(0x02, 0b1101000000000001);
            Thread.Sleep(100);

            ChangeChannel(16);
            Thread.Sleep(100);

            WriteRegister(0x05, 0b1000100010001111);
        }

        public void SeekUp()
        {
            WriteRegister(0x02, 0b1110001110000001);
            Thread.Sleep(200);
        }

        public void SeekDown()
        {
            WriteRegister(0x02, 0b1110000110000001);
            Thread.Sleep(200);
        }

        public ushort GetFrequency()
        {
            ushort channel = GetChannelFromReg();
            return (ushort)(channel * 10 + 8700);
        }

        public void ChangeChannel(ushort channel)
        {
            byte msb = (byte)(channel >> 2);
            byte lsb = (byte)((channel << 6) | 0b00010000);
            WriteTwoBytes(0x03, msb, lsb);

            _oldChannel = channel;
        }

        public ushort GetChannelFromReg()
        {
            // Write register address
            byte[] writeBuf = { 0x0A };
            _device.Write(writeBuf);

            // Read two bytes
            byte[] readBuf = new byte[2];
            _device.Read(readBuf);

            ushort currentChannel = (ushort)((readBuf[0] << 8) | readBuf[1]);
            return currentChannel;
        }

        private void WriteRegister(byte reg, ushort value)
        {
            byte msb = (byte)((value >> 8) & 0xFF);
            byte lsb = (byte)(value & 0xFF);

            WriteTwoBytes(reg, msb, lsb);
        }

        private void WriteTwoBytes(byte reg, byte msb, byte lsb)
        {
            byte[] buffer = { reg, msb, lsb };
            _device.Write(buffer);
        }
    }
}
