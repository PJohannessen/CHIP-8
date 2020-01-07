using SDL2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CHIP8
{
    public class Display : IDisposable
    {
        public const int Width = Constants.Display.Width;
        public const int Height = Constants.Display.Height;
        private readonly BitArray GFX = new BitArray(Height * Width, false);
        private readonly int ResolutionMultiplier;
        private readonly IntPtr Window;
        private readonly IntPtr Renderer;
        
        public Display(int resolutionMultiplier)
        {
            ResolutionMultiplier = resolutionMultiplier;

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            Window = SDL.SDL_CreateWindow("CHIP-8",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                Width * ResolutionMultiplier,
                Height * ResolutionMultiplier,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );
            Renderer = SDL.SDL_CreateRenderer(Window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
        }

        public bool Draw(int x, int y, byte b)
        {
            bool collisionDetected = false;
            for (int j = 7; j >= 0; j--) {
                bool set = (b & (1 << j)) != 0;
                int yPos = (y % Height) * Width;
                int xPos = (x+Math.Abs(j-7)) % Width;
                int pos = yPos+xPos;
                if (GFX[pos] == true && set == true) collisionDetected = true;
                GFX[pos] = GFX[pos] != set;
            }
            return collisionDetected;
        }

        public void Render()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0);
            SDL.SDL_RenderClear(Renderer);
            SDL.SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);

            List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    bool paintPixels = GFX[y*Width+x];
                    if (paintPixels) {
                        for (int y2 = 0; y2 < ResolutionMultiplier; y2++) {
                            for (int x2 = 0; x2 < ResolutionMultiplier; x2++) {
                                SDL.SDL_Point p;
                                p.x = x*ResolutionMultiplier+x2;
                                p.y = y*ResolutionMultiplier+y2;
                                points.Add(p);
                            }
                        }
                    }
                }
            }
            SDL.SDL_RenderDrawPoints(Renderer, points.ToArray(), points.Count);
            SDL.SDL_RenderPresent(Renderer);
            sw.Stop();
        }

        public void Clear()
        {
            GFX.SetAll(false);
        }

        public void Dispose()
        {
            SDL.SDL_DestroyWindow(Window);          
            SDL.SDL_Quit();
        }
    }
}