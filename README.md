# CHIP-8

Yet another CHIP-8 emulator/interpreter, this one written in C# running on .NET Core 3.1.

This project is purely for fun and self education. There is a wealth of more detailed and accurate resources available. Should I ever complete enough of it I intend to create (yet another) blog series detailing its creation.

## Prerequisites

* [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [SDL2](https://www.libsdl.org/download-2.0.php)

Tested on Windows 10 and MacOS Catalina, but should be compatible anywhere that .NET Core 3.1 and SDL2 are supported.

## Getting Started

* Install .NET Core 3.1
* Install .NET 
* `cd /src`
* `dotnet run`

## Resources

This project would not be possible without the following:

* [Matthew Mikolay's Mastering CHIP-8](http://mattmik.com/files/chip8/mastering/chip8.html)
* [mirz's CHIP-8 Emulator](http://mir3z.github.io/chip8-emu/) for comparing ROM execution
* [Emulating a PlayStation 1 (PSX) entirely with C# and .NET](https://www.hanselman.com/blog/EmulatingAPlayStation1PSXEntirelyWithCAndNET.aspx) for the initial spark of inspiration