using MaestroUsb;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MaestroUsbUI
{
    public enum NotifyType
    {
        Connected,
        Removed
    };
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CommsPage : Page
    {
        // add observable collection to show list of maestro devices in combobox
        private MaestroDeviceListItem maestroDevice;
        private UscSettings settings;
        
              
        public CommsPage()
        {
           
            this.InitializeComponent();


        }
        
            

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.Parameter as MaestroBoard != null)
            {
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                status.Text = maestroDevice.Name + " Connected";
                updateSerialSettings(); 
            }

        }

        private async void updateSerialSettings()
        {
            settings = await maestroDevice.Maestro.getUscSettings();
            Task.WaitAll();
            if (settings.serialMode == uscSerialMode.SERIAL_MODE_USB_DUAL_PORT)
            {
                usbDualPort.IsChecked = true;
            }
            if (settings.serialMode == uscSerialMode.SERIAL_MODE_USB_CHAINED)
            {
                usbChained.IsChecked = true;
            }
            if (settings.serialMode == uscSerialMode.SERIAL_MODE_UART_FIXED_BAUD_RATE)
            {
                uartFixedBaud.IsChecked = true;
            }
            if (settings.serialMode == uscSerialMode.SERIAL_MODE_UART_DETECT_BAUD_RATE)
            {
                uartbaudDetect.IsChecked = true;
            }
            BaudRate.Value = (int)settings.fixedBaudRate;
            crcenabled.IsChecked = settings.enableCrc;
            deviceNumber.Value = settings.serialDeviceNumber;
            sscoffset.Value = settings.miniSscOffset;
            timeout.Value = settings.serialTimeout;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
                
            }
        }

        private void BaudRate_valueChanged(int newValue)
        {
            settings.fixedBaudRate = (uint)newValue;
           
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            maestroDevice.Maestro.setUscSettings(settings, false);
        }




        //  private unsafe int getservostructsize()
        //{
        //  return sizeof(ServoStatus);
        //}

    }

}

