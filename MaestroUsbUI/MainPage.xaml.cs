using MaestroUsb;
using MaestroUsbUI.Pages;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MaestroUsbUI
{

    

    public class MaestroBoard
    {
        public bool BoardConnected = false;
        public MaestroDeviceListItem maestro;
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        
        private ObservableCollection<MaestroDeviceListItem> listOfDevices;
        //  single maestro board
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");
        public event EventHandler<NotifyType> maestroConnectionEvent;
        

        public MainPage()
        {
            this.InitializeComponent();
            
        }

      

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            // retrieve a list of boards
            listOfDevices = new ObservableCollection<MaestroDeviceListItem>();
           
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
                if (Globals.maestroBoard == null)
                    Globals.maestroBoard = new MaestroUsbUI.MaestroBoard();
                Globals.maestroBoard.maestro = listOfDevices[lbDevices.SelectedIndex];
                // check if we are connected
                if (Globals.maestroBoard.maestro.device == null)
                {
                    Globals.maestroManager.deviceConnectedCallback += Maestrodevices_deviceConnectedCallback;
                    // not connected so connect
                    await Globals.maestroManager.OpenDeviceAsync(Globals.maestroBoard.maestro);
                    

                }
                else
                {
                    // already connected so draw the controls
                    // drawMaestroControls(maestroDevice);
                    status.Text = Globals.maestroBoard.maestro.Name + " Connected";
                     if (maestroConnectionEvent != null)
                        maestroConnectionEvent(this, NotifyType.Connected);
                }
            }
        }

        // gets a list of connected boards
        private async void getDeviceList()
        {
            // create devicemanager
            if (Globals.maestroManager == null)
            {
                Globals.maestroManager = new MaestroDeviceManager();
                // callback so that we are notified that the list of devices is ready
                Globals.maestroManager.deviceListReadyCallback += Maestrodevices_deviceListReadyCallback;
                // buid the list of currently plugged in boards
                Globals.maestroManager.BuildDeviceList();
            }
            else
            {
                if (listOfDevices != null)
                {
                    // run on ui thread
                    await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            foreach (MaestroDeviceListItem item in Globals.maestroManager.DeviceList)
                            {
                                // make the combobox list match our device list
                                listOfDevices.Add(item);
                            }
                            if (listOfDevices.Count > 0)
                            {
                                
                                // see if we already have a selected board
                                if (Globals.maestroBoard != null)
                                {
                                    if (listOfDevices.Contains(Globals.maestroBoard.maestro)){
                                        lbDevices.SelectedIndex = listOfDevices.IndexOf(Globals.maestroBoard.maestro);
                                    }
                                    else
                                    {
                                        lbDevices.SelectedIndex = 0; // select first item by default
                                    }
                                } else
                                {
                                    lbDevices.SelectedIndex = 0; // select first item by default
                                }
                               
                            }
                        }));
                }
            }

        }


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // empty everything to force the board scan again
            
        }

        private async void Maestrodevices_deviceListReadyCallback(Collection<MaestroDeviceListItem> devices)
        {
            //We now have completed new built list
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
            Globals.maestroManager.deviceAddedCallback += Maestrodevices_deviceAddedCallback;
            // notify us when a board is removed
            Globals.maestroManager.deviceRemovedCallback += Maestrodevices_deviceRemovedCallback;
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

                              status.Text = "Not Connected";
                              if (listOfDevices.Count > 0)
                              {
                                  // if we have more than one device select the first item
                                  lbDevices.SelectedIndex = 0;
                              }
                          }
                          if (maestroConnectionEvent != null)
                              maestroConnectionEvent(this, NotifyType.Removed);
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

        private async void Maestrodevices_deviceConnectedCallback(MaestroDeviceListItem device)
        {
            await Dispatcher.RunAsync(
                     CoreDispatcherPriority.Normal,
                     new DispatchedHandler(() =>
                     {
                         // now connected so draw our sliders
                         status.Text = device.Name + " Connected";

                         if (maestroConnectionEvent != null)
                             maestroConnectionEvent(this, NotifyType.Connected);
                     }));
            Globals.maestroBoard.maestro = device;
            Globals.maestroBoard.BoardConnected = true;
        }

        
        private void btnSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
           this.Frame.Navigate(typeof(SettingsPage), Globals.maestroBoard);
           
        }

        private void btnComms_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CommsPage), Globals.maestroBoard);
        }

        private void btnControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
           this.Frame.Navigate(typeof(ManualControl), Globals.maestroBoard);
           
        }

        private void btnSettings_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Brush blueBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            brSettings.BorderBrush = blueBrush;
        }

        private void btnSettings_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Brush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            brSettings.BorderBrush = whiteBrush;
        }

        private void btnControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Brush blueBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            brControl.BorderBrush = blueBrush;
        }

        private void btnControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Brush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            brControl.BorderBrush = whiteBrush;
        }

        private void btnComms_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Brush blueBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            brComms.BorderBrush = blueBrush;
        }

        private void btnComms_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Brush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            brComms.BorderBrush = whiteBrush;
        }

        private void btnArm_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Brush blueBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            brArm.BorderBrush = blueBrush;
        }

        private void btnArm_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Brush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            brArm.BorderBrush = whiteBrush;

        }

        private void btnArm_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(RoboticArmPage), Globals.maestroBoard);
        }

        private void btnBratt_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Brush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
            brBrat.BorderBrush = whiteBrush;
        }

        private void btnBratt_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Brush blueBrush = new SolidColorBrush(Windows.UI.Colors.SlateBlue);
            brBrat.BorderBrush = blueBrush;
        }

        private void btnBratt_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(BratBipedPage), Globals.maestroBoard);
        }
    }
}
