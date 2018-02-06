# Scarlet
**Please read the documentation found in [our OneNote notebook](https://1drv.ms/u/s!AseFMLuUHe5LlrNT8SEuX9cTHDTddA)!**

<img alt="Scarlet Logo" src="_Logo/ScarletLogo.png" width="192">

**Scarlet** is a C# robotics library, originally designed for the Husky Robotics science station for 2017-18. It is meant to be modular, flexible, cross-platform, and usable by all subsystems, and anyone else. Some current library features include:
- Packet building, interpreting, and networking (send/receive, connection management)
- Sensor and motor frameworks, and some sensor/motor implementations
- Raspberry Pi and BeagleBone Black hardware interfacing (GPIO, Interrupts, PWM, I2C, SPI, UART, CAN, ADC)
- Simple persistent data storage
- Filters
- Configurable logging
- Some miscellaneous utilities

To use it, simply install the library into your solution, from our [releases](https://github.com/huskyroboticsteam/Scarlet/releases), via [NuGet](https://www.nuget.org/packages/HuskyRobotics.Scarlet/), or compile your own version from source. Then, read the _Getting Started_ page in our OneNote documentation, then build & copy your program, as well as Scarlet and its minimal dependencies onto your target platform. Code may require minimal changes between platforms in some limited circumstances (hardware I/O).

Scarlet makes some particularly tedious aspects of robotics software very simple, such as managing unreliable network connections, cross-platform hardware IO (and the related device tree edits on BBB), and comes with several sensors and motor controller implementations out of the box, which means less work and time for you until you're up and running.

You can find contact info, more details, and a lot more in our [our OneNote notebook](https://1drv.ms/u/s!AseFMLuUHe5LlrNT8SEuX9cTHDTddA).

Scarlet is under the [GNU Lesser General Public License v3.0](LICENSE.txt).