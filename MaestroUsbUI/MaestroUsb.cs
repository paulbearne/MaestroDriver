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
  /*  public enum DeviceType
    {
        Maestro,
        All,    // Can be any device
        None
    };

    public enum Descriptor
    {
        Device,
        Configuration,
        Interface,
        Endpoint,
        String,
        Custom
    };


    public struct ServoStatus
    {
        /// <summary>The position in units of quarter-microseconds.</summary>
        public UInt16 position;

        /// <summary>The target position in units of quarter-microseconds.</summary>
        public UInt16 target;

        /// <summary>The speed limit.  Units depends on your settings.</summary>
        public UInt16 speed;

        /// <summary>The acceleration limit.  Units depend on your settings.</summary>
        public Byte acceleration;
    };*/


  /*  public enum uscSerialMode : byte
    {
        ///<summary>On the Command Port, user can send commands and receive responses.
        ///TTL port/UART are connected to make a USB-to-serial adapter.</summary> 
        SERIAL_MODE_USB_DUAL_PORT = 0,

        ///<summary>On the Command Port, user can send commands to Maestro and
        /// simultaneously transmit bytes on the UART TX line, and user
        /// can receive bytes from the Maestro and the UART RX line.
        /// TTL port does not do anything.</summary>
        SERIAL_MODE_USB_CHAINED = 1,

        /// <summary>
        /// On the UART, user can send commands and receive reponses after
        /// sending a 0xAA byte to indicate the baud rate.
        /// Command Port receives bytes from the RX line.
        /// TTL Port does not do anything.
        /// </summary>
        SERIAL_MODE_UART_DETECT_BAUD_RATE = 2,

        /// <summary>
        /// On the UART, user can send commands and receive reponses
        /// at a predetermined, fixed baud rate.
        /// Command Port receives bytes from the RX line.
        /// TTL Port does not do anything.
        /// </summary>
        SERIAL_MODE_UART_FIXED_BAUD_RATE = 3,
    };

    /// <summary>
    /// The correspondence between errors and bits in the two-byte error register.
    /// For more details about what the errors mean, see the user's guide. 
    /// </summary>
    public enum uscError : byte
    {
        ERROR_SERIAL_SIGNAL = 0,
        ERROR_SERIAL_OVERRUN = 1,
        ERROR_SERIAL_BUFFER_FULL = 2,
        ERROR_SERIAL_CRC = 3,
        ERROR_SERIAL_PROTOCOL = 4,
        ERROR_SERIAL_TIMEOUT = 5,
        ERROR_SCRIPT_STACK = 6,
        ERROR_SCRIPT_CALL_STACK = 7,
        ERROR_SCRIPT_PROGRAM_COUNTER = 8,
    };

    public enum performanceFlag : byte
    {
        PERROR_ADVANCED_UPDATE = 0,
        PERROR_BASIC_UPDATE = 1,
        PERROR_PERIOD = 2
    };*/

   /* public class OnDeviceConnectedEventArgs
    {
        private Boolean isDeviceSuccessfullyConnected;
        private Windows.Devices.Enumeration.DeviceInformation deviceInformation;

        public OnDeviceConnectedEventArgs(Boolean isDeviceSuccessfullyConnected, Windows.Devices.Enumeration.DeviceInformation deviceInformation)
        {
            this.isDeviceSuccessfullyConnected = isDeviceSuccessfullyConnected;
            this.deviceInformation = deviceInformation;
        }

        public Boolean IsDeviceSuccessfullyConnected
        {
            get
            {
                return isDeviceSuccessfullyConnected;
            }
        }

        public Windows.Devices.Enumeration.DeviceInformation DeviceInformation
        {
            get
            {
                return deviceInformation;
            }
        }
    }

    public class LocalSettingKeys
    {
        public const String SyncBackgroundTaskStatus = "SyncBackgroundTaskStatus";
        public const String SyncBackgroundTaskResult = "SyncBackgroundTaskResult";
    }

    public class SyncBackgroundTaskInformation
    {
        public const String Name = "SyncBackgroundTask";
        public const String TaskEntryPoint = "BackgroundTask.IoSyncBackgroundTask";
        public const String TaskCanceled = "Canceled";
        public const String TaskCompleted = "Completed";
    }

    public class DeviceProperties
    {
        public const String DeviceInstanceId = "System.Devices.DeviceInstanceId";
    }

    public enum uscCommand : byte
    {
        REQUEST_GET_FIRMWARE = 0x06,
        REQUEST_GET_PARAMETER = 0x81,
        REQUEST_SET_PARAMETER = 0x82,
        REQUEST_GET_VARIABLES = 0x83,
        REQUEST_SET_SERVO_VARIABLE = 0x84,
        REQUEST_SET_TARGET = 0x85,
        REQUEST_CLEAR_ERRORS = 0x86,
        REQUEST_GET_SERVO_SETTINGS = 0x87,

        // GET STACK and GET CALL STACK are only used on the Mini Maestro.
        REQUEST_GET_STACK = 0x88,
        REQUEST_GET_CALL_STACK = 0x89,
        REQUEST_SET_PWM = 0x8A,

        REQUEST_REINITIALIZE = 0x90,
        REQUEST_ERASE_SCRIPT = 0xA0,
        REQUEST_WRITE_SCRIPT = 0xA1,
        REQUEST_SET_SCRIPT_DONE = 0xA2, // value.low.b is 0 for go, 1 for stop, 2 for single-step
        REQUEST_RESTART_SCRIPT_AT_SUBROUTINE = 0xA3,
        REQUEST_RESTART_SCRIPT_AT_SUBROUTINE_WITH_PARAMETER = 0xA4,
        REQUEST_RESTART_SCRIPT = 0xA5,
        REQUEST_START_BOOTLOADER = 0xFF
    }

    public enum uscParameter : byte
    {
        PARAMETER_INITIALIZED = 0, // 1 byte - 0 or 0xFF
        PARAMETER_SERVOS_AVAILABLE = 1, // 1 byte - 0-5
        PARAMETER_SERVO_PERIOD = 2, // 1 byte - ticks allocated to each servo/256
        PARAMETER_SERIAL_MODE = 3, // 1 byte unsigned value.  Valid values are SERIAL_MODE_*.  Init variable.
        PARAMETER_SERIAL_FIXED_BAUD_RATE = 4, // 2-byte unsigned value; 0 means autodetect.  Init parameter.
        PARAMETER_SERIAL_TIMEOUT = 6, // 2-byte unsigned value
        PARAMETER_SERIAL_ENABLE_CRC = 8, // 1 byte boolean value
        PARAMETER_SERIAL_NEVER_SUSPEND = 9, // 1 byte boolean value
        PARAMETER_SERIAL_DEVICE_NUMBER = 10, // 1 byte unsigned value, 0-127
        PARAMETER_SERIAL_BAUD_DETECT_TYPE = 11, // 1 byte value

        PARAMETER_IO_MASK_C = 16, // 1 byte - pins used for I/O instead of servo
        PARAMETER_OUTPUT_MASK_C = 17, // 1 byte - outputs that are enabled

        PARAMETER_CHANNEL_MODES_0_3 = 12, // 1 byte - channel modes 0-3
        PARAMETER_CHANNEL_MODES_4_7 = 13, // 1 byte - channel modes 4-7
        PARAMETER_CHANNEL_MODES_8_11 = 14, // 1 byte - channel modes 8-11
        PARAMETER_CHANNEL_MODES_12_15 = 15, // 1 byte - channel modes 12-15
        PARAMETER_CHANNEL_MODES_16_19 = 16, // 1 byte - channel modes 16-19
        PARAMETER_CHANNEL_MODES_20_23 = 17, // 1 byte - channel modes 20-23
        PARAMETER_MINI_MAESTRO_SERVO_PERIOD_L = 18, // servo period: 3-byte unsigned values, units of quarter microseconds
        PARAMETER_MINI_MAESTRO_SERVO_PERIOD_HU = 19,
        PARAMETER_ENABLE_PULLUPS = 21,  // 1 byte: 0 or 1
        PARAMETER_SCRIPT_CRC = 22, // 2 bytes - stores a checksum of the bytecode program, for comparison
        PARAMETER_SCRIPT_DONE = 24, // 1 byte - copied to scriptDone on startup
        PARAMETER_SERIAL_MINI_SSC_OFFSET = 25, // 1 byte (0-254)
        PARAMETER_SERVO_MULTIPLIER = 26, // 1 byte (0-255)

        // 9 * 24 = 216, so we can safely start at 30
        PARAMETER_SERVO0_HOME = 30, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO0_MIN = 32, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO0_MAX = 33, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO0_NEUTRAL = 34, // 2 byte neutral position
        PARAMETER_SERVO0_RANGE = 36, // 1 byte range
        PARAMETER_SERVO0_SPEED = 37, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO0_ACCELERATION = 38, // 1 byte (speed changes that much every 10ms)
        PARAMETER_SERVO1_HOME = 39, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO1_MIN = 41, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO1_MAX = 42, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO1_NEUTRAL = 43, // 2 byte neutral position
        PARAMETER_SERVO1_RANGE = 45, // 1 byte range
        PARAMETER_SERVO1_SPEED = 46, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO1_ACCELERATION = 47, // 1 byte (speed changes that much every 10ms)
        PARAMETER_SERVO2_HOME = 48, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO2_MIN = 50, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO2_MAX = 51, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO2_NEUTRAL = 52, // 2 byte neutral position
        PARAMETER_SERVO2_RANGE = 54, // 1 byte range
        PARAMETER_SERVO2_SPEED = 55, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO2_ACCELERATION = 56, // 1 byte (speed changes that much every 10ms)
        PARAMETER_SERVO3_HOME = 57, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO3_MIN = 59, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO3_MAX = 60, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO3_NEUTRAL = 61, // 2 byte neutral position
        PARAMETER_SERVO3_RANGE = 63, // 1 byte range
        PARAMETER_SERVO3_SPEED = 64, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO3_ACCELERATION = 65, // 1 byte (speed changes that much every 10ms)
        PARAMETER_SERVO4_HOME = 66, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO4_MIN = 68, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO4_MAX = 69, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO4_NEUTRAL = 70, // 2 byte neutral position
        PARAMETER_SERVO4_RANGE = 72, // 1 byte range
        PARAMETER_SERVO4_SPEED = 73, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO4_ACCELERATION = 74, // 1 byte (speed changes that much every 10ms)
        PARAMETER_SERVO5_HOME = 75, // 2 byte home position (0=off; 1=ignore)
        PARAMETER_SERVO5_MIN = 77, // 1 byte min allowed value (x2^6)
        PARAMETER_SERVO5_MAX = 78, // 1 byte max allowed value (x2^6)
        PARAMETER_SERVO5_NEUTRAL = 79, // 2 byte neutral position
        PARAMETER_SERVO5_RANGE = 81, // 1 byte range
        PARAMETER_SERVO5_SPEED = 82, // 1 byte (5 mantissa,3 exponent) us per 10ms
        PARAMETER_SERVO5_ACCELERATION = 83, // 1 byte (speed changes that much every 10ms)
    };


    public class MaestroDeviceListEntry
    {
        private DeviceInformation device;
        private String deviceSelector;



        public String InstanceId
        {
            get
            {

                return (String)device.Properties[DeviceProperties.DeviceInstanceId];
            }
        }

        public String Name
        {
            get
            {
                return (String)device.Name;
            }
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return device;
            }
        }

        public String DeviceSelector
        {
            get
            {
                return deviceSelector;
            }
        }

        /// <summary>
        /// The class is mainly used as a DeviceInformation wrapper so that the UI can bind to a list of these.
        /// </summary>
        /// <param name="deviceInformation"></param>
        /// <param name="deviceSelector">The AQS used to find this device</param>
        public MaestroDeviceListEntry(DeviceInformation deviceInformation, String deviceSelector)
        {
            device = deviceInformation;
            this.deviceSelector = deviceSelector;

        }

    }

    public class MaestroOldDevice
    {

        private UInt16 servoCount = 0;
        public const byte servoParameterBytes = 9;
        public const UInt16 DeviceVid = 0x1FFB;
        private UsbDevice usbDevice;
        private string msg;
        public static UInt16[] DevicePids = { (UInt16)0x0089, (UInt16)0x008A, (UInt16)0x008B, (UInt16)0x008C };
        public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");

        public delegate void OnMaestroUsbDeviceConnected(Object deviceObject);
        public event OnMaestroUsbDeviceConnected deviceConnectedCallback;

        public delegate void OnMaestroUsbDeviceError();
        public event OnMaestroUsbDeviceError deviceConnectErrorCallback;

        public delegate void OnMaestroUsbDeviceClosed();
        public event OnMaestroUsbDeviceClosed deviceClosedCallback;
        private DeviceInformation device;
        private String deviceSelector;


        public MaestroOldDevice()
        {

            // should start watchers here
        }


        public String InstanceId
        {
            get
            {

                return (String)device.Properties[DeviceProperties.DeviceInstanceId];
            }
        }

        public String Name
        {
            get
            {
                return (String)device.Name;
            }
        }

        public DeviceInformation DeviceInformation
        {
            get
            {
                return device;
            }
        }

        public String DeviceSelector
        {
            get
            {
                return deviceSelector;
            }
        }

        public MaestroDevice Device
        {
            get
            {
                return maestro;
            }
        }



        private Range getRange(uscParameter parameterId)
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

        public struct Range
        {
            public Byte bytes;
            public Int32 minimumValue;
            public Int32 maximumValue;

            public Range(Byte bytes, Int32 minimumValue, Int32 maximumValue)
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

            public static Range u32 = new Range(4, 0, 0x7FFFFFFF);
            public static Range u16 = new Range(2, 0, 0xFFFF);
            public static Range u12 = new Range(2, 0, 0x0FFF);
            public static Range u10 = new Range(2, 0, 0x03FF);
            public static Range u8 = new Range(1, 0, 0xFF);
            public static Range u7 = new Range(1, 0, 0x7F);
            public static Range boolean = new Range(1, 0, 1);
        }



        public class Pipe
        {
            // for now only one set here but will add second later
            public const UInt32 BulkInPipeIndex = 0;
            public const UInt32 BulkOutPipeIndex = 0;
        }

        public string statusMsg
        {
            get
            {
                return msg;
            }
        }

        public UsbDevice maestroDevice
        {
            get
            {
                return maestroDevice;
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

        public UInt16 getChannelCount()
        {

            UInt16 id = (UInt16)usbDevice.DeviceDescriptor.ProductId;
            // Determine the number of servos from the product id.
            switch (id)
            {
                case 0x89: servoCount = 6; break;
                case 0x8A: servoCount = 12; break;
                case 0x8B: servoCount = 18; break;
                case 0x8C: servoCount = 24; break;
                default: throw new Exception("Unknown product id " + id.ToString("x2") + ".");
            }
            return servoCount;
        }

        public static uscParameter getServoParameter(uscParameter p, byte servo)
        {
            return (uscParameter)((Byte)p + servo * servoParameterBytes);
        }


        protected bool microMaestro
        {
            get
            {
                return servoCount == 6;
            }
        }



        public async Task SetMaestroServoTargetAsync(byte channel, UInt16 Target)
        {

            byte target = (byte)((Target * 4) / 64);
            await setRawParameterNoChecks((byte)uscCommand.REQUEST_SET_TARGET, target, channel);
        }


        // get the servo minimum from a servo
        // param channel the channel number to get
        public async Task<UInt16> GetMaestroServoMinAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_MIN, channel);
            Range range = getRange(param);
            UInt16 minMicroSeconds = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    minMicroSeconds = (UInt16)(reader.ReadByte() / 4 * 64);
                }
                else
                {
                    minMicroSeconds = 0xFFFF; // if error set to FF
                }
            }
            return minMicroSeconds;

        }



        public async Task SetMaestroServoMinAsync(byte channel, UInt16 value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_MIN, channel);
            byte min = (byte)((value * 4) / 64);

            await SendVendorControlTransferParameterOutAsync(param, min);
        }

        public async Task<UInt16> GetMaestroServoNuetralAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_NEUTRAL, channel);
            Range range = getRange(param);
            UInt16 nuetralMicroSeconds = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    nuetralMicroSeconds = (UInt16)(reader.ReadByte() / 4 * 64);
                }
                else
                {
                    nuetralMicroSeconds = 0xFFFF; // if error set to FF
                }
            }
            return nuetralMicroSeconds;

        }

        public async Task SetMaestroServoNeutralAsync(byte channel, UInt16 value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_NEUTRAL, channel);
            byte neutral = (byte)((value * 4) / 64);

            await SendVendorControlTransferParameterOutAsync(param, neutral);
        }

        public async Task<UInt16> GetMaestroServoRangeAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_RANGE, channel);
            Range range = getRange(param);
            UInt16 servorange = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    servorange = (UInt16)(reader.ReadByte() / 4 * 127);
                }
                else
                {
                    servorange = 0xFFFF; // if error set to FF
                }
            }
            return servorange;

        }


        public async Task SetMaestroServoRangeAsync(byte channel, UInt16 value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_RANGE, channel);
            byte servorange = (byte)((value * 4) / 127);

            await SendVendorControlTransferParameterOutAsync(param, servorange);
        }

        public async Task<byte> GetMaestroServoAccelerationAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_ACCELERATION, channel);
            Range range = getRange(param);
            byte acceleration = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    acceleration = reader.ReadByte();
                }
                else
                {
                    acceleration = 0x00; // if error set to FF
                }
            }
            return acceleration;

        }


        public async Task SetMaestroServoAccelerationAsync(byte channel, byte value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_ACCELERATION, channel);
            await SendVendorControlTransferParameterOutAsync(param, value);
        }


        public async Task<byte> GetMaestroServoHomeEnabledAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_HOME, channel);
            Range range = getRange(param);
            byte homeenabled = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    homeenabled = reader.ReadByte();
                }
                else
                {
                    homeenabled = 0x00; // if error set to FF
                }
            }
            return homeenabled;

        }

        public async Task SetMaestroServoHomeEnabledAsync(byte channel, byte value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_HOME, channel);
            await SendVendorControlTransferParameterOutAsync(param, value);
        }

        public async Task<byte> GetMaestroServoSpeedAsync(byte channel)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_SPEED, channel);
            Range range = getRange(param);
            byte speed = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    speed = reader.ReadByte();
                }
                else
                {
                    speed = 0x00; // if error set to FF
                }
            }
            return speed;

        }

        public async Task SetMaestroServoSpeedAsync(byte channel, byte speed)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_SPEED, channel);
            await SendVendorControlTransferParameterOutAsync(param, speed);
        }


        // return max value for servo in us
        public async Task<UInt16> GetMaestroServoMaxAsync(byte channel)
        {
            // We expect to receive 1 byte of data with our control transfer, which is the state of the seven segment
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_MAX, channel);
            Range range = getRange(param);
            UInt16 maxMicroSeconds = 0;

            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            if (dataBuffer.Length == range.bytes)
            {
                DataReader reader = DataReader.FromBuffer(dataBuffer);

                if (range.bytes == 1)
                {
                    // read a byte 

                    maxMicroSeconds = (UInt16)(reader.ReadByte() / 4 * 64);
                }
                else
                {
                    maxMicroSeconds = 0xFFFF; // if error set to FF
                }
            }
            return maxMicroSeconds;

        }

        public async Task SetMaestroServoMaxAsync(byte channel, UInt16 value)
        {
            uscParameter param = getServoParameter(uscParameter.PARAMETER_SERVO0_MAX, channel);
            Range range = getRange(param);
            byte maxUnits = (byte)((value / 64) * 4);

            await SendVendorControlTransferParameterOutAsync(param, maxUnits);
        }

        private async Task<IBuffer> GetMaestroServoParamDataAsync(uscParameter uscparam, byte channel)
        {
            // We expect to receive 1 byte of data with our control transfer, which is the state of the seven segment
            uscParameter param = getServoParameter(uscparam, channel);
            Range range = getRange(param);
            // Send the request
            IBuffer dataBuffer = await SendVendorControlTransferParameterInAsync(param);

            return dataBuffer;
        }



        private async Task<IBuffer> SendVendorControlTransferInToDeviceRecipientAsync(Byte vendorCommand, UInt16 value, UInt16 index, UInt32 dataPacketLength)
        {
            try
            {
                // Data will be written to this buffer when we receive it
                var buffer = new Windows.Storage.Streams.Buffer(dataPacketLength);

                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)vendorCommand,
                    Value = value,
                    Index = index,
                    Length = dataPacketLength
                };

                IBuffer retbuffer = await usbDevice.SendControlInTransferAsync(setupPacket, buffer);

                return retbuffer;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return new Windows.Storage.Streams.Buffer(dataPacketLength + 2);  // we return a buffer with the wrong size it will then be ignored higher up

            }
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
                    Request = (byte)uscCommand.REQUEST_GET_PARAMETER,
                    Value = value,
                    Index = (byte)parameter,
                    Length = range.bytes
                };
                return await usbDevice.SendControlInTransferAsync(setupPacket, buffer);
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                // todo fix this kludge 
                return new Windows.Storage.Streams.Buffer(7);
            }
        }


        private async Task SendVendorControlTransferParameterOutAsync(uscParameter parameter, ushort value)
        {
            try
            {
                Range range = getRange(parameter);

                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.Out,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscCommand.REQUEST_SET_PARAMETER,
                    Value = value,
                    Index = (byte)parameter,
                    Length = 0
                };


                await usbDevice.SendControlOutTransferAsync(setupPacket);
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
            }
        }


        private async Task<string> setRawParameterNoChecks(byte parameter, ushort value, byte index)
        {

            try
            {
                UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.Out,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor
                    },
                    Request = parameter,
                    Index = index,
                    Value = value,
                    Length = 0
                };

                UInt32 bytesTransferred = await usbDevice.SendControlOutTransferAsync(setupPacket);
                msg = "Parameter set to " + value.ToString();
            }
            catch (Exception e)
            {
                Debug.WriteLine("oops" + e.Message);
                msg = "oops";
            }
            return (msg);
        }


        // manually open first meastro device
        public async Task<Boolean> OpenFirstDevice()
        {


            string aqs = UsbDevice.GetDeviceSelector(DeviceInterfaceClass);

            var maestroDevices = await DeviceInformation.FindAllAsync(aqs);

            try
            {

                usbDevice = await UsbDevice.FromIdAsync(maestroDevices[0].Id);
                device = maestroDevices[0];
                deviceSelector = aqs;
                return true;
            }
            catch (Exception exception)
            {
                msg = exception.Message.ToString();
                return false;
            }


        }

        public async Task<Boolean> OpenDeviceAsync(DeviceInformation deviceInfo, String deviceSelector)
        {
            Boolean successfullyOpenedDevice = false;
            try
            {

                usbDevice = await UsbDevice.FromIdAsync(deviceInfo.Id);

                // Device could have been blocked by user or the device has already been opened by another app.
                if (usbDevice != null)
                {

                    // Notify registered callback handle that the device has been opened
                    if (deviceConnectedCallback != null)
                    {
                        deviceConnectedCallback(usbDevice);
                    }
                    successfullyOpenedDevice = true;



                }
                else
                {
                    if (deviceConnectErrorCallback != null)
                    {
                        deviceConnectErrorCallback();
                    }
                }


            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

            }
            return successfullyOpenedDevice;
        }

        public void CloseConnectedDevice()
        {
            if (usbDevice != null)
            {
                usbDevice.Dispose();
                usbDevice = null;
                if (deviceClosedCallback != null)
                {
                    deviceClosedCallback();
                }
            }
        }
    }


     public class MaestroDevices
      {
          private Collection<Maestro> maestroDevices;

          public delegate void OnMaestroRemoved(DeviceInformationUpdate deviceInfo);
          public event OnMaestroRemoved deviceRemovedCallback;

          public delegate void OnMaestroAdded(DeviceInformation deviceInfo, string deviceSelector);
          public event OnMaestroAdded deviceAddedCallback;

          public delegate void OnMaestroStopped(Object deviceObject);
          public event OnMaestroStopped deviceStoppedCallback;

          public delegate void OnMaestroEnumerated(Object deviceObject);
          public event OnMaestroEnumerated deviceEnumeratedCallBack;

          public static Guid DeviceInterfaceClass = new Guid("{e0fbe39f-7670-4db6-9b1a-1dfb141014a7}");






          public MaestroDevices()
          {
              maestroDevices = new Collection<Maestro>();
              OpenDeviceWatcher();
          }

          public void OpenDeviceWatcher()
          {
              string aqs = UsbDevice.GetDeviceSelector(DeviceInterfaceClass);

              var maestroWatcher = DeviceInformation.CreateWatcher(aqs);

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

              if (deviceRemovedCallback != null)
              {

                  deviceRemovedCallback(args);
              }
          }

           private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
           {
               if ((args != null) && (deviceInfo.Id == deviceInformation.Id) && !IsDeviceConnected && isEnabledAutoReconnect)
               {
                    await OpenDeviceAsync(deviceInformation, deviceSelector);

                           // Any app specific device intialization should be done here because we don't know the state of the device when it is re-enumerated.

               }
               if (deviceAddedCallback != null)
               {
                   deviceAddedCallback(args, args.Id);

               }
      }


          private void OnDeviceStopped(DeviceWatcher sender, object args)
          {
              if (deviceStoppedCallback != null)
              {
                  deviceStoppedCallback(args);
              }
          }

          private void OnDeviceEnumerationComplete(DeviceWatcher sender, Object args)
          {
              if (deviceEnumeratedCallBack != null)
              {
                  deviceEnumeratedCallBack(args);
              }
          }

      }*/

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
           // Range range = getRange(parameter);
            ushort value = 0;
            try
            {
                /*UsbSetupPacket setupPacket = new UsbSetupPacket
                {
                    RequestType = new UsbControlRequestType
                    {
                        Direction = UsbTransferDirection.In,
                        Recipient = UsbControlRecipient.Device,
                        ControlTransferType = UsbControlTransferType.Vendor,
                    },
                    Request = (byte)uscRequest.REQUEST_GET_PARAMETER,
                    Value = value,
                    Index = (UInt16) parameter,
                    Length = range.bytes
                };
                */
                IBuffer retbuffer = await SendVendorControlTransferParameterInAsync(parameter);
                //IBuffer retbuffer = await maestroDevice.device.SendControlInTransferAsync(setupPacket);
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
