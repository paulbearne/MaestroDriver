using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MaestroUsbUI.Control;
using MaestroUsb;
using Pololu.Usc;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MaestroUsbUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ChannelSettingsControl[] channelSettings;
        
        private MaestroDeviceListItem maestroDevice;
        private UscSettings settings;
        public SettingsPage()
        {
            this.InitializeComponent();
         //   this.maestroDevice = maestro;
            
        }

        


        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
                drawMaestroControls();
                btnSave.IsEnabled = true;
            }
            else
            {
                tbDeviceName.Text = "Not Connected Pleaese Connect to Device First";
                // just create dummy set of panels
                channelSettings = new ChannelSettingsControl[6];
                for (byte i = 0; i < 6; i++)
                {
                    // add a speed , acceleration and target controls to the app
                    channelSettings[i] = new ChannelSettingsControl();
                    channelSettings[i].ChannelNumber = i;
                    ControlPanel.Children.Add(channelSettings[i]);
                    channelSettings[i].IsEnabled = false;
                    btnSave.IsEnabled = false;

                }
            }
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            
            if (maestroDevice != null)
            {
                // remove all the panels from the display
                ControlPanel.Children.Clear();
                channelSettings = null;
            }
        }


        private async void drawMaestroControls()
        {
            // get the number of servos on the board
            UInt16 count = maestroDevice.Maestro.ServoCount;
            // get all the settings stored on the board
            settings =  await maestroDevice.Maestro.getUscSettings();
            Task.WaitAll();
            // maestroDevice.Maestro.getMaestroVariables();
            tbDeviceName.Text = maestroDevice.Name + " Connected";
            // Create an array of  controls
            channelSettings = new ChannelSettingsControl[count];
            for (byte i = 0; i < count; i++)
            {
                // add a speed , acceleration and target controls to the app
                channelSettings[i] = new ChannelSettingsControl();
                channelSettings[i].ChannelNumber = i;
                channelSettings[i].Acceleration = settings.channelSettings[i].acceleration;
                channelSettings[i].Speed = settings.channelSettings[i].speed;
                channelSettings[i].MinPosition = settings.channelSettings[i].minimum;
                channelSettings[i].MaxPosition = settings.channelSettings[i].maximum;
                channelSettings[i].Name = settings.channelSettings[i].name;
                channelSettings[i].Mode = settings.channelSettings[i].mode;
                channelSettings[i].homeMode = settings.channelSettings[i].homeMode;
                channelSettings[i].Range = (Int16)settings.channelSettings[i].range;
                channelSettings[i].Target = settings.channelSettings[i].home;
                channelSettings[i].Nuetral8Bit = settings.channelSettings[i].neutral;
                ControlPanel.Children.Add(channelSettings[i]);

            }


        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();

            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 count = maestroDevice.Maestro.ServoCount;
            // update settings with the control values
            for (byte i = 0; i < count; i++)
            {
                // add a speed , acceleration and target controls to the app
                settings.channelSettings[i].acceleration = (byte)channelSettings[i].Acceleration;
                settings.channelSettings[i].speed =channelSettings[i].Speed;
                settings.channelSettings[i].minimum =(UInt16)(channelSettings[i].MinPosition * 4) ;
                settings.channelSettings[i].maximum = (UInt16)(channelSettings[i].MaxPosition * 4);
                settings.channelSettings[i].name = channelSettings[i].Name;
                settings.channelSettings[i].mode = channelSettings[i].Mode;
                settings.channelSettings[i].homeMode = channelSettings[i].homeMode;
                settings.channelSettings[i].range = (UInt16)channelSettings[i].Range;
                settings.channelSettings[i].home = (UInt16)(channelSettings[i].Target);
                settings.channelSettings[i].neutral = channelSettings[i].Nuetral8Bit;
            }

            maestroDevice.Maestro.setUscSettings(settings, false);
        }
    }
}
