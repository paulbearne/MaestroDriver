using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MaestroUsbUI
{
    public sealed partial class MaestroControl : UserControl
    {
        private UInt16 speed = 0;
        private UInt16 acceleration = 0;
        private UInt16 channel = 0;
        private UInt16 position = 1500;
        private UInt16 min = 500;
        private UInt16 max = 2500;

        public delegate void OnPositionChanged(byte Channel,UInt16 newPosition);
        public event OnPositionChanged positionChanged;

        public delegate void OnSpeedChanged(byte Channel, byte newSpeed);
        public event OnSpeedChanged speedChanged;

        public delegate void OnAccelerationChanged(byte Channel, byte newAcceleration);
        public event OnAccelerationChanged accelerationChanged;

        public MaestroControl()
        {
            this.InitializeComponent();
           
        }

        public UInt16 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                slPosition.Value = position;

            }
        }

        public UInt16 Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
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
                if (min < max)
                {
                    slPosition.Minimum = min;
                }
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
                if (max > min)
                {
                    slPosition.Minimum = max;
                }
            }
        }

        public UInt16 ChannelNumber
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
                Channel.Text = channel.ToString();
            }
        }

        private void speedUp_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (speed < 254)
            {
                speed++;
                speedValue.Text = speed.ToString();
            }
        }

        private void speedDown_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (speed > 0)
            {
                speed--;
                speedValue.Text = speed.ToString();
            }
        }

        private void accelerationUp_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if(acceleration < 254)
            {
                acceleration++;
                accelerationValue.Text = acceleration.ToString();
            }
        }

        private void accelrationDown_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (acceleration > 0)
            {
                acceleration--;
                accelerationValue.Text = acceleration.ToString();
            }
        }

        private void speedValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if( (Int16.Parse(speedValue.Text) > 254) || (Int16.Parse(speedValue.Text) < 0))
            {
                speedValue.Text = speed.ToString();
            }
            else
            {
                speed = UInt16.Parse(speedValue.Text);
                if (speedChanged != null)
                {
                    speedChanged(Convert.ToByte(channel), Convert.ToByte(speed) );
                }
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
                    accelerationChanged(Convert.ToByte(channel), Convert.ToByte(acceleration) );
                }
            }
        }

        private void slPosition_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            position = Convert.ToUInt16(slPosition.Value);
            if (positionChanged != null)
            {
                positionChanged(Convert.ToByte(channel),position);
            }
            
        }
    }
}
