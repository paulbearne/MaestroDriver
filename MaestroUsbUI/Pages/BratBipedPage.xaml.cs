using MaestroUsb;
using MaestroUsbUI;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

namespace MaestroUsbUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BratBipedPage : Page
    {
        private MaestroDeviceListItem maestroDevice;
        // maestro settings
        private UscSettings settings;
        private ServoStatus[] servoStatus;
        private Int16[] Offsets = { 0, 0, 0, 0 ,0,0 };
        private const uint period = 10;

        private double RHP = 90;
        private double RKP = 90;
        private double RAP = 90;
        private double LHP = 90;
        private double LKP = 90;
        private double LAP = 90;
        private string[] cmdstring = new string[7];
        private bool stepN;
        private UInt16 stepAngle = 20;
        private int last_angle = 90;
        private int Speed = 30;
        private string fname;
        private string IpAddress;
      //  private Timer commandTimer;
        private int Range = 1;
        private UdpServer udpserver;
        

        public BratBipedPage()
        {
            this.InitializeComponent();
           
           
        }



        private void startCommandServer()
        {
            udpserver = new UdpServer(9998);
            udpserver.StartListener();
          //  Globals.commandBuffer = new List<udpBufferItem>();
            udpserver.OnDataReceived += Commandserver_OnDataReceived;
            udpserver.OnError += Udpserver_OnError;
            Globals.udpserver.OnDataReceived += Udpserver_OnDataReceived;
            Globals.locatorMessage = "Brat";
            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (udpserver != null)
            {
                udpserver.Close();
            }
            base.OnNavigatedFrom(e);
        }

        // check page change
        private async void pagechanged(string data, string senderIp)
        {
            if (data.Contains("Robot Arm"))
            {
                // switch to robot arm page make sure we are in Ui thread
                await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           this.Frame.Navigate(typeof(RoboticArmPage), Globals.maestroBoard);
                       }));
            }
            if (data.Contains("Brat"))
            {
                
            }
            if (data.Contains("Navigation"))
            {
                // switch to robot arm page
                await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           this.Frame.Navigate(typeof(MainPage), Globals.maestroBoard);
                       }));
            }
            await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           tbTcpStatus.Text = "Connected to " + senderIp;
                       }));
        }

        // handle remote page change
        private void Udpserver_OnDataReceived(string senderIp, string data)
        {
            pagechanged(data, senderIp);
        }

        private void Udpserver_OnError(string message)
        {
            throw new NotImplementedException();
        }

        private async void Commandserver_OnDataReceived(string senderIp, string data)
        {

            // we have recieved data so stop broadcasting 
            
            if (await doRemoteCommand(data, senderIp))
            {

            }
            //pagechanged(data, senderIp);

        }
        
        private async Task<bool> doRemoteCommand(string cmd, string ip)
        {
            IpAddress = ip;
            Debug.WriteLine("Ip Address " + IpAddress);
            
            string[] message = cmd.Split(',');
            // message 0 should be either cmd , value or values
            if (message[0].Contains("values"))
            {
                await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            
                            status.Text = "Sending Servo positions to "+ IpAddress.ToString();
                            cmdstring[0] = "value,cmd,0," + slRH.Value.ToString();
                            cmdstring[1] = "value,cmd,1," + slRK.Value.ToString();
                            cmdstring[2] = "value,cmd,2," + slRA.Value.ToString();
                            cmdstring[3] = "value,cmd,3," + slLH.Value.ToString();
                            cmdstring[4] = "value,cmd,4," + slLK.Value.ToString();
                            cmdstring[5] = "value,cmd,5," + slLA.Value.ToString();
                            cmdstring[6]= "value,cmd,6," + slAngle.Value.ToString();
                            
                        }));

                for (int i = 0; i < 7; i++)
                {
                    await udpserver.SendMessage(cmdstring[i], IpAddress);
                }


            }
            else
            {
                if (message[0].Contains("value"))
                {
                    if (message.Length == 4) {
                        await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            UInt16 cmdnumber = UInt16.Parse(message[2]);
                            switch (cmdnumber)
                            {
                                case 0:
                                    slRH.Value = UInt16.Parse(message[3]);
                                    break;
                                case 1:
                                    slRK.Value = UInt16.Parse(message[3]);
                                    break;
                                case 2:
                                    slRA.Value = UInt16.Parse(message[3]);
                                    break;
                                case 3:
                                    slLH.Value = UInt16.Parse(message[3]);
                                    break;
                                case 4:
                                    slLK.Value = UInt16.Parse(message[3]);
                                    break;
                                case 5:
                                    slLA.Value = UInt16.Parse(message[3]);
                                    break;
                                case 6:
                                    slAngle.Value = UInt16.Parse(message[3]);
                                    break;
                            }
                        }));
                    }
                }
                else
                {
                    if (message[0].Contains("cmd"))
                    {
                        if (message.Length == 2)
                        {
                            await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            UInt16 cmdnumber = UInt16.Parse(message[1]);
                            switch (cmdnumber)
                            {
                                case 0:
                                    // power off command;
                                    break;
                                case 1:
                                    // power on command
                                    break;
                                case 2:
                                    walkForward(stepAngle);
                                    break;
                                case 3:
                                    turnLeft(stepAngle);
                                    break;
                                case 4:
                                    walkBackward(-stepAngle);
                                    break;
                                case 5:
                                    turnRight(stepAngle);
                                    break;
                                case 6:
                                    rollLeft();
                                    break;
                                case 7:
                                    rollLeft();
                                    break;
                                case 8:
                                    homeServos();
                                    break;
                                case 9:
                                    // kick
                                    kickLeft();
                                    break;
                                case 10:
                                    // getup back
                                    getUpFromBack();
                                    break;
                                case 11:
                                    // get up front 
                                    getUpFromFront();
                                    break;
                                case 12:
                                    // get up front 
                                    kickRight();
                                    break;
                                case 13:
                                    // get up front 
                                    headThrust();
                                    break;
                            }
                        }));
                        }
                    }
                }
            }
            return true;
        }

        private async Task<bool> groupMove(int p1, int p2, int p3, int p4, int p5, int p6, int Speed)
        {

            UInt16 speed = maestroDevice.Maestro.normalSpeedToExponentialSpeed((UInt16)(Speed));
            bool done = false;
            for (byte channel = 0; channel < 6; channel++)
            {
                maestroDevice.Maestro.setSpeed(channel, 10);
                
            }
            maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, p1) + (Offsets[0] * 4)));
            maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, p2) + (Offsets[1] * 4)));
            maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, p3) + (Offsets[2] * 4)));
            maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, p4) + (Offsets[3] * 4)));
            maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, p5) + (Offsets[4] * 4)));
            maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, p6) + (Offsets[5] * 4)));

            do
            {
                if (await maestroDevice.Maestro.updateMaestroVariables())
                {

                    if ((maestroDevice.Maestro.servoStatus[0].position == maestroDevice.Maestro.servoStatus[0].target) &&
                        (maestroDevice.Maestro.servoStatus[1].position == maestroDevice.Maestro.servoStatus[1].target) &&
                        (maestroDevice.Maestro.servoStatus[2].position == maestroDevice.Maestro.servoStatus[2].target) &&
                        (maestroDevice.Maestro.servoStatus[3].position == maestroDevice.Maestro.servoStatus[3].target) &&
                        (maestroDevice.Maestro.servoStatus[4].position == maestroDevice.Maestro.servoStatus[4].target) &&
                        (maestroDevice.Maestro.servoStatus[5].position == maestroDevice.Maestro.servoStatus[5].target))
                    {

                        done = true;
                    }
                }
            } while (done == false);

           
            return done;


        }

        private async void walkForward(int angle)
        {
            if (maestroDevice != null)
            {
                angle = (byte)(angle * Range);
                if (stepN)
                {
                    angle = (byte)(90 + angle);
                    await groupMove(last_angle, last_angle, 75, last_angle, last_angle, 55, Speed);
                    await groupMove(angle, angle, 75, angle, angle, 55, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                }
                else
                {
                    angle = (byte)(90 - angle);
                    await groupMove(last_angle, last_angle, 120, last_angle, last_angle, 105, Speed);
                    await groupMove(angle, angle, 125, angle, angle, 105, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                }
                last_angle = angle;
            }
        }

        private async void turnRight(int angle)
        {
            if (maestroDevice != null)
            {
                if (angle > 125)
                    angle = 125;
                else if (angle < 110)
                    angle = 110;
                await groupMove(90, 90, 90, 90, 90, 90, Speed);
                await groupMove(90, 90, 75, 90, 90, 55, Speed);
                await groupMove(90, 90, 75, 90, 90, 75, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 75, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 75, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 90, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 90, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 125, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 105, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 105, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 105, Speed);
                await groupMove(angle, angle, 105, angle, angle, 105, Speed);
                await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                await groupMove(90, 90, 90, 90, 90, 90, Speed);
                last_angle = 90;
            }
        }

        private async void turnLeft(int angle)
        {
            if (maestroDevice != null)
            {
                if (angle < 55)
                    angle = 55;
                else if (angle > 70)
                    angle = 70;
                await groupMove(90, 90, 90, 90, 90, 90, Speed);
                await groupMove(90, 90, 125, 90, 90, 105, Speed);
                await groupMove(90, 90, 105, 90, 90, 105, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 105, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 105, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 90, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 90, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 75, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 55, Speed);
                await groupMove(angle + (90 - angle) / 2, angle + (90 - angle) / 2, 75, angle + (90 - angle) / 2, angle + (90 - angle) / 2, 75, Speed);
                await groupMove(angle, angle, 75, angle, angle, 75, Speed);
                await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                await groupMove(90, 90, 90, 90, 90, 90, Speed);
                last_angle = 90;
            }
        }

        private async void kickRight()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(90, 90, 48, 90, 90, 104, 500);
                await groupMove(130, 35, 110, 90, 90, 110, 500);
                await groupMove(70, 100, 110, 90, 90, 110, 250);
                await groupMove(90, 90, 110, 90, 90, 110, 500);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                last_angle = 90;
            }
        }

        private async void kickLeft()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(90, 90, 76, 90, 90, 132, 500);
                await groupMove(90, 90, 70, 50, 145, 70, 500);
                await groupMove(90, 90, 70, 110, 80, 70, 250);
                await groupMove(90, 90, 70, 90, 90, 70, 500);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                last_angle = 90;
            }
        }

        private async void headThrust()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(25, 45, 90, 155, 135, 90, 500);
                await groupMove(70, 125, 90, 110, 65, 90, 300);
                await Task.Delay(100);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                last_angle = 90;
            }
        }


       /* private async void walkForward(int angle)
        {
          //  for (int i = 0; i < 2; i++)
           // {
                if (stepN)
                {
                    angle = (byte)(90 + angle);
                    await groupMove(last_angle, last_angle, 55, last_angle, last_angle, 75, Speed);
                    await groupMove(angle, angle, 75, angle, angle, 75, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                    //last_angle = angle;
                }
                else
                {
                    angle = (byte)(90 - angle);
                    await groupMove(last_angle, last_angle, 105, last_angle, last_angle, 125, Speed);
                    await groupMove(angle, angle, 105, angle, angle, 105, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                }
                last_angle = angle;
            //}
        }*/

        private async void walkBackward(int angle)
        {
            if (maestroDevice != null)
            {
                if (stepN)
                {
                    angle = (byte)(90 + angle);
                    await groupMove(last_angle, last_angle, 55, last_angle, last_angle, 75, Speed);
                    await groupMove(angle, angle, 75, angle, angle, 75, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                    //last_angle = angle;
                }
                else
                {
                    angle = (byte)(90 - angle);
                    await groupMove(last_angle, last_angle, 105, last_angle, last_angle, 125, Speed);
                    await groupMove(angle, angle, 105, angle, angle, 105, Speed);
                    await groupMove(angle, angle, 90, angle, angle, 90, Speed);
                    stepN = !stepN;
                }
                last_angle = angle;
            }
        }

      /*  private async void turnLeft()
        {
            await groupMove(90, 90, 90, 90, 90, 90, 500);
            await groupMove(90, 90, 75, 90, 90, 55, 450);
            await groupMove(90, 90, 75, 90, 90, 75, 50);
            await groupMove(55, 55, 75, 55, 55, 75, 500);
            await groupMove(55, 55, 90, 55, 55, 90, 250);
            await groupMove(90, 90, 90, 90, 90, 90, 500);
            await groupMove(90, 90, 75, 90, 90, 55, 450);
            await groupMove(90, 90, 75, 90, 90, 75, 50);
            await groupMove(55, 55, 75, 55, 55, 75, 500);
            await groupMove(55, 55, 90, 55, 55, 90, 250);
            await groupMove(90, 90, 90, 90, 90, 90, 500);
        }

        private async void turnRight()
        {
            await groupMove(90, 90, 90, 90, 90, 90, 500);
            await groupMove(90, 90, 55, 90, 90, 75, 450);
            await groupMove(90, 90, 75, 90, 90, 75, 50);
            await groupMove(55, 55, 75, 55, 55, 75, 500);
            await groupMove(55, 55, 90, 55, 55, 90, 250);
            await groupMove(90, 90, 90, 90, 90, 90, 500);
            await groupMove(90, 90, 55, 90, 90, 75, 450);
            await groupMove(90, 90, 75, 90, 90, 75, 50);
            await groupMove(55, 55, 75, 55, 55, 75, 500);
            await groupMove(55, 55, 90, 55, 55, 90, 250);
            await groupMove(90, 90, 90, 90, 90, 90, 500);
        }*/

        private async void getUpFromFront()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(60, 0, 90, 120, 170, 90, 500);
                await groupMove(120, 0, 60, 120, 170, 90, 500);
                await groupMove(170, 0, 90, 10, 180, 90, 500);
                await groupMove(90, 90, 90, 90, 90, 90, 1000);
            }
        }

        private async void getUpFromBack()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(90, 180, 90, 90, 0, 90, 500);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
            }
        }

        private async void rollLeft()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(90, 90, 90, 40, 90, 90, 100);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
            }
        }

        private async void rollRight()
        {
            if (maestroDevice != null)
            {
                await groupMove(90, 90, 90, 90, 90, 90, 500);
                await groupMove(140, 90, 90, 90, 90, 90, 100);
                await groupMove(90, 90, 90, 90, 90, 90, 500);
            }
        }

        private void homeServos()
        {
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, 90) + (Offsets[0] * 4)));
                maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, 90) + (Offsets[1] * 4)));
                maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, 90) + (Offsets[2] * 4)));
                maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, 90) + (Offsets[3] * 4)));
                maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, 90) + (Offsets[4] * 4)));
                maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, 90) + (Offsets[5] * 4)));
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                Globals.locatorMessage = "Brat ";
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                status.Text = maestroDevice.Name + " Connected";
                UInt16 count = maestroDevice.Maestro.ServoCount;
                // get all the settings stored on the board

                settings = await maestroDevice.Maestro.getUscSettings();
                await maestroDevice.Maestro.updateMaestroVariables();
                Task.WaitAll();  // wait until we have all the data
                servoStatus = maestroDevice.Maestro.servoStatus;
               
                fname = ApplicationData.Current.LocalFolder.Path + "\\Brat.cfg";
                if (File.Exists(fname))
                {
                    // load offsets
                    loadOffsets();
                }
                else
                {
                    // use 0 offset
                    maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, RHP) + (Offsets[0] * 4)));
                    maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, RKP) + (Offsets[1] * 4)));
                    maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, RAP) + (Offsets[2] * 4)));
                    maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, LHP) + (Offsets[3] * 4)));
                    maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, LKP) + (Offsets[4] * 4)));
                    maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, LAP) + (Offsets[5] * 4)));
                }

                

            }
            startCommandServer();
        }



        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();

            }
        }

        private void btnrollRight_Click(object sender, RoutedEventArgs e)
        {
            rollRight();
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            walkForward(20);
        }

        private void slRH_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbRH != null)
            {
                if (Convert.ToUInt16(tbRH.Text) != slRH.Value)
                {
                    tbRH.Text = slRH.Value.ToString();
                    maestroDevice.Maestro.setTarget(0, (UInt16)(slRH.Value * 4));
                }
            }
        }

        private void tbRH_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbRH.Text) != slRH.Value)
                {
                    slRH.Value = Convert.ToUInt16(tbRH.Text);
                   

                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void slRK_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbRK != null)
            {
                if (Convert.ToUInt16(tbRK.Text) != slRK.Value)
                {
                    tbRK.Text = slRK.Value.ToString();
                    maestroDevice.Maestro.setTarget(1, (UInt16)(slRK.Value * 4));
                }
            }
        }

        private void tbRK_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbRK.Text) != slRK.Value)
                {
                    slRK.Value = Convert.ToUInt16(tbRK.Text);
                    
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }
            
        

        private void slRA_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbRA != null)
            {
                if (Convert.ToUInt16(tbRA.Text) != slRA.Value)
                {
                    tbRA.Text = slRA.Value.ToString();
                    if (maestroDevice != null)
                    {
                        maestroDevice.Maestro.setTarget(2, (UInt16)(slRA.Value * 4));
                    }
                }
            }
        }

        private void tbRA_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbRA.Text) != slRA.Value)
                {
                    slRA.Value = Convert.ToUInt16(tbRA.Text);
                   

                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void slLH_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbLH != null)
            {
                if (Convert.ToUInt16(tbLH.Text) != slLH.Value)
                {
                    tbLH.Text = slLH.Value.ToString();
                    if (maestroDevice != null)
                    {
                        maestroDevice.Maestro.setTarget(3, (UInt16)(slLH.Value * 4));
                    }
                }
            }
        }

        private void tbLH_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbLH.Text) != slLH.Value)
                {
                    slLH.Value = Convert.ToUInt16(tbLH.Text);
                    

                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void slLK_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbLK != null)
            {
                if (Convert.ToUInt16(tbLK.Text) != slLK.Value)
                {
                    tbLK.Text = slLK.Value.ToString();
                    if (maestroDevice != null)
                    {
                        maestroDevice.Maestro.setTarget(4, (UInt16)(slLK.Value * 4));
                    }
                }
            }
        }

        private void tbLK_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbLK.Text) != slLK.Value)
                {
                    slLK.Value = Convert.ToUInt16(tbLK.Text);
                    

                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void slLA_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbLA != null)
            {
                if (Convert.ToUInt16(tbLA.Text) != slLA.Value)
                {
                    tbLA.Text = slLA.Value.ToString();
                    if (maestroDevice != null)
                    {
                        maestroDevice.Maestro.setTarget(5, (UInt16)(slLA.Value * 4));
                    }
                }
            }
        }

        private void tbLA_TextChanged(object sender, TextChangedEventArgs e)
        {

            try
            {
                if (Convert.ToUInt16(tbLA.Text) != slLA.Value)
                {
                    slLA.Value = Convert.ToUInt16(tbLK.Text);
                    

                }
            } catch(Exception error)
            {
                Debug.WriteLine(error);
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Offsets[0] = Convert.ToInt16(slRH.Value - 1500) ;
            Offsets[1] = Convert.ToInt16(slRK.Value - 1500);
            Offsets[2] = Convert.ToInt16(slRA.Value - 1500);
            Offsets[3] = Convert.ToInt16(slLH.Value - 1500);
            Offsets[4] = Convert.ToInt16(slLK.Value - 1500);
            Offsets[5] = Convert.ToInt16(slLA.Value - 1500);
            saveOffsets();
        }

        private void loadOffsets()
        {
            StreamReader file = File.OpenText(fname);
            try
            {
                Offsets[0] = Int16.Parse(file.ReadLine());
                slRH.Value = 1500 + Offsets[0];
                Offsets[1] = Int16.Parse(file.ReadLine());
                slRK.Value = 1500 + Offsets[1];
                Offsets[2] = Int16.Parse(file.ReadLine());
                slRA.Value = 1500 + Offsets[2];
                Offsets[3] = Int16.Parse(file.ReadLine());
                slLH.Value = 1500 + Offsets[3];
                Offsets[4] = Int16.Parse(file.ReadLine());
                slLK.Value = 1500 + Offsets[4];
                Offsets[5] = Int16.Parse(file.ReadLine());
                slLA.Value = 1500 + Offsets[5];
                if (!file.EndOfStream)
                {
                    stepAngle = UInt16.Parse(file.ReadLine());
                    slAngle.Value = stepAngle;

                }
                file.Dispose();
                if (maestroDevice != null)
                {
                    maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, RHP) + (Offsets[0] * 4)));
                    maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, RKP) + (Offsets[1] * 4)));
                    maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, RAP) + (Offsets[2] * 4)));
                    maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, LHP) + (Offsets[3] * 4)));
                    maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, LKP) + (Offsets[4] * 4)));
                    maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, LAP) + (Offsets[5] * 4)));
                }
            } catch(Exception e)
            {
                file.Dispose();
                Debug.WriteLine("Error Reading Offsets" + e);
            }
        }

        private void saveOffsets()
        {
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
            StreamWriter file = File.CreateText(fname);
            file.WriteLine(Offsets[0].ToString());
            file.WriteLine(Offsets[1].ToString());
            file.WriteLine(Offsets[2].ToString());
            file.WriteLine(Offsets[3].ToString());
            file.WriteLine(Offsets[4].ToString());
            file.WriteLine(Offsets[5].ToString());
            file.WriteLine(stepAngle.ToString());
            file.Flush();
            file.Dispose();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            turnRight(stepAngle);
        }

        

        private void btnrollLeft_Click(object sender, RoutedEventArgs e)
        {
            rollLeft();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            turnLeft(stepAngle);
        }

        private void btnPower_Checked(object sender, RoutedEventArgs e)
        {
            if (btnPower.IsChecked == true)
            {
                // enable all servos
            }
            else
            {
                // disbale servos
            }
        }

        private void slAngle_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (tbAngle != null)
            {
                if (Convert.ToUInt16(tbAngle.Text) != slAngle.Value)
                {
                    stepAngle = Convert.ToUInt16(slAngle.Value);
                    tbAngle.Text = stepAngle.ToString();
                }
            }
        }

        private void tbAngle_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Convert.ToUInt16(tbAngle.Text) != slAngle.Value)
                {
                    slAngle.Value = Convert.ToUInt16(tbAngle.Text);
                  

                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            homeServos();
        }

        private void btnWalkBack_Click(object sender, RoutedEventArgs e)
        {
            walkForward(-20);
        }

        private void btnKick_Click(object sender, RoutedEventArgs e)
        {
            kickLeft();
        }

        private void btnKickRight_Click(object sender, RoutedEventArgs e)
        {
            kickRight();
        }

        private void btnGetUpBack_Click(object sender, RoutedEventArgs e)
        {
            getUpFromBack();
        }

        private void btngetUpFront_Click(object sender, RoutedEventArgs e)
        {
            getUpFromFront();
        }

        private void btnHeader_Click(object sender, RoutedEventArgs e)
        {
            headThrust();
        }
    }
}
