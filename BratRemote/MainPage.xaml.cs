using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BratRemote
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        UdpServer locatorServer;
        UdpServer commandServer;
        string IpAddress;

        public MainPage()
        {
            this.InitializeComponent();
            
        }

        public void setupUdpservers()
        {
            locatorServer = new UdpServer(9999);
            commandServer = new UdpServer(9998);
            locatorServer.StartListener();
            commandServer.StartListener();
            commandServer.OnDataReceived += CommandServer_OnDataReceived;
            locatorServer.OnDataReceived += locatorServer_OnDataReceived;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
            setupUdpservers();
        }


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            
            locatorServer.Close();
            commandServer.Close();
            base.OnNavigatingFrom(e);
        }

        private  void CommandServer_OnDataReceived(string senderIp, string data)
        {
            
            IpAddress = senderIp;
           
        }

        private void locatorServer_OnDataReceived(string senderIp, string data)
        {
            if (Globals.tcpClient == null)
            {
                Globals.tcpClient = new TcpClient(senderIp, 1500);
                Globals.tcpClient.OnConnected += TcpClient_OnConnected;
                Globals.tcpClient.OnDataReceived += TcpClient_OnDataReceived;
                Globals.tcpClient.OnError += TcpClient_OnError;
                Globals.tcpClient.Connect();
            }
        }

        private void TcpClient_OnError(string message)
        {
            throw new NotImplementedException();
        }

        private async void TcpClient_OnConnected(string Ip, int port)
        {
            // connected so stop listening for device
            await Dispatcher.RunAsync(
                   CoreDispatcherPriority.Normal,
                   new DispatchedHandler(() =>
                   {
                       tbStatus.Text = "Connected to " + Ip +" on Port "+port.ToString();
                   }));

        }

        private async void TcpClient_OnDataReceived(string data)
        {
            if (data.Contains("Brat"))
            {
                await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           this.Frame.Navigate(typeof(BratBipedPage));
                       }));
            }
            else
            {
                if (data.Contains("Robot Arm"))
                {
                    await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           this.Frame.Navigate(typeof(RoboticArmPage));
                       }));
                }

            }
        }

        // recieved a locator message so connect our tcp socket to it
        /* if (data.Contains("Brat"))
         {
             await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() =>
                    {
                        this.Frame.Navigate(typeof(BratBipedPage));
                    }));
         }
         else
         {
             if (data.Contains("Robot Arm"))
             {
                 await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() =>
                    {
                        this.Frame.Navigate(typeof(RoboticArmPage));
                    }));
             }

         }
         IpAddress = senderIp;
         await Dispatcher.RunAsync(
                   CoreDispatcherPriority.Normal,
                   new DispatchedHandler(() =>
                   {
                       tbStatus.Text = "Connected to " + IpAddress;
                   }));
    } */



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
            if (Globals.tcpClient.isConnected)
            {
                Globals.tcpClient.Send("Page=0");
            }
            this.Frame.Navigate(typeof(RoboticArmPage));
            
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
            if (Globals.tcpClient != null)
            {
                if (Globals.tcpClient.isConnected)
                {
                   Globals.tcpClient.Send("Page=1");
                }
                this.Frame.Navigate(typeof(BratBipedPage));
            }
        }
    }
}
