using SDL2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CHIP8
{
    public class Display : IDisposable
    {
        private readonly int SystemWidth;
        private readonly int SystemHeight;
        private readonly int ResolutionMultiplier;
        private readonly IntPtr Window;
        private readonly IntPtr Renderer;
        public Display(int systemWidth, int systemHeight, int resolutionMultiplier)
        {
            SystemWidth = systemWidth;
            SystemHeight = systemHeight;
            ResolutionMultiplier = resolutionMultiplier;

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            Window = SDL.SDL_CreateWindow("CHIP-8",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                SystemWidth * ResolutionMultiplier,
                SystemHeight * ResolutionMultiplier,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );
            Renderer = SDL.SDL_CreateRenderer(Window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
        }

        public void Dispose()
        {
            SDL.SDL_DestroyWindow(Window);          
            SDL.SDL_Quit();
        }

        public void Draw(BitArray gfx)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SDL.SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
            SDL.SDL_RenderClear(Renderer);
            SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0);

            List<SDL.SDL_Point> points = new List<SDL.SDL_Point>();
            for (int y = 0; y < SystemHeight; y++) {
                for (int x = 0; x < SystemWidth; x++) {
                    bool paintPixels = gfx[y*SystemWidth+x];
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
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        }
    }
}