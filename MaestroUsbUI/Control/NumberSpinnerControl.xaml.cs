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

namespace MaestroUsbUI
{
    public sealed partial class NumberSpinnerControl : UserControl
    {
        private int numbervalue = 0;
        private int min = 0;
        private int max = 999999999;

        public delegate void OnValueChanged( int newValue);
        public event OnValueChanged valueChanged;

        public int Value
        {
            get
            {
                return numbervalue;
            }
            set
            {
                numbervalue = value;
                spText.Text = numbervalue.ToString();
            }
        }

        public int Min
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
                if (numbervalue < min)
                {
                    numbervalue = min;
                    spText.Text = numbervalue.ToString();
                }
            }
        }


        public int Max
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
                if (numbervalue > max)
                {
                    numbervalue = max;
                    spText.Text = numbervalue.ToString();
                }
            }
        }

        public NumberSpinnerControl()
        {
            this.InitializeComponent();
        }

        private void btnUp_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (numbervalue < max)
            {
                numbervalue++;
            }
            spText.Text = numbervalue.ToString();
        
        }

        private void btnDown_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (numbervalue > min)
            {
                numbervalue--;
            }
            spText.Text = numbervalue.ToString();
        }

        private void spText_TextChanged(object sender, TextChangedEventArgs e)
        {
            numbervalue = int.Parse(spText.Text);
            if(valueChanged != null)
            {
                valueChanged(numbervalue);
            }
        }
    }
}
