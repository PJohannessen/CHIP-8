using System;
using System.IO;

namespace CHIP8
{
    public class Loader
    {
        private readonly Memory<byte> Memory;
        public Loader(Memory<byte> memory)
        {
            Memory = memory;
        }

        public void LoadRom(FileInfo romFile)
        {
            byte[] rom = File.ReadAllBytes(romFile.FullName);
            if (rom.Length > Memory.Length)
            {
                throw new Exception("Not enough memory to load rom");
            }
            
            rom.CopyTo(Memory);
        }
    }
}