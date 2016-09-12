using MaestroUsb;
using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.IO;
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
        private Int16[] Offsets = { 231, 281, -24, -231, -281, 24 };
        private const uint period = 10;

        private double RHP = 90;
        private double RKP = 90;
        private double RAP = 90;
        private double LHP = 90;
        private double LKP = 90;
        private double LAP = 90;

        private bool stepN;
        private int last_angle = 90;
        private int Speed = 400;

        public BratBipedPage()
        {
            this.InitializeComponent();
        }

        void GroupMove(int p1, int p2, int p3, int p4, int p5, int p6, int Speed)
        {
            double ticks = Speed / period;
            double RHS = (p1 - RHP) / ticks;
            double RKS = (p2 - RKP) / ticks;
            double RAS = (p3 - RAP) / ticks;
            double LHS = (p4 - LHP) / ticks;
            double LKS = (p5 - LKP) / ticks;
            double LAS = (p6 - LAP) / ticks;
            for (int x = 0; x < ticks; x++)
            {
                RHP = RHP + RHS;
                RKP = RKP + RKS;
                RAP = RAP + RAS;
                LHP = LHP + LHS;
                LKP = LKP + LKS;
                LAP = LAP + LAS;
                maestroDevice.Maestro.setTarget(0,(ushort)(maestroDevice.Maestro.angleToMicroseconds(0,RHP) + Offsets[0]));
                maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, RKP) + Offsets[1]));
                maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, RAP) + Offsets[2]));
                maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, LHP) + Offsets[3]));
                maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, LKP) + Offsets[4]));
                maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, LAP) + Offsets[5]));
                Task.Delay((int)period);
            }
        }


        void Walk(byte angle)
        {
            if (stepN)
            {
                angle = (byte)(90 + angle);
                GroupMove(last_angle, last_angle, 55, last_angle, last_angle, 75, Speed);
                GroupMove(angle, angle, 75, angle, angle, 75, Speed);
                GroupMove(angle, angle, 90, angle, angle, 90, Speed);
                stepN = !stepN;
            }
            else
            {
                angle = (byte)(90 - angle);
                GroupMove(last_angle, last_angle, 105, last_angle, last_angle, 125, Speed);
                GroupMove(angle, angle, 105, angle, angle, 105, Speed);
                GroupMove(angle, angle, 90, angle, angle, 90, Speed);
                stepN = !stepN;
            }
            last_angle = angle;
        }

        void Turn_Left()
        {
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(90, 90, 75, 90, 90, 55, 450);
            GroupMove(90, 90, 75, 90, 90, 75, 50);
            GroupMove(55, 55, 75, 55, 55, 75, 500);
            GroupMove(55, 55, 90, 55, 55, 90, 250);
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(90, 90, 75, 90, 90, 55, 450);
            GroupMove(90, 90, 75, 90, 90, 75, 50);
            GroupMove(55, 55, 75, 55, 55, 75, 500);
            GroupMove(55, 55, 90, 55, 55, 90, 250);
            GroupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void Get_Up_From_Front()
        {
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(60, 0, 90, 120, 170, 90, 500);
            GroupMove(120, 0, 60, 120, 170, 90, 500);
            GroupMove(170, 0, 90, 10, 180, 90, 500);
            GroupMove(90, 90, 90, 90, 90, 90, 1000);
        }

        void Get_Up_From_Back()
        {
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(90, 180, 90, 90, 0, 90, 500);
            GroupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void Roll_Left()
        {
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(90, 90, 90, 40, 90, 90, 100);
            GroupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void Roll_Right()
        {
            GroupMove(90, 90, 90, 90, 90, 90, 500);
            GroupMove(140, 90, 90, 90, 90, 90, 100);
            GroupMove(90, 90, 90, 90, 90, 90, 500);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {

            if (eventArgs.Parameter as MaestroBoard != null)
            {
                maestroDevice = (eventArgs.Parameter as MaestroBoard).maestro;
                tbDeviceName.Text = maestroDevice.Name + " Connected";
                UInt16 count = maestroDevice.Maestro.ServoCount;
                // get all the settings stored on the board

                settings = await maestroDevice.Maestro.getUscSettings();
                await maestroDevice.Maestro.updateMaestroVariables();
                Task.WaitAll();  // wait until we have all the data
                servoStatus = maestroDevice.Maestro.servoStatus;
                maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, RHP) +( Offsets[0] * 4)));
                maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, RKP) +( Offsets[1] * 4)));
                maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, RAP) +( Offsets[2] * 4)));
                maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, LHP) +( Offsets[3] * 4)));
                maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, LKP) +( Offsets[4] * 4)));
                maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, LAP) +( Offsets[5] * 4)));

                chn1.positionChanged += Chn1_positionChanged;
                chn2.positionChanged += Chn2_positionChanged;
                chn3.positionChanged += Chn3_positionChanged;
                chn4.positionChanged += Chn4_positionChanged;
                chn5.positionChanged += Chn5_positionChanged;
                chn6.positionChanged += Chn6_positionChanged;
                
               
            }
        }




        private void Chn6_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(5, (UInt16)(newPosition * 4));
        }

        private void Chn5_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(4, (UInt16)(newPosition * 4));
        }

        private void Chn4_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(3, (UInt16)(newPosition * 4));
        }

        private void Chn3_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(2, (UInt16)(newPosition * 4));
        }

        private void Chn2_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(1, (UInt16)(newPosition * 4));
        }

        private void Chn1_positionChanged(byte Channel, ushort newPosition)
        {
            maestroDevice.Maestro.setTarget(0, (UInt16)(newPosition * 4));
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

        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            Walk(20);
        }
    }
}
