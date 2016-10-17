using MaestroUsb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaestroUsbUI
{
    public class udpBufferItem
    {
        public string ipAddress;
        public string message;
        public udpBufferItem(string ip, string data)
        {
            ipAddress = ip;
            message = data;
        }
    }
    /// <summary>
    /// Class hols any global variables
    /// </summary>
    public class Globals
    {
        // holds global copy of device manager so it doesn't get destroyed when we switch pages
        public static MaestroDeviceManager maestroManager;
        public static MaestroBoard maestroBoard;
        public static MaestroDeviceListItem maestroDevice;
        public static List<udpBufferItem> commandBuffer;
        public static UdpServer udpserver;
        public static TcpServer tcpserver;
        public static string locatorMessage;

    }
}
