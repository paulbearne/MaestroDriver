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
        StatusMessage,
        ErrorMessage
    };
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const String ButtonNameDisconnectFromDevice = "Disconnect from device";
        private const String ButtonNameDisableReconnectToDevice = "Do not automatically reconnect to device that was just closed";

        private ObservableCollection<MaestroDeviceListItem> listOfDevices;
        private bool Connected = false;
        private MaestroDeviceListItem maestroDevice;
        private MaestroDeviceManager maestrodevices;
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");
        public MainPage()
        {
            listOfDevices = new ObservableCollection<MaestroDeviceListItem>();
            this.InitializeComponent();


        }


        private void getDeviceList()
        {
            maestrodevices = new MaestroDeviceManager();
            maestrodevices.deviceListReadyCallback += Maestrodevices_deviceListReadyCallback;
            maestrodevices.BuildDeviceList();

        }

        private async void Maestrodevices_deviceListReadyCallback(Collection<MaestroDeviceListItem> devices)
        {
            //We now have completed built list
            if (listOfDevices != null)
            {
                // run on ui thread
                await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() =>
                    {
                        foreach (MaestroDeviceListItem item in devices)
                        {
                            // make the initial device list match our list
                            listOfDevices.Add(item);
                        }
                        if (listOfDevices.Count > 0)
                        {
                            lbDevices.SelectedIndex = 0; // select first item by default
                        }
                    }));
            }
            //add callbacks 
            maestrodevices.deviceAddedCallback += Maestrodevices_deviceAddedCallback;
            maestrodevices.deviceRemovedCallback += Maestrodevices_deviceRemovedCallback;
        }

        private async void Maestrodevices_deviceRemovedCallback(DeviceInformationUpdate deviceInfo)
        {
            if (listOfDevices != null)
            {
                // make sure were running on the Ui Thread
                await Dispatcher.RunAsync(
                      CoreDispatcherPriority.Normal,
                      new DispatchedHandler(() =>
                      {
                          var firstitem = listOfDevices.First(e => e.Id == deviceInfo.Id);
                          int index = listOfDevices.IndexOf(firstitem);
                          lbDevices.SelectedIndex = -1;
                          if (index > -1)
                          {

                              listOfDevices.RemoveAt(index);
                              if (listOfDevices.Count > 0)
                              {
                                 // if we have more than one device select the first item
                                 lbDevices.SelectedIndex = 0;
                              }
                          }
                      }));

            }
        }


        private async void Maestrodevices_deviceAddedCallback(MaestroDeviceListItem device)
        {
            bool matched = false;
            // we will normally only have one device so
            if (listOfDevices != null)
            {
                await Dispatcher.RunAsync(
                     CoreDispatcherPriority.Normal,
                     new DispatchedHandler(() =>
                     {
                         foreach (MaestroDeviceListItem item in listOfDevices)
                         {
                             if (item.deviceInformation.Id == device.deviceInformation.Id)
                             {
                                 // don't add device as we already have it
                                 matched = true;
                                 break;
                             }

                         }
                         if (matched == false)
                         {
                             listOfDevices.Add(device);
                             if ((lbDevices.SelectedIndex == -1) && (listOfDevices.Count > 0))
                             {
                                 lbDevices.SelectedIndex = 0; // select first item by deafult if we have one

                             }
                         }

                     }));
            }
        }


        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            getDeviceList();


            DeviceListSource.Source = listOfDevices;
            lbDevices.SelectionChanged += LbDevices_SelectionChanged;


            /*  Connected = await maestroDevice.OpenFirstDevice();
              if (Connected)
              {
                  UInt16 count = maestroDevice.getChannelCount();
                  tbDeviceName.Text = maestroDevice.Name + " Connected";
                  MaestroControl[] maestroChannels = new MaestroControl[count];
                  for (UInt16 i = 0; i < count; i++)
                  {
                      maestroChannels[i] = new MaestroControl();
                      maestroChannels[i].ChannelNumber = i;

                      maestroChannels[i].Acceleration = Convert.ToUInt16(await maestroDevice.GetMaestroServoAccelerationAsync((byte)i));
                      maestroChannels[i].Speed = Convert.ToUInt16(await maestroDevice.GetMaestroServoSpeedAsync((byte)i));
                      maestroPanel.Children.Add(maestroChannels[i]);
                      maestroChannels[i].positionChanged += MainPage_positionChanged;
                      maestroChannels[i].speedChanged += MainPage_speedChanged;
                      maestroChannels[i].accelerationChanged += MainPage_accelerationChanged;

                  }
               }
               */




        }

        private async void LbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbDevices.SelectedIndex > -1)
            {
                maestroDevice = listOfDevices[lbDevices.SelectedIndex];
                // check if we are connected
                if (maestroDevice.device == null)
                {
                    maestrodevices.deviceConnectedCallback += Maestrodevices_deviceConnectedCallback;
                    // not connected so connect
                    await maestrodevices.OpenDeviceAsync(maestroDevice);
                }
                else
                {
                    // already connected so draw the controls
                    drawMaestroControls(maestroDevice);
                }
            }
        }

        private async void Maestrodevices_deviceConnectedCallback(MaestroDeviceListItem device)
        {
            await Dispatcher.RunAsync(
                     CoreDispatcherPriority.Normal,
                     new DispatchedHandler(() =>
                     {
                         // now connected
                         drawMaestroControls(device);
                     }));
        }

        private async void drawMaestroControls(MaestroDeviceListItem maestroItem)
        {
            UInt16 count = maestroItem.Maestro.ServoCount;
            UscSettings settings;
            settings = await maestroItem.Maestro.getUscSettings();
            Connected = true;
            tbDeviceName.Text = maestroItem.Name + " Connected";
            MaestroControl[] maestroChannels = new MaestroControl[count];
            for (UInt16 i = 0; i < count; i++)
            {
                maestroChannels[i] = new MaestroControl();
                maestroChannels[i].ChannelNumber = i;
               
                maestroChannels[i].Acceleration = Convert.ToUInt16(settings.channelSettings[i].acceleration);
                maestroChannels[i].Speed = Convert.ToUInt16(settings.channelSettings[i].speed);
                maestroPanel.Children.Add(maestroChannels[i]);
                maestroChannels[i].positionChanged += MainPage_positionChanged;
                maestroChannels[i].speedChanged += MainPage_speedChanged;
                maestroChannels[i].accelerationChanged += MainPage_accelerationChanged;
            }
            

        }

        private void updateTarget(byte Channel, UInt16 newPosition)
        {
            if (Connected)
            {
                maestroDevice.Maestro.setTarget(Channel,(UInt16)( newPosition * 4));
                //await Task.Delay(200);
            }

        }


        private  void MainPage_accelerationChanged(byte Channel, byte newAcceleration)
        {
            
        }

        private  void MainPage_speedChanged(byte Channel, byte newSpeed)
        {
            
        }

        private  void MainPage_positionChanged(byte Channel, UInt16 newPosition)
        {
            updateTarget(Channel, newPosition);
        }
    }

}

