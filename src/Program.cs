namespace CHIP8
{
    class Program
    {
        static void Main(int resolutionMultiplier = 8)
        {
            using (var emulator = new Emulator(resolutionMultiplier))
            {
                emulator.Run();
            }
        }
    }
}
