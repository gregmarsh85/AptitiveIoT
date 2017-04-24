using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Windows.Devices.Gpio;
using Newtonsoft.Json;


namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 6;
        private const int BUTTON_PIN = 5;
        private GpioPin ledpin;
        private GpioPinValue ledpinValue;
        private GpioPin buttonPin;
        private DispatcherTimer timer;
        private Random rnd = new Random();
        private int minspeed = 0; //in ms
        private int maxspeed = 150;
        private int currentspeed;


        public MainPage()
        {
            InitializeComponent();

            int timerval = 3000;  

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(timerval);
            timer.Tick += Timer_Tick;

            InitGPIO();
            
            if (ledpin != null)
            {
                timer.Start();
            }

            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonPin_ValueChanged;
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                ledpin = null;
                buttonPin = null;
                return;
            }

            buttonPin = gpio.OpenPin(BUTTON_PIN);
            ledpin = gpio.OpenPin(LED_PIN);

            ledpinValue = GpioPinValue.High;
            ledpin.Write(ledpinValue);
            ledpin.SetDriveMode(GpioPinDriveMode.Output);

            // Check if input pull-up resistors are supported
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
                buttonPin.SetDriveMode(GpioPinDriveMode.Input);

            // Set a debounce timeout to filter out switch bounce noise from a button press
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
        }

        private void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            // toggle the state of the LED every time the button is pressed
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                ledpinValue = (ledpinValue == GpioPinValue.Low) ?
                    GpioPinValue.High : GpioPinValue.Low;
                ledpin.Write(ledpinValue);

                //Transmit the data set
                if (ledpinValue == GpioPinValue.High)
                {
                    SendDeviceToCloudMessagesAsync(0, currentspeed);
                }
                else
                {
                    SendDeviceToCloudMessagesAsync(1, currentspeed);
                }
            }          

        }

        private void Timer_Tick(object sender, object e)
        {
            //Get the next random speed telemetry
            int nextspeed = rnd.Next(minspeed, maxspeed);

            currentspeed = nextspeed;

            //Transmit the data set
            if (ledpinValue == GpioPinValue.High)
            {
                SendDeviceToCloudMessagesAsync(0, nextspeed);
            }
            else
            {
                SendDeviceToCloudMessagesAsync(1, nextspeed);
            }
        }

        static async void SendDeviceToCloudMessagesAsync(int status, int speed)
        {
            string iotHubUri = "aptIoThub.azure-devices.net"; 
            string deviceId = "Device1"; 
            string deviceKey = "QNctTB6FKukrXBOEesSzy/s8maVpUyvl31s2fDBSKbs="; 
        
            var deviceClient = DeviceClient.Create(iotHubUri,
                    AuthenticationMethodFactory.
                        CreateAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey),
                    TransportType.Http1);

            var pocDataPoint = new
            {               
                status = status,
                speed = speed
            };

            var str = JsonConvert.SerializeObject(pocDataPoint);      
            var message = new Message(Encoding.UTF8.GetBytes(str));
        
            await deviceClient.SendEventAsync(message);
        }

    }


}
