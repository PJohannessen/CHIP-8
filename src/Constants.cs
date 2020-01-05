using SDL2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CHIP8
{
    public static class Constants
    {
       public static class Memory
       {
           public const int TotalMemory = 0x1000;
           public const int ProgramMemoryStart = 0x200;
       }

       public static class Display
       {
           public const int Width = 64;
           public const int Height = 32;
       }
    }
}