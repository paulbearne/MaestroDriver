using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BratRemote
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BratBipedPage : Page
    {
        string Ipaddress;
        UdpServer locatorServer;
        UdpServer commandServer;
        Boolean controlsEnabled = false;
        bool firstconnect = false;
        
        public BratBipedPage()
        {
            this.InitializeComponent();
            controlState(false);
            
        }

        public void setupUdpservers()
        {
            locatorServer = new UdpServer(9999);
            locatorServer.StartListener();
            locatorServer.OnDataReceived += locatorServer_OnDataReceived;
            commandServer = new UdpServer(9998);
            commandServer.StartListener();
            commandServer.OnDataReceived += CommandServer_OnDataReceived;
           
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
           
            setupUdpservers();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {

            if (locatorServer != null)
            {
                locatorServer.Close();
            }
            if (commandServer != null)
            {
                commandServer.Close();
            }
            base.OnNavigatingFrom(e);
        }

        // check page change
        private async void pagechanged(string data, string senderIp)
        {
            if (data.Contains("Robot Arm"))
            {

                await Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal,
                           new DispatchedHandler(() =>
                           {
                               Frame.Navigate(typeof(RoboticArmPage));
                           }));

            }
            else
            {
                if (data.Contains("Brat"))
                {
                    
                                      
                    if (!firstconnect)
                    {
                        firstconnect = true;
                        controlsEnabled = true;
                        // first time send values command to get slider values
                        await commandServer.SendMessage("values", senderIp);
                        controlState(controlsEnabled);
                    }
                }
                else
                {
                    if (data.Contains("Navigation"))
                    {
                        await Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal,
                           new DispatchedHandler(() =>
                           {
                               Frame.Navigate(typeof(MainPage));
                           }));
                    }
                    else
                    {
                        // been disconnected form brat 
                        await Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal,
                           new DispatchedHandler(() =>
                           {
                               tbDeviceName.Text = "Not Connected";
                           }));
                        firstconnect = false;
                        if (controlsEnabled)
                        {
                            controlsEnabled = false;
                            controlState(controlsEnabled);

                        }
                    }

                }

            }
            await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           tbDeviceName.Text = "Connected to " + senderIp;
                       }));
        }

        private void locatorServer_OnDataReceived(string senderIp, string data)
        {
            pagechanged(data, senderIp);
        }


        private async void controlState(Boolean enabled)
        {
            await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           btnBack.IsEnabled = enabled;
                           btnGetUpBack.IsEnabled = enabled;
                           btngetUpFront.IsEnabled = enabled;
                           btnHeader.IsEnabled = enabled;
                           btnKick.IsEnabled = enabled;
                           btnKickRight.IsEnabled = enabled;
                           btnLeft.IsEnabled = enabled;
                           btnRight.IsEnabled = enabled;
                           btnUp.IsEnabled = enabled;
                           btnrollLeft.IsEnabled = enabled;
                           btnrollRight.IsEnabled = enabled;
                           btnOk.IsEnabled = enabled;
                           btnPower.IsEnabled = enabled;
                           slAngle.IsEnabled = enabled;
                           slRH.IsEnabled = enabled;
                           slRK.IsEnabled = enabled;
                           slRA.IsEnabled = enabled;
                           slLH.IsEnabled = enabled;
                           slLK.IsEnabled = enabled;
                           slLA.IsEnabled = enabled;
                       }));
        }

        private async void CommandServer_OnDataReceived(string senderIp, string data)
        {
            Ipaddress = senderIp;
            
            // value command resposne is of Value,Cmd,num,num
            if (data.Contains("value"))
            {
                
                string[] parameters = data.Split(',');
                UInt16 cmd = UInt16.Parse(parameters[2]);
                Int16 value = Int16.Parse(parameters[3]);
                await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            Status.Text = "recieved value " + value.ToString();

                            switch (cmd)
                            {
                                case 0:
                                    slRH.Value = value;
                                    break;
                                case 1:
                                    slRK.Value = value;
                                    break;
                                case 2:
                                    slRA.Value = value;
                                    break;
                                case 3:
                                    slLH.Value = value;
                                    break;
                                case 4:
                                    slLK.Value = value;
                                    break;
                                case 5:
                                    slLA.Value = value;
                                    break;
                                case 6:
                                    slAngle.Value = value;
                                    break;
                            }
                        }));

            }
                        
            // shouldn't get anything but value here
        }
 
        private async void btnUp_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,2",Ipaddress);
            }
        }

        private async void btnrollLeft_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,6",Ipaddress);
            }
        }

        private async void btnrollRight_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,7",Ipaddress);
            }
        }

        private async void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,3",Ipaddress);
            }
        }

        private async void btnRight_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,5",Ipaddress);
            }
        }

        private async void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,8",Ipaddress);
            }
        }


        private async void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,4",Ipaddress);
            }
        }

        private async void btnKick_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,9",Ipaddress);
            }
        }

        private async void btnGetUpBack_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,10",Ipaddress);
            }
        }

        private async void btngetUpFront_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,11",Ipaddress);
            }
        }

        private async void btnPower_Unchecked(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,0",Ipaddress);
            }
        }

       
        private async void btnPower_Checked(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,1",Ipaddress);
            }
        }

        private async void slRH_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,0," + slRH.Value.ToString(),Ipaddress);
            }
        }

        private async void slRK_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,1," + slRK.Value.ToString(),Ipaddress);
            }
        }

        private async void slRA_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,2," + slRA.Value.ToString(),Ipaddress);
            }
        }

        private async void slLH_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,3," + slLH.Value.ToString(),Ipaddress);
            }
        }

        private async void slLK_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,4," + slLK.Value.ToString(),Ipaddress);
            }
        }

        private async void slLA_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,5," + slLA.Value.ToString(),Ipaddress);
            }
        }

        private async void slAngle_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,6," + slAngle.Value.ToString(),Ipaddress);
            }
        }

        private async void btnKickRight_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,12");
            }
        }

        private async void btnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,13");
            }
        }

        private async void btnPageBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                if (Ipaddress != null)
                {
                    
                    await locatorServer.SendMessage("Navigation ", Ipaddress);
                }
                Frame.GoBack();

            }
        }
    }
}
