using MaestroUsb;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MaestroUsbUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RoboticArmPage : Page
    {

        private MaestroDeviceListItem maestroDevice;
        // maestro settings
        private string IpAddress;
        private UdpServer udpserver;



        private Int16[] Offsets = { 0, 0, 0, 0, 0, 0 };
        private ServoStatus[] servoStatus;


        // Arm dimensions (mm). 
        private double baseHeight = 4.5;    // Base height to X/Y plane 13.4cm
        private static double upperArmLength = 107.00;     // Shoulder-to-elbow "bone" " 10.7cm
        private static double lowerArmLength = 118.00;       // Elbow-to-wrist "bone" " 11.8cm
        private double gripperLength = 110.0;     // Gripper length, to middle of grip surface  11.0cm

        private const Byte BaseServo = 0;
        private const Byte ShoulderServo = 1;
        private const Byte ElbowServo = 2;
        private const Byte WristServo = 3;
        private const Byte WristRotateServo = 4;
        private const Byte GripServo = 5;

        // Set physical limits (in degrees) by using manual control slider
        private double baseMin = 10.0;         // Fully CCW
        private double baseMid = 90.0;
        private double baseMax = 170.0;       // Fully CW

        private double shoulderMin = 0;        // Max forward motion
        private double shoulderMid = 90;
        private double shoulderMax = 180.0;       // Max rearward motion

        private double elbowMin = 0;        // Max upward motion
        private double elbowMid = 90;
        private double elbowMax = 180.0;       // Max downward motion

        private double wristMin = 5.0;         // Max downward motion
        private static double wristMid = 90;
        private double wristMax = 180.0;       // Max upward motion

        private double gripperMin;        // Fully open
        private static double gripperMid = 45.0;
        private double gripperMax;       // Fully closed

        private double wristRotateMin;
        private static double wristRotateMid = 90.0;
        private double wristRotateMax;


        private static double SPEED_DEFAULT = 1.0;

        private string[] cmdstring = new string[20];
        private string fname;


        // IK function return values
        enum IKstatus
        {
            IK_SUCCESS = 0,
            IK_ERROR = 1          // Desired position not possible
        }

        // Arm parking positions
        enum parktype
        {
            MidPointPosition = 1,     // Servos at midpoints
            ReadyPosition = 2        // Arm at Ready-To-Run position
        }
        // Ready-To-Run arm position. See descriptions below
        // NOTE: Have the arm near this position before turning on the 
        //       servo power to prevent whiplash
        // Arm Position X Gripper Tip side to side
        //              Y Gripper Tip distance above base center
        //              Z Gripper Tip height from surface


        private static double armReadyPosX = 87;
        private static double armReadyPosY = 45;
        private static double armReadyPosZ = 90;
        private static double armReadyGripperAngle = 0.0;
        private static double armReadyGripperPos = gripperMid;

        private static double armReadyWristRotate = wristRotateMid;


        // Global variables for arm position, and initial settings
        // 3D kinematics
        private double X = armReadyPosX;         // Left/right distance (mm) from base centerline - 0 is straight 
        private double Y = armReadyPosY;          // Distance (mm) out from base center
        private double Z = armReadyPosZ;          // Height (mm) from surface (i.e. X/Y plane)
        private double GA = armReadyGripperAngle;        // Gripper angle. Servo degrees, relative to X/Y plane - 0 is horizontal
        private double G = armReadyGripperPos;          // Gripper jaw opening. Servo degrees - midpoint is halfway open

        private double WR = armReadyWristRotate;       // Wrist Rotate. Servo degrees - midpoint is horizontal

        private double Speed = SPEED_DEFAULT;

        // Pre-calculations
        private double upperArmSq = upperArmLength * upperArmLength;
        private double lowerArmSq = lowerArmLength * lowerArmLength;

        private double baseServoPos;
        private double shoulderServoPos;
        private double elbowServoPos;
        private double wristServoPos;
        private double wristRotateServoPos;
        private double gripperServoPos;

        private UscSettings settings;


        // 6dof robot controller 
        public RoboticArmPage()
        {
            this.InitializeComponent();
        }



        private async Task<bool> groupMove(Int16 p1, Int16 p2, Int16 p3, Int16 p4, Int16 p5, Int16 p6, Int16 Speed)
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
                
            }
            if (data.Contains("Brat"))
            {
                // switch to robot arm page
                await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           this.Frame.Navigate(typeof(BratBipedPage), Globals.maestroBoard);
                       }));
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
                           tbDeviceName.Text = "Connected to " + senderIp;
                       }));
        }


        protected async override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                Globals.locatorMessage = "Robot Arm ";
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
                
                settings = await maestroDevice.Maestro.getUscSettings();
                Task.WaitAll();
                fname = ApplicationData.Current.LocalFolder.Path + "\\Arm.cfg";
                if (File.Exists(fname))
                {
                    // load offsets
                   // loadConfig();
                }
                updateMaxMin();

            }
           // startCommandServer();
            Update();
        }


        private async void updateMaxMin()
        {
            if (maestroDevice != null)
            {
                slBaseMin.Value = settings.channelSettings[BaseServo].minimum;
                slBaseMax.Value = settings.channelSettings[BaseServo].maximum;
                slShoulderMin.Value = settings.channelSettings[ShoulderServo].minimum;
                slShoulderMax.Value = settings.channelSettings[ShoulderServo].maximum;
                slElbowMin.Value = settings.channelSettings[ElbowServo].minimum;
                slElbowMax.Value = settings.channelSettings[ElbowServo].maximum;
                slWristMin.Value = settings.channelSettings[WristServo].minimum;
                slWristMax.Value = settings.channelSettings[WristServo].maximum;
                slWristRotateMin.Value = settings.channelSettings[WristRotateServo].minimum;
                slWristRotateMax.Value = settings.channelSettings[WristRotateServo].maximum;
                slGripperMin.Value = settings.channelSettings[GripServo].minimum;
                slGripperMax.Value = settings.channelSettings[GripServo].maximum;
                await maestroDevice.Maestro.updateMaestroVariables();
                servoStatus = maestroDevice.Maestro.servoStatus;
                slBaseMidPoint.Value = servoStatus[BaseServo].position;
                slShoulderMidPoint.Value = servoStatus[ShoulderServo].position;
                slElbowMidPoint.Value = servoStatus[ElbowServo].position;
                slWristMidPoint.Value = servoStatus[WristServo].position;
                slWristRotateMidPoint.Value = servoStatus[WristRotateServo].position;
                slGripperMidPoint.Value = servoStatus[GripServo].position;
            }
            else
            {
                // set sliders to defaults
                slBaseMin.Value = angleToMicroseconds(500,2500,baseMin);
                slBaseMax.Value = angleToMicroseconds(500, 2500, baseMax);
                slShoulderMin.Value = angleToMicroseconds(500, 2500, shoulderMin);
                slShoulderMax.Value = angleToMicroseconds(500, 2500, shoulderMax);
                slElbowMin.Value = angleToMicroseconds(500, 2500, elbowMin);
                slElbowMax.Value = angleToMicroseconds(500, 2500, elbowMax);
                slWristMin.Value = angleToMicroseconds(500, 2500, wristMin);
                slWristMax.Value = angleToMicroseconds(500, 2500, wristMax);
                slWristRotateMin.Value = angleToMicroseconds(500, 2500, wristRotateMin);
                slWristRotateMax.Value = angleToMicroseconds(500, 2500, wristRotateMax);
                slGripperMin.Value = angleToMicroseconds(500, 2500, gripperMin);
                slGripperMax.Value = angleToMicroseconds(500, 2500, gripperMax);
                slBaseMidPoint.Value = angleToMicroseconds(500, 2500, baseMid); 
                slShoulderMidPoint.Value = angleToMicroseconds(500, 2500, shoulderMid);
                slElbowMidPoint.Value = angleToMicroseconds(500, 2500, elbowMid);
                slWristMidPoint.Value = angleToMicroseconds(500, 2500, wristMid);
                slWristRotateMidPoint.Value = angleToMicroseconds(500, 2500, wristRotateMin); 
                slGripperMidPoint.Value = angleToMicroseconds(500, 2500, gripperMid); 
            }

        }

        private void loadConfig()
        {

            StreamReader file = File.OpenText(fname);
            try
            {
                
                baseMid = Double.Parse(file.ReadLine());
                shoulderMid = Double.Parse(file.ReadLine());
                elbowMid = Double.Parse(file.ReadLine());
                wristMid = Double.Parse(file.ReadLine());
                gripperMid = Double.Parse(file.ReadLine());
                wristRotateMid = Double.Parse(file.ReadLine());
                armReadyPosX = Double.Parse(file.ReadLine());
                armReadyPosY = Double.Parse(file.ReadLine());
                armReadyPosZ = Double.Parse(file.ReadLine());
                armReadyGripperAngle = Double.Parse(file.ReadLine());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }


        private void saveConfig()
        {
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
            StreamWriter file = File.CreateText(fname);
            file.WriteLine(baseMid.ToString());
            file.WriteLine(shoulderMid.ToString());
            file.WriteLine(elbowMid.ToString());
            file.WriteLine(wristMid.ToString());
            file.WriteLine(gripperMid.ToString());
            file.WriteLine(wristRotateMid.ToString());
            file.WriteLine(armReadyPosX.ToString());
            file.WriteLine(armReadyPosY.ToString());
            file.WriteLine(armReadyPosZ.ToString());
            file.WriteLine(armReadyGripperAngle.ToString());
        }

        private void startCommandServer()
        {
            udpserver = new UdpServer(9998);
            udpserver.StartListener();
            //  Globals.commandBuffer = new List<udpBufferItem>();
            udpserver.OnDataReceived += Commandserver_OnDataReceived;
            udpserver.OnError += Udpserver_OnError;
            Globals.udpserver.OnDataReceived += Udpserver_OnDataReceived;
            Globals.locatorMessage = "Robot Arm";


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
            // pagechanged(data, senderIp);
            if (await doRemoteCommand(data, senderIp))
            {

            }
            await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            tbDeviceName.Text = "Connected to remote address " + senderIp;
                        }));
        }

        private async Task<bool> doRemoteCommand(string cmd, string ip)
        {
            IpAddress = ip;
            Debug.WriteLine("Ip Address " + IpAddress);

            string[] message = cmd.Split(',');
            // message 0 should be either cmd , value or values
            if (message[0].Contains("values"))
            {
               /* await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {

                            cmdstring[0] = "value,cmd,0," + slBaseMidPoint.Value.ToString();
                            cmdstring[1] = "value,cmd,1," + slShoulderMidPoint.Value.ToString();
                            cmdstring[2] = "value,cmd,2," + slElbowMidPoint.Value.ToString();
                            cmdstring[3] = "value,cmd,3," + slWristMidPoint.Value.ToString();
                            cmdstring[4] = "value,cmd,4," + slWristRotateMidPoint.Value.ToString();
                            cmdstring[5] = "value,cmd,5," + slGripperMidPoint.Value.ToString();
                            cmdstring[6] = "value,cmd,6," + slBaseMin.Value.ToString();
                            cmdstring[7] = "value,cmd,7," + slShoulderMin.Value.ToString();
                            cmdstring[8] = "value,cmd,8," + slElbowMin.Value.ToString();
                            cmdstring[9] = "value,cmd,9," + slWristMin.Value.ToString();
                            cmdstring[10] = "value,cmd,10," + slWristRotateMin.Value.ToString();
                            cmdstring[11] = "value,cmd,11," + slGripperMin.Value.ToString();
                            cmdstring[12] = "value,cmd,12," + slBaseMax.Value.ToString();
                            cmdstring[13] = "value,cmd,13," + slShoulderMax.Value.ToString();
                            cmdstring[14] = "value,cmd,14," + slElbowMax.Value.ToString();
                            cmdstring[15] = "value,cmd,15," + slWristMax.Value.ToString();
                            cmdstring[16] = "value,cmd,16," + slWristRotateMax.Value.ToString();
                            cmdstring[17] = "value,cmd,17," + slGripperMax.Value.ToString();

                        }));
                for (int i = 0; i < 18; i++)
                {
                    if ((cmdstring != null) && (IpAddress != null))
                    {
                        await udpserver.SendMessage(cmdstring[i], IpAddress);
                    }
                }
                */
            }
            else
            {
                if (message[0].Contains("value"))
                {
                    if (message.Length == 4)
                    {
                        await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            UInt16 cmdnumber = UInt16.Parse(message[2]);
                            switch (cmdnumber)
                            {
                                case 0:
                                    slBaseMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 1:
                                    slElbowMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 2:
                                    slShoulderMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 3:
                                    slWristMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 4:
                                    slWristRotateMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 5:
                                    slGripperMidPoint.Value = UInt16.Parse(message[3]);
                                    break;
                                case 6:
                                    slBaseMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 7:
                                    slElbowMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 8:
                                    slShoulderMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 9:
                                    slWristMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 10:
                                    slWristRotateMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 11:
                                    slGripperMin.Value = UInt16.Parse(message[3]);
                                    break;
                                case 12:
                                    slBaseMax.Value = UInt16.Parse(message[3]);
                                    break;
                                case 13:
                                    slElbowMax.Value = UInt16.Parse(message[3]);
                                    break;
                                case 14:
                                    slShoulderMax.Value = UInt16.Parse(message[3]);
                                    break;
                                case 15:
                                    slWristMax.Value = UInt16.Parse(message[3]);
                                    break;
                                case 16:
                                    slWristRotateMax.Value = UInt16.Parse(message[3]);
                                    break;
                                case 17:
                                    slGripperMax.Value = UInt16.Parse(message[3]);
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
                                    servo_park(parktype.MidPointPosition);
                                    Update();
                                    break;
                                case 3:
                                    // park set
                                    break;
                                case 4:
                                    servo_park(parktype.ReadyPosition);
                                    Update();
                                    break;
                                case 5:
                                    // ready point set
                                    break;
                                case 6:
                                    // run
                                    break;
                                case 7:
                                    // save
                                    break;

                            }
                        }));
                        }
                    }
                }
            }
            return true;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();

            }
        }

        // rads  = 180 /pi *
        private double Degrees(double rads)
        {
            return (180.0 / Math.PI) * rads;
        }

        private double Radians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }



        private double map_float(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return ((x - in_min) * (out_max - out_min) / (in_max - in_min)) + out_min;
        }

        private IKstatus set_arm(double x, double y, double z, double grip_angle_d)
        {
            //grip angle in radians for use in calculations
            double grip_angle_r = Radians(grip_angle_d);

            // Base angle and radial distance from x,y coordinates
            double bas_angle_r = Math.Atan2(x, y);
            double rdist = Math.Sqrt((x * x) + (y * y));

            // rdist is y coordinate for the arm
            y = rdist;

            // Grip offsets calculated based on grip angle
            double grip_off_z = (Math.Sin(grip_angle_r)) * gripperLength;
            double grip_off_y = (Math.Cos(grip_angle_r)) * gripperLength;

            // Wrist position
            double wrist_z = (z - grip_off_z) - baseHeight;
            double wrist_y = y - grip_off_y;

            // Shoulder to wrist distance (AKA sw)
            double s_w = (wrist_z * wrist_z) + (wrist_y * wrist_y);
            double s_w_sqrt = Math.Sqrt(s_w);

            // s_w angle to ground
            double a1 = Math.Atan2(wrist_z, wrist_y);

            // s_w angle to humerus
            double a2 = Math.Acos(((upperArmSq - lowerArmSq) + s_w) / (2 * upperArmLength * s_w_sqrt));

            // Shoulder angle
            double shl_angle_r = a1 + a2;
            // If result is NAN or Infinity, the desired arm position is not possible
            if (Double.IsNaN(shl_angle_r) || Double.IsInfinity(shl_angle_r))
                return IKstatus.IK_ERROR;
            double shl_angle_d = Degrees(shl_angle_r);

            // Elbow angle
            double elb_angle_r = Math.Acos((upperArmSq + lowerArmSq - s_w) / (2 * upperArmLength * lowerArmLength));
            // If result is NAN or Infinity, the desired arm position is not possible
            if (Double.IsNaN(elb_angle_r) || Double.IsInfinity(elb_angle_r))
                return IKstatus.IK_ERROR;
            double elb_angle_d = Degrees(elb_angle_r);
            double elb_angle_dn = -(180.0 - elb_angle_d);

            // Wrist angle
            double wri_angle_d = (grip_angle_d - elb_angle_dn) - shl_angle_d;

            // Calculate servo angles
            // Calc relative to servo midpoint to allow compensation for servo alignment
            double bas_pos = baseMid + Degrees(bas_angle_r);
            double shl_pos = shoulderMid + (shl_angle_d - 90.0);
            double elb_pos = elbowMid - (elb_angle_d - 90.0);
            double wri_pos = wristMid + wri_angle_d;

            // check all servos are valid 
            if (bas_pos < baseMin || bas_pos > baseMax ||
                shl_pos < shoulderMin || shl_pos > shoulderMax ||
                elb_pos < elbowMin || elb_pos > elbowMax ||
                wri_pos < wristMin || wri_pos > wristMax)
                return IKstatus.IK_ERROR;


            baseServoPos = angleToMicroseconds(500, 2500, bas_pos);
            shoulderServoPos = angleToMicroseconds(500, 2500, shl_pos);
            elbowServoPos = angleToMicroseconds(500, 2500, elb_pos);
            wristServoPos = angleToMicroseconds(500, 2500, wri_pos);
            // Position the servos
            // 3D kinematics
            // write the servos
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(BaseServo, maestroDevice.Maestro.angleToMicroseconds(BaseServo, bas_pos));
                // Bas_Servo.writeMicroseconds(deg_to_us(bas_pos));
                maestroDevice.Maestro.setTarget(ShoulderServo, maestroDevice.Maestro.angleToMicroseconds(ShoulderServo, shl_pos));
                //Shl_Servo.writeMicroseconds(deg_to_us(shl_pos));
                maestroDevice.Maestro.setTarget(ElbowServo, maestroDevice.Maestro.angleToMicroseconds(ElbowServo, elb_pos));
                //Elb_Servo.writeMicroseconds(deg_to_us(elb_pos));
                //Wri_Servo.writeMicroseconds(deg_to_us(wri_pos));
                maestroDevice.Maestro.setTarget(WristServo, maestroDevice.Maestro.angleToMicroseconds(WristServo, wri_pos));
            }

#if DEBUG
            Debug.Write("X: ");
            Debug.Write(x);
            Debug.Write("  Y: ");
            Debug.Write(y);
            Debug.Write("  Z: ");
            Debug.Write(z);
            Debug.Write("  GA: ");
            Debug.Write(grip_angle_d);
            Debug.Write("");
            Debug.Write("Base Pos: ");
            Debug.Write(bas_pos);
            Debug.Write("  Shld Pos: ");
            Debug.Write(shl_pos);
            Debug.Write("  Elbw Pos: ");
            Debug.Write(elb_pos);
            Debug.Write("  Wrst Pos: ");
            Debug.Write(wri_pos);
            Debug.Write("bas_angle_d: ");
            Debug.Write(Degrees(bas_angle_r));
            Debug.Write("  shl_angle_d: ");
            Debug.Write(shl_angle_d);
            Debug.Write("  elb_angle_d: ");
            Debug.Write(elb_angle_d);
            Debug.Write("  wri_angle_d: ");
            Debug.WriteLine(wri_angle_d);
            Debug.WriteLine("");
#endif

            return IKstatus.IK_SUCCESS;
        }

        // Move servos to parking position
        void servo_park(parktype park_type)
        {
            switch (park_type)
            {
                // All servos at midpoint
                case parktype.MidPointPosition:
                    baseServoPos = angleToMicroseconds(500, 2500, baseMid);
                    shoulderServoPos = angleToMicroseconds(500, 2500, shoulderMid);
                    elbowServoPos = angleToMicroseconds(500, 2500, elbowMid);
                    wristServoPos = angleToMicroseconds(500, 2500, wristMid);
                    gripperServoPos = angleToMicroseconds(500, 2500, gripperMid);
                    wristRotateServoPos = angleToMicroseconds(500, 2500, wristRotateMid);
                    // Position the servos
                    // 3D kinematics
                    // write the servos
                    if (maestroDevice != null)
                    {
                        maestroDevice.Maestro.setTarget(BaseServo, maestroDevice.Maestro.angleToMicroseconds(BaseServo, baseMid));
                        // Bas_Servo.writeMicroseconds(deg_to_us(BAS_MID));
                        // Shl_Servo.writeMicroseconds(deg_to_us(shoulderMid));
                        maestroDevice.Maestro.setTarget(ShoulderServo, maestroDevice.Maestro.angleToMicroseconds(ShoulderServo, shoulderMid));
                        // Elb_Servo.writeMicroseconds(deg_to_us(elbowMid));
                        maestroDevice.Maestro.setTarget(ElbowServo, maestroDevice.Maestro.angleToMicroseconds(ElbowServo, elbowMid));
                        // Wri_Servo.writeMicroseconds(deg_to_us(wristMid));
                        maestroDevice.Maestro.setTarget(WristServo, maestroDevice.Maestro.angleToMicroseconds(WristServo, wristMid));
                        // Gri_Servo.writeMicroseconds(deg_to_us(gripperMid));
                        maestroDevice.Maestro.setTarget(GripServo, maestroDevice.Maestro.angleToMicroseconds(GripServo, gripperMid));

                        //Wro_Servo.writeMicroseconds(deg_to_us(wristRotateMid));
                        maestroDevice.Maestro.setTarget(WristRotateServo, maestroDevice.Maestro.angleToMicroseconds(WristRotateServo, wristRotateMid));
                    }
                    break;

                // Ready-To-Run position
                case parktype.ReadyPosition:
                    // 3D kinematics
                    set_arm(armReadyPosX, armReadyPosY, armReadyPosZ, armReadyGripperAngle);
                    gripperServoPos = angleToMicroseconds(500, 2500, gripperMid);
                    wristRotateServoPos = angleToMicroseconds(500, 2500, wristRotateMid);

                    if (maestroDevice != null)
                    {
                        //Gri_Servo.writeMicroseconds(deg_to_us(armReadyGripperPos));
                        maestroDevice.Maestro.setTarget(GripServo, maestroDevice.Maestro.angleToMicroseconds(GripServo, gripperMid));
                        // Wro_Servo.writeMicroseconds(deg_to_us(armReadyWristRotate));
                        maestroDevice.Maestro.setTarget(WristRotateServo, maestroDevice.Maestro.angleToMicroseconds(WristRotateServo, armReadyWristRotate));
                    }
                    break;
            }

            return;
        }

        private Point DrawBase()
        {
            int rectindex = canvas.Children.IndexOf(BaseRect);
            int lineindex = canvas.Children.IndexOf(Base);
            Line line = (Line)canvas.Children[lineindex]; ;
            Rectangle rect = (Rectangle)canvas.Children[rectindex];
            rect.Stroke = new SolidColorBrush(Windows.UI.Colors.DarkBlue);
            rect.Fill = new SolidColorBrush(Windows.UI.Colors.DarkBlue);
            int height = Convert.ToUInt16(Math.Abs(line.Y1 - line.Y2));
            line.X1 = Canvas.GetLeft(rect) + (rect.Width / 2);
            line.Y1 = Canvas.GetTop(rect);
            line.X2 = line.X1;
            line.Y2 = line.Y1 - height;
            return new Point(line.X2, line.Y2);
        }


        public UInt16 microsecondsToAngle(ushort min, ushort max, ushort us)
        {
            UInt16 angle = Convert.ToUInt16((us - min) / ((max - min) / 180));
            Debug.WriteLine(angle.ToString());
            return (UInt16)(angle);
        }

        public UInt16 angleToMicroseconds(ushort min, ushort max, double angle)
        {
            UInt16 us = Convert.ToUInt16(((((max - min) / 180) * angle)) + min);
            return (UInt16)(us);
        }


        private void DrawMainArm(double x, double y)
        {
            try
            {
                Line shoulder = (Line)canvas.Children[canvas.Children.IndexOf(Shoulder)];
                Line elbow = (Line)canvas.Children[canvas.Children.IndexOf(Elbow)];
                Line wrist = (Line)canvas.Children[canvas.Children.IndexOf(Wrist)];
                Line gripper = (Line)canvas.Children[canvas.Children.IndexOf(Gripper)];
                Ellipse shoulderServo = (Ellipse)canvas.Children[canvas.Children.IndexOf(ShoulderPoint)];
                Ellipse elbowServo = (Ellipse)canvas.Children[canvas.Children.IndexOf(ElbowPoint)];
                Ellipse wristServo = (Ellipse)canvas.Children[canvas.Children.IndexOf(WristPoint)];
                Ellipse gripperServo = (Ellipse)canvas.Children[canvas.Children.IndexOf(GripperPoint)];

                shoulder.X1 = x;
                shoulder.Y1 = y;
                RotateTransform r1 = new RotateTransform();
                r1.CenterX = x;
                r1.CenterY = y;
                r1.Angle = microsecondsToAngle(500, 2500, (ushort)shoulderServoPos);
                shoulder.RenderTransform = r1;
                Point p1 = r1.TransformPoint(new Windows.Foundation.Point(shoulder.X2, shoulder.Y2));

                Canvas.SetLeft(shoulderServo, x - 12);
                Canvas.SetTop(shoulderServo, y - 4);
                elbow.X1 = p1.X;
                elbow.Y1 = p1.Y;
                elbow.X2 = p1.X + 150;
                elbow.Y2 = p1.Y;
                RotateTransform r2 = new RotateTransform();
                r2.CenterX = elbow.X1;
                r2.CenterY = elbow.Y1;
                r2.Angle = microsecondsToAngle(500, 2500, (ushort)elbowServoPos);
                // draw the server centered on arm joint
                elbow.RenderTransform = r2;
                Point p2 = r2.TransformPoint(new Windows.Foundation.Point(elbow.X2, elbow.Y2));
                Canvas.SetLeft(elbowServo, p1.X - 12);
                Canvas.SetTop(elbowServo, p1.Y - 8);
                wrist.X1 = p2.X;
                wrist.Y1 = p2.Y;
                wrist.X2 = p2.X + 50;
                wrist.Y2 = p2.Y;
                RotateTransform r3 = new RotateTransform();
                r3.CenterX = wrist.X1;
                r3.CenterY = wrist.Y1;
                r3.Angle = microsecondsToAngle(500, 2500, (ushort)wristServoPos);
                // draw the server centered on arm joint
                wrist.RenderTransform = r3;
                Point p3 = r3.TransformPoint(new Windows.Foundation.Point(wrist.X2, wrist.Y2));
                Point p3_1 = r3.TransformPoint(new Windows.Foundation.Point(wrist.X2 - 12, wrist.Y2 - 4));
                Canvas.SetLeft(wristServo, p2.X - 12);
                Canvas.SetTop(wristServo, p2.Y - 8);
                gripper.X1 = p3.X;
                gripper.Y1 = p3.Y;
                gripper.X2 = p3.X + 50;
                gripper.Y2 = p3.Y;
                RotateTransform r4 = new RotateTransform();
                r4.CenterX = gripper.X1;
                r4.CenterY = gripper.Y1;
                r4.Angle = microsecondsToAngle(500, 2500, (ushort)wristServoPos);
                // draw the server centered on arm joint
                gripper.RenderTransform = r4;
                Point p4 = r4.TransformPoint(new Windows.Foundation.Point(gripper.X2, gripper.Y2));
                Canvas.SetLeft(gripperServo, p3.X - 12);
                Canvas.SetTop(gripperServo, p3.Y - 8);


            }
            catch (Exception e)
            {
                Debug.Write(e);

            }

        }


        private void DrawOverHeadView()
        {
            try
            {
                Line overhead = (Line)canvas.Children[canvas.Children.IndexOf(OverHeadArm)];


                RotateTransform r1 = new RotateTransform();
                r1.CenterX = overhead.X1;
                r1.CenterY = overhead.Y1;
                r1.Angle = microsecondsToAngle(500, 2500, (ushort)baseServoPos) + 270;
                overhead.RenderTransform = r1;
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void DrawGripper()
        {
            try
            {
                Line gripperLeft = (Line)canvas.Children[canvas.Children.IndexOf(GripperLeft)];
                Line gripperRight = (Line)canvas.Children[canvas.Children.IndexOf(GripperRight)];

                double gripperAngle = ((microsecondsToAngle(1600, 500, (ushort)(gripperServoPos)) / 2) - 12.5);


                RotateTransform r1 = new RotateTransform();
                r1.CenterX = gripperLeft.X1;
                r1.CenterY = gripperLeft.Y1;
                r1.Angle = -gripperAngle;
                gripperLeft.RenderTransform = r1;
                RotateTransform r2 = new RotateTransform();
                r2.CenterX = gripperRight.X1;
                r2.CenterY = gripperRight.Y1;
                r2.Angle = gripperAngle;
                gripperRight.RenderTransform = r2;
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void DrawWristRotate()
        {
            try
            {
                Line wristrotate = (Line)canvas.Children[canvas.Children.IndexOf(WristRotate)];


                RotateTransform r1 = new RotateTransform();
                r1.CenterX = wristrotate.X1;
                r1.CenterY = wristrotate.Y1;
                r1.Angle = microsecondsToAngle(500, 2500, (ushort)(wristRotateServoPos)) + 270;
                wristrotate.RenderTransform = r1;

            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }


        private void Update()
        {
            // base we'll draw arm projected into screen
            // update screen
            Point Basetop = DrawBase();
            DrawGripper();
            DrawWristRotate();
            DrawOverHeadView();
            DrawMainArm(Basetop.X, Basetop.Y);
            // update servos

        }

        private void slShoulderMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderServoPos = slShoulderMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(ShoulderServo, Convert.ToUInt16(shoulderServoPos));
            }
        }

        private void slElbowMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowServoPos = slElbowMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(ElbowServo, Convert.ToUInt16(elbowServoPos));
            }
        }

        private void slWristMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristServoPos = slWristMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(WristServo, Convert.ToUInt16(wristServoPos));
            }
        }

        private void slBaseMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseServoPos = slBaseMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(BaseServo, Convert.ToUInt16(baseServoPos));
            }
        }

        private void slWristRotateMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateServoPos = slWristRotateMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(WristServo, Convert.ToUInt16(wristServoPos));
            }
        }

        private void slGripperMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperServoPos = slGripperMidPoint.Value;
            Update();
            if (maestroDevice != null)
            {
                maestroDevice.Maestro.setTarget(GripServo, Convert.ToUInt16(gripperServoPos));
            }
        }


        private void updateChannelMin(byte channel,UInt16 value)
        {
            if ( maestroDevice != null)
            {
                    settings.channelSettings[channel].minimum = value;
                    maestroDevice.Maestro.setUscSettings(settings,false);
            }
        }

        
        private void slBaseMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseMin = microsecondsToAngle(500, 2500, (ushort)slBaseMin.Value);
            updateChannelMin(BaseServo, Convert.ToUInt16(baseMin));
        }

        private void slShoulderMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderMin = microsecondsToAngle(500, 2500, (ushort)slShoulderMin.Value);
            updateChannelMin(ShoulderServo, Convert.ToUInt16(shoulderMin));
        }

        private void slElbowMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowMin = microsecondsToAngle(500, 2500, (ushort)slElbowMin.Value);
            updateChannelMin(ElbowServo, Convert.ToUInt16(elbowMin));
        }

        private void slWristMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristMin = microsecondsToAngle(500, 2500, (ushort)slWristMin.Value);
            updateChannelMin(WristServo, Convert.ToUInt16(wristMin));
        }

        private void slWristRotateMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateMin = microsecondsToAngle(500, 2500, (ushort)slWristRotateMin.Value);
            updateChannelMin(WristRotateServo, Convert.ToUInt16(wristRotateMin));
        }

        private void slGripperMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperMin = microsecondsToAngle(500, 2500, (ushort)slGripperMin.Value);
            updateChannelMin(GripServo, Convert.ToUInt16(gripperMin));
        }


        private void updateChannelMax(byte channel, UInt16 value)
        {
            if (maestroDevice != null)
            {
                settings.channelSettings[channel].maximum = value;
                maestroDevice.Maestro.setUscSettings(settings, false);
            }
        }


        private void slBaseMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseMax = microsecondsToAngle(500, 2500, (ushort)slBaseMax.Value);
            updateChannelMax(BaseServo, Convert.ToUInt16(baseMax));
        }

        private void slShoulderMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderMax = microsecondsToAngle(500, 2500, (ushort)slShoulderMax.Value);
            updateChannelMax(ShoulderServo, Convert.ToUInt16(shoulderMax));
        }

        private void slElbowMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowMax = microsecondsToAngle(500, 2500, (ushort)slElbowMax.Value);
            updateChannelMax(ElbowServo, Convert.ToUInt16(elbowMax));
        }

        private void slWristMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristMax = microsecondsToAngle(500, 2500, (ushort)slWristMax.Value);
            updateChannelMax(WristServo, Convert.ToUInt16(wristMax));
        }

        private void slWristRotateMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateMax = microsecondsToAngle(500, 2500, (ushort)slWristRotateMax.Value);
            updateChannelMax(WristRotateServo, Convert.ToUInt16(wristRotateMax));
        }

        private void slGripperMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperMax = microsecondsToAngle(500, 2500, (ushort)slGripperMax.Value);
            updateChannelMax(GripServo, Convert.ToUInt16(gripperMax));
        }

        private void btnParkPos_Click(object sender, RoutedEventArgs e)
        {
            baseMid = microsecondsToAngle(500, 2500, (ushort)slBaseMidPoint.Value);
            shoulderMid = microsecondsToAngle(500, 2500, (ushort)slShoulderMidPoint.Value);
            elbowMid = microsecondsToAngle(500, 2500, (ushort)slElbowMidPoint.Value);
            wristMid = microsecondsToAngle(500, 2500, (ushort)slWristMidPoint.Value);
            gripperMid = microsecondsToAngle(500, 2500, (ushort)slGripperMidPoint.Value);
            wristRotateMid = microsecondsToAngle(500, 2500, (ushort)slWristMidPoint.Value);
        }

        private void btnReadyPos_Click(object sender, RoutedEventArgs e)
        {
           armReadyGripperAngle = microsecondsToAngle(500, 2500, (ushort)slGripperMidPoint.Value);
           armReadyPosX = microsecondsToAngle(500, 2500, (ushort)slShoulderMidPoint.Value);
           armReadyPosY = microsecondsToAngle(500, 2500, (ushort)slElbowMidPoint.Value);
           armReadyPosZ = microsecondsToAngle(500, 2500, (ushort)slWristMidPoint.Value);
            
        }

        private void btnRunPos_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            saveConfig();
        }

        private void btnGotoReadyPos_Click_1(object sender, RoutedEventArgs e)
        {
            servo_park(parktype.ReadyPosition);
            Update();
        }

        private void btnGetParkPos_Click_1(object sender, RoutedEventArgs e)
        {
            servo_park(parktype.MidPointPosition);
            Update();
        }
    }
}

