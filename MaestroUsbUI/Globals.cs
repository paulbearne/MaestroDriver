using MaestroUsb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaestroUsbUI
{
    /// <summary>
    /// Class hols any global variables
    /// </summary>
    public class Globals
    {
        // holds global copy of device manager so it doesn't get destroyed when we switch pages
        public static MaestroDeviceManager maestroManager;
        public static MaestroBoard maestroBoard;
        public static MaestroDeviceListItem maestroDevice;
        
    }
}
