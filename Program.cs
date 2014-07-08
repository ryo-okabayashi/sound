using NAudio.Wave;
using System;

namespace Sound
{
    class Program
    {
        public static WaveOut waveOut;

        static void Main(string[] args)
        {
            var sound = new MySound();
            sound.SetWaveFormat(44100, 2);
            waveOut = new WaveOut();
            waveOut.Init(sound);
            waveOut.Play();

            ConsoleKeyInfo keyInfo;
            bool loop = true;
            while (loop)
            {
                keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Q)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    loop = false;
                }
            }
        }
    }
}
