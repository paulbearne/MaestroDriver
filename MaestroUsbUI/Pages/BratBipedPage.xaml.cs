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
        private string fname;

        public BratBipedPage()
        {
            this.InitializeComponent();
            
        }



        void groupMove(int p1, int p2, int p3, int p4, int p5, int p6, int Speed)
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
                maestroDevice.Maestro.setTarget(0, (ushort)(maestroDevice.Maestro.angleToMicroseconds(0, RHP) + Offsets[0]));
                maestroDevice.Maestro.setTarget(1, (ushort)(maestroDevice.Maestro.angleToMicroseconds(1, RKP) + Offsets[1]));
                maestroDevice.Maestro.setTarget(2, (ushort)(maestroDevice.Maestro.angleToMicroseconds(2, RAP) + Offsets[2]));
                maestroDevice.Maestro.setTarget(3, (ushort)(maestroDevice.Maestro.angleToMicroseconds(3, LHP) + Offsets[3]));
                maestroDevice.Maestro.setTarget(4, (ushort)(maestroDevice.Maestro.angleToMicroseconds(4, LKP) + Offsets[4]));
                maestroDevice.Maestro.setTarget(5, (ushort)(maestroDevice.Maestro.angleToMicroseconds(5, LAP) + Offsets[5]));
                Task.Delay((int)period);
            }
        }


        void walkForward(byte angle)
        {
            if (stepN)
            {
                angle = (byte)(90 + angle);
                groupMove(last_angle, last_angle, 55, last_angle, last_angle, 75, Speed);
                groupMove(angle, angle, 75, angle, angle, 75, Speed);
                groupMove(angle, angle, 90, angle, angle, 90, Speed);
                stepN = !stepN;
            }
            else
            {
                angle = (byte)(90 - angle);
                groupMove(last_angle, last_angle, 105, last_angle, last_angle, 125, Speed);
                groupMove(angle, angle, 105, angle, angle, 105, Speed);
                groupMove(angle, angle, 90, angle, angle, 90, Speed);
                stepN = !stepN;
            }
            last_angle = angle;
        }

        void turnLeft()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 90, 75, 90, 90, 55, 450);
            groupMove(90, 90, 75, 90, 90, 75, 50);
            groupMove(55, 55, 75, 55, 55, 75, 500);
            groupMove(55, 55, 90, 55, 55, 90, 250);
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 90, 75, 90, 90, 55, 450);
            groupMove(90, 90, 75, 90, 90, 75, 50);
            groupMove(55, 55, 75, 55, 55, 75, 500);
            groupMove(55, 55, 90, 55, 55, 90, 250);
            groupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void turnRight()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 90, 55, 90, 90, 75, 450);
            groupMove(90, 90, 75, 90, 90, 75, 50);
            groupMove(55, 55, 75, 55, 55, 75, 500);
            groupMove(55, 55, 90, 55, 55, 90, 250);
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 90, 55, 90, 90, 75, 450);
            groupMove(90, 90, 75, 90, 90, 75, 50);
            groupMove(55, 55, 75, 55, 55, 75, 500);
            groupMove(55, 55, 90, 55, 55, 90, 250);
            groupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void getUpFromFront()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(60, 0, 90, 120, 170, 90, 500);
            groupMove(120, 0, 60, 120, 170, 90, 500);
            groupMove(170, 0, 90, 10, 180, 90, 500);
            groupMove(90, 90, 90, 90, 90, 90, 1000);
        }

        void getUpFromBack()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 180, 90, 90, 0, 90, 500);
            groupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void rollLeft()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(90, 90, 90, 40, 90, 90, 100);
            groupMove(90, 90, 90, 90, 90, 90, 500);
        }

        void rollRight()
        {
            groupMove(90, 90, 90, 90, 90, 90, 500);
            groupMove(140, 90, 90, 90, 90, 90, 100);
            groupMove(90, 90, 90, 90, 90, 90, 500);
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
                fname = ApplicationData.Current.LocalFolder.Path + "\\Brat.cfg";
                if (File.Exists(fname))
                {
                    // load offsets
                    loadOffsets();
                }



            }
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
                    maestroDevice.Maestro.setTarget(0, (UInt16)(slRH.Value * 4));

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
                    maestroDevice.Maestro.setTarget(1, (UInt16)(slRK.Value * 4));
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
                    maestroDevice.Maestro.setTarget(2, (UInt16)(slRA.Value * 4));

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
                    tbLH.Text = slLK.Value.ToString();
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
                    maestroDevice.Maestro.setTarget(3, (UInt16)(slLH.Value * 4));

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
                    maestroDevice.Maestro.setTarget(4, (UInt16)(slLK.Value * 4));

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
                    maestroDevice.Maestro.setTarget(5, (UInt16)(slLA.Value * 4));

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
                file.Dispose();

            } catch(Exception e)
            {
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
            file.Flush();
            file.Dispose();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            turnRight();
        }

        

        private void btnrollLeft_Click(object sender, RoutedEventArgs e)
        {
            rollLeft();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            turnLeft();
        }

        private void btnPower_Checked(object sender, RoutedEventArgs e)
        {
            if (btnPower.IsChecked == true)
            {
                // enable all servos
            }
            else
            {

            }
        }
    }
}
