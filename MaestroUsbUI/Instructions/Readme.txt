# MaestroDriver and usb Library
# Please Read Fully before Coding
Pololu maestro Usb driver for raspberry Pi 3 Windows 10 Iot

Library Functions

  SetTarget(Channel,Targetus)
  SetSpeed(Channel,Speed)
  SetAcceleration(Channel,Acceleration)

settings

SERIAL_MODE
SERIAL_FIXED_BAUD_RATE
SERIAL_ENABLE_CRC
SERIAL_NEVER_SUSPEND
SERIAL_DEVICE_NUMBER
SERIAL_MINI_SSC_OFFSET
SERIAL_TIMEOUT
SCRIPT_DONE
SERVOS_AVAILABLE
SERVO_PERIOD    6channel device
MINI_MAESTRO_SERVO_PERIOD_L
MINI_MAESTRO_SERVO_PERIOD_HU
SERVO_MULTIPLIER
ENABLE_PULLUPS   >= 18Channel
IO_MASK_C       micro only
OUTPUT_MASK_C   micro only
CHANNEL_MODES_0_3 mini
plus 
Servo_Home
Servo_Min
Servo_Max
Servo_Nuetral
Servo_Range
Servo_Speed
Servo_Paramter


Installation 
Before you can use the maestro in Usb mode you have install and assign winusb drivers.

NOTE in the latest versions of windows 10 IOT 
you must start by going into the device portal
then choose Devices
and change Default Controller Driver to
Direct Memory Mapped Driver

step 1 open powershell with administrator access (right click powershell run as administrator)
step 2 type

	   net start WinRM<CR>    <CR> = carraige return key 
	   
step 3 add your pi board to to windows trusts with the following replace xxx.xxx.xxx.xxx with the boards ip address

	   Set-Item WSMan:\localhost\Client\TrustedHosts -Value xxx.xxx.xxx.xxx<CR>
	   Hit enter to accept default option of Yes
	   
step 4 connect to the board replacing the xxx.xxx.xxx.xxx with your ip address

	enter-PSSession -ComputerName xxx.xxx.xxx.xxx -Credential Administrator<>
	   
	enter your boards admin password if you haven't changed it the default is p@ssw0rd
	   it will then connect to your pi may take a minute
step 5 make sure you have plugged your maestro board in then type
	   devcon status 'USB\VID_1FFB*'<CR> 
 
	you should now see the info for your Maestro board something like this depending on which meastro your running

USB\VID_1FFB&PID_008A&MI_04\6&3B96929&0&0004
	Name: Pololu Mini Maestro 12-Channel USB Servo Controller
	The device has the following problem: 28			<------ note the device has a problem 
	Problem status: 0xC0000490
USB\VID_1FFB&PID_008A&MI_02\6&3B96929&0&0002
	Name: USB Serial Device
	Driver is running.
USB\VID_1FFB&PID_008A\00137654                      <----- note this line of text as it is needed later
	Name: USB Composite Device
	Driver is running.
USB\VID_1FFB&PID_008A&MI_00\6&3B96929&0&0000
	Name: USB Serial Device
	Driver is running.
	
Step 6
to fix the problem an get winusb installed for the meastro device 
in windows file explorer type \\xxx.xxx.xxx.xxx\c$ to open up your boards shared directory you will be prompted for the user and password 
user administrator password your boards administrator password default is p@ssw0rd

you will now see a list of directorys 
navigate to users\public         you can probably put them anywhere but seemed like a good place to put them
now copy the pololu.inf and pololu_usb_to_serial.inf located in the MasestroUsbUi\instructions directory above to your board

now type cd \users\public<CR>

step 7 install the driver by typeing the following
	 devcon install pololu.inf USB\VID_1FFBPID_00XX\0000000<CR>
	 replacing the XX with your board model and 0000000 with the number at the end of the line you noted in the previuos step.
step 8 recycle power to the board and then repeat steps 4 and 5 you should now see your board no longer has the error

USB\VID_1FFB&PID_008A&MI_04\6&3B96929&0&0004
	Name: Pololu Mini Maestro 12-Channel USB Servo Controller
	Driver is running.
USB\VID_1FFB&PID_008A&MI_02\6&3B96929&0&0002
	Name: USB Serial Device
	Driver is running.
USB\VID_1FFB&PID_008A\00137654                      
	Name: USB Composite Device
	Driver is running.
USB\VID_1FFB&PID_008A&MI_00\6&3B96929&0&0000
	Name: USB Serial Device
	Driver is running.

your now ready to start using the maestro library in your app

after starting a new Windows Iot or Uwp App you must add the following to 
Package.appxmanifest file open it in text mode right click open with Xaml Text Editor
add the following in the <Capabilities> section

<DeviceCapability Name="usb">
	  <!--Maestro Device-->
	  <Device Id="vidpid:1FFB 008A">
		<Function Type="name:vendorSpecific" />
	  </Device>
	  <!--Maestro Device-->
	  <Device Id="vidpid:1FFB 008B">
		<Function Type="name:vendorSpecific" />
	  </Device>
	  <!--Maestro Device-->
	  <Device Id="vidpid:1FFB 008C">
		<Function Type="name:vendorSpecific" />
	  </Device>
	  <!--Maestro Device-->
	  <Device Id="vidpid:1FFB 0089">
		<Function Type="name:vendorSpecific" />
	  </Device>
  </DeviceCapability> 

  you will also need to have bytecode.dll added to your project referances which is part of the Pololu Sdk available here 
  https://www.pololu.com/file/download/pololu-usb-sdk-140604.zip?file_id=0J765
