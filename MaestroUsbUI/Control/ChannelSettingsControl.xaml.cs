using Pololu.Usc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MaestroUsbUI.Control
{
    public sealed partial class ChannelSettingsControl : UserControl
    {
        private Int16 speed = 0;
        private UInt16 acceleration = 0;
        private String name = "";
        private byte channel = 0;
        private UInt16 target = 1500;
        private UInt16 min = 1000;
        private UInt16 max = 2500;
        private UInt16 range = 984;
        private UInt16 nuetral8b = 1500;
        private ChannelMode mode = ChannelMode.Servo;
        private HomeMode homemode = HomeMode.Off;

        public delegate void OnTargetChanged(byte Channel, UInt16 newTarget);
        public event OnTargetChanged targetChanged;

        public delegate void OnSpeedChanged(byte Channel, UInt16 newSpeed);
        public event OnSpeedChanged speedChanged;

        public delegate void OnAccelerationChanged(byte Channel, UInt16 newAcceleration);
        public event OnAccelerationChanged accelerationChanged;

        public delegate void OnMinChanged(byte Channel, UInt16 newMin);
        public event OnMinChanged minimumChanged;

        public delegate void OnMaxChanged(byte Channel, UInt16 newMax);
        public event OnMaxChanged maximumChanged;

        public delegate void OnRangeChanged(byte Channel, UInt16 newRange);
        public event OnRangeChanged rangeChanged;

        public delegate void OnNuetralChanged(byte Channel, UInt16 newNuetral);
        public event OnNuetralChanged nuetralChanged;

        public delegate void OnModeChanged(byte Channel, ChannelMode newMode);
        public event OnModeChanged modeChanged;

        public delegate void OnHomeModeChanged(byte Channel, HomeMode newHomeMode);
        public event OnHomeModeChanged homeModeChanged;

        public ChannelSettingsControl()
        {
            this.InitializeComponent();
        }


        public UInt16 Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
                spTarget.Text = target.ToString();

            }
        }

        public UInt16 Speed
        {
            get
            {
                return Convert.ToUInt16(speed);
            }
            set
            {
                speed = Convert.ToInt16(value);
                speedValue.Text = speed.ToString();
            }
        }

        public UInt16 Acceleration
        {
            get
            {
                return acceleration;
            }
            set
            {
                acceleration = value;
                accelerationValue.Text = acceleration.ToString();

            }
        }

        public UInt16 MinPosition
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
                spMin.Text = min.ToString();
            }
        }

        public UInt16 MaxPosition
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
                spMax.Text = max.ToString();
            }
        }

        public byte ChannelNumber
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
                tbChannel.Text = channel.ToString();
            }
        }

        private void spMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(spMin.Text) > Int16.Parse(spMax.Text)) || (Int16.Parse(spMin.Text) < 0))
            {
                spMin.Text = min.ToString();
            }
            else
            {
                min = UInt16.Parse(spMin.Text);
                if (minimumChanged != null)
                {
                    minimumChanged(Convert.ToByte(channel), min);
                }
            }
        }

        private void minUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (min < max)
            {
                min++;
                spMin.Text = min.ToString();
            }
        }

        private void minDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (min > 0)
            {
                min--;
                spMin.Text = min.ToString();
            }
        }

        private void spMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(spMax.Text) > 9999) || (Int16.Parse(spMax.Text) < 0))
            {
                spMax.Text = max.ToString();
            }
            else
            {
                max = UInt16.Parse(spMax.Text);
                if (maximumChanged != null)
                {
                   maximumChanged(Convert.ToByte(channel), max);
                }
            }
        }

        private void maxUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (max < 9999)
            {
                max++;
                spMax.Text = max.ToString();
            }
        }

        private void maxDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if ( max > min)
            {
                max--;
                spMax.Text = max.ToString();
            }
        }

        private void spTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(spTarget.Text) > 9999) || (Int16.Parse(spTarget.Text) < 0))
            {
                spTarget.Text = target.ToString();
            }
            else
            {
                target = UInt16.Parse(spTarget.Text);
                if (targetChanged != null)
                {
                    targetChanged(Convert.ToByte(channel), target);
                }
            }

        }

        private void targetUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (target < max)
            {
                target++;
                spTarget.Text = target.ToString();
            }
        }

        private void targetDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (target > min)
            {
                target--;
                spTarget.Text = target.ToString();
            }
        }

        private void speedValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(speedValue.Text) > 3968) || (Int16.Parse(speedValue.Text) < 0))
            {
                speedValue.Text = speed.ToString();
            }
            else
            {
                speed = Int16.Parse(speedValue.Text);
                if (speedChanged != null)
                {
                    speedChanged(Convert.ToByte(channel), Convert.ToUInt16(speed));
                }
            }
        }

        private void speedUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (speed < 3969)
            {
                speed++;
                speedValue.Text = speed.ToString();
            }
        }

        private void speedDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (speed > 0)
            {
                speed--;
                speedValue.Text = speed.ToString();
            }
        }

        private void accelerationValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(accelerationValue.Text) > 254) || (Int16.Parse(accelerationValue.Text) < 0))
            {
                accelerationValue.Text = acceleration.ToString();
            }
            else
            {
                acceleration = UInt16.Parse(accelerationValue.Text);
                if (accelerationChanged != null)
                {
                    accelerationChanged(Convert.ToByte(channel), Convert.ToByte(acceleration));
                }
            }
        }

        private void accelerationUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (acceleration < 254)
            {
                acceleration++;
                accelerationValue.Text = acceleration.ToString();
            }
        }

        private void accelrationDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (acceleration > 0)
            {
                acceleration--;
                accelerationValue.Text = acceleration.ToString();
            }
        }

        private void spNueatral8b_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(spNueatral8b.Text) > 9999) || (Int16.Parse(spNueatral8b.Text) < 0))
            {
                spNueatral8b.Text = nuetral8b.ToString();
            }
            else
            {
                nuetral8b = UInt16.Parse(spNueatral8b.Text);
                if (nuetralChanged != null)
                {
                    nuetralChanged(Convert.ToByte(channel), Convert.ToByte(nuetral8b));
                }
            }
        }

        private void nuetral8bUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (nuetral8b < 9999)
            {
                nuetral8b--;
                spNueatral8b.Text = nuetral8b.ToString();
            }
        }

        private void nuetral8bDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (nuetral8b > 0)
            {
                nuetral8b++;
                spNueatral8b.Text = nuetral8b.ToString();
            }
        }

      
        private void spRange8b_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((Int16.Parse(spRange8b.Text) > 9999) || (Int16.Parse(spRange8b.Text) < 0))
            {
                spRange8b.Text = range.ToString();
            }
            else
            {
                range = UInt16.Parse(spRange8b.Text);
                if (rangeChanged != null)
                {
                   rangeChanged(Convert.ToByte(channel), range);
                }
            }
        }

        private void range8bUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (range < 9999)
            {
                range++;
                spRange8b.Text = range.ToString();
            }
        }

        private void range8bDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (range > 0)
            {
                range--;
                spRange8b.Text = range.ToString();
            }
        }

        private void cbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbMode.SelectedIndex > -1)
            {
                mode = (ChannelMode)(cbHomeMode.SelectedIndex);
                
                if (modeChanged != null)
                {
                    modeChanged(channel, mode);
                }
            }
        }

        private void cbHomeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbHomeMode.SelectedIndex > -1)
            {
                homemode = (HomeMode)(cbHomeMode.SelectedIndex);
                if (homeModeChanged != null)
                {
                    homeModeChanged(channel,homemode);
                }
            }
        }
    }
}
