
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BratRemote
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RoboticArmPage : Page
    {

        string Ipaddress;
        UdpServer locatorServer;
        UdpServer commandServer;
        Boolean controlsEnabled = false;
        bool firstconnect = false;



        private Int16[] Offsets = { 0, 0, 0, 0, 0, 0 };


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
        private double shoulderMid = 0;
        private double shoulderMax = 180.0;       // Max rearward motion

        private double elbowMin = 0;        // Max upward motion
        private double elbowMid = 0;
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

        private string[] cmdstring = new string[18];


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


        // 6dof robot controller 
        public RoboticArmPage()
        {
            this.InitializeComponent();
        }




        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            setupUdpservers();
            Update();
            
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

        public void setupUdpservers()
        {
            locatorServer = new UdpServer(9999);
            locatorServer.StartListener();
            locatorServer.OnDataReceived += locatorServer_OnDataReceived;
            commandServer = new UdpServer(9998);
            commandServer.StartListener();
            commandServer.OnDataReceived += CommandServer_OnDataReceived;

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
                            //Status.Text = "recieved value " + value.ToString();

                            switch (cmd)
                            {
                                case 0:
                                    slBaseMidPoint.Value = value;
                                    break;
                                case 1:
                                    slElbowMidPoint.Value = value;
                                    break;
                                case 2:
                                    slShoulderMidPoint.Value = value;
                                    break;
                                case 3:
                                    slWristMidPoint.Value = value;
                                    break;
                                case 4:
                                    slWristRotateMidPoint.Value = value;
                                    break;
                                case 5:
                                    slGripperMidPoint.Value = value;
                                    break;
                                case 6:
                                    slBaseMin.Value = value;
                                    break;
                                case 7:
                                    slElbowMin.Value = value;
                                    break;
                                case 8:
                                    slShoulderMin.Value = value;
                                    break;
                                case 9:
                                    slWristMin.Value = value;
                                    break;
                                case 10:
                                    slWristRotateMin.Value = value;
                                    break;
                                case 11:
                                    slGripperMin.Value = value;
                                    break;
                                case 12:
                                    slBaseMax.Value = value;
                                    break;
                                case 13:
                                    slElbowMax.Value = value;
                                    break;
                                case 14:
                                    slShoulderMax.Value = value;
                                    break;
                                case 15:
                                    slWristMax.Value = value;
                                    break;
                                case 16:
                                    slWristRotateMax.Value = value;
                                    break;
                                case 17:
                                    slGripperMax.Value = value;
                                    break;

                            }
                        }));

            }
            

            // shouldn't get anything but value here
        }

        private async void controlState(Boolean enabled)
        {
            await Dispatcher.RunAsync(
                       CoreDispatcherPriority.Normal,
                       new DispatchedHandler(() =>
                       {
                           btnBack.IsEnabled = enabled;
                           btnGetParkPos.IsEnabled = enabled;
                           btnGotoReadyPos.IsEnabled = enabled;
                           btnParkPos.IsEnabled = enabled;
                           btnReadyPos.IsEnabled = enabled;
                           btnRunPos.IsEnabled = enabled;
                           btnSave.IsEnabled = enabled;
                           slBaseMin.IsEnabled = enabled;
                           slBaseMax.IsEnabled = enabled;
                           slBaseMidPoint.IsEnabled = enabled;
                           slShoulderMin.IsEnabled = enabled;
                           slShoulderMax.IsEnabled = enabled;
                           slShoulderMidPoint.IsEnabled = enabled;
                           slElbowMin.IsEnabled = enabled;
                           slElbowMax.IsEnabled = enabled;
                           slElbowMidPoint.IsEnabled = enabled;
                           slWristMin.IsEnabled = enabled;
                           slWristMax.IsEnabled = enabled;
                           slWristMidPoint.IsEnabled = enabled;
                           slWristRotateMin.IsEnabled = enabled;
                           slWristRotateMax.IsEnabled = enabled;
                           slWristRotateMidPoint.IsEnabled = enabled;
                           slGripperMin.IsEnabled = enabled;
                           slGripperMax.IsEnabled = enabled;
                           slGripperMidPoint.IsEnabled = enabled;
                       }));
        }

        // check page change
        private async void pagechanged(string data, string senderIp)
        {
            if (data.Contains("Robot Arm"))
            {

                Ipaddress = senderIp;
                await Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        new DispatchedHandler(() =>
                        {
                            tbDeviceName.Text = "Robot Arm IPAddress :" + Ipaddress;
                        }));
                if (!firstconnect)
                {
                    firstconnect = true;
                    controlsEnabled = true;
                    // first time send values command to get slider values
                    await commandServer.SendMessage("values", Ipaddress);
                    controlState(controlsEnabled);
                }
            }
            else
            {
                if (data.Contains("Brat"))
                {
                    await Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal,
                           new DispatchedHandler(() =>
                           {
                               Frame.Navigate(typeof(BratBipedPage));
                           }));
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
            Ipaddress = senderIp;
            pagechanged(data, senderIp);
        }

        private async void btnBack_Click(object sender, RoutedEventArgs e)
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

                    break;

                // Ready-To-Run position
                case parktype.ReadyPosition:
                    // 3D kinematics
                    set_arm(armReadyPosX, armReadyPosY, armReadyPosZ, armReadyGripperAngle);
                    gripperServoPos = angleToMicroseconds(500, 2500, gripperMid);
                    wristRotateServoPos = angleToMicroseconds(500, 2500, wristRotateMid);


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

        private async void slShoulderMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderServoPos = slShoulderMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,2," + slShoulderMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void slElbowMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowServoPos = slElbowMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,1," + slElbowMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristServoPos = slWristMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,3," + slWristMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void slBaseMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseServoPos = slBaseMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,0," + slBaseMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristRotateMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateServoPos = slWristRotateMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,4," + slWristRotateMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void slGripperMidPoint_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperServoPos = slGripperMidPoint.Value;
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,5," + slGripperMidPoint.Value.ToString(), Ipaddress);
            }
        }

        private async void btnGetParkPos_Click(object sender, RoutedEventArgs e)
        {

            servo_park(parktype.MidPointPosition);
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,2", Ipaddress);
            }
        }

        private async void btnGotoReadyPos_Click(object sender, RoutedEventArgs e)
        {
            servo_park(parktype.ReadyPosition);
            Update();
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,4", Ipaddress);
            }
        }
         
        private async void slBaseMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseMin = microsecondsToAngle(500, 2500, (ushort)slBaseMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,6," + slBaseMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slShoulderMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderMin = microsecondsToAngle(500, 2500, (ushort)slShoulderMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,8," + slShoulderMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slElbowMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowMin = microsecondsToAngle(500, 2500, (ushort)slElbowMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,7," + slElbowMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristMin = microsecondsToAngle(500, 2500, (ushort)slWristMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,9," + slWristMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristRotateMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateMin = microsecondsToAngle(500, 2500, (ushort)slWristRotateMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,10," + slWristRotateMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slGripperMin_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperMin = microsecondsToAngle(500, 2500, (ushort)slGripperMin.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,11," + slGripperMin.Value.ToString(), Ipaddress);
            }
        }

        private async void slBaseMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            baseMax = microsecondsToAngle(500, 2500, (ushort)slBaseMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,12," + slBaseMax.Value.ToString(), Ipaddress);
            }
        }

        private async void slShoulderMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            shoulderMax = microsecondsToAngle(500, 2500, (ushort)slShoulderMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,14," + slShoulderMax.Value.ToString(), Ipaddress);
            }
        }

        private async void slElbowMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            elbowMax = microsecondsToAngle(500, 2500, (ushort)slElbowMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,13," + slElbowMax.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristMax = microsecondsToAngle(500, 2500, (ushort)slWristMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,15," + slWristMax.Value.ToString(), Ipaddress);
            }
        }

        private async void slWristRotateMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            wristRotateMax = microsecondsToAngle(500, 2500, (ushort)slWristRotateMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,16," + slWristRotateMax.Value.ToString(), Ipaddress);
            }
        }

        private async void slGripperMax_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            gripperMax = microsecondsToAngle(500, 2500, (ushort)slGripperMax.Value);
            if (commandServer != null)
            {
                await commandServer.SendMessage("value,cmd,17," + slGripperMax.Value.ToString(), Ipaddress);
            }
        }

        private async void btnRunPos_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,6", Ipaddress);
            }
        }

        private async void btnReadyPos_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,5", Ipaddress);
            }
        }

        private async void btnParkPos_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,3", Ipaddress);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (commandServer != null)
            {
                await commandServer.SendMessage("cmd,7", Ipaddress);
            }
        }
    }
}

                              
                               
                          