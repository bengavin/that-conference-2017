using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.UI;

namespace ThatPiSample.Services
{
    public interface ILedService
    {
        Task<bool> InitializeAsync();
        bool Shutdown();
        bool SetLEDColor(Color color);
    }

    public class LedService : ILedService
    {
        /// <summary>
        /// The following pins should be set to appropriately reflect the pins
        /// that you used when wiring the hardware side of the project
        /// </summary>
        private const int LED_PIN_R = 13;
        private const int LED_PIN_G = 6;
        private const int LED_PIN_B = 5;

        private GpioPin _pinR;
        private GpioPin _pinG;
        private GpioPin _pinB;

        private bool _isInitialized = false;
        private Color _currentColor;

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) { return true; }

            // Set the Lightning Provider as the default if Lightning driver is enabled on the target device
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            var gpio = await GpioController.GetDefaultAsync();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                _pinR = null;
                _pinG = null;
                _pinB = null;
                return false;
            }

            var pinValue = GpioPinValue.High;

            try
            {

                _pinR = gpio.OpenPin(LED_PIN_R);
                _pinR.Write(pinValue);
                _pinR.SetDriveMode(GpioPinDriveMode.Output);

                _pinG = gpio.OpenPin(LED_PIN_G);
                _pinG.Write(pinValue);
                _pinG.SetDriveMode(GpioPinDriveMode.Output);

                _pinB = gpio.OpenPin(LED_PIN_B);
                _pinB.Write(pinValue);
                _pinB.SetDriveMode(GpioPinDriveMode.Output);

                _isInitialized = true;
            }
            catch
            {
                // TODO: Add logging
                return false;
            }

            return true;
        }

        public bool Shutdown()
        {
            if (!_isInitialized) { return false; }

            try
            {
                _isInitialized = false;

                _pinR.Write(GpioPinValue.High);
                _pinR.Dispose();
                _pinR = null;

                _pinG.Write(GpioPinValue.High);
                _pinG.Dispose();
                _pinG = null;

                _pinB.Write(GpioPinValue.High);
                _pinB.Dispose();
                _pinB = null;

            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool SetLEDColor(bool red, bool green, bool blue)
        {
            if (!_isInitialized) { return false; }

            // NOTE: To support a wider range of colors, we would need
            //       to have a digital potentiometer (adjustable resistor)
            //       or PWM (pulse-width modulator) inline with the LEDs, 
            //       a standard resistor will only manage one level of 
            //       brightness for each diode (on/off).

            try
            {
                _pinR.Write(red ? GpioPinValue.Low : GpioPinValue.High);
                _pinG.Write(green ? GpioPinValue.Low : GpioPinValue.High);
                _pinB.Write(blue ? GpioPinValue.Low : GpioPinValue.High);
            }
            catch
            {
                // TODO: Add logging
                return false;
            }

            return true;
        }

        public bool SetLEDColor(Color color)
        {
            _currentColor = color;

            // Attempt to set to the color they've asked for, but we need
            // to 'clip' the color, because we only support 'on/off', not
            // grades of colors
            return SetLEDColor(color.R >= 128, color.G >= 128, color.B >= 128);
        }
    }

}
