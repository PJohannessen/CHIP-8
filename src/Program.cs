﻿using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace CHIP8
{
    class Program
    {
        static void Main(string[] args)
        {
            var emulator = new Emulator();
            emulator.Init();
            emulator.Load();
            emulator.Run();
            Console.Read();
        }
    }

    public class Emulator
    {
        private const int Height = 32;
        private const int Width = 64;
        private byte[] V = new byte[16];
        private byte[] M = new byte[4096];
        private ushort I = 0;
        private ushort PC = 0x200; // Program Counter
        private ushort SP = 0; // Stack Pointer
        private byte DT = 0; // Delay Timer
        private byte ST = 0; // Sound Timer
        private Random R = new Random();
        private BitArray GFX = new BitArray(Height * Width, false);

        private byte VF { set { V[15] = value; } }

        public void Init()
        {
            Span<byte> sprites = M.AsSpan(0, 80);
            new Span<byte>(new byte[] {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            }).CopyTo(sprites);
        }

        public void Run()
        {
            while (true)
            {
                bool shouldDraw = Process();
                if (shouldDraw) {
                    Console.Clear();
                    for (int y = 0; y < Height; y++) {
                        string s = new string(Enumerable.Range(0, Width).Select(x => GFX[y*Width+x] ? '#' : ' ').ToArray());
                        Console.WriteLine(s);
                    }
                }
            }
        }

        private bool Process() {
            ushort opcode = Fetch();
            switch (opcode & 0xF000) {
                case 0x0000:
                    switch (opcode & 0x00FF) {
                        case 0xE0: // 00E0: Clear the screen
                            GFX = new BitArray(Height * Width, false);
                            PC += 2;
                            return true;
                        case 0xEE: // 00EE: Return from a subroutine
                            throw new NotImplementedException("OOEE opcode not yet supported");
                        default: // 0NNN: Execute machine language subroutine at address NNN
                            throw new NotImplementedException("ONNN opcode not yet supported");
                    }
                case 0x1000: // 1NNN: Jump to address NNN
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x2000: // 2NNN: Execute subroutine starting at address NNN
                    throw new NotImplementedException("2NNN opcode not yet supported");
                case 0x3000: // 3XNN: Skip the following instruction if the value of register VX equals NN
                    throw new NotImplementedException("3XNN opcode not yet supported");
                case 0x4000: // 4XNN: Skip the following instruction if the value of register VX is not equal to NN
                    throw new NotImplementedException("4XNN opcode not yet supported");
                case 0x5000: // 5XY0: Skip the following instruction if the value of register VX is equal to the value of register VY
                    throw new NotImplementedException("5XY0 opcode not yet supported");
                case 0x6000: { // 6XNN: Store number NN in register VX
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte val = (byte)(opcode & 0x00FF);
                    V[register] = val;
                    PC += 2;
                    break;
                }
                case 0x7000: // 7XNN: Add the value NN to register VX
                    break;
                case 0x8000:
                    switch (opcode & 0x000F) {
                        case 0x0: { // 8XY0: Store the value of register VY in register VX
                            byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            V[regX] = V[regY];
                            break;
                        }
                        case 0x1: { // 8XY1: Set VX to VX OR VY
                            byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            V[regX] = (byte)(V[regX] | V[regY]);
                            break;
                        }
                        case 0x2: { // 8XY2: Set VX to VX AND VY
                            byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            V[regX] = (byte)(V[regX] & V[regY]);
                            break;
                        }
                        case 0x3: { // 8XY3: Set VX to VX XOR VY
                            byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            V[regX] = (byte)(V[regX] ^ V[regY]);
                            break;
                        }
                        case 0x4: { // 8XY4: Add the value of register VY to register VX. Set VF to 01 if a carry occurs. Set VF to 00 if a carry does not occur.
                            byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            int sum = V[regX] + V[regY];
                            if (sum >= 256) {
                                V[regX] = (byte)(sum % 256);
                                VF = 1;
                            } else {
                                V[regX] = (byte)sum;
                                VF = 0;
                            }
                            break;
                        }
                        case 0x5: { // 8XY5: Subtract the value of register VY from register VX. Set VF to 00 if a borrow occurs. Set VF to 01 if a borrow does not occur.
                                                        byte regX = (byte)((opcode & 0x0F00) >> 8);
                            byte regY = (byte)((opcode & 0x00F0) >> 4);
                            int result = V[regX] - V[regY];
                            if (result < 0) {
                                V[regX] = (byte)(result % 256);
                                VF = 1;
                            } else {
                                V[regX] = (byte)result;
                                VF = 0;
                            }
                            break;
                        }
                        case 0x6: // 8XY6: 
                            throw new NotImplementedException("8XY6 opcode not yet supported");
                        case 0x7: // 8XY7: 
                            throw new NotImplementedException("8XY7 opcode not yet supported");
                        case 0xE: // 8XYE: 
                            throw new NotImplementedException("8XYE opcode not yet supported");
                        default:
                            throw new Exception($"Unrecognised opcode {opcode}");
                    }
                    break;
                case 0x9000: // 9XY0: Skip the following instruction if the value of register VX is not equal to the value of register VY
                    break;
                case 0xA000: // ANNN: Store memory address NNN in register I
                    I = (ushort)(opcode & 0x0FFF);
                    PC += 2;
                    break;
                case 0xB000: // BNNN: Jump to address NNN + V0
                    PC = (ushort)((opcode & 0x0FFF) + V[0]);
                    break;
                case 0xC000: { // CXNN: Set VX to a random number with a mask of NN
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte mask = (byte)(opcode & 0x00FF);
                    byte rnd = (byte)(R.Next(0, 255) & mask);
                    V[register] =  rnd;
                    PC += 2;
                } break;
                // DXYN: Draw a sprite at position VX, VY
                // with N bytes of sprite data starting at the address stored in I.
                // Set VF to 01 if any set pixels are changed to unset, and 00 otherwise
                case 0xD000: {
                    byte rX = (byte)((opcode & 0x0F00) >> 8);
                    byte x = V[rX];
                    byte rY = (byte)((opcode & 0x00F0) >> 4);
                    byte y = V[rY];
                    byte len = (byte)(opcode & 0x000F);
                    for (int i = 0; i < len; i++) {
                        byte b = M[I+i];
                        for (int j = 7; j >= 0; j--) {
                            bool set = (b & (1 << j)) != 0;
                            int pos = ((y+i)*Width)+(x+Math.Abs(j-7));
                            GFX[pos] = GFX[pos] != set;
                        }
                    }
                    PC += 2;
                    return true;
                }
                case 0xE000:
                    switch (opcode & 0x00FF) {
                        case 0x9E: // EX9E: 
                            throw new NotImplementedException("EX9E opcode not yet supported");
                        case 0xA1: // EXA1: 
                            throw new NotImplementedException("EXA1 opcode not yet supported");
                        default:
                            throw new Exception($"Unrecognised opcode {opcode}");
                    }
                case 0xF000:
                    switch (opcode & 0x00FF) {
                        case 0x07: // FX07: 
                            throw new NotImplementedException("FX07 opcode not yet supported");
                        case 0x0A: { // FX0A: 
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            int input = Console.Read();
                            V[register] = (byte)input;
                            PC += 2;
                            break;
                        }
                        case 0x15: // FX15: 
                            throw new NotImplementedException("FX15 opcode not yet supported");
                        case 0x18: // FX18: 
                            throw new NotImplementedException("FX18 opcode not yet supported");
                        case 0x1E: // FX1E: 
                            throw new NotImplementedException("FX1E opcode not yet supported");
                        case 0x29: {// FX29: 
                            byte rX = (byte)((opcode & 0x0F00) >> 8);
                            I = (ushort)(V[rX] * 5);
                            byte something = M[I];
                            PC += 2;
                            break;
                        }
                        case 0x33: {// FX33: 
                            byte rX = (byte)((opcode & 0x0F00) >> 8);
                            byte x = V[rX];
                            M[I] = (byte)(x / 100 % 10);
                            M[I+1] = (byte)(x / 10 % 10);
                            M[I+2] = (byte)(x % 10);
                            PC += 2;
                            break;
                        }
                        case 0x55: // FX55: 
                            throw new NotImplementedException("FX55 opcode not yet supported");
                        case 0x65: { // FX65: 
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            for (byte b = 0; b <= register; b++) {
                                V[b] = M[I+b];
                            }
                            PC += 2;
                            break;
                        }
                        default:
                            throw new Exception($"Unrecognised opcode {opcode}");
                    }
                    break;
                default:
                    throw new Exception($"Unrecognised opcode {opcode}");
            }

            return false;
        }

        public void Load()
        {
            byte[] rom = File.ReadAllBytes("../roms/Random Number Test [Matthew Mikolay, 2010].ch8");
            for (int i = 0; i < rom.Length; i=i+2)
            {
                M[0x200+i] = rom[i];
                M[0x200+i+1] = rom[i+1];
            }
        }

        private ushort Fetch()
        {
            ushort opcode = (ushort)((M[PC] << 8) | M[PC+1]);
            return opcode;
        }
    }
}