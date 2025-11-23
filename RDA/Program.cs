using System;
using System.Threading;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Adc;
using nanoFramework.Hardware.Esp32;
using Iot.Device.CharacterLcd;
//using I2cEeprom;

namespace NanoFM
{

    public class Program
    {
        static GpioController gpio = new GpioController();
        static Lcd1602 lcd;
        static FM fm;
        static AdcController adc;

        const int buttonPin1 = 2;
        const int buttonPin2 = 7;
        const int saveButton = 8;
        const int loadButton = 9;
        const int cancelButton = 10;

        const int poChannel = 36; // ADC1_CH0
        static int oldPotChannel = -1;

        public static void Main()
        {
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);
            Configuration.SetPinFunction(poChannel, DeviceFunction.ADC1_CH0);

            var lcdSettings = new I2cConnectionSettings(1, 0x27);
            var lcdDevice = I2cDevice.Create(lcdSettings);
            lcd = new Lcd1602(lcdDevice) { BacklightOn = true };
            lcd.Clear();

            var fmSettings = new I2cConnectionSettings(1, 0x11);
            var fmDevice = I2cDevice.Create(fmSettings);
            fm = new FM(fmDevice);
            fm.Setup();

            var eepromSettings = new I2cConnectionSettings(1, 0x50);
            var eepromDevice = I2cDevice.Create(eepromSettings);
            var eeprom = new I2cEeprom(eepromDevice);

            int[] buttons = { buttonPin1, buttonPin2, saveButton, loadButton, cancelButton };
            foreach (var b in buttons)
            {
                gpio.OpenPin(b, PinMode.InputPullUp);
            }                lcd.Clear();
                lcd.SetCursorPosition(0, 0);
                lcd.Write("Load failed!");
                Thread.Sleep(2000);
                SetFrequencyDisplay();

            adc = new AdcController();
            var adcChannel = adc.OpenChannel(poChannel);

            SetFrequencyDisplay();

            while (true)
            {
                if (gpio.Read(buttonPin1) == PinValue.Low)
                {
                    fm.SeekUp();
                    SetFrequencyDisplay();
                }
                else if (gpio.Read(buttonPin2) == PinValue.Low)
                {
                    fm.SeekDown();
                    SetFrequencyDisplay();
                }
                else if (gpio.Read(saveButton) == PinValue.Low)
                {
                    SaveChannel(eeprom);
                }
                else if (gpio.Read(loadButton) == PinValue.Low)
                {
                    LoadChannel(eeprom);
                }

                ManualChannelChange(adcChannel);

                Thread.Sleep(50);
            }
        }

        static void SetFrequencyDisplay()
        {
            ushort frequency = fm.GetFrequency();
            string freqStr = frequency.ToString();
            string wholeNum = frequency >= 10000 ? freqStr.Substring(0, 3) : freqStr.Substring(0, 2);
            string deciNum = frequency >= 10000 ? freqStr.Substring(3, 3) : freqStr.Substring(2, 3);

            lcd.Clear();
            lcd.SetCursorPosition(0, 0);
            lcd.Write("--FM Frequency--");
            lcd.SetCursorPosition(0, 1);
            lcd.Write($"    {wholeNum}.{deciNum}MHz ");
        }

        static void ManualChannelChange(AdcChannel adcChannel)
        {
            try
            {
                int potReading = adcChannel.ReadValue();
                int channel = Map(potReading, 0, 4095, 0, 210);
                if (channel != oldPotChannel)
                {
                    fm.ChangeChannel((ushort)channel);
                    oldPotChannel = channel;
                    SetFrequencyDisplay();
                }
            }
            catch
            {
                
            }
        }

        static void SaveChannel(I2cEeprom eeprom)
        {
            lcd.Clear();
            lcd.SetCursorPosition(0, 0);
            lcd.Write("Save frequency?");
            lcd.SetCursorPosition(0, 1);
            lcd.Write("   Press again");
            Thread.Sleep(500);

            while (gpio.Read(saveButton) == PinValue.High &&
                   gpio.Read(cancelButton) == PinValue.High)
            {
                Thread.Sleep(50);
            }

            if (gpio.Read(saveButton) == PinValue.Low)
            {
                ushort channel = fm.GetChannelFromReg();
                byte highByte = (byte)(channel >> 8);
                byte lowByte = (byte)(channel & 0xFF);

                eeprom.WriteByte(0x00, highByte);
                eeprom.WriteByte(0x01, lowByte);

                lcd.Clear();
                lcd.SetCursorPosition(0, 0);
                lcd.Write("-----Saved!-----");
                Thread.Sleep(2000);
                SetFrequencyDisplay();
            }
            else if (gpio.Read(cancelButton) == PinValue.Low)
            {
                lcd.Clear();
                lcd.SetCursorPosition(0, 0);
                lcd.Write("----Cancelled---");
                Thread.Sleep(2000);
                SetFrequencyDisplay();
            }
        }

        static void LoadChannel(I2cEeprom eeprom)
        {
            try
            {
                byte highByte = eeprom.ReadByte(0x00);
                byte lowByte  = eeprom.ReadByte(0x01);
                ushort channel = (ushort)((highByte << 8) | lowByte);

                fm.ChangeChannel(channel);
                SetFrequencyDisplay();
            }
            catch
            {
                lcd.Clear();
                lcd.SetCursorPosition(0, 0);
                lcd.Write("Load failed!");
                Thread.Sleep(2000);
                SetFrequencyDisplay();
            }
        }

        static int Map(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}