using System;
using System.Collections.Generic;
using System.IO;
using SDL2;

namespace CHIP8
{
    public class Emulator : IDisposable
    {
        private byte[] V = new byte[16];
        private byte[] M = new byte[Constants.Memory.TotalMemory];
        private ushort I = 0;
        private ushort PC = 0x200; // Program Counter
        private ushort SP = 0; // Stack Pointer
        private byte DT = 0; // Delay Timer
        private byte ST = 0; // Sound Timer
        private Random R = new Random();

        private byte VF
        {
            get { return V[0xF]; }
            set { V[0xF] = value; }
        }

        private readonly Display _display;
        private readonly Loader _loader;
        private readonly Stack<ushort> CallStack = new Stack<ushort>();

        public Emulator(int resolutionMultiplier)
        {
            _display = new Display(resolutionMultiplier);
            _loader = new Loader(M.AsMemory(Constants.Memory.ProgramMemoryStart));
            _loader.LoadRom(new FileInfo("../roms/c8_test.c8"));

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

        private const int MS_PER_FRAME = (1000/60);

        public void Run()
        {
            SDL.SDL_Event e;
            bool quit = false;
            uint previous = SDL.SDL_GetTicks();
            uint lag = 0;
            bool shouldDraw = false;

            while (!quit)
            {
                uint current = SDL.SDL_GetTicks();
                uint elapsed = current - previous;
                previous = current;
                lag += elapsed;
                SDL.SDL_PollEvent(out e);

                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    quit = true;
                }

                shouldDraw = Process() || shouldDraw;
                SDL.SDL_Delay(2);
                while (lag >= MS_PER_FRAME)
                {
                    if (shouldDraw) _display.Render();
                    lag -= MS_PER_FRAME;
                    shouldDraw = false;
                    if (ST == 1)
                    {
                        Console.WriteLine("Beeping");
                        Console.Beep();
                    }
                    if (ST > 0) ST--;
                    if (DT > 0) DT--;
                }
            }
        }

        private bool Process() {
            ushort opcode = Fetch();
            switch (opcode & 0xF000) {
                case 0x0000:
                    switch (opcode & 0x00FF) {
                        case 0xE0: // 00E0: Clear the screen
                            _display.Clear();
                            PC += 2;
                            return true;
                        case 0xEE: // 00EE: Return from a subroutine
                            PC = CallStack.Pop();
                            break;
                        default: // 0NNN: Execute machine language subroutine at address NNN
                            PC += 2;
                            break;
                    }
                    break;
                case 0x1000: // 1NNN: Jump to address NNN
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x2000: // 2NNN: Execute subroutine starting at address NNN
                    CallStack.Push((ushort)(PC + 2));
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x3000: { // 3XNN: Skip the following instruction if the value of register VX equals NN
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte val = (byte)(opcode & 0x00FF);
                    if (V[register] == val) PC += 4;
                    else PC += 2;
                    break;
                }
                case 0x4000: { // 4XNN: Skip the following instruction if the value of register VX is not equal to NN
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte val = (byte)(opcode & 0x00FF);
                    if (V[register] != val) PC += 4;
                    else PC += 2;
                    break;
                }
                case 0x5000: { // 5XY0: Skip the following instruction if the value of register VX is equal to the value of register VY
                    byte registerX = (byte)((opcode & 0x0F00) >> 8);
                    byte registerY = (byte)((opcode & 0x00F0) >> 4);
                    if (V[registerX] == V[registerY]) PC += 4;
                    else PC += 2;
                    break;
                }
                case 0x6000: { // 6XNN: Store number NN in register VX
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte val = (byte)(opcode & 0x00FF);
                    V[register] = val;
                    PC += 2;
                    break;
                }
                case 0x7000: {// 7XNN: Add the value NN to register VX
                    byte register = (byte)((opcode & 0x0F00) >> 8);
                    byte val = (byte)(opcode & 0x00FF);
                    V[register] += val;
                    PC += 2;
                    break;
                }
                case 0x8000: {
                    byte regX = (byte)((opcode & 0x0F00) >> 8);
                    byte regY = (byte)((opcode & 0x00F0) >> 4);
                    switch (opcode & 0x000F) {
                        case 0x0: { // 8XY0: Store the value of register VY in register VX
                            V[regX] = V[regY];
                            PC += 2;
                            break;
                        }
                        case 0x1: { // 8XY1: Set VX to VX OR VY
                            V[regX] = (byte)(V[regX] | V[regY]);
                            PC += 2;
                            break;
                        }
                        case 0x2: { // 8XY2: Set VX to VX AND VY
                            V[regX] = (byte)(V[regX] & V[regY]);
                            PC += 2;
                            break;
                        }
                        case 0x3: { // 8XY3: Set VX to VX XOR VY
                            V[regX] = (byte)(V[regX] ^ V[regY]);
                            PC += 2;
                            break;
                        }
                        case 0x4: { // 8XY4: Add the value of register VY to register VX. Set VF to 01 if a carry occurs. Set VF to 00 if a carry does not occur.
                            int sum = V[regX] + V[regY];
                            if (sum >= 256) {
                                V[regX] = (byte)(sum % 256);
                                VF = 1;
                            } else {
                                V[regX] = (byte)sum;
                                VF = 0;
                            }
                            PC += 2;
                            break;
                        }
                        case 0x5: { // 8XY5: Subtract the value of register VY from register VX. Set VF to 00 if a borrow occurs. Set VF to 01 if a borrow does not occur.
                            int result = V[regX] - V[regY];
                            if (result < 0) {
                                V[regX] = (byte)(result % 256);
                                VF = 0;
                            } else {
                                V[regX] = (byte)result;
                                VF = 1;
                            }
                            PC += 2;
                            break;
                        }
                        case 0x6: { // 8XY6: Store the value of register VY shifted right one bit in register VX¹. Set register VF to the least significant bit prior to the shift. VY is unchanged.
                            VF = (byte)(V[regY] & 0x0001);
                            V[regX] = (byte)(V[regY] >> 1);
                            PC += 2;
                            break;
                        }
                        case 0x7: { // 8XY7: Set register VX to the value of VY minus VX. Set VF to 00 if a borrow occurs. Set VF to 01 if a borrow does not occur.
                            int result = V[regY] - V[regX];
                            if (result < 0) {
                                V[regX] = (byte)(result % 256);
                                VF = 0;
                            } else {
                                V[regX] = (byte)result;
                                VF = 1;
                            }
                            PC += 2;
                            break;
                        }
                        case 0xE: { // 8XYE: Store the value of register VY shifted left one bit in register VX¹. Set register VF to the most significant bit prior to the shift. VY is unchanged.
                            VF = (byte)((V[regY] >> 7) & 0x0001);
                            V[regX] = (byte)(V[regY] << 1);
                            PC += 2;
                            break;
                        }
                        default:
                            throw new Exception($"Unrecognised opcode {opcode}");
                    }
                    break;
                }
                case 0x9000: { // 9XY0: Skip the following instruction if the value of register VX is not equal to the value of register VY
                    byte regX = (byte)((opcode & 0x0F00) >> 8);
                    byte regY = (byte)((opcode & 0x00F0) >> 4);
                    if (V[regX] != V[regY]) PC += 4;
                    else PC += 2;
                    break;
                }
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
                    bool collision = false;
                    byte rX = (byte)((opcode & 0x0F00) >> 8);
                    byte x = V[rX];
                    byte rY = (byte)((opcode & 0x00F0) >> 4);
                    byte y = V[rY];
                    byte len = (byte)(opcode & 0x000F);
                    for (int i = 0; i < len; i++) {
                        byte b = M[I+i];
                        collision = _display.Draw(x, y+i, b) || collision;
                    }
                    if (collision) VF = 1;
                    else VF = 0;
                    PC += 2;
                    return true;
                }
                case 0xE000: {
                    SDL.SDL_Event e;
                    SDL.SDL_PollEvent(out e);
                    int input = -1;
                    if (e.type == SDL.SDL_EventType.SDL_KEYDOWN) {
                        switch (e.key.keysym.sym) {
                            case SDL.SDL_Keycode.SDLK_1:
                                input = 0x1;
                                break;
                            case SDL.SDL_Keycode.SDLK_2:
                                input = 0x2;
                                break;
                            case SDL.SDL_Keycode.SDLK_3:
                                input = 0x3;
                                break;
                            case SDL.SDL_Keycode.SDLK_4:
                                input = 0xC;
                                break;
                            case SDL.SDL_Keycode.SDLK_q:
                                input = 0x4;
                                break;
                            case SDL.SDL_Keycode.SDLK_w:
                                input = 0x5;
                                break;
                            case SDL.SDL_Keycode.SDLK_e:
                                input = 0x6;
                                break;
                            case SDL.SDL_Keycode.SDLK_r:
                                input = 0xD;
                                break;
                            case SDL.SDL_Keycode.SDLK_a:
                                input = 0x7;
                                break;
                            case SDL.SDL_Keycode.SDLK_s:
                                input = 0x8;
                                break;
                            case SDL.SDL_Keycode.SDLK_d:
                                input = 0x9;
                                break;
                            case SDL.SDL_Keycode.SDLK_f:
                                input = 0xE;
                                break;
                            case SDL.SDL_Keycode.SDLK_z:
                                input = 0xA;
                                break;
                            case SDL.SDL_Keycode.SDLK_x:
                                input = 0x0;
                                break;
                            case SDL.SDL_Keycode.SDLK_c:
                                input = 0xB;
                                break;
                            case SDL.SDL_Keycode.SDLK_v:
                                input = 0xF;
                                break;
                            default:
                                break;
                        }
                    }
                    byte X = (byte)((opcode & 0x0F00) >> 8);
                    switch (opcode & 0x00FF) {
                        case 0x9E: // EX9E: Skip the following instruction if the key corresponding to the hex value currently stored in register VX is pressed
                            if (V[X] == input) PC += 4;
                            else PC += 2;
                            break;
                        case 0xA1: // EXA1: Skip the following instruction if the key corresponding to the hex value currently stored in register VX is not pressed
                            if (V[X] != input) PC += 4;
                            else PC += 2;
                            break;
                        default:
                            throw new Exception($"Unrecognised opcode {opcode}");
                    }
                    break;
                }
                case 0xF000:
                    switch (opcode & 0x00FF) {
                        case 0x07: { // FX07: Store the current value of the delay timer in register VX
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            V[register] = DT;
                            PC += 2;
                            break;
                        }
                        case 0x0A: { // FX0A: 
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            int input = -1;
                            while (input < 0)
                            { 
                                SDL.SDL_Event e;
                                SDL.SDL_WaitEvent(out e);
                                if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                                {
                                    switch (e.key.keysym.sym) {
                                        case SDL.SDL_Keycode.SDLK_1:
                                            input = 0x1;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_2:
                                            input = 0x2;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_3:
                                            input = 0x3;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_4:
                                            input = 0xC;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_q:
                                            input = 0x4;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_w:
                                            input = 0x5;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_e:
                                            input = 0x6;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_r:
                                            input = 0xD;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_a:
                                            input = 0x7;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_s:
                                            input = 0x8;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_d:
                                            input = 0x9;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_f:
                                            input = 0xE;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_z:
                                            input = 0xA;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_x:
                                            input = 0x0;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_c:
                                            input = 0xB;
                                            break;
                                        case SDL.SDL_Keycode.SDLK_v:
                                            input = 0xF;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            V[register] = (byte)input;
                            PC += 2;
                            break;
                        }
                        case 0x15: { // FX15: Set the delay timer to the value of register VX
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            DT = V[register];
                            PC += 2;
                            break;
                        }
                        case 0x18: { // FX18: Set the sound timer to the value of register VX
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            Console.WriteLine($"Setting ST to {V[register]}");
                            ST = V[register];
                            PC += 2;
                            break;
                        }
                        case 0x1E: { // FX1E: Add the value stored in register VX to register I
                            byte register = (byte)((opcode & 0x0F00) >> 8);
                            I += V[register];
                            PC += 2;
                            break;
                        }
                        case 0x29: { // FX29: Set I to the memory address of the sprite data corresponding to the hexadecimal digit stored in register VX
                            byte rX = (byte)((opcode & 0x0F00) >> 8);
                            I = (ushort)(V[rX] * 5);
                            byte something = M[I];
                            PC += 2;
                            break;
                        }
                        case 0x33: { // FX33: Store the binary-coded decimal equivalent of the value stored in register VX at addresses I, I + 1, and I + 2
                            byte rX = (byte)((opcode & 0x0F00) >> 8);
                            byte x = V[rX];
                            M[I] = (byte)(x / 100 % 10);
                            M[I+1] = (byte)(x / 10 % 10);
                            M[I+2] = (byte)(x % 10);
                            PC += 2;
                            break;
                        }
                        case 0x55: { // FX55: Store the values of registers V0 to VX inclusive in memory starting at address I. I is set to I + X + 1 after operation².
                            byte X = (byte)((opcode & 0x0F00) >> 8);
                            for (int i = 0; i <= X; i++)
                            {
                                M[I+i] = V[i];
                            }
                            I = (ushort)(I+X+1);
                            PC += 2;
                            break;
                        }
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

        private ushort Fetch()
        {
            ushort opcode = (ushort)((M[PC] << 8) | M[PC+1]);
            return opcode;
        }

        public void Dispose()
        {
            _display.Dispose();
        }
    }
}
