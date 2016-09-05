using System;
using Windows.Storage.Streams;
using Windows.Devices.Usb;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using System.Collections.ObjectModel;
using Pololu.Usc;
using Pololu.Usc.Bytecode;
using System.Collections.Generic;
using Windows.Storage;
using System.IO;
using System.Xml;

namespace MaestroUsb
{


    /// <summary>
    /// Maestro usb list item holds device information 
    /// </summary>
     public class MaestroDeviceListItem
     {
        private String name;
        private bool isConnected = false;
        private DeviceInformation devinformation;
        private UsbDevice usbDevice;
        private String id;
        private UInt16 ProductId;
        private MaestroDevice maestro;



        /// <summary>
        /// Windows device information
        /// </summary>
        public DeviceInformation deviceInformation
        {
            get
            {
                return devinformation;
            }
        }

        /// <summary>
        /// true if the usb is connected to the maestro board
        /// </summary>
        public bool Connected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
            }
        }

        /// <summary>
        /// Board type name
        /// </summary>
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

       

        /// <summary>
        /// Gets the device Id
        /// </summary>
        public String Id
        {
            get
            {
                return id;
            }
        }

        

        /// <summary>
        /// Gets the device pointer.
        /// </summary>
        public UsbDevice device
        {
            get
            {
                return usbDevice;
            }
            set
            {
                usbDevice = value;
            }
        }

        

        /// <summary>
        /// Gets the USB product ID of the device.
        /// </summary>
        public UInt16 productId
        {
            get
            {
                return ProductId;
            }
        }

        /// <summary>
        /// Maestro Device holder allows access to maestro usb functions
        /// and settings
        /// </summary>
        public MaestroDevice Maestro
        {
            get
            {
                return maestro;
            }
        }

        /// <summary>
        /// true if the devices are the same
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool isSameDeviceAs(MaestroDeviceListItem item)
        {
            return (device == item.device);
        }

        /// <summary>
        /// Maestro List Item Constructor 
        /// </summary>
        /// <param name="devicePointer">Windows UsbDevice/param>
        /// <param name="devInfo">Windows DeviceInformation</param>
        /// <param name="text">Human readable device Name</param>
        /// <param name="Id">Windows Device Id</param>
        /// <param name="productId">usb device pid</param>
        public MaestroDeviceListItem(UsbDevice devicePointer, DeviceInformation devInfo, string text, string Id, UInt16 productId)
        {
            usbDevice = devicePointer;
            name = text;
            devinformation = devInfo;
            id = Id;
            ProductId = productId;
            maestro = new MaestroDevice(this);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MaestroDeviceListItem()
        {
            if (device != null)
            {
                device.Dispose();

            }
        }


    }


    /// <summary>
    /// Maestro Device Manager holds a collection of meastro items
    /// </summary>
    public class MaestroDeviceManager
    {
        private Collection<MaestroDeviceListItem> maestroDeviceList;
        private string deviceSelector;
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");
        private bool deviceListReady = false;

        public delegate void OnMaestroRemoved(DeviceInformationUpdate deviceInfo);
        public event OnMaestroRemoved deviceRemovedCallback;

        public delegate void OnMaestroAdded(MaestroDeviceListItem device);
        public event OnMaestroAdded deviceAddedCallback;

        public delegate void OnMaestroStopped(MaestroDeviceListItem device);
        public event OnMaestroStopped deviceStoppedCallback;

        public delegate void OnMaestroConnected(MaestroDeviceListItem device);
        public event OnMaestroConnected deviceConnectedCallback;

        public delegate void OnMaestroError(string msg);
        public event OnMaestroError deviceErrorCallback;

        public delegate void OnMaestroDeviceListReady(Collection<MaestroDeviceListItem> devices);
        public event OnMaestroDeviceListReady deviceListReadyCallback;


        /// <summary>
        /// Constructor
        /// </summary>
        public MaestroDeviceManager()
        {
            // create new device list
            maestroDeviceList = new Collection<MaestroDeviceListItem>(); 
          //  OpenDeviceWatcher();
        }

        /// <summary>
        /// DeviceList Property
        /// </summary>
        public Collection<MaestroDeviceListItem> DeviceList
        {
            get
            {
                return maestroDeviceList;
            }
        }


        /// <summary>
        /// Builds a List of Maestro Boards connected
        /// </summary>
        public async void BuildDeviceList()
        {
              string deviceSelector = UsbDevice.GetDeviceSelector(DeviceInterfaceClass);

              var maestroDevices = await DeviceInformation.FindAllAsync(deviceSelector);
              try
              {

                for (int i = 0; i < maestroDevices.Count; i++)
                {
                    // construct out list item
                    // check we dont alraedy have this device in the list
                    // MaestroDeviceListItem maestroListItem = maestroDeviceList.Contains(item => item.deviceInformation.Id == maestroDevices[i].Id);
                    UsbDevice usbDevice = await UsbDevice.FromIdAsync(maestroDevices[i].Id);


                    MaestroDeviceListItem maestroListItem = new MaestroDeviceListItem(usbDevice, maestroDevices[i], maestroDevices[i].Name, maestroDevices[i].Id, Convert.ToUInt16(usbDevice.DeviceDescriptor.ProductId));
                    maestroListItem.Connected = true;
                    if (maestroDeviceList.Contains(maestroListItem) == false)
                    {
                        maestroDeviceList.Add(maestroListItem);
                    }
                    

                }
                deviceListReady = true;
                // everything ok we now have list so add the watchers
                OpenDeviceWatchers();
              }
              catch (Exception e)
              {
                Debug.WriteLine(e);
                deviceListReady = false;

              }
              if (deviceListReadyCallback != null)
              {
                deviceListReadyCallback(maestroDeviceList);
              }

        }

        /// <summary>
        /// Connects to a Maestro Board 
        /// </summary>
        /// <param name="deviceItem">MaestroDeviceListItem for Board to Connnect to</param>
        /// <returns></returns>
        public async Task<Boolean> OpenDeviceAsync(MaestroDeviceListItem deviceItem)
        {
            Boolean successfullyOpenedDevice = false;
            try
            {
                if (deviceItem.device == null)
                {
                    deviceItem.device = await UsbDevice.FromIdAsync(deviceItem.Id);
                }

                // Device could have been blocked by user or the device has already been opened by another app.
                if (deviceItem.device != null)
                {

                    // Notify registered callback handle that the device has been opened
                    successfullyOpenedDevice = true;
                    deviceItem.Connected = true;
                    if (deviceConnectedCallback != null)
                    {
                        deviceConnectedCallback(deviceItem);
                    }


                }
                else
                {
                    if (deviceErrorCallback != null)
                    {
                        deviceErrorCallback("Error opening Device");
                    }
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

            }
            return successfullyOpenedDevice;
        }


        /// <summary>
        /// Close the connection to a connected Maestro Board
        /// </summary>
        /// <param name="deviceItem">MaestroDeviceListItem for board to disconnect</param>
        public void CloseDevice(MaestroDeviceListItem deviceItem)
        {
            bool match = maestroDeviceList.Contains(deviceItem);
            // notify the user 

            if (match == true)
            {
                if (deviceItem.device != null)
                {
                    deviceItem.device.Dispose();
                }
                deviceItem.Connected = false;
            }
        }


        /// <summary>
        /// Open device event handlers
        /// </summary>
        public void OpenDeviceWatchers()
        {
            deviceSelector = UsbDevice.GetDeviceSelector(DeviceInterfaceClass);

            var maestroWatcher = DeviceInformation.CreateWatcher(deviceSelector);

            maestroWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>
                                        (this.OnDeviceAdded);

            maestroWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>
                                    (this.OnDeviceRemoved);
            maestroWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>
                                    (this.OnDeviceEnumerationComplete);
            maestroWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(this.OnDeviceStopped);
            maestroWatcher.Start();
        }


        /// <summary>
        /// Device removed callback called when a device is disconnected from a Usb port
        /// </summary>
        /// <param name="sender">DeviceWatcher</param>
        /// <param name="args">DeviceInformationUpdate</param>
        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {

            // remove it from list 
            if (deviceListReady)
            {
                MaestroDeviceListItem match = null;
                foreach (MaestroDeviceListItem devlistItem in maestroDeviceList)
                {
                    if (devlistItem.Id == args.Id)
                    {
                        match = devlistItem;
                        break;  // found match so get out of loop
                    }
                }
                // notify the user 

                if (match != null)
                {
                    if (match.Connected)
                    {
                        match.device.Dispose();
                    }
                    int x = maestroDeviceList.IndexOf(match);
                    if (x > -1)
                    {
                        maestroDeviceList.RemoveAt(x);
                        // if we have a user callback set call it
                        if (deviceRemovedCallback != null)
                        {
                            deviceRemovedCallback(args);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Device Added callback called when a device is connected to a Usb port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            bool matched = false;
            if (deviceListReady)
            {

                if (args != null)
                {

                    //new device so open it and add it to list  
                    // construct out list item
                    try
                    {
                        foreach (MaestroDeviceListItem item in maestroDeviceList)
                        {
                            if (item.Id == args.Id)
                            {
                                matched = true;
                                break;
                            }
                        }
                        if (matched == false)
                        {
                            UsbDevice usbDevice = await UsbDevice.FromIdAsync(args.Id);
                            if (usbDevice != null)
                            {
                                MaestroDeviceListItem maestroListItem = new MaestroDeviceListItem(usbDevice, args, args.Name, args.Id, Convert.ToUInt16(usbDevice.DeviceDescriptor.ProductId));
                                maestroListItem.Connected = true;
                                maestroDeviceList.Add(maestroListItem);
                                // User call back
                                if (deviceAddedCallback != null)
                                {
                                    deviceAddedCallback(maestroListItem);
                                }

                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }

                }
            }
             
        }

        /// <summary>
        /// Device stopped callback called when a device usb device stops working
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDeviceStopped(DeviceWatcher sender, Object args)
        {
            if (deviceListReady)
            {
                // we'll notify user
                if (args is UsbDevice)
                {
                    MaestroDeviceListItem match = null;
                    foreach (MaestroDeviceListItem devlistItem in maestroDeviceList)
                    {
                        if (devlistItem.device == args)
                        {
                            match = devlistItem;
                            break;  // found match so get out of loop
                        }
                    }
                    if (match != null)
                    {
                        match.Connected = false;
                    }
                    // user call back
                    if (deviceStoppedCallback != null)
                    {
                        deviceStoppedCallback(match);
                    }
                }
            }
        }

        /// <summary>
        /// Device Enumeration callback called when a device is fully enumerated from a Usb port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDeviceEnumerationComplete(DeviceWatcher sender, Object args)
        {
            if (deviceListReady)
            {
                if (args is UsbDevice)
                {
                    UsbDevice usbDevice = args as UsbDevice;

                }
            }
        }

    }


    /// <summary>
    /// Main Maestro class contains most of the Pololu sdk functions modified to work with win uwp 
    /// UsbDevice class
    /// </summary>
    public class MaestroDevice
    {
        private byte servoParameterBytes = 9;
        private MaestroDeviceListItem maestroDevice;
        private const int INSTRUCTION_FREQUENCY = 12000000;
        private MaestroVariables mVariables;
        private MicroMaestroVariables mMicroVariables;
        private ServoStatus[] mServoStatus;
       // private ChannelSetting[] servosetting;
        private UscSettings settings;
        private short[] stack;
        private ushort[] callstack;
        public const int MicroMaestroStackSize = 32;
        public const int MicroMaestroCallStackSize = 10;
        public const int MiniMaestroStackSize = 126;
        public const int MiniMaestroCallStackSize = 126;
        uint microVariablePacketSize;
        uint miniVariablePacketSize;
        private byte servoCount;
        public static UInt16[] DevicePids = { (UInt16)0x0089, (UInt16)0x008A, (UInt16)0x008B, (UInt16)0x008C };
        public Byte privateFirmwareVersionMajor = 0xFF;
        public Byte privateFirmwareVersionMinor = 0xFF;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private ApplicationDataCompositeValue regSettings;
        private string keyname;

        /// <summary>
        /// Constructor does some initialization 
        /// </summary>
        /// <param name="maestro"></param>
        public MaestroDevice(MaestroDeviceListItem maestro)
        {
            maestroDevice = maestro;
            switch (maestro.productId)
            {
                case 0x89: servoCount = 6; break;
                case 0x8A: servoCount = 12; break;
                case 0x8B: servoCount = 18; break;
                case 0x8C: servoCount = 24; break;
                default: throw new Exception("Unknown product id " + maestro.productId.ToString("x2") + ".");
            }
            mServoStatus = new ServoStatus[servoCount];
            mVariables = new MaestroVariables();
            mMicroVariables = new MicroMaestroVariables();
            updateVariablePacketSize();
        }

        /// <summary>
        /// setsup usb packets sizes for getting setting varaiables 
        /// this function is need as most of windows UsbDevice Calls are Async and cant use unsafe with Asysnc
        /// </summary>
        public unsafe void updateVariablePacketSize()
        {
            microVariablePacketSize =(uint)(sizeof(MicroMaestroVariables) + servoCount * sizeof(ServoStatus));
            miniVariablePacketSize = (uint)(sizeof(MiniMaestroVariables));
        }


        /// <summary>
        /// Mini Maestro Variables Property
        /// </summary>
        public MaestroVariables MiniVariables
        {
            get
            {
                return mVariables;
            }
            set
            {
                mVariables = value;
            }
        }

        /// <summary>
        /// micro Maestro Variables property
        /// </summary>
        public MicroMaestroVariables MicroVariables
        {
            get
            {
                return mMicroVariables;
            }
            set
            {
                mMicroVariables = value;
            }
        }

        /// <summary>
        /// Maximum script length property
        /// </summary>
        public ushort maxScriptLength
        {
            get
            {
                if (microMaestro)
                {
                    return 1024;
                }
                else
                {
                    return 8192;
                }
            }
        }

        /// <summary>
        /// servo status array readonly
        /// </summary>
        public ServoStatus[] servoStatus
        {
            get
            {
                return mServoStatus;
            }
        }

        /// <summary>
        /// call stack property
        /// </summary>
        public ushort[] CallStack
        {
            get
            {
                return callstack;
            }
            set
            {
                callstack = value;
            }
        }


        /// <summary>
        /// stack property
        /// </summary>
        public short[] Stack
        {
            get
            {
                return stack;
            }
            set
            {
                stack = value;
            }
        }


        /// <summary>
        /// servo count property
        /// </summary>
        public byte ServoCount
        {
            get
            {
                return servoCount;
            }
        }

        /// <summary>
        ///  test if micro or mini true if micro
        /// </summary>
        public bool microMaestro
        {
            get
            {
                return servoCount == 6;
            }
        }

        /// <summary>
        /// Exponential Speed to Noarmal Speed
        /// </summary>
        /// <param name="exponentialSpeed"></param>
        /// <returns></returns>
        private static ushort exponentialSpeedToNormalSpeed(byte exponentialSpeed)
        {
            // Maximum value of normalSpeed is 31*(1<<7)=3968

            int mantissa = exponentialSpeed >> 3;
            int exponent = exponentialSpeed & 7;

            return (ushort)(mantissa * (1 << exponent));
        }

        /// <summary>
        /// Normal Speed in us to Exponential Speed
        /// </summary>
        /// <param name="normalSpeed"></param>
        /// <returns></returns>
        private static byte normalSpeedToExponentialSpeed(ushort normalSpeed)
        {
            ushort mantissa = normalSpeed;
            byte exponent = 0;

            while (true)
            {
                if (mantissa < 32)
                {
                    // We have reached the correct representation.
                    return (byte)(exponent + (mantissa << 3));
                }

                if (exponent == 7)
                {
                    // The number is too big to express in this format.
                    return 0xFF;
                }

                // Try representing the number with a bigger exponent.
                exponent += 1;
                mantissa >>= 1;
            }
        }

        /// <summary>
        /// position to us maestro has 0.25us steps
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static decimal positionToMicroseconds(ushort position)
        {
            return (decimal)position / 4M;
        }

        /// <summary>
        /// converts 0.25 us position to us
        /// </summary>
        /// <param name="us"></param>
        /// <returns></returns>
        public static ushort microsecondsToPosition(decimal us)
        {
            return (ushort)(us * 4M);
        }

        /// <summary>
        /// The approximate number of microseconds represented by the servo
        /// period when PARAMETER_SERVO_PERIOD is set to this value.
        /// </summary>
        public static decimal periodToMicroseconds(ushort period, byte servos_available)
        {
            return (decimal)period * 256M * servos_available / 12M;
        }

        /// <summary>
        /// The closest value of PARAMETER_SERVO_PERIOD for a given number of us per period.
        /// </summary>
        /// <returns>Amount of time allocated to each servo, in units of 256/12.</returns>
        public static byte microsecondsToPeriod(decimal us, byte servos_avaiable)
        {
            return (byte)Math.Round(us / 256M * 12M / servos_avaiable);
        }

        /// <summary>
        /// See Sec 16.3 of the PIC18F14K50 datasheet for information about SPBRG.
        /// On the umc01a, we have SYNC=0, BRG16=1, and BRGH=1, so the pure math
        /// formula for the baud rate is Baud = INSTRUCTION_FREQUENCY / (spbrg+1);
        /// </summary>
        private static UInt32 convertSpbrgToBps(UInt16 spbrg)
        {
            if (spbrg == 0)
            {
                return 0;
            }

            return (UInt32)((INSTRUCTION_FREQUENCY + (spbrg + 1) / 2) / (spbrg + 1));
        }

        /// <summary>
        /// The converts from bps to SPBRG, so it is the opposite of convertSpbrgToBps.
        /// The purse math formula is spbrg = INSTRUCTION_FREQUENCY/Baud - 1.
        /// </summary>
        private static UInt16 convertBpsToSpbrg(UInt32 bps)
        {
            if (bps == 0)
            {
                return 0;
            }

            return (UInt16)((INSTRUCTION_FREQUENCY - bps / 2) / bps);
        }

        /// <summary>
        /// Converts channel number (0-5) to port mask bit number
        /// on the Micro Maestro.  Not useful on other Maestros.
        /// </summary>
        private byte channelToPort(byte channel)
        {
            if (channel <= 3)
            {
                return channel;
            }
            else if (channel < 6)
            {
                return (byte)(channel + 2);
            }
            throw new ArgumentException("Invalid channel number " + channel);
        }

        /// <summary>
        /// Returns the parameter number for the parameter of a given servo,
        /// given the corresponding parameter number for servo 0.
        /// </summary>
        /// <param name="p">e.g. PARAMETER_SERVO0_HOME</param>
        /// <param name="servo">Channel number.</param>
        uscParameter specifyServo(uscParameter p, byte servo)
        {
            return (uscParameter)((byte)(p) + servo * servoParameterBytes);
        }


        /// <summary>
        /// Control Transfer function to handle writing arrays used in script
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="request"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected async Task<UInt16> controlTransfer(byte requestType, byte request, ushort value, ushort index, byte[] data)
        {
            if (requestType == 0x40)
            {
                DataWriter writer = new DataWriter();

                // Convert the to buffer
                writer.WriteBytes(data);

                // The buffer with the data
                var bufferToSend = writer.DetachBuffer();

                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.Out,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor
                    },
                    Request = request,
                    Index = index,
                    Length = bufferToSend.Length
                };

                UInt32 bytesTransferred = await maestroDevice.device.SendControlOutTransferAsync(setupPacket, bufferToSend);

            }
            if (requestType == 0xC0)
            {
                
            }
            return 0;// dummy get rid of when coded
        }

        /// <summary>
        /// Control transfer used to take a ushort value and index 
        /// used by setTarget , setSpeed , setAcceleration
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="request"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        protected async void controlTransfer(byte requestType, byte request, ushort value, ushort index)
        {

            if (requestType == 0xC0)
            {
                // write
                
               

            }
            if (requestType == 0x40)
            {
                try
                {
                    // Range range = getRange(parameter);

                    UsbSetupPacket setupPacket = new UsbSetupPacket
                    {
                        RequestType = new UsbControlRequestType
                        {
                            Direction = UsbTransferDirection.Out,
                            Recipient = UsbControlRecipient.Device,
                            ControlTransferType = UsbControlTransferType.Vendor,
                        },
                        Request = request,
                        Value = value,
                        Index = index,
                        Length = 0
                    };

                    
                    await maestroDevice.device.SendControlOutTransferAsync(setupPacket);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("oops" + e.Message);
                }
            }
            
        }

        
        /// <summary>
        /// convert a byte to serialmode
        /// </summary>
        /// <param name="modebyte"></param>
        /// <returns></returns>
        private uscSerialMode bytetoSerialMode(byte modebyte)
        {
            return (uscSerialMode)(modebyte);
        }

        /// <summary>
        /// get all the uscSettings from the Maestro Board
        /// </summary>
        /// <returns></returns>
        public async Task<UscSettings> getUscSettings()
        {
            settings = new UscSettings();
            string fname = ApplicationData.Current.LocalFolder.Path + "\\" + maestroDevice.Id;

            settings.serialMode = (uscSerialMode)( await getRawParameter(uscParameter.PARAMETER_SERIAL_MODE));
            settings.fixedBaudRate = convertSpbrgToBps(await getRawParameter(uscParameter.PARAMETER_SERIAL_FIXED_BAUD_RATE));
            settings.enableCrc = await getRawParameter(uscParameter.PARAMETER_SERIAL_ENABLE_CRC) != 0;
            settings.neverSuspend = await getRawParameter(uscParameter.PARAMETER_SERIAL_NEVER_SUSPEND) != 0;
            settings.serialDeviceNumber = (byte)await getRawParameter(uscParameter.PARAMETER_SERIAL_DEVICE_NUMBER);
            settings.miniSscOffset = (byte)await getRawParameter(uscParameter.PARAMETER_SERIAL_MINI_SSC_OFFSET);
            settings.serialTimeout = await getRawParameter(uscParameter.PARAMETER_SERIAL_TIMEOUT);
            settings.scriptDone = await getRawParameter(uscParameter.PARAMETER_SCRIPT_DONE) != 0;

            if (servoCount == 6)
            {
                settings.servosAvailable = (byte)await getRawParameter(uscParameter.PARAMETER_SERVOS_AVAILABLE);
                settings.servoPeriod = (byte)await getRawParameter(uscParameter.PARAMETER_SERVO_PERIOD);
            }
            else
            {
               
                UInt32 tmp = (UInt32)(await getRawParameter(uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU) << 8);
                tmp |= (byte)await getRawParameter(uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L);
                settings.miniMaestroServoPeriod = tmp;

                settings.servoMultiplier = (ushort)(await getRawParameter(uscParameter.PARAMETER_SERVO_MULTIPLIER) + 1);
            }

            if (servoCount > 18)
            {
                settings.enablePullups = await getRawParameter(uscParameter.PARAMETER_ENABLE_PULLUPS) != 0;
            }

            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new Byte[6];

            if (microMaestro)
            {
                ioMask = (byte)await getRawParameter(uscParameter.PARAMETER_IO_MASK_C);
                outputMask = (byte)await getRawParameter(uscParameter.PARAMETER_OUTPUT_MASK_C);
            }
            else
            {
                for (byte i = 0; i < 6; i++)
                {
                    channelModeBytes[i] = (byte)await getRawParameter(uscParameter.PARAMETER_CHANNEL_MODES_0_3 + i);
                }
            }

            for (byte i = 0; i < servoCount; i++)
            {
                // Initialize the ChannelSettings objects and 
                // set all parameters except name and mode.
                ChannelSetting setting = new ChannelSetting();

                if (microMaestro)
                {
                    byte bitmask = (byte)(1 << channelToPort(i));
                    if ((ioMask & bitmask) == 0)
                    {
                        setting.mode = ChannelMode.Servo;
                    }
                    else if ((outputMask & bitmask) == 0)
                    {
                        setting.mode = ChannelMode.Input;
                    }
                    else
                    {
                        setting.mode = ChannelMode.Output;
                    }
                }
                else
                {
                    setting.mode = (ChannelMode)((channelModeBytes[i >> 2] >> ((i & 3) << 1)) & 3);
                }

                ushort home = await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_HOME, i));
                if (home == 0)
                {
                    setting.homeMode = HomeMode.Off;
                    setting.home = 0;
                }
                else if (home == 1)
                {
                    setting.homeMode = HomeMode.Ignore;
                    setting.home = 0;
                }
                else
                {
                    setting.homeMode = HomeMode.Goto;
                    setting.home = home;
                }

                setting.minimum = (ushort)(64 * await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MIN, i)));
                setting.maximum = (ushort)(64 * await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MAX, i)));
                setting.neutral = await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_NEUTRAL, i));
                setting.range = (ushort)(127 * await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_RANGE, i)));
                setting.speed = exponentialSpeedToNormalSpeed((byte)await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_SPEED, i)));
                setting.acceleration = (byte)await getRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_ACCELERATION, i));

                settings.channelSettings.Add(setting);
            }
            
            if (File.Exists(fname))
            {
                // Get names for servos from the registry.
                XmlDocument settingsFile = new XmlDocument();
                settingsFile.Load(File.OpenRead(fname));
                for (byte i = 0; i < servoCount; i++)
                {
                    settings.channelSettings[i].name = "";
                    if ( settingsFile != null)
                    {
                        if (settingsFile.Attributes[i].Name.Contains("servoNames"))
                        {
                            settings.channelSettings[i].name = settingsFile.Attributes[i].Value.ToString(); // example value
                        }
                    }
                }

                // Get the script from the registry
                if (localSettings.Containers.ContainsKey("script"))
                {
                    string script = localSettings.Containers["script"].ToString();
                    try
                    {
                        // compile it to get the checksum
                        settings.setAndCompileScript(script);

                        BytecodeProgram program = settings.bytecodeProgram;
                        if (program.getByteList().Count > this.maxScriptLength)
                        {
                            throw new Exception();
                        }
                        if (program.getCRC() != (ushort)await getRawParameter(uscParameter.PARAMETER_SCRIPT_CRC))
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception)
                    {
                        // no script found or error compiling - leave script at ""
                        settings.scriptInconsistent = true;
                    }

                    // Get the sequences from the registry.
                    settings.sequences = Pololu.Usc.Sequencer.Sequence.readSequencesFromRegistry(settingsFile,fname, servoCount);
                }
            }


            return settings;
        }

        /// <summary>
        /// send all the settings to the board
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="newScript"></param>
        public async void setUscSettings(UscSettings settings, bool newScript)
        {
            string fname = ApplicationData.Current.LocalFolder.Path + "\\" + maestroDevice.Id;
            await setRawParameter(uscParameter.PARAMETER_SERIAL_MODE, (byte)settings.serialMode);
            await setRawParameter(uscParameter.PARAMETER_SERIAL_FIXED_BAUD_RATE, convertBpsToSpbrg(settings.fixedBaudRate));
            await setRawParameter(uscParameter.PARAMETER_SERIAL_ENABLE_CRC, (ushort)(settings.enableCrc ? 1 : 0));
            await setRawParameter(uscParameter.PARAMETER_SERIAL_NEVER_SUSPEND, (ushort)(settings.neverSuspend ? 1 : 0));
            await setRawParameter(uscParameter.PARAMETER_SERIAL_DEVICE_NUMBER, settings.serialDeviceNumber);
            await setRawParameter(uscParameter.PARAMETER_SERIAL_MINI_SSC_OFFSET, settings.miniSscOffset);
            await setRawParameter(uscParameter.PARAMETER_SERIAL_TIMEOUT, settings.serialTimeout);
            await setRawParameter(uscParameter.PARAMETER_SCRIPT_DONE, (ushort)(settings.scriptDone ? 1 : 0));

            if (servoCount == 6)
            {
                await setRawParameter(uscParameter.PARAMETER_SERVOS_AVAILABLE, settings.servosAvailable);
                await setRawParameter(uscParameter.PARAMETER_SERVO_PERIOD, settings.servoPeriod);
            }
            else
            {
                await setRawParameter(uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L, (byte)(settings.miniMaestroServoPeriod & 0xFF));
                await setRawParameter(uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU, (ushort)(settings.miniMaestroServoPeriod >> 8));

                byte multiplier;
                if (settings.servoMultiplier < 1)
                {
                    multiplier = 0;
                }
                else if (settings.servoMultiplier > 256)
                {
                    multiplier = 255;
                }
                else
                {
                    multiplier = (byte)(settings.servoMultiplier - 1);
                }
                await setRawParameter(uscParameter.PARAMETER_SERVO_MULTIPLIER, multiplier);
            }

            if (servoCount > 18)
            {
                await setRawParameter(uscParameter.PARAMETER_ENABLE_PULLUPS, (ushort)(settings.enablePullups ? 1 : 0));
            }

            // registry not support so we now use an xml file
            // if it exists delete and then creat a new xml doc
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
            XmlDocument settingsFile = new XmlDocument();
            
            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new byte[6] { 0, 0, 0, 0, 0, 0 };

            for (byte i = 0; i < servoCount; i++)
            {
                
               // servosetting.name = "";  // don't support naming yet
                XmlAttribute attr = settingsFile.CreateAttribute("servoName" + i.ToString("d2"));
                attr.Value = settings.channelSettings[i].name;
                
               // regSettings.Add("servoName" + i.ToString("d2"), servosetting.name);
                //key.SetValue("servoName" + i.ToString("d2"), setting.name, RegistryValueKind.String);

                if (microMaestro)
                {
                    if (settings.channelSettings[i].mode == ChannelMode.Input || settings.channelSettings[i].mode == ChannelMode.Output)
                    {
                        ioMask |= (byte)(1 << channelToPort(i));
                    }

                    if (settings.channelSettings[i].mode == ChannelMode.Output)
                    {
                        outputMask |= (byte)(1 << channelToPort(i));
                    }
                }
                else
                {
                    channelModeBytes[i >> 2] |= (byte)((byte)settings.channelSettings[i].mode << ((i & 3) << 1));
                }

                // Make sure that HomeMode is "Ignore" for inputs.  This is also done in
                // fixUscSettings.
                HomeMode correctedHomeMode = settings.channelSettings[i].homeMode;
                if (settings.channelSettings[i].mode == ChannelMode.Input)
                {
                    correctedHomeMode = HomeMode.Ignore;
                }

                // Compute the raw value of the "home" parameter.
                ushort home;
                if (correctedHomeMode == HomeMode.Off) home = 0;
                else if (correctedHomeMode == HomeMode.Ignore) home = 1;
                else home = settings.channelSettings[i].home;
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_HOME, i), home);

                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MIN, i), (ushort)(settings.channelSettings[i].minimum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MAX, i), (ushort)(settings.channelSettings[i].maximum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_NEUTRAL, i), settings.channelSettings[i].neutral);
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_RANGE, i), (ushort)(settings.channelSettings[i].range / 127));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_SPEED, i), normalSpeedToExponentialSpeed(settings.channelSettings[i].speed));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_ACCELERATION, i), settings.channelSettings[i].acceleration);
            }

            if (microMaestro)
            {
                await setRawParameter(uscParameter.PARAMETER_IO_MASK_C, ioMask);
                await setRawParameter(uscParameter.PARAMETER_OUTPUT_MASK_C, outputMask);
            }
            else
            {
                for (byte i = 0; i < 6; i++)
                {
                    await setRawParameter(uscParameter.PARAMETER_CHANNEL_MODES_0_3 + i, channelModeBytes[i]);
                }
            }

            if (newScript)
            {
                setScriptDone(1); // stop the script

                // load the new script
                BytecodeProgram program = settings.bytecodeProgram;
                List<byte> byteList = program.getByteList();
                if (byteList.Count > maxScriptLength)
                {
                    throw new Exception("Script too long for device (" + byteList.Count + " bytes)");
                }
                if (byteList.Count < maxScriptLength)
                {
                    // if possible, add QUIT to the end to prevent mysterious problems with
                    // unterminated scripts
                    byteList.Add((byte)Opcode.QUIT);
                }
                eraseScript();
                setSubroutines(program.subroutineAddresses, program.subroutineCommands);
                writeScript(byteList);
                await setRawParameter(uscParameter.PARAMETER_SCRIPT_CRC, program.getCRC());

                // Save the script in the registry
                XmlAttribute attr = settingsFile.CreateAttribute("script");
                attr.Value = settings.script;
            }

            Pololu.Usc.Sequencer.Sequence.saveSequencesInRegistry(settings.sequences, settingsFile,fname);


        }

        /// <summary>
        /// Opens the local registry file
        /// windows store , uwp and iot device do not support regestry access
        /// this function is only here as a stop gap and will change so use with caution
        /// </summary>
        private void openRegistryKey()
        {
            
            keyname = "Software\\Pololu\\Maestro USB servo controller\\" + maestroDevice.productId.ToString();
            regSettings = (ApplicationDataCompositeValue)localSettings.Values[keyname];

        }

        /// <summary>
        /// Sets the Servo Parameters within the Usc Settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="Channel"></param>
        public async void setServoParameters(UscSettings settings, byte Channel)
        {
            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new byte[6] { 0, 0, 0, 0, 0, 0 };
            ChannelSetting setting = settings.channelSettings[Channel];
            setting.name = "";  // don't support naming yet
                                //key.SetValue("servoName" + i.ToString("d2"), setting.name, RegistryValueKind.String);

            if (microMaestro)
            {
                if (setting.mode == ChannelMode.Input || setting.mode == ChannelMode.Output)
                {
                    ioMask |= (byte)(1 << channelToPort(Channel));
                }

                if (setting.mode == ChannelMode.Output)
                {
                    outputMask |= (byte)(1 << channelToPort(Channel));
                }
            }
            else
            {
                channelModeBytes[Channel >> 2] |= (byte)((byte)setting.mode << ((Channel & 3) << 1));
            }

            // Make sure that HomeMode is "Ignore" for inputs.  This is also done in
            // fixUscSettings.
            HomeMode correctedHomeMode = setting.homeMode;
            if (setting.mode == ChannelMode.Input)
            {
                correctedHomeMode = HomeMode.Ignore;
            }

            // Compute the raw value of the "home" parameter.
            ushort home;
            if (correctedHomeMode == HomeMode.Off) home = 0;
            else if (correctedHomeMode == HomeMode.Ignore) home = 1;
            else home = setting.home;
            try
            {
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_HOME, Channel), home);

                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MIN, Channel), (ushort)(setting.minimum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MAX, Channel), (ushort)(setting.maximum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_NEUTRAL, Channel), setting.neutral);
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_RANGE, Channel), (ushort)(setting.range / 127));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_SPEED, Channel), normalSpeedToExponentialSpeed(setting.speed));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_ACCELERATION, Channel), setting.acceleration);
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }


        private unsafe uint getMiniVariablesPacketSize()
        {
            return (uint)sizeof(MaestroVariables);
        }


        /// <summary>
        /// read the Variables from a mini maestro
        /// </summary>
        /// <returns></returns>
        private async Task<MaestroVariables> getVariablesMiniMaestro()
        {
            try
            {
                // Get miscellaneous variables.
               
                IBuffer buffer = await miniMaestroControlTransfer((byte)uscRequest.REQUEST_GET_VARIABLES, getMiniVariablesPacketSize());
                UInt32 bytesRead =buffer.Length;
                if (bytesRead != getMiniVariablesPacketSize())
                {
                    throw new Exception("Short read: " + bytesRead + " < " + getMiniVariablesPacketSize() + ".");
                }

                DataReader reader = DataReader.FromBuffer(buffer);
                MaestroVariables variables = new MaestroVariables();
                // Copy the variable data
                variables.stackPointer = reader.ReadByte();
                variables.callStackPointer = reader.ReadByte();
                variables.errors = reader.ReadUInt16();
                variables.programCounter = reader.ReadUInt16();
                variables.scriptDone = reader.ReadByte();
                variables.performanceFlags = reader.ReadByte();
                return variables;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return new MaestroVariables(); // return empty structs
            }
        }


       /// <summary>
       /// reads the servo variables from maestro
       /// </summary>
       /// <returns></returns>
        private async Task<ServoStatus[]> getServosMiniMaestro()
        {
            try
            {
                uint packetSize = (uint)(servoCount * 7);
                ServoStatus[] servos = new ServoStatus[servoCount];
                IBuffer buffer = await miniMaestroControlTransfer((byte)uscRequest.REQUEST_GET_SERVO_SETTINGS,packetSize);
                DataReader reader = DataReader.FromBuffer(buffer);
                servos = new ServoStatus[servoCount];
                for (byte i = 0; i < servoCount; i++)
                {
                    servos[i].position = reader.ReadUInt16();
                    servos[i].target = reader.ReadUInt16();
                    servos[i].speed = reader.ReadUInt16();
                    servos[i].acceleration = reader.ReadByte();
                }
                return servos;
                
            } catch(Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
         }

        private unsafe uint getMicroVariablesPacketSize()
        {
            return (uint)(sizeof(MicroMaestroVariables) + servoCount * sizeof(ServoStatus));
        }

        /// <summary>
        /// read variables from micro maestro
        /// </summary>
        /// <returns></returns>
        private async Task<bool> getVariablesMicroMaestro()
        {
            try
            {
                uint packetSize = getMicroVariablesPacketSize();
                IBuffer buffer = await microMaestroControlTransfer((byte)uscRequest.REQUEST_GET_VARIABLES, packetSize);
                if (buffer.Length != getMicroVariablesPacketSize())
                {
                    throw new Exception("Short read: " + buffer.Length + " < " + getMicroVariablesPacketSize() + ".");
                }
                // copy the variable data
                DataReader reader = DataReader.FromBuffer(buffer);

                // Copy the variable data
                mVariables.stackPointer = reader.ReadByte();
                mVariables.callStackPointer = reader.ReadByte();
                mVariables.errors = reader.ReadUInt16();
                mVariables.programCounter = reader.ReadUInt16();
                mVariables.scriptDone = reader.ReadByte();
                mVariables.performanceFlags = reader.ReadByte();
                for (byte i = 0; i < servoCount; i++)
                {
                    mServoStatus[i].position = reader.ReadUInt16();
                    mServoStatus[i].target = reader.ReadUInt16();
                    mServoStatus[i].speed = reader.ReadUInt16();
                    mServoStatus[i].acceleration = reader.ReadByte();
                }
                stack = new short[mVariables.stackPointer];
                for (byte i = 0; i < stack.Length; i++)
                {
                    stack[i] = reader.ReadInt16();
                }
                callstack = new ushort[mVariables.callStackPointer];
                for (byte i = 0; i < callstack.Length; i++)
                {
                    callstack[i] = reader.ReadUInt16();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }


        /// <summary>
        /// read the mini mastro stack
        /// </summary>
        /// <returns></returns>
        private async Task<short[]> getStackMiniMaestro()
        {
            try
            {
                // set stack to maximumsize.
                short[] stack = new short[MiniMaestroStackSize];
                // try and read a full stack worth of data
                IBuffer buffer = await miniMaestroControlTransfer((byte)uscRequest.REQUEST_GET_STACK, (uint)(sizeof(short) * MiniMaestroStackSize));
                // setup a datareader
                DataReader reader = DataReader.FromBuffer(buffer);
                // copy the number of unsigned  shorts read into callstack array
                for (int i = 0;i < (buffer.Length / sizeof(short)); i++)
                {
                    stack[i] = reader.ReadInt16();
                }
                // resize the stack to size used
                Array.Resize<short>(ref stack, (int)(buffer.Length / sizeof(short)));
                return stack;

            }
            catch (Exception e)
            {
                Debug.WriteLine("error reading stack variables "+e);
                return new short[MiniMaestroStackSize];
            }
        }

        /// <summary>
        /// Read the MiniMaestro Call Stack
        /// </summary>
        /// <returns></returns>
        private async Task<ushort[]> getCallStackMiniMaestro()
        {
            try
            {
                // set callstack to maximum size 
                ushort[] callStack = new ushort[MiniMaestroCallStackSize];
                // try and read a full callstack worth of data
                IBuffer buffer = await miniMaestroControlTransfer((byte)uscRequest.REQUEST_GET_CALL_STACK, (uint)(sizeof(ushort) * MiniMaestroCallStackSize));
                // setup a datareader
                DataReader reader = DataReader.FromBuffer(buffer);
                // copy the number of unsigned  shorts read into callstack array
                for (int i = 0; i < (buffer.Length /sizeof(ushort)); i++)
                {
                    callStack[i] = reader.ReadUInt16();
                }
                Array.Resize<ushort>(ref callStack, (int)(buffer.Length / sizeof(ushort)));
                return callStack;

            }
            catch (Exception e)
            {
                Debug.WriteLine("error reading callstack variables "+e);
                return new ushort[MiniMaestroCallStackSize];
            }
        }


        /// <summary>
        /// Mini Meastro Variables control transfer
        /// </summary>
        /// <param name="uscrequest"></param>
        /// <param name="packetSize"></param>
        /// <returns></returns>
        public async Task<IBuffer> miniMaestroControlTransfer(byte uscrequest, uint packetSize)
        {
            try
            {
                var buffer = new Windows.Storage.Streams.Buffer(packetSize);
                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscrequest,
                    Length = packetSize
                };

                IBuffer retBuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
                return retBuffer;
            } catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }


        /// <summary>
        /// micro maestro read variable ControlTransfer
        /// </summary>
        /// <param name="uscrequest"></param>
        /// <param name="packetSize"></param>
        /// <returns></returns>
        public async Task<IBuffer> microMaestroControlTransfer(byte uscrequest, uint packetSize)
        {
            try
            {
                var buffer = new Windows.Storage.Streams.Buffer(packetSize);
                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscRequest.REQUEST_GET_SERVO_SETTINGS,
                    Length = packetSize
                };

                IBuffer retBuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
                return retBuffer;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }


        /// <summary>
        /// read maestro variables 
        /// </summary>
        public async void getMaestroVariables()
        {
            try
            {
                uint packetLength;
                //set size depending on device
                if (microMaestro)
                {
                    packetLength = microVariablePacketSize;
                    stack = new short[MicroMaestroStackSize];
                    callstack = new ushort[MicroMaestroCallStackSize];
                    await getVariablesMicroMaestro();
                }
                else
                {
                    mVariables = await getVariablesMiniMaestro();
                    mServoStatus = await  getServosMiniMaestro();
                    stack = new short[MiniMaestroStackSize];
                    stack = await getStackMiniMaestro();
                    callstack = new ushort[MiniMaestroCallStackSize];       
                    callstack = await getCallStackMiniMaestro();
                }
              
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                
            }
        }

        /// <summary>
        /// get firmware Majorversion
        /// </summary>
        public UInt16 firmwareVersionMajor
        {
            get
            {
                if (privateFirmwareVersionMajor == 0xFF)
                {
                    getFirmwareVersion();
                }
                return privateFirmwareVersionMajor;
            }
        }


        /// <summary>
        /// get firmawre Minor version
        /// </summary>
        public Byte firmwareVersionMinor
        {
            get
            {
                if (privateFirmwareVersionMajor == 0xFF)
                {
                    getFirmwareVersion();
                }
                return privateFirmwareVersionMinor;
            }
        }

        /// <summary>
        /// firmware version as a string
        /// </summary>
        public String firmwareVersionString
        {
            get
            {
                return firmwareVersionMajor.ToString() + "." + firmwareVersionMinor.ToString("D2");
            }
        }


        /// <summary>
        /// Read firmware version from the board
        /// </summary>
        private async void getFirmwareVersion()
        {
            Byte[] bytebuffer = new Byte[14];

            try
            {
                var buffer = new Windows.Storage.Streams.Buffer(14);
                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)0x06,
                    Value = 0x0100,
                    Length = 14
                };

                IBuffer retBuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
                DataReader reader = DataReader.FromBuffer(retBuffer);
                //controlTransfer(0x80, 6, 0x0100, 0x0000, buffer);
                if (retBuffer.Length == bytebuffer.Length)
                {
                    for(int i=0;i < retBuffer.Length;i++)
                    {
                        bytebuffer[i] = reader.ReadByte();
                    }
                    privateFirmwareVersionMinor = (Byte)((bytebuffer[12] & 0xF) + (bytebuffer[12] >> 4 & 0xF) * 10);
                    privateFirmwareVersionMajor = (Byte)((bytebuffer[13] & 0xF) + (bytebuffer[13] >> 4 & 0xF) * 10);
                }
                else
                {
                    throw new Exception("There was an error getting the firmware version from the device.");
                }
            }
            catch (Exception exception)
            {
                throw new Exception("There was an error getting the firmware version from the device.", exception);
            }

            
        }


        /// <summary>
        /// servo setTarget
        /// </summary>
        /// <param name="servo">channel number</param>
        /// <param name="value">new position in us</param>
        public void setTarget(byte servo, ushort value)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_SET_TARGET, value, servo);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set target of servo " + servo + " to " + value + ".", e);
            }
        }


        /// <summary>
        /// set the speed of the servo
        /// </summary>
        /// <param name="servo"></param>
        /// <param name="value"></param>
        public void setSpeed(byte servo, ushort value)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_SET_SERVO_VARIABLE, value, servo);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set speed of servo " + servo + " to " + value + ".", e);
            }
        }


        /// <summary>
        /// set servo acceleration
        /// </summary>
        /// <param name="servo"></param>
        /// <param name="value"></param>
        public void setAcceleration(byte servo, ushort value)
        {
            // set the high bit of servo to specify acceleration
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_SET_SERVO_VARIABLE,
                                       value, (byte)(servo | 0x80));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to set acceleration of servo " + servo + " to " + value + ".", e);
            }
        }

        /// <summary>
        /// Erases the entire script and subroutine address table from the devices.
        /// </summary>
        public void eraseScript()
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_ERASE_SCRIPT, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error erasing the script.", e);
            }
        }

        /// <summary>
        /// restart script at subroutine  with a parameter
        /// </summary>
        /// <param name="subroutine"></param>
        /// <param name="parameter"></param>
        public void restartScriptAtSubroutineWithParameter(byte subroutine, short parameter)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_RESTART_SCRIPT_AT_SUBROUTINE_WITH_PARAMETER, (ushort)parameter, subroutine);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script with a parameter at subroutine " + subroutine + ".", e);
            }
        }


        public void restartScriptAtSubroutine(byte subroutine)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_RESTART_SCRIPT_AT_SUBROUTINE, 0, subroutine);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script at subroutine " + subroutine + ".", e);
            }
        }

        public void restartScript()
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_RESTART_SCRIPT, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error restarting the script.", e);
            }
        }

        /// <summary>
        /// Write Script to Meastro 
        /// Not fully tested 
        /// </summary>
        /// <param name="bytecode"></param>
        public async void writeScript(List<byte> bytecode)
        {
            ushort block;
            for (block = 0; block < (bytecode.Count + 15) / 16; block++)
            {
                // write each block in a separate request
                byte[] block_bytes = new byte[16];

                ushort j;
                for (j = 0; j < 16; j++)
                {
                    if (block * 16 + j < bytecode.Count)
                        block_bytes[j] = bytecode[block * 16 + j];
                    else
                        block_bytes[j] = (byte)0xFF; // don't change flash if it is not necessary
                }

                try
                {
                    //                    System.Console.WriteLine((block)+": "+block_bytes[0]+" "+block_bytes[1]+" "+block_bytes[2]+" "+block_bytes[3]+" "+block_bytes[4]+" "+block_bytes[5]+" "+block_bytes[6]+" "+block_bytes[7]+" "+block_bytes[8]+" "+block_bytes[9]+" "+block_bytes[10]+" "+block_bytes[11]+" "+block_bytes[12]+" "+block_bytes[13]+" "+block_bytes[14]+" "+block_bytes[15]); // XXX
                    await controlTransfer(0x40, (byte)uscRequest.REQUEST_WRITE_SCRIPT, 0, block,
                                           block_bytes);
                }
                catch (Exception e)
                {
                    throw new Exception("There was an error writing script block " + block + ".", e);
                }
            }
        }

        /// <remarks>
        /// Prior to 2011-7-20, this function had a bug in it that made
        /// subroutines 64-123 not work!
        /// </remarks>
        public async void setSubroutines(Dictionary<string, ushort> subroutineAddresses,
                                   Dictionary<string, byte> subroutineCommands)
        {
            byte[] subroutineData = new byte[256];

            ushort i;
            for (i = 0; i < 256; i++)
                subroutineData[i] = 0xFF; // initialize to the default flash state

            foreach (KeyValuePair<string, ushort> kvp in subroutineAddresses)
            {
                string name = kvp.Key;
                byte bytecode = subroutineCommands[name];

                if (bytecode == (byte)Opcode.CALL)
                    continue; // skip CALLs - these do not get a position in the subroutine memory

                subroutineData[2 * (bytecode - 128)] = (byte)(kvp.Value % 256);
                subroutineData[2 * (bytecode - 128) + 1] = (byte)(kvp.Value >> 8);
            }

            ushort block;
            for (block = 0; block < 16; block++)
            {
                // write each block in a separate request
                byte[] block_bytes = new byte[16];

                ushort j;
                for (j = 0; j < 16; j++)
                {
                    block_bytes[j] = subroutineData[block * 16 + j];
                }

                try
                {
                    
                    //                    System.Console.WriteLine((block + subroutineOffsetBlocks)+": "+block_bytes[0]+" "+block_bytes[1]+" "+block_bytes[2]+" "+block_bytes[3]+" "+block_bytes[4]+" "+block_bytes[5]+" "+block_bytes[6]+" "+block_bytes[7]+" "+block_bytes[8]+" "+block_bytes[9]+" "+block_bytes[10]+" "+block_bytes[11]+" "+block_bytes[12]+" "+block_bytes[13]+" "+block_bytes[14]+" "+block_bytes[15]); // XXX
                    await controlTransfer(0x40, (byte)uscRequest.REQUEST_WRITE_SCRIPT, 0,
                                           (ushort)(block + subroutineOffsetBlocks),
                                           block_bytes);
                }
                catch (Exception e)
                {
                    throw new Exception("There was an error writing subroutine block " + block + ".", e);
                }
            }
        }


        private uint subroutineOffsetBlocks
        {
            get
            {
                switch (maestroDevice.productId)
                {
                    case 0x89: return 64;
                    case 0x8A: return 512;
                    case 0x8B: return 512;
                    case 0x8C: return 512;
                    default: throw new Exception("unknown product ID");
                }
            }
        }

        public void setScriptDone(byte value)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_SET_SCRIPT_DONE, value, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error setting the script done.", e);
            }
        }

        public void startBootloader()
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_START_BOOTLOADER, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error entering bootloader mode.", e);
            }
        }

        public void reinitialize()
        {
            reinitialize(50);
        }


        private void reinitialize(int waitTime)
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_REINITIALIZE, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was an error re-initializing the device.", e);
            }

            Task.Delay(waitTime);
            if (!microMaestro)
            {
                // Flush out any spurious performance flags that might have occurred.
                getMaestroVariables();
            }
        }

        public void clearErrors()
        {
            try
            {
                controlTransfer(0x40, (byte)uscRequest.REQUEST_CLEAR_ERRORS, 0, 0);
            }
            catch (Exception e)
            {
                throw new Exception("There was a USB communication error while clearing the servo errors.", e);
            }
        }



        private async Task<uint> setRawParameter(uscParameter parameter, ushort value)
        {
            Range range = getRange(parameter);
            requireArgumentRange(value, range.minimumValue, range.maximumValue, parameter.ToString());
            int bytes = range.bytes;
            return await setRawParameterNoChecks((ushort)parameter, value, bytes);
        }

        /// <summary>
        /// Sets the parameter without checking the range or bytes
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        /// <param name="bytes"></param>
        
        private async Task<uint> setRawParameterNoChecks(ushort parameter, ushort value, int bytes)
        {
            ushort index = (ushort)((bytes << 8) + parameter); // high bytes = # of bytes
            try
            {
               // controlTransfer(0x40, (byte)uscRequest.REQUEST_SET_PARAMETER, value, index);
               // Range range = getRange(parameter);

                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.Out,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscRequest.REQUEST_SET_PARAMETER,
                    Value = value,
                    Index = index,
                    Length = 0
                };


                return await maestroDevice.device.SendControlOutTransferAsync(setupPacket);
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                return 0;
            }
        }

      
        private async Task<UInt16> getRawParameter(uscParameter parameter)
        {
            ushort value = 0;
            try
            {
                IBuffer retbuffer = await SendVendorControlTransferParameterInAsync(parameter);
                DataReader reader = DataReader.FromBuffer(retbuffer);
                if (retbuffer.Length == 1)
                {
                    value = Convert.ToUInt16(reader.ReadByte());
                }
                else
                   if (retbuffer.Length == 2)
                {
                    value = reader.ReadUInt16();
                }
                else
                    value = 0;  // actually an error
            }
            catch (Exception e)
            {
                throw new Exception("There was an error getting parameter " + parameter.ToString() + " from the device.", e);
            }
            return value;
        }

        private async Task<IBuffer> SendVendorControlTransferParameterInAsync(uscParameter parameter)
        {
            try
            {
                Range range = getRange(parameter);
                ushort value = 0;
                byte[] array = new byte[range.bytes];
                // Data will be written to this buffer when we receive it
                var buffer = new Windows.Storage.Streams.Buffer(range.bytes);


                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscRequest.REQUEST_GET_PARAMETER,
                    Value = value,
                    Index = (UInt16)parameter,
                    Length = range.bytes
                };
                return await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                // todo fix this kludge 
                return new Windows.Storage.Streams.Buffer(7);
            }
        }

       
        private static void requireArgumentRange(uint argumentValue, Int32 minimum, Int32 maximum, String argumentName)
        {
            if (argumentValue < minimum || argumentValue > maximum)
            {
                throw new ArgumentException("The " + argumentName + " must be between " + minimum +
                    " and " + maximum + " but the value given was " + argumentValue);
            }
        }
        protected static Range getRange(uscParameter parameterId)
        {
            if (parameterId == uscParameter.PARAMETER_INITIALIZED)
                return Range.u8;

            switch (parameterId)
            {
                case uscParameter.PARAMETER_SERVOS_AVAILABLE:
                    return Range.u8;
                case uscParameter.PARAMETER_SERVO_PERIOD:
                    return Range.u8;
                case uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L:
                    return Range.u8;
                case uscParameter.PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU:
                    return Range.u16;
                case uscParameter.PARAMETER_SERVO_MULTIPLIER:
                    return Range.u8;
                case uscParameter.PARAMETER_CHANNEL_MODES_0_3:
                case uscParameter.PARAMETER_CHANNEL_MODES_4_7:
                case uscParameter.PARAMETER_CHANNEL_MODES_8_11:
                case uscParameter.PARAMETER_CHANNEL_MODES_12_15:
                case uscParameter.PARAMETER_CHANNEL_MODES_16_19:
                case uscParameter.PARAMETER_CHANNEL_MODES_20_23:
                case uscParameter.PARAMETER_ENABLE_PULLUPS:
                    return Range.u8;
                case uscParameter.PARAMETER_SERIAL_MODE:
                    return new Range(1, 0, 3);
                case uscParameter.PARAMETER_SERIAL_BAUD_DETECT_TYPE:
                    return new Range(1, 0, 1);
                case uscParameter.PARAMETER_SERIAL_NEVER_SUSPEND:
                    return Range.boolean;
                case uscParameter.PARAMETER_SERIAL_TIMEOUT:
                    return Range.u16;
                case uscParameter.PARAMETER_SERIAL_ENABLE_CRC:
                    return Range.boolean;
                case uscParameter.PARAMETER_SERIAL_DEVICE_NUMBER:
                    return Range.u7;
                case uscParameter.PARAMETER_SERIAL_FIXED_BAUD_RATE:
                    return Range.u16;
                case uscParameter.PARAMETER_SERIAL_MINI_SSC_OFFSET:
                    return new Range(1, 0, 254);
                case uscParameter.PARAMETER_SCRIPT_CRC:
                    return Range.u16;
                case uscParameter.PARAMETER_SCRIPT_DONE:
                    return Range.boolean;
            }

            // must be one of the servo parameters
            switch ((((byte)parameterId - (byte)uscParameter.PARAMETER_SERVO0_HOME) % 9) +
                    (byte)uscParameter.PARAMETER_SERVO0_HOME)
            {
                case (byte)uscParameter.PARAMETER_SERVO0_HOME:
                case (byte)uscParameter.PARAMETER_SERVO0_NEUTRAL:
                    return new Range(2, 0, 32440); // 32640 - 200
                case (byte)uscParameter.PARAMETER_SERVO0_RANGE:
                    return new Range(1, 1, 50); // the upper limit could be adjusted
                case (byte)uscParameter.PARAMETER_SERVO0_SPEED:
                case (byte)uscParameter.PARAMETER_SERVO0_MAX:
                case (byte)uscParameter.PARAMETER_SERVO0_MIN:
                case (byte)uscParameter.PARAMETER_SERVO0_ACCELERATION:
                    return Range.u8;
            }

            throw new ArgumentException("Invalid parameterId " + parameterId.ToString() + ", can not determine the range of this parameter.");
        }

        protected struct Range
        {
            public Byte bytes;
            public Int32 minimumValue;
            public Int32 maximumValue;

            internal Range(Byte bytes, Int32 minimumValue, Int32 maximumValue)
            {
                this.bytes = bytes;
                this.minimumValue = minimumValue;
                this.maximumValue = maximumValue;
            }

            public Boolean signed
            {
                get
                {
                    return minimumValue < 0;
                }
            }

            internal static Range u32 = new Range(4, 0, 0x7FFFFFFF);
            internal static Range u16 = new Range(2, 0, 0xFFFF);
            internal static Range u12 = new Range(2, 0, 0x0FFF);
            internal static Range u10 = new Range(2, 0, 0x03FF);
            internal static Range u8 = new Range(1, 0, 0xFF);
            internal static Range u7 = new Range(1, 0, 0x7F);
            internal static Range boolean = new Range(1, 0, 1);
        }


       
    }
}
