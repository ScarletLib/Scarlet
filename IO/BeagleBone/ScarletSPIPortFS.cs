﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Scarlet.IO;

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
    /// Provides the SPI Master Input/Output Port functionality for a BeagleBone Black
    /// This is the SPIDEV version
    /// 
    /// Be aware that you need to ensure the SPI port is configured in the Device
    /// Tree before this code will work.
    /// 
    /// Also NOTE that you cannot use SPIPORT_1 without disabling the HDMI device
    /// as they use some of the same pins.
    /// 
    /// Also NOTE that if you use the GPIO based slave select lines you cannot
    ///   also connect anything to the SPI devices internal CS0 or CS1 slave select
    ///   lines. These will always be activated on every write irregardless
    ///   of the GPIO based slave select also asserted. This is just the way the
    ///   SPIDev driver works.
    /// 
    /// </summary>
    /// <history>
    ///    16 Sep 17  Cai Biesinger - Modified for Scarlet:
    ///     - Removed native CS pin usage features, as we don't need this.
    ///     - Switched from using pins to IDigitalOut to better align with Scarlet's structure.
    ///     - Fixed bug that caused CS line to not be returned to high after communication finished.
    ///    21 Dec 14  Cynic - Originally written
    /// </history>
    public class ScarletSPIPortFS : PortFS
    {

        // the SPI port we use
        private SPIPortEnum spiPort = SPIPortEnum.SPIPORT_NONE;

        // the open slave devices we have created
        //List<SPISlaveDeviceHandle> openSlaveDevices = new List<SPISlaveDeviceHandle>();
        SPISlaveDeviceHandle PortDevice;
        List<IDigitalOut> SlaveDevices = new List<IDigitalOut>();

        // used for external file open calls
        const int O_RDONLY = 0x0;
        const int O_WRONLY = 0x1;
        const int O_RDWR = 0x2;
        const int O_NONBLOCK = 0x0004;

        // these magic numbers are defined by the Ioctl driver (spidev in this case)
        // to tell it what to do. Each byte and nibble has meaning and it is generally
        // built on the fly in a C program by a macro which shifts flags about and OR's
        // them together. I have not attempted to reproduce this build here and simply
        // use the end result since the resulting value is essentially constant for
        // an particular ioctl call to a specific driver type
        uint SPI_IOC_WR_MODE = 0x40016b01;
        uint SPI_IOC_RD_MODE = 0x80016b01;
        uint SPI_IOC_WR_MAX_SPEED_HZ=0x40046b04;
        uint SPI_IOC_RD_MAX_SPEED_HZ = 0x80046b04;
        uint SPI_IOC_MESSAGE_1 = 0x40206B00;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spiPortIn">The SPI port we use</param>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Renamed
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public ScarletSPIPortFS(SPIPortEnum spiPortIn) : base(GpioEnum.GPIO_NONE)
        {
            spiPort = spiPortIn;
            PortDevice = EnableSPIDevice();
            // NOTE the port is not opened here as per the usual manner. Because
            // each SPI dev device represents a distinct Slave select, we open the
            // spidev file on the EnableSPISlaveDevice call();
            //Console.WriteLine("SPIPort Starts");
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Writes/Reads a buffer out/in to/from an SPI Slave Device. 
        /// 
        /// Note that the TO/FROM's and IN/OUT's in the above line are there because
        /// the SPI protocol always reads a byte for every byte sent. If you send a
        /// byte you get a byte. If you do not sent a byte you will never receive
        /// a byte since this code operates as an SPI Master. Thus if you only
        /// wish to receive you must send an equivalent number of bytes. The
        /// bytes you send are determined by the Slave. Sometimes this is just
        /// 0x00 and sometimes it represents an address to read - exactly what you,
        /// send is entirely slave device implementation dependent. 
        /// 
        /// If you only wish to transmit, not receive, just use NULL for your
        /// rxBuffer. The SPI port will still receive, of course, but you will
        /// not be bothered by it.
        /// 
        /// NOTE: the Slave Select is set when you opened the port. The rule is 
        /// one slave to one SPISlaveDeviceHandle.
        /// 
        /// </summary>
        /// <param name="ssHandle">The SPI Slave Device handle to write to</param>
        /// <param name="txByteBuf">The buffer with bytes to write</param>
        /// <param name="rxByteBuf">The buffer with bytes to receive. Can be NULL</param>
        /// <param name="numBytes">The number of bytes to send/receive
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: switching from file descriptor GPIO to IDigitalOut.
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public void SPITransfer(IDigitalOut output, byte[] txByteBuf, byte[] rxByteBuf, int numBytes)
        {
            int spiFileDescriptor = -1;
            int ioctlRetVal = -1;

            // sanity check
            if (output == null) { throw new Exception("Null IDigitalOut object"); }
            if (txByteBuf == null) { throw new Exception ("Null tx buffer"); }
            if (numBytes <= 0) { throw new Exception ("numBytes <= 0"); }

            // set up our file descriptor

            // use the descriptor of the first non-GPIO slave select 
            // we find.

            // get first slave device. We need the file descriptor
            SPISlaveDeviceHandle firstHandle = PortDevice;
            // sanity check
            if (firstHandle == null) { throw new Exception("At least one non GPIO Slave Device must be enabled."); }
            // use this descriptor
            spiFileDescriptor = firstHandle.SpiDevFileDescriptor;

            // the data needs to be in unmanaged global memory
            // so the spidev driver can see it. This allocates
            // that memory. We MUST release this!
            IntPtr txBufPtr = Marshal.AllocHGlobal(numBytes+1);
            IntPtr rxBufPtr = Marshal.AllocHGlobal(numBytes+1);

            try
            {
                // copy the data from the tx buffer to our pointer               
                Marshal.Copy(txByteBuf, 0, txBufPtr, numBytes);

                // create and fill in the contents of our transfer struct
                spi_ioc_transfer xfer = new spi_ioc_transfer();
                xfer.tx_buf = txBufPtr;
                xfer.rx_buf = rxBufPtr;
                xfer.len = (UInt32)numBytes;
                xfer.speed_hz = 0;//(UInt16)ssHandle.SpeedInHz;
                xfer.delay_usecs = 0;// ssHandle.DelayUSecs;
                xfer.bits_per_word = 0;// ssHandle.BitsPerWord;
                xfer.cs_change = 0;// ssHandle.CSChange;
                xfer.pad = 0;

                    // lower the slave select
                    output.SetOutput(false);
                    try
                    {
                        // this is an external call to the libc.so.6 library
                        ioctlRetVal = ExternalIoCtl(spiFileDescriptor, SPI_IOC_MESSAGE_1, ref xfer);
                    }
                    finally
                    {
                        // raise the slave select
                        // CaiB 2017-09-16: Chenged this to true, BBBCSIO never released SS.
                        output.SetOutput(true);
                    }

                // did the call succeed?
                if(ioctlRetVal < 0)
                {
                    // it failed
                    throw new Exception("ExternalIoCtl on device " + output + " failed. retval="+ioctlRetVal.ToString());
                }

                // did the caller supply a receive buffer
                if(rxByteBuf!=null)
                {
                    // yes they did, copy the returned data in
                    Marshal.Copy(rxBufPtr, rxByteBuf, 0, numBytes);
                }
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(txBufPtr);
                Marshal.FreeHGlobal(rxBufPtr);
            }

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the SPI mode. This is used for all Slave Devices on the port
        /// It cannot be individually set on a per slave basis. At least one
        /// non-GPIO slave must be enabled
        /// </summary>
        /// <param name="ssHandle">The SPI Slave Device handle</param>
        /// <returns>the spi mode</returns>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Removing SS file descriptors
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public SPIModeEnum GetMode()
        {
            // get first slave device. We need the file descriptor
            SPISlaveDeviceHandle ssHandle = PortDevice;
            // sanity check
            if (ssHandle == null)
            {
                throw new Exception ("At least one non GPIO Slave Device must be enabled.");
            }

            uint mode = 0;

            // this is an external call to the libc.so.6 library
            int retVal = ExternalIoCtl(ssHandle.SpiDevFileDescriptor, SPI_IOC_RD_MODE, ref mode);
            // did the call succeed?
            if(retVal < 0)
            {
                // it failed
                throw new Exception("ExternalIoCtl on device " + ssHandle.SPISlaveDevice + " failed. retval="+retVal.ToString());
            }
            return (SPIModeEnum)mode;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the SPI mode. This is used for all Slave Devices on the port
        /// It cannot be individually set on a per slave basis. At least one
        /// non-GPIO slave must be enabled
        /// </summary>
        /// <param name="spiMode">The spi mode to set</param>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Removed SS file descriptors
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public void SetMode(SPIModeEnum spiMode)
        {
            // get first slave device. We need the file descriptor
            SPISlaveDeviceHandle ssHandle = PortDevice;
            // sanity check
            if (ssHandle == null)
            {
                throw new Exception ("At least one non GPIO Slave Device must be enabled.");
            }
                
            // this is an external call to the libc.so.6 library
            uint mode = (uint)spiMode;
            int retVal = ExternalIoCtl(ssHandle.SpiDevFileDescriptor, SPI_IOC_WR_MODE, ref mode);
            // did the call succeed?
            if(retVal < 0)
            {
                // it failed
                throw new Exception("ExternalIoCtl on device " + ssHandle.SPISlaveDevice + " failed. retval="+retVal.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the Default SPI speed in Hertz. This value can be overridden on 
        /// a per slave basis by setting the SpeedInHz in the SPISlaveDeviceHandle
        /// </summary>
        /// <returns>the spi port speed in Hertz</returns>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Removed SS file descriptors
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public uint GetDefaultSpeedInHz()
        {
            // get first slave device. We need the file descriptor
            SPISlaveDeviceHandle ssHandle = PortDevice;
            // sanity check
            if (ssHandle == null)
            {
                throw new Exception ("At least one non GPIO Slave Device must be enabled.");
            }

            uint speedInHz = 0;

            // this is an external call to the libc.so.6 library
            int retVal = ExternalIoCtl(ssHandle.SpiDevFileDescriptor, SPI_IOC_RD_MAX_SPEED_HZ, ref speedInHz);
            // did the call succeed?
            if(retVal < 0)
            {
                // it failed
                throw new Exception("ExternalIoCtl on device " + ssHandle.SPISlaveDevice + " failed. retval="+retVal.ToString());
            }
            return speedInHz;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the Default SPI speed in Hertz. This value can be overridden on 
        /// a per slave basis by setting the SpeedInHz in the SPISlaveDeviceHandle
        /// </summary>
        /// <param name="spiSpeedInHz">The speed in Hertz to set</param>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Removed SS file descriptors
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public void SetDefaultSpeedInHz(uint spiSpeedInHz)
        {
            // get first slave device. We need the file descriptor
            SPISlaveDeviceHandle ssHandle = PortDevice;
            // sanity check
            if (ssHandle == null)
            {
                throw new Exception ("At least one non GPIO Slave Device must be enabled.");
            }

            // this is an external call to the libc.so.6 library
            uint speedInHz = (uint)spiSpeedInHz;
            int retVal = ExternalIoCtl(ssHandle.SpiDevFileDescriptor, SPI_IOC_WR_MAX_SPEED_HZ, ref speedInHz);
            // did the call succeed?
            if(retVal < 0)
            {
                // it failed
                throw new Exception("ExternalIoCtl on device " + ssHandle.SPISlaveDevice + " failed. retval="+retVal.ToString());
            }
        }

        // CaiB 2017-09-16: Removed this method, as we are no longer using file descriptors for SS control.
        // private SPISlaveDeviceHandle GetFirstSlaveDeviceWithFD()

        // 16 Sep 17  Cai Biesinger - Modified for Scarlet: Prepares the general port file descriptor instead of individual SS line file descriptors.
        private SPISlaveDeviceHandle EnableSPIDevice()
        {
            string deviceFileName;
            // set up now
            deviceFileName = BBBDefinitions.SPIDEV_FILENAME;

            // set up the spi device number, this is based off the port
            // NOTE that SPI port 0 goes to /dev/spidev1.x and SPI port 1
            //      goes to /dev/spidev2.x. That is just the way it is
            if (SPIPort == SPIPortEnum.SPIPORT_0)
            {
                deviceFileName = deviceFileName.Replace("%device%", "1");
            }
            else if (SPIPort == SPIPortEnum.SPIPORT_1)
            {
                deviceFileName = deviceFileName.Replace("%device%", "2");
            }
            else
            {
                // should never happen
                throw new Exception("Unknown SPI Port:" + SPIPort.ToString());
            }

            // set up the spi slave number
            deviceFileName = deviceFileName.Replace("%slave%", "1");

            // we open the file. We have to have an open file descriptor
            // note this is an external call. It has to be because the 
            // ioctl needs an open file descriptor it can use
            int fd = ExternalFileOpen(deviceFileName, O_RDWR | O_NONBLOCK);
            if (fd <= 0)
            {
                throw new Exception("Could not open spidev file:" + deviceFileName);
            }

            //Console.WriteLine("SPIPort Slave Device Enabled: "+ deviceFileName);

            // create a new slave device handle
            SPISlaveDeviceHandle outHandle = new SPISlaveDeviceHandle(SPISlaveDeviceEnum.SPI_SLAVEDEVICE_CS1, fd);
            // return the slave device handle
            return outHandle;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Enables the SPI slave device which uses a GPIO pin as a slave select.
        /// 
        /// NOTE on how this works. When using a GPIO pin as a slave select line
        ///   we still need to open a SPIDev device because we need a device to
        ///   send the data to. The way SPIDev works each spidev device is 
        ///   fundamentally associated with a particular slave select line and 
        ///   this cannot be changed. The GPIO line will be used as a separate
        ///   slave select but the spidev device specific slave select will also
        ///   be asserted whenever the device is writtent to. 
        /// 
        ///   In order to use a GPIO as a slave select you must ignore and not
        ///   electrically attach anything to the slave select pin the SPIDEV
        ///   device uses as it will be asserted on each write. 
        /// 
        ///   In other words, the SPIDev device is needed to shift the data in 
        ///   and out, however you must ignore its internal slave select line 
        ///   entirely if you wish to use GPIO based slave selects. Otherwise
        ///   any device attached to it will be receive every write to the 
        ///   SPIPort no matter which GPIO slave select is also asserted.
        /// 
        /// </summary>
        /// <returns>ssHandle - the handle for the Slave Device or null for fail</returns>
        ///// <param name="spiSlaveDeviceIn">The GPIO of the pin we use as the slave select</param>
        /// <param name="output"> The <see cref="IDigitalOut"/> to prepare for SPI CS use. </param>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Switched from direct GPIO control to IDigitalOut.
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        public void EnableSPIGPIOSlaveDevice(IDigitalOut output)
        {
            // get first slave device. We need to check we have one
            SPISlaveDeviceHandle ssHandle = PortDevice;
            // sanity check
            if (ssHandle == null)
            {
                throw new Exception ("At least one non GPIO Slave Device must be enabled first.");
            }

            // set this high by default, most modes have slave selects high and go low to activate
            output.SetOutput(true);

            //Console.WriteLine("SPIPort GPIO Slave Device Enabled: "+ gpioEnum.ToString());

            // record that we opened this slave device (so we can close it later)
            SlaveDevices.Add(output);
        }

        // CaiB 2017-09-16: Removed, as we no longer use device handles.
        // private void DisableSPISlaveDevice(SPISlaveDeviceHandle ssHandle)

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Disables all SPI slave devices. 
        /// </summary>
        /// <history>
        ///    16 Sep 17  Cai Biesinger - Modified for Scarlet: Disposes IDigitalOut objects instead of closing handles.
        ///    21 Dec 14  Cynic - Originally written
        /// </history>
        private void DisableAllSPISlaveDevices()
        {
            if (SlaveDevices == null) { return; }
            SlaveDevices.ForEach(x => x.Dispose());
            // reset this
            SlaveDevices = new List<IDigitalOut>();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Closes the port. Because we do not open the port at the PortFS level
        /// we cannot close it there either. This overrides the PortFS.ClosePort()
        /// and does what is necessary internally to close things up.
        /// 
        /// </summary>
        /// <history>
        ///    21 Dec 14 Cynic - Originally written
        /// </history>
        public override void ClosePort()
        {
            //Console.WriteLine("SPIPort Closing");
            DisableAllSPISlaveDevices();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the SPI Port. There is no Set accessor this is set in the constructor
        /// </summary>
        /// <history>
        ///    21 Dec 14 Cynic - Originally written
        /// </history>
        public SPIPortEnum SPIPort
        {
            get
            {
                return spiPort;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the PortDirection
        /// </summary>
        /// <history>
        ///    21 Dec 14 Cynic - Originally written
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
                //Console.WriteLine("Disposing SPIPORT");
         
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

        [DllImport("libc", EntryPoint = "ioctl")]
        static extern int ExternalIoCtl(int fd, uint request, ref uint intVal);

        [DllImport("libc", EntryPoint = "ioctl")]
        static extern int ExternalIoCtl(int fd, uint request, ref spi_ioc_transfer xfer);

        [DllImport("libc", EntryPoint = "open")]
        static extern int ExternalFileOpen(string path, int flags);

        [DllImport("libc", EntryPoint = "close")]
        static extern int ExternalFileClose(int fd);

        #endregion
    }
}

