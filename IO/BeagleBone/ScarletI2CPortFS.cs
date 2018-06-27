using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// +------------------------------------------------------------------------------------------------------------------------------+
/// |                                                   TERMS OF USE: MIT License                                                  |
/// +------------------------------------------------------------------------------------------------------------------------------|
/// |Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation    |
/// |files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,    |
/// |modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software|
/// |is furnished to do so, subject to the following conditions:                                                                   |
/// |                                                                                                                              |
/// |The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.|
/// |                                                                                                                              |
/// |THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE          |
/// |WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR         |
/// |COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,   |
/// |ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                         |
/// +------------------------------------------------------------------------------------------------------------------------------+

namespace BBBCSIO
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// Provides the I2C Input/Output Port functionality for a BeagleBone Black
    /// This is the SYSFS version
    /// 
    /// Be aware that you need to ensure the I2C port is configured in the Device
    /// Tree before this code will work.
    /// 
    /// NOTE that you can use the i2c-tools package to probe the BBB for all available
    ///   I2C ports and also to probe for the addresses of the devices addresses
    ///   connected to a particular I2C port.
    /// 
    /// </summary>
    /// <history>
    ///    22 Jan 15  Cynic - Originally written
    ///    20 Jan 18  Cai B - Added error code output for I/O errors
    ///                       Lines 45, 78, 137, 141, 353 edited.
    /// </history>
    // CaiB 2018-01-20: Renamed class
    // public class I2CPortFS : PortFS
    public class ScarletI2CPortFS : PortFS
    {

        // the I2C port we use
        private I2CPortEnum i2cPort = I2CPortEnum.I2CPORT_NONE;

        // used for external file open calls
        const int O_RDONLY = 0x0;
        const int O_WRONLY = 0x1;
        const int O_RDWR = 0x2;
        const int O_NONBLOCK = 0x0004;

        // the file descriptor for the open i2c port
        int i2CPortFD = -1;

        // these magic numbers are defined by the ioctl driver 
        // to tell it what to do. Each byte and nibble has meaning and it is generally
        // built on the fly in a C program by a macro which shifts flags about and OR's
        // them together. I have not attempted to reproduce this build here and simply
        // use the end result since the resulting value is essentially constant for
        // an particular ioctl call to a specific driver type
        uint I2C_DEV = 0x00000703;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="i2cPortIn">The I2C port we use</param>
        /// <history>
        ///    22 Jan 15  Cynic - Originally written
        /// </history>
        // CaiB 2018-01-20: Renamed class and constructor.
        // public I2CPortFS(I2CPortEnum i2cPortIn) : base(GpioEnum.GPIO_NONE)
        public ScarletI2CPortFS(I2CPortEnum i2cPortIn) : base(GpioEnum.GPIO_NONE)
        {
            i2cPort = i2cPortIn;
            //Console.WriteLine("I2CPort Starts");
            // open the port
            OpenPort ();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Writes a buffer out to an I2C Device. 
        /// 
        /// NOTE: 
        ///   The address of the device is specified by the devID in the i2cPort.Write or
        ///   i2cPort.Read calls. You do not need to include the address as the first outgoing
        ///   byte in the txByteBuf or set the READ/WRITE bit in that address. The I2C driver does
        ///   that and sends it for you automatically.
        ///
        /// </summary>
        /// <param name="devID">The I2C Device ID to write to</param>
        /// <param name="txByteBuf">The buffer with bytes to write</param>
        /// <param name="numBytes">The number of bytes to send
        /// <history>
        ///    22 Jan 15  Cynic - Originally written
        /// </history>
        public void Write(int devID, byte[] txByteBuf, int numBytes)
        {
            int ioctlRetVal = -1;

            // sanity check
            if (txByteBuf == null)
            {
                throw new Exception ("Null tx buffer");
            }
            if (numBytes <= 0)
            {
                throw new Exception ("numBytes <= 0");
            }
            if (i2CPortFD <= 0)
            {
                throw new Exception ("I2C port is not open, fd=0");
            }
            if (PortIsOpen == false)
            {
                throw new Exception ("I2C port is not open");
            }
                           
            // we set the slave device we wish to write to using an ioctl
            // this is an external call to the libc.so.6 library
            ioctlRetVal = ExternalIoCtl(i2CPortFD, I2C_DEV, devID);
            if (ioctlRetVal < 0)
            {
                throw new Exception ("Error setting I2C device 0x" + devID.ToString("x4") + ", ioctlRetVal=" + ioctlRetVal.ToString());
            }

            // the write is an external write to the device file
            int numWritten = ExternalWrite(i2CPortFD, txByteBuf, numBytes);
            if (numWritten != numBytes)
            {
                int ErrorCode = Marshal.GetLastWin32Error(); // CaiB 2018-01-20: Added

                // CaiB 2018-01-20: Added error code output and nicer output formatting
                // throw new Exception ("Error writing to I2C device 0x" + devID.ToString("x4") + ", numWritten != numBytes" + numWritten.ToString() + " != " + numBytes.ToString());
                throw new Exception ("Error writing to I2C device 0x" + devID.ToString("x4") + ", numWritten != numBytes: " + numWritten.ToString() + " != " + numBytes.ToString() + ". Error code " + ErrorCode);
            }
        }
       
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Reads a buffer in from an I2C Device. 
        /// 
        /// NOTE: 
        ///   The address of the device is specified by the devID in the i2cPort.Write or
        ///   i2cPort.Read calls. You do not need to include the address as the first outgoing
        ///   byte in the rxByteBuf or set the READ/WRITE bit in that address. The I2C driver does
        ///   that and sends it for you automatically.
        ///   
        /// </summary>
        /// <param name="devID">The I2C Device ID to write to</param>
        /// <param name="rxByteBuf">The buffer with bytes to read</param>
        /// <param name="numBytes">The number of bytes to read
        /// <history>
        ///    22 Jan 15  Cynic - Originally written
        /// </history>
        public void Read(int devID, byte[] rxByteBuf, int numBytes)
        {
            int ioctlRetVal = -1;

            // sanity check
            if (rxByteBuf == null)
            {
                throw new Exception ("Null rx buffer");
            }
            if (numBytes <= 0)
            {
                throw new Exception ("numBytes <= 0");
            }
            if (i2CPortFD <= 0)
            {
                throw new Exception ("I2C port is not open, fd=0");
            }
            if (PortIsOpen == false)
            {
                throw new Exception ("I2C port is not open");
            }

            // we set the slave device we wish to read from using an ioctl
            // this is an external call to the libc.so.6 library
            ioctlRetVal = ExternalIoCtl(i2CPortFD, I2C_DEV, devID);
            if (ioctlRetVal < 0)
            {
                throw new Exception ("Error setting I2C device 0x" + devID.ToString("x4") + ", ioctlRetVal=" + ioctlRetVal.ToString());
            }

            // the read is an external read to the device file
            int numRead = ExternalRead(i2CPortFD, rxByteBuf, numBytes);
            if (numRead != numBytes)
            {
                throw new Exception ("Error reading to I2C device 0x" + devID.ToString("x4") + ", numRead != numBytes" + numRead.ToString() + " != " + numBytes.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the port. Throws an exception on failure
        /// 
        /// </summary>
        /// <history>
        ///    22 Jan 15 Cynic - Originally written
        /// </history>
        protected override void OpenPort()
        {
            string deviceFileName;
            // set up now
            deviceFileName = BBBDefinitions.I2CDEV_FILENAME;

            // set up the i2c port number
            if (I2CPort == I2CPortEnum.I2CPORT_0)
            {
                deviceFileName = deviceFileName.Replace("%port%", "0");
            }
            else if (I2CPort == I2CPortEnum.I2CPORT_1)
            {
                deviceFileName = deviceFileName.Replace("%port%", "1");
            }
            else if (I2CPort == I2CPortEnum.I2CPORT_2)
            {
                deviceFileName = deviceFileName.Replace("%port%", "2");
            }
            else
            {
                // should never happen
                throw new Exception ("Unknown I2C Port:" + I2CPort.ToString());
            }
                
            // we open the file. We have to have an open file descriptor
            // note this is an external call. It has to be because the 
            // ioctl needs an open file descriptor it can use
            i2CPortFD = ExternalFileOpen(deviceFileName, O_RDWR|O_NONBLOCK);
            if(i2CPortFD <= 0)
            {
                throw new Exception("Could not open i2c device file:" + deviceFileName);
            }
            portIsOpen = true;

            //   Console.WriteLine("I2CPort Port Device Enabled: "+ deviceFileName);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Closes the port. 
        /// 
        /// </summary>
        /// <history>
        ///    22 Jan 15 Cynic - Originally written
        /// </history>
        public override void ClosePort()
        {
            //Console.WriteLine("I2CPort Closing");
            if (i2CPortFD >= 0)
            {
                // do an external close
                ExternalFileClose(i2CPortFD);
            }
            portIsOpen = false;
            i2CPortFD = -1;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the I2C Port. There is no Set accessor this is set in the constructor
        /// </summary>
        /// <history>
        ///    22 Jan 15 Cynic - Originally written
        /// </history>
        public I2CPortEnum I2CPort
        {
            get
            {
                return i2cPort;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the PortDirection
        /// </summary>
        /// <history>
        ///    22 Jan 15 Cynic - Originally written
        /// </history>
        public override PortDirectionEnum PortDirection()
        {
            return PortDirectionEnum.PORTDIR_INPUTOUTPUT;
        }           

        // #########################################################################
        // ### Dispose Code
        // #########################################################################
        #region

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Implement IDisposable. 
        /// Dispose(bool disposing) executes in two distinct scenarios. 
        /// 
        ///    If disposing equals true, the method has been called directly 
        ///    or indirectly by a user's code. Managed and unmanaged resources 
        ///    can be disposed.
        ///  
        ///    If disposing equals false, the method has been called by the 
        ///    runtime from inside the finalizer and you should not reference 
        ///    other objects. Only unmanaged resources can be disposed. 
        /// 
        ///  see: http://msdn.microsoft.com/en-us/library/system.idisposable.dispose%28v=vs.110%29.aspx
        /// 
        /// </summary>
        /// <history>
        ///    28 Aug 14 Cynic - Originally written
        /// </history>
        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if(Disposed==false)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if(disposing==true)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here. If disposing is false, 
                // only the following code is executed.

                // Clean up our code
                //Console.WriteLine("Disposing I2CPORT");
         
                // call the base to dispose there
                base.Dispose(disposing);

            }
        }
        #endregion

        // #########################################################################
        // ### External Library Calls
        // #########################################################################
        #region External Library Calls

        // these calls are in the libc.so.6 library. We can just say "libc" and mono
        // will figure out which libc.so is the latest version and use that.

        // CaiB 2018-01-20: Added SetLastError.
        // [DllImport("libc", EntryPoint = "write")]
        [DllImport("libc", EntryPoint = "write", SetLastError = true)]
        static extern int ExternalWrite(int fd, byte[] outBuf, int numBytes);

        [DllImport("libc", EntryPoint = "read")]
        static extern int ExternalRead(int fd, byte[] inBuf, int numBytes);

        [DllImport("libc", EntryPoint = "ioctl")]
        static extern int ExternalIoCtl(int fd, uint request, int intVal);

        [DllImport("libc", EntryPoint = "open")]
        static extern int ExternalFileOpen(string path, int flags);

        [DllImport("libc", EntryPoint = "close")]
        static extern int ExternalFileClose(int fd);

        #endregion
    }
}

