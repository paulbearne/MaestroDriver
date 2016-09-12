using MaestroUsb;
using Pololu.Usc;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MaestroUsbUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManualControl : Page
    {
        // add observable collection to show list of maestro devices in combobox
        
        private bool Connected = false;
        //  single maestro board
        private MaestroDeviceListItem maestroDevice;
         // maestro settings
        private UscSettings settings;
        private MaestroControl[] maestroChannels;
        private ServoStatus[] servoStatus;
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");
        public ManualControl()
        {
            
           
            this.InitializeComponent();
            //this.maestroDevice = maestro;
            
        }

       
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
                drawMaestroControls();
            }
            else
            {
                tbDeviceName.Text = "Not Connected Pleaese Connect to Device First";
                maestroChannels = new MaestroControl[6];
                for (UInt16 i = 0; i < 6; i++)
                {
                    // add a speed , acceleration and target controls to the app
                    maestroChannels[i] = new MaestroControl();
                    maestroChannels[i].ChannelNumber = i;
                    maestroChannels[i].IsEnabled = false;
                    maestroPanel.Children.Add(maestroChannels[i]);
                }
            }
        }

        
      //  private unsafe int getservostructsize()
        //{
          //  return sizeof(ServoStatus);
        //}

        private async void drawMaestroControls()
        {
            // get the number of servos on the board
            UInt16 count = maestroDevice.Maestro.ServoCount;
            // get all the settings stored on the board
            
            settings = await maestroDevice.Maestro.getUscSettings();
            
            await maestroDevice.Maestro.updateMaestroVariables();
            Task.WaitAll();  // wait until we have all the data
            Connected = true;
            tbDeviceName.Text = maestroDevice.Name + " Connected";
            // Create an array of  controls
            maestroChannels = new MaestroControl[count];
            for (UInt16 i = 0; i < count; i++)
            {
                // add a speed , acceleration and target controls to the app
                maestroChannels[i] = new MaestroControl();
                maestroChannels[i].ChannelNumber = i;
                // update the controls to show current values from the board
                maestroChannels[i].Acceleration = Convert.ToUInt16(settings.channelSettings[i].acceleration);
                maestroChannels[i].Speed = Convert.ToUInt16(settings.channelSettings[i].speed);
                // position / 4 as it returns it in 1/4 microseconds
                maestroChannels[i].Position = Convert.ToUInt16(MaestroDevice.positionToMicroseconds(maestroDevice.Maestro.servoStatus[i].position));
                maestroPanel.Children.Add(maestroChannels[i]);
                // add the callbacks for changes
                maestroChannels[i].positionChanged += MainPage_positionChanged;
                maestroChannels[i].speedChanged += MainPage_speedChanged;
                maestroChannels[i].accelerationChanged += MainPage_accelerationChanged;
            }
            

        }


        private async void updateServoData(byte channel)
        {
            if (maestroDevice.Maestro.microMaestro)
            {
                await maestroDevice.Maestro.updateMaestroVariables();
                maestroChannels[channel].Acceleration = Convert.ToUInt16(settings.channelSettings[channel].acceleration);
                maestroChannels[channel].Speed = Convert.ToUInt16(settings.channelSettings[channel].speed);
                // position / 4 as it returns it in 1/4 microseconds
                maestroChannels[channel].Position = Convert.ToUInt16(MaestroDevice.positionToMicroseconds(maestroDevice.Maestro.servoStatus[channel].position));

            } else
            {
                servoStatus = await maestroDevice.Maestro.getServosMiniMaestro();
                maestroChannels[channel].Position = Convert.ToUInt16(MaestroDevice.positionToMicroseconds(servoStatus[channel].position));
            }

        }


        private  void MainPage_accelerationChanged(byte Channel, byte newAcceleration)
        {
            //  add new value to
           // settings.channelSettings[Channel].acceleration = newAcceleration;
            maestroDevice.Maestro.setAcceleration(Channel,newAcceleration);
        }

        private  void MainPage_speedChanged(byte Channel, UInt16 newSpeed)
        {
           // settings.channelSettings[Channel].speed = newSpeed;
            maestroDevice.Maestro.setSpeed(Channel,newSpeed);


        }

        private  void MainPage_positionChanged(byte Channel, UInt16 newPosition)
        {
            if (Connected)
            {
                
                // set target position in us
                maestroDevice.Maestro.setTarget(Channel, (UInt16)(newPosition * 4));
                
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();

            }
        }
    }

}

