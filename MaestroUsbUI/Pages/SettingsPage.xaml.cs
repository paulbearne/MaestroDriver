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
using MaestroUsbUI.Control;
using MaestroUsb;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MaestroUsbUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ChannelSettingsControl[] channelSettings;
        private UInt16 count = 0;
        private MaestroDeviceListItem maestro;
        public SettingsPage(MaestroDeviceListItem maestro)
        {
            this.InitializeComponent();
            this.maestro = maestro;
            count = maestro.Maestro.ServoCount;
        }

        private void DrawControls()
        {
            channelSettings = new ChannelSettingsControl[count];
        }

        
    }
}
