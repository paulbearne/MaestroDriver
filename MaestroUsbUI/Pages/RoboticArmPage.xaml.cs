using MaestroUsb;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        private UscSettings settings;
        private ServoStatus[] servoStatus;
        private string IpAddress;
        //  private Timer commandTimer;
        private int Range = 1;
        private UdpServer udpserver;


       
        private Int16[] Offsets = { 0, 0, 0, 0, 0, 0 };

        private Boolean CYL_IK = false;              // Apply only 2D, or cylindrical, kinematics. The X-axis component is
        // removed from the equations by fixing it at 0. The arm position is
        // calculated in the Y and Z planes, and simply rotates around the base.

        private Boolean WRIST_ROTATE = true;      // Uncomment if wrist rotate hardware is installed

        // Arm dimensions (mm). Standard AL5D arm, but with longer arm segments
        private double BASE_HGT = 80.9625;    // Base height to X/Y plane 3.1875"
        private static double HUMERUS = 263.525;     // Shoulder-to-elbow "bone" 10.375"
        private static double ULNA = 325.4375;       // Elbow-to-wrist "bone" 12.8125"
        private double GRIPPER = 73.025;     // Gripper length, to middle of grip surface 2.875" (3.375" - 0.5")

        private const Byte BaseServo = 0;
        private const Byte ShoulderServo = 1;
        private const Byte ElbowServo = 2;
        private const Byte WristServo = 3;
        private const Byte WristRotateServo = 4;
        private const Byte GripServo = 5;

        private UInt16 SERVO_MIN_US = 600;
        private UInt16 SERVO_MID_US = 1500;
        private UInt16 SERVO_MAX_US = 2400;
        private double SERVO_MIN_DEG = 0.0;
        private double SERVO_MID_DEG = 90.0;
        private double SERVO_MAX_DEG = 180.0;

        // Set physical limits (in degrees) per servo/joint.
        // Will vary for each servo/joint, depending on mechanical range of motion.
        // The MID setting is the required servo input needed to achieve a 
        // 90 degree joint angle, to allow compensation for horn misalignment
        private double BASE_MIN = 10.0;         // Fully CCW
        private double BASE_MID = 90.0;
        private double BASE_MAX = 170.0;       // Fully CW

        private double SHL_MIN = 20.0;        // Max forward motion
        private double SHL_MID = 81.0;
        private double SHL_MAX = 140.0;       // Max rearward motion

        private double ELB_MIN = 20.0;        // Max upward motion
        private double ELB_MID = 88.0;
        private double ELB_MAX = 165.0;       // Max downward motion

        private double WRI_MIN = 5.0;         // Max downward motion
        private double WRI_MID = 93.0;
        private double WRI_MAX = 175.0;       // Max upward motion

        private static double GRI_MIN = 25.0;        // Fully open
        private static double GRI_MID = 45.0;
        private static double GRI_MAX = 90.0;       // Fully closed

        private static double WRO_MIN = 5.0;
        private static double WRO_MID = 90.0;
        private static double WRO_MAX = 175.0;


        // Speed adjustment parameters
        // Percentages (1.0 = 100%) - applied to all arm movements
        private double SPEED_MIN = 0.5;
        private double SPEED_MAX = 1.5;
        private static double SPEED_DEFAULT = 1.0;
        private double SPEED_INCREMENT = 0.25;

        // Practical navigation limit.
        // Enforced on controller input, and used for CLV calculation 
        // for base rotation in 2D mode. 
        private double Y_MIN = 100.0;         // mm

        // replace joystick with buttons
        // PS2 controller characteristics
        private UInt16 JS_MIDPOINT = 128;     // Numeric value for joystick midpoint
                                              //#define JS_DEADBAND 4       // Ignore movement this close to the center position
        private double JS_IK_SCALE = 50.0;    // Divisor for scaling JS output for IK control
        private double JS_SCALE = 100.0;      // Divisor for scaling JS output for raw servo control
        private double Z_INCREMENT = 2.0;     // Change in Z axis (mm) per button press
        private double G_INCREMENT = 2.0;     // Change in Gripper jaw opening (servo angle) per button press


        // IK function return values
        enum IKstatus {
          IK_SUCCESS = 0,
          IK_ERROR = 1          // Desired position not possible
        }

        // Arm parking positions
        enum parktype {
            PARK_MIDPOINT = 1,     // Servos at midpoints
            PARK_READY = 2        // Arm at Ready-To-Run position
        }
        // Ready-To-Run arm position. See descriptions below
        // NOTE: Have the arm near this position before turning on the 
        //       servo power to prevent whiplash
        // Arm Position X Gripper Tip side to side
        //              Y Gripper Tip distance above base center
        //              Z Gripper Tip height from surface


        private static double READY_X = 0.0;
        private static double READY_Y = 170.0;
        private static double READY_Z = 45.0;
        private static double READY_GA = 0.0;
        private static double READY_G = GRI_MID;

        private static double READY_WR = WRO_MID;


        // Global variables for arm position, and initial settings
          // 3D kinematics
        private double X = READY_X;         // Left/right distance (mm) from base centerline - 0 is straight 
        private double Y = READY_Y;          // Distance (mm) out from base center
        private double Z = READY_Z;          // Height (mm) from surface (i.e. X/Y plane)
        private double GA = READY_GA;        // Gripper angle. Servo degrees, relative to X/Y plane - 0 is horizontal
        private double G = READY_G;          // Gripper jaw opening. Servo degrees - midpoint is halfway open

        private double WR = READY_WR;       // Wrist Rotate. Servo degrees - midpoint is horizontal

        private double Speed = SPEED_DEFAULT;

        // Pre-calculations
        private double hum_sq = HUMERUS * HUMERUS;
        private double uln_sq = ULNA * ULNA;




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


       
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                Globals.locatorMessage = "Robot Arm ";
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
                startCommandServer();
            }
        }

        private void startCommandServer()
        {
            udpserver = new UdpServer(9998);
            udpserver.StartListener();
            //  Globals.commandBuffer = new List<udpBufferItem>();
            udpserver.OnDataReceived += Commandserver_OnDataReceived;
            udpserver.OnError += Udpserver_OnError;
            Globals.udpserver.OnDataReceived += Udpserver_OnDataReceived;


        }

        // handle remote page change
        private void Udpserver_OnDataReceived(string senderIp, string data)
        {
            if (data.Contains("Brat"))
            {
                // switch to robot arm page
                this.Frame.Navigate(typeof(BratBipedPage), Globals.maestroBoard);
            }
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

        }

        private async Task<bool> doRemoteCommand(string cmd, string ip)
        {
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
            double grip_off_z = (Math.Sin(grip_angle_r)) * GRIPPER;
            double grip_off_y = (Math.Cos(grip_angle_r)) * GRIPPER;

            // Wrist position
            double wrist_z = (z - grip_off_z) - BASE_HGT;
            double wrist_y = y - grip_off_y;

            // Shoulder to wrist distance (AKA sw)
            double s_w = (wrist_z * wrist_z) + (wrist_y * wrist_y);
            double s_w_sqrt = Math.Sqrt(s_w);

            // s_w angle to ground
            double a1 = Math.Atan2(wrist_z, wrist_y);

            // s_w angle to humerus
            double a2 = Math.Acos(((hum_sq - uln_sq) + s_w) / (2 * HUMERUS * s_w_sqrt));

            // Shoulder angle
            double shl_angle_r = a1 + a2;
            // If result is NAN or Infinity, the desired arm position is not possible
            if (Double.IsNaN(shl_angle_r) || Double.IsInfinity(shl_angle_r))
                return IKstatus.IK_ERROR;
            double shl_angle_d = Degrees(shl_angle_r);

            // Elbow angle
            double elb_angle_r = Math.Acos((hum_sq + uln_sq - s_w) / (2 * HUMERUS * ULNA));
            // If result is NAN or Infinity, the desired arm position is not possible
            if (Double.IsNaN(elb_angle_r) || Double.IsInfinity(elb_angle_r))
                return IKstatus.IK_ERROR;
            double elb_angle_d = Degrees(elb_angle_r);
            double elb_angle_dn = -(180.0 - elb_angle_d);

            // Wrist angle
            double wri_angle_d = (grip_angle_d - elb_angle_dn) - shl_angle_d;

            // Calculate servo angles
            // Calc relative to servo midpoint to allow compensation for servo alignment
            double bas_pos = BASE_MID + Degrees(bas_angle_r);
            double shl_pos = SHL_MID + (shl_angle_d - 90.0);
            double elb_pos = ELB_MID - (elb_angle_d - 90.0);
            double wri_pos = WRI_MID + wri_angle_d;

            // If any servo ranges are exceeded, return an error
            if (bas_pos < BASE_MIN || bas_pos > BASE_MAX || shl_pos < SHL_MIN || shl_pos > SHL_MAX || elb_pos < ELB_MIN || elb_pos > ELB_MAX || wri_pos < WRI_MIN || wri_pos > WRI_MAX)
                return IKstatus.IK_ERROR;

            // Position the servos
            // 3D kinematics
            // write the servos
            maestroDevice.Maestro.setTarget(BaseServo, maestroDevice.Maestro.angleToMicroseconds(BaseServo, bas_pos));
           // Bas_Servo.writeMicroseconds(deg_to_us(bas_pos));
            maestroDevice.Maestro.setTarget(ShoulderServo, maestroDevice.Maestro.angleToMicroseconds(ShoulderServo, shl_pos));
            //Shl_Servo.writeMicroseconds(deg_to_us(shl_pos));
            maestroDevice.Maestro.setTarget(ElbowServo, maestroDevice.Maestro.angleToMicroseconds(ElbowServo, elb_pos));
            //Elb_Servo.writeMicroseconds(deg_to_us(elb_pos));
            //Wri_Servo.writeMicroseconds(deg_to_us(wri_pos));
            maestroDevice.Maestro.setTarget(WristServo, maestroDevice.Maestro.angleToMicroseconds(WristServo, wri_pos));

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
                case parktype.PARK_MIDPOINT:
                    maestroDevice.Maestro.setTarget(BaseServo, maestroDevice.Maestro.angleToMicroseconds(BaseServo, BASE_MID));
                   // Bas_Servo.writeMicroseconds(deg_to_us(BAS_MID));
                   // Shl_Servo.writeMicroseconds(deg_to_us(SHL_MID));
                    maestroDevice.Maestro.setTarget(ShoulderServo, maestroDevice.Maestro.angleToMicroseconds(ShoulderServo, SHL_MID));
                   // Elb_Servo.writeMicroseconds(deg_to_us(ELB_MID));
                    maestroDevice.Maestro.setTarget(ElbowServo, maestroDevice.Maestro.angleToMicroseconds(ElbowServo, ELB_MID));
                   // Wri_Servo.writeMicroseconds(deg_to_us(WRI_MID));
                    maestroDevice.Maestro.setTarget(WristServo, maestroDevice.Maestro.angleToMicroseconds(WristServo, WRI_MID));
                   // Gri_Servo.writeMicroseconds(deg_to_us(GRI_MID));
                    maestroDevice.Maestro.setTarget(GripServo, maestroDevice.Maestro.angleToMicroseconds(GripServo, GRI_MID));
#if WRIST_ROTATE
                    Wro_Servo.writeMicroseconds(deg_to_us(WRO_MID));
#endif
                    break;

                // Ready-To-Run position
                case parktype.PARK_READY:
#if CYL_IK   // 2D kinematics
                    set_arm(0.0, READY_Y, READY_Z, READY_GA);
                   // Bas_Servo.writeMicroseconds(deg_to_us(READY_BA));
                    maestroDevice.Maestro.setTarget(BaseServo, maestroDevice.Maestro.angleToMicroseconds(BaseServo, READY_BA));
#else           // 3D kinematics
                    set_arm(READY_X, READY_Y, READY_Z, READY_GA);
#endif
                    //Gri_Servo.writeMicroseconds(deg_to_us(READY_G));
                    maestroDevice.Maestro.setTarget(GripServo, maestroDevice.Maestro.angleToMicroseconds(GripServo, GRI_MID));
                   // Wro_Servo.writeMicroseconds(deg_to_us(READY_WR));
                    maestroDevice.Maestro.setTarget(WristRotateServo, maestroDevice.Maestro.angleToMicroseconds(WristRotateServo, READY_WR));

                    break;
            }

            return;
        }

    }
}
