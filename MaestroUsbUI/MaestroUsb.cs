//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using Windows.Storage.Streams;
using Windows.Devices.Usb;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MaestroUsbUI;
using Pololu.Usc;

namespace MaestroUsb
{
  


     public class MaestroDeviceListItem
    {
        private String name;
        private bool isConnected = false;
        private DeviceInformation devinformation;
        private UsbDevice usbDevice;
        private String id;
        private UInt16 ProductId;
        private MaestroDevice maestro;
        



        public DeviceInformation deviceInformation
        {
            get
            {
                return devinformation;
            }
        }

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
        /// The text to display to the user in the list to represent this
        /// device.  By default, this text is "#" + serialNumberString,
        /// but it can be changed to suit the application's needs
        /// (for example, adding model information to it).
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
        /// Gets the serial number.
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
        /// Creates an item that doesn't actually refer to a device; just for populating the list with things like "Disconnected"
        /// </summary>
        /// <param name="text"></param>
        public static MaestroDeviceListItem CreateDummyItem(String text)
        {
            var item = new MaestroDeviceListItem(null,null, text, "", 0);
            return item;
        }

        public MaestroDeviceListItem(UsbDevice devicePointer, DeviceInformation devInfo, string text, string Id, UInt16 productId)
        {
            usbDevice = devicePointer;
            name = text;
            devinformation = devInfo;
            id = Id;
            ProductId = productId;
            maestro = new MaestroDevice(this);
    }

        ~MaestroDeviceListItem()
        {
            if (device != null)
            {
                device.Dispose();

            }
        }


    }

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



        public MaestroDeviceManager()
        {
            // create new device list
            maestroDeviceList = new Collection<MaestroDeviceListItem>(); 
          //  OpenDeviceWatcher();
        }

        //list built by class watchers
        public Collection<MaestroDeviceListItem> DeviceList
        {
            get
            {
                return maestroDeviceList;
            }
        }


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


        // open device watchers for our class
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
                        if (deviceRemovedCallback != null)
                        {
                            deviceRemovedCallback(args);
                        }
                    }
                }
            }
        }

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
                    if (deviceStoppedCallback != null)
                    {
                        deviceStoppedCallback(match);
                    }
                }
            }
        }

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



    public class MaestroDevice
    {
        private byte servoParameterBytes = 9;
        private MaestroDeviceListItem maestroDevice;
        private const int INSTRUCTION_FREQUENCY = 12000000;
        public const int MicroMaestroStackSize = 32;
        public const int MicroMaestroCallStackSize = 10;

        public const int MiniMaestroStackSize = 126;
        public const int MiniMaestroCallStackSize = 126;
        private byte servoCount;
        public static UInt16[] DevicePids = { (UInt16)0x0089, (UInt16)0x008A, (UInt16)0x008B, (UInt16)0x008C };
        
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
        }

        public byte ServoCount
        {
            get
            {
                return servoCount;
            }
        }

        protected bool microMaestro
        {
            get
            {
                return servoCount == 6;
            }
        }
       

        private static ushort exponentialSpeedToNormalSpeed(byte exponentialSpeed)
        {
            // Maximum value of normalSpeed is 31*(1<<7)=3968

            int mantissa = exponentialSpeed >> 3;
            int exponent = exponentialSpeed & 7;

            return (ushort)(mantissa * (1 << exponent));
        }

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

        public static decimal positionToMicroseconds(ushort position)
        {
            return (decimal)position / 4M;
        }

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
                    Value = value,
                    Index = index,
                    Length = bufferToSend.Length
                };

                UInt32 bytesTransferred = await maestroDevice.device.SendControlOutTransferAsync(setupPacket, bufferToSend);

            }
            if (requestType == 0xC0)
            {
                // read
                try
                {
                    UInt16 packetSize = Convert.ToUInt16(data.Length);
                    UInt16 retValue;
                    // Data will be written to this buffer when we receive it
                    var buffer = new Windows.Storage.Streams.Buffer(packetSize);

                    UsbSetupPacket setupPacket = new UsbSetupPacket
                    {
                        RequestType = new UsbControlRequestType
                        {
                            Direction = UsbTransferDirection.In,
                            Recipient = UsbControlRecipient.Device,
                            ControlTransferType = UsbControlTransferType.Vendor,
                        },
                        Request = request,
                        Value = value,
                        Index = index,
                        Length = packetSize
                    };

                    IBuffer retbuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
                    DataReader reader = DataReader.FromBuffer(retbuffer);
                    if (retbuffer.Length == 1)
                    {
                        retValue = Convert.ToUInt16(reader.ReadByte());
                    }
                    else
                    if (retbuffer.Length == 2)
                    {
                        retValue = reader.ReadUInt16();
                    }
                    else
                        retValue = 0;  // actually an error

                    return retValue;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    return 0;  // we return a buffer with the wrong size it will then be ignored higher up

                }
            }
            return 0;// dummy get rid of when coded
        }

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

        protected unsafe uint controlTransfer(byte RequestType, byte Request, ushort Value, ushort Index, void* data, ushort Length)
        {
            return 0; // dummy get rid of when coded
        }

        private uscSerialMode bytetoSerialMode(byte modebyte)
        {
            return (uscSerialMode)(modebyte);
        }

        public async Task<UscSettings> getUscSettings()
        {
            UscSettings settings = new UscSettings();
           
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

           
            return settings;
        }


        public async void setUscSettings(UscSettings settings, bool newScript)
        {
            
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

           

            byte ioMask = 0;
            byte outputMask = 0;
            byte[] channelModeBytes = new byte[6] { 0, 0, 0, 0, 0, 0 };

            for (byte i = 0; i < servoCount; i++)
            {
                ChannelSetting setting = settings.channelSettings[i];
                setting.name = "";  // don't support naming yet
                //key.SetValue("servoName" + i.ToString("d2"), setting.name, RegistryValueKind.String);

                if (microMaestro)
                {
                    if (setting.mode == ChannelMode.Input || setting.mode == ChannelMode.Output)
                    {
                        ioMask |= (byte)(1 << channelToPort(i));
                    }

                    if (setting.mode == ChannelMode.Output)
                    {
                        outputMask |= (byte)(1 << channelToPort(i));
                    }
                }
                else
                {
                    channelModeBytes[i >> 2] |= (byte)((byte)setting.mode << ((i & 3) << 1));
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
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_HOME, i), home);

                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MIN, i), (ushort)(setting.minimum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_MAX, i), (ushort)(setting.maximum / 64));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_NEUTRAL, i), setting.neutral);
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_RANGE, i), (ushort)(setting.range / 127));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_SPEED, i), normalSpeedToExponentialSpeed(setting.speed));
                await setRawParameter(specifyServo(uscParameter.PARAMETER_SERVO0_ACCELERATION, i), setting.acceleration);
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

           
        }


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


        /// <summary>
        /// Gets the complete set of status information for the Maestro.
        /// </summary>
        /// <remarks>If you are using a Mini Maestro and do not need all of
        /// the data provided by this function, you can save some CPU time
        /// by using the overloads with fewer arguments.</remarks>
     /*   public unsafe void getVariables(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos)
        {
            if (microMaestro)
            {
                // On the Micro Maestro, this function requires just one control transfer:
                getVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                // On the Mini Maestro, this function requires four control transfers:
                getVariablesMiniMaestro(out variables);
                getVariablesMiniMaestro(out servos);
                getVariablesMiniMaestro(out stack);
                getVariablesMiniMaestro(out callStack);
            }
        }

        /// <summary>
        /// Gets a MaestroVariables struct representing the current status
        /// of the device.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void getVariables(out MaestroVariables variables)
        {
            if (microMaestro)
            {
                ServoStatus[] servos;
                short[] stack;
                ushort[] callStack;
                getVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                getVariablesMiniMaestro(out variables);
            }
        }

        /// <summary>
        /// Gets an array of ServoStatus structs representing
        /// the current status of all the channels.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void getVariables(out ServoStatus[] servos)
        {
            if (microMaestro)
            {
                MaestroVariables variables;
                short[] stack;
                ushort[] callStack;
                getVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                getVariablesMiniMaestro(out servos);
            }
        }

        /// <summary>
        /// Gets an array of shorts[] representing the current stack.
        /// The maximum size of the array is stackSize.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void getVariables(out short[] stack)
        {
            if (microMaestro)
            {
                MaestroVariables variables;
                ServoStatus[] servos;
                ushort[] callStack;
                getVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                getVariablesMiniMaestro(out stack);
            }
        }

        /// <summary>
        /// Gets an array of ushorts[] representing the current stack.
        /// The maximum size of the array is callStackSize.
        /// </summary>
        /// <remarks>If you are using the Micro Maestro and calling
        /// getVariables more than once in quick succession,
        /// then you can save some CPU time by just using the
        /// overload that has 4 arguments.
        /// </remarks>
        public void getVariables(out ushort[] callStack)
        {
            if (microMaestro)
            {
                MaestroVariables variables;
                short[] stack;
                ServoStatus[] servos;
                getVariablesMicroMaestro(out variables, out stack, out callStack, out servos);
            }
            else
            {
                getVariablesMiniMaestro(out callStack);
            }
        }

        private unsafe void getVariablesMicroMaestro(out MaestroVariables variables, out short[] stack, out ushort[] callStack, out ServoStatus[] servos)
        {
            byte[] array = new byte[sizeof(MicroMaestroVariables) + servoCount * sizeof(ServoStatus)];

            try
            {
                controlTransfer(0xC0, (byte)uscRequest.REQUEST_GET_VARIABLES, 0, 0, array).RunSynchronously();
            }
            catch (Exception e)
            {
                throw new Exception("There was an error getting the device variables.", e);
            }

            fixed (byte* pointer = array)
            {
                // copy the variable data
                MicroMaestroVariables tmp = *(MicroMaestroVariables*)pointer;
                variables.stackPointer = tmp.stackPointer;
                variables.callStackPointer = tmp.callStackPointer;
                variables.errors = tmp.errors;
                variables.programCounter = tmp.programCounter;
                variables.scriptDone = tmp.scriptDone;
                variables.performanceFlags = 0;

                servos = new ServoStatus[servoCount];
                for (byte i = 0; i < servoCount; i++)
                {
                    servos[i] = *(ServoStatus*)(pointer + sizeof(MicroMaestroVariables) + sizeof(ServoStatus) * i);
                }

                stack = new short[variables.stackPointer];
                for (byte i = 0; i < stack.Length; i++) { stack[i] = *(tmp.stack + i); }

                callStack = new ushort[variables.callStackPointer];
                for (byte i = 0; i < callStack.Length; i++) { callStack[i] = *(tmp.callStack + i); }
            }
        }

        private unsafe void getVariablesMiniMaestro(out MaestroVariables variables)
        {
            try
            {
                // Get miscellaneous variables.
                MiniMaestroVariables tmp;
                UInt32 bytesRead = controlTransfer(0xC0, (byte)uscRequest.REQUEST_GET_VARIABLES, 0, 0, &tmp, (ushort)sizeof(MiniMaestroVariables));
                if (bytesRead != sizeof(MiniMaestroVariables))
                {
                    throw new Exception("Short read: " + bytesRead + " < " + sizeof(MiniMaestroVariables) + ".");
                }

                // Copy the variable data
                variables.stackPointer = tmp.stackPointer;
                variables.callStackPointer = tmp.callStackPointer;
                variables.errors = tmp.errors;
                variables.programCounter = tmp.programCounter;
                variables.scriptDone = tmp.scriptDone;
                variables.performanceFlags = tmp.performanceFlags;
            }
            catch (Exception e)
            {
                throw new Exception("Error getting variables from device.", e);
            }
        }*/

           public async Task<ServoStatus[]> getVariablesMiniMaestro(int servostructsize)
           {
               try
               {
                   // each servo status struct is 7 bytes
                   uint packetLength = (uint)(servoCount * servostructsize);
                   var buffer = new Windows.Storage.Streams.Buffer(packetLength);
                   byte[] servoSettingsArray = new byte[packetLength];

                   UsbSetupPacket setupPacket = new UsbSetupPacket
                   {
                       RequestType = new UsbControlRequestType
                       {
                           Direction = UsbTransferDirection.In,
                           Recipient = UsbControlRecipient.Device,
                           ControlTransferType = UsbControlTransferType.Vendor,
                       },
                       Request = (byte)uscRequest.REQUEST_GET_SERVO_SETTINGS,
                       Length = packetLength
                   };
                   IBuffer retBuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket,buffer);
                   DataReader reader = DataReader.FromBuffer(retBuffer);

                   if (retBuffer.Length != servoSettingsArray.Length)
                   {
                       throw new Exception("Short read: " + retBuffer.Length + " < " + servoSettingsArray.Length + ".");
                   }
                   reader.ReadBytes(servoSettingsArray);
                   // Put the data in to a managed array object.
                   ServoStatus[] servos = new ServoStatus[servoCount];
                   for (int i = 0; i < servoCount; i++)
                   {
                       servos[i].position = reader.ReadUInt16();
                       servos[i].target = reader.ReadUInt16();
                       servos[i].speed = reader.ReadUInt16();
                       servos[i].acceleration = reader.ReadByte();

                   }

                   return servos;
               }
               catch (Exception e)
               {
                   Debug.WriteLine(e);
                   return null;
               }
           }

        private unsafe void getVariablesMiniMaestro(out short[] stack)
        {
            try
            {
                // Get the data stack.
                stack = new short[MiniMaestroStackSize];
                fixed (short* pointer = stack)
                {
                    UInt32 bytesRead = controlTransfer(0xC0, (byte)uscRequest.REQUEST_GET_STACK, 0, 0, pointer, (ushort)(sizeof(short) * stack.Length));
                    Array.Resize<short>(ref stack, (int)(bytesRead / sizeof(short)));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error getting stack from device.", e);
            }
        }

        private unsafe void getVariablesMiniMaestro(out ushort[] callStack)
        {
            try
            {
                callStack = new ushort[MiniMaestroCallStackSize];
                fixed (ushort* pointer = callStack)
                {
                    UInt32 bytesRead = controlTransfer(0xC0, (byte)uscRequest.REQUEST_GET_CALL_STACK, 0, 0, pointer, (ushort)(sizeof(ushort) * callStack.Length));
                    Array.Resize<ushort>(ref callStack, (int)(bytesRead / sizeof(ushort)));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error getting call stack from device.", e);
            }
        }



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
                return await  maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                // todo fix this kludge 
                return new Windows.Storage.Streams.Buffer(7);
            }
        }

        private async Task<byte[]> SendVendorControlTransferArrayInAsync(uscRequest parameter,byte[] dataarray)
        {
            try
            {
                
                ushort arraySize = (ushort)(dataarray.Length);
                byte[] array = new byte[arraySize];
                // Data will be written to this buffer when we receive it
                var buffer = new Windows.Storage.Streams.Buffer(arraySize);


                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)parameter,
                    Value = 0,
                    Index = 0,
                    Length = arraySize
                };
                IBuffer retBuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket, buffer);
                DataReader reader = DataReader.FromBuffer(retBuffer); 
                reader.ReadBytes(array);
                return array;
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                // todo fix this kludge 
                return null;
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
