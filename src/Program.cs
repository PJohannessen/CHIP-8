namespace CHIP8
{
    class Program
    {
        static void Main(int resolutionMultiplier = 8, bool quirksMode = false)
        {
            using (var emulator = new Emulator(resolutionMultiplier, quirksMode))
            {
                emulator.Run();
            }
        }
    }
}
