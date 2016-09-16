using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MaestroUsbUI
{


    internal class TcpServer
    {
        private readonly int _port;
        public int Port { get { return _port; } }

        private StreamSocketListener listener;
        private DataWriter _writer;

        public delegate void DataRecived(string data);
        public event DataRecived OnDataRecived;

        public delegate void Error(string message);
        public event Error OnError;

        public TcpServer(int port)
        {
            _port = port;
        }

        public async void StartListener()
        {
            try
            {
                // dispose exisiting listener
                if (listener != null)
                {
                    await listener.CancelIOAsync();
                    listener.Dispose();
                    listener = null;
                }

                //create new listener
                listener = new StreamSocketListener();
                

                //set recieved call back 
                listener.ConnectionReceived += Listener_ConnectionReceived;
                //bind to port
                await listener.BindServiceNameAsync(Port.ToString());
            }
            catch (Exception e)
            {
                //call error callback
                if (OnError != null)
                    OnError(e.Message);
            }
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var reader = new DataReader(args.Socket.InputStream);
            _writer = new DataWriter(args.Socket.OutputStream);
            try
            {
                while (true)
                {
                    uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    //make sure we have string size
                    if (sizeFieldCount != sizeof(uint))
                        return;

                    //Tamanho da string
                    uint stringLength = reader.ReadUInt32();
                    //read the string
                    uint actualStringLength = await reader.LoadAsync(stringLength);
                    //Caso ocora um desconexão
                    if (stringLength != actualStringLength)
                        return;
                    //see if we have data recieved call back
                    if (OnDataRecived != null)
                    {
                        //read the string 
                        string data = reader.ReadString(actualStringLength);
                        //call callback
                        OnDataRecived(data);
                    }
                }

            }
            catch (Exception ex)
            {
                //error callback
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        public async void Send(string message)
        {
            if (_writer != null)
            {
                // write string size
                _writer.WriteUInt32(_writer.MeasureString(message));
                // write the string
                _writer.WriteString(message);

                try
                {
                    // send it
                    await _writer.StoreAsync();
                    // flush the buffers
                    await _writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        OnError(ex.Message);
                }
            }
        }
    }

    internal class TcpClient
    {
        private readonly string _ip;
        private bool _connected = false;
        private readonly int _port;
        private StreamSocket _socket;
        private DataWriter _writer;
        private DataReader _reader;

        public delegate void Error(string message);
        public event Error OnError;

        public delegate void DataReceived(string data);
        public event DataReceived OnDataReceived;

        public string Ip { get { return _ip; } }
        public int Port { get { return _port; } }
        public bool Connected { get { return _connected; } }

        public TcpClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async void Connect()
        {
            try
            {
                var hostName = new HostName(Ip);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(hostName, Port.ToString());
                _writer = new DataWriter(_socket.OutputStream);
                _connected = true;
                Read();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        public async void Send(string message)
        {
            //get string length
            _writer.WriteUInt32(_writer.MeasureString(message));
            //write string 
            _writer.WriteString(message);

            try
            {
                //send it
                await _writer.StoreAsync();
                //flush the buffers
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        
        private async void Read()
        {
            _reader = new DataReader(_socket.InputStream);
            try
            {
                while (true)
                {
                    uint sizeFieldCount = await _reader.LoadAsync(sizeof(uint));
                    // will be differant if disconnected
                    if (sizeFieldCount != sizeof(uint))
                        return;

                    uint stringLength = _reader.ReadUInt32();
                    uint actualStringLength = await _reader.LoadAsync(stringLength);
                    //if desconneted
                    if (stringLength != actualStringLength)
                        return;
                    if (OnDataReceived != null)
                        OnDataReceived(_reader.ReadString(actualStringLength));
                }

            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(ex.Message);
            }
        }

        public void Close()
        {
            _writer.DetachStream();
            _writer.Dispose();

            _reader.DetachStream();
            _reader.Dispose();
            _connected = false;

            _socket.Dispose();
        }
    }

    public class UdpServer
    {
        private readonly string _ip;
        private readonly int _port;
        private DatagramSocket listener;
        private DataWriter _writer;
        private DataReader _reader;

        public delegate void Error(string message);
        public event Error OnError;

        public delegate void DataReceived(string senderIp, string data);
        public event DataReceived OnDataReceived;

        public UdpServer(int port)
        {
            _port = port;
        }


        public async void StartListener()
        {
            try
            {
                // dispose exisiting listener
                if (listener != null)
                {
                    await listener.CancelIOAsync();
                    listener.Dispose();
                    listener = null;
                }

                //create new listener
                listener = new DatagramSocket();

                //set recieved call back 
                listener.MessageReceived += Listener_MessageReceived;
                //bind to port

                await listener.BindServiceNameAsync(_port.ToString());
            }
            catch (Exception e)
            {
                //call error callback
                if (OnError != null)
                    OnError(e.Message);
            }
        }

        private async void Listener_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            string message;
            string senderIp = args.RemoteAddress.DisplayName;
            var result = args.GetDataStream();
            var resultStream = result.AsStreamForRead(1024);

            using (var reader = new StreamReader(resultStream))
            {
                message = await reader.ReadToEndAsync();
            }

            if (OnDataReceived != null)
            {
                OnDataReceived(senderIp, message);
            }
        }



        public async Task SendMessage(string message)
        {

            using (var stream = await listener.GetOutputStreamAsync(new HostName("255.255.255.255"), _port.ToString()))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = Encoding.UTF8.GetBytes(message);

                    writer.WriteBytes(data);
                    await writer.StoreAsync();

                }
            }

        }

        public async Task SendMessage(string message, string ipAddress)
        {

            using (var stream = await listener.GetOutputStreamAsync(new HostName(ipAddress), _port.ToString()))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = Encoding.UTF8.GetBytes(message);

                    writer.WriteBytes(data);
                    await writer.StoreAsync();

                }
            }

        }


        public string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;
            var hostname =
                NetworkInformation.GetHostNames()
                    .SingleOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return hostname?.CanonicalName;
        }
    }



}
