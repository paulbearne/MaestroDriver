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
        // add observable collection to show list of maestro devices in combobox
        private ObservableCollection<MaestroDeviceListItem> listOfDevices;
        private bool Connected = false;
        //  single maestro board
        private MaestroDeviceListItem maestroDevice;
        // all maestro boards
        private MaestroDeviceManager maestrodevices;
        // maestro settings
        private UscSettings settings;
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");
        public MainPage()
        {
            // init the comboboc collection
            listOfDevices = new ObservableCollection<MaestroDeviceListItem>();
            this.InitializeComponent();


        }

        // gets a list of connected boards
        private void getDeviceList()
        {
            // create devicemanager
            maestrodevices = new MaestroDeviceManager();
            // callback so that we are notified that the list of devices is ready
            maestrodevices.deviceListReadyCallback += Maestrodevices_deviceListReadyCallback;
            // buid the list of currently plugged in boards
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
                            // make the combobox list match our device list
                            listOfDevices.Add(item);
                        }
                        if (listOfDevices.Count > 0)
                        {
                            lbDevices.SelectedIndex = 0; // select first item by default
                        }
                    }));
            }
            //add device callbacks 
            // notify us when a board is plugged in
            maestrodevices.deviceAddedCallback += Maestrodevices_deviceAddedCallback;
            // notify us when a board is removed
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
                          // find the board thats been removed in the combobox list
                          var firstitem = listOfDevices.First(e => e.Id == deviceInfo.Id);
                          // get its item index
                          int index = listOfDevices.IndexOf(firstitem);
                          lbDevices.SelectedIndex = -1;
                          if (index > -1)
                          {
                              // remove the board from the list
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
                         // this can happen on first build as the event will be triggered even though we have built the list manually
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
                             // add the new board
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
            // retrieve a list of boards
            getDeviceList();
            // bind the combobox to the collection
            DeviceListSource.Source = listOfDevices;
            // add callback for a device being selected
            lbDevices.SelectionChanged += LbDevices_SelectionChanged;

        }

        private async void LbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbDevices.SelectedIndex > -1)
            {
                // get the board
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
                         // now connected so draw our sliders
                         drawMaestroControls(device);
                     }));
        }


      //  private unsafe int getservostructsize()
        //{
          //  return sizeof(ServoStatus);
        //}

        private async void drawMaestroControls(MaestroDeviceListItem maestroItem)
        {
            // get the number of servos on the board
            UInt16 count = maestroItem.Maestro.ServoCount;
            // get all the settings stored on the board
            settings = await maestroItem.Maestro.getUscSettings();
           
            //  ServoStatus[] servos = await maestroItem.Maestro.getVariablesMiniMaestro(getservostructsize());
            Connected = true;
            tbDeviceName.Text = maestroItem.Name + " Connected";
            // Create an array of  controls
            MaestroControl[] maestroChannels = new MaestroControl[count];
            for (UInt16 i = 0; i < count; i++)
            {
                // add a speed , acceleration and target controls to the app
                maestroChannels[i] = new MaestroControl();
                maestroChannels[i].ChannelNumber = i;
                // update the controls to show current values from the board
                maestroChannels[i].Acceleration = Convert.ToUInt16(settings.channelSettings[i].acceleration);
                maestroChannels[i].Speed = Convert.ToUInt16(settings.channelSettings[i].speed);
                maestroPanel.Children.Add(maestroChannels[i]);
                // add the callbacks for changes
                maestroChannels[i].positionChanged += MainPage_positionChanged;
                maestroChannels[i].speedChanged += MainPage_speedChanged;
                maestroChannels[i].accelerationChanged += MainPage_accelerationChanged;
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
    }

}

